<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { adminErrorsApi, organizationsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// Bosqich 10.4: platformadagi tizim xatoliklari (API, fon xizmatlari, fiskal, desktop, mobil, agent).
// Bir xil xato (fingerprint) bitta qatorga guruhlanadi — Count / LastSeenAt.
const SOURCES = ['Api', 'Background', 'Fiscal', 'Desktop', 'Mobile', 'Agent']
const LEVELS = { Warning: 'pill-amber', Error: 'pill-red', Critical: 'pill-red crit' }

const f = reactive({ status: 'open', source: '', level: '', organizationId: '', search: '', from: '', to: '' })
const page = ref(1)
const pageSize = 50
const data = ref({ items: [], total: 0, openCount: 0 })
const summary = ref(null)
const loading = ref(false)
const err = ref('')
const ok = ref('')
const orgs = ref([])
const checked = ref(new Set())

const fmtDate = (d) => (d ? new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '—')
const pages = computed(() => Math.max(1, Math.ceil((data.value.total || 0) / pageSize)))

function notifyMenu() {
  window.dispatchEvent(new Event('admin-errors-changed'))
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    const p = { status: f.status, page: page.value, pageSize }
    for (const k of ['source', 'level', 'organizationId', 'from', 'to']) if (f[k]) p[k] = f[k]
    if (f.search.trim()) p.search = f.search.trim()
    const r = await adminErrorsApi.list(p)
    data.value = { items: r?.items || [], total: r?.total || 0, openCount: r?.openCount || 0 }
    checked.value = new Set()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
  loadSummary()
}

async function loadSummary() {
  try { summary.value = await adminErrorsApi.summary() } catch { /* ixtiyoriy */ }
}

function apply() {
  if (page.value !== 1) page.value = 1
  else load()
}

// Tanlash (ommaviy "Hal qilindi")
const openItems = computed(() => data.value.items.filter((x) => !x.resolvedAt))
const allChecked = computed(() => openItems.value.length > 0 && openItems.value.every((x) => checked.value.has(x.id)))
function toggle(id) {
  const s = new Set(checked.value)
  s.has(id) ? s.delete(id) : s.add(id)
  checked.value = s
}
function toggleAll() {
  checked.value = allChecked.value ? new Set() : new Set(openItems.value.map((x) => x.id))
}

const busy = ref(false)
async function resolveBulk() {
  const ids = [...checked.value]
  if (!ids.length) return
  if (!window.confirm(`${ids.length} ta xatoni "hal qilindi" deb belgilaysizmi?`)) return
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    await adminErrorsApi.resolveBulk(ids)
    ok.value = `${ids.length} ta xato hal qilindi`
    notifyMenu()
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

// Tafsilot
const detail = ref(null)
const detailLoading = ref(false)
const copied = ref(false)
async function open(row) {
  detail.value = row
  copied.value = false
  detailLoading.value = true
  try {
    const full = await adminErrorsApi.get(row.id)
    if (detail.value?.id === row.id) detail.value = full
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

async function resolveOne() {
  const d = detail.value
  if (!d) return
  busy.value = true
  err.value = ''
  try {
    const r = await adminErrorsApi.resolve(d.id)
    detail.value = r && typeof r === 'object' && r.id === d.id ? r : { ...d, resolvedAt: new Date().toISOString() }
    ok.value = 'Hal qilindi'
    notifyMenu()
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

function copyText(d) {
  const lines = [
    `[${d.level}] ${d.source}: ${d.message}`,
    d.method || d.path ? `${d.method || ''} ${d.path || ''}${d.statusCode ? ' → ' + d.statusCode : ''}` : '',
    d.organizationName ? `Tashkilot: ${d.organizationName}` : '',
    d.userName ? `Foydalanuvchi: ${d.userName}` : '',
    d.appVersion ? `Versiya: ${d.appVersion}` : '',
    d.device ? `Qurilma: ${d.device}` : '',
    d.requestId ? `RequestId: ${d.requestId}` : '',
    `Birinchi: ${fmtDate(d.firstSeenAt)} · Oxirgi: ${fmtDate(d.lastSeenAt)} · ${d.count} marta`,
    '',
    d.details || ''
  ]
  return lines.filter((x, i) => x || i >= 8).join('\n')
}

async function copy() {
  const text = copyText(detail.value)
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    const ta = document.createElement('textarea')
    ta.value = text
    document.body.appendChild(ta)
    ta.select()
    document.execCommand('copy')
    ta.remove()
  }
  copied.value = true
  setTimeout(() => { copied.value = false }, 1500)
}

watch(page, load)
watch(() => [f.status, f.source, f.level, f.organizationId, f.from, f.to], apply)

onMounted(async () => {
  load()
  try {
    const r = await organizationsApi.list(1, 500)
    orgs.value = Array.isArray(r) ? r : r?.items || []
  } catch { /* filtr ixtiyoriy */ }
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Xatoliklar</h1>
      <div class="muted small">Server, fon xizmatlari, fiskal, desktop kassa, mobil ilova va agent xatolari. Bir xil xatolar guruhlanadi.</div>
    </div>
    <button class="btn btn-ghost btn-sm" :disabled="loading" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="summary" class="summary">
    <div class="card stat"><div class="stat-label">Ochiq xatolar</div><div class="stat-value" style="color:var(--danger)">{{ summary.openCount || 0 }}</div></div>
    <div class="card stat"><div class="stat-label">Oxirgi 24 soat</div><div class="stat-value">{{ summary.last24h || 0 }}</div></div>
    <div class="card stat">
      <div class="stat-label">Manba bo'yicha</div>
      <div class="src-list">
        <button v-for="(n, k) in summary.bySource || {}" :key="k" class="src-chip" :class="{ on: f.source === k }" @click="f.source = f.source === k ? '' : k">
          {{ k }} · {{ n }}
        </button>
      </div>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>

  <div class="card toolbar">
    <select v-model="f.status" class="select w-auto">
      <option value="open">Ochiq</option>
      <option value="resolved">Hal qilingan</option>
      <option value="all">Hammasi</option>
    </select>
    <select v-model="f.source" class="select w-auto">
      <option value="">Barcha manbalar</option>
      <option v-for="s in SOURCES" :key="s" :value="s">{{ s }}</option>
    </select>
    <select v-model="f.level" class="select w-auto">
      <option value="">Barcha darajalar</option>
      <option v-for="(v, l) in LEVELS" :key="l" :value="l">{{ l }}</option>
    </select>
    <select v-if="orgs.length" v-model="f.organizationId" class="select w-auto" style="max-width:220px">
      <option value="">Barcha tashkilotlar</option>
      <option v-for="o in orgs" :key="o.id" :value="o.id">{{ o.name }}</option>
    </select>
    <input v-model="f.from" type="date" class="input date-in" />
    <input v-model="f.to" type="date" class="input date-in" />
    <input v-model="f.search" class="input" style="width:220px" placeholder="Xabar matni..." @keyup.enter="apply" />
    <button class="btn btn-sm" @click="apply">Qidirish</button>
    <button v-if="checked.size" class="btn btn-success btn-sm" :disabled="busy" @click="resolveBulk">✓ Hal qilindi ({{ checked.size }})</button>
  </div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th style="width:32px"><input type="checkbox" :checked="allChecked" :disabled="!openItems.length" @change="toggleAll" /></th>
          <th>Daraja</th>
          <th>Manba</th>
          <th>Xabar</th>
          <th>Tashkilot</th>
          <th class="text-right">Soni</th>
          <th>Oxirgi</th>
          <th>Birinchi</th>
          <th>Holat</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="9" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
        <tr v-else-if="!data.items.length"><td colspan="9" class="muted" style="text-align:center; padding:24px">Xato yo'q</td></tr>
        <tr v-for="e in data.items" v-else :key="e.id" class="clickable" @click="open(e)">
          <td @click.stop><input type="checkbox" :checked="checked.has(e.id)" :disabled="!!e.resolvedAt" @change="toggle(e.id)" /></td>
          <td><span class="pill" :class="LEVELS[e.level] || 'pill-gray'">{{ e.level }}</span></td>
          <td class="small">{{ e.source }}</td>
          <td class="msg">{{ e.message }}</td>
          <td class="small">{{ e.organizationName || '—' }}</td>
          <td class="text-right"><strong>{{ e.count }}</strong></td>
          <td class="muted small nowrap">{{ fmtDate(e.lastSeenAt) }}</td>
          <td class="muted small nowrap">{{ fmtDate(e.firstSeenAt) }}</td>
          <td>
            <span v-if="e.resolvedAt" class="pill pill-green">Hal qilingan</span>
            <span v-else class="pill pill-amber">Ochiq</span>
          </td>
        </tr>
      </tbody>
    </table>
    <div v-if="pages > 1" class="pager">
      <button class="btn btn-ghost btn-sm" :disabled="page <= 1 || loading" @click="page--">‹</button>
      <span class="muted small">{{ page }} / {{ pages }} · jami {{ data.total }}</span>
      <button class="btn btn-ghost btn-sm" :disabled="page >= pages || loading" @click="page++">›</button>
    </div>
  </div>

  <!-- Tafsilot -->
  <div v-if="detail" class="modal-overlay" @click.self="detail = null">
    <div class="modal-box" style="max-width:900px">
      <div class="modal-head">
        <h3>
          <span class="pill" :class="LEVELS[detail.level] || 'pill-gray'">{{ detail.level }}</span>
          {{ detail.source }}
          <span v-if="detail.resolvedAt" class="pill pill-green" style="margin-left:6px">Hal qilingan</span>
        </h3>
        <button class="modal-x" @click="detail = null">×</button>
      </div>
      <div class="modal-body">
        <div class="msg-full">{{ detail.message }}</div>
        <div v-if="detailLoading" class="muted small">Yuklanmoqda...</div>
        <div class="meta">
          <div v-if="detail.method || detail.path"><span class="muted">So'rov:</span> <code>{{ detail.method }} {{ detail.path }}</code><template v-if="detail.statusCode"> → {{ detail.statusCode }}</template></div>
          <div v-if="detail.organizationName"><span class="muted">Tashkilot:</span> {{ detail.organizationName }}</div>
          <div v-if="detail.userName"><span class="muted">Foydalanuvchi:</span> {{ detail.userName }}</div>
          <div v-if="detail.appVersion"><span class="muted">Versiya:</span> {{ detail.appVersion }}</div>
          <div v-if="detail.device"><span class="muted">Qurilma:</span> {{ detail.device }}</div>
          <div v-if="detail.requestId"><span class="muted">RequestId:</span> <code>{{ detail.requestId }}</code></div>
          <div><span class="muted">Soni:</span> <b>{{ detail.count }}</b></div>
          <div><span class="muted">Birinchi:</span> {{ fmtDate(detail.firstSeenAt) }}</div>
          <div><span class="muted">Oxirgi:</span> {{ fmtDate(detail.lastSeenAt) }}</div>
          <div v-if="detail.resolvedAt"><span class="muted">Hal qilindi:</span> {{ fmtDate(detail.resolvedAt) }}<template v-if="detail.resolvedBy"> · {{ detail.resolvedBy }}</template></div>
          <div v-if="detail.fingerprint" class="small"><span class="muted">Fingerprint:</span> <code>{{ detail.fingerprint }}</code></div>
        </div>
        <pre v-if="detail.details" class="stack">{{ detail.details }}</pre>
        <div class="row" style="gap:8px; margin-top:12px">
          <button class="btn btn-ghost" @click="copy">{{ copied ? '✓ Nusxa olindi' : '⧉ Nusxa olish' }}</button>
          <button v-if="!detail.resolvedAt" class="btn btn-success" :disabled="busy" @click="resolveOne">✓ Hal qilindi</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.toolbar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 12px; }
.w-auto { width: auto; }
.date-in { width: 150px; }
.summary { display: grid; grid-template-columns: 1fr 1fr 2fr; gap: 12px; margin-bottom: 12px; }
.stat-label { font-size: 12px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 22px; font-weight: 700; margin-top: 4px; }
.src-list { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 6px; }
.src-chip { border: 1px solid var(--border); background: #f8fafc; border-radius: 999px; padding: 3px 10px; font-size: 12px; font-weight: 600; cursor: pointer; color: var(--muted); }
.src-chip.on { background: var(--primary); color: #fff; border-color: var(--primary); }
.clickable { cursor: pointer; }
.msg { max-width: 520px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: ui-monospace, Consolas, monospace; font-size: 12.5px; }
.msg-full { font-family: ui-monospace, Consolas, monospace; font-size: 13.5px; font-weight: 600; word-break: break-word; margin-bottom: 10px; }
.meta { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 4px 16px; font-size: 13px; }
.meta code { word-break: break-all; }
.stack { margin-top: 12px; background: #0f172a; color: #e2e8f0; padding: 12px; border-radius: 10px; font-size: 12px; max-height: 46vh; overflow: auto; white-space: pre-wrap; word-break: break-word; }
.crit { outline: 2px solid #dc2626; }
.pager { display: flex; justify-content: center; align-items: center; gap: 10px; padding: 12px; }
@media (max-width: 900px) {
  .summary { grid-template-columns: 1fr; }
}
</style>
