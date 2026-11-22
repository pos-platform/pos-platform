<script setup>
import { localDateStr } from '../utils/date'
import { ref, reactive, onMounted } from 'vue'
import { promotionsApi, productsApi, categoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const promotions = ref([])
const products = ref([])
const categories = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

function today() {
  return localDateStr()
}
function plusDays(n) {
  const d = new Date()
  d.setDate(d.getDate() + n)
  return localDateStr(d)
}

const blank = () => ({
  name: '',
  discountPercent: 10,
  scope: 'all', // all | product | category
  productId: '',
  categoryId: '',
  startDate: today(),
  endDate: plusDays(7)
})
const form = reactive(blank())

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('uz-UZ') : '—'
}

function targetLabel(p) {
  if (p.productName) return `Mahsulot: ${p.productName}`
  if (p.categoryName) return `Kategoriya: ${p.categoryName}`
  return 'Barcha mahsulotlar'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [pr, prod, cat] = await Promise.all([
      promotionsApi.list(),
      productsApi.list(),
      categoriesApi.list()
    ])
    promotions.value = pr
    products.value = prod
    categories.value = cat
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function create() {
  error.value = ''
  success.value = ''
  saving.value = true
  try {
    const payload = {
      name: form.name.trim(),
      discountPercent: Number(form.discountPercent),
      productId: form.scope === 'product' ? form.productId || null : null,
      categoryId: form.scope === 'category' ? form.categoryId || null : null,
      startDate: `${form.startDate}T00:00:00Z`,
      endDate: `${form.endDate}T23:59:59Z`
    }
    await promotionsApi.create(payload)
    success.value = `"${payload.name}" aksiyasi qo'shildi`
    Object.assign(form, blank())
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function toggle(p) {
  error.value = ''
  try {
    if (p.isActive) await promotionsApi.deactivate(p.id)
    else await promotionsApi.activate(p.id)
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function remove(p) {
  if (!window.confirm(`"${p.name}" aksiyasini o'chirasizmi?`)) return
  error.value = ''
  try {
    await promotionsApi.remove(p.id)
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Aksiyalar</h1>
    <span class="muted">{{ promotions.length }} ta</span>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row tbl-main" style="align-items: flex-start">
    <!-- Yangi aksiya -->
    <form class="card" style="flex: 1; min-width: 320px; max-width: 380px" @submit.prevent="create">
      <h3 style="margin-top: 0">Yangi aksiya</h3>

      <div class="field">
        <label>Nomi</label>
        <input v-model="form.name" class="input" required placeholder="Masalan: Yozgi chegirma" />
      </div>

      <div class="field">
        <label>Chegirma foizi (%)</label>
        <input v-model="form.discountPercent" type="number" min="0" max="100" step="any" class="input" required />
      </div>

      <div class="field">
        <label>Qamrov</label>
        <select v-model="form.scope" class="select">
          <option value="all">Barcha mahsulotlar</option>
          <option value="category">Kategoriya bo'yicha</option>
          <option value="product">Bitta mahsulot</option>
        </select>
      </div>

      <div v-if="form.scope === 'category'" class="field">
        <label>Kategoriya</label>
        <select v-model="form.categoryId" class="select" required>
          <option value="" disabled>Tanlang...</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>

      <div v-if="form.scope === 'product'" class="field">
        <label>Mahsulot</label>
        <select v-model="form.productId" class="select" required>
          <option value="" disabled>Tanlang...</option>
          <option v-for="p in products" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>

      <div class="row">
        <div class="field">
          <label>Boshlanish</label>
          <input v-model="form.startDate" type="date" class="input" required />
        </div>
        <div class="field">
          <label>Tugash</label>
          <input v-model="form.endDate" type="date" class="input" required />
        </div>
      </div>

      <button class="btn" style="width: 100%" :disabled="saving">
        {{ saving ? 'Saqlanmoqda...' : 'Qo\'shish' }}
      </button>
    </form>

    <!-- Ro'yxat -->
    <div class="card" style="flex: 2; min-width: 380px">
      <table class="table">
        <thead>
          <tr>
            <th>Nomi</th>
            <th>Qamrov</th>
            <th class="text-right">Chegirma</th>
            <th>Muddat</th>
            <th>Holat</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in promotions" :key="p.id">
            <td><strong>{{ p.name }}</strong></td>
            <td class="muted">{{ targetLabel(p) }}</td>
            <td class="text-right"><span class="pill pill-red">−{{ Number(p.discountPercent).toFixed(0) }}%</span></td>
            <td class="muted">{{ fmtDate(p.startDate) }} – {{ fmtDate(p.endDate) }}</td>
            <td>
              <span v-if="p.isCurrentlyActive" class="pill pill-green">Amalda</span>
              <span v-else-if="p.isActive" class="pill pill-amber">Rejada</span>
              <span v-else class="pill pill-gray">O'chiq</span>
            </td>
            <td class="text-right">
              <div class="row-actions">
                <button class="btn btn-ghost btn-sm" @click="toggle(p)">
                  {{ p.isActive ? 'O\'chirish' : 'Yoqish' }}
                </button>
                <button class="btn btn-icon danger" title="O'chirish" @click="remove(p)"><AppIcon name="trash" :size="15" /></button>
              </div>
            </td>
          </tr>
          <tr v-if="!promotions.length && !loading">
            <td colspan="6" class="muted" style="text-align: center; padding: 24px">Aksiya yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
