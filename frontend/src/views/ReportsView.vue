<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { reportsApi, downloadBlob } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()

function todayStr() { return new Date().toISOString().slice(0, 10) }

const tab = ref('sales')  // 'sales' | 'stock'

// --- Filters ---
const from = ref(todayStr())
const to   = ref(todayStr())

// --- Data ---
const salesReport = ref(null)
const stockReport = ref(null)
const loading = ref(false)
const error = ref('')

function fmt(n) { return Number(n || 0).toLocaleString() }
function pct(n) { return Number(n || 0).toFixed(1) + '%' }

// --- Load ---
async function loadSales() {
  loading.value = true
  error.value = ''
  try {
    const params = { from: from.value, to: to.value }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    salesReport.value = await reportsApi.sales(params)
  } catch (e) { error.value = errorMessage(e) }
  finally { loading.value = false }
}

async function loadStock() {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    stockReport.value = await reportsApi.stock(params)
  }
  catch (e) { error.value = errorMessage(e) }
  finally { loading.value = false }
}

function load() {
  if (tab.value === 'sales') loadSales()
  else loadStock()
}

// --- Export ---
async function exportSales() {
  try {
    const params = { from: from.value, to: to.value }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    downloadBlob(await reportsApi.exportSales(params), `savdolar-${from.value}-${to.value}.xlsx`)
  } catch (e) { error.value = errorMessage(e) }
}

async function exportStock() {
  try {
    const params = {}
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    downloadBlob(await reportsApi.exportStock(params), `qoldiqlar-${todayStr()}.xlsx`)
  } catch (e) { error.value = errorMessage(e) }
}

function printPage() { window.print() }

// --- Stock search ---
const stockSearch = ref('')
const filteredStock = computed(() => {
  if (!stockReport.value) return []
  const q = stockSearch.value.trim().toLowerCase()
  if (!q) return stockReport.value.items
  return stockReport.value.items.filter(
    (i) => i.name.toLowerCase().includes(q) ||
           i.category.toLowerCase().includes(q) ||
           (i.barcode || '').toLowerCase().includes(q)
  )
})

