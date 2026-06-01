<script setup>
import { ref, onMounted, reactive, computed } from 'vue'
import { productsApi, categoriesApi } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

const products = ref([])
const categories = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const saving = ref(false)
const uploading = ref(false)
const filterCategory = ref('')

const blankForm = () => ({
  name: '',
  mxikCode: '',
  barcode: '',
  costPrice: 0,
  markupPercent: 0,
  price: 0,
  quantity: 0,
  imageUrl: '',
  unit: 'Piece',
  isWeighed: false,
  pluNumber: null,
  tare: null,
  categoryId: ''
})
const form = reactive(blankForm())

const newCategory = ref('')

const visibleProducts = computed(() =>
  filterCategory.value
    ? products.value.filter((p) => p.categoryId === filterCategory.value)
    : products.value
)

function round2(n) {
  return Math.round((Number(n) || 0) * 100) / 100
}

// Tannarx yoki ustama o'zgarsa -> sotish narxini hisoblaymiz.
function recalcPrice() {
  const cost = Number(form.costPrice) || 0
  const markup = Number(form.markupPercent) || 0
  form.price = round2(cost * (1 + markup / 100))
}
// Sotish narxi qo'lda o'zgarsa -> ustama foizni hisoblaymiz.
function recalcMarkup() {
  const cost = Number(form.costPrice) || 0
  const price = Number(form.price) || 0
  form.markupPercent = cost > 0 ? round2(((price - cost) / cost) * 100) : 0
}

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

async function onImageChange(event) {
  const file = event.target.files?.[0]
  if (!file) return
  error.value = ''
  uploading.value = true
  try {
    const res = await productsApi.uploadImage(file)
    form.imageUrl = res.url
  } catch (e) {
    error.value = errorMessage(e, 'Rasm yuklanmadi')
  } finally {
    uploading.value = false
  }
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
      costPrice: Number(form.costPrice) || 0,
      price: Number(form.price) || 0,
      quantity: Number(form.quantity) || 0,
      imageUrl: form.imageUrl || null,
      unit: form.unit,
      isWeighed: form.isWeighed,
      pluNumber: form.pluNumber ? Number(form.pluNumber) : null,
      tare: form.tare ? Number(form.tare) : null,
      categoryId: form.categoryId
    }
    await productsApi.create(payload)
    success.value = `"${payload.name}" qo'shildi`
    const keepCat = form.categoryId
    Object.assign(form, blankForm())
    form.categoryId = keepCat
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
    <span class="muted">{{ visibleProducts.length }} / {{ products.length }} ta</span>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start">
    <!-- Yangi mahsulot formasi -->
    <form class="card" style="flex: 1; min-width: 340px; max-width: 440px" @submit.prevent="submit">
      <h3 style="margin-top: 0">Yangi mahsulot</h3>

      <!-- Rasm -->
      <div class="field">
        <label>Rasm</label>
        <div class="img-upload">
          <div class="img-preview">
            <img v-if="form.imageUrl" :src="mediaUrl(form.imageUrl)" alt="" />
            <span v-else class="muted">rasm yo'q</span>
          </div>
          <div>
            <input type="file" accept="image/*" @change="onImageChange" />
            <div v-if="uploading" class="muted" style="font-size: 12px">Yuklanmoqda...</div>
          </div>
        </div>
      </div>

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

      <!-- Narxlar: tannarx -> ustama % -> sotish narxi -->
      <div class="row">
        <div class="field">
          <label>Olingan narx (tannarx)</label>
          <input v-model="form.costPrice" type="number" min="0" step="0.01" class="input" @input="recalcPrice" />
        </div>
        <div class="field">
          <label>Ustama %</label>
          <input v-model="form.markupPercent" type="number" step="0.1" class="input" @input="recalcPrice" />
        </div>
      </div>
      <div class="row">
        <div class="field">
          <label>Sotish narxi</label>
          <input v-model="form.price" type="number" min="0" step="0.01" class="input" required @input="recalcMarkup" />
          <span class="muted" style="font-size: 12px">
            Foyda: {{ (Number(form.price) - Number(form.costPrice)).toLocaleString() }} so'm
          </span>
        </div>
        <div class="field">
          <label>Boshlang'ich qoldiq</label>
          <input v-model="form.quantity" type="number" min="0" step="0.001" class="input" />
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label>Birlik</label>
          <select v-model="form.unit" class="select">
            <option value="Piece">Dona</option>
            <option value="Kilogram">Kilogramm</option>
          </select>
        </div>
        <div class="field">
          <label>Kategoriya</label>
          <select v-model="form.categoryId" class="select" required>
            <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
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

      <button class="btn" style="width: 100%; margin-top: 8px" :disabled="saving || uploading">
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
    <div class="card" style="flex: 2; min-width: 380px">
      <div class="field" style="max-width: 260px; margin-bottom: 12px">
        <label>Kategoriya bo'yicha filtr</label>
        <select v-model="filterCategory" class="select">
          <option value="">Barchasi</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>

      <table class="table">
        <thead>
          <tr>
            <th></th>
            <th>Nomi</th>
            <th>Kategoriya</th>
            <th class="text-right">Tannarx</th>
            <th class="text-right">Sotish</th>
            <th class="text-right">Ustama</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in visibleProducts" :key="p.id">
            <td>
              <div class="thumb">
                <img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" />
              </div>
            </td>
            <td>
              {{ p.name }}
              <span v-if="p.isWeighed" class="pill pill-amber" style="margin-left: 6px">tarozi</span>
              <div class="muted" style="font-size: 12px">MXIK: {{ p.mxikCode }}</div>
            </td>
            <td>{{ categoryName(p.categoryId) }}</td>
            <td class="text-right muted">{{ Number(p.costPrice).toLocaleString() }}</td>
            <td class="text-right">{{ Number(p.price).toLocaleString() }}</td>
            <td class="text-right">
              <span class="pill pill-green">{{ Number(p.markupPercent).toFixed(1) }}%</span>
            </td>
          </tr>
          <tr v-if="!visibleProducts.length && !loading">
            <td colspan="6" class="muted" style="text-align: center; padding: 24px">
              Mahsulot yo'q
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
