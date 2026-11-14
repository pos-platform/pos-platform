<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { loyaltyApi } from '../../api/catalog'
import { errorMessage } from '../../api/http'
import { exportCsv } from '../../utils/exportCsv'

// Bosqich 10.2: barcha mijozlar bo'yicha bonus harakatlari jurnali.
const TYPE_LABELS = {
  Earn: 'Cashback',
  Redeem: 'Sarflandi',
  Refund: 'Qaytarish',
  Expire: "Muddati o'tdi",
  Adjust: 'Tuzatish',
  Signup: "Ro'yxatdan o'tish bonusi",
  Referral: 'Taklif bonusi'
}
const SOURCE_LABELS = { Store: "Do'kon", Online: 'Online', Manual: "Qo'lda", System: 'Tizim' }

function daysAgo(n) {
  const d = new Date()
  d.setDate(d.getDate() - n)
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}

const f = reactive({ from: daysAgo(29), to: daysAgo(0), type: '', search: '' })
const page = ref(1)
const pageSize = 50
const data = ref({ items: [], total: 0, totals: null })
const loading = ref(false)
const exporting = ref(false)
const err = ref('')

const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtDate = (d) => (d ? new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '')
const pages = computed(() => Math.max(1, Math.ceil((data.value.total || 0) / pageSize)))

function params(p, size) {
  const x = { from: f.from, to: f.to, page: p, pageSize: size }
  if (f.type) x.type = f.type
  if (f.search.trim()) x.search = f.search.trim()
  return x
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    const r = await loyaltyApi.transactions(params(page.value, pageSize))
    data.value = { items: r?.items || [], total: r?.total || 0, totals: r?.totals || null }
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function apply() {
  if (page.value !== 1) page.value = 1
  else load()
}

// Excel: server eksporti yo'q — barcha sahifalarni yig'ib CSV ga yozamiz.
async function exportExcel() {
  exporting.value = true
  err.value = ''
  try {
    const size = 500
    const all = []
    for (let p = 1; p <= 100; p++) {
      const r = await loyaltyApi.transactions(params(p, size))
      const items = r?.items || []
      all.push(...items)
      if (!items.length || all.length >= (r?.total || 0)) break
    }
    exportCsv(`bonus-harakatlari-${f.from}-${f.to}.csv`, [
      { title: 'Sana', value: (t) => fmtDate(t.createdAt) },
      { title: 'Mijoz', value: (t) => t.customerName },
      { title: 'Telefon', value: (t) => t.customerPhone },
      { title: 'Turi', value: (t) => TYPE_LABELS[t.type] || t.type },
      { title: 'Ball', value: (t) => Number(t.points) || 0 },
      { title: 'Balans', value: (t) => Number(t.balanceAfter) || 0 },
      { title: 'Manba', value: (t) => SOURCE_LABELS[t.source] || t.source },
      { title: 'Chek', value: (t) => t.receiptNumber || '' },
      { title: 'Xodim', value: (t) => t.userName || '' },
      { title: 'Izoh', value: (t) => t.note || '' }
    ], all)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    exporting.value = false
  }
}

watch(page, load)
watch(() => f.type, apply)
onMounted(load)
</script>

<template>
  <div class="card" style="margin-bottom:16px">
    <div class="row" style="align-items:flex-end">
      <div class="field" style="margin:0"><label>Dan</label><input v-model="f.from" type="date" class="input" /></div>
      <div class="field" style="margin:0"><label>Gacha</label><input v-model="f.to" type="date" class="input" /></div>
      <div class="field" style="margin:0">
        <label>Turi</label>
        <select v-model="f.type" class="select">
          <option value="">Hammasi</option>
          <option v-for="(v, k) in TYPE_LABELS" :key="k" :value="k">{{ v }}</option>
        </select>
      </div>
      <div class="field" style="margin:0; min-width:220px">
        <label>Qidirish (mijoz, telefon, chek)</label>
        <input v-model="f.search" class="input" @keyup.enter="apply" />
      </div>
      <button class="btn" :disabled="loading" @click="apply">{{ loading ? '...' : 'Ko\'rsatish' }}</button>
      <button class="btn btn-ghost" :disabled="exporting" @click="exportExcel">{{ exporting ? '...' : '⬇ Excel' }}</button>
    </div>

    <div v-if="data.totals" class="kpi-grid">
      <div class="kpi"><div class="kpi-value" style="color:var(--success)">{{ fmtNum(data.totals.earned) }}</div><div class="kpi-label">Yozilgan</div></div>
      <div class="kpi"><div class="kpi-value" style="color:var(--primary)">{{ fmtNum(data.totals.redeemed) }}</div><div class="kpi-label">Sarflangan</div></div>
      <div class="kpi"><div class="kpi-value" style="color:var(--danger)">{{ fmtNum(data.totals.refunded) }}</div><div class="kpi-label">Qaytarishda ayirilgan</div></div>
      <div class="kpi"><div class="kpi-value">{{ fmtNum(data.totals.expired) }}</div><div class="kpi-label">Muddati o'tgan</div></div>
      <div class="kpi"><div class="kpi-value">{{ fmtNum(data.totals.adjusted) }}</div><div class="kpi-label">Qo'lda tuzatish</div></div>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr><th>Sana</th><th>Mijoz</th><th>Turi</th><th class="text-right">Ball</th><th class="text-right">Balans</th><th>Manba</th><th>Chek</th><th>Xodim</th><th>Izoh</th></tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="9" class="muted" style="text-align:center; padding:20px">Yuklanmoqda...</td></tr>
        <tr v-else-if="!data.items.length"><td colspan="9" class="muted" style="text-align:center; padding:20px">Harakat yo'q</td></tr>
        <tr v-for="t in data.items" v-else :key="t.id">
          <td class="muted nowrap">{{ fmtDate(t.createdAt) }}</td>
          <td><strong>{{ t.customerName || '—' }}</strong><div class="muted small">{{ t.customerPhone || '' }}</div></td>
          <td>{{ TYPE_LABELS[t.type] || t.type }}</td>
          <td class="text-right" :style="{ color: Number(t.points) >= 0 ? 'var(--success)' : 'var(--danger)', fontWeight: 600 }">
            {{ Number(t.points) > 0 ? '+' : '' }}{{ fmtNum(t.points) }}
          </td>
          <td class="text-right">{{ fmtNum(t.balanceAfter) }}</td>
          <td class="muted">{{ SOURCE_LABELS[t.source] || t.source || '—' }}</td>
          <td class="muted">{{ t.receiptNumber || '—' }}</td>
          <td class="muted">{{ t.userName || '' }}</td>
          <td class="muted small">{{ t.note || '' }}</td>
        </tr>
      </tbody>
    </table>
    <div v-if="pages > 1" class="pager">
      <button class="btn btn-ghost btn-sm" :disabled="page <= 1 || loading" @click="page--">‹</button>
      <span class="muted small">{{ page }} / {{ pages }} · jami {{ data.total }}</span>
      <button class="btn btn-ghost btn-sm" :disabled="page >= pages || loading" @click="page++">›</button>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.kpi-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(160px, 1fr)); gap:12px; margin-top:16px; }
.kpi { padding:12px; border:1px solid var(--border, #e5e7eb); border-radius:10px; }
.kpi-value { font-size:20px; font-weight:800; line-height:1.1; }
.kpi-label { color:var(--muted); margin-top:6px; font-weight:600; font-size:13px; }
.pager { display:flex; justify-content:center; align-items:center; gap:10px; padding:12px; }
</style>
