<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { onlineOrdersApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'
import ReturnsTab from './online/ReturnsTab.vue'

// Online (mobil ilova) buyurtmalar — faqat ko'rish va hisobot. Amallar kassada (desktop) bajariladi.

const storeFilter = useStoreFilterStore()

const orders = ref([])
const loading = ref(false)
const err = ref('')
const selected = ref(null)
const detailLoading = ref(false)

function toDateStr(d) {
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}
function addDays(dateStr, days) {
  const d = new Date(`${dateStr}T00:00:00`)
  d.setDate(d.getDate() + days)
  return toDateStr(d)
}

const dateFrom = ref(addDays(toDateStr(new Date()), -6))
const dateTo = ref(toDateStr(new Date()))
const status = ref('')
// Bosqich 10.1: faqat oldindan buyurtmalar (yopiq do'konga tushganlar)
const preorderOnly = ref(false)

const STATUS = {
  New: { text: 'Yangi', cls: 'st-new' },
  Accepted: { text: 'Qabul qilindi', cls: 'st-work' },
  Picking: { text: 'Yig\'ilmoqda', cls: 'st-work' },
  Ready: { text: 'Tayyor', cls: 'st-ready' },
  AwaitingTransfer: { text: 'O\'tkazma kutilmoqda', cls: 'st-ready' },
  ReadyForPickup: { text: 'Olib ketishga tayyor', cls: 'st-ready' },
  OnTheWay: { text: 'Yo\'lda', cls: 'st-ready' },
  Delivered: { text: 'Yetkazildi', cls: 'st-done' },
  Rejected: { text: 'Rad etildi', cls: 'st-bad' },
  Cancelled: { text: 'Bekor qilindi', cls: 'st-bad' }
}
const PAY = { Cash: 'Naqd', Card: 'Terminal', Transfer: 'O\'tkazma' }
const PAY_STATUS = { Paid: 'to\'langan', Pending: 'kutilmoqda', Unpaid: 'to\'lanmagan', Refunded: 'qaytarilgan' }
const POLICY = { ReplaceSimilar: 'O\'xshashiga almashtirish', Remove: 'Olib tashlash', CallMe: 'Mijozga qo\'ng\'iroq' }

// Bosqich 9.3: mijoz bahosi teglari
const TAGS = {
  fast: 'Tez yetkazildi', fresh: 'Yangi mahsulotlar', polite: 'Xushmuomala', accurate: 'Aniq yig\'ildi',
  late: 'Kechikdi', damaged: 'Shikastlangan', missing: 'Yetishmadi', wrong_item: 'Noto\'g\'ri mahsulot'
}
const NEGATIVE_TAGS = new Set(['late', 'damaged', 'missing', 'wrong_item'])
const tagText = (t) => TAGS[t] || t
const stars = (n) => '★'.repeat(n || 0) + '☆'.repeat(5 - (n || 0))

const statusOf = (s) => STATUS[s] || { text: s, cls: 'st-work' }

// Bosqich 10.1: "🌙 Oldindan · 09:00" (UTC → lokal vaqt; bugun bo'lmasa sana ham)
function preorderText(o) {
  if (!o?.isPreorder) return ''
  if (!o.scheduledFor) return '🌙 Oldindan'
  const d = new Date(o.scheduledFor)
  const time = d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  const sameDay = d.toDateString() === new Date().toDateString()
  const date = sameDay ? '' : `${d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' })} `
  return `🌙 Oldindan · ${date}${time}`
}

// Bosqich 10.6: kuryerga taklif holati
const OFFER = {
  Pending: { text: '⏳ Kuryer kutilmoqda', cls: 'st-work' },
  Accepted: { text: '✅ Qabul qildi', cls: 'st-done' },
  NoCourier: { text: '⚠ Kuryer topilmadi', cls: 'st-bad' }
}
const OFFER_ITEM = { Pending: 'Kutilmoqda', Accepted: 'Qabul qildi', Declined: 'Rad etdi', Expired: "Vaqti o'tdi", Cancelled: 'Bekor (boshqasi oldi)' }
const offerOf = (o) => OFFER[o.courierOfferStatus] || null
const fmtTime = (v) => (v ? new Date(v).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }) : '')
function offerSummary(o) {
  const offers = o.courierOffers || []
  if (o.courierOfferStatus !== 'Pending' || !offers.length) return ''
  const declined = offers.filter((x) => x.status === 'Declined').length
  return `${declined}/${offers.length} rad`
}

