<script setup>
import { localDateStr } from '../utils/date'
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { loyaltyApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import TransactionsTab from './loyalty/TransactionsTab.vue'
import HistoryTab from './loyalty/HistoryTab.vue'
import { settingsToPayload } from './loyalty/settingsPayload'

// Tablar — Sozlamalar · Bonus harakatlari · O'zgarishlar tarixi
const TABS = [
  { id: 'settings', text: 'Sozlamalar' },
  { id: 'transactions', text: 'Bonus harakatlari' },
  { id: 'history', text: "O'zgarishlar tarixi" }
]
const tab = ref('settings')
const route = useRoute()
const router = useRouter()

// Sozlamalar bu yerda faqat ko'rsatiladi; tahrirlash — alohida sahifada (/loyalty/settings)
const settings = ref(null)
const loading = ref(false)
const toggling = ref(false)
const error = ref('')
const success = ref(route.query.saved ? 'Sozlamalar saqlandi' : '')
if (route.query.saved) router.replace({ query: {} })

const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtPct = (n) => `${Number(n) || 0}%`

const summary = computed(() => {
  const s = settings.value
  if (!s) return []
  const rates = s.categoryRates || []
  const tiers = [...(s.tiers || [])].sort((a, b) => Number(a.minSpend) - Number(b.minSpend))
  return [
    { label: 'Cashback', value: fmtPct(s.cashbackPercent) },
    { label: 'Minimal savdo summasi', value: Number(s.minSaleAmount) ? `${fmtNum(s.minSaleAmount)} so'm` : 'Cheklovsiz' },
    { label: "Ball bilan to'lash — maks.", value: fmtPct(s.maxRedeemPercent) },
    { label: 'Ball muddati', value: s.pointsExpireDays ? `${s.pointsExpireDays} kun` : 'Muddatsiz' },
    { label: "Ro'yxatdan o'tish bonusi", value: `${fmtNum(s.signupBonus)} ball` },
    { label: 'Chegirmali mahsulotlarga ball', value: s.earnOnDiscountedItems !== false ? 'Ha' : "Yo'q" },
    {
      label: 'Kategoriya foizlari',
      value: rates.length ? rates.map((r) => `${r.categoryName} — ${fmtPct(r.percent)}`).join(', ') : "Yo'q (umumiy foiz)"
    },
    {
      label: 'Darajalar',
      value: tiers.length ? tiers.map((t) => `${t.name} — ${fmtPct(t.cashbackPercent)}`).join(', ') : "Yo'q"
    }
  ]
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    settings.value = await loyaltyApi.getSettings()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function toggleEnabled() {
  if (!settings.value) return
  const next = !settings.value.enabled
  const q = next
    ? 'Cashback tizimi faollashtirilsinmi?'
    : "Cashback tizimi passiv qilinsinmi? Yangi ball yozilmaydi va ball bilan to'lab bo'lmaydi."
  if (!window.confirm(q)) return
  toggling.value = true
  error.value = ''
  success.value = ''
  try {
    const saved = await loyaltyApi.saveSettings(settingsToPayload(settings.value, { enabled: next }))
    settings.value = saved && typeof saved === 'object' ? saved : { ...settings.value, enabled: next }
    success.value = next ? 'Cashback faollashtirildi' : 'Cashback passiv qilindi'
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    toggling.value = false
  }
}

const edit = () => router.push({ name: 'loyalty-settings' })

function daysAgo(n) {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return localDateStr(d)
}
const period = reactive({ from: daysAgo(29), to: daysAgo(0) })
const report = ref(null)
const reportLoading = ref(false)
const reportError = ref('')

async function loadReport() {
  reportLoading.value = true
  reportError.value = ''
  try {
    report.value = await loyaltyApi.report({ from: period.from, to: period.to })
  } catch (e) {
    reportError.value = errorMessage(e)
  } finally {
    reportLoading.value = false
  }
}

onMounted(() => {
  load()
  loadReport()
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Cashback va bonus</h1>
      <div class="muted small">
        Har xariddan mijozga ball yoziladi: 1 ball = 1 so'm. Mijoz mobil ilovadagi QR bonus kartasini kassada skanerlatadi —
        ball yig'iladi yoki keyingi xaridda to'lov sifatida ishlatiladi. Do'kon va restoran savdolariga, online buyurtmalarga ham amal qiladi.
      </div>
    </div>
  </div>

  <div class="row" style="gap:8px; margin-bottom:12px">
    <button
      v-for="t in TABS"
      :key="t.id"
      class="btn btn-ghost btn-sm"
      :class="{ 'btn-active': tab === t.id }"
      @click="tab = t.id"
    >{{ t.text }}</button>
  </div>

  <TransactionsTab v-if="tab === 'transactions'" />
  <HistoryTab v-else-if="tab === 'history'" />

  <template v-else>
    <div v-if="error" class="alert alert-error">{{ error }}</div>
    <div v-if="success" class="alert alert-success">{{ success }}</div>

    <!-- Joriy sozlamalar (faqat ko'rish) -->
    <div class="card" style="margin-bottom:16px">
      <div class="report-head" style="align-items:center">
        <div class="row" style="gap:10px; align-items:center; flex:none">
          <h3 style="margin:0">Joriy sozlamalar</h3>
          <span v-if="settings" class="status" :class="settings.enabled ? 'on' : 'off'">
            {{ settings.enabled ? 'Faol' : 'Passiv' }}
          </span>
        </div>
        <div v-if="settings" class="row" style="gap:8px; flex:none">
          <button class="btn btn-ghost" :class="{ danger: settings.enabled }" :disabled="toggling" @click="toggleEnabled">
            {{ toggling ? '...' : settings.enabled ? 'Passiv qilish' : 'Faollashtirish' }}
          </button>
          <button class="btn" @click="edit">Tahrirlash</button>
        </div>
      </div>
      <div v-if="loading" class="muted" style="margin-top:12px">Yuklanmoqda...</div>
      <table v-else-if="settings" class="table" style="margin-top:12px">
        <tbody>
          <tr v-for="r in summary" :key="r.label">
            <td class="muted" style="width:280px">{{ r.label }}</td>
            <td><strong>{{ r.value }}</strong></td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Hisobot -->
    <div class="card" style="margin-bottom:16px">
      <div class="report-head">
        <h3 style="margin:0">Hisobot</h3>
        <div class="row" style="align-items:flex-end">
          <div class="field" style="margin:0"><label>Dan</label><input v-model="period.from" type="date" class="input" /></div>
          <div class="field" style="margin:0"><label>Gacha</label><input v-model="period.to" type="date" class="input" /></div>
          <button class="btn" :disabled="reportLoading" @click="loadReport">{{ reportLoading ? '...' : "Ko'rsatish" }}</button>
        </div>
      </div>
      <div v-if="reportError" class="alert alert-error" style="margin-top:12px">{{ reportError }}</div>
      <div v-if="report" class="kpi-grid">
        <div class="kpi">
          <div class="kpi-value" style="color:var(--success)">{{ fmtNum(report.earned) }}</div>
          <div class="kpi-label">Yozilgan ball</div>
        </div>
        <div class="kpi">
          <div class="kpi-value" style="color:var(--primary)">{{ fmtNum(report.redeemed) }}</div>
          <div class="kpi-label">Sarflangan ball</div>
        </div>
        <div class="kpi">
          <div class="kpi-value" style="color:var(--danger)">{{ fmtNum(report.expired) }}</div>
          <div class="kpi-label">Muddati o'tgan</div>
        </div>
        <div class="kpi">
          <div class="kpi-value">{{ fmtNum(report.activeCustomers) }}</div>
          <div class="kpi-label">Faol mijozlar</div>
        </div>
        <div class="kpi">
          <div class="kpi-value">{{ fmtNum(report.outstandingPoints) }}</div>
          <div class="kpi-label">Mijozlardagi qoldiq ball (so'm)</div>
        </div>
      </div>
    </div>
  </template>
</template>

<style scoped>
.small { font-size: 12px; }
.btn-active { background: var(--primary); color: #fff; }
.report-head { display:flex; justify-content:space-between; align-items:flex-end; gap:12px; flex-wrap:wrap; }
.status { font-size:12px; font-weight:700; padding:3px 10px; border-radius:999px; }
.status.on { background:#dcfce7; color:#166534; }
.status.off { background:#fee2e2; color:#991b1b; }
.kpi-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(170px, 1fr)); gap:12px; margin-top:16px; }
.kpi { padding:14px; border:1px solid var(--border, #e5e7eb); border-radius:10px; }
.kpi-value { font-size:22px; font-weight:800; line-height:1.1; }
.kpi-label { color:var(--muted); margin-top:6px; font-weight:600; font-size:13px; }
</style>
