<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { kitchenIssuesApi, dishesApi, productsApi, storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const tab = ref('issues') // issues | report
const issues = ref([])
const dishes = ref([])
const goods = ref([])
const branches = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)
const expanded = ref(null)

// Yangi hujjat
const showForm = ref(false)
const form = reactive({ comment: '', branchId: '' })
const lines = ref([]) // { product, dishProductId, quantity }
const goodSearch = ref('')

// Retsept asosida to'ldirish
const portionDishId = ref('')
const portions = ref(1)

// Hisobot
const report = ref([])
const reportFrom = ref('')
const reportTo = ref('')

const goodResults = computed(() => {
  const q = goodSearch.value.trim().toLowerCase()
  if (!q) return []
  return goods.value.filter((p) => p.name.toLowerCase().includes(q)).slice(0, 8)
})
const total = computed(() =>
  lines.value.reduce((s, l) => s + Number(l.quantity || 0) * Number(l.product.costPrice || 0), 0)
)

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [i, d, p, s] = await Promise.all([kitchenIssuesApi.list(), dishesApi.list(), productsApi.list(), storesApi.list()])
    issues.value = i
    dishes.value = d
    goods.value = p.filter((x) => x.productType === 'Good')
    branches.value = s
    if (!form.branchId && s.length) form.branchId = (s.find((b) => b.businessType === 'Restaurant') || s[0]).id
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function addGood(p, dishProductId = null) {
  const existing = lines.value.find((l) => l.product.id === p.id && l.dishProductId === dishProductId)
  if (existing) { goodSearch.value = ''; return }
  lines.value.push({ product: p, dishProductId, quantity: 1 })
  goodSearch.value = ''
}
function removeLine(idx) { lines.value.splice(idx, 1) }

// Tanlangan ovqat retsepti bo'yicha N porsiya tovarlarni qatorlarga qo'shadi
async function fillFromRecipe() {
  if (!portionDishId.value) return
  err.value = ''
  try {
    const suggested = await kitchenIssuesApi.suggested(portionDishId.value, Number(portions.value) || 1)
    for (const s of suggested) {
      const good = goods.value.find((g) => g.id === s.ingredientProductId)
      if (!good) continue
      const line = lines.value.find((l) => l.product.id === good.id && l.dishProductId === portionDishId.value)
      if (line) line.quantity = Number(line.quantity) + s.quantity
      else lines.value.push({ product: good, dishProductId: portionDishId.value, quantity: s.quantity })
    }
  } catch (e) {
    err.value = errorMessage(e)
  }
}

function dishName(id) { return dishes.value.find((d) => d.id === id)?.name || '—' }

