<script setup>
import { localDateStr } from '../utils/date'
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { analyticsApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()

function daysAgo(n) {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return localDateStr(d)
}

const data = ref(null)
const loading = ref(false)
const err = ref('')
const filters = reactive({ from: daysAgo(6), to: daysAgo(0) })

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleDateString('uz-UZ', { day: '2-digit', month: '2-digit' }) }

const maxDaily = computed(() => {
  if (!data.value?.dailySales?.length) return 1
  return Math.max(...data.value.dailySales.map((d) => Number(d.total)), 1)
})

async function load() {
  loading.value = true
  err.value = ''
  try {
    const params = { from: filters.from, to: filters.to }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    data.value = await analyticsApi.get(params)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

watch(() => storeFilter.selectedBranchId, () => load())

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Аналитика</h1>
  </div>

  <div class="card" style="margin-bottom:16px">
    <div class="row" style="align-items:flex-end">
      <div class="field" style="margin:0"><label>Dan</label><input v-model="filters.from" type="date" class="input" /></div>
      <div class="field" style="margin:0"><label>Gacha</label><input v-model="filters.to" type="date" class="input" /></div>
      <button class="btn" @click="load">Ko'rsatish</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <template v-if="data">
    <!-- Summary -->
    <div class="kpi-grid" style="margin-bottom:16px">
      <div class="card kpi"><div class="kpi-value" style="color:var(--primary)">{{ fmt(data.revenue) }}</div><div class="kpi-label">Tushum</div></div>
      <div class="card kpi"><div class="kpi-value" style="color:var(--danger)">{{ fmt(data.cost) }}</div><div class="kpi-label">Tannarx (COGS)</div></div>
      <div class="card kpi"><div class="kpi-value" style="color:var(--success)">{{ fmt(data.profit) }}</div><div class="kpi-label">Foyda</div></div>
      <div class="card kpi"><div class="kpi-value">{{ Number(data.marginPercent).toFixed(1) }}%</div><div class="kpi-label">Marja</div></div>
      <div class="card kpi"><div class="kpi-value">{{ data.salesCount }}</div><div class="kpi-label">Savdolar</div></div>
      <div class="card kpi"><div class="kpi-value">{{ fmt(data.avgCheck) }}</div><div class="kpi-label">O'rtacha chek</div></div>
    </div>

    <div class="row" style="align-items:flex-start">
      <!-- Kunlik sotuv -->
      <div class="card" style="flex:1; min-width:340px">
        <h3 style="margin-top:0">Kunlik sotuv</h3>
        <div v-if="data.dailySales.length" class="bars">
          <div v-for="d in data.dailySales" :key="d.date" class="bar-col">
            <div class="bar-val">{{ fmt(d.total) }}</div>
            <div class="bar" :style="{ height: (Number(d.total) / maxDaily * 160 + 2) + 'px' }"></div>
            <div class="bar-lbl">{{ fmtDate(d.date) }}</div>
          </div>
        </div>
        <p v-else class="muted">Ma'lumot yo'q</p>
      </div>

      <!-- Top tovarlar -->
      <div class="card" style="flex:1; min-width:340px">
        <h3 style="margin-top:0">Eng ko'p sotilgan (tushum bo'yicha)</h3>
        <table class="table">
          <thead><tr><th>Tovar</th><th class="text-right">Soni</th><th class="text-right">Tushum</th></tr></thead>
          <tbody>
            <tr v-for="(p, i) in data.topProducts" :key="i">
              <td>{{ p.productName }}</td>
              <td class="text-right">{{ fmt(p.quantity) }}</td>
              <td class="text-right">{{ fmt(p.revenue) }}</td>
            </tr>
            <tr v-if="!data.topProducts.length"><td colspan="3" class="muted" style="text-align:center;padding:16px">Ma'lumot yo'q</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  </template>
</template>

<style scoped>
.kpi-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(180px, 1fr)); gap:14px; }
.kpi { padding:18px; }
.kpi-value { font-size:24px; font-weight:800; line-height:1.1; }
.kpi-label { color:var(--muted); margin-top:6px; font-weight:600; font-size:13px; }
.bars { display:flex; align-items:flex-end; gap:10px; height:220px; padding-top:10px; overflow-x:auto; }
.bar-col { display:flex; flex-direction:column; align-items:center; justify-content:flex-end; min-width:48px; }
.bar { width:34px; background:var(--primary); border-radius:6px 6px 0 0; transition:height .2s; }
.bar-val { font-size:11px; color:var(--muted); margin-bottom:4px; white-space:nowrap; }
.bar-lbl { font-size:11px; color:var(--muted); margin-top:6px; }
</style>
