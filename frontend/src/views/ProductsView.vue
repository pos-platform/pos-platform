<script setup>
import { ref, onMounted, onUnmounted, reactive, computed, watch, nextTick } from 'vue'
import { productsApi, categoriesApi, suppliersApi, storesApi, stockReceiptsApi, newRequestId, downloadBlob } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'
import { useAuthStore } from '../stores/auth'
import JsBarcode from 'jsbarcode'
import AppIcon from '../components/AppIcon.vue'

const auth = useAuthStore()

const products = ref([])
const categories = ref([])
const suppliers = ref([])
const stores = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const saving = ref(false)
const uploading = ref(false)
// Modal ichidagi xabar — modal yopilmasdan ketma-ket mahsulot qo'shish uchun
const modalSuccess = ref('')
const nameInput = ref(null)
const addedCount = ref(0)

// Filters
const filterCategory = ref('')
const filterBranch = ref('')
const searchName = ref('')
const filterQtyOp = ref('')
const filterQtyVal = ref('')
const filterUnit = ref('') // '' = hammasi, 'Kilogram' = kg, 'Piece' = dona
const filterOnline = ref('') // '' = hammasi, 'yes' = ilovada, 'no' = ilovada emas
const filterMxik = ref('') // '' = hammasi, 'check' = MXIK yaroqsiz (hammasi), 'suggest' = taklif bor, 'none' = taklif ham yo'q
const filterImage = ref('') // '' = hammasi, 'yes' = rasmli, 'no' = rasmsiz
const mxikValid = (code) => /^\d{17}$/.test(code || '')
// Kategoriya filtri daraxt ko'rinishida: asosiy bo'lim, ostida ichki bo'limlari.
const categoryOptions = computed(() => {
  const all = categories.value
  const ids = new Set(all.map((c) => c.id))
  const bySort = (a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name)
  return all.filter((c) => !c.parentId || !ids.has(c.parentId)).sort(bySort).flatMap((r) => [
    { id: r.id, label: r.name },
    ...all.filter((c) => c.parentId === r.id).sort(bySort).map((c) => ({ id: c.id, label: ' ↳ ' + c.name }))
  ])
})

// Ommaviy tanlash ("Ilovada ko'rsatish / yashirish")
const selectedIds = ref(new Set())
const bulkBusy = ref(false)
const bulkMsg = ref('')

// Barcode scanner
const barcodeInput = ref(null)
const barcodeBuffer = ref('')
const barcodeHighlight = ref(null) // topilgan mahsulot ID

