<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import { loyaltyApi, categoriesApi } from '../../api/catalog'
import { errorMessage } from '../../api/http'
import AppIcon from '../../components/AppIcon.vue'
import { settingsToPayload } from './settingsPayload'

// Cashback sozlamalarini tahrirlash — alohida sahifa (/loyalty/settings).
// Ro'yxat sahifasidan "Tahrirlash" orqali kiriladi; Saqlash/Bekor qilish bilan qaytadi.
const router = useRouter()
const loading = ref(false)
const saving = ref(false)
const error = ref('')

const form = reactive({
  enabled: true,
  cashbackPercent: 1,
  minSaleAmount: 0,
  maxRedeemPercent: 50,
  pointsExpireDays: '',
  signupBonus: 0,
  earnOnDiscountedItems: true
})
const rates = ref([])
const tiers = ref([])
const categories = ref([])
const newCategoryId = ref('')
const newPercent = ref(2)

// Saqlanmagan o'zgarishlarni aniqlash uchun boshlang'ich holat
const initial = ref('')
const snapshot = () => JSON.stringify(buildPayload())
const dirty = computed(() => !!initial.value && snapshot() !== initial.value)
let leavingAfterSave = false

const availableCategories = computed(() => {
  const used = new Set(rates.value.map((r) => r.categoryId))
  return categories.value.filter((c) => !used.has(c.id))
})

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
  tiers.value = [...(s.tiers || [])]
    .sort((a, b) => Number(a.minSpend) - Number(b.minSpend))
    .map((t) => ({ id: t.id || null, name: t.name || '', minSpend: Number(t.minSpend), cashbackPercent: Number(t.cashbackPercent) }))
}

function buildPayload() {
  return settingsToPayload({ ...form, categoryRates: rates.value, tiers: tiers.value })
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [s, cats] = await Promise.all([loyaltyApi.getSettings(), categoriesApi.list()])
    applySettings(s)
    categories.value = Array.isArray(cats) ? cats : cats?.items || []
    initial.value = snapshot()
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
const removeRate = (i) => rates.value.splice(i, 1)

function addTier() {
  const last = tiers.value[tiers.value.length - 1]
  tiers.value.push({
    id: null,
    name: '',
    minSpend: last ? Number(last.minSpend) * 2 || 1000000 : 1000000,
    cashbackPercent: last ? Number(last.cashbackPercent) + 1 : Number(form.cashbackPercent) + 1
  })
}
const removeTier = (i) => tiers.value.splice(i, 1)

const inRange = (v, min, max) => Number.isFinite(v) && v >= min && v <= max

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
  const seen = new Set()
  for (const t of tiers.value) {
    const name = (t.name || '').trim()
    if (!name || name.length > 30) return 'Daraja nomi 1–30 belgi bo\'lishi kerak'
    if (!(Number(t.minSpend) >= 0)) return `"${name}" minimal xaridi manfiy bo'lmasin`
    if (!inRange(Number(t.cashbackPercent), 0, 100)) return `"${name}" foizi 0..100 oralig'ida bo'lishi kerak`
    if (seen.has(Number(t.minSpend))) return 'Ikki darajaning minimal xaridi bir xil bo\'lmasin'
    seen.add(Number(t.minSpend))
  }
  return ''
}

