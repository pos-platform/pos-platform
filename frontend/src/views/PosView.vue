<script setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { RouterLink } from 'vue-router'
import { productsApi, categoriesApi, salesApi, shiftsApi, promotionsApi, storesApi, discountPercentFor } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

const products = ref([])
const categories = ref([])
const stores = ref([])
const selectedBranchId = ref('')
const activeCategory = ref('')
const activePromos = ref([])
const search = ref('')
const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const lastSale = ref(null)

// Idempotentlik kaliti: javob kelmay qolganda (tarmoq/timeout) kassir qayta bosadi —
// o'sha kalit bilan ketgan so'rovga server yangi savdo yaratmay, avvalgisini qaytaradi.
// Savat yoki summa o'zgarsa kalit bekor qilinadi (bu — boshqa savdo).
const clientSaleId = ref('')
function newClientSaleId() {
  return crypto.randomUUID ? crypto.randomUUID().replace(/-/g, '') : String(Date.now()) + Math.random().toString(16).slice(2)
}

// --- Smena ---
const shift = ref(null)
const openingCash = ref(0)
const shiftBusy = ref(false)

// --- Multi-session (bir vaqtda bir nechta savdo) ---
let nextSessionId = 2
const sessions = ref([{ id: 1, cart: [], paidAmount: 0, paymentMethod: 'Card' }])
const activeSessionId = ref(1)

const currentSession = computed(
  () => sessions.value.find((s) => s.id === activeSessionId.value) || sessions.value[0]
)

function addSession() {
  const id = nextSessionId++
  sessions.value.push({ id, cart: [], paidAmount: 0, paymentMethod: 'Card' })
  activeSessionId.value = id
}

function removeSession(id) {
  if (sessions.value.length === 1) return
  const idx = sessions.value.findIndex((s) => s.id === id)
  sessions.value.splice(idx, 1)
  if (activeSessionId.value === id) {
    activeSessionId.value = sessions.value[Math.max(0, idx - 1)].id
  }
}

// --- Writable computed shortcuts for current session ---
const cart = computed(() => currentSession.value.cart)

const paymentMethod = computed({
  get: () => currentSession.value.paymentMethod,
  set: (v) => { currentSession.value.paymentMethod = v }
})

const paidAmount = computed({
  get: () => currentSession.value.paidAmount,
  set: (v) => { currentSession.value.paidAmount = v }
})

function round2(n) {
  return Math.round((Number(n) || 0) * 100) / 100
}

// --- Aksiya (chegirma) ---
function discPct(product) {
  return discountPercentFor(product, activePromos.value)
}
function effUnitPrice(product) {
  return round2(Number(product.price) * (1 - discPct(product) / 100))
}
function lineTotal(line) {
  const gross = Number(line.product.price) * Number(line.quantity || 0)
  const discount = round2((gross * discPct(line.product)) / 100)
  return round2(gross - discount)
}

const filtered = computed(() => {
  let list = products.value
  if (activeCategory.value) {
    list = list.filter((p) => p.categoryId === activeCategory.value)
  }
  const q = search.value.trim().toLowerCase()
  if (!q) return list
  return list.filter(
    (p) =>
      p.name.toLowerCase().includes(q) ||
      (p.barcode && p.barcode.toLowerCase().includes(q)) ||
      p.mxikCode.toLowerCase().includes(q)
  )
})

const total = computed(() => cart.value.reduce((sum, l) => sum + lineTotal(l), 0))
const change = computed(() => Math.max(0, Number(paidAmount.value || 0) - total.value))
const cartItemCount = computed(() =>
  cart.value.reduce((sum, l) => sum + Number(l.quantity || 0), 0)
)

function addToCart(product) {
  const existing = cart.value.find((l) => l.product.id === product.id)
  if (existing) existing.quantity = Number(existing.quantity) + 1
  else cart.value.push({ product, quantity: 1 })
}
function inc(line) {
  line.quantity = round2(Number(line.quantity) + 1)
}
function dec(line) {
  const q = round2(Number(line.quantity) - 1)
  if (q <= 0) removeLine(line.product.id)
  else line.quantity = q
}
function removeLine(id) {
  currentSession.value.cart = currentSession.value.cart.filter((l) => l.product.id !== id)
}
function clearCart() {
  currentSession.value.cart = []
  currentSession.value.paidAmount = 0
}

function setFullAmount() {
  currentSession.value.paidAmount = total.value
}

