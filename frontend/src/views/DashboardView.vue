<script setup>
import { ref, reactive, watch, onMounted, onUnmounted } from 'vue'
import { dashboardApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()

function todayStr() {
  return new Date().toISOString().slice(0, 10)
}

const loading = ref(false)
const err = ref('')
const summary = ref(null)
const filters = reactive({ from: todayStr(), to: todayStr() })

function fmt(n) {
  return Number(n || 0).toLocaleString()
}

const cards = ref([])
function buildCards(s) {
  cards.value = [
    { label: 'Jami sotuv', value: s.totalSales, icon: '🧾', color: 'var(--primary)' },
    { label: 'Naqd kirim', value: s.cashIncome, icon: '💵', color: 'var(--success)' },
    { label: 'Karta kirim', value: s.cardIncome, icon: '💳', color: 'var(--primary)' },
    { label: 'Qaytarishlar', value: s.returns, icon: '↩️', color: 'var(--warning)' },
    { label: 'Qarzlar', value: s.debts, icon: '📕', color: 'var(--danger)' },
    { label: 'Xarajatlar', value: s.expenses, icon: '📉', color: 'var(--danger)' }
  ]
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    const params = { from: filters.from, to: filters.to }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    summary.value = await dashboardApi.summary(params)
    buildCards(summary.value)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

const lastUpdated = ref('')
const polling = ref(true)
let pollTimer = null

async function loadSilent() {
  if (!polling.value) return
  try {
    const params = { from: filters.from, to: filters.to }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    summary.value = await dashboardApi.summary(params)
    buildCards(summary.value)
    lastUpdated.value = new Date().toLocaleTimeString()
  } catch { /* silent — UI ni buzmasin */ }
}

// Sidebar'dan do'kon o'zgarsa sahifani qayta yuklaydi
watch(() => storeFilter.selectedBranchId, () => load())

onMounted(() => {
  load()
  // Bugun filtri aktiv bo'lsa har 30 sek yangilanadi
  pollTimer = setInterval(() => {
    if (filters.from === todayStr() && filters.to === todayStr()) loadSilent()
  }, 30_000)
})

onUnmounted(() => clearInterval(pollTimer))
</script>

<template>
  <div class="page-head">
    <h1>Главный</h1>
    <div style="display:flex; align-items:center; gap:12px">
      <span v-if="summary" class="muted">{{ summary.salesCount }} ta savdo</span>
      <span v-if="lastUpdated" class="muted" style="font-size:12px">● yangilandi {{ lastUpdated }}</span>
      <label style="display:flex; align-items:center; gap:6px; font-size:13px; cursor:pointer; color:var(--muted)">
        <input type="checkbox" v-model="polling" style="cursor:pointer" />
        Avtomatik
      </label>
    </div>
  </div>

  <!-- Filtrlar -->
  <div class="card" style="margin-bottom: 16px">
    <div class="row" style="align-items: flex-end">
      <div class="field" style="margin:0">
        <label>Dan</label>
        <input v-model="filters.from" type="date" class="input" />
      </div>
      <div class="field" style="margin:0">
        <label>Gacha</label>
        <input v-model="filters.to" type="date" class="input" />
      </div>
      <button class="btn" @click="load">Ko'rsatish</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <!-- KPI kartalar -->
  <div class="kpi-grid">
    <div v-for="c in cards" :key="c.label" class="card kpi">
      <div class="kpi-icon">{{ c.icon }}</div>
      <div class="kpi-value" :style="{ color: c.color }">{{ fmt(c.value) }}</div>
      <div class="kpi-label">{{ c.label }}</div>
    </div>
    <p v-if="!cards.length && !loading" class="muted">Ma'lumot yo'q</p>
  </div>
</template>

<style scoped>
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;
}
.kpi { padding: 22px; }
.kpi-icon { font-size: 22px; margin-bottom: 10px; }
.kpi-value { font-size: 28px; font-weight: 800; line-height: 1.1; }
.kpi-label { color: var(--muted); margin-top: 6px; font-weight: 600; }
</style>