function onBarcodeKey(e) {
  if (e.key === 'Enter') {
    const code = barcodeBuffer.value.trim()
    barcodeBuffer.value = ''
    if (!code) return
    const found = products.value.find(
      (p) => p.barcode && p.barcode.toLowerCase() === code.toLowerCase()
    )
    if (found) {
      barcodeHighlight.value = found.id
      searchName.value = ''
      filterCategory.value = ''
      currentPage.value = 1
      // Sahifada qaysi sahifada ekanligini toping
      const idx = filteredProducts.value.findIndex((p) => p.id === found.id)
      if (idx >= 0) currentPage.value = Math.floor(idx / pageSize.value) + 1
      setTimeout(() => {
        document.getElementById(`product-row-${found.id}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
        setTimeout(() => { barcodeHighlight.value = null }, 2500)
      }, 50)
    } else {
      error.value = `Barcode topilmadi: ${code}`
      setTimeout(() => { error.value = '' }, 3000)
    }
  } else if (e.key.length === 1) {
    barcodeBuffer.value += e.key
  }
}

// Pagination
const currentPage = ref(1)
const pageSize = ref(25)

const showModal = ref(false)
const editId = ref(null)


const deleteTarget = ref(null)
const deleting = ref(false)

// Excel
const importInput = ref(null)
const txtImportInput = ref(null)
const importing = ref(false)
const importResult = ref(null)

function triggerImport() {
  importResult.value = null
  importInput.value?.click()
}

function triggerTxtImport() {
  importResult.value = null
  txtImportInput.value?.click()
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
    success.value = `Import: ${importResult.value.created} ta qo'shildi, ${importResult.value.updated} ta yangilandi, ${importResult.value.failed} ta xato`
      + (importResult.value.stockSkipped
        ? `. ${importResult.value.stockSkipped} ta mavjud tovarning soni o'zgartirilmadi — qoldiq kirim hujjati orqali kiritiladi`
        : '')
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

async function downloadTxtTemplate() {
  try {
    downloadBlob(await productsApi.importTemplateTxt(), 'mahsulot-shablon.txt')
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

// Filtrga mos BARCHA mahsulotlarni (sahifalashdan qat'i nazar — filterUnit=Kilogram
// bo'lsa hamma kg tovar) FENIX/tarozi formatida (;-ajratilgan) .txt qilib yuklaydi.
// Qator: kod;nomi;;narx;0;0;0;kod;0;0;;01.01.01;0;0;0;0;01.01.01
function exportFenixTxt() {
  // Diqqat: paginatedProducts emas — joriy sahifa emas, butun filtrlangan ro'yxat.
  const list = filteredProducts.value
  if (!list.length) {
    error.value = 'Eksport uchun mahsulot yo\'q'
    setTimeout(() => { error.value = '' }, 3000)
    return
  }

  // Kodlar takrorlanmasligi kerak: aks holda tarozi bir qatorni ikkinchisi bilan
  // almashtiradi va tovarlar "yo'qoladi". Avval mavjud PLU'larni band qilamiz,
  // qolganlariga esa bo'sh kodlarni ketma-ket beramiz.
  const used = new Set()
  let dupPlu = 0
  for (const p of list) {
    const plu = Number(p.pluNumber)
    if (p.pluNumber != null && plu > 0) {
      if (used.has(plu)) dupPlu++
      used.add(plu)
    }
  }
  let seq = 0
  const nextFreeCode = () => {
    do { seq++ } while (used.has(seq))
    used.add(seq)
    return seq
  }

  const lines = list.map((p) => {
    const plu = Number(p.pluNumber)
    const code = p.pluNumber != null && plu > 0 ? plu : nextFreeCode()
    const name = (p.name || '').replace(/;/g, ' ').replace(/[\r\n]+/g, ' ').trim()
    const price = Math.round(Number(p.price) || 0)
    return `${code};${name};;${price};0;0;0;${code};0;0;;01.01.01;0;0;0;0;01.01.01`
  })

  // UTF-8 BOM (﻿) — kirill harflari Notepad/eski dasturlarda buzilmasligi uchun
  const blob = new Blob(['﻿' + lines.join('\r\n') + '\r\n'], { type: 'text/plain;charset=utf-8' })
  const suffix = filterUnit.value === 'Kilogram' ? '-kg' : filterUnit.value === 'Piece' ? '-dona' : ''
  downloadBlob(blob, `mahsulotlar${suffix}.txt`)

  success.value = `${lines.length} ta mahsulot .txt ga eksport qilindi (barcha sahifalar)`
  setTimeout(() => { success.value = '' }, 4000)

  // Bir xil PLU'li mahsulotlar tarozida bir-birini almashtiradi — ogohlantiramiz.
  if (dupPlu > 0) {
    error.value = `Diqqat: ${dupPlu} ta mahsulotning PLU raqami takrorlangan — tarozida ular bir-birini almashtiradi. PLU'larni tuzating.`
    setTimeout(() => { error.value = '' }, 8000)
  }
}

const blankForm = () => ({
  name: '',
  mxikCode: '',
  mxikSuggestion: '',
  mxikSuggestionSource: '',
  barcode: '',
  description: '',
  costPrice: 0,
  markupPercent: 0,
  price: 0,
  quantity: 0,
  imageUrl: '',
  isVat: true,
  showOnline: true,
  isMarked: false,
  markingCode: '',
  packageCode: '',
  commitentTin: '',
  supplierId: '',
  unit: 'Piece',
  isWeighed: false,
  pluNumber: null,
  tare: null,
  categoryId: '',
  branchId: '',
  // Tahrirlashda: qo'shimcha kirim (qoldiqqa qo'shiladi, Kirim hujjati yaratiladi)
  addQty: '',
  addCost: ''
})
const form = reactive(blankForm())

// ───── Tez kirim (mavjud tovarga qoldiq qo'shish → Kirim hujjati) ─────
const restockTarget = ref(null)
const restockSaving = ref(false)
const restockError = ref('')
const restock = reactive({ quantity: '', costPrice: 0, supplierId: '', branchId: '' })
let restockRequestId = newRequestId()

const activeSuppliers = computed(() => suppliers.value.filter((s) => s.isActive !== false))

// Kirim oynasida yangi yetkazib beruvchini joyida qo'shish
const newSupplierOpen = ref(false)
const newSupplierSaving = ref(false)
const newSupplier = reactive({ name: '', phone: '' })

function toggleNewSupplier() {
  newSupplierOpen.value = !newSupplierOpen.value
  newSupplier.name = ''
  newSupplier.phone = ''
  if (newSupplierOpen.value) nextTick(() => document.getElementById('new-supplier-name')?.focus())
}

async function saveNewSupplier() {
  const name = newSupplier.name.trim()
  if (!name) { restockError.value = 'Yetkazib beruvchi nomini kiriting'; return }
  newSupplierSaving.value = true
  restockError.value = ''
  try {
    const created = await suppliersApi.create({ name, phone: newSupplier.phone.trim() || null, contactPerson: null, address: null })
    suppliers.value = [...suppliers.value, created]
    restock.supplierId = created.id
    newSupplierOpen.value = false
  } catch (e) {
    restockError.value = errorMessage(e)
  } finally {
    newSupplierSaving.value = false
  }
}

function defaultBranchFor(p) {
  return p?.branchId || filterBranch.value || (stores.value.length === 1 ? stores.value[0].id : '')
}

function openRestock(p) {
  restockTarget.value = p
  restockError.value = ''
  newSupplierOpen.value = false
  restockRequestId = newRequestId()
  Object.assign(restock, {
    quantity: '',
    costPrice: Number(p.costPrice) || 0,
    supplierId: p.supplierId || '',
    branchId: defaultBranchFor(p)
  })
  nextTick(() => document.getElementById('restock-qty')?.focus())
}

function closeRestock() {
  if (restockSaving.value) return
  restockTarget.value = null
}

// Bitta tovar uchun Kirim hujjati yaratadi (ombor, partiya, yetkazib beruvchi qarzi yangilanadi).
function createReceiptFor(productId, { quantity, costPrice, supplierId, branchId }, requestId) {
  return stockReceiptsApi.create({
    supplierId,
    branchId: branchId || null,
    comment: 'Mahsulotlar sahifasidan tez kirim',
    clientRequestId: requestId,
    items: [{ productId, quantity: Number(quantity), costPrice: Number(costPrice) || 0 }]
  })
}

function validateReceipt({ quantity, supplierId, branchId }) {
  if (!(Number(quantity) > 0)) return 'Kirim sonini kiriting'
  if (!supplierId) return 'Kirim uchun yetkazib beruvchini tanlang'
  if (stores.value.length > 1 && !branchId) return 'Kirim uchun do\'konni tanlang'
  return ''
}

async function submitRestock() {
  restockError.value = validateReceipt(restock)
  if (restockError.value) return
  restockSaving.value = true
  try {
    const p = restockTarget.value
    const r = await createReceiptFor(p.id, restock, restockRequestId)
    success.value = `"${p.name}" ga ${Number(restock.quantity).toLocaleString()} ${p.unit === 'Kilogram' ? 'kg' : 'dona'} kirim qilindi (${r.number})`
    restockTarget.value = null
    await loadAll()
  } catch (e) {
    restockError.value = errorMessage(e)
  } finally {
    restockSaving.value = false
  }
}

// ───── Takrorni aniqlash (yangi mahsulot qo'shishda) ─────
const duplicateProduct = computed(() => {
  if (!showModal.value || editId.value) return null
  const bc = (form.barcode || '').trim().toLowerCase()
  const nm = (form.name || '').trim().toLowerCase()
  if (!bc && !nm) return null
  return products.value.find((p) =>
    (bc && (p.barcode || '').trim().toLowerCase() === bc) ||
    (nm && p.name.trim().toLowerCase() === nm)) || null
})

function restockDuplicate() {
  const p = duplicateProduct.value
  if (!p) return
  showModal.value = false
  openRestock(p)
}

function editDuplicate() {
  const p = duplicateProduct.value
  if (p) openEdit(p)
}

const filteredProducts = computed(() => {
  let list = products.value

  if (filterBranch.value) {
    list = list.filter((p) => p.branchId === filterBranch.value)
  }

  if (filterCategory.value) {
    // Asosiy bo'lim tanlansa — ichki bo'limlaridagi mahsulotlar ham.
    const ids = new Set([filterCategory.value,
      ...categories.value.filter((c) => c.parentId === filterCategory.value).map((c) => c.id)])
    list = list.filter((p) => ids.has(p.categoryId))
  }

  if (filterImage.value) {
    const want = filterImage.value === 'yes'
    list = list.filter((p) => !!p.imageUrl === want)
  }

  if (filterMxik.value) {
    list = list.filter((p) =>
      !mxikValid(p.mxikCode) && (
        filterMxik.value === 'suggest' ? !!p.mxikSuggestion
          : filterMxik.value === 'none' ? !p.mxikSuggestion
            : true))
  }

  if (filterUnit.value) {
    list = list.filter((p) => p.unit === filterUnit.value)
  }

  if (filterOnline.value) {
    const want = filterOnline.value === 'yes'
    list = list.filter((p) => (p.showOnline !== false) === want)
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

// Filtrlangan ro'yxat bo'yicha umumiy qiymatlar (tannarx, sotuv qiymati, foyda)
const summary = computed(() => {
  let cost = 0
  let value = 0
  for (const p of filteredProducts.value) {
    const qty = Number(p.quantity) || 0
    cost += qty * (Number(p.costPrice) || 0)
    value += qty * (Number(p.price) || 0)
  }
  return { cost, value, profit: value - cost }
})

function fmtSum(n) {
  return Math.round(Number(n) || 0).toLocaleString('ru-RU')
}

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

watch([searchName, filterCategory, filterBranch, filterQtyOp, filterQtyVal, filterUnit, filterOnline, filterMxik, filterImage], () => {
  currentPage.value = 1
})

// ───── ommaviy tanlash ─────
const allOnPageSelected = computed(
  () => paginatedProducts.value.length > 0 && paginatedProducts.value.every((p) => selectedIds.value.has(p.id))
)

function toggleSelect(id) {
  const s = new Set(selectedIds.value)
  if (s.has(id)) s.delete(id)
  else s.add(id)
  selectedIds.value = s
}

function toggleSelectPage() {
  const s = new Set(selectedIds.value)
  const all = allOnPageSelected.value
  for (const p of paginatedProducts.value) {
    if (all) s.delete(p.id)
    else s.add(p.id)
  }
  selectedIds.value = s
}

function clearSelection() {
  selectedIds.value = new Set()
}

async function bulkShowOnline(show) {
  const ids = [...selectedIds.value]
  if (!ids.length || bulkBusy.value) return
  bulkBusy.value = true
  bulkMsg.value = ''
  error.value = ''
  try {
    const res = await productsApi.setShowOnline(ids, show)
    // Yuklangan ro'yxatni joyida yangilaymiz (qayta yuklamasdan).
    const set = new Set(ids)
    for (const p of products.value) if (set.has(p.id)) p.showOnline = show
    bulkMsg.value = `${res?.updated ?? ids.length} ta mahsulot ${show ? 'ilovada ko\'rsatildi' : 'ilovadan yashirildi'}.`
    clearSelection()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    bulkBusy.value = false
  }
}

watch(pageSize, () => {
  currentPage.value = 1
})

function onUnitChange() {
  if (form.unit === 'Piece') {
    form.pluNumber = null
    form.isWeighed = false
    form.tare = null
  }
}

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

// Filtrlangan ("taklif bor") mahsulotlarning MXIK taklifini haqiqiy MXIK kodga o'tkazadi
const applyingMxik = ref(false)
async function applyMxikSuggestions() {
  const ids = filteredProducts.value.map((p) => p.id)
  if (!ids.length) return
  if (!confirm(`${ids.length} ta mahsulotga taklif qilingan MXIK kod asosiy MXIK sifatida yozilsinmi?`)) return
  applyingMxik.value = true
  error.value = ''
  success.value = ''
  try {
    const r = await productsApi.applyMxikSuggestions(ids)
    success.value = `${r.updated} ta mahsulotga MXIK kod yozildi`
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    applyingMxik.value = false
  }
}

async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    const [p, c, s, st] = await Promise.all([productsApi.list(), categoriesApi.list(), suppliersApi.list(), storesApi.list()])
    products.value = p
    categories.value = c
    suppliers.value = s
    stores.value = st
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


async function openCreate() {
  Object.assign(form, blankForm())
  // Agar bitta do'kon bo'lsa — avtomatik tanlash (polya yashiriladi)
  if (stores.value.length === 1) {
    form.branchId = stores.value[0].id
  }
  editId.value = null
  error.value = ''
  modalSuccess.value = ''
  addedCount.value = 0
  showModal.value = true
  focusName()
}

function openEdit(p) {
  Object.assign(form, {
    name: p.name,
    mxikCode: p.mxikCode,
    mxikSuggestion: p.mxikSuggestion || '',
    mxikSuggestionSource: p.mxikSuggestionSource || '',
    barcode: p.barcode || '',
    description: p.description || '',
    costPrice: p.costPrice,
    markupPercent: p.markupPercent,
    price: p.price,
    quantity: p.quantity,
    imageUrl: p.imageUrl || '',
    isVat: p.isVat,
    showOnline: p.showOnline !== false,
    isMarked: p.isMarked,
    markingCode: p.markingCode || '',
    packageCode: p.packageCode || '',
    commitentTin: p.commitentTin || '',
    supplierId: p.supplierId || '',
    unit: p.unit,
    isWeighed: p.isWeighed,
    pluNumber: p.pluNumber,
    tare: p.tare,
    categoryId: p.categoryId,
    branchId: p.branchId || (stores.value.length === 1 ? stores.value[0].id : ''),
    addQty: '',
    addCost: Number(p.costPrice) || 0
  })
  restockRequestId = newRequestId()
  editId.value = p.id
  error.value = ''
  modalSuccess.value = ''
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  modalSuccess.value = ''
}

function focusName() {
  nextTick(() => nameInput.value?.focus())
}

// Modal yopilmasdan keyingi mahsulotni kiritish uchun formani tozalash.
// Kategoriya / ta'minotchi / do'kon / birlik / НДС kabi takrorlanuvchi maydonlar saqlanadi.
function resetForNext() {
  const keep = {
    categoryId: form.categoryId,
    supplierId: form.supplierId,
    branchId: form.branchId,
    unit: form.unit,
    isVat: form.isVat,
    isMarked: form.isMarked,
    markupPercent: form.markupPercent
  }
  Object.assign(form, blankForm(), keep)
}

async function submit() {
  error.value = ''
  success.value = ''
  modalSuccess.value = ''

  // Agar soni > 0 va do'kon tanlanmagan bo'lsa, yagona do'konni avtomatik tanlash
  // (tahrirlashda qoldiq o'zgarmaydi — tekshiruv faqat yangi mahsulot uchun)
  const qty = editId.value ? 0 : Number(form.quantity) || 0
  if (qty > 0 && !form.branchId && stores.value.length === 1) {
    form.branchId = stores.value[0].id
  }
  // Bir nechta do'kon bo'lsa va tanlalmagan bo'lsa — xato
  if (qty > 0 && !form.branchId && stores.value.length > 1) {
    error.value = 'Soni kiritish uchun avval do\'kon tanlang'
    return
  }

  // Tahrirlashda qo'shimcha kirim kiritilgan bo'lsa — oldindan tekshiramiz
  const addQty = editId.value ? Number(form.addQty) || 0 : 0
  const receipt = { quantity: addQty, costPrice: form.addCost, supplierId: form.supplierId, branchId: form.branchId || defaultBranchFor(null) }
  if (addQty > 0) {
    const msg = validateReceipt(receipt)
    if (msg) { error.value = msg; return }
  }

  saving.value = true
  try {
    const payload = {
      name: form.name.trim(),
      mxikCode: form.mxikCode.trim() || '00000000',
      barcode: form.barcode?.trim() || null,
      description: form.description?.trim() || null,
      costPrice: Number(form.costPrice) || 0,
      price: Number(form.price) || 0,
      quantity: qty,
      imageUrl: form.imageUrl || null,
      isVat: form.isVat,
      showOnline: form.showOnline,
      isMarked: form.isMarked,
      markingCode: form.markingCode?.trim() || null,
      packageCode: form.packageCode?.trim() || null,
      commitentTin: form.commitentTin?.trim() || null,
      supplierId: form.supplierId || null,
      unit: form.unit,
      isWeighed: form.isWeighed,
      pluNumber: form.pluNumber ? Number(form.pluNumber) : null,
      tare: form.tare ? Number(form.tare) : null,
      categoryId: form.categoryId,
      branchId: form.branchId || null
    }
    if (editId.value) {
      await productsApi.update(editId.value, payload)
      success.value = `"${payload.name}" yangilandi`
      if (addQty > 0) {
        const r = await createReceiptFor(editId.value, receipt, restockRequestId)
        restockRequestId = newRequestId()
        success.value += ` va ${addQty.toLocaleString()} ${payload.unit === 'Kilogram' ? 'kg' : 'dona'} kirim qilindi (${r.number})`
      }
      showModal.value = false
      modalSuccess.value = ''
    } else {
      await productsApi.create(payload)
      success.value = `"${payload.name}" qo'shildi`
      // Modal ochiq qoladi — ketma-ket mahsulot qo'shish uchun
      addedCount.value++
      modalSuccess.value = `"${payload.name}" qo'shildi. Keyingisini kiriting.`
      resetForNext()
      focusName()
    }
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
    await loadAll()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    deleting.value = false
    deleteTarget.value = null
  }
}

let stockPollTimer = null

async function refreshStock() {
  if (loading.value || saving.value || showModal.value) return
  try {
    const fresh = await productsApi.list()
    products.value = fresh
  } catch {
    // silent — foydalanuvchiga ko'rsatilmaydi
  }
}

onMounted(async () => {
  await loadAll()
  barcodeInput.value?.focus()
  // Qoldiqlarni har 30 soniyada yangilab turadi (savdo bo'lsa ko'rinadi)
  stockPollTimer = setInterval(refreshStock, 30_000)
})

onUnmounted(() => {
  clearInterval(stockPollTimer)
})

function printBarcode(product) {
  // Barcode bo'lmasa PLU dan foydalaniladi, u ham bo'lmasa faqat nom+narx chop etiladi
  const code = String(product.barcode || '').trim()
    || (product.pluNumber != null && Number(product.pluNumber) > 0 ? String(product.pluNumber) : '')

  let codeHtml = ''
  if (code) {
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
    try {
      JsBarcode(svg, code, {
        format: 'CODE128',
        width: 1.4,
        height: 38,
        displayValue: true,
        fontSize: 10,
        textMargin: 0,
        margin: 2,
        background: '#ffffff',
        lineColor: '#000000'
      })
      codeHtml = new XMLSerializer().serializeToString(svg)
    } catch {
      // Barcode formati chizilmasa — kodni matn ko'rinishida chiqaramiz
      codeHtml = `<div class="code">${code}</div>`
    }
  }

  const priceFmt = Number(product.price).toLocaleString()

  const w = window.open('', '_blank', 'width=400,height=280')
  if (!w) {
    error.value = 'Chop etish oynasi ochilmadi. Brauzerda popup (pop-up) ruxsatini yoqing.'
    setTimeout(() => { error.value = '' }, 5000)
    return
  }
  w.document.write(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>${product.name}</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    /* Etiketka qog'ozi: 58mm x 30mm */
    @page { size: 58mm 30mm; margin: 0; }
    html, body { width: 58mm; height: 30mm; background: #fff; }
    body { font-family: Arial, sans-serif; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .label {
      width: 58mm; height: 30mm;
      display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      padding: 1mm 2mm; overflow: hidden;
    }
    .name {
      font-weight: bold; font-size: 11px; text-align: center; line-height: 1.1;
      max-width: 100%;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    svg {
      display: block;
      max-width: 54mm; max-height: 14mm;
      width: auto; height: auto;
    }
    .code { font-size: 11px; letter-spacing: 1px; line-height: 1.4; }
    .price { font-weight: bold; font-size: 15px; line-height: 1.1; }
    .label.no-code .name { font-size: 13px; white-space: normal; }
    .label.no-code .price { font-size: 20px; margin-top: 1mm; }
  </style>
</head>
<body>
  <div class="label${codeHtml ? '' : ' no-code'}">
    <div class="name">${product.name}</div>
    ${codeHtml}
    <div class="price">${priceFmt}</div>
  </div>
  <script>
    setTimeout(function() { window.print(); }, 300);
  <\/script>
</body>
</html>`)
  w.document.close()
}
</script>

<template>
  <div class="page-head">
    <h1>Mahsulotlar</h1>
    <div style="display:flex; gap:8px; align-items:center; flex-wrap:wrap">
      <!-- Hozircha yashirilgan (kerak bo'lsa qaytariladi): Shablon, TXT shablon, TXT import
      <button class="btn btn-ghost btn-sm" @click="downloadTemplate">Shablon</button>
      <button class="btn btn-ghost btn-sm" @click="downloadTxtTemplate">TXT shablon</button>
      -->
      <button class="btn btn-ghost btn-sm" :disabled="importing" @click="triggerImport">
        <AppIcon name="upload" :size="15" />{{ importing ? 'Import...' : 'Excel import' }}
      </button>
      <!--
      <button class="btn btn-ghost btn-sm" :disabled="importing" @click="triggerTxtImport">
        {{ importing ? 'Import...' : 'TXT import' }}
      </button>
      -->
      <button class="btn btn-ghost btn-sm" @click="exportFenixTxt"><AppIcon name="download" :size="15" />Eksport (.txt)</button>
      <button class="btn btn-ghost btn-sm" @click="exportExcel"><AppIcon name="sheet" :size="15" />Excel</button>
      <button class="btn" @click="openCreate">+ Mahsulot qo'shish</button>
      <input ref="importInput" type="file" accept=".xlsx" style="display:none" @change="onImportFile" />
      <input ref="txtImportInput" type="file" accept=".txt,.csv,.tsv" style="display:none" @change="onImportFile" />
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

  <!-- Barcode scanner: yashirin input, sahifa fokusda bo'lsa barkod o'qiydi -->
  <input
    ref="barcodeInput"
    style="position:fixed; opacity:0; pointer-events:none; width:1px; height:1px; top:0; left:0"
    @keydown="onBarcodeKey"
    @blur="() => $nextTick(() => { if (!['INPUT','TEXTAREA','SELECT'].includes(document.activeElement?.tagName)) barcodeInput?.focus() })"
  />

  <!-- Umumiy qiymatlar: filtrlangan ro'yxat bo'yicha -->
  <div class="stats-strip">
    <div class="stat-cell">
      <div class="stat-label">Umumiy tannarx</div>
      <div class="stat-val">{{ fmtSum(summary.cost) }} <span class="stat-unit">so'm</span></div>
    </div>
    <div class="stat-cell">
      <div class="stat-label">Sotuv qiymati</div>
      <div class="stat-val">{{ fmtSum(summary.value) }} <span class="stat-unit">so'm</span></div>
    </div>
    <div class="stat-cell stat-profit">
      <div class="stat-label">Kutilayotgan foyda</div>
      <div class="stat-val">{{ fmtSum(summary.profit) }} <span class="stat-unit">so'm</span></div>
    </div>
  </div>

  <div class="card flush">
  <!-- Filters -->
    <div class="card-toolbar">
      <div style="position:relative; flex:2; min-width:180px">
        <input
          v-model="searchName"
          class="input"
          placeholder="Nomi yoki barkod bo'yicha qidirish"
          style="padding-left:34px; width:100%; box-sizing:border-box"
        />
        <svg style="position:absolute; left:10px; top:50%; transform:translateY(-50%); color:#aaa; pointer-events:none" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
          <circle cx="11" cy="11" r="7"/><line x1="16.5" y1="16.5" x2="22" y2="22"/>
        </svg>
      </div>
      <select v-model="filterCategory" class="select" style="flex:1; min-width:150px">
        <option value="">Barcha kategoriyalar</option>
        <option v-for="c in categoryOptions" :key="c.id" :value="c.id">{{ c.label }}</option>
      </select>
      <select v-model="filterMxik" class="select" style="width:190px" title="MXIK holati">
        <option value="">MXIK: hammasi</option>
        <option value="check">MXIK: yo'q / yaroqsiz</option>
        <option value="suggest">MXIK: taklif bor</option>
        <option value="none">MXIK: taklif ham yo'q</option>
      </select>
      <button v-if="filterMxik === 'suggest' && filteredProducts.length" type="button" class="btn btn-sm"
        :disabled="applyingMxik" @click="applyMxikSuggestions">
        {{ applyingMxik ? '...' : `Takliflarni qo'llash (${filteredProducts.length})` }}
      </button>
      <select v-model="filterImage" class="select" style="width:140px" title="Mahsulot rasmi">
        <option value="">Rasm: hammasi</option>
        <option value="yes">Rasm: bor</option>
        <option value="no">Rasm: yo'q</option>
      </select>
      <select v-model="filterUnit" class="select" style="width:130px">
        <option value="">Barcha birlik</option>
        <option value="Kilogram">kg</option>
        <option value="Piece">dona</option>
      </select>
      <select v-model="filterOnline" class="select" style="width:150px" title="Mobil ilovada ko'rinishi">
        <option value="">Ilovada: hammasi</option>
        <option value="yes">Ilovada: ha</option>
        <option value="no">Ilovada: yo'q</option>
      </select>
      <select v-if="stores.length" v-model="filterBranch" class="select" style="flex:1; min-width:140px">
        <option value="">Barcha do'konlar</option>
        <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
      </select>
      <select v-model="filterQtyOp" class="select" style="width:130px">
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
        style="width:110px"
        :disabled="!filterQtyOp"
      />
      <button class="btn btn-ghost btn-sm icon-sq" title="Yangilash" :disabled="loading" @click="loadAll" style="flex-shrink:0">
        <svg :style="loading ? 'animation:spin 0.8s linear infinite' : ''" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
          <path d="M1 4v6h6M23 20v-6h-6"/><path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4-4.64 4.36A9 9 0 0 1 3.51 15"/>
        </svg>
      </button>
    </div>
    <!-- Top bar: info + page size -->
    <div class="card-meta">
      <div>
        <template v-if="filteredProducts.length">
          {{ filteredProducts.length }} ta mahsulotdan
          {{ (currentPage - 1) * pageSize + 1 }}–{{ Math.min(currentPage * pageSize, filteredProducts.length) }}
          ko'rsatilmoqda
        </template>
        <template v-else>Natija yo'q</template>
      </div>
      <div style="display:flex; align-items:center; gap:8px">
        <span>Sahifada</span>
        <select v-model.number="pageSize" class="select" style="width:76px; min-height:30px">
          <option :value="25">25</option>
          <option :value="50">50</option>
          <option :value="100">100</option>
        </select>
      </div>
    </div>

    <!-- Ommaviy amallar: tanlov bo'sh bo'lmasa -->
    <div v-if="selectedIds.size" class="bulk-bar">
      <span>Tanlandi: <b>{{ selectedIds.size }}</b></span>
      <button class="btn btn-sm" :disabled="bulkBusy" @click="bulkShowOnline(true)"><AppIcon name="phone" :size="15" />Ilovada ko'rsatish</button>
      <button class="btn btn-ghost btn-sm" :disabled="bulkBusy" @click="bulkShowOnline(false)">Ilovadan yashirish</button>
      <button class="btn btn-ghost btn-sm" :disabled="bulkBusy" @click="clearSelection">Tanlovni bekor qilish</button>
    </div>
    <div v-else-if="bulkMsg" class="bulk-bar bulk-ok">{{ bulkMsg }}</div>

    <table class="table">
      <thead>
        <tr>
          <th style="width:32px">
            <input type="checkbox" :checked="allOnPageSelected" title="Sahifadagilarni tanlash" @change="toggleSelectPage" />
          </th>
          <th style="width:52px"></th>
          <th>Nomi</th>
          <th>Kategoriya</th>
          <th v-if="stores.length">Do'kon</th>
          <th>Barcode</th>
          <th>PLU</th>
          <th class="text-right">Soni</th>
          <th class="text-right">Tannarx</th>
          <th class="text-right">Ustama</th>
          <th class="text-right">Narx</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td :colspan="stores.length ? 12 : 11" class="muted" style="text-align:center; padding:32px">Yuklanmoqda...</td>
        </tr>
        <tr
          v-for="p in paginatedProducts"
          :key="p.id"
          :id="`product-row-${p.id}`"
          :style="barcodeHighlight === p.id ? 'background:var(--primary-soft); transition:background 0.3s' : ''"
        >
          <td><input type="checkbox" :checked="selectedIds.has(p.id)" @change="toggleSelect(p.id)" /></td>
          <td><div class="thumb"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></div></td>
          <td>
            <div style="font-weight:500">
              {{ p.name }}
              <span v-if="p.showOnline !== false" class="online-mark" title="Mobil ilovada ko'rinadi"><AppIcon name="phone" :size="13" /></span>
            </div>
            <div v-if="p.mxikCode && p.mxikCode !== '00000000'" class="muted" style="font-size:12px">MXIK: {{ p.mxikCode }}</div>
            <span v-if="!mxikValid(p.mxikCode)" class="pill pill-red" style="margin-top:2px; display:inline-block">MXIK yo'q</span>
            <span v-if="!mxikValid(p.mxikCode) && p.mxikSuggestion" class="pill pill-amber" style="margin-top:2px; margin-left:4px; display:inline-block" title="Taxminiy — formada tekshirib qo'llang">taklif: {{ p.mxikSuggestion }}</span>
            <span v-if="p.isWeighed" class="pill pill-amber" style="margin-top:2px; display:inline-block">tarozi</span>
          </td>
          <td>{{ categoryName(p.categoryId) }}</td>
          <td v-if="stores.length" class="muted" style="font-size:13px">{{ p.branchName || '—' }}</td>
          <td class="muted" style="font-size:13px">{{ p.barcode || '—' }}</td>
          <td class="muted" style="font-size:13px">{{ p.pluNumber ?? '—' }}</td>
          <td class="text-right" :style="{ color: Number(p.quantity) <= 0 ? 'var(--danger)' : 'inherit' }">
            {{ Number(p.quantity).toLocaleString() }}
          </td>
          <td class="text-right muted">{{ Number(p.costPrice).toLocaleString() }}</td>
          <td class="text-right"><span class="pill pill-gray">{{ Number(p.markupPercent).toFixed(1) }}%</span></td>
          <td class="text-right" style="font-weight:500">{{ Number(p.price).toLocaleString() }}</td>
          <td class="text-right" style="white-space:nowrap">
            <div class="row-actions">
              <button class="btn btn-icon" title="Barcode chop etish" @click="printBarcode(p)"><AppIcon name="printer" :size="15" /></button>
              <button class="btn btn-icon" title="Kirim qilish (qoldiq qo'shish)" @click="openRestock(p)"><AppIcon name="plus" :size="15" /></button>
              <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(p)"><AppIcon name="edit" :size="15" /></button>
              <button class="btn btn-icon danger" title="O'chirish" @click="removeProduct(p)"><AppIcon name="trash" :size="15" /></button>
            </div>
          </td>
        </tr>
        <tr v-if="!paginatedProducts.length && !loading">
          <td :colspan="stores.length ? 12 : 11" class="muted" style="text-align:center; padding:32px">Mahsulot yo'q</td>
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
  <div v-if="showModal" class="modal-overlay">
    <div class="modal-box pm-box">
      <div class="modal-head">
        <h3>
          {{ editId ? 'Mahsulotni tahrirlash' : 'Mahsulot qo\'shish' }}
          <span v-if="!editId && addedCount" class="added-badge">{{ addedCount }} ta qo'shildi</span>
        </h3>
        <button class="modal-x" @click="closeModal">×</button>
      </div>
      <form class="pm-form" @submit.prevent="submit">
        <div class="pm-body">
          <div v-if="error" class="alert alert-error">{{ error }}</div>
          <div v-if="modalSuccess" class="alert alert-success">{{ modalSuccess }}</div>
          <div v-if="duplicateProduct" class="dup-warn">
            <div>
              Bu mahsulot allaqachon mavjud: <strong>{{ duplicateProduct.name }}</strong>
              <span class="muted">
                (qoldiq: {{ Number(duplicateProduct.quantity).toLocaleString() }}<template v-if="duplicateProduct.branchName">, {{ duplicateProduct.branchName }}</template>)
              </span>
            </div>
            <div class="dup-actions">
              <button type="button" class="btn btn-sm" @click="restockDuplicate"><AppIcon name="plus" :size="14" />Kirim qilish</button>
              <button type="button" class="btn btn-ghost btn-sm" @click="editDuplicate">Ochish</button>
            </div>
          </div>

          <div class="pm-grid">
            <!-- ===== Chap ustun: asosiy ma'lumotlar ===== -->
            <section class="pm-col">
              <div class="pm-title">Asosiy ma'lumotlar</div>

              <!-- Rasm | Yetkazib beruvchi + Kategoriya -->
              <div style="display:flex; gap:12px; margin-bottom:12px">
                <label class="img-upload-box" style="cursor:pointer; position:relative">
                  <img v-if="form.imageUrl" :src="mediaUrl(form.imageUrl)" alt="" style="width:100%;height:100%;object-fit:cover;border-radius:8px" />
                  <template v-else>
                    <svg width="26" height="26" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24" style="color:#cbd5e1">
                      <rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21,15 16,10 5,21"/>
                    </svg>
                    <span style="margin-top:6px; font-size:11px; line-height:1.3">Rasm<br/>qo'shish</span>
                  </template>
                  <span v-if="uploading" style="position:absolute;bottom:4px;font-size:10px;color:var(--muted)">yuklanyapti...</span>
                  <input type="file" accept="image/*" style="display:none" @change="onImageChange" />
                </label>
                <div style="flex:1; min-width:0; display:flex; flex-direction:column; gap:10px">
                  <div class="field" style="margin:0">
                    <label>Kategoriya <span class="req-star">*</span></label>
                    <select v-model="form.categoryId" class="select" required>
                      <option value="" disabled>Kategoriya tanlang</option>
                      <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
                    </select>
                  </div>
                  <div class="field" style="margin:0">
                    <label>Yetkazib beruvchi</label>
                    <select v-model="form.supplierId" class="select">
                      <option value="">Yetkazib beruvchini tanlang</option>
                      <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
                    </select>
                  </div>
                </div>
              </div>

              <div class="field">
                <label>Nomi <span class="req-star">*</span></label>
                <input ref="nameInput" v-model="form.name" class="input" required />
              </div>
              <div class="field">
                <label>Tavsif</label>
                <input v-model="form.description" class="input" />
              </div>

              <!-- Shtrix-kod | PLU | Birlik -->
              <div style="display:flex; gap:8px; margin-bottom:12px">
                <div class="field" style="flex:3; min-width:0; margin:0">
                  <label>Shtrix-kod</label>
                  <div style="display:flex; gap:6px">
                    <input v-model="form.barcode" class="input" style="flex:1; min-width:0" />
                    <button type="button" class="btn btn-ghost icon-sq" title="Generatsiya" @click="genBarcode">
                      <svg width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <path d="M1 4v6h6M23 20v-6h-6"/><path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4-4.64 4.36A9 9 0 0 1 3.51 15"/>
                      </svg>
                    </button>
                  </div>
                </div>
                <div class="field" style="flex:2; min-width:0; margin:0">
                  <label>PLU <span v-if="form.unit === 'Kilogram'" class="req-star">*</span></label>
                  <div style="display:flex; gap:6px">
                    <input
                      v-model="form.pluNumber"
                      type="number"
                      min="0"
                      class="input"
                      style="flex:1; min-width:0"
                      :disabled="form.unit === 'Piece'"
                      :style="form.unit === 'Piece' ? 'background:var(--bg); color:var(--muted)' : ''"
                    />
                    <button
                      type="button"
                      class="btn btn-ghost icon-sq"
                      title="Keyingisi"
                      :disabled="form.unit === 'Piece'"
                      @click="genPlu"
                    >
                      <svg width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <circle cx="11" cy="11" r="7"/><line x1="16.5" y1="16.5" x2="22" y2="22"/>
                      </svg>
                    </button>
                  </div>
                </div>
                <div class="field" style="flex:0 0 90px; margin:0">
                  <label>Birlik</label>
                  <select v-model="form.unit" class="select" @change="onUnitChange">
                    <option value="Piece">Dona</option>
                    <option value="Kilogram">Kg</option>
                  </select>
                </div>
              </div>

              <!-- Tarozi + Tara -->
              <div class="pm-inline">
                <div class="checkbox" style="margin:0">
                  <input id="weighed" v-model="form.isWeighed" type="checkbox" />
                  <label for="weighed" style="margin:0">Tarozida tortiladi</label>
                </div>
                <div v-if="form.isWeighed" class="pm-tara">
                  <label>Tara (kg)</label>
                  <input v-model="form.tare" type="number" min="0" step="0.001" class="input" />
                </div>
              </div>

              <!-- Toggles -->
              <div class="pm-toggles">
                <div class="toggle-col" title="Mobil ilovada (online do'konda) ko'rsatilsinmi">
                  <span>Ilovada</span>
                  <label class="ios-toggle">
                    <input v-model="form.showOnline" type="checkbox" />
                    <span class="ios-slider"></span>
                  </label>
                </div>
                <div class="toggle-col">
                  <span>НДС</span>
                  <label class="ios-toggle">
                    <input v-model="form.isVat" type="checkbox" />
                    <span class="ios-slider"></span>
                  </label>
                </div>
                <div class="toggle-col">
                  <span>Маркировка</span>
                  <label class="ios-toggle">
                    <input v-model="form.isMarked" type="checkbox" />
                    <span class="ios-slider"></span>
                  </label>
                </div>
              </div>
            </section>

            <!-- ===== O'ng ustun: narx, qoldiq, soliq ===== -->
            <section class="pm-col">
              <div class="pm-title">Narx va qoldiq</div>

              <div class="pm-row3">
                <div class="field">
                  <label>Tannarx <span class="req-star">*</span></label>
                  <input v-model="form.costPrice" type="number" min="0" step="0.01" class="input" @input="recalcPrice" />
                </div>
                <div class="field">
                  <label>Ustama % <span class="req-star">*</span></label>
                  <input v-model="form.markupPercent" type="number" step="any" class="input" @input="recalcPrice" />
                </div>
                <div class="field">
                  <label>Sotish narxi <span class="req-star">*</span></label>
                  <input v-model="form.price" type="number" min="0" step="0.01" class="input" required @input="recalcMarkup" />
                </div>
              </div>

              <div class="pm-row2">
                <div class="field">
                  <template v-if="!editId">
                    <label>Boshlang'ich soni</label>
                    <input v-model="form.quantity" type="number" min="0" step="0.001" class="input" />
                  </template>
                  <template v-else>
                    <label>Qoldiq</label>
                    <input :value="form.quantity" type="number" class="input" disabled
                           title="Qoldiq kirim / hisobdan chiqarish / inventarizatsiya orqali o'zgaradi" />
                  </template>
                </div>
                <!-- Do'kon (faqat bir nechta do'kon bo'lsa ko'rinadi) -->
                <div v-if="stores.length > 1" class="field">
                  <label>Do'kon <span v-if="!editId && Number(form.quantity) > 0" class="req-star">*</span></label>
                  <select v-model="form.branchId" class="select">
                    <option value="">— Umumiy —</option>
                    <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
                  </select>
                </div>
              </div>
              <template v-if="editId">
                <div class="pm-row2">
                  <div class="field">
                    <label>Kirim soni (+)</label>
                    <input v-model="form.addQty" type="number" min="0" step="0.001" class="input" placeholder="0" />
                  </div>
                  <div class="field">
                    <label>Kirim tannarxi</label>
                    <input v-model="form.addCost" type="number" min="0" step="0.01" class="input" />
                  </div>
                </div>
                <div class="muted" style="font-size:12px; margin:-6px 0 12px">
                  <template v-if="Number(form.addQty) > 0">
                    Yangi qoldiq: <strong>{{ (Number(form.quantity) + Number(form.addQty)).toLocaleString() }}</strong>.
                    Kirim hujjati yaratiladi (yetkazib beruvchi majburiy).
                  </template>
                  <template v-else>Qo'shimcha tovar kelgan bo'lsa, sonini kiriting. Kirim hujjati avtomatik yaratiladi.</template>
                </div>
              </template>

              <div class="pm-title" style="margin-top:4px">Soliq va markirovka</div>

              <div class="pm-row2">
                <div class="field">
                  <label>MXIK (ИКПУ)</label>
                  <input v-model="form.mxikCode" class="input" placeholder="Soliq uchun majburiy" />
                </div>
                <div class="field">
                  <label>Komitent STIR / JSHSHIR</label>
                  <input v-model="form.commitentTin" class="input" inputmode="numeric" maxlength="14"
                         placeholder="9 yoki 14 raqam" />
                </div>
              </div>
              <div v-if="form.mxikSuggestion && !mxikValid(form.mxikCode)" class="mxik-suggest" style="margin:-4px 0 12px">
                Taxminiy MXIK taklifi: <strong>{{ form.mxikSuggestion }}</strong>
                <span class="muted">({{ form.mxikSuggestionSource || 'avto' }})</span>
                <button type="button" class="btn btn-sm" style="margin-left:8px" @click="form.mxikCode = form.mxikSuggestion">Qo'llash</button>
                <div class="muted" style="margin-top:4px">Taklif chekka yozilmaydi — tasnif.soliq.uz da tekshirib, qo'llang va saqlang.</div>
              </div>

              <div class="pm-row2">
                <div class="field">
                  <label>Qadoq kodi (Код на упаковке)</label>
                  <input v-model="form.packageCode" class="input" placeholder="Ixtiyoriy" />
                </div>
                <div class="field">
                  <label>Markirovka kodi (Код маркировки)</label>
                  <input v-model="form.markingCode" class="input" placeholder="Ixtiyoriy" />
                </div>
              </div>
            </section>
          </div>
        </div>

        <div class="pm-foot">
          <button
            v-if="!editId"
            type="button"
            class="btn btn-ghost"
            :disabled="saving"
            @click="closeModal"
          >
            Yopish
          </button>
          <button class="btn" :disabled="saving || uploading">
            {{ saving ? 'Saqlanmoqda...' : (editId ? 'Saqlash' : 'Mahsulot qo\'shish') }}
          </button>
        </div>
      </form>
    </div>
  </div>

  <!-- Tez kirim modali -->
  <div v-if="restockTarget" class="modal-overlay" @click.self="closeRestock">
    <div class="modal-box" style="max-width:440px">
      <div class="modal-head">
        <h3>Kirim qilish</h3>
        <button class="modal-x" @click="closeRestock">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submitRestock">
        <div v-if="restockError" class="alert alert-error">{{ restockError }}</div>
        <div style="margin-bottom:12px">
          <strong>{{ restockTarget.name }}</strong>
          <div class="muted" style="font-size:13px">
            Hozirgi qoldiq: {{ Number(restockTarget.quantity).toLocaleString() }}
            {{ restockTarget.unit === 'Kilogram' ? 'kg' : 'dona' }}
            <template v-if="Number(restock.quantity) > 0">
              → <strong>{{ (Number(restockTarget.quantity) + Number(restock.quantity)).toLocaleString() }}</strong>
            </template>
          </div>
        </div>
        <div class="pm-row2">
          <div class="field">
            <label>Soni <span class="req-star">*</span></label>
            <input id="restock-qty" v-model="restock.quantity" type="number" min="0.001" step="0.001" class="input" required />
          </div>
          <div class="field">
            <label>Tannarx</label>
            <input v-model="restock.costPrice" type="number" min="0" step="0.01" class="input" />
          </div>
        </div>
        <div class="field">
          <label>Yetkazib beruvchi <span class="req-star">*</span></label>
          <div style="display:flex; gap:8px">
            <select v-model="restock.supplierId" class="select" style="flex:1" required>
              <option value="" disabled>Yetkazib beruvchini tanlang</option>
              <option v-for="s in activeSuppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
            <button type="button" class="btn btn-ghost" style="white-space:nowrap" @click="toggleNewSupplier">
              {{ newSupplierOpen ? 'Yopish' : '+ Yangi' }}
            </button>
          </div>
          <div v-if="newSupplierOpen" style="display:flex; gap:8px; margin-top:8px">
            <input id="new-supplier-name" v-model="newSupplier.name" class="input" style="flex:2" placeholder="Nomi *"
              @keydown.enter.prevent="saveNewSupplier" />
            <input v-model="newSupplier.phone" class="input" style="flex:1" placeholder="Telefon"
              @keydown.enter.prevent="saveNewSupplier" />
            <button type="button" class="btn" :disabled="newSupplierSaving" @click="saveNewSupplier">
              {{ newSupplierSaving ? '...' : 'Saqlash' }}
            </button>
          </div>
        </div>
        <div v-if="stores.length > 1" class="field">
          <label>Do'kon <span class="req-star">*</span></label>
          <select v-model="restock.branchId" class="select" required>
            <option value="" disabled>Do'kon tanlang</option>
            <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <div class="del-actions">
          <button type="button" class="btn btn-ghost" :disabled="restockSaving" @click="closeRestock">Bekor qilish</button>
          <button class="btn" :disabled="restockSaving">{{ restockSaving ? 'Saqlanmoqda...' : 'Kirim qilish' }}</button>
        </div>
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
        <div class="del-warn"><AppIcon name="alert" :size="40" /></div>
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

.dup-warn {
  display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap;
  padding: 10px 12px; margin-bottom: 12px; border-radius: 8px;
  background: var(--surface-2); border: 1px solid var(--border-strong); border-left: 3px solid var(--warning); font-size: 13px;
}
.dup-actions { display: flex; gap: 6px; }
/* Taxminiy MXIK taklifi (mahsulot formasida) */
.mxik-suggest {
  margin-top: 6px;
  padding: 8px 10px;
  border-radius: 8px;
  background: var(--warning-soft);
  font-size: 13px;
}

/* Ommaviy amallar paneli */
.bulk-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  padding: 8px 16px;
  border-bottom: 1px solid var(--border);
  background: var(--primary-soft);
  font-size: 13px;
}
.bulk-ok { color: var(--success, #0b6b45); }
.online-mark { display: inline-flex; vertical-align: -2px; margin-left: 4px; color: var(--muted-2); }

/* Umumiy qiymatlar — bitta chiziq, bo'linmalar bilan */
.stats-strip {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-bottom: 16px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
}
.stat-cell { padding: 14px 20px; min-width: 0; }
.stat-cell + .stat-cell { border-left: 1px solid var(--border); }
.stat-label { font-size: 13px; color: var(--muted); margin-bottom: 4px; }
.stat-val {
  font-size: 24px;
  font-weight: 600;
  letter-spacing: -0.02em;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.stat-unit { font-size: 13px; font-weight: 400; color: var(--muted); letter-spacing: 0; }
.stat-profit .stat-val { color: var(--accent); }
@media (max-width: 900px) {
  .stats-strip { grid-template-columns: 1fr; }
  .stat-cell + .stat-cell { border-left: none; border-top: 1px solid var(--border); }
  .stat-val { font-size: 20px; }
}

.btn-print {
  background: #16a34a;
  color: #fff;
  border: none;
  border-radius: 6px;
  padding: 0;
  width: 32px;
  height: 32px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.15s;
  vertical-align: middle;
}
.btn-print:hover { background: #15803d; }
.btn-print:active { background: #166534; }

.pagination-row {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 4px;
  padding: 12px 16px;
  border-top: 1px solid var(--border);
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

.btn-add-inline {
  flex-shrink: 0;
  width: 36px;
  height: 36px;
  border: 1px solid var(--border);
  background: var(--bg-card);
  border-radius: 8px;
  font-size: 20px;
  line-height: 1;
  cursor: pointer;
  color: var(--primary);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s, color 0.15s;
}
.btn-add-inline:hover,
.btn-add-inline.active {
  background: var(--primary);
  color: #fff;
  border-color: var(--primary);
}

.inline-add-box {
  margin-top: 8px;
  padding: 12px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
}

/* Rasm yuklash tugmasi */
.img-upload-box {
  width: 96px;
  height: 96px;
  flex-shrink: 0;
  border: 2px dashed var(--border);
  border-radius: 10px;
  background: var(--bg);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  font-size: 11px;
  color: var(--muted);
  overflow: hidden;
  transition: border-color 0.15s;
}
.img-upload-box:hover { border-color: var(--primary); }

/* Kvadrat icon tugma (barcode/PLU) */
.icon-sq {
  flex-shrink: 0;
  width: 36px;
  height: 36px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Majburiy yulduzcha */
.req-star { color: var(--danger); }
.added-badge {
  margin-left: 8px;
  font-size: 12px;
  font-weight: 600;
  color: var(--success, #16a34a);
  background: rgba(22, 163, 74, 0.12);
  padding: 2px 8px;
  border-radius: 999px;
  vertical-align: middle;
}

/* iOS toggle */
.ios-toggle {
  position: relative;
  display: inline-block;
  width: 46px;
  height: 26px;
  cursor: pointer;
}
.ios-toggle input { display: none; }
.ios-slider {
  position: absolute;
  inset: 0;
  border-radius: 999px;
  background: #cbd5e1;
  transition: background 0.2s;
}
.ios-slider::before {
  content: '';
  position: absolute;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #fff;
  top: 3px;
  left: 3px;
  transition: transform 0.2s;
  box-shadow: 0 1px 3px rgba(0,0,0,.2);
}
.ios-toggle input:checked + .ios-slider { background: var(--primary); }
.ios-toggle input:checked + .ios-slider::before { transform: translateX(20px); }

@keyframes spin { to { transform: rotate(360deg); } }

/* Toggle ustun (label + switch) */
.toggle-col {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 600;
  color: var(--muted);
}

/* Mahsulot modali — keng, 2 ustunli, scrollsiz */
.pm-box { max-width: 1120px; }
.pm-form { display: flex; flex-direction: column; max-height: calc(100vh - 140px); }
.pm-body { padding: 18px 22px 6px; overflow-y: auto; }
.pm-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 28px; }
.pm-col + .pm-col { padding-left: 28px; border-left: 1px solid var(--border); }
.pm-title { font-size: 14px; font-weight: 600; color: var(--text); margin-bottom: 12px; }
.pm-row2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.pm-row3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px; }
.pm-inline { display: flex; align-items: center; gap: 16px; min-height: 42px; margin-bottom: 12px; }
.pm-tara { display: flex; align-items: center; gap: 8px; }
.pm-tara label { font-size: 13px; font-weight: 600; color: var(--muted); white-space: nowrap; }
.pm-tara .input { width: 110px; }
.pm-toggles { display: flex; gap: 24px; padding-top: 12px; border-top: 1px solid var(--border); }
.pm-foot { display: flex; justify-content: flex-end; gap: 10px; padding: 14px 22px; border-top: 1px solid var(--border); background: #fafbfc; }
.pm-foot .btn { min-width: 160px; }
@media (max-width: 900px) {
  .pm-grid { grid-template-columns: 1fr; gap: 8px; }
  .pm-col + .pm-col { padding-left: 0; border-left: none; padding-top: 12px; border-top: 1px solid var(--border); }
  .pm-row3 { grid-template-columns: 1fr 1fr; }
  .pm-row2 { grid-template-columns: 1fr; }
  .pm-foot .btn { flex: 1; min-width: 0; }
}
</style>
