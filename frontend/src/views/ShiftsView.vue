<script setup>
import { ref, onMounted } from 'vue'
import { shiftsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const current = ref(null)
const shifts = ref([])
const countedCash = ref(0)
const openingCash = ref(0)
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const closedReport = ref(null)
const xReportSent = ref(false)

function fmt(n) {
  return Number(n || 0).toLocaleString()
}
function fmtDate(d) {
  return d ? new Date(d).toLocaleString('uz-UZ') : '—'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [c, list] = await Promise.all([shiftsApi.current(), shiftsApi.list()])
    current.value = c
    shifts.value = list
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function openShift() {
  error.value = ''
  busy.value = true
  try {
    await shiftsApi.open({ openingCash: Number(openingCash.value) || 0 })
    openingCash.value = 0
    closedReport.value = null
    xReportSent.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function requestXReport() {
  error.value = ''
  busy.value = true
  try {
    await shiftsApi.requestXReport()
    xReportSent.value = true
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function closeShift() {
  error.value = ''
  busy.value = true
  try {
    closedReport.value = await shiftsApi.close({ countedCash: Number(countedCash.value) || 0 })
    countedCash.value = 0
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Smenalar</h1>
    <button class="btn btn-ghost btn-sm" @click="load">Yangilash</button>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>

  <!-- Ochiq smenani yopish -->
  <div v-if="current" class="card" style="margin-bottom: 16px">
    <h3 style="margin-top: 0">Ochiq smenani yopish</h3>
    <p class="muted">
      Ochilgan: {{ fmtDate(current.openedAt) }} ·
      Boshlang'ich naqd: {{ fmt(current.openingCash) }} so'm
    </p>
    <div class="row" style="align-items: flex-end; gap: 8px; flex-wrap: wrap">
      <div class="field" style="margin: 0">
        <label>Sanab chiqilgan naqd</label>
        <input v-model="countedCash" type="number" min="0" class="input" style="width: 220px" />
      </div>
      <button class="btn btn-danger" :disabled="busy" @click="closeShift">
        {{ busy ? '...' : 'Smenani yopish' }}
      </button>
      <button class="btn btn-ghost" :disabled="busy" @click="requestXReport" :title="'X-hisobot printer\'ga yuborish'">
        {{ xReportSent ? 'X-hisobot yuborildi ✓' : 'X-hisobot chop et' }}
      </button>
    </div>
  </div>
  <div v-else class="card" style="margin-bottom: 16px">
    <h3 style="margin-top: 0">Smena ochish</h3>
    <p class="muted">Ochiq smena yo'q. Savdo qilish uchun smenani oching.</p>
    <div class="row" style="align-items: flex-end">
      <div class="field" style="margin: 0">
        <label>Boshlang'ich naqd (kassadagi pul)</label>
        <input v-model="openingCash" type="number" min="0" class="input" style="width: 220px" />
      </div>
      <button class="btn" :disabled="busy" @click="openShift">
        {{ busy ? '...' : 'Smenani ochish' }}
      </button>
    </div>
  </div>

  <!-- Yopilgan smena hisoboti (X-Z hisobot) -->
  <div v-if="closedReport" class="card" style="margin-bottom: 16px">
    <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:12px">
      <h3 style="margin: 0">Smena hisoboti — {{ fmtDate(closedReport.closedAt) }}</h3>
      <span class="muted" style="font-size:13px">{{ closedReport.cashierName || '' }}</span>
    </div>

    <!-- Savdo ko'rsatkichlari -->
    <div class="report-section">
      <div class="report-label">Sotuvlar</div>
      <div class="totals">
        <div class="t-row"><span>Sotuv operatsiyalari</span><span>{{ closedReport.saleCount }} ta</span></div>
        <div class="t-row"><span>Sotilgan tovarlar</span><span>{{ fmt(closedReport.totalItemsSold) }} dona</span></div>
        <div class="t-row"><span>Sotuv jami</span><span>{{ fmt(closedReport.totalSales) }} so'm</span></div>
        <div class="t-row"><span>QQS (12%)</span><span>{{ fmt(closedReport.vatAmount) }} so'm</span></div>
        <div class="t-row" v-if="closedReport.refundCount"><span>Qaytarishlar ({{ closedReport.refundCount }} ta)</span><span>− {{ fmt(closedReport.totalRefunds) }} so'm</span></div>
      </div>
    </div>

    <!-- To'lov usullari bo'yicha -->
    <div class="report-section">
      <div class="report-label">To'lov usullari (sof)</div>
      <div class="totals">
        <div class="t-row"><span>Naqd</span><span>{{ fmt(closedReport.cashSalesNet) }} so'm</span></div>
        <div class="t-row"><span>Karta</span><span>{{ fmt(closedReport.cardSalesNet) }} so'm</span></div>
        <div class="t-row" v-if="closedReport.transferSalesNet"><span>O'tkazma</span><span>{{ fmt(closedReport.transferSalesNet) }} so'm</span></div>
        <div class="t-row" v-if="closedReport.debtSalesNet"><span>Nasiya</span><span>{{ fmt(closedReport.debtSalesNet) }} so'm</span></div>
      </div>
    </div>

    <!-- Kassa hisob-kitobi -->
    <div class="report-section">
      <div class="report-label">Kassa hisob-kitobi (X-hisobot)</div>
      <div class="totals">
        <div class="t-row"><span>Boshlang'ich naqd</span><span>{{ fmt(closedReport.openingCash) }} so'm</span></div>
        <div class="t-row"><span>Kutilgan naqd</span><span>{{ fmt(closedReport.expectedCash) }} so'm</span></div>
        <div class="t-row"><span>Sanalgan naqd</span><span>{{ fmt(closedReport.countedCash) }} so'm</span></div>
        <div class="t-row grand">
          <span>Farq</span>
          <span :style="{ color: closedReport.cashDifference < 0 ? 'var(--danger)' : 'var(--success)' }">
            {{ closedReport.cashDifference >= 0 ? '+' : '' }}{{ fmt(closedReport.cashDifference) }} so'm
          </span>
        </div>
      </div>
    </div>
  </div>

  <!-- Tarix -->
  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Ochilgan</th>
          <th>Yopilgan</th>
          <th>Holat</th>
          <th class="text-right">Sotuv</th>
          <th class="text-right">Qaytarish</th>
          <th class="text-right">Farq</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in shifts" :key="s.id">
          <td>{{ fmtDate(s.openedAt) }}</td>
          <td class="muted">{{ fmtDate(s.closedAt) }}</td>
          <td>
            <span class="pill" :class="s.status === 'Open' ? 'pill-amber' : 'pill-gray'">
              {{ s.status === 'Open' ? 'Ochiq' : 'Yopilgan' }}
            </span>
          </td>
          <td class="text-right">{{ fmt(s.totalSales) }}</td>
          <td class="text-right muted">{{ fmt(s.totalRefunds) }}</td>
          <td class="text-right">{{ s.status === 'Closed' ? fmt(s.cashDifference) : '—' }}</td>
        </tr>
        <tr v-if="!shifts.length && !loading">
          <td colspan="6" class="muted" style="text-align: center; padding: 24px">Hozircha smena yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.report-section {
  margin-bottom: 12px;
}
.report-label {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--muted, #888);
  margin-bottom: 4px;
  padding-top: 8px;
  border-top: 1px solid var(--border, #e5e7eb);
}
.report-section:first-child .report-label {
  border-top: none;
  padding-top: 0;
}
</style>
