<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { auditApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const logs = ref([])
const loading = ref(false)
const err = ref('')
const filter = ref('all') // all | login | license | user | org | password | fail

const actionLabel = {
  'login.success': "Kirish (muvaffaqiyatli)",
  'login.fail': 'Kirish (xato)',
  'license.extend': 'Obuna uzaytirildi',
  'license.revoke': 'Obuna bekor qilindi',
  'license.lifetime.set': 'Doimiy litsenziya berildi',
  'license.lifetime.unset': 'Doimiy litsenziya olib tashlandi',
  'user.create': 'Foydalanuvchi yaratildi',
  'user.update': 'Foydalanuvchi tahrirlandi',
  'user.activate': 'Foydalanuvchi faollashtirildi',
  'user.deactivate': 'Foydalanuvchi bloklandi',
  'org.create': 'Tashkilot yaratildi',
  'org.activate': 'Tashkilot faollashtirildi',
  'org.deactivate': 'Tashkilot bloklandi',
  'org.businessType': "Tashkilot turi o'zgartirildi",
  'password.change': "Parol o'zgartirildi",
  'password.change.fail': "Parol o'zgartirish (xato)"
}

/** Tafsilot qatoridan xato sababini odam tushunadigan matnga o'giradi. */
function failReason(l) {
  if (l.success) return ''
  const d = l.detail || ''
  if (d.includes('sabab=bunday login topilmadi'))
    return "Bunday login tizimda ro'yxatdan o'tmagan — foydalanuvchi nomi noto'g'ri terilgan bo'lishi mumkin."
  if (d.includes("sabab=parol noto'g'ri"))
    return "Login to'g'ri, lekin parol noto'g'ri kiritilgan."
  if (d.includes('hisob bloklangan'))
    return 'Foydalanuvchi hisobi administrator tomonidan bloklangan.'
  if (d.includes('tashkilot bloklangan'))
    return 'Tashkilot platforma administratori tomonidan bloklangan.'
  if (d.includes('obuna'))
    return "Kassir/menejer obunasi faollashtirilmagan yoki muddati tugagan."
  if (d.includes('joriy parol'))
    return "Parolni o'zgartirishda joriy parol noto'g'ri kiritilgan."
  if (l.action === 'login.fail')
    return "Login topilmadi yoki parol noto'g'ri kiritilgan (eski yozuv — aniq sabab saqlanmagan)."
  return 'Amal muvaffaqiyatsiz yakunlangan.'
}

const detailLog = ref(null)

// ── Filtrlar ────────────────────────────────────────────────────────────────
const f = reactive({
  period: 'all',   // today | 7d | 30d | custom | all
  dateFrom: '',
  dateTo: '',
  status: 'all',   // all | ok | fail
  username: '',
  ip: '',
  search: '',
  take: 300
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

/** Tanlangan davrni {from, to} ISO (UTC) juftligiga o'giradi. */
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
  if (filter.value !== 'all' && filter.value !== 'fail') p.action = filter.value
  if (filter.value === 'fail') p.success = false
  else if (f.status === 'ok') p.success = true
  else if (f.status === 'fail') p.success = false
  if (f.username.trim()) p.username = f.username.trim()
  if (f.ip.trim()) p.ip = f.ip.trim()
  if (f.search.trim()) p.search = f.search.trim()
  return p
}

const activeCount = computed(() => {
  let n = 0
  if (f.period !== 'all') n++
  if (f.status !== 'all') n++
  if (f.username.trim()) n++
  if (f.ip.trim()) n++
  if (f.search.trim()) n++
  if (filter.value !== 'all') n++
  return n
})

function resetFilters() {
  f.period = 'all'; f.dateFrom = ''; f.dateTo = ''
  f.status = 'all'; f.username = ''; f.ip = ''; f.search = ''
  f.take = 300
  filter.value = 'all'
}

function fmt(d) { return new Date(d).toLocaleString('uz-UZ') }
function label(a) { return actionLabel[a] || a }
function cls(a) {
  if (a.startsWith('login')) return 'b-login'
  if (a.startsWith('license')) return 'b-license'
  if (a.startsWith('org')) return 'b-org'
  return 'b-user'
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    logs.value = await auditApi.recent(buildParams())
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Filtr o'zgarganda avtomatik qayta yuklash (matn kiritishda biroz kutib).
let timer = null
function reload(delay = 0) {
  clearTimeout(timer)
  timer = setTimeout(load, delay)
}
watch(() => [filter.value, f.period, f.dateFrom, f.dateTo, f.status, f.take], () => reload(0))
watch(() => [f.username, f.ip, f.search], () => reload(400))

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Audit jurnali</h1>
    <button class="btn btn-ghost btn-sm" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="filters">
    <button v-for="ff in [['all','Hammasi'],['fail','Xatolar'],['login','Kirishlar'],['license','Obuna'],['user','Foydalanuvchi'],['org','Tashkilot'],['password','Parol']]"
            :key="ff[0]" class="chip" :class="{ 'chip-on': filter === ff[0] }" @click="filter = ff[0]">
      {{ ff[1] }}
    </button>
  </div>

  <div class="filters">
    <button v-for="p in periods" :key="p[0]" class="chip chip-sm"
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
    <input class="inp" style="width:180px" v-model="f.username" placeholder="Foydalanuvchi…" />
    <input class="inp" style="width:160px" v-model="f.ip" placeholder="IP manzil…" />
    <input class="inp" style="width:220px" v-model="f.search" placeholder="Tafsilot/hodisa qidirish…" />
    <select class="inp" v-model="f.status" :disabled="filter === 'fail'">
      <option value="all">Holat: hammasi</option>
      <option value="ok">Faqat muvaffaqiyatli</option>
      <option value="fail">Faqat xatolar</option>
    </select>
    <select class="inp" v-model.number="f.take">
      <option :value="100">100 ta</option>
      <option :value="300">300 ta</option>
      <option :value="1000">1000 ta</option>
      <option :value="2000">2000 ta</option>
    </select>
    <button v-if="activeCount" class="chip chip-reset" @click="resetFilters">✕ Tozalash ({{ activeCount }})</button>
    <span class="muted count">{{ logs.length }} ta yozuv</span>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr><th>Vaqt</th><th>Hodisa</th><th>Foydalanuvchi</th><th>IP</th><th>Tafsilot</th><th></th></tr>
      </thead>
      <tbody>
        <tr v-for="l in logs" :key="l.id">
          <td class="muted" style="white-space:nowrap">{{ fmt(l.createdAt) }}</td>
          <td>
            <span class="badge-a" :class="cls(l.action)">{{ label(l.action) }}</span>
            <span v-if="!l.success" class="dot-fail" title="muvaffaqiyatsiz"></span>
          </td>
          <td>{{ l.actorUsername || '—' }}</td>
          <td class="muted">{{ l.ipAddress || '—' }}</td>
          <td class="muted" style="font-size:13px">{{ l.detail }}</td>
          <td style="text-align:right">
            <button class="btn btn-icon" title="Tafsilotni ko'rish" @click="detailLog = l"><AppIcon name="eye" :size="15" /></button>
          </td>
        </tr>
        <tr v-if="!logs.length && !loading">
          <td colspan="6" class="muted" style="text-align:center;padding:24px">Yozuv yo'q</td>
        </tr>
        <tr v-if="loading">
          <td colspan="6" class="muted" style="text-align:center;padding:24px">Yuklanmoqda…</td>
        </tr>
      </tbody>
    </table>
  </div>

  <div v-if="detailLog" class="modal-overlay" @click.self="detailLog = null">
    <div class="modal-box" style="max-width:480px">
      <div class="modal-head">
        <h3>Hodisa tafsiloti</h3>
        <button class="modal-x" @click="detailLog = null">×</button>
      </div>
      <div class="modal-body">
        <div v-if="!detailLog.success" class="alert alert-error" style="margin-bottom:14px">
          <strong>Xato sababi:</strong> {{ failReason(detailLog) }}
        </div>
        <div class="dl">
          <div class="dl-row"><span>Vaqt</span><b>{{ fmt(detailLog.createdAt) }}</b></div>
          <div class="dl-row">
            <span>Hodisa</span>
            <b><span class="badge-a" :class="cls(detailLog.action)">{{ label(detailLog.action) }}</span></b>
          </div>
          <div class="dl-row">
            <span>Holat</span>
            <b :style="{ color: detailLog.success ? '#15803d' : '#dc2626' }">
              {{ detailLog.success ? 'Muvaffaqiyatli' : 'Muvaffaqiyatsiz' }}
            </b>
          </div>
          <div class="dl-row"><span>Foydalanuvchi</span><b>{{ detailLog.actorUsername || '—' }}</b></div>
          <div class="dl-row"><span>IP manzil</span><b>{{ detailLog.ipAddress || '—' }}</b></div>
          <div class="dl-row"><span>Texnik tafsilot</span><b>{{ detailLog.detail || '—' }}</b></div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.filters { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; align-items: center; }
.chip { padding: 6px 14px; border-radius: 20px; border: 1px solid var(--border); background: #f8fafc; cursor: pointer; font-size: 13px; font-weight: 600; color: var(--muted); }
.chip-on { background: #2563eb; color: #fff; border-color: #2563eb; }
.chip-sm { padding: 5px 12px; font-size: 12.5px; }
.chip-reset { background: #fee2e2; color: #b91c1c; border-color: #fecaca; }
.inp { padding: 6px 10px; border: 1px solid var(--border); border-radius: 8px; font-size: 13px; background: #fff; color: inherit; }
.inp:disabled { background: #f1f5f9; color: var(--muted); }
.count { font-size: 12.5px; margin-left: auto; }

.badge-a { padding: 3px 9px; border-radius: 6px; font-size: 12.5px; font-weight: 700; }
.b-login { background: #dbeafe; color: #1d4ed8; }
.b-license { background: #dcfce7; color: #15803d; }
.b-org { background: #fef3c7; color: #92400e; }
.b-user { background: #f1f5f9; color: #475569; }
.dot-fail { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: #dc2626; margin-left: 8px; vertical-align: middle; }


.dl { display: flex; flex-direction: column; }
.dl-row { display: flex; justify-content: space-between; align-items: baseline; gap: 16px; padding: 9px 0; border-bottom: 1px solid var(--border); font-size: 14px; }
.dl-row:last-child { border-bottom: none; }
.dl-row > span { color: var(--muted); flex-shrink: 0; }
.dl-row > b { text-align: right; word-break: break-word; }
</style>