// Shtrix-kod skaner: barcode + Enter -> mahsulotni savatga qo'shadi
function onScan() {
  const q = search.value.trim().toLowerCase()
  if (!q) return
  const exact =
    products.value.find((p) => p.barcode && p.barcode.toLowerCase() === q) ||
    products.value.find((p) => p.pluNumber != null && String(p.pluNumber) === q) ||
    products.value.find((p) => p.name.toLowerCase() === q) ||
    filtered.value[0]
  if (exact) {
    addToCart(exact)
    search.value = ''
  } else {
    error.value = `Mahsulot topilmadi: ${search.value}`
  }
}

// F12 -> To'lash
function onKeydown(e) {
  if (e.key === 'F12') {
    e.preventDefault()
    if (!submitting.value && cart.value.length && shift.value) checkout()
  }
}

// Chek chop etish
function printReceipt(sale) {
  const itemsHtml = (sale._items || [])
    .map(
      (item) => `<tr>
      <td>${item.name}</td>
      <td style="text-align:right">${Number(item.quantity).toLocaleString()} ${item.unit === 'Kilogram' ? 'kg' : 'dona'}</td>
      <td style="text-align:right">${Number(item.unitPrice).toLocaleString()}</td>
      <td style="text-align:right">${Number(item.totalPrice).toLocaleString()}</td>
    </tr>`
    )
    .join('')

  const w = window.open('', '_blank', 'width=380,height=620')
  w.document.write(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8"/>
  <title>Chek ${sale.receiptNumber}</title>
  <style>
    * { margin:0; padding:0; box-sizing:border-box; }
    body { font-family: monospace; font-size: 13px; padding: 16px; }
    h2 { text-align:center; font-size:15px; margin-bottom:4px; }
    .center { text-align:center; }
    .muted { color:#666; }
    hr { border:none; border-top:1px dashed #999; margin:8px 0; }
    table { width:100%; border-collapse:collapse; }
    th { text-align:left; padding:3px 0; border-bottom:1px solid #ccc; }
    td { padding:3px 0; vertical-align:top; }
    .row { display:flex; justify-content:space-between; margin:3px 0; }
    .bold { font-weight:bold; font-size:14px; }
    @page { size: 80mm auto; margin: 4mm; }
    @media print { body { padding:4px; } }
  </style>
</head>
<body>
  <h2>PosPlatform</h2>
  <div class="center muted">${new Date(sale.createdAt || Date.now()).toLocaleString('uz-UZ')}</div>
  <div class="center">Chek: <strong>${sale.receiptNumber}</strong></div>
  <hr/>
  <table>
    <thead><tr>
      <th>Nomi</th>
      <th style="text-align:right">Soni</th>
      <th style="text-align:right">Narx</th>
      <th style="text-align:right">Jami</th>
    </tr></thead>
    <tbody>${itemsHtml}</tbody>
  </table>
  <hr/>
  <div class="row bold"><span>Jami:</span><span>${Number(sale.totalAmount).toLocaleString()} so'm</span></div>
  ${Number(sale.discountAmount) > 0 ? `<div class="row"><span>Chegirma:</span><span>${Number(sale.discountAmount).toLocaleString()} so'm</span></div>` : ''}
  <div class="row"><span>To'lov:</span><span>${sale.paymentMethod === 'Cash' ? 'Naqd' : sale.paymentMethod === 'Card' ? 'Karta' : sale.paymentMethod === 'Transfer' ? "O'tkazma" : 'Aralash'}</span></div>
  ${sale.paymentMethod === 'Cash' ? `<div class="row"><span>Qaytim:</span><span>${Number(sale.changeAmount).toLocaleString()} so'm</span></div>` : ''}
  <hr/>
  <div class="center muted">Xarid uchun rahmat!</div>
  <script>setTimeout(function(){window.print();},300);<\/script>
</body>
</html>`)
  w.document.close()
}

async function loadShift() {
  try {
    shift.value = await shiftsApi.current()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function openShift() {
  error.value = ''
  shiftBusy.value = true
  try {
    shift.value = await shiftsApi.open({ openingCash: Number(openingCash.value) || 0 })
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    shiftBusy.value = false
  }
}

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const [p, promos, cats, sts] = await Promise.all([
      productsApi.list(),
      promotionsApi.active(),
      categoriesApi.list(),
      storesApi.list()
    ])
    products.value = p
    activePromos.value = promos
    categories.value = cats
    stores.value = sts
    if (!selectedBranchId.value && sts.length === 1) {
      selectedBranchId.value = sts[0].id
    }
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Savat tarkibi yoki summa o'zgardi — avvalgi urinish kaliti endi yaramaydi
// (aks holda server eski savdoning chekini qaytarib yuborardi).
watch([cart, paidAmount], () => { clientSaleId.value = '' }, { deep: true })

async function checkout() {
  error.value = ''
  lastSale.value = null
  if (!shift.value) { error.value = 'Avval smenani oching'; return }
  if (!cart.value.length) { error.value = 'Savatcha bo\'sh'; return }
  if (paymentMethod.value === 'Cash' && Number(paidAmount.value) < Math.round(total.value)) {
    error.value = 'Berilgan summa yetarli emas'
    return
  }
  submitting.value = true
  try {
    const cartSnapshot = cart.value.map((l) => ({ ...l }))
    const branchId = selectedBranchId.value || (stores.value.length >= 1 ? stores.value[0].id : null)
    if (!branchId && stores.value.length === 0) {
      error.value = 'Filial topilmadi. Admin panelida kassirni filialga bog\'lang.'
      submitting.value = false
      return
    }
    if (!clientSaleId.value) clientSaleId.value = newClientSaleId()
    const sale = await salesApi.create({
      clientSaleId: clientSaleId.value,
      paymentMethod: paymentMethod.value,
      paidAmount: Number(paidAmount.value) || total.value,
      branchId: branchId || undefined,
      items: cart.value.map((l) => ({ productId: l.product.id, quantity: Number(l.quantity) }))
    })
    clientSaleId.value = ''
    sale._items = cartSnapshot.map((l) => ({
      name: l.product.name,
      quantity: l.quantity,
      unit: l.product.unit,
      unitPrice: effUnitPrice(l.product),
      totalPrice: lineTotal(l)
    }))
    lastSale.value = sale
    clearCart()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    submitting.value = false
  }
}

// --- Virtual scroll for products grid ---
const productsScrollEl = ref(null)
const vsScrollTop = ref(0)
const vsContainerHeight = ref(600)
const vsContainerWidth = ref(800)

const VS_ITEM_MIN_W = 150
const VS_GAP = 10
const VS_ROW_H = 190  // card height + gap
const VS_OVERSCAN = 2

const vsColumns = computed(() =>
  Math.max(1, Math.floor((vsContainerWidth.value + VS_GAP) / (VS_ITEM_MIN_W + VS_GAP)))
)
const vsTotalRows = computed(() => Math.ceil(filtered.value.length / vsColumns.value))
const vsStartRow = computed(() =>
  Math.max(0, Math.floor(vsScrollTop.value / VS_ROW_H) - VS_OVERSCAN)
)
const vsEndRow = computed(() =>
  Math.min(
    vsTotalRows.value,
    Math.floor((vsScrollTop.value + vsContainerHeight.value) / VS_ROW_H) + VS_OVERSCAN + 1
  )
)
const visibleProducts = computed(() => {
  const start = vsStartRow.value * vsColumns.value
  const end = Math.min(vsEndRow.value * vsColumns.value, filtered.value.length)
  return filtered.value.slice(start, end)
})
const vsPadTop = computed(() => vsStartRow.value * VS_ROW_H)
const vsPadBottom = computed(() =>
  Math.max(0, (vsTotalRows.value - vsEndRow.value) * VS_ROW_H)
)

function onProductsScroll(e) {
  vsScrollTop.value = e.target.scrollTop
}

watch([activeCategory, search], () => {
  vsScrollTop.value = 0
  if (productsScrollEl.value) productsScrollEl.value.scrollTop = 0
})

let _resizeObs = null

onMounted(() => {
  loadData()
  loadShift()
  window.addEventListener('keydown', onKeydown)
  nextTick(() => {
    if (productsScrollEl.value) {
      _resizeObs = new ResizeObserver(entries => {
        for (const entry of entries) {
          vsContainerWidth.value = entry.contentRect.width
          vsContainerHeight.value = entry.contentRect.height
        }
      })
      _resizeObs.observe(productsScrollEl.value)
    }
  })
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  if (_resizeObs) _resizeObs.disconnect()
})
</script>

<template>
  <div class="page-head">
    <h1>Savdo</h1>
  </div>

  <!-- Smena bari -->
  <div class="card" style="margin-bottom: 16px">
    <div v-if="shift" class="row" style="align-items: center; justify-content: space-between">
      <div>
        <span class="pill pill-green">Smena ochiq</span>
        <span class="muted" style="margin-left: 10px">
          Boshlang'ich naqd: {{ Number(shift.openingCash).toLocaleString() }} so'm ·
          ochilgan {{ new Date(shift.openedAt).toLocaleString('uz-UZ') }}
        </span>
      </div>
      <RouterLink :to="{ name: 'shifts' }" class="btn btn-ghost btn-sm">Smenani yopish</RouterLink>
    </div>
    <div v-else class="row" style="align-items: flex-end">
      <div class="field" style="margin: 0">
        <label>Boshlang'ich naqd (smena ochish)</label>
        <input v-model="openingCash" type="number" min="0" class="input" style="width: 200px" />
      </div>
      <button class="btn" :disabled="shiftBusy" @click="openShift">
        {{ shiftBusy ? '...' : 'Smenani ochish' }}
      </button>
      <span class="muted">Savdo qilish uchun smena ochilishi shart.</span>
    </div>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>

  <!-- Bir vaqtda bir nechta savdo tablari -->
  <div class="session-tabs">
    <button
      v-for="(s, i) in sessions"
      :key="s.id"
      class="session-tab"
      :class="{ active: s.id === activeSessionId }"
      @click="activeSessionId = s.id"
    >
      Savdo {{ i + 1 }}
      <template v-if="s.cart.length > 0">
        <span class="session-badge">{{ s.cart.reduce((n, l) => n + Number(l.quantity || 0), 0) }}</span>
      </template>
      <span v-if="sessions.length > 1" class="session-close" @click.stop="removeSession(s.id)">×</span>
    </button>
    <button class="session-tab session-add" title="Yangi savdo" @click="addSession">+</button>
  </div>

  <div class="pos">
    <!-- Chap: mahsulotlar -->
    <div>
      <div style="display:flex; gap:8px; margin-bottom:10px">
        <input
          v-model="search"
          class="input"
          placeholder="Qidirish yoki shtrix-kodni urang (Enter)"
          style="flex:1"
          @keyup.enter="onScan"
        />
        <button
          class="btn btn-ghost"
          :disabled="loading"
          title="Mahsulotlarni yangilash"
          @click="loadData"
        >
          <svg
            :style="loading ? 'animation:spin .8s linear infinite' : ''"
            width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"
          >
            <path d="M1 4v6h6M23 20v-6h-6"/>
            <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4-4.64 4.36A9 9 0 0 1 3.51 15"/>
          </svg>
        </button>
      </div>

      <!-- Kategoriya tablari -->
      <div class="cat-tabs">
        <button
          class="cat-tab"
          :class="{ active: activeCategory === '' }"
          @click="activeCategory = ''"
        >Barchasi</button>
        <button
          v-for="c in categories"
          :key="c.id"
          class="cat-tab"
          :class="{ active: activeCategory === c.id }"
          @click="activeCategory = c.id"
        >{{ c.name }}</button>
      </div>

      <!-- Mahsulotlar grid (virtual scroll) -->
      <div
        class="pos-products-scroll"
        ref="productsScrollEl"
        @scroll.passive="onProductsScroll"
      >
        <!-- Skeleton loading -->
        <div v-if="loading" class="pos-products">
          <div v-for="n in 15" :key="'sk'+n" class="prod-card prod-skeleton">
            <div class="sk-img"></div>
            <div class="sk-line" style="width:70%"></div>
            <div class="sk-line short" style="width:45%; margin-top:4px"></div>
          </div>
        </div>

        <div
          v-else
          class="pos-products"
          :style="{ paddingTop: vsPadTop + 'px', paddingBottom: vsPadBottom + 'px' }"
        >
          <button
            v-for="p in visibleProducts"
            :key="p.id"
            class="prod-card"
            :class="{ 'prod-out': Number(p.quantity) <= 0 }"
            :disabled="!shift"
            @click="addToCart(p)"
          >
            <span v-if="discPct(p) > 0" class="promo-badge">−{{ Number(discPct(p)).toFixed(0) }}%</span>
            <img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" class="prod-img" alt="" loading="lazy" decoding="async" />
            <div v-else class="prod-img-placeholder"></div>
            <div class="name">{{ p.name }}</div>
            <div v-if="discPct(p) > 0">
              <span class="old-price">{{ Number(p.price).toLocaleString() }}</span>
              <span class="price">{{ effUnitPrice(p).toLocaleString() }} so'm</span>
            </div>
            <div v-else class="price">{{ Number(p.price).toLocaleString() }} so'm</div>
            <div
              class="stock-label"
              :class="{
                'stock-ok': Number(p.quantity) > 5,
                'stock-low': Number(p.quantity) > 0 && Number(p.quantity) <= 5,
                'stock-zero': Number(p.quantity) <= 0
              }"
            >
              {{ Number(p.quantity) <= 0
                ? 'Tugagan'
                : `${Number(p.quantity).toLocaleString()} ${p.unit === 'Kilogram' ? 'kg' : 'dona'}` }}
            </div>
          </button>
          <p v-if="!filtered.length" class="muted" style="padding:16px 0">Mahsulot topilmadi</p>
        </div>
      </div>
    </div>

    <!-- O'ng: savatcha + to'lov -->
    <div class="card">
      <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:12px">
        <h3 style="margin:0">
          Savatcha
          <span v-if="cartItemCount > 0" class="cart-count-badge">{{ cartItemCount }}</span>
        </h3>
        <button
          v-if="cart.length"
          class="btn btn-ghost btn-sm"
          style="color:var(--danger); gap:4px; display:flex; align-items:center"
          @click="clearCart"
        >
          <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <polyline points="3 6 5 6 21 6"/>
            <path d="M19 6l-1 14H6L5 6"/>
            <path d="M10 11v6M14 11v6"/>
          </svg>
          Savatni tozalash
        </button>
      </div>

      <div v-if="!cart.length" class="muted" style="padding: 16px 0; text-align:center; font-size:13px">
        Mahsulot tanlang yoki shtrix-kod urang
      </div>

      <div v-for="line in cart" :key="line.product.id" class="cart-line">
        <div class="cl-name">
          {{ line.product.name }}
          <span v-if="discPct(line.product) > 0" class="pill pill-red" style="font-size: 10px">
            −{{ Number(discPct(line.product)).toFixed(0) }}%
          </span>
        </div>
        <div class="qty-ctrl">
          <button class="qty-btn" @click="dec(line)">−</button>
          <input
            v-model="line.quantity"
            type="number"
            min="0"
            :step="line.product.unit === 'Kilogram' ? '0.001' : '1'"
            class="input qty-input"
          />
          <button class="qty-btn" @click="inc(line)">+</button>
        </div>
        <span class="text-right" style="width: 86px; font-size:13px">
          {{ lineTotal(line).toLocaleString() }}
        </span>
        <button class="btn btn-ghost btn-sm" title="O'chirish" @click="removeLine(line.product.id)">✕</button>
      </div>

      <div class="totals">
        <div v-if="cartItemCount > 0" class="t-row" style="color:var(--muted); font-size:13px">
          <span>Tovarlar soni</span>
          <span style="font-weight:600; color:var(--text)">{{ cartItemCount }} ta</span>
        </div>
        <div class="t-row grand">
          <span>Jami</span>
          <span>{{ total.toLocaleString() }} so'm</span>
        </div>
      </div>

      <!-- Ko'p filial: qaysi do'kondan sotilayotganini tanlash -->
      <div v-if="stores.length > 1" class="field" style="margin-top:10px">
        <label>Filial <span style="color:var(--danger)">*</span></label>
        <select v-model="selectedBranchId" class="select">
          <option value="">— Tanlang —</option>
          <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>

      <div class="field" style="margin-top: 14px">
        <label>To'lov turi</label>
        <select v-model="paymentMethod" class="select">
          <option value="Card">💳 Karta</option>
          <option value="Cash">💵 Naqd</option>
          <option value="Transfer">📱 O'tkazma (Payme/Click)</option>
          <option value="Mixed">🔀 Aralash</option>
        </select>
      </div>

      <div class="field">
        <label>Berilgan summa</label>
        <div style="display:flex; gap:6px">
          <input v-model="paidAmount" type="number" min="0" class="input" style="flex:1" />
          <button
            class="btn btn-ghost btn-sm"
            style="white-space:nowrap; flex-shrink:0"
            title="Jami summani kiritish"
            @click="setFullAmount"
          >To'liq summa</button>
        </div>
      </div>

      <div v-if="paymentMethod === 'Cash'" class="t-row" style="font-weight:600; margin-bottom:4px">
        <span>Qaytim</span>
        <span>{{ change.toLocaleString() }} so'm</span>
      </div>

      <button
        class="btn btn-success"
        style="width:100%; margin-top:12px; font-size:15px; padding:12px"
        :disabled="submitting || !cart.length || !shift"
        @click="checkout"
      >
        {{ submitting ? 'Saqlanmoqda...' : "To'lash  (F12)" }}
      </button>

      <!-- Oxirgi chek -->
      <template v-if="lastSale">
        <button class="btn btn-ghost" style="width:100%; margin-top:8px" @click="printReceipt(lastSale)">
          🖨 Chekni ko'rish va chop etish
        </button>
        <div class="last-receipt-info">
          ✓ Chek {{ lastSale.receiptNumber }} · Jami {{ Number(lastSale.totalAmount).toLocaleString() }} · Qaytim {{ Number(lastSale.changeAmount).toLocaleString() }}
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Session tabs */
.session-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.session-tab {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 8px 8px 0 0;
  border: 1px solid var(--border);
  border-bottom: none;
  background: var(--bg);
  color: var(--text);
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}
.session-tab:hover { background: var(--bg-card); }
.session-tab.active {
  background: var(--primary);
  color: #fff;
  border-color: var(--primary);
}
.session-tab.active .session-badge {
  background: #fff;
  color: var(--primary);
}
.session-tab.session-add {
  color: var(--muted);
  font-size: 18px;
  padding: 4px 12px;
}
.session-tab.session-add:hover { color: var(--primary); }

.session-badge {
  background: var(--primary);
  color: #fff;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
  padding: 2px 8px;
  min-width: 22px;
  text-align: center;
}

.session-close {
  margin-left: 2px;
  opacity: 0.5;
  font-size: 14px;
  line-height: 1;
  border-radius: 50%;
  padding: 0 2px;
  transition: opacity 0.15s, background 0.15s;
}
.session-close:hover { opacity: 1; background: rgba(0,0,0,.08); }

/* Category tabs */
.cat-tabs {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-bottom: 12px;
}

.cat-tab {
  padding: 5px 14px;
  border-radius: 999px;
  border: 1px solid var(--border);
  background: transparent;
  color: var(--text);
  font-size: 13px;
  cursor: pointer;
  transition: background 0.15s, color 0.15s, border-color 0.15s;
  white-space: nowrap;
}
.cat-tab:hover { background: var(--bg-card); border-color: var(--primary); color: var(--primary); }
.cat-tab.active {
  background: var(--primary);
  color: #fff;
  border-color: var(--primary);
  font-weight: 600;
}

/* Stock label on product card */
.stock-label {
  font-size: 11px;
  margin-top: 2px;
}
.stock-ok { color: #16a34a; }
.stock-low { color: #d97706; font-weight: 600; }
.stock-zero { color: var(--danger); font-weight: 600; }

.prod-out {
  opacity: 0.5;
}

/* Cart item count badge near "Savatcha" */
.cart-count-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--primary);
  color: #fff;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
  padding: 1px 7px;
  margin-left: 6px;
  vertical-align: middle;
  min-width: 20px;
}

/* Last receipt info line */
.last-receipt-info {
  margin-top: 8px;
  font-size: 12px;
  color: #16a34a;
  text-align: center;
  padding: 4px 0;
}

/* Jami count badge */
.jami-count {
  font-size: 13px;
  font-weight: 600;
  color: #fff;
  background: var(--primary);
  border-radius: 999px;
  padding: 1px 10px;
  line-height: 1.6;
}

/* Skeleton loading */
@keyframes shimmer {
  0% { background-position: -400px 0; }
  100% { background-position: 400px 0; }
}
.prod-skeleton {
  pointer-events: none;
}
.sk-img {
  width: 100%;
  height: 90px;
  border-radius: 8px;
  margin-bottom: 8px;
  background: linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%);
  background-size: 400px 100%;
  animation: shimmer 1.2s infinite linear;
}
.sk-line {
  height: 12px;
  border-radius: 6px;
  background: linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%);
  background-size: 400px 100%;
  animation: shimmer 1.2s infinite linear;
}

/* Rasm yo'q bo'lsa placeholder */
.prod-img-placeholder {
  width: 100%;
  height: 90px;
  border-radius: 8px;
  margin-bottom: 8px;
  background: var(--bg);
  border: 1px solid var(--border);
}

/* Virtual scroll wrapper for products */
.pos-products-scroll {
  height: calc(100vh - 320px);
  min-height: 380px;
  overflow-y: auto;
  overflow-x: hidden;
}
</style>
