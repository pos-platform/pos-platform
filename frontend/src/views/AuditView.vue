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
  'user.create': 'Foydalanuvchi yaratildi',
  'user.activate': 'Foydalanuvchi faollashtirildi',
  'user.deactivate': 'Foydalanuvchi bloklandi',
  'org.create': 'Tashkilot yaratildi',
  'org.activate': 'Tashkilot faollashtirildi',
  'org.deactivate': 'Tashkilot bloklandi'
}

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
        <tr><th>Vaqt</th><th>Hodisa</th><th>Foydalanuvchi</th><th>IP</th><th>Tafsilot</th></tr>
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
        </tr>
        <tr v-if="!filtered.length && !loading">
          <td colspan="5" class="muted" style="text-align:center;padding:24px">Yozuv yo'q</td>
        </tr>
      </tbody>
    </table>
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
</style>
