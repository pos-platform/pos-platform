<script setup>
import { localDateStr } from '../utils/date'
import { useI18n } from 'vue-i18n'
import AppIcon from '../components/AppIcon.vue'
import { ref, reactive, watch, onMounted, onUnmounted } from 'vue'
import { dashboardApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()
const { t } = useI18n()

function todayStr() {
  return localDateStr()
}

const loading = ref(false)
const err = ref('')
const summary = ref(null)
const filters = reactive({ from: todayStr(), to: todayStr() })

function fmt(n) {
  return Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 2 })
}

const cards = ref([])
function buildCards(s) {
  cards.value = [
    { label: 'Jami sotuv', value: s.totalSales, icon: 'receipt', tone: 'blue' },
    { label: 'Sof sotuv', value: s.netSales, icon: 'trendUp', tone: 'green' },
    { label: 'Qaytarishlar', value: s.returns, icon: 'undo', tone: 'amber' },
    { label: 'Xarajatlar', value: s.expenses, icon: 'trendDown', tone: 'red' },
    { label: 'Naqd kirim', value: s.cashIncome, icon: 'banknote', tone: 'slate' },
    { label: 'Karta kirim', value: s.cardIncome, icon: 'card', tone: 'slate' },
    { label: "O'tkazma", value: s.transferIncome, icon: 'transfer', tone: 'slate' },
    { label: 'Qarzlar', value: s.debts, icon: 'book', tone: 'red' }
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
    <div>
      <h1>{{ t('nav.dashboard') }}</h1>
      <div class="dash-sub">
        <span v-if="summary">{{ summary.salesCount }} ta savdo</span>
        <span v-if="lastUpdated"> · yangilandi {{ lastUpdated }}</span>
      </div>
    </div>
    <label class="auto-toggle">
      <input type="checkbox" v-model="polling" />
      <AppIcon name="refresh" :size="14" />
      Avtomatik yangilash
    </label>
  </div>

  <!-- Filtrlar -->
  <div class="dash-filter">
    <div class="df-field">
      <label>Dan</label>
      <input v-model="filters.from" type="date" class="input" />
    </div>
    <div class="df-field">
      <label>Gacha</label>
      <input v-model="filters.to" type="date" class="input" />
    </div>
    <button class="btn" :disabled="loading" @click="load">Ko'rsatish</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <!-- KPI kartalar -->
  <div class="kpi-grid">
    <div v-for="c in cards" :key="c.label" class="kpi" :class="'tone-' + c.tone">
      <div class="kpi-top">
        <span class="kpi-label">{{ c.label }}</span>
        <span class="kpi-icon"><AppIcon :name="c.icon" :size="16" /></span>
      </div>
      <div class="kpi-value">{{ fmt(c.value) }}<span class="kpi-cur">so'm</span></div>
    </div>
    <p v-if="!cards.length && !loading" class="muted">Ma'lumot yo'q</p>
  </div>
</template>

<style scoped>
.dash-sub { margin-top: 4px; font-size: 13px; color: var(--muted); }
.auto-toggle {
  display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--muted);
  cursor: pointer; padding: 7px 12px; border: 1px solid var(--border); border-radius: 8px; background: var(--surface);
}
.auto-toggle input { margin: 0; cursor: pointer; }

.dash-filter {
  display: flex; align-items: flex-end; gap: 12px; flex-wrap: wrap;
  padding: 14px 16px; margin-bottom: 16px;
  background: var(--surface); border: 1px solid var(--border); border-radius: 10px;
}
.df-field { display: flex; flex-direction: column; gap: 5px; width: 200px; }
.df-field label { font-size: 12px; font-weight: 600; color: var(--muted); }

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}
@media (max-width: 1200px) { .kpi-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 560px) {
  .kpi-grid { grid-template-columns: 1fr; }
  .df-field { width: 100%; }
}

.kpi {
  position: relative;
  padding: 16px 18px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  overflow: hidden;
}
.kpi::before {
  content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 3px;
  background: var(--accent);
}
.kpi-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.kpi-label {
  font-size: 12px; font-weight: 600; letter-spacing: .04em; text-transform: uppercase; color: var(--muted);
}
.kpi-icon {
  width: 30px; height: 30px; border-radius: 8px; display: flex; align-items: center; justify-content: center;
  color: var(--accent); background: var(--accent-soft);
}
.kpi-value {
  margin-top: 14px; font-size: 24px; font-weight: 700; color: var(--text);
  font-variant-numeric: tabular-nums; letter-spacing: -0.01em; white-space: nowrap;
  overflow: hidden; text-overflow: ellipsis;
}
.kpi-cur { margin-left: 6px; font-size: 13px; font-weight: 500; color: var(--muted-2); }

.tone-blue  { --accent: #1d5fd6; --accent-soft: #eaf1fd; }
.tone-green { --accent: #15803d; --accent-soft: #e8f5ec; }
.tone-amber { --accent: #b45309; --accent-soft: #fdf3e2; }
.tone-red   { --accent: #be123c; --accent-soft: #fdecef; }
.tone-slate { --accent: #475569; --accent-soft: #f1f4f8; }
</style>
