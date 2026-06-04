<script setup>
import { ref, onMounted, reactive, computed, watch } from 'vue'
import { productsApi, categoriesApi, suppliersApi, downloadBlob } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

const products = ref([])
const categories = ref([])
const suppliers = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const saving = ref(false)
const uploading = ref(false)

// Filters
const filterCategory = ref('')
const searchName = ref('')
const filterQtyOp = ref('')
const filterQtyVal = ref('')

// Pagination
const currentPage = ref(1)
const pageSize = ref(50)

const showModal = ref(false)
const editId = ref(null)
const newCategory = ref('')

const deleteTarget = ref(null)
const deleting = ref(false)

// Excel
const importInput = ref(null)
const importing = ref(false)
const importResult = ref(null)

function triggerImport() {
  importResult.value = null
  importInput.value?.click()
}

async function onImportFile(event) {
  const file = event.target.files?.[0]
  if (!file) return
  error.value = ''
  success.value = ''
  importResult.value = null
  importing.value = true
  try {
    importResult.value = await productsApi.importExcel(file)
    success.value = `Import: ${importResult.value.created} ta qo'shildi, ${importResult.value.failed} ta xato`
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    importing.value = false
    event.target.value = ''
  }
}

async function downloadTemplate() {
  try {
    downloadBlob(await productsApi.importTemplate(), 'mahsulot-shablon.xlsx')
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function exportExcel() {
  try {
    downloadBlob(await productsApi.exportExcel(), 'mahsulotlar.xlsx')
  } catch (e) {
    error.value = errorMessage(e)
  }
}

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

const filteredProducts = computed(() => {
  let list = products.value

  if (filterCategory.value) {
    list = list.filter((p) => p.categoryId === filterCategory.value)
  }

  if (searchName.value.trim()) {
    const q = searchName.value.trim().toLowerCase()
    list = list.filter(
      (p) => p.name.toLowerCase().includes(q) || (p.barcode || '').toLowerCase().includes(q)
    )
  }

  if (filterQtyOp.value && filterQtyVal.value !== '') {
    const val = Number(filterQtyVal.value)
    list = list.filter((p) => {
      const qty = Number(p.quantity)
      if (filterQtyOp.value === '=') return qty === val
      if (filterQtyOp.value === '>') return qty > val
      if (filterQtyOp.value === '<') return qty < val
      if (filterQtyOp.value === '>=') return qty >= val
      if (filterQtyOp.value === '<=') return qty <= val
      return true
    })
  }

  return list
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredProducts.value.length / pageSize.value)))

const paginatedProducts = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredProducts.value.slice(start, start + pageSize.value)
})

const pageNumbers = computed(() => {
  const total = totalPages.value
  const cur = currentPage.value
  const pages = []
  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i)
  } else if (cur <= 4) {
    for (let i = 1; i <= 5; i++) pages.push(i)
    pages.push('...')
    pages.push(total)
  } else if (cur >= total - 3) {
    pages.push(1)
    pages.push('...')
    for (let i = total - 4; i <= total; i++) pages.push(i)
  } else {
    pages.push(1)
    pages.push('...')
    for (let i = cur - 1; i <= cur + 1; i++) pages.push(i)
    pages.push('...')
    pages.push(total)
  }
  return pages
})

function setPage(p) {
  if (typeof p === 'number') currentPage.value = Math.min(Math.max(1, p), totalPages.value)
}

watch([searchName, filterCategory, filterQtyOp, filterQtyVal], () => {
  currentPage.value = 1
})

watch(pageSize, () => {
  currentPage.value = 1
})

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

function removeProduct(p) { deleteTarget.value = p }
function cancelDelete() { if (deleting.value) return; deleteTarget.value = null }

async function confirmDelete() {
  if (!deleteTarget.value) return
  error.value = ''
  deleting.value = true
  try {
    await productsApi.remove(deleteTarget.value.id)
    deleteTarget.value = null
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    deleting.value = false
  }
}

