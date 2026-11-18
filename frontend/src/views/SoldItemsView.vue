<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { reportsApi, categoriesApi, usersApi, downloadBlob } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'
import SaleDetailModal from './sales/SaleDetailModal.vue'
import AppIcon from '../components/AppIcon.vue'

// Bosqich 10.3: sotilgan tovarlar logi — har bir chek qatori (SaleItem) alohida.
const storeFilter = useStoreFilterStore()

function toDateStr(d) {
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}
function daysAgo(n) {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return toDateStr(d)
}

const f = reactive({ from: daysAgo(0), to: daysAgo(0), categoryId: '', cashierId: '', source: 'All', type: 'All', search: '' })
const page = ref(1)
const pageSize = 50
const data = ref({ items: [], total: 0, totals: null })
const loading = ref(false)
const exporting = ref(false)
const err = ref('')

const categories = ref([])
const cashiers = ref([])

const PAY = { Cash: 'Naqd', Card: 'Karta', Mixed: 'Aralash', Debt: 'Nasiya', Transfer: "O'tkazma" }
const FISCAL = { NotSent: { t: 'Yuborilmagan', c: 'pill-gray' }, Queued: { t: 'Navbatda', c: 'pill-amber' }, Registered: { t: 'Fiskal', c: 'pill-green' }, Failed: { t: 'Xato', c: 'pill-red' } }

const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtQty = (n) => Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
const fmtDate = (d) => (d ? new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '')
const pages = computed(() => Math.max(1, Math.ceil((data.value.total || 0) / pageSize)))

function params(withPage = true) {
  const x = { from: f.from, to: f.to, source: f.source, type: f.type }
  if (storeFilter.selectedBranchId) x.branchId = storeFilter.selectedBranchId
  if (f.categoryId) x.categoryId = f.categoryId
  if (f.cashierId) x.cashierId = f.cashierId
  if (f.search.trim()) x.search = f.search.trim()
  if (withPage) { x.page = page.value; x.pageSize = pageSize }
  return x
}

