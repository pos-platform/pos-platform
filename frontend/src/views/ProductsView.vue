<script setup>
import { ref, onMounted, reactive, computed } from 'vue'
import { productsApi, categoriesApi, suppliersApi } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

const products = ref([])
const categories = ref([])
const suppliers = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const saving = ref(false)
const uploading = ref(false)
const filterCategory = ref('')

const showModal = ref(false)
const editId = ref(null) // null = yaratish, aks holda tahrir
const newCategory = ref('')

const blankForm = () => ({
  name: '',
  mxikCode: '',
  barcode: '',
  description: '',
  costPrice: 0,
  markupPercent: 0,
  price: 0,
  quantity: 0,
  imageUrl: '',
  isVat: true,
  isMarked: false,
  supplierId: '',
  unit: 'Piece',
  isWeighed: false,
  pluNumber: null,
  tare: null,
  categoryId: ''
})
const form = reactive(blankForm())

const visibleProducts = computed(() =>
  filterCategory.value
    ? products.value.filter((p) => p.categoryId === filterCategory.value)
    : products.value
)

function round2(n) { return Math.round((Number(n) || 0) * 100) / 100 }
function recalcPrice() {
  const cost = Number(form.costPrice) || 0
  const markup = Number(form.markupPercent) || 0
  form.price = round2(cost * (1 + markup / 100))
}
function recalcMarkup() {
  const cost = Number(form.costPrice) || 0
  const price = Number(form.price) || 0
  form.markupPercent = cost > 0 ? round2(((price - cost) / cost) * 100) : 0
}

async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    const [p, c, s] = await Promise.all([productsApi.list(), categoriesApi.list(), suppliersApi.list()])
    products.value = p
    categories.value = c
    suppliers.value = s
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function categoryName(id) {
  return categories.value.find((c) => c.id === id)?.name || '—'
}

