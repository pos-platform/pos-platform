<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { loyaltyApi, categoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// Cashback va bonus karta sozlamalari (tashkilot bo'yicha). 1 ball = 1 so'm.
// Hisoblash faqat serverda — bu yerda faqat sozlash va hisobot.
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

const form = reactive({
  enabled: true,
  cashbackPercent: 1,
  minSaleAmount: 0,
  maxRedeemPercent: 50,
  pointsExpireDays: '', // bo'sh — muddatsiz
  signupBonus: 0,
  earnOnDiscountedItems: true
})
const rates = ref([]) // [{ categoryId, categoryName, percent }]

const categories = ref([])
const newCategoryId = ref('')
const newPercent = ref(2)

// Hali stavka berilmagan kategoriyalar
const availableCategories = computed(() => {
  const used = new Set(rates.value.map((r) => r.categoryId))
  return categories.value.filter((c) => !used.has(c.id))
})

function daysAgo(n) {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return d.toISOString().slice(0, 10)
}
const period = reactive({ from: daysAgo(29), to: daysAgo(0) })
const report = ref(null)
const reportLoading = ref(false)
const reportError = ref('')

const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')

function applySettings(s) {
  form.enabled = !!s.enabled
  form.cashbackPercent = Number(s.cashbackPercent ?? 0)
  form.minSaleAmount = Number(s.minSaleAmount ?? 0)
  form.maxRedeemPercent = Number(s.maxRedeemPercent ?? 0)
  form.pointsExpireDays = s.pointsExpireDays ?? ''
  form.signupBonus = Number(s.signupBonus ?? 0)
  form.earnOnDiscountedItems = s.earnOnDiscountedItems !== false
  rates.value = (s.categoryRates || []).map((r) => ({
    categoryId: r.categoryId,
    categoryName: r.categoryName,
    percent: Number(r.percent)
  }))
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [s, cats] = await Promise.all([loyaltyApi.getSettings(), categoriesApi.list()])
    applySettings(s)
    categories.value = Array.isArray(cats) ? cats : cats?.items || []
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function addRate() {
  const cat = categories.value.find((c) => c.id === newCategoryId.value)
  if (!cat) return
  rates.value.push({ categoryId: cat.id, categoryName: cat.name, percent: Number(newPercent.value) || 0 })
  newCategoryId.value = ''
}

function removeRate(i) {
  rates.value.splice(i, 1)
}

const inRange = (v, min, max) => Number.isFinite(v) && v >= min && v <= max

// Kontraktdagi validatsiyani oldindan tekshirish (server baribir tekshiradi)
function validate() {
  if (!inRange(Number(form.cashbackPercent), 0, 100)) return 'Cashback foizi 0..100 oralig\'ida bo\'lishi kerak'
  if (!inRange(Number(form.maxRedeemPercent), 0, 100)) return 'Ball bilan to\'lash ulushi 0..100 oralig\'ida bo\'lishi kerak'
  if (Number(form.minSaleAmount) < 0) return 'Minimal savdo summasi manfiy bo\'lmasin'
  if (Number(form.signupBonus) < 0) return 'Ro\'yxatdan o\'tish bonusi manfiy bo\'lmasin'
  if (form.pointsExpireDays !== '' && form.pointsExpireDays !== null) {
    if (!inRange(Number(form.pointsExpireDays), 30, 3650)) return 'Ball muddati 30..3650 kun yoki bo\'sh (muddatsiz) bo\'lishi kerak'
  }
  for (const r of rates.value) {
    if (!inRange(Number(r.percent), 0, 100)) return `"${r.categoryName}" foizi 0..100 oralig'ida bo'lishi kerak`
  }
  return ''
}

async function save() {
  error.value = ''
  success.value = ''
  const v = validate()
  if (v) { error.value = v; return }
  saving.value = true
  try {
    const payload = {
      enabled: form.enabled,
      cashbackPercent: Number(form.cashbackPercent),
      minSaleAmount: Number(form.minSaleAmount),
      maxRedeemPercent: Number(form.maxRedeemPercent),
      pointsExpireDays: form.pointsExpireDays === '' || form.pointsExpireDays === null ? null : Number(form.pointsExpireDays),
      signupBonus: Number(form.signupBonus),
      earnOnDiscountedItems: form.earnOnDiscountedItems,
      categoryRates: rates.value.map((r) => ({ categoryId: r.categoryId, percent: Number(r.percent) }))
    }
    const saved = await loyaltyApi.saveSettings(payload)
    if (saved && typeof saved === 'object') applySettings(saved)
    success.value = 'Sozlamalar saqlandi'
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

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

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Hisobot -->
  <div class="card" style="margin-bottom:16px">
    <div class="report-head">
      <h3 style="margin:0">Hisobot</h3>
      <div class="row" style="align-items:flex-end">
        <div class="field" style="margin:0"><label>Dan</label><input v-model="period.from" type="date" class="input" /></div>
        <div class="field" style="margin:0"><label>Gacha</label><input v-model="period.to" type="date" class="input" /></div>
        <button class="btn" :disabled="reportLoading" @click="loadReport">{{ reportLoading ? '...' : 'Ko\'rsatish' }}</button>
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

  <!-- Sozlamalar -->
  <div class="card" style="margin-bottom:16px">
    <h3 style="margin-top:0">Sozlamalar</h3>
    <div v-if="loading" class="muted">Yuklanmoqda...</div>
    <template v-else>
      <label class="checkbox" style="margin-bottom:14px">
        <input v-model="form.enabled" type="checkbox" />
        <strong>Cashback tizimi yoqilgan</strong>
      </label>

      <div class="row">
        <div class="field">
          <label>Cashback (%)</label>
          <input v-model="form.cashbackPercent" type="number" min="0" max="100" step="0.1" class="input" />
          <span class="hint">Xarid summasidan qancha foiz ball yoziladi (kategoriya stavkasi bo'lmasa)</span>
        </div>
        <div class="field">
          <label>Minimal savdo summasi (so'm)</label>
          <input v-model="form.minSaleAmount" type="number" min="0" class="input" />
          <span class="hint">Bundan kichik chekka ball yozilmaydi (0 — cheklovsiz)</span>
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label>Ball bilan to'lash — maks. (%)</label>
          <input v-model="form.maxRedeemPercent" type="number" min="0" max="100" class="input" />
          <span class="hint">Chek summasining necha foizini ball bilan yopish mumkin</span>
        </div>
        <div class="field">
          <label>Ball muddati (kun)</label>
          <input v-model="form.pointsExpireDays" type="number" min="30" max="3650" class="input" placeholder="Muddatsiz" />
          <span class="hint">Mijoz shuncha kun xarid qilmasa, ballari kuyadi. Bo'sh — muddatsiz (30..3650)</span>
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label>Ro'yxatdan o'tish bonusi (ball)</label>
          <input v-model="form.signupBonus" type="number" min="0" class="input" />
          <span class="hint">Mijoz ilovada bonus kartani birinchi ochganda bir marta beriladi</span>
        </div>
        <div class="field" style="justify-content:center">
          <label class="checkbox">
            <input v-model="form.earnOnDiscountedItems" type="checkbox" />
            <span>Chegirmali mahsulotlarga ham ball yozilsin</span>
          </label>
          <span class="hint">O'chirilsa, aksiya yoki promokod chegirmasi bor qatorlarga ball yozilmaydi</span>
        </div>
      </div>

      <!-- Kategoriya stavkalari -->
      <h4 style="margin:12px 0 6px">Kategoriya bo'yicha foiz</h4>
      <div class="muted small" style="margin-bottom:10px">Belgilangan kategoriyadagi mahsulotlar uchun umumiy foiz o'rniga shu foiz qo'llanadi.</div>
      <table class="table" style="margin-bottom:12px">
        <thead><tr><th>Kategoriya</th><th class="text-right" style="width:160px">Foiz (%)</th><th style="width:60px"></th></tr></thead>
        <tbody>
          <tr v-for="(r, i) in rates" :key="r.categoryId">
            <td>{{ r.categoryName }}</td>
            <td class="text-right"><input v-model="r.percent" type="number" min="0" max="100" step="0.1" class="input" style="width:110px; text-align:right" /></td>
            <td class="text-right"><button class="btn btn-ghost btn-sm" @click="removeRate(i)">✕</button></td>
          </tr>
          <tr v-if="!rates.length">
            <td colspan="3" class="muted" style="text-align:center; padding:16px">Barcha kategoriyalarga umumiy foiz qo'llanadi</td>
          </tr>
        </tbody>
      </table>
      <div class="row" style="align-items:flex-end; margin-bottom:16px">
        <div class="field" style="margin:0">
          <label>Kategoriya</label>
          <select v-model="newCategoryId" class="input">
            <option value="">— tanlang —</option>
            <option v-for="c in availableCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="field" style="margin:0; max-width:140px">
          <label>Foiz (%)</label>
          <input v-model="newPercent" type="number" min="0" max="100" step="0.1" class="input" />
        </div>
        <button class="btn btn-ghost" :disabled="!newCategoryId" @click="addRate">+ Qo'shish</button>
      </div>

      <button class="btn" :disabled="saving" @click="save">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
    </template>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.hint { font-size: 12px; color: var(--muted); }
.report-head { display:flex; justify-content:space-between; align-items:flex-end; gap:12px; flex-wrap:wrap; }
.kpi-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(170px, 1fr)); gap:12px; margin-top:16px; }
.kpi { padding:14px; border:1px solid var(--border, #e5e7eb); border-radius:10px; }
.kpi-value { font-size:22px; font-weight:800; line-height:1.1; }
.kpi-label { color:var(--muted); margin-top:6px; font-weight:600; font-size:13px; }
</style>