async function load() {
  if (!f.from || !f.to) return
  loading.value = true
  err.value = ''
  try {
    const r = await reportsApi.soldItems(params())
    data.value = { items: r?.items || [], total: r?.total || 0, totals: r?.totals || null }
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function apply() {
  if (page.value !== 1) page.value = 1
  else load()
}

async function exportExcel() {
  exporting.value = true
  err.value = ''
  try {
    downloadBlob(await reportsApi.exportSoldItems(params(false)), `sotuvlar-logi-${f.from}-${f.to}.xlsx`)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    exporting.value = false
  }
}

// Chekni ochish (mavjud savdo tafsiloti modali)
const detailSaleId = ref(null)

watch(page, load)
watch(() => [f.from, f.to, f.categoryId, f.cashierId, f.source, f.type], apply)
watch(() => storeFilter.selectedBranchId, apply)

onMounted(async () => {
  await storeFilter.ensureLoaded()
  load()
  try {
    const c = await categoriesApi.list()
    categories.value = Array.isArray(c) ? c : c?.items || []
  } catch { /* filtr ixtiyoriy */ }
  try {
    const u = await usersApi.list()
    cashiers.value = (Array.isArray(u) ? u : u?.items || []).filter((x) => x.role !== 'Courier' && x.role !== 'SuperAdmin')
  } catch { /* ruxsat bo'lmasa — kassir filtri ko'rinmaydi */ }
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Sotuvlar logi</h1>
      <div class="muted small">Har bir sotilgan va qaytarilgan tovar qatori: kim, qachon, qaysi chekda, foyda va keshbek.</div>
    </div>
    <button class="btn btn-ghost btn-sm" :disabled="exporting" @click="exportExcel"><AppIcon name="sheet" :size="15" />{{ exporting ? '...' : 'Excel' }}</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="card toolbar">
    <div class="field-inline">
      <input v-model="f.from" type="date" class="input date-in" :max="f.to" />
      <span class="muted">—</span>
      <input v-model="f.to" type="date" class="input date-in" :min="f.from" />
    </div>
    <select v-model="f.type" class="select w-auto">
      <option value="All">Sotuv va qaytarish</option>
      <option value="Sale">Faqat sotuv</option>
      <option value="Refund">Faqat qaytarish</option>
    </select>
    <select v-model="f.source" class="select w-auto">
      <option value="All">Kassa va online</option>
      <option value="Pos">Kassa</option>
      <option value="Online">Online</option>
    </select>
    <select v-if="categories.length" v-model="f.categoryId" class="select w-auto">
      <option value="">Barcha kategoriyalar</option>
      <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
    </select>
    <select v-if="cashiers.length" v-model="f.cashierId" class="select w-auto">
      <option value="">Barcha kassirlar</option>
      <option v-for="u in cashiers" :key="u.id" :value="u.id">{{ u.fullName || u.username }}</option>
    </select>
    <input v-model="f.search" class="input" style="width:240px" placeholder="Tovar, shtrix-kod, chek, mijoz..." @keyup.enter="apply" />
    <button class="btn btn-sm" :disabled="loading" @click="apply">Ko'rsatish</button>
    <span v-if="storeFilter.selectedStore" class="muted small">Do'kon: {{ storeFilter.selectedStore.name }}</span>
  </div>

  <div v-if="data.totals" class="summary">
    <div class="card stat"><div class="stat-label">Miqdor</div><div class="stat-value">{{ fmtQty(data.totals.quantity) }}</div></div>
    <div class="card stat"><div class="stat-label">Tushum</div><div class="stat-value">{{ fmtNum(data.totals.revenue) }}</div></div>
    <div class="card stat"><div class="stat-label">Foyda</div><div class="stat-value" style="color:var(--success)">{{ fmtNum(data.totals.profit) }}</div></div>
    <div class="card stat"><div class="stat-label">Qaytarishlar</div><div class="stat-value" style="color:var(--danger)">− {{ fmtNum(data.totals.refunds) }}</div></div>
  </div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th>Vaqt</th>
          <th>Chek</th>
          <th>Tovar</th>
          <th class="text-right">Miqdor</th>
          <th class="text-right">Narx</th>
          <th class="text-right">Chegirma</th>
          <th class="text-right">Summa</th>
          <th class="text-right">Foyda</th>
          <th class="text-right">Keshbek</th>
          <th>To'lov</th>
          <th>Kassir / filial</th>
          <th>Mijoz</th>
          <th>Fiskal</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="13" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
        <tr v-else-if="!data.items.length"><td colspan="13" class="muted" style="text-align:center; padding:24px">Ma'lumot yo'q</td></tr>
        <tr v-for="r in data.items" v-else :key="r.saleItemId" :class="{ refund: r.type === 'Refund' }">
          <td class="muted nowrap">{{ fmtDate(r.soldAt) }}</td>
          <td class="nowrap">
            <a href="#" class="rcpt-link" @click.prevent="detailSaleId = r.saleId"><strong>{{ r.receiptNumber }}</strong></a>
            <div>
              <span v-if="r.type === 'Refund'" class="pill pill-red">Qaytarish</span>
              <span v-if="r.source === 'Online'" class="pill pill-gray">Online<template v-if="r.onlineOrderNumber"> #{{ r.onlineOrderNumber }}</template></span>
            </div>
          </td>
          <td><strong>{{ r.productName }}</strong><div class="muted small">{{ r.barcode || '' }}</div></td>
          <td class="text-right nowrap">
            {{ fmtQty(r.quantity) }}
            <div v-if="Number(r.refundedQuantity) > 0" class="small" style="color:var(--danger)">qaytarilgan {{ fmtQty(r.refundedQuantity) }}</div>
          </td>
          <td class="text-right">{{ fmtNum(r.unitPrice) }}</td>
          <td class="text-right">{{ Number(r.discountAmount) > 0 ? fmtNum(r.discountAmount) : '—' }}</td>
          <td class="text-right" :style="{ color: r.type === 'Refund' ? 'var(--danger)' : '', fontWeight: 600 }">{{ r.type === 'Refund' ? '− ' : '' }}{{ fmtNum(r.lineTotal) }}</td>
          <td class="text-right">{{ fmtNum(r.profit) }}<div class="muted small">tannarx {{ fmtNum(r.costPrice) }}</div></td>
          <td class="text-right">{{ Number(r.cashbackEarned) ? fmtNum(r.cashbackEarned) : '—' }}</td>
          <td class="small">{{ PAY[r.paymentMethod] || r.paymentMethod }}</td>
          <td class="small">{{ r.cashierName || '—' }}<div class="muted">{{ r.branchName || '' }}</div></td>
          <td class="small">{{ r.customerName || '—' }}</td>
          <td><span class="pill" :class="FISCAL[r.fiscalStatus]?.c || 'pill-gray'">{{ FISCAL[r.fiscalStatus]?.t || r.fiscalStatus || '—' }}</span></td>
        </tr>
      </tbody>
    </table>
    <div v-if="pages > 1" class="pager">
      <button class="btn btn-ghost btn-sm" :disabled="page <= 1 || loading" @click="page--">‹</button>
      <span class="muted small">{{ page }} / {{ pages }} · jami {{ data.total }} qator</span>
      <button class="btn btn-ghost btn-sm" :disabled="page >= pages || loading" @click="page++">›</button>
    </div>
  </div>

  <SaleDetailModal v-if="detailSaleId" :sale-id="detailSaleId" @close="detailSaleId = null" />
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.toolbar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 12px; }
.field-inline { display: flex; align-items: center; gap: 6px; }
.date-in { width: 150px; }
.w-auto { width: auto; }
.summary { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 12px; }
.stat-label { font-size: 12px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 22px; font-weight: 700; margin-top: 4px; }
.rcpt-link { color: inherit; text-decoration: none; border-bottom: 1px dashed var(--border); }
.rcpt-link:hover { color: var(--primary); }
.pill + .pill { margin-left: 4px; }
tr.refund { background: #fff7f7; }
.pager { display: flex; justify-content: center; align-items: center; gap: 10px; padding: 12px; }
@media (max-width: 900px) {
  .summary { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
