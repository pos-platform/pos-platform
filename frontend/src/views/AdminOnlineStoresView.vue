<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { onlineStoresAdminApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// SuperAdmin: mobil ilovadagi do'konlar — yoqish (tasdiqlash), bloklash, to'lov usullari, tavsiya, tarif muddati.
const filter = ref('all')
const search = ref('')
const rows = ref([])
const loading = ref(false)
const err = ref('')
const ok = ref('')

const selected = ref(null) // to'liq sozlamalar
const busy = ref(false)
const form = reactive({
  isEnabled: false,
  isBlocked: false,
  blockReason: '',
  isFeatured: false,
  sortOrder: 0,
  paidUntil: '',
  allowCash: true,
  allowTransfer: true,
  allowCardOnDelivery: false,
  allowPayme: false,
  allowClick: false
})

const filters = [
  { id: 'all', label: 'Barchasi' },
  { id: 'requested', label: 'So\'rov yuborgan' },
  { id: 'enabled', label: 'Ilovada' },
  { id: 'blocked', label: 'Bloklangan' }
]

async function load() {
  loading.value = true
  err.value = ''
  try {
    rows.value = await onlineStoresAdminApi.list({ filter: filter.value, search: search.value.trim() || undefined })
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

let timer = null
watch(filter, () => load())
watch(search, () => {
  clearTimeout(timer)
  timer = setTimeout(load, 400)
})

async function open(row) {
  err.value = ''
  ok.value = ''
  try {
    const s = await onlineStoresAdminApi.get(row.organizationId)
    selected.value = s
    Object.assign(form, {
      isEnabled: s.isEnabled,
      isBlocked: s.isBlocked,
      blockReason: s.blockReason || '',
      isFeatured: s.isFeatured,
      sortOrder: s.sortOrder,
      paidUntil: s.paidUntil ? s.paidUntil.substring(0, 10) : '',
      allowCash: s.allowCash,
      allowTransfer: s.allowTransfer,
      allowCardOnDelivery: s.allowCardOnDelivery,
      allowPayme: !!s.allowPayme,
      allowClick: !!s.allowClick
    })
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function save() {
  if (!selected.value) return
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    const s = await onlineStoresAdminApi.update(selected.value.organizationId, {
      isEnabled: form.isEnabled,
      isBlocked: form.isBlocked,
      blockReason: form.isBlocked ? form.blockReason.trim() : null,
      isFeatured: form.isFeatured,
      sortOrder: Number(form.sortOrder) || 0,
      // Kun oxirigacha amal qiladi (UTC+5 → UTC 18:59).
      paidUntil: form.paidUntil ? `${form.paidUntil}T18:59:59Z` : null,
      allowCash: form.allowCash,
      allowTransfer: form.allowTransfer,
      allowCardOnDelivery: form.allowCardOnDelivery,
      allowPayme: form.allowPayme,
      allowClick: form.allowClick
    })
    selected.value = s
    ok.value = 'Saqlandi.'
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}


function statusOf(r) {
  if (r.isBlocked) return { text: 'Bloklangan', cls: 'st-blocked' }
  if (r.isEnabled) return { text: 'Ilovada', cls: 'st-on' }
  if (r.isRequested) return { text: 'So\'rov', cls: 'st-req' }
  return { text: 'O\'chiq', cls: 'st-off' }
}

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('uz-UZ') : '—'
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Online do'konlar</h1>
    <button class="btn btn-ghost btn-sm" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>

  <div class="card toolbar">
    <div class="chips">
      <button v-for="f in filters" :key="f.id" class="chip" :class="{ active: filter === f.id }" @click="filter = f.id">
        {{ f.label }}
      </button>
    </div>
    <input v-model="search" class="input" placeholder="Tashkilot nomi..." style="max-width:260px" />
  </div>

  <div class="layout">
    <div class="card" style="overflow:auto">
      <table class="table">
        <thead>
          <tr>
            <th>Tashkilot</th>
            <th>Holat</th>
            <th>Do'konlar</th>
            <th>Tarif</th>
            <th>30 kun buyurtma</th>
            <th>Qabul</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="6" class="muted">Yuklanmoqda...</td></tr>
          <tr v-else-if="!rows.length"><td colspan="6" class="muted">Ma'lumot yo'q</td></tr>
          <tr v-for="r in rows" :key="r.organizationId" class="clickable"
              :class="{ sel: selected?.organizationId === r.organizationId }" @click="open(r)">
            <td>
              <div style="font-weight:600">{{ r.displayName || r.organizationName }}</div>
              <div v-if="r.displayName" class="muted small">{{ r.organizationName }}</div>
              <div v-if="r.slug || r.appName" class="muted small">
                📱 {{ r.appName || '—' }}<span v-if="r.slug"> · {{ r.slug }}</span>
              </div>
              <div v-if="!r.organizationActive" class="small" style="color:#dc2626">tashkilot bloklangan</div>
            </td>
            <td>
              <span class="st" :class="statusOf(r).cls">{{ statusOf(r).text }}</span>
              <span v-if="r.isFeatured" class="st st-feat">★</span>
            </td>
            <td>{{ r.storeBranchCount }}</td>
            <td>{{ fmtDate(r.paidUntil) }}</td>
            <td>{{ r.ordersLast30Days }}</td>
            <td>{{ r.isAcceptingOrders ? 'Ha' : 'Yo\'q' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="selected" class="card detail">
      <div class="detail-head">
        <div>
          <h3 style="margin:0">{{ selected.displayName || selected.organizationName }}</h3>
          <p class="muted small" style="margin:2px 0 0">
            {{ selected.isVisible ? 'Ilovada ko\'rinmoqda' : 'Ilovada ko\'rinmaydi' }}
            · {{ selected.phone || 'telefon yo\'q' }}
          </p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <label class="row"><input v-model="form.isEnabled" type="checkbox" /> Ilovada yoqilgan (tasdiqlangan)</label>
      <label class="row"><input v-model="form.isBlocked" type="checkbox" /> Bloklangan</label>
      <input v-if="form.isBlocked" v-model="form.blockReason" class="input" placeholder="Bloklash sababi (majburiy)" />
      <label class="row"><input v-model="form.isFeatured" type="checkbox" /> Tavsiya etilgan (bosh sahifada yuqorida)</label>

      <div class="grid2">
        <div class="field">
          <label>Tartib raqami</label>
          <input v-model="form.sortOrder" type="number" class="input" />
        </div>
        <div class="field">
          <label>Tarif muddati (bo'sh — cheklovsiz)</label>
          <input v-model="form.paidUntil" type="date" class="input" />
        </div>
      </div>

      <div class="field">
        <label>To'lov usullari</label>
        <label class="row"><input v-model="form.allowCash" type="checkbox" /> Naqd</label>
        <label class="row"><input v-model="form.allowTransfer" type="checkbox" /> Kartaga o'tkazma</label>
        <label class="row"><input v-model="form.allowCardOnDelivery" type="checkbox" /> Kuryer terminali</label>
        <label class="row"><input v-model="form.allowPayme" type="checkbox" /> Payme (buyurtma yig'ilgach online to'lov)</label>
        <label class="row"><input v-model="form.allowClick" type="checkbox" /> Click (buyurtma yig'ilgach online to'lov)</label>
        <p v-if="form.allowTransfer && !selected.transferCardNumber" class="small" style="color:#b45309;margin:4px 0 0">
          Do'kon karta raqamini kiritmagan — o'tkazma ilovada chiqmaydi.
        </p>
      </div>

      <div class="info small muted">
        <div>Ish vaqti: {{ selected.opensAt && selected.closesAt ? `${selected.opensAt.substring(0,5)}–${selected.closesAt.substring(0,5)}` : 'ko\'rsatilmagan' }}</div>
        <div>Yetkazish: {{ selected.deliveryEnabled ? `${selected.deliveryFee.toLocaleString()} so'm` : 'yo\'q' }}
          · Olib ketish: {{ selected.pickupEnabled ? 'ha' : 'yo\'q' }}</div>
        <div>Minimal summa: {{ selected.minOrderAmount.toLocaleString() }} so'm</div>
      </div>

      <button class="btn" :disabled="busy" @click="save">{{ busy ? 'Saqlanmoqda...' : 'Saqlash' }}</button>

    </div>
  </div>
</template>

<style scoped>
.toolbar { display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap; margin-bottom:12px; }
.chips { display:flex; gap:6px; flex-wrap:wrap; }
.chip { border:1px solid var(--border, #e2e8f0); background:#fff; border-radius:999px; padding:6px 12px; cursor:pointer; font-weight:600; font-size:13px; }
.chip.active { background:#0f172a; color:#fff; border-color:#0f172a; }
.layout { display:grid; grid-template-columns: minmax(0, 1fr) 380px; gap:14px; align-items:start; }
.clickable { cursor:pointer; }
.clickable.sel { background:#f1f5f9; }
.small { font-size:12px; }
.st { display:inline-block; font-size:12px; font-weight:700; padding:2px 8px; border-radius:999px; margin-right:4px; }
.st-on { background:#dcfce7; color:#166534; }
.st-req { background:#fef3c7; color:#92400e; }
.st-off { background:#f1f5f9; color:#475569; }
.st-blocked { background:#fee2e2; color:#991b1b; }
.st-feat { background:#e0e7ff; color:#3730a3; }
.detail { display:flex; flex-direction:column; gap:10px; }
.detail-head { display:flex; justify-content:space-between; align-items:flex-start; }
.row { display:flex; align-items:center; gap:8px; font-size:14px; }
.grid2 { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
.field { display:flex; flex-direction:column; gap:4px; }
.field > label:first-child { font-size:13px; color:var(--text-muted); font-weight:600; }
.info { display:flex; flex-direction:column; gap:2px; background:#f8fafc; border-radius:8px; padding:8px 10px; }
.field .input + .input, .field .grid2 + .input { margin-top:6px; }
@media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
</style>