onMounted(loadAll)
</script>

<template>
  <div class="page-head">
    <h1>Mahsulotlar</h1>
    <div style="display:flex; gap:8px; align-items:center; flex-wrap:wrap">
      <button class="btn btn-ghost btn-sm" @click="downloadTemplate">📄 Shablon</button>
      <button class="btn btn-ghost btn-sm" :disabled="importing" @click="triggerImport">
        {{ importing ? '⏳ Import...' : '📥 Excel import' }}
      </button>
      <button class="btn btn-ghost btn-sm" @click="exportExcel">📤 Eksport</button>
      <button class="btn" @click="openCreate">+ Mahsulot qo'shish</button>
      <input ref="importInput" type="file" accept=".xlsx" style="display:none" @change="onImportFile" />
    </div>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>
  <div v-if="importResult && importResult.errors.length" class="alert alert-error">
    <strong>Import xatolari ({{ importResult.errors.length }}):</strong>
    <ul style="margin:6px 0 0; padding-left:18px">
      <li v-for="(e, i) in importResult.errors.slice(0, 10)" :key="i">{{ e }}</li>
    </ul>
  </div>

  <!-- Filters -->
  <div class="card" style="margin-bottom:12px; padding:16px 20px">
    <div style="display:flex; gap:12px; flex-wrap:wrap; align-items:center; margin-bottom:12px">
      <div style="position:relative; flex:1; min-width:220px; max-width:400px">
        <input
          v-model="searchName"
          class="input"
          placeholder="Nomi yoki barkod bo'yicha qidirish"
          style="padding-left:38px; width:100%; box-sizing:border-box"
        />
        <svg style="position:absolute; left:11px; top:50%; transform:translateY(-50%); color:#aaa; pointer-events:none" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
          <circle cx="11" cy="11" r="7"/><line x1="16.5" y1="16.5" x2="22" y2="22"/>
        </svg>
      </div>
      <select v-model="filterCategory" class="select" style="min-width:180px">
        <option value="">Barcha kategoriyalar</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>
    <div style="display:flex; gap:8px; align-items:center">
      <select v-model="filterQtyOp" class="select" style="width:150px">
        <option value="">Qoldiq filtri</option>
        <option value="=">=</option>
        <option value=">">&gt;</option>
        <option value="<">&lt;</option>
        <option value=">=">&gt;=</option>
        <option value="<=">&lt;=</option>
      </select>
      <input
        v-model="filterQtyVal"
        type="number"
        class="input"
        placeholder="Miqdor"
        style="width:160px"
        :disabled="!filterQtyOp"
      />
    </div>
  </div>

  <div class="card">
    <!-- Top bar: info + page size -->
    <div style="display:flex; justify-content:space-between; align-items:center; padding:10px 4px 12px; border-bottom:1px solid var(--border)">
      <div class="muted" style="font-size:13px">
        <template v-if="filteredProducts.length">
          {{ filteredProducts.length }} ta mahsulotdan
          {{ (currentPage - 1) * pageSize + 1 }}–{{ Math.min(currentPage * pageSize, filteredProducts.length) }}
          ko'rsatilmoqda
        </template>
        <template v-else>Natija yo'q</template>
      </div>
      <div style="display:flex; align-items:center; gap:8px">
        <span class="muted" style="font-size:13px">Sahifada:</span>
        <select v-model.number="pageSize" class="select" style="width:76px">
          <option :value="25">25</option>
          <option :value="50">50</option>
          <option :value="100">100</option>
        </select>
      </div>
    </div>

    <table class="table">
      <thead>
        <tr>
          <th style="width:52px"></th>
          <th>Nomi</th>
          <th>Kategoriya</th>
          <th>Barcode</th>
          <th>PLU</th>
          <th class="text-right">Qoldiq</th>
          <th class="text-right">Tannarx</th>
          <th class="text-right">Ustama</th>
          <th class="text-right">Narx</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td colspan="10" class="muted" style="text-align:center; padding:32px">Yuklanmoqda...</td>
        </tr>
        <tr v-for="p in paginatedProducts" :key="p.id">
          <td><div class="thumb"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></div></td>
          <td>
            <div style="font-weight:500">{{ p.name }}</div>
            <div class="muted" style="font-size:12px">MXIK: {{ p.mxikCode }}</div>
            <span v-if="p.isWeighed" class="pill pill-amber" style="margin-top:2px; display:inline-block">tarozi</span>
          </td>
          <td>{{ categoryName(p.categoryId) }}</td>
          <td class="muted" style="font-size:13px">{{ p.barcode || '—' }}</td>
          <td class="muted" style="font-size:13px">{{ p.pluNumber ?? '—' }}</td>
          <td class="text-right" :style="{ color: Number(p.quantity) <= 0 ? 'var(--danger)' : 'inherit' }">
            {{ Number(p.quantity).toLocaleString() }}
          </td>
          <td class="text-right muted">{{ Number(p.costPrice).toLocaleString() }}</td>
          <td class="text-right"><span class="pill pill-green">{{ Number(p.markupPercent).toFixed(1) }}%</span></td>
          <td class="text-right" style="font-weight:500">{{ Number(p.price).toLocaleString() }}</td>
          <td class="text-right" style="white-space:nowrap">
            <button class="btn btn-ghost btn-sm" @click="openEdit(p)">✏️</button>
            <button class="btn btn-ghost btn-sm" @click="removeProduct(p)">🗑️</button>
          </td>
        </tr>
        <tr v-if="!paginatedProducts.length && !loading">
          <td colspan="10" class="muted" style="text-align:center; padding:32px">Mahsulot yo'q</td>
        </tr>
      </tbody>
    </table>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="pagination-row">
      <button class="btn btn-ghost btn-sm pag-btn" :disabled="currentPage === 1" @click="setPage(currentPage - 1)">‹</button>
      <template v-for="pg in pageNumbers" :key="String(pg) + '_' + pageNumbers.indexOf(pg)">
        <span v-if="pg === '...'" class="muted pag-ellipsis">…</span>
        <button
          v-else
          class="btn btn-sm pag-btn"
          :class="{ 'pag-active': pg === currentPage, 'btn-ghost': pg !== currentPage }"
          @click="setPage(pg)"
        >{{ pg }}</button>
      </template>
      <button class="btn btn-ghost btn-sm pag-btn" :disabled="currentPage === totalPages" @click="setPage(currentPage + 1)">›</button>
    </div>
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

  <!-- O'chirishni tasdiqlash modali -->
  <div v-if="deleteTarget" class="modal-overlay" @click.self="cancelDelete">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Mahsulotni o'chirish</h3>
        <button class="modal-x" @click="cancelDelete">×</button>
      </div>
      <div class="modal-body">
        <div class="del-warn">⚠️</div>
        <p style="text-align:center; margin:0 0 6px">
          <strong>"{{ deleteTarget.name }}"</strong> mahsulotini o'chirmoqchimisiz?
        </p>
        <p class="muted" style="text-align:center; font-size:13px; margin:0">
          Bu amalni qaytarib bo'lmaydi.
        </p>
        <div class="del-actions">
          <button class="btn btn-ghost" :disabled="deleting" @click="cancelDelete">Bekor qilish</button>
          <button class="btn btn-danger" :disabled="deleting" @click="confirmDelete">
            {{ deleting ? 'O\'chirilmoqda...' : 'O\'chirish' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pagination-row {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 4px;
  padding: 16px 0 8px;
  flex-wrap: wrap;
}

.pag-btn {
  min-width: 32px;
  height: 32px;
  padding: 0 8px;
  font-size: 13px;
  border-radius: 6px;
}

.pag-active {
  background: var(--primary);
  color: #fff;
  border-color: var(--primary);
}

.pag-ellipsis {
  padding: 0 6px;
  font-size: 15px;
}
</style>
