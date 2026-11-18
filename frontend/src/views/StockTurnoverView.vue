<script setup>
import { ref, computed, onMounted } from 'vue'
import { stockMovementsApi, storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// Tovar aylanmasi: har tovar bo'yicha boshlang'ich qoldiq → kirim / chiqim → oxirgi qoldiq.
// Qatorni bossangiz — shu tovarning qoldiq tarixi (har bir hujjat bilan).

function localDay(d = new Date()) {
  const z = new Date(d.getTime() - d.getTimezoneOffset() * 60000)
  return z.toISOString().slice(0, 10)
}
const today = localDay()
const monthStart = today.slice(0, 8) + '01'

const from = ref(monthStart)
const to = ref(today)
const branchId = ref('')
const search = ref('')
const includeIdle = ref(false)

const branches = ref([])
const report = ref(null)
const loading = ref(false)
const err = ref('')

const history = ref(null) // { product, rows }
const historyLoading = ref(false)

const TYPE_LABELS = {
  Initial: "Boshlang'ich qoldiq",
  Receipt: 'Kirim',
  Sale: 'Sotuv',
  Refund: 'Qaytarish',
  WriteOff: 'Hisobdan chiqarish',
  TransferOut: 'Transfer (jo\'natildi)',
  TransferIn: 'Transfer (qabul)',
  TransferCancel: 'Transfer bekor qilindi',
  Inventory: 'Inventarizatsiya',
  ReceiptCancel: 'Kirim bekor qilindi',
  WriteOffCancel: 'Hisobdan chiqarish bekor qilindi',
  OnlineReturn: 'Online — rad etilgan tovar qaytdi'
}

const rows = computed(() => report.value?.rows || [])
const totals = computed(() => {
  const t = { opening: 0, totalIn: 0, totalOut: 0, closing: 0 }
  for (const r of rows.value) {
    t.opening += r.opening; t.totalIn += r.totalIn; t.totalOut += r.totalOut; t.closing += r.closing
  }
  return t
})

function fmt(n) {
  const v = Number(n || 0)
  return v === 0 ? '—' : v.toLocaleString(undefined, { maximumFractionDigits: 3 })
}
function fmtSigned(n) {
  const v = Number(n || 0)
  if (v === 0) return '—'
  return (v > 0 ? '+' : '−') + Math.abs(v).toLocaleString(undefined, { maximumFractionDigits: 3 })
}
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    report.value = await stockMovementsApi.turnover({
      from: from.value || undefined,
      to: to.value || undefined,
      branchId: branchId.value || undefined,
      search: search.value.trim() || undefined,
      includeIdle: includeIdle.value || undefined
    })
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function openHistory(r) {
  history.value = { product: r, rows: [] }
  historyLoading.value = true
  try {
    history.value.rows = await stockMovementsApi.productHistory(r.productId, {
      branchId: branchId.value || undefined
    })
  } catch (e) {
    err.value = errorMessage(e)
    history.value = null
  } finally {
    historyLoading.value = false
  }
}

onMounted(async () => {
  try { branches.value = await storesApi.list() } catch { /* filial ro'yxati ixtiyoriy */ }
  await load()
})
</script>

<template>
  <div class="page-head">
    <h1>Tovar aylanmasi</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="card" style="margin-bottom:16px">
    <form class="filters" @submit.prevent="load">
      <div class="field"><label>Dan</label><input v-model="from" type="date" class="input" /></div>
      <div class="field"><label>Gacha</label><input v-model="to" type="date" class="input" /></div>
      <div v-if="branches.length > 1" class="field">
        <label>Filial</label>
        <select v-model="branchId" class="select">
          <option value="">Barcha filiallar</option>
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
      <div class="field" style="flex:1; min-width:180px">
        <label>Qidirish</label>
        <input v-model="search" class="input" placeholder="Nom yoki shtrix-kod" />
      </div>
      <label class="idle">
        <input v-model="includeIdle" type="checkbox" /> Harakatsiz tovarlar ham
      </label>
      <button class="btn" :disabled="loading">{{ loading ? '...' : 'Ko\'rsatish' }}</button>
    </form>
    <div class="muted" style="font-size:12px; margin-top:8px">
      Qatorni bosing — tovar qoldig'i tarixi (har bir hujjat bilan) ochiladi.
    </div>
  </div>

  <div class="card" style="overflow-x:auto">
    <table class="table turnover">
      <thead>
        <tr>
          <th>Tovar</th>
          <th class="text-right">Boshlang'ich</th>
          <th class="text-right in">Kirim</th>
          <th class="text-right in">Qaytarish</th>
          <th class="text-right in">Transfer +</th>
          <th class="text-right in">Boshqa +</th>
          <th class="text-right out">Sotuv</th>
          <th class="text-right out">Hisobdan chiq.</th>
          <th class="text-right out">Transfer −</th>
          <th class="text-right out">Boshqa −</th>
          <th class="text-right">Inventar.</th>
          <th class="text-right">Oxirgi</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.productId" class="clickable" @click="openHistory(r)">
          <td>
            {{ r.productName }}
            <div v-if="r.barcode" class="muted" style="font-size:12px">{{ r.barcode }}</div>
          </td>
          <td class="text-right">{{ fmt(r.opening) }}</td>
          <td class="text-right in">{{ fmt(r.receipt) }}</td>
          <td class="text-right in">{{ fmt(r.refund) }}</td>
          <td class="text-right in">{{ fmt(r.transferIn) }}</td>
          <td class="text-right in">{{ fmt(r.otherIn) }}</td>
          <td class="text-right out">{{ fmt(r.sale) }}</td>
          <td class="text-right out">{{ fmt(r.writeOff) }}</td>
          <td class="text-right out">{{ fmt(r.transferOut) }}</td>
          <td class="text-right out">{{ fmt(r.otherOut) }}</td>
          <td class="text-right">{{ fmtSigned(r.inventory) }}</td>
          <td class="text-right"><strong>{{ fmt(r.closing) }}</strong></td>
        </tr>
        <tr v-if="!rows.length && !loading">
          <td colspan="12" class="muted" style="text-align:center;padding:24px">Bu davrda ombor harakati yo'q</td>
        </tr>
      </tbody>
      <tfoot v-if="rows.length">
        <tr>
          <td><strong>Jami ({{ rows.length }} ta tovar)</strong></td>
          <td class="text-right"><strong>{{ fmt(totals.opening) }}</strong></td>
          <td colspan="4" class="text-right in"><strong>+{{ fmt(totals.totalIn) }}</strong></td>
          <td colspan="4" class="text-right out"><strong>−{{ fmt(totals.totalOut) }}</strong></td>
          <td></td>
          <td class="text-right"><strong>{{ fmt(totals.closing) }}</strong></td>
        </tr>
      </tfoot>
    </table>
  </div>

  <!-- Tovar qoldig'i tarixi -->
  <div v-if="history" class="modal-overlay" @click.self="history = null">
    <div class="modal-box" style="max-width:900px">
      <div class="modal-head">
        <h3>{{ history.product.productName }} — qoldiq tarixi</h3>
        <button class="modal-x" @click="history = null">×</button>
      </div>
      <div class="modal-body">
        <div v-if="historyLoading" class="muted">Yuklanmoqda...</div>
        <table v-else class="table">
          <thead>
            <tr>
              <th>Sana</th><th>Amal</th><th>Hujjat</th>
              <th v-if="!branchId && branches.length > 1">Filial</th>
              <th class="text-right">O'zgarish</th><th class="text-right">Qoldiq</th><th>Kim</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in history.rows" :key="m.id">
              <td class="muted">{{ fmtDate(m.createdAt) }}</td>
              <td>{{ TYPE_LABELS[m.type] || m.type }}</td>
              <td>{{ m.documentNumber || '—' }}</td>
              <td v-if="!branchId && branches.length > 1" class="muted">{{ m.branchName }}</td>
              <td class="text-right" :class="m.quantity > 0 ? 'in' : 'out'"><strong>{{ fmtSigned(m.quantity) }}</strong></td>
              <td class="text-right">{{ Number(m.balanceAfter).toLocaleString(undefined, { maximumFractionDigits: 3 }) }}</td>
              <td class="muted">{{ m.userName || '—' }}</td>
            </tr>
            <tr v-if="!history.rows.length">
              <td colspan="7" class="muted" style="text-align:center;padding:20px">
                Harakat yozilmagan (jurnal joriy etilishidan oldingi o'zgarishlar ko'rinmaydi)
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.filters { display: flex; gap: 12px; align-items: flex-end; flex-wrap: wrap; }
.filters .field { margin: 0; }
.idle { display: flex; align-items: center; gap: 6px; font-size: 13px; padding-bottom: 10px; white-space: nowrap; }
.turnover th, .turnover td { white-space: nowrap; }
.turnover td:first-child { white-space: normal; min-width: 200px; }
.in { color: #166534; }
.out { color: #9f1239; }
.clickable { cursor: pointer; }
.clickable:hover td { background: var(--bg); }
tfoot td { border-top: 2px solid var(--border); }
</style>