watch(() => storeFilter.selectedBranchId, () => load())

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Hisobotlar</h1>
    <div style="display:flex; gap:8px">
      <button v-if="tab === 'sales'" class="btn btn-ghost btn-sm" @click="exportSales">📥 Excel</button>
      <button v-if="tab === 'stock'" class="btn btn-ghost btn-sm" @click="exportStock">📥 Excel</button>
      <button class="btn btn-ghost btn-sm" @click="printPage">🖨 Chop etish</button>
    </div>
  </div>

  <!-- Tabs -->
  <div style="display:flex; gap:4px; margin-bottom:16px; border-bottom:2px solid var(--border); padding-bottom:0">
    <button
      v-for="t in [{ key:'sales', label:'Savdolar' }, { key:'stock', label:'Qoldiqlar' }]"
      :key="t.key"
      class="btn btn-ghost btn-sm"
      :style="tab === t.key ? 'border-bottom:2px solid var(--primary); border-radius:0; color:var(--primary); font-weight:700; margin-bottom:-2px' : 'border-radius:0; margin-bottom:-2px'"
      @click="tab = t.key; load()"
    >{{ t.label }}</button>
  </div>

  <!-- Sales filters -->
  <div v-if="tab === 'sales'" class="card" style="margin-bottom:14px; padding:14px 20px">
    <div style="display:flex; gap:10px; flex-wrap:wrap; align-items:flex-end">
      <div class="field" style="margin:0">
        <label>Dan</label>
        <input v-model="from" type="date" class="input" />
      </div>
      <div class="field" style="margin:0">
        <label>Gacha</label>
        <input v-model="to" type="date" class="input" />
      </div>
      <div v-if="storeFilter.multiStore" class="field" style="margin:0; min-width:160px">
        <label>Do'kon</label>
        <select
          class="select"
          :value="storeFilter.selectedBranchId"
          @change="storeFilter.select($event.target.value)"
        >
          <option value="">— Hammasi —</option>
          <option v-for="s in storeFilter.stores" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      <button class="btn" :disabled="loading" @click="loadSales">
        {{ loading ? 'Yuklanmoqda...' : 'Ko\'rsatish' }}
      </button>
    </div>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>

  <!-- ═══════════ SAVDOLAR ═══════════ -->
  <template v-if="tab === 'sales' && salesReport">
    <!-- KPI qatorlar -->
    <div class="kpi-row">
      <div class="card kpi-card">
        <div class="kpi-lbl">Jami tushum</div>
        <div class="kpi-val" style="color:var(--primary)">{{ fmt(salesReport.totalRevenue) }}</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-lbl">Tannarx</div>
        <div class="kpi-val" style="color:var(--muted)">{{ fmt(salesReport.totalCost) }}</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-lbl">Foyda</div>
        <div class="kpi-val" style="color:var(--success)">{{ fmt(salesReport.totalProfit) }}</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-lbl">Marjinallik</div>
        <div class="kpi-val" style="color:var(--success)">{{ pct(salesReport.marginPercent) }}</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-lbl">Savdolar soni</div>
        <div class="kpi-val">{{ salesReport.totalCount }}</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-lbl">Qaytarishlar</div>
        <div class="kpi-val" style="color:var(--danger)">{{ fmt(salesReport.refundAmount) }}</div>
      </div>
    </div>

    <!-- To'lov turlar -->
    <div class="card" style="margin-bottom:14px; padding:14px 20px">
      <div style="display:flex; gap:24px; flex-wrap:wrap">
        <div><span class="muted">Naqd:</span> <strong>{{ fmt(salesReport.cashAmount) }}</strong></div>
        <div><span class="muted">Karta:</span> <strong>{{ fmt(salesReport.cardAmount) }}</strong></div>
        <div><span class="muted">Nasiya:</span> <strong>{{ fmt(salesReport.debtAmount) }}</strong></div>
      </div>
    </div>

    <!-- Kunlik jadval -->
    <div class="card" style="margin-bottom:14px">
      <table class="table">
        <thead>
          <tr>
            <th>Sana</th>
            <th class="text-right">Savdolar</th>
            <th class="text-right">Tushum</th>
            <th class="text-right">Tannarx</th>
            <th class="text-right">Foyda</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in salesReport.days" :key="d.date">
            <td>{{ new Date(d.date).toLocaleDateString('uz-UZ') }}</td>
            <td class="text-right">{{ d.count }}</td>
            <td class="text-right">{{ fmt(d.revenue) }}</td>
            <td class="text-right muted">{{ fmt(d.cost) }}</td>
            <td class="text-right" :style="{ color: d.profit >= 0 ? 'var(--success)' : 'var(--danger)' }">{{ fmt(d.profit) }}</td>
          </tr>
          <tr v-if="!salesReport.days.length">
            <td colspan="5" class="muted" style="text-align:center; padding:20px">Ma'lumot yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Top mahsulotlar -->
    <div v-if="salesReport.topProducts.length" class="card">
      <div style="font-weight:700; margin-bottom:10px">Top 20 mahsulot</div>
      <table class="table">
        <thead>
          <tr>
            <th>#</th><th>Mahsulot</th>
            <th class="text-right">Miqdor</th>
            <th class="text-right">Tushum</th>
            <th class="text-right">Foyda</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(p, i) in salesReport.topProducts" :key="p.productName">
            <td class="muted">{{ i + 1 }}</td>
            <td>{{ p.productName }}</td>
            <td class="text-right">{{ fmt(p.quantity) }}</td>
            <td class="text-right">{{ fmt(p.revenue) }}</td>
            <td class="text-right" style="color:var(--success)">{{ fmt(p.profit) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </template>

  <!-- ═══════════ QOLDIQLAR ═══════════ -->
  <template v-if="tab === 'stock'">
    <!-- Stock filters -->
    <div class="card" style="margin-bottom:14px; padding:14px 20px">
      <div style="display:flex; gap:10px; flex-wrap:wrap; align-items:flex-end">
        <div v-if="storeFilter.multiStore" class="field" style="margin:0; min-width:160px">
          <label>Do'kon</label>
          <select
            class="select"
            :value="storeFilter.selectedBranchId"
            @change="storeFilter.select($event.target.value)"
          >
            <option value="">— Hammasi —</option>
            <option v-for="s in storeFilter.stores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <button class="btn" :disabled="loading" @click="loadStock">
          {{ loading ? 'Yuklanmoqda...' : 'Ko\'rsatish' }}
        </button>
        <div style="margin-left:auto">
          <input v-model="stockSearch" class="input" placeholder="Qidirish..." style="width:220px" />
        </div>
      </div>
    </div>

    <div v-if="stockReport" class="card" style="margin-bottom:14px; padding:14px 20px">
      <div style="display:flex; gap:24px; flex-wrap:wrap; align-items:center">
        <div><span class="muted">Jami mahsulot:</span> <strong>{{ stockReport.totalProducts }}</strong></div>
        <div><span class="muted">Tannarx qiymati:</span> <strong>{{ fmt(stockReport.totalCostValue) }}</strong></div>
        <div><span class="muted">Savdo qiymati:</span> <strong>{{ fmt(stockReport.totalSaleValue) }}</strong></div>
      </div>
    </div>

    <div v-if="stockReport" class="card">
      <table class="table">
        <thead>
          <tr>
            <th>Mahsulot</th><th>Kategoriya</th><th>Barcode</th>
            <th class="text-right">Qoldiq</th>
            <th class="text-right">Tannarx</th>
            <th class="text-right">Narx</th>
            <th class="text-right">Qiymat (tan)</th>
            <th class="text-right">Qiymat (savdo)</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="i in filteredStock" :key="i.name + i.barcode"
              :style="i.quantity <= 0 ? 'opacity:0.5' : ''">
            <td>{{ i.name }}</td>
            <td class="muted">{{ i.category }}</td>
            <td class="muted" style="font-size:12px">{{ i.barcode || '—' }}</td>
            <td class="text-right" :style="{ color: i.quantity <= 0 ? 'var(--danger)' : 'inherit' }">
              {{ fmt(i.quantity) }} {{ i.unit }}
            </td>
            <td class="text-right muted">{{ fmt(i.costPrice) }}</td>
            <td class="text-right">{{ fmt(i.salePrice) }}</td>
            <td class="text-right muted">{{ fmt(i.costValue) }}</td>
            <td class="text-right">{{ fmt(i.saleValue) }}</td>
          </tr>
          <tr v-if="!filteredStock.length">
            <td colspan="8" class="muted" style="text-align:center; padding:20px">Ma'lumot yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="loading" class="muted" style="text-align:center; padding:32px">Yuklanmoqda...</div>
  </template>
</template>

<style scoped>
.kpi-row {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
  margin-bottom: 14px;
}
.kpi-card { padding: 18px 20px; }
.kpi-lbl  { font-size: 12px; color: var(--muted); font-weight: 600; margin-bottom: 6px; }
.kpi-val  { font-size: 22px; font-weight: 800; }

@media print {
  .page-head button, .btn { display: none !important; }
  .card { box-shadow: none; border: 1px solid #ddd; }
}
</style>