async function genBarcode() {
  try { form.barcode = await productsApi.generateBarcode() } catch (e) { error.value = errorMessage(e) }
}
async function genPlu() {
  try { form.pluNumber = await productsApi.nextPlu() } catch (e) { error.value = errorMessage(e) }
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

async function openCreate() {
  Object.assign(form, blankForm())
  if (categories.value.length) form.categoryId = categories.value[0].id
  editId.value = null
  error.value = ''
  showModal.value = true
  await genBarcode()
}

function openEdit(p) {
  Object.assign(form, {
    name: p.name,
    mxikCode: p.mxikCode,
    barcode: p.barcode || '',
    description: p.description || '',
    costPrice: p.costPrice,
    markupPercent: p.markupPercent,
    price: p.price,
    quantity: p.quantity,
    imageUrl: p.imageUrl || '',
    isVat: p.isVat,
    isMarked: p.isMarked,
    supplierId: p.supplierId || '',
    unit: p.unit,
    isWeighed: p.isWeighed,
    pluNumber: p.pluNumber,
    tare: p.tare,
    categoryId: p.categoryId
  })
  editId.value = p.id
  error.value = ''
  showModal.value = true
}

function closeModal() { showModal.value = false }

async function submit() {
  error.value = ''
  success.value = ''
  saving.value = true
  try {
    const payload = {
      name: form.name.trim(),
      mxikCode: form.mxikCode.trim(),
      barcode: form.barcode?.trim() || null,
      description: form.description?.trim() || null,
      costPrice: Number(form.costPrice) || 0,
      price: Number(form.price) || 0,
      quantity: Number(form.quantity) || 0,
      imageUrl: form.imageUrl || null,
      isVat: form.isVat,
      isMarked: form.isMarked,
      supplierId: form.supplierId || null,
      unit: form.unit,
      isWeighed: form.isWeighed,
      pluNumber: form.pluNumber ? Number(form.pluNumber) : null,
      tare: form.tare ? Number(form.tare) : null,
      categoryId: form.categoryId
    }
    if (editId.value) {
      await productsApi.update(editId.value, payload)
      success.value = `"${payload.name}" yangilandi`
    } else {
      await productsApi.create(payload)
      success.value = `"${payload.name}" qo'shildi`
    }
    showModal.value = false
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function removeProduct(p) {
  if (!window.confirm(`"${p.name}" mahsulotini o'chirasizmi?`)) return
  error.value = ''
  try {
    await productsApi.remove(p.id)
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(loadAll)
</script>

<template>
  <div class="page-head">
    <h1>Mahsulotlar</h1>
    <div style="display:flex; gap:12px; align-items:center">
      <span class="muted">{{ visibleProducts.length }} / {{ products.length }} ta</span>
      <button class="btn" @click="openCreate">+ Mahsulot qo'shish</button>
    </div>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card">
    <div class="field" style="max-width:260px; margin-bottom:12px">
      <label>Kategoriya bo'yicha filtr</label>
      <select v-model="filterCategory" class="select">
        <option value="">Barchasi</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <table class="table">
      <thead>
        <tr>
          <th></th><th>Nomi</th><th>Kategoriya</th>
          <th class="text-right">Qoldiq</th><th class="text-right">Tannarx</th>
          <th class="text-right">Sotish</th><th class="text-right">Ustama</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in visibleProducts" :key="p.id">
          <td><div class="thumb"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></div></td>
          <td>
            {{ p.name }}
            <span v-if="p.isWeighed" class="pill pill-amber" style="margin-left:6px">tarozi</span>
            <div class="muted" style="font-size:12px">MXIK: {{ p.mxikCode }}</div>
          </td>
          <td>{{ categoryName(p.categoryId) }}</td>
          <td class="text-right" :style="{ color: Number(p.quantity) <= 0 ? 'var(--danger)' : 'inherit' }">{{ Number(p.quantity).toLocaleString() }}</td>
          <td class="text-right muted">{{ Number(p.costPrice).toLocaleString() }}</td>
          <td class="text-right">{{ Number(p.price).toLocaleString() }}</td>
          <td class="text-right"><span class="pill pill-green">{{ Number(p.markupPercent).toFixed(1) }}%</span></td>
          <td class="text-right" style="white-space:nowrap">
            <button class="btn btn-ghost btn-sm" @click="openEdit(p)">✏️</button>
            <button class="btn btn-ghost btn-sm" @click="removeProduct(p)">🗑️</button>
          </td>
        </tr>
        <tr v-if="!visibleProducts.length && !loading">
          <td colspan="8" class="muted" style="text-align:center; padding:24px">Mahsulot yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Modal -->
  <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
    <div class="modal-box">
      <div class="modal-head">
        <h3>{{ editId ? 'Mahsulotni tahrirlash' : 'Mahsulot qo\'shish' }}</h3>
        <button class="modal-x" @click="closeModal">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

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
              <div v-if="uploading" class="muted" style="font-size:12px">Yuklanmoqda...</div>
            </div>
          </div>
        </div>

        <div class="row">
          <div class="field">
            <label>MXIK (ИКПУ) *</label>
            <input v-model="form.mxikCode" class="input" required placeholder="Soliq uchun majburiy" />
          </div>
          <div class="field">
            <label>Shtrix-kod (kod tovara)</label>
            <div style="display:flex; gap:6px">
              <input v-model="form.barcode" class="input" style="flex:1" />
              <button type="button" class="btn btn-ghost" title="Generatsiya" @click="genBarcode">🔄</button>
            </div>
          </div>
          <div class="field" style="max-width:120px">
            <label>Birlik</label>
            <select v-model="form.unit" class="select">
              <option value="Piece">Dona</option>
              <option value="Kilogram">Kg</option>
            </select>
          </div>
        </div>

        <div class="row">
          <div class="field">
            <label>Kategoriya *</label>
            <select v-model="form.categoryId" class="select" required>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div class="field">
            <label>Yetkazib beruvchi</label>
            <select v-model="form.supplierId" class="select">
              <option value="">— tanlanmagan —</option>
              <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
        </div>

        <div class="field">
          <label>Nomi *</label>
          <input v-model="form.name" class="input" required />
        </div>
        <div class="field">
          <label>Tavsif</label>
          <input v-model="form.description" class="input" />
        </div>

        <div class="row">
          <div class="field">
            <label>Tannarx</label>
            <input v-model="form.costPrice" type="number" min="0" step="0.01" class="input" @input="recalcPrice" />
          </div>
          <div class="field">
            <label>Ustama %</label>
            <input v-model="form.markupPercent" type="number" step="0.1" class="input" @input="recalcPrice" />
          </div>
          <div class="field">
            <label>Sotish narxi *</label>
            <input v-model="form.price" type="number" min="0" step="0.01" class="input" required @input="recalcMarkup" />
          </div>
        </div>

        <div class="row" style="align-items:center">
          <div class="field" style="max-width:160px">
            <label>Qoldiq</label>
            <input v-model="form.quantity" type="number" min="0" step="0.001" class="input" />
          </div>
          <div class="toggle-row" style="margin-top:18px">
            <label class="toggle"><input v-model="form.isVat" type="checkbox" /> НДС (QQS)</label>
            <label class="toggle"><input v-model="form.isMarked" type="checkbox" /> Маркировка</label>
          </div>
        </div>

        <div class="checkbox">
          <input id="weighed" v-model="form.isWeighed" type="checkbox" />
          <label for="weighed" style="margin:0">Tarozida tortiladi</label>
        </div>
        <div v-if="form.isWeighed" class="row" style="margin-top:8px">
          <div class="field">
            <label>PLU raqami</label>
            <div style="display:flex; gap:6px">
              <input v-model="form.pluNumber" type="number" min="0" class="input" style="flex:1" />
              <button type="button" class="btn btn-ghost" title="Generatsiya" @click="genPlu">🔄</button>
            </div>
          </div>
          <div class="field">
            <label>Tara (kg)</label>
            <input v-model="form.tare" type="number" min="0" step="0.001" class="input" />
          </div>
        </div>

        <!-- Yangi kategoriya (faqat yaratishda) -->
        <template v-if="!editId">
          <hr style="border:none;border-top:1px solid var(--border);margin:14px 0" />
          <label class="muted" style="font-weight:600;font-size:13px">Yangi kategoriya</label>
          <div class="row" style="margin-top:6px">
            <input v-model="newCategory" class="input" placeholder="Kategoriya nomi" style="flex:1" />
            <button type="button" class="btn btn-ghost" @click="addCategory">+</button>
          </div>
        </template>

        <button class="btn" style="width:100%; margin-top:16px" :disabled="saving || uploading">
          {{ saving ? 'Saqlanmoqda...' : (editId ? 'Saqlash' : 'Qo\'shish') }}
        </button>
      </form>
    </div>
  </div>
</template>