// Bosqich 10.5: eshik oldida rad etilgan tovarlar sabablari
const REJECT_REASON = { Damaged: 'Shikastlangan', WrongItem: "Noto'g'ri tovar", Quality: 'Sifatsiz', Expired: "Muddati o'tgan", Missing: 'Yetishmadi', Other: 'Boshqa' }

const canReoffer = computed(() => {
  const o = selected.value
  return !!o && o.fulfillmentType === 'Delivery' && !o.courierUserId && o.courierOfferStatus !== 'Accepted' &&
    ['Accepted', 'Picking', 'Ready', 'AwaitingTransfer'].includes(o.status)
})
const reoffering = ref(false)
async function reoffer() {
  const o = selected.value
  if (!o) return
  reoffering.value = true
  err.value = ''
  try {
    const r = await onlineOrdersApi.offerCouriers(o.id)
    if (r && typeof r === 'object' && r.id === o.id) selected.value = r
    else await open(o)
    load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    reoffering.value = false
  }
}
const typeText = (o) => (o.fulfillmentType === 'Delivery' ? 'Yetkazish' : 'Olib ketish')
const payText = (o) => `${PAY[o.paymentMethod] || o.paymentMethod} · ${PAY_STATUS[o.paymentStatus] || o.paymentStatus}`

