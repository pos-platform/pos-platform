<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { auditApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const fails = ref([])
const loading = ref(false)
const err = ref('')
const options = ref({ organizations: [], terminals: [] })

/** Fiskal moduldan qaytgan xom xato matnini kassir tushunadigan sababga o'giradi. */
function fiscalHint(msg) {
  const m = (msg || '').toLowerCase()
  if (m.includes('time is in the past') || m.includes('receipt time'))
    return "Chek vaqti moduldagi oxirgi chekdan orqada — kompyuter soati/vaqt mintaqasini tekshiring."
  if (m.includes('send_all_receipts_first') || m.includes('locked_sync'))
    return "Fiskal modul to'lgan — cheklar OFD serverga sinxronlanishi kerak (internetni tekshiring)."
  if (m.includes('unsupported applet') || m.includes('65525'))
    return "Modul appleti (0400) mos drayver bilan ishlamayapti — FiscalDriveService ulanishini tekshiring."
  if ((m.includes('list') && m.includes("bo'sh")) || m.includes('topilmadi'))
    return "Fiskal modul ulanmagan yoki drayver ko'rmayapti — fleshka/kabelni tekshiring."
  if (m.includes('spic') || m.includes('mxik'))
    return "Mahsulot MXIK/SPIC kodi noto'g'ri yoki yo'q — tovar kartochkasini to'ldiring."
  if (m.includes('http') || m.includes('connection') || m.includes('refused'))
    return "Drayver (FiscalDriveService) javob bermayapti — dastur ishga tushganini tekshiring."
  return '' // notanish xato — faqat xom matn ko'rsatiladi
}

/** Xato matnidan qisqa turkum — "Sabab turi" filtri uchun (mijoz tomonda hisoblanadi). */
function errorKind(msg) {
  const m = (msg || '').toLowerCase()
  if (m.includes('time is in the past') || m.includes('receipt time')) return 'time'
  if (m.includes('send_all_receipts_first') || m.includes('locked_sync')) return 'full'
  if ((m.includes('list') && m.includes("bo'sh")) || m.includes('topilmadi')) return 'nomodule'
  if (m.includes('unsupported applet') || m.includes('65525')) return 'applet'
  if (m.includes('spic') || m.includes('mxik')) return 'mxik'
  if (m.includes('http') || m.includes('connection') || m.includes('refused')) return 'driver'
  return 'other'
}

const errorKinds = [
  ['all', 'Barcha sabablar'],
  ['nomodule', 'Modul ulanmagan'],
  ['time', 'Vaqt orqada (9030)'],
  ['full', "Modul to'lgan (9031)"],
  ['applet', 'Applet/drayver (65525)'],
  ['mxik', 'MXIK/SPIC xatosi'],
  ['driver', 'Drayver javob bermayapti'],
  ['other', 'Boshqa']
]

// ── Filtrlar ────────────────────────────────────────────────────────────────
const f = reactive({
  period: 'all',       // today | yesterday | 7d | 30d | custom | all
  dateFrom: '',
  dateTo: '',
  status: 'all',       // all | Failed | Retrying
  organizationId: '',
  terminalId: '',
  search: '',
  minAmount: '',
  maxAmount: '',
  kind: 'all',
  take: 200
})

const periods = [
  ['today', 'Bugun'],
  ['yesterday', 'Kecha'],
  ['7d', '7 kun'],
  ['30d', '30 kun'],
  ['custom', 'Oraliq'],
  ['all', 'Hammasi']
]

function dayStart(d) { const x = new Date(d); x.setHours(0, 0, 0, 0); return x }
function dayEnd(d) { const x = new Date(d); x.setHours(23, 59, 59, 999); return x }

/** Tanlangan davrni {from, to} sanalariga o'giradi. */
function periodRange() {
  const now = new Date()
  if (f.period === 'today') return { from: dayStart(now), to: dayEnd(now) }
  if (f.period === 'yesterday') {
    const y = new Date(now); y.setDate(y.getDate() - 1)
    return { from: dayStart(y), to: dayEnd(y) }
  }
  if (f.period === '7d') {
    const s = new Date(now); s.setDate(s.getDate() - 6)
    return { from: dayStart(s), to: dayEnd(now) }
  }
  if (f.period === '30d') {
    const s = new Date(now); s.setDate(s.getDate() - 29)
    return { from: dayStart(s), to: dayEnd(now) }
  }
  if (f.period === 'custom') {
    return {
      from: f.dateFrom ? dayStart(new Date(`${f.dateFrom}T00:00:00`)) : null,
      to: f.dateTo ? dayEnd(new Date(`${f.dateTo}T00:00:00`)) : null
    }
  }
  return { from: null, to: null }
}

function buildParams() {
  const { from, to } = periodRange()
  const p = { take: f.take }
  if (from) p.from = from.toISOString()
  if (to) p.to = to.toISOString()
  if (f.status !== 'all') p.status = f.status
  if (f.organizationId) p.organizationId = f.organizationId
  if (f.terminalId) p.terminalId = f.terminalId
  if (f.search.trim()) p.search = f.search.trim()
  if (f.minAmount !== '') p.minAmount = f.minAmount
  if (f.maxAmount !== '') p.maxAmount = f.maxAmount
  return p
}

// Sabab turi bo'yicha filtr — xato matni serverda turkumlanmagani uchun mijozda.
const shown = computed(() =>
  f.kind === 'all' ? fails.value : fails.value.filter((x) => errorKind(x.errorMessage) === f.kind)
)

const totalAmount = computed(() => shown.value.reduce((s, x) => s + Number(x.totalAmount || 0), 0))

const activeCount = computed(() => {
  let n = 0
  if (f.period !== 'all') n++
  if (f.status !== 'all') n++
  if (f.organizationId) n++
  if (f.terminalId) n++
  if (f.search.trim()) n++
  if (f.minAmount !== '' || f.maxAmount !== '') n++
  if (f.kind !== 'all') n++
  return n
})

function resetFilters() {
  f.period = 'all'; f.dateFrom = ''; f.dateTo = ''
  f.status = 'all'; f.organizationId = ''; f.terminalId = ''
  f.search = ''; f.minAmount = ''; f.maxAmount = ''; f.kind = 'all'; f.take = 200
}

function fmt(d) { return new Date(d).toLocaleString('uz-UZ') }
function money(n) { return Number(n).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    fails.value = await auditApi.fiscalFailures(buildParams())
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function loadOptions() {
  try {
    options.value = await auditApi.fiscalFilterOptions()
  } catch {
    // filtr ro'yxatlari yuklanmasa ham jadval ishlaydi
  }
}

let timer = null
function reload(delay = 0) {
  clearTimeout(timer)
  timer = setTimeout(load, delay)
}
watch(() => [f.period, f.dateFrom, f.dateTo, f.status, f.organizationId, f.terminalId, f.take], () => reload(0))
watch(() => [f.search, f.minAmount, f.maxAmount], () => reload(400))

onMounted(() => { load(); loadOptions() })
</script>

<template>
  <div class="page-head">
    <h1>Fiskal audit</h1>
    <button class="btn btn-ghost btn-sm" @click="() => { load(); loadOptions() }">↻ Yangilash</button>
  </div>
  <p class="muted sub">
    Fiskalizatsiya muvaffaqiyatsiz tugagan cheklar — QR kod chiqmagan savdolar, fiskal modul
    qaytargan aniq xato matni bilan (barcha tashkilotlar bo'yicha).
  </p>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="filters">
    <button v-for="p in periods" :key="p[0]" class="chip"
            :class="{ 'chip-on': f.period === p[0] }" @click="f.period = p[0]">
      {{ p[1] }}
    </button>
    <template v-if="f.period === 'custom'">
      <input type="date" class="inp" v-model="f.dateFrom" />
      <span class="muted">—</span>
      <input type="date" class="inp" v-model="f.dateTo" />
    </template>
  </div>

  <div class="filters">
    <select class="inp" v-model="f.status">
      <option value="all">Holat: hammasi</option>
      <option value="Retrying">Qayta urinilmoqda</option>
      <option value="Failed">To'xtagan</option>
    </select>
    <select class="inp" v-model="f.organizationId">
      <option value="">Tashkilot: hammasi</option>
      <option v-for="o in options.organizations" :key="o.id" :value="o.id">{{ o.name }}</option>
    </select>
    <select class="inp" v-model="f.terminalId">
      <option value="">Kassa: hammasi</option>
      <option v-for="t in options.terminals" :key="t" :value="t">{{ t }}</option>
    </select>
    <select class="inp" v-model="f.kind">
      <option v-for="k in errorKinds" :key="k[0]" :value="k[0]">{{ k[1] }}</option>
    </select>
    <select class="inp" v-model.number="f.take">
      <option :value="100">100 ta</option>
      <option :value="200">200 ta</option>
      <option :value="500">500 ta</option>
      <option :value="1000">1000 ta</option>
    </select>
  </div>

  <div class="filters">
    <input class="inp" style="width:260px" v-model="f.search" placeholder="Chek raqami yoki xato matni…" />
    <input class="inp" style="width:130px" type="number" v-model="f.minAmount" placeholder="Summa dan" />
    <input class="inp" style="width:130px" type="number" v-model="f.maxAmount" placeholder="Summa gacha" />
    <button v-if="activeCount" class="chip chip-reset" @click="resetFilters">✕ Tozalash ({{ activeCount }})</button>
    <span class="muted count">{{ shown.length }} ta chek · {{ money(totalAmount) }} so'm</span>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Vaqt</th><th>Holat</th><th>Chek</th><th>Tashkilot</th><th>Kassa</th>
          <th style="text-align:right">Summa</th><th>Xato sababi</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="x in shown" :key="x.saleId">
          <td class="muted nowrap">{{ fmt(x.failedAt) }}</td>
          <td class="nowrap">
            <span class="st" :class="x.status === 'Retrying' ? 'st-retry' : 'st-fail'">
              {{ x.status === 'Retrying' ? 'Qayta urinilmoqda' : "To'xtagan" }}
            </span>
            <span v-if="x.retryCount" class="muted rc">×{{ x.retryCount }}</span>
          </td>
          <td class="nowrap"><b>{{ x.receiptNumber }}</b></td>
          <td class="muted">{{ x.organizationName || '—' }}</td>
          <td class="muted">{{ x.terminalId || '—' }}</td>
          <td class="nowrap" style="text-align:right">{{ money(x.totalAmount) }}</td>
          <td>
            <div class="fiscal-raw">{{ x.errorMessage || 'Xato matni saqlanmagan' }}</div>
            <div v-if="fiscalHint(x.errorMessage)" class="fiscal-hint">↳ {{ fiscalHint(x.errorMessage) }}</div>
          </td>
        </tr>
        <tr v-if="!shown.length && !loading">
          <td colspan="7" class="muted" style="text-align:center;padding:28px">
            Tanlangan filtrlar bo'yicha fiskalizatsiya xatosi yo'q 🎉
          </td>
        </tr>
        <tr v-if="loading">
          <td colspan="7" class="muted" style="text-align:center;padding:28px">Yuklanmoqda…</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.sub { margin: 2px 0 16px; font-size: 13.5px; }
.filters { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; align-items: center; }
.chip { padding: 6px 14px; border-radius: 20px; border: 1px solid var(--border); background: #f8fafc; cursor: pointer; font-size: 13px; font-weight: 600; color: var(--muted); }
.chip-on { background: #2563eb; color: #fff; border-color: #2563eb; }
.chip-reset { background: #fee2e2; color: #b91c1c; border-color: #fecaca; }
.inp { padding: 6px 10px; border: 1px solid var(--border); border-radius: 8px; font-size: 13px; background: #fff; color: inherit; }
.count { font-size: 12.5px; margin-left: auto; }
.nowrap { white-space: nowrap; }
.fiscal-raw { font-family: ui-monospace, 'Cascadia Code', monospace; font-size: 12.5px; color: #b91c1c; word-break: break-word; }
.fiscal-hint { font-size: 12.5px; color: var(--muted); margin-top: 3px; }
.st { padding: 2px 8px; border-radius: 6px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.st-retry { background: #fef3c7; color: #92400e; }
.st-fail { background: #fee2e2; color: #b91c1c; }
.rc { font-size: 12px; margin-left: 6px; }
</style>
