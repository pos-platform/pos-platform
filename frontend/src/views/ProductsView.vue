<script setup>
import { ref, onMounted, reactive } from 'vue'
import { productsApi, categoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const products = ref([])
const categories = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const saving = ref(false)

const blankForm = () => ({
  name: '',
  mxikCode: '',
  barcode: '',
  price: 0,
  unit: 'Piece',
  isWeighed: false,
  pluNumber: null,
  tare: null,
  categoryId: ''
})
const form = reactive(blankForm())

const newCategory = ref('')

async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    const [p, c] = await Promise.all([productsApi.list(), categoriesApi.list()])
    products.value = p
    categories.value = c
    if (!form.categoryId && c.length) form.categoryId = c[0].id
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function categoryName(id) {
  return categories.value.find((c) => c.id === id)?.name || '—'
}

async function addCategory() {
  if (!newCategory.value.trim()) return
  try {
    const created = await categoriesApi.create({ name: newCategory.value.trim() })
    categories.value.push(created)
    form.categoryId = created.id
    newCategory.value = ''
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function submit() {
  error.value = ''
  success.value = ''
  saving.value = true
  try {
    const payload = {
      name: form.name.trim(),
      mxikCode: form.mxikCode.trim(),
      barcode: form.barcode?.trim() || null,
      price: Number(form.price),
      unit: form.unit,
      isWeighed: form.isWeighed,
      pluNumber: form.pluNumber ? Number(form.pluNumber) : null,
      tare: form.tare ? Number(form.tare) : null,
      categoryId: form.categoryId
    }
    await productsApi.create(payload)
    success.value = `"${payload.name}" qo'shildi`
    Object.assign(form, blankForm())
    if (categories.value.length) form.categoryId = categories.value[0].id
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

onMounted(loadAll)
</script>

<template>
  <div class="page-head">
    <h1>Mahsulotlar</h1>
    <span class="muted">{{ products.length }} ta</span>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start">
    <!-- Yangi mahsulot formasi -->
    <form class="card" style="flex: 1; min-width: 320px; max-width: 420px" @submit.prevent="submit">
      <h3 style="margin-top: 0">Yangi mahsulot</h3>

      <div class="field">
        <label>Nomi</label>
        <input v-model="form.name" class="input" required />
      </div>

      <div class="row">
        <div class="field">
          <label>MXIK kodi</label>
          <input v-model="form.mxikCode" class="input" required placeholder="Soliq uchun majburiy" />
        </div>
        <div class="field">
          <label>Shtrix-kod</label>
          <input v-model="form.barcode" class="input" />
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label>Narx</label>
          <input v-model="form.price" type="number" min="0" step="0.01" class="input" required />
        </div>
        <div class="field">
          <label>Birlik</label>
          <select v-model="form.unit" class="select">
            <option value="Piece">Dona</option>
            <option value="Kilogram">Kilogramm</option>
          </select>
        </div>
      </div>

      <div class="field">
        <label>Kategoriya</label>
        <select v-model="form.categoryId" class="select" required>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>

      <div class="checkbox">
        <input id="weighed" v-model="form.isWeighed" type="checkbox" />
        <label for="weighed" style="margin: 0">Tarozida tortiladi</label>
      </div>

      <div v-if="form.isWeighed" class="row" style="margin-top: 8px">
        <div class="field">
          <label>PLU raqami</label>
          <input v-model="form.pluNumber" type="number" min="0" class="input" />
        </div>
        <div class="field">
          <label>Tara (kg)</label>
          <input v-model="form.tare" type="number" min="0" step="0.001" class="input" />
        </div>
      </div>

      <button class="btn" style="width: 100%; margin-top: 8px" :disabled="saving">
        {{ saving ? 'Saqlanmoqda...' : 'Qo\'shish' }}
      </button>

      <hr style="border: none; border-top: 1px solid var(--border); margin: 16px 0" />

      <label class="muted" style="font-weight: 600; font-size: 13px">Yangi kategoriya</label>
      <div class="row" style="margin-top: 6px">
        <input v-model="newCategory" class="input" placeholder="Kategoriya nomi" style="flex: 1" />
        <button type="button" class="btn btn-ghost" @click="addCategory">+</button>
      </div>
    </form>

    <!-- Ro'yxat -->
    <div class="card" style="flex: 2; min-width: 360px">
      <table class="table">
        <thead>
          <tr>
            <th>Nomi</th>
            <th>MXIK</th>
            <th>Kategoriya</th>
            <th class="text-right">Narx</th>
            <th>Birlik</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in products" :key="p.id">
            <td>
              {{ p.name }}
              <span v-if="p.isWeighed" class="pill pill-amber" style="margin-left: 6px">tarozi</span>
            </td>
            <td class="muted">{{ p.mxikCode }}</td>
            <td>{{ categoryName(p.categoryId) }}</td>
            <td class="text-right">{{ Number(p.price).toLocaleString() }}</td>
            <td>{{ p.unit === 'Kilogram' ? 'kg' : 'dona' }}</td>
          </tr>
          <tr v-if="!products.length && !loading">
            <td colspan="5" class="muted" style="text-align: center; padding: 24px">
              Hozircha mahsulot yo'q
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