async function submit() {
  if (!lines.value.length) { err.value = 'Tovar qo\'shing'; return }
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const k = await kitchenIssuesApi.create({
      branchId: form.branchId || null,
      comment: form.comment.trim() || null,
      items: lines.value.map((l) => ({
        dishProductId: l.dishProductId || null,
        ingredientProductId: l.product.id,
        quantity: Number(l.quantity)
      }))
    })
    success.value = `${k.number} — ${lines.value.length} ta tovar oshpazga berildi`
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

async function loadReport() {
  err.value = ''
  try {
    report.value = await kitchenIssuesApi.consumption({
      from: reportFrom.value || undefined,
      to: reportTo.value || undefined
    })
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Ombordan oshpazga berish</h1>
    <button v-if="tab === 'issues'" class="btn" @click="showForm = !showForm">+ Yangi hujjat</button>
  </div>

  <div class="row" style="gap:8px; margin-bottom:12px">
    <button class="btn btn-ghost btn-sm" :class="{ 'btn-active': tab === 'issues' }" @click="tab = 'issues'">Hujjatlar</button>
    <button class="btn btn-ghost btn-sm" :class="{ 'btn-active': tab === 'report' }" @click="tab = 'report'; loadReport()">Sarf hisoboti</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Yangi hujjat -->
  <form v-if="showForm && tab === 'issues'" class="card" style="margin-bottom:16px" @submit.prevent="submit">
    <h3 style="margin-top:0">Yangi berish hujjati</h3>

    <div class="field" style="max-width:280px; margin-bottom:8px">
      <label>Filial</label>
      <select v-model="form.branchId" class="input">
        <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}{{ b.businessType === 'Restaurant' ? ' 🍽️' : '' }}</option>
      </select>
    </div>

    <div class="row" style="gap:12px; align-items:flex-end; flex-wrap:wrap; margin-bottom:8px">
      <div class="field"><label>Ovqat (retsept bo'yicha)</label>
        <select v-model="portionDishId" class="input"><option value="">— tanlang —</option><option v-for="d in dishes" :key="d.id" :value="d.id">{{ d.name }}</option></select>
      </div>
      <div class="field"><label>Porsiya</label><input v-model="portions" type="number" min="1" class="input" style="width:100px" /></div>
      <button type="button" class="btn btn-ghost" :disabled="!portionDishId" @click="fillFromRecipe">Retseptdan to'ldirish</button>
    </div>

    <div class="field" style="position:relative; max-width:480px">
      <label>Tovar qo'lda qo'shish</label>
      <input v-model="goodSearch" class="input" placeholder="Tovar qidirish..." />
      <div v-if="goodResults.length" class="search-pop">
        <div v-for="p in goodResults" :key="p.id" class="search-item" @click="addGood(p, portionDishId || null)">
          {{ p.name }} <span class="muted">— qoldiq {{ fmt(p.quantity) }}</span>
        </div>
      </div>
    </div>

    <table class="table" style="margin:12px 0">
      <thead><tr><th>Tovar</th><th>Ovqat uchun</th><th style="width:120px">Miqdor</th><th class="text-right">Tannarx</th><th></th></tr></thead>
      <tbody>
        <tr v-for="(l, idx) in lines" :key="idx">
          <td>{{ l.product.name }} <span class="muted">(qoldiq {{ fmt(l.product.quantity) }})</span></td>
          <td class="muted">{{ l.dishProductId ? dishName(l.dishProductId) : '—' }}</td>
          <td><input v-model="l.quantity" type="number" min="0" step="0.001" class="input" /></td>
          <td class="text-right">{{ fmt(Number(l.quantity) * Number(l.product.costPrice)) }}</td>
          <td class="text-right"><button type="button" class="btn btn-ghost btn-sm" @click="removeLine(idx)">✕</button></td>
        </tr>
        <tr v-if="!lines.length"><td colspan="5" class="muted">Tovar qo'shilmagan</td></tr>
      </tbody>
    </table>

    <div class="field" style="max-width:480px"><label>Izoh</label><input v-model="form.comment" class="input" /></div>

    <div class="row" style="justify-content:space-between; align-items:center; margin-top:8px">
      <strong style="font-size:16px">Jami tannarx: {{ fmt(total) }} so'm</strong>
      <button class="btn" :disabled="saving">{{ saving ? '...' : 'Oshpazga berish' }}</button>
    </div>
  </form>

  <!-- Hujjatlar ro'yxati -->
  <div v-if="tab === 'issues'" class="card">
    <table class="table">
      <thead><tr><th>№</th><th>Izoh</th><th>Sana</th><th class="text-right">Tannarx</th></tr></thead>
      <tbody>
        <template v-for="k in issues" :key="k.id">
          <tr style="cursor:pointer" @click="expanded = expanded === k.id ? null : k.id">
            <td><strong>{{ k.number }}</strong></td>
            <td class="muted">{{ k.comment || '—' }}</td>
            <td class="muted">{{ fmtDate(k.createdAt) }}</td>
            <td class="text-right">{{ fmt(k.totalCost) }}</td>
          </tr>
          <tr v-if="expanded === k.id">
            <td colspan="4" style="background:#fafbfc">
              <table class="table">
                <thead><tr><th>Tovar</th><th>Ovqat</th><th class="text-right">Miqdor</th><th class="text-right">Summa</th></tr></thead>
                <tbody>
                  <tr v-for="(i, idx) in k.items" :key="idx">
                    <td>{{ i.ingredientName }}</td>
                    <td class="muted">{{ i.dishName || '—' }}</td>
                    <td class="text-right">{{ fmt(i.quantity) }}</td>
                    <td class="text-right">{{ fmt(i.lineTotal) }}</td>
                  </tr>
                </tbody>
              </table>
            </td>
          </tr>
        </template>
        <tr v-if="!issues.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Hujjat yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <!-- Sarf hisoboti -->
  <div v-if="tab === 'report'" class="card">
    <div class="row" style="gap:12px; align-items:flex-end; margin-bottom:12px">
      <div class="field"><label>Dan</label><input v-model="reportFrom" type="date" class="input" /></div>
      <div class="field"><label>Gacha</label><input v-model="reportTo" type="date" class="input" /></div>
      <button class="btn btn-ghost" @click="loadReport">Yangilash</button>
    </div>
    <table class="table">
      <thead><tr><th>Ovqat</th><th>Tovar</th><th class="text-right">Jami miqdor</th><th class="text-right">Jami tannarx</th></tr></thead>
      <tbody>
        <tr v-for="(r, idx) in report" :key="idx">
          <td>{{ r.dishName || '—' }}</td>
          <td>{{ r.ingredientName }}</td>
          <td class="text-right">{{ fmt(r.totalQuantity) }}</td>
          <td class="text-right">{{ fmt(r.totalCost) }}</td>
        </tr>
        <tr v-if="!report.length"><td colspan="4" class="muted" style="text-align:center;padding:24px">Ma'lumot yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.search-pop { position:absolute; top:100%; left:0; right:0; z-index:5; background:#fff; border:1px solid var(--border); border-radius:8px; box-shadow:var(--shadow); max-height:260px; overflow:auto; }
.search-item { padding:9px 12px; cursor:pointer; }
.search-item:hover { background:var(--bg); }
.btn-active { background:var(--primary); color:#fff; }
</style>
