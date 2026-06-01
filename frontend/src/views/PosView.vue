<script setup>
import { ref, computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { productsApi, salesApi, shiftsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const products = ref([])
const search = ref('')
const cart = ref([]) // { product, quantity }
const paymentMethod = ref('Cash')
const paidAmount = ref(0)
const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const lastSale = ref(null)

// --- Smena ---
const shift = ref(null)
const openingCash = ref(0)
const shiftBusy = ref(false)

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return products.value
  return products.value.filter(
    (p) =>
      p.name.toLowerCase().includes(q) ||
      (p.barcode && p.barcode.toLowerCase().includes(q)) ||
      p.mxikCode.toLowerCase().includes(q)
  )
})

const total = computed(() =>
  cart.value.reduce((sum, l) => sum + Number(l.product.price) * Number(l.quantity || 0), 0)
)
const change = computed(() => Math.max(0, Number(paidAmount.value || 0) - total.value))

function addToCart(product) {
  const existing = cart.value.find((l) => l.product.id === product.id)
  if (existing) existing.quantity = Number(existing.quantity) + 1
  else cart.value.push({ product, quantity: 1 })
}
function removeLine(id) {
  cart.value = cart.value.filter((l) => l.product.id !== id)
}
function clearCart() {
  cart.value = []
  paidAmount.value = 0
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

async function loadProducts() {
  loading.value = true
  error.value = ''
  try {
    products.value = await productsApi.list()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function checkout() {
  error.value = ''
  lastSale.value = null
  if (!shift.value) {
    error.value = 'Avval smenani oching'
    return
  }
  if (!cart.value.length) {
    error.value = 'Savatcha bo\'sh'
    return
  }
  if (paymentMethod.value === 'Cash' && Number(paidAmount.value) < total.value) {
    error.value = 'Berilgan summa yetarli emas'
    return
  }
  submitting.value = true
  try {
    const sale = await salesApi.create({
      paymentMethod: paymentMethod.value,
      paidAmount: Number(paidAmount.value) || total.value,
      items: cart.value.map((l) => ({ productId: l.product.id, quantity: Number(l.quantity) }))
    })
    lastSale.value = sale
    clearCart()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  loadProducts()
  loadShift()
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
  <div v-if="lastSale" class="alert alert-success">
    Savdo yakunlandi — chek <strong>{{ lastSale.receiptNumber }}</strong>.
    Jami: {{ Number(lastSale.totalAmount).toLocaleString() }} so'm.
    Qaytim: {{ Number(lastSale.changeAmount).toLocaleString() }} so'm.
  </div>

  <div class="pos">
    <div>
      <input v-model="search" class="input" placeholder="Qidirish: nom, shtrix-kod, MXIK..." style="margin-bottom: 12px" />
      <div class="pos-products">
        <button v-for="p in filtered" :key="p.id" class="prod-card" :disabled="!shift" @click="addToCart(p)">
          <div class="name">{{ p.name }}</div>
          <div class="price">{{ Number(p.price).toLocaleString() }} so'm</div>
          <div class="muted" style="font-size: 12px">{{ p.unit === 'Kilogram' ? 'kg' : 'dona' }}</div>
        </button>
        <p v-if="!filtered.length && !loading" class="muted">Mahsulot topilmadi</p>
      </div>
    </div>

    <div class="card">
      <h3 style="margin-top: 0">Savatcha</h3>
      <div v-if="!cart.length" class="muted" style="padding: 16px 0">Mahsulot tanlang</div>

      <div v-for="line in cart" :key="line.product.id" class="cart-line">
        <span class="cl-name">{{ line.product.name }}</span>
        <input
          v-model="line.quantity"
          type="number"
          min="0"
          :step="line.product.unit === 'Kilogram' ? '0.001' : '1'"
          class="input qty-input"
        />
        <span class="text-right" style="width: 90px">
          {{ (Number(line.product.price) * Number(line.quantity || 0)).toLocaleString() }}
        </span>
        <button class="btn btn-ghost btn-sm" @click="removeLine(line.product.id)">✕</button>
      </div>

      <div class="totals">
        <div class="t-row grand">
          <span>Jami</span>
          <span>{{ total.toLocaleString() }} so'm</span>
        </div>
      </div>

      <div class="field" style="margin-top: 14px">
        <label>To'lov turi</label>
        <select v-model="paymentMethod" class="select">
          <option value="Cash">Naqd</option>
          <option value="Card">Karta</option>
          <option value="Mixed">Aralash</option>
        </select>
      </div>

      <div class="field">
        <label>Berilgan summa</label>
        <input v-model="paidAmount" type="number" min="0" class="input" />
      </div>

      <div v-if="paymentMethod === 'Cash'" class="t-row" style="font-weight: 600">
        <span>Qaytim</span>
        <span>{{ change.toLocaleString() }} so'm</span>
      </div>

      <button
        class="btn btn-success"
        style="width: 100%; margin-top: 12px"
        :disabled="submitting || !cart.length || !shift"
        @click="checkout"
      >
        {{ submitting ? 'Saqlanmoqda...' : 'To\'lash' }}
      </button>
      <button v-if="cart.length" class="btn btn-ghost" style="width: 100%; margin-top: 8px" @click="clearCart">
        Tozalash
      </button>
    </div>
  </div>
</template>
