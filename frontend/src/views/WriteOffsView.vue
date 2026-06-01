<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { writeOffsApi, productsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const writeOffs = ref([])
const products = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)

const showForm = ref(false)
const form = reactive({ reason: '' })
const lines = ref([]) // { product, quantity }
const productSearch = ref('')
const expanded = ref(null)

const searchResults = computed(() => {
  const q = productSearch.value.trim().toLowerCase()
  if (!q) return []
  return products.value
    .filter((p) => p.name.toLowerCase().includes(q) || (p.barcode && p.barcode.toLowerCase().includes(q)))
    .slice(0, 8)
})
const total = computed(() =>
  lines.value.reduce((s, l) => s + Number(l.quantity || 0) * Number(l.product.costPrice || 0), 0)
)

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

function addLine(p) {
  if (lines.value.find((l) => l.product.id === p.id)) { productSearch.value = ''; return }
  lines.value.push({ product: p, quantity: 1 })
  productSearch.value = ''
}
function removeLine(id) { lines.value = lines.value.filter((l) => l.product.id !== id) }

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [w, p] = await Promise.all([writeOffsApi.list(), productsApi.list()])
    writeOffs.value = w
    products.value = p
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function submit() {
  err.value = ''
  success.value = ''
  if (!lines.value.length) { err.value = 'Tovar qo\'shing'; return }
  saving.value = true
  try {
    const w = await writeOffsApi.create({
      reason: form.reason.trim() || null,
      items: lines.value.map((l) => ({ productId: l.product.id, quantity: Number(l.quantity) }))
    })
    success.value = `${w.number} — ${lines.value.length} ta tovar hisobdan chiqarildi`
    form.reason = ''
    lines.value = []
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Hisobdan chiqarish (списание)</h1>
    <button class="btn" @click="showForm = !showForm">+ Yangi</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="submit">
    <h3 style="margin-top:0">Yangi hisobdan chiqarish</h3>
    <div class="field" style="max-width:480px">
      <label>Sabab</label>
      <input v-model="form.reason" class="input" placeholder="Muddati o'tgan, buzilgan..." />
    </div>

    <div class="field" style="position:relative; max-width:480px">
      <label>Tovar qo'shish</label>
      <input v-model="productSearch" class="input" placeholder="Qidirish (nom/shtrix-kod)..." />
      <div v-if="searchResults.length" class="search-pop">
        <div v-for="p in searchResults" :key="p.id" class="search-item" @click="addLine(p)">
          {{ p.name }} <span class="muted">— qoldiq {{ fmt(p.quantity) }}</span>
        </div>
      </div>
    </div>

    <table class="table" style="margin:12px 0">
      <thead><tr><th>Tovar</th><th style="width:120px">Miqdor</th><th class="text-right">Tannarx summa</th><th></th></tr></thead>
      <tbody>
        <tr v-for="l in lines" :key="l.product.id">
          <td>{{ l.product.name }} <span class="muted">(qoldiq {{ fmt(l.product.quantity) }})</span></td>
          <td><input v-model="l.quantity" type="number" min="0" step="0.001" class="input" /></td>
          <td class="text-right">{{ fmt(Number(l.quantity) * Number(l.product.costPrice)) }}</td>
          <td class="text-right"><button type="button" class="btn btn-ghost btn-sm" @click="removeLine(l.product.id)">✕</button></td>
        </tr>
        <tr v-if="!lines.length"><td colspan="4" class="muted">Tovar qo'shilmagan</td></tr>
      </tbody>
    </table>

    <div class="row" style="justify-content:space-between; align-items:center">
      <strong style="font-size:16px">Jami tannarx: {{ fmt(total) }} so'm</strong>
      <button class="btn btn-danger" :disabled="saving">{{ saving ? '...' : 'Hisobdan chiqarish' }}</button>
    </div>
  </form>

  <div class="card">
    <table class="table">
      <thead><tr><th>№</th><th>Sabab</th><th>Sana</th><th class="text-right">Tannarx</th></tr></thead>
      <tbody>
        <template v-for="w in writeOffs" :key="w.id">
          <tr style="cursor:pointer" @click="expanded = expanded === w.id ? null : w.id">
            <td><strong>{{ w.number }}</strong></td>
            <td class="muted">{{ w.reason || '—' }}</td>
            <td class="muted">{{ fmtDate(w.createdAt) }}</td>
            <td class="text-right" style="color:var(--danger)">− {{ fmt(w.totalCost) }}</td>
          </tr>
          <tr v-if="expanded === w.id">
            <td colspan="4" style="background:#fafbfc">
              <table class="table">
                <tbody>
                  <tr v-for="(i, idx) in w.items" :key="idx">
                    <td>{{ i.productName }}</td>
                    <td class="text-right">{{ fmt(i.quantity) }}</td>
                    <td class="text-right">{{ fmt(i.lineTotal) }}</td>
                  </tr>
                </tbody>
              </table>
            </td>
          </tr>
        </template>
        <tr v-if="!writeOffs.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Yozuv yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.search-pop { position:absolute; top:100%; left:0; right:0; z-index:5; background:#fff; border:1px solid var(--border); border-radius:8px; box-shadow:var(--shadow); max-height:260px; overflow:auto; }
.search-item { padding:9px 12px; cursor:pointer; }
.search-item:hover { background:var(--bg); }
</style>