async function save() {
  error.value = ''
  const v = validate()
  if (v) { error.value = v; window.scrollTo({ top: 0, behavior: 'smooth' }); return }
  saving.value = true
  try {
    await loyaltyApi.saveSettings(buildPayload())
    leavingAfterSave = true
    router.push({ name: 'loyalty', query: { saved: '1' } })
  } catch (e) {
    error.value = errorMessage(e)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}

const cancel = () => router.push({ name: 'loyalty' })

onBeforeRouteLeave(() => {
  if (leavingAfterSave || !dirty.value) return true
  return window.confirm('Saqlanmagan o\'zgarishlar bor. Chiqib ketilsinmi?')
})

onMounted(load)
</script>

<template>
  <div class="page-head">
    <div>
      <button class="btn btn-ghost btn-sm" style="margin-bottom:8px" @click="cancel">← Orqaga</button>
      <h1>Cashback sozlamalarini tahrirlash</h1>
      <div class="muted small">1 ball = 1 so'm. O'zgarishlar "Saqlash" bosilgandan keyin kuchga kiradi.</div>
    </div>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="loading" class="card muted">Yuklanmoqda...</div>

  <template v-else>
    <!-- Holat -->
    <div class="card section">
      <h3>Holat</h3>
      <div class="toggle-row">
        <label class="toggle"><input v-model="form.enabled" type="radio" :value="true" /> Faol</label>
        <label class="toggle"><input v-model="form.enabled" type="radio" :value="false" /> Passiv</label>
      </div>
      <span class="hint">Passiv bo'lsa, yangi ball yozilmaydi va ball bilan to'lab bo'lmaydi. Mijozlardagi ballar saqlanib qoladi.</span>
    </div>

    <!-- Asosiy -->
    <div class="card section">
      <h3>Asosiy qoidalar</h3>
      <div class="row">
        <div class="field">
          <label>Cashback (%)</label>
          <input v-model="form.cashbackPercent" type="number" min="0" max="100" step="any" class="input" />
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
    </div>

    <!-- Kategoriya stavkalari -->
    <div class="card section">
      <h3>Kategoriya bo'yicha foiz</h3>
      <div class="muted small" style="margin-bottom:10px">Belgilangan kategoriyadagi mahsulotlar uchun umumiy foiz o'rniga shu foiz qo'llanadi.</div>
      <table class="table" style="margin-bottom:12px">
        <thead><tr><th>Kategoriya</th><th class="text-right" style="width:160px">Foiz (%)</th><th style="width:60px"></th></tr></thead>
        <tbody>
          <tr v-for="(r, i) in rates" :key="r.categoryId">
            <td>{{ r.categoryName }}</td>
            <td class="text-right"><input v-model="r.percent" type="number" min="0" max="100" step="any" class="input" style="width:110px; text-align:right" /></td>
            <td class="text-right"><button class="btn btn-icon danger" title="Olib tashlash" @click="removeRate(i)"><AppIcon name="x" :size="15" /></button></td>
          </tr>
          <tr v-if="!rates.length">
            <td colspan="3" class="muted" style="text-align:center; padding:16px">Barcha kategoriyalarga umumiy foiz qo'llanadi</td>
          </tr>
        </tbody>
      </table>
      <div class="row" style="align-items:flex-end">
        <div class="field" style="margin:0">
          <label>Kategoriya</label>
          <select v-model="newCategoryId" class="input">
            <option value="">— tanlang —</option>
            <option v-for="c in availableCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="field" style="margin:0; max-width:140px">
          <label>Foiz (%)</label>
          <input v-model="newPercent" type="number" min="0" max="100" step="any" class="input" />
        </div>
        <button class="btn btn-ghost" :disabled="!newCategoryId" @click="addRate">+ Qo'shish</button>
      </div>
    </div>

    <!-- Darajalar -->
    <div class="card section">
      <h3>Darajalar</h3>
      <div class="muted small" style="margin-bottom:10px">
        Mijozning oxirgi 365 kundagi xaridi minimal summadan oshsa, shu daraja foizi qo'llanadi
        (umumiy yoki kategoriya foizidan qaysi biri katta bo'lsa — o'shasi). Masalan: Silver, Gold, Platinum.
      </div>
      <table class="table" style="margin-bottom:12px">
        <thead>
          <tr>
            <th>Nomi</th>
            <th class="text-right" style="width:220px">Minimal xarid, 365 kun (so'm)</th>
            <th class="text-right" style="width:160px">Cashback (%)</th>
            <th style="width:60px"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(t, i) in tiers" :key="i">
            <td><input v-model="t.name" class="input" maxlength="30" placeholder="Gold" /></td>
            <td class="text-right"><input v-model="t.minSpend" type="number" min="0" step="1000" class="input" style="width:180px; text-align:right" /></td>
            <td class="text-right"><input v-model="t.cashbackPercent" type="number" min="0" max="100" step="any" class="input" style="width:110px; text-align:right" /></td>
            <td class="text-right"><button class="btn btn-icon danger" title="Olib tashlash" @click="removeTier(i)"><AppIcon name="x" :size="15" /></button></td>
          </tr>
          <tr v-if="!tiers.length">
            <td colspan="4" class="muted" style="text-align:center; padding:16px">Darajalar yo'q — barcha mijozlarga bir xil foiz</td>
          </tr>
        </tbody>
      </table>
      <button class="btn btn-ghost" @click="addTier">+ Daraja qo'shish</button>
    </div>

    <!-- Pastki panel: doim ko'rinib turadi -->
    <div class="save-bar">
      <span v-if="dirty" class="muted small">Saqlanmagan o'zgarishlar bor</span>
      <span v-else class="muted small">O'zgarish yo'q</span>
      <div class="row" style="gap:8px; flex:none">
        <button class="btn btn-ghost" :disabled="saving" @click="cancel">Bekor qilish</button>
        <button class="btn" :disabled="saving || !dirty" @click="save">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </div>
    </div>
  </template>
</template>

<style scoped>
.small { font-size: 12px; }
.hint { font-size: 12px; color: var(--muted); }
.section { margin-bottom: 16px; }
.section h3 { margin-top: 0; }
.save-bar {
  position: sticky; bottom: 0; z-index: 5;
  display: flex; justify-content: space-between; align-items: center; gap: 12px;
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
  padding: 12px 20px; box-shadow: 0 -4px 12px rgba(0, 0, 0, .06);
}
</style>