function fmtNum(n) {
  return Math.round(Number(n) || 0).toLocaleString('ru-RU')
}
function fmtDateTime(v) {
  if (!v) return '—'
  const d = new Date(v)
  return d.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function fmtQty(n) {
  return Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}

/** "+998901234567" → "+998 90 *** ** 67" (jadvalda; to'liq raqam faqat tafsilotda). */
function maskPhone(p) {
  if (!p) return ''
  const d = String(p).replace(/\D/g, '')
  if (d.length === 12 && d.startsWith('998')) return `+998 ${d.slice(3, 5)} *** ** ${d.slice(10)}`
  if (d.length <= 2) return '*'.repeat(d.length)
  return '*'.repeat(d.length - 2) + d.slice(-2)
}

function addressText(o) {
  if (o.fulfillmentType !== 'Delivery') return 'Olib ketish (do\'kondan)'
  const parts = []
  if (o.addressLine) parts.push(o.addressLine)
  if (o.entrance) parts.push(`${o.entrance}-podyezd`)
  if (o.floor) parts.push(`${o.floor}-qavat`)
  if (o.apartment) parts.push(`${o.apartment}-xonadon`)
  if (o.intercom) parts.push(`domofon ${o.intercom}`)
  if (o.landmark) parts.push(`mo'ljal: ${o.landmark}`)
  return parts.join(', ') || '—'
}

async function load() {
  if (!dateFrom.value || !dateTo.value) return
  loading.value = true
  err.value = ''
  try {
    // Backend 'to' kunini o'zi qamrab oladi (shu kun ham kiradi) — tugash sanasi o'zgarishsiz yuboriladi.
    const params = { active: false, from: dateFrom.value, to: dateTo.value }
    if (status.value) params.status = status.value
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    const list = await onlineOrdersApi.list(params)
    orders.value = [...list].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    if (selected.value && !orders.value.some((o) => o.id === selected.value.id)) selected.value = null
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// "Baholar" tabi
const tab = ref('orders')
const ratings = ref(null)
const ratingsLoading = ref(false)

async function loadRatings() {
  if (!dateFrom.value || !dateTo.value) return
  ratingsLoading.value = true
  err.value = ''
  try {
    const params = { from: dateFrom.value, to: dateTo.value }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    ratings.value = await onlineOrdersApi.ratings(params)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    ratingsLoading.value = false
  }
}

const returnsTab = ref(null)
function reload() {
  if (tab.value === 'ratings') loadRatings()
  else if (tab.value === 'returns') returnsTab.value?.load()
  else load()
}

const shownOrders = computed(() => (preorderOnly.value ? orders.value.filter((o) => o.isPreorder) : orders.value))

// Taqsimot: { "1": n, ..., "5": n } (yoki massiv) → 5..1 qatorlar
const distRows = computed(() => {
  const d = ratings.value?.distribution || {}
  const get = (k) => Number(Array.isArray(d) ? d[k - 1] : d[k] ?? d[String(k)]) || 0
  const total = [1, 2, 3, 4, 5].reduce((a, k) => a + get(k), 0)
  return [5, 4, 3, 2, 1].map((k) => ({ k, n: get(k), pct: total ? Math.round((get(k) / total) * 100) : 0 }))
})
const tagRows = computed(() =>
  Object.entries(ratings.value?.tagCounts || {})
    .map(([t, n]) => ({ t, n: Number(n) || 0 }))
    .sort((a, b) => b.n - a.n)
)

async function open(o) {
  selected.value = o
  detailLoading.value = true
  try {
    const full = await onlineOrdersApi.get(o.id)
    if (selected.value?.id === o.id) selected.value = full
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

const summary = computed(() => {
  let deliveredSum = 0
  let deliveredCount = 0
  let cancelled = 0
  for (const o of shownOrders.value) {
    if (o.status === 'Delivered') {
      deliveredSum += Number(o.totalAmount) || 0
      deliveredCount++
    } else if (o.status === 'Cancelled' || o.status === 'Rejected') {
      cancelled++
    }
  }
  return { count: shownOrders.value.length, deliveredSum, deliveredCount, cancelled }
})

// Tafsilot: qator holati (yig'ilgan / olib tashlangan / almashtirilgan)
const itemRows = computed(() => {
  const items = selected.value?.items || []
  const byId = Object.fromEntries(items.map((i) => [i.id, i]))
  return items.map((i) => {
    const notes = []
    if (i.isRemoved) notes.push({ text: 'olib tashlandi', cls: 'n-bad' })
    else if (i.pickedQuantity != null && Number(i.pickedQuantity) !== Number(i.quantity))
      notes.push({ text: `olindi ${fmtQty(i.pickedQuantity)} (so'ralgan ${fmtQty(i.quantity)})`, cls: 'n-warn' })
    if (i.replacesItemId) {
      const old = byId[i.replacesItemId]
      notes.push({ text: `almashtirish: ${old ? old.productName : 'boshqa mahsulot'} o'rniga`, cls: 'n-info' })
    }
    // Markirovka: skanerlangan kodlar soni
    if (i.isMarked) {
      const n = i.markingCodes?.length || 0
      notes.push({ text: n ? `markirovka: ${n} ta kod` : 'markirovkali (kod yo\'q)', cls: n ? 'n-info' : 'n-warn' })
    }
    const qty = i.isRemoved ? 0 : (i.pickedQuantity ?? i.quantity)
    return { ...i, notes, qty }
  })
})

// Kuryer naqd olib yetkazgan, lekin kassaga hali topshirmagan
const courierCashPending = computed(() => {
  const o = selected.value
  return !!o && 'courierCashHandedOverAt' in o && o.status === 'Delivered' && o.paymentMethod === 'Cash' && o.fulfillmentType === 'Delivery' &&
    !!o.courierName && !o.courierCashHandedOverAt
})

const timeline = computed(() => {
  const o = selected.value
  if (!o) return []
  return [
    ['Yaratildi', o.createdAt],
    ['Qabul qilindi', o.acceptedAt],
    ['Yig\'ish boshlandi', o.pickingStartedAt],
    ['Tayyor', o.readyAt],
    ['To\'lov tasdiqlandi', o.paymentConfirmedAt],
    ['Kuryer qabul qildi', o.courierAcceptedAt],
    ['Kuryerga berildi', o.dispatchedAt],
    ['Yetkazildi / topshirildi', o.deliveredAt],
    ['Bekor / rad', o.cancelledAt]
  ].filter((t) => t[1])
})

watch([dateFrom, dateTo, status], () => reload())
watch(() => storeFilter.selectedBranchId, () => reload())
watch(tab, (v) => { if (v !== 'returns') reload() })

onMounted(async () => {
  await storeFilter.ensureLoaded()
  load()
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Online buyurtmalar</h1>
      <div class="muted small">Mobil ilovadan kelgan buyurtmalar. Buyurtma bilan ishlash — kassadagi "Online" oynasida.</div>
    </div>
    <button class="btn btn-ghost btn-sm" :disabled="loading || ratingsLoading" @click="reload">↻ Yangilash</button>
  </div>

  <div class="row" style="gap:8px; margin-bottom:12px">
    <button class="btn btn-ghost btn-sm" :class="{ 'btn-active': tab === 'orders' }" @click="tab = 'orders'">Buyurtmalar</button>
    <button class="btn btn-ghost btn-sm" :class="{ 'btn-active': tab === 'ratings' }" @click="tab = 'ratings'">Baholar</button>
    <button class="btn btn-ghost btn-sm" :class="{ 'btn-active': tab === 'returns' }" @click="tab = 'returns'">Qaytarishlar</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <ReturnsTab v-if="tab === 'returns'" ref="returnsTab" />

  <template v-else>
  <div class="card toolbar">
    <div class="field-inline">
      <span class="muted small">Sana</span>
      <input v-model="dateFrom" type="date" class="input date-in" :max="dateTo" />
      <span class="muted">—</span>
      <input v-model="dateTo" type="date" class="input date-in" :min="dateFrom" />
    </div>
    <select v-if="tab === 'orders'" v-model="status" class="select" style="width:200px">
      <option value="">Barcha holatlar</option>
      <option v-for="(v, k) in STATUS" :key="k" :value="k">{{ v.text }}</option>
    </select>
    <label v-if="tab === 'orders'" class="field-inline small" style="cursor:pointer">
      <input v-model="preorderOnly" type="checkbox" /> 🌙 Oldindan buyurtmalar
    </label>
    <span v-if="storeFilter.selectedStore" class="muted small">Do'kon: {{ storeFilter.selectedStore.name }}</span>
  </div>

  <!-- Baholar (9.3) -->
  <template v-if="tab === 'ratings'">
    <div v-if="ratingsLoading && !ratings" class="muted">Yuklanmoqda...</div>
    <template v-if="ratings">
      <div class="summary">
        <div class="card stat">
          <div class="stat-label">O'rtacha baho</div>
          <div class="stat-value">{{ ratings.count ? Number(ratings.average || 0).toFixed(2) : '—' }} <span class="star">★</span></div>
          <div class="muted small">{{ ratings.count || 0 }} ta baho</div>
        </div>
        <div class="card stat">
          <div class="stat-label">Taqsimot</div>
          <div v-for="r in distRows" :key="r.k" class="dist">
            <span class="dist-k">{{ r.k }} ★</span>
            <span class="dist-bar"><span :style="{ width: r.pct + '%' }"></span></span>
            <span class="dist-n muted">{{ r.n }}</span>
          </div>
        </div>
        <div class="card stat">
          <div class="stat-label">Teglar</div>
          <div class="tags">
            <span v-for="t in tagRows" :key="t.t" class="tag" :class="{ neg: NEGATIVE_TAGS.has(t.t) }">{{ tagText(t.t) }} · {{ t.n }}</span>
            <span v-if="!tagRows.length" class="muted small">Teg yo'q</span>
          </div>
        </div>
      </div>

      <div class="card" style="overflow:auto">
        <table class="table">
          <thead>
            <tr><th>#</th><th>Sana</th><th>Mijoz</th><th>Baho</th><th>Teglar</th><th>Izoh</th></tr>
          </thead>
          <tbody>
            <tr v-if="!(ratings.items || []).length"><td colspan="6" class="muted" style="text-align:center; padding:24px">Baho yo'q</td></tr>
            <tr v-for="r in ratings.items || []" :key="r.orderId">
              <td style="font-weight:700">{{ r.number }}</td>
              <td class="nowrap">{{ fmtDateTime(r.ratedAt) }}</td>
              <td>{{ r.customerName }}</td>
              <td class="nowrap star" :class="{ low: r.rating <= 3 }">{{ stars(r.rating) }}</td>
              <td>
                <span v-for="t in r.tags || []" :key="t" class="tag" :class="{ neg: NEGATIVE_TAGS.has(t) }">{{ tagText(t) }}</span>
              </td>
              <td class="small">{{ r.comment || '' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </template>

  <template v-else>
  <div class="summary">
    <div class="card stat">
      <div class="stat-label">Buyurtmalar</div>
      <div class="stat-value">{{ summary.count }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Yetkazilgan summa</div>
      <div class="stat-value">{{ fmtNum(summary.deliveredSum) }}</div>
      <div class="muted small">{{ summary.deliveredCount }} ta buyurtma</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Bekor / rad etilgan</div>
      <div class="stat-value" style="color: var(--danger)">{{ summary.cancelled }}</div>
    </div>
  </div>

  <div class="layout">
    <div class="card" style="overflow:auto">
      <table class="table">
        <thead>
          <tr>
            <th>#</th>
            <th>Sana</th>
            <th>Mijoz</th>
            <th>Telefon</th>
            <th>Turi</th>
            <th>To'lov</th>
            <th class="text-right">Summa</th>
            <th>Holat</th>
            <th>Kuryer</th>
            <th>Baho</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="10" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
          <tr v-else-if="!shownOrders.length"><td colspan="10" class="muted" style="text-align:center; padding:24px">Buyurtma yo'q</td></tr>
          <tr v-for="o in shownOrders" :key="o.id" class="clickable" :class="{ sel: selected?.id === o.id }" @click="open(o)">
            <td style="font-weight:700">{{ o.number }}</td>
            <td class="nowrap">
              {{ fmtDateTime(o.createdAt) }}
              <div v-if="o.isPreorder" class="pre-badge" :title="o.scheduledFor ? fmtDateTime(o.scheduledFor) : ''">{{ preorderText(o) }}</div>
            </td>
            <td>{{ o.customerName }}</td>
            <td class="muted nowrap">{{ maskPhone(o.customerPhone) }}</td>
            <td>{{ typeText(o) }}</td>
            <td class="small">{{ payText(o) }}</td>
            <td class="text-right" style="font-weight:600">{{ fmtNum(o.totalAmount) }}</td>
            <td><span class="st" :class="statusOf(o.status).cls">{{ statusOf(o.status).text }}</span></td>
            <td class="small">
              <div v-if="o.courierName">{{ o.courierName }}<span v-if="o.courierAcceptedAt" class="muted"> · {{ fmtTime(o.courierAcceptedAt) }}</span></div>
              <span v-else-if="offerOf(o)" class="st" :class="offerOf(o).cls">{{ offerOf(o).text }}</span>
              <span v-else class="muted">—</span>
            </td>
            <td class="nowrap"><span v-if="o.rating" class="star" :class="{ low: o.rating <= 3 }" :title="o.ratingComment || ''">★ {{ o.rating }}</span></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="selected" class="card detail">
      <div class="detail-head">
        <div>
          <span class="st" :class="statusOf(selected.status).cls">{{ statusOf(selected.status).text }}</span>
          <h3 style="margin:6px 0 0">Buyurtma #{{ selected.number }}</h3>
          <p class="muted small" style="margin:2px 0 0">{{ selected.branchName || '' }}</p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <div class="info">
        <div><b>{{ selected.customerName }}</b> · {{ selected.customerPhone }}</div>
        <div>{{ typeText(selected) }}: {{ addressText(selected) }}<span v-if="selected.contactless"> · kontaktsiz</span></div>
        <div v-if="selected.isPreorder"><span class="pre-badge">{{ preorderText(selected) }}</span> <span class="muted small">do'kon yopiq paytda tushgan</span></div>
        <div v-if="selected.scheduledFor">Vaqt: {{ fmtDateTime(selected.scheduledFor) }} ga</div>
        <div v-if="selected.comment" class="comment">Izoh: {{ selected.comment }}</div>
        <div v-if="selected.outOfStockPolicy" class="muted small">Mahsulot bo'lmasa: {{ POLICY[selected.outOfStockPolicy] || selected.outOfStockPolicy }}</div>
      </div>

      <div v-if="selected.courierName || selected.courierPhone || selected.courierUserId" class="info">
        <div>
          Kuryer: <b>{{ selected.courierName || '—' }}</b> <span v-if="selected.courierPhone">· {{ selected.courierPhone }}</span>
          <span v-if="selected.courierUserId" class="muted small"> · ilova orqali</span>
        </div>
        <!-- Kuryer naqdi: olingan summa va kassaga topshirilganligi -->
        <div v-if="selected.courierCashCollected != null || courierCashPending" class="small">
          <template v-if="selected.courierCashCollected != null">Naqd olindi: <b>{{ fmtNum(selected.courierCashCollected) }}</b> so'm · </template>
          <span v-if="selected.courierCashHandedOverAt" style="color:#166534">kassaga topshirildi {{ fmtDateTime(selected.courierCashHandedOverAt) }}</span>
          <span v-else style="color:#b45309">kassaga hali topshirilmagan</span>
        </div>
      </div>

      <!-- Bosqich 10.6: kuryerga taklif -->
      <div v-if="selected.fulfillmentType === 'Delivery' && (offerOf(selected) || (selected.courierOffers || []).length || canReoffer)" class="info">
        <div v-if="offerOf(selected)">
          <span class="st" :class="offerOf(selected).cls">{{ offerOf(selected).text }}</span>
          <span v-if="selected.courierOfferStatus === 'Accepted' && selected.courierName"> {{ selected.courierName }} {{ fmtTime(selected.courierAcceptedAt) }}</span>
          <span v-if="offerSummary(selected)" class="muted small"> ({{ offerSummary(selected) }})</span>
        </div>
        <div v-for="(of, idx) in selected.courierOffers || []" :key="idx" class="tl small">
          <span>{{ of.courierName }}</span>
          <span class="muted">{{ OFFER_ITEM[of.status] || of.status }}<template v-if="of.respondedAt"> · {{ fmtDateTime(of.respondedAt) }}</template></span>
        </div>
        <div v-if="canReoffer">
          <button class="btn btn-ghost btn-sm" :disabled="reoffering" @click="reoffer">{{ reoffering ? '...' : 'Kuryerlarga qayta yuborish' }}</button>
        </div>
      </div>

      <div v-if="selected.rating" class="info" :class="{ 'info-bad': selected.rating <= 3 }">
        <div><span class="star">{{ stars(selected.rating) }}</span> <span class="muted small">{{ fmtDateTime(selected.ratedAt) }}</span></div>
        <div v-if="(selected.ratingTags || []).length">
          <span v-for="t in selected.ratingTags" :key="t" class="tag" :class="{ neg: NEGATIVE_TAGS.has(t) }">{{ tagText(t) }}</span>
        </div>
        <div v-if="selected.ratingComment">“{{ selected.ratingComment }}”</div>
      </div>

      <div v-if="selected.cancelReason" class="info info-bad">Sabab: {{ selected.cancelReason }}</div>

      <div>
        <div class="sec-title">Mahsulotlar <span v-if="detailLoading" class="muted small">· yuklanmoqda...</span></div>
        <div v-for="i in itemRows" :key="i.id" class="item" :class="{ removed: i.isRemoved }">
          <div class="item-main">
            <div class="item-name">{{ i.productName }}</div>
            <div class="muted small">{{ i.barcode || '—' }} · {{ fmtNum(i.unitPrice) }} × {{ fmtQty(i.qty) }}</div>
            <div v-for="(n, idx) in i.notes" :key="idx" class="note" :class="n.cls">{{ n.text }}</div>
          </div>
          <div class="item-sum">{{ fmtNum(i.isRemoved ? 0 : i.lineTotal) }}</div>
        </div>
      </div>

      <!-- Bosqich 10.5: eshik oldida rad etilgan tovarlar -->
      <div v-if="(selected.rejectedItems || []).length" class="info info-bad">
        <div class="sec-title" style="margin:0">Eshik oldida rad etilgan</div>
        <div v-for="(r, idx) in selected.rejectedItems" :key="r.itemId || idx" class="tl small">
          <span>{{ r.productName }} × {{ fmtQty(r.quantity) }}</span>
          <span>{{ REJECT_REASON[r.reason] || r.reason }}</span>
        </div>
        <div class="small">
          <span v-if="selected.rejectedReturnedAt" style="color:#166534">Do'konga qaytarildi: {{ fmtDateTime(selected.rejectedReturnedAt) }}</span>
          <span v-else style="color:#b45309">Kuryer hali do'konga qaytarmagan</span>
        </div>
      </div>

      <div class="totals">
        <div><span>Mahsulotlar</span><span>{{ fmtNum(selected.itemsTotal) }}</span></div>
        <div v-if="Number(selected.promoDiscount) > 0" class="disc">
          <span>Promokod<span v-if="selected.promoCode" class="code"> {{ selected.promoCode }}</span></span>
          <span>−{{ fmtNum(selected.promoDiscount) }}</span>
        </div>
        <div v-else-if="selected.promoCode" class="muted small"><span>Promokod</span><span class="code">{{ selected.promoCode }}</span></div>
        <div v-if="Number(selected.bonusUsed) > 0" class="disc"><span>Bonus ballar</span><span>−{{ fmtNum(selected.bonusUsed) }}</span></div>
        <div v-if="Number(selected.deliveryFee) > 0"><span>Yetkazish</span><span>{{ fmtNum(selected.deliveryFee) }}</span></div>
        <div class="grand"><span>Jami</span><span>{{ fmtNum(selected.totalAmount) }} so'm</span></div>
        <div class="muted small"><span>To'lov</span><span>{{ payText(selected) }}</span></div>
        <div v-if="selected.receiptNumber" class="muted small"><span>Chek raqami</span><span>{{ selected.receiptNumber }}</span></div>
      </div>

      <div>
        <div class="sec-title">Vaqtlar</div>
        <div v-for="t in timeline" :key="t[0]" class="tl"><span class="muted">{{ t[0] }}</span><span>{{ fmtDateTime(t[1]) }}</span></div>
      </div>
    </div>
  </div>
  </template>
  </template>
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.field-inline { display: flex; align-items: center; gap: 6px; }
.date-in { width: 150px; }
.summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin-bottom: 12px; }
.stat-label { font-size: 12px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 22px; font-weight: 700; margin-top: 4px; }
.layout { display: grid; grid-template-columns: minmax(0, 1fr) 400px; gap: 14px; align-items: start; }
.clickable { cursor: pointer; }
.clickable.sel { background: #f1f5f9; }
.st { display: inline-block; font-size: 12px; font-weight: 700; padding: 2px 8px; border-radius: 999px; white-space: nowrap; }
.st-new { background: #ffedd5; color: #9a3412; }
.st-work { background: #fef3c7; color: #92400e; }
.st-ready { background: #dbeafe; color: #1e40af; }
.st-done { background: #dcfce7; color: #166534; }
.st-bad { background: #fee2e2; color: #991b1b; }
.detail { display: flex; flex-direction: column; gap: 10px; }
.detail-head { display: flex; justify-content: space-between; align-items: flex-start; }
.info { display: flex; flex-direction: column; gap: 3px; background: #f8fafc; border-radius: 8px; padding: 8px 10px; font-size: 14px; }
.info-bad { background: #fef2f2; color: #991b1b; }
.comment { color: #9a3412; }
.sec-title { font-weight: 700; font-size: 13px; margin-bottom: 6px; }
.item { display: flex; justify-content: space-between; gap: 8px; padding: 6px 0; border-bottom: 1px solid var(--border); }
.item.removed .item-name { text-decoration: line-through; color: var(--muted); }
.item-name { font-weight: 600; font-size: 14px; }
.item-sum { font-weight: 600; white-space: nowrap; }
.note { font-size: 12px; font-weight: 600; margin-top: 2px; }
.n-bad { color: #b91c1c; }
.n-warn { color: #b45309; }
.n-info { color: #1d4ed8; }
.totals > div, .tl { display: flex; justify-content: space-between; gap: 8px; font-size: 14px; padding: 2px 0; }
.totals .grand { font-weight: 700; font-size: 16px; }
.totals .disc { color: #166534; }
.code { font-family: ui-monospace, Consolas, monospace; }
.btn-active { background: var(--primary); color: #fff; }
.star { color: #f59e0b; letter-spacing: 1px; }
.star.low { color: #dc2626; }
.dist { display: flex; align-items: center; gap: 8px; font-size: 12px; margin-top: 4px; }
.dist-k { width: 30px; font-weight: 600; }
.dist-bar { flex: 1; height: 8px; background: #f1f5f9; border-radius: 999px; overflow: hidden; }
.dist-bar span { display: block; height: 100%; background: #f59e0b; }
.dist-n { width: 32px; text-align: right; }
.tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 6px; }
.tag { display: inline-block; font-size: 12px; font-weight: 600; padding: 2px 8px; border-radius: 999px; background: #dcfce7; color: #166534; margin: 1px 4px 1px 0; }
.tag.neg { background: #fee2e2; color: #991b1b; }
.pre-badge { display: inline-block; font-size: 11.5px; font-weight: 700; padding: 1px 8px; border-radius: 999px; background: #ede9fe; color: #5b21b6; margin-top: 2px; white-space: nowrap; }
@media (max-width: 1000px) {
  .layout { grid-template-columns: 1fr; }
  .summary { grid-template-columns: 1fr; }
}
</style>
