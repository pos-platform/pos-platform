<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { stockReceiptsApi, suppliersApi, productsApi, labelsApi, storesApi, newRequestId } from '../api/catalog'
import { errorMessage } from '../api/http'

const receipts = ref([])
const branches = ref([])
const suppliers = ref([])
const products = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)

const showForm = ref(false)
const form = reactive({ supplierId: '', comment: '', branchId: '' })
const lines = ref([]) // { product, quantity, costPrice }
const productSearch = ref('')
const expanded = ref(null)
// Ikki marta saqlanishdan himoya: bitta forma = bitta kalit (javob kelmay qayta bosilsa ham kirim bitta bo'ladi)
let requestId = newRequestId()
const cancellingId = ref(null)

const searchResults = computed(() => {
  const q = productSearch.value.trim().toLowerCase()
  if (!q) return []
  return products.value
    .filter((p) => p.name.toLowerCase().includes(q) || (p.barcode && p.barcode.toLowerCase().includes(q)))
    .slice(0, 8)
})

const total = computed(() =>
  lines.value.reduce((sum, l) => sum + Number(l.quantity || 0) * Number(l.costPrice || 0), 0)
)

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

const labelSuccess = ref('')

function addLine(product) {
  if (lines.value.find((l) => l.product.id === product.id)) {
    productSearch.value = ''
    return
  }
  lines.value.push({ product, quantity: 1, costPrice: Number(product.costPrice) || 0, batchNumber: '', expiryDate: '' })
  productSearch.value = ''
}
function removeLine(id) {
  lines.value = lines.value.filter((l) => l.product.id !== id)
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [r, s, p, b] = await Promise.all([stockReceiptsApi.list(), suppliersApi.list(), productsApi.list(), storesApi.list()])
    receipts.value = r
    branches.value = b
    if (!form.branchId && b.length) form.branchId = b[0].id
    suppliers.value = s.filter((x) => x.isActive)
    products.value = p
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function printLabels() {
  err.value = ''; labelSuccess.value = ''
  if (!lines.value.length) { err.value = 'Tovar qo\'shing'; return }
  try {
    await labelsApi.print({ items: lines.value.map(l => ({ productId: l.product.id, copies: 1 })) })
    labelSuccess.value = 'Yorliqlar chop etish so\'rovi yuborildi (agent printer\'ga chop etadi)'
  } catch (e) { err.value = errorMessage(e) }
}

async function submit() {
  err.value = ''
  success.value = ''
  if (!form.supplierId) { err.value = 'Yetkazib beruvchini tanlang'; return }
  if (!lines.value.length) { err.value = 'Kamida bitta tovar qo\'shing'; return }
  saving.value = true
  try {
    const r = await stockReceiptsApi.create({
      supplierId: form.supplierId,
      branchId: form.branchId || null,
      comment: form.comment.trim() || null,
      clientRequestId: requestId,
      items: lines.value.map((l) => ({
        productId: l.product.id,
        quantity: Number(l.quantity),
        costPrice: Number(l.costPrice),
        batchNumber: l.batchNumber || null,
        expiryDate: l.expiryDate || null
      }))
    })
    success.value = `Kirim ${r.number} saqlandi — ombor yangilandi`
    requestId = newRequestId()
    form.supplierId = ''
    form.comment = ''
    lines.value = []
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function cancelReceipt(r) {
  if (!confirm(`${r.number} kirimi bekor qilinsinmi?\n\nTovar ombordan ayiriladi va yetkazib beruvchi qarzi ${fmt(r.totalAmount)} so'mga kamayadi.`)) return
  err.value = ''
  success.value = ''
  cancellingId.value = r.id
  try {
    await stockReceiptsApi.cancel(r.id)
    success.value = `${r.number} kirimi bekor qilindi`
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    cancellingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>{{ $t('stockReceipts.title') }}</h1>
    <button class="btn" @click="showForm = !showForm">{{ $t('stockReceipts.newBtn') }}</button>
  </div>

  <div class="card hint-card">
    <strong>ℹ️ {{ $t('stockReceipts.hintTitle') }}</strong>
    <ul>
      <li>{{ $t('stockReceipts.hint1') }}</li>
      <li>{{ $t('stockReceipts.hint2') }}</li>
      <li>{{ $t('stockReceipts.hint3') }}</li>
      <li>{{ $t('stockReceipts.hint4') }}</li>
    </ul>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Yangi kirim formasi -->
  <form v-if="showForm" class="card" style="margin-bottom: 16px" @submit.prevent="submit">
    <h3 style="margin-top: 0">Yangi kirim</h3>
    <div v-if="branches.length > 1" class="field" style="max-width:280px">
      <label>Filial</label>
      <select v-model="form.branchId" class="input">
        <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
      </select>
    </div>
    <div class="row">
      <div class="field">
        <label>Yetkazib beruvchi *</label>
        <select v-model="form.supplierId" class="select" required>
          <option value="" disabled>Tanlang...</option>
          <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      <div class="field">
        <label>Izoh</label>
        <input v-model="form.comment" class="input" />
      </div>
    </div>

    <!-- Tovar qidirish -->
    <div class="field" style="position: relative; max-width: 480px">
      <label>Tovar qo'shish (nom yoki shtrix-kod)</label>
      <input v-model="productSearch" class="input" placeholder="Qidirish..." />
      <div v-if="searchResults.length" class="search-pop">
        <div v-for="p in searchResults" :key="p.id" class="search-item" @click="addLine(p)">
          {{ p.name }} <span class="muted">— qoldiq {{ fmt(p.quantity) }}, tannarx {{ fmt(p.costPrice) }}</span>
        </div>
      </div>
    </div>

    <table class="table" style="margin: 12px 0">
      <thead>
        <tr>
          <th>Tovar</th><th style="width:110px">Miqdor</th><th style="width:120px">Tannarx</th>
          <th style="width:130px">Partiya №</th><th style="width:130px">Muddati</th>
          <th class="text-right">Summa</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="l in lines" :key="l.product.id">
          <td>{{ l.product.name }}</td>
          <td><input v-model="l.quantity" type="number" min="0" step="0.001" class="input" /></td>
          <td><input v-model="l.costPrice" type="number" min="0" step="0.01" class="input" /></td>
          <td><input v-model="l.batchNumber" class="input" placeholder="Lot/seriya" /></td>
          <td><input v-model="l.expiryDate" type="date" class="input" /></td>
          <td class="text-right">{{ fmt(Number(l.quantity) * Number(l.costPrice)) }}</td>
          <td class="text-right"><button type="button" class="btn btn-ghost btn-sm" @click="removeLine(l.product.id)">✕</button></td>
        </tr>
        <tr v-if="!lines.length"><td colspan="7" class="muted">Tovar qo'shilmagan</td></tr>
      </tbody>
    </table>

    <div v-if="labelSuccess" class="alert alert-success" style="margin-bottom:8px">{{ labelSuccess }}</div>
    <div class="row" style="justify-content: space-between; align-items: center">
      <strong style="font-size: 16px">Jami: {{ fmt(total) }} so'm</strong>
      <div style="display:flex; gap:8px">
        <button type="button" class="btn btn-ghost" @click="printLabels">🏷️ Yorliq chop</button>
        <button class="btn btn-success" :disabled="saving">{{ saving ? 'Saqlanmoqda...' : 'Saqlash (omborga qo\'shish)' }}</button>
      </div>
    </div>
  </form>

  <!-- Kirimlar ro'yxati -->
  <div class="card">
    <table class="table">
      <thead>
        <tr><th>№</th><th>Yetkazib beruvchi</th><th>Sana</th><th class="text-right">Summa</th><th>Izoh</th><th></th></tr>
      </thead>
      <tbody>
        <template v-for="r in receipts" :key="r.id">
          <tr style="cursor:pointer" :class="{ cancelled: r.status === 'Cancelled' }" @click="expanded = expanded === r.id ? null : r.id">
            <td>
              <strong>{{ r.number }}</strong>
              <span v-if="r.status === 'Cancelled'" class="pill pill-red" style="margin-left:6px">Bekor qilingan</span>
            </td>
            <td>{{ r.supplierName }}</td>
            <td class="muted">{{ fmtDate(r.createdAt) }}</td>
            <td class="text-right amount">{{ fmt(r.totalAmount) }}</td>
            <td class="muted">{{ r.comment || '—' }}</td>
            <td class="text-right">
              <button v-if="r.status !== 'Cancelled'" type="button" class="btn btn-ghost btn-sm"
                :disabled="cancellingId === r.id" @click.stop="cancelReceipt(r)">
                {{ cancellingId === r.id ? '...' : 'Bekor qilish' }}
              </button>
            </td>
          </tr>
          <tr v-if="expanded === r.id">
            <td colspan="6" style="background:#fafbfc">
              <table class="table">
                <thead><tr><th>Tovar</th><th class="text-right">Miqdor</th><th class="text-right">Tannarx</th><th>Partiya</th><th>Muddati</th><th class="text-right">Summa</th></tr></thead>
                <tbody>
                  <tr v-for="(i, idx) in r.items" :key="idx">
                    <td>{{ i.productName }}</td>
                    <td class="text-right">{{ fmt(i.quantity) }}</td>
                    <td class="text-right">{{ fmt(i.costPrice) }}</td>
                    <td class="muted">{{ i.batchNumber || '—' }}</td>
                    <td class="muted">{{ i.expiryDate ? new Date(i.expiryDate).toLocaleDateString('uz-UZ') : '—' }}</td>
                    <td class="text-right">{{ fmt(i.lineTotal) }}</td>
                  </tr>
                </tbody>
              </table>
            </td>
          </tr>
        </template>
        <tr v-if="!receipts.length && !loading"><td colspan="6" class="muted" style="text-align:center;padding:24px">Kirim yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.hint-card { margin-bottom: 16px; background: #f0f7ff; border: 1px solid #cfe3ff; font-size: 14px; }
.hint-card ul { margin: 8px 0 0; padding-left: 20px; }
.hint-card li { margin: 4px 0; }
.search-pop {
  position: absolute; top: 100%; left: 0; right: 0; z-index: 5;
  background: #fff; border: 1px solid var(--border); border-radius: 8px;
  box-shadow: var(--shadow); max-height: 260px; overflow: auto;
}
.search-item { padding: 9px 12px; cursor: pointer; }
.search-item:hover { background: var(--bg); }
.cancelled td { color: var(--muted, #94a3b8); }
.cancelled .amount { text-decoration: line-through; }
</style>
