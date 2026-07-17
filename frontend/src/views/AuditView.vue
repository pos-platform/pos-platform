<script setup>
import { ref, computed, onMounted } from 'vue'
import { auditApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const logs = ref([])
const loading = ref(false)
const err = ref('')
const filter = ref('all') // all | login | license | user | org | fail

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

const filtered = computed(() => {
  if (filter.value === 'all') return logs.value
  if (filter.value === 'fail') return logs.value.filter((l) => !l.success)
  return logs.value.filter((l) => l.action.startsWith(filter.value))
})

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
    logs.value = await auditApi.recent(300)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Audit jurnali</h1>
    <button class="btn btn-ghost btn-sm" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="filters">
    <button v-for="f in [['all','Hammasi'],['fail','Xatolar'],['login','Kirishlar'],['license','Obuna'],['user','Foydalanuvchi'],['org','Tashkilot']]"
            :key="f[0]" class="chip" :class="{ 'chip-on': filter === f[0] }" @click="filter = f[0]">
      {{ f[1] }}
    </button>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr><th>Vaqt</th><th>Hodisa</th><th>Foydalanuvchi</th><th>IP</th><th>Tafsilot</th><th></th></tr>
      </thead>
      <tbody>
        <tr v-for="l in filtered" :key="l.id">
          <td class="muted" style="white-space:nowrap">{{ fmt(l.createdAt) }}</td>
          <td>
            <span class="badge-a" :class="cls(l.action)">{{ label(l.action) }}</span>
            <span v-if="!l.success" class="dot-fail" title="muvaffaqiyatsiz"></span>
          </td>
          <td>{{ l.actorUsername || '—' }}</td>
          <td class="muted">{{ l.ipAddress || '—' }}</td>
          <td class="muted" style="font-size:13px">{{ l.detail }}</td>
          <td style="text-align:right">
            <button class="eye-btn" title="Tafsilotni ko'rish" @click="detailLog = l">👁</button>
          </td>
        </tr>
        <tr v-if="!filtered.length && !loading">
          <td colspan="6" class="muted" style="text-align:center;padding:24px">Yozuv yo'q</td>
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
.filters { display: flex; gap: 8px; margin-bottom: 14px; flex-wrap: wrap; }
.chip { padding: 6px 14px; border-radius: 20px; border: 1px solid var(--border); background: #f8fafc; cursor: pointer; font-size: 13px; font-weight: 600; color: var(--muted); }
.chip-on { background: #2563eb; color: #fff; border-color: #2563eb; }

.badge-a { padding: 3px 9px; border-radius: 6px; font-size: 12.5px; font-weight: 700; }
.b-login { background: #dbeafe; color: #1d4ed8; }
.b-license { background: #dcfce7; color: #15803d; }
.b-org { background: #fef3c7; color: #92400e; }
.b-user { background: #f1f5f9; color: #475569; }
.dot-fail { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: #dc2626; margin-left: 8px; vertical-align: middle; }

.eye-btn { border: 1px solid var(--border); background: #f8fafc; border-radius: 8px; padding: 4px 10px; cursor: pointer; font-size: 15px; line-height: 1; }
.eye-btn:hover { background: #e2e8f0; }

.dl { display: flex; flex-direction: column; }
.dl-row { display: flex; justify-content: space-between; align-items: baseline; gap: 16px; padding: 9px 0; border-bottom: 1px solid var(--border); font-size: 14px; }
.dl-row:last-child { border-bottom: none; }
.dl-row > span { color: var(--muted); flex-shrink: 0; }
.dl-row > b { text-align: right; word-break: break-word; }
</style>
