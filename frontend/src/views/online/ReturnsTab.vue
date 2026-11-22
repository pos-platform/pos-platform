<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { onlineReturnsApi, onlineOrdersApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'
import { useStoreFilterStore } from '../../stores/storeFilter'

// Bosqich 10.5: online buyurtmalarni qaytarish so'rovlari (mijoz ilovadan yuboradi, do'kon tasdiqlaydi).
const storeFilter = useStoreFilterStore()

const STATUS = {
  Requested: { text: 'So\'rov', cls: 'st-new' },
  Approved: { text: 'Tasdiqlandi', cls: 'st-work' },
  PickupAssigned: { text: 'Kuryer biriktirildi', cls: 'st-work' },
  PickedUp: { text: 'Kuryer oldi', cls: 'st-ready' },
  Received: { text: 'Do\'konga keldi', cls: 'st-ready' },
  Refunded: { text: 'Pul qaytarildi', cls: 'st-done' },
  Rejected: { text: 'Rad etildi', cls: 'st-bad' },
  Cancelled: { text: 'Bekor qilindi', cls: 'st-bad' }
}
const METHOD = { CourierPickup: 'Kuryer olib ketadi', BringToStore: 'Mijoz do\'konga olib keladi' }
const REASON = { Damaged: 'Shikastlangan', WrongItem: 'Noto\'g\'ri tovar', Quality: 'Sifatsiz', Expired: 'Muddati o\'tgan', Missing: 'Yetishmadi', Other: 'Boshqa' }
const REFUND_METHOD = { Cash: 'Naqd (do\'konda)', Transfer: 'O\'tkazma', Bonus: 'Bonus ball' }

const statusOf = (s) => STATUS[s] || { text: s, cls: 'st-work' }
const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtQty = (n) => Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
const fmtDateTime = (v) =>
  v ? new Date(v).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '—'
const listOf = (r) => (Array.isArray(r) ? r : r?.items || [])

const items = ref([])
const loading = ref(false)
const err = ref('')
const ok = ref('')
const status = ref('')
const selected = ref(null)
const detailLoading = ref(false)
const busy = ref(false)

async function load() {
  loading.value = true
  err.value = ''
  try {
    const params = {}
    if (status.value) params.status = status.value
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    items.value = listOf(await onlineReturnsApi.list(params)).sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    if (selected.value) {
      const fresh = items.value.find((x) => x.id === selected.value.id)
      if (!fresh) selected.value = null
    }
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function open(r) {
  selected.value = r
  approveMethod.value = r.method || 'CourierPickup'
  courierId.value = r.courierId || ''
  rejectReason.value = ''
  detailLoading.value = true
  try {
    const full = await onlineReturnsApi.get(r.id)
    if (selected.value?.id === r.id) {
      selected.value = full
      approveMethod.value = full.method || approveMethod.value
      courierId.value = full.courierId || ''
    }
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

// Amallar
const approveMethod = ref('CourierPickup')
const rejectReason = ref('')
const courierId = ref('')
const couriers = ref([])

async function loadCouriers() {
  try {
    const params = {}
    const b = selected.value?.branchId || storeFilter.selectedBranchId
    if (b) params.branchId = b
    couriers.value = listOf(await onlineOrdersApi.couriers(params))
  } catch {
    couriers.value = []
  }
}

const needsCourier = computed(() => {
  const r = selected.value
  return !!r && r.method === 'CourierPickup' && ['Approved', 'PickupAssigned'].includes(r.status)
})
watch(needsCourier, (v) => { if (v) loadCouriers() })

async function act(fn, msg) {
  const r = selected.value
  if (!r) return
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    const res = await fn(r.id)
    if (res && typeof res === 'object' && res.id === r.id) selected.value = res
    else await open(r)
    ok.value = msg
    load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

const approve = () => act((id) => onlineReturnsApi.approve(id, approveMethod.value), 'Qaytarish tasdiqlandi')
function reject() {
  const reason = rejectReason.value.trim()
  if (!reason) { err.value = 'Rad etish sababini yozing'; return }
  act((id) => onlineReturnsApi.reject(id, reason), 'Qaytarish rad etildi')
}
function assign() {
  if (!courierId.value) { err.value = 'Kuryerni tanlang'; return }
  act((id) => onlineReturnsApi.assignCourier(id, courierId.value), 'Kuryer biriktirildi')
}
function received() {
  if (!window.confirm('Tovar do\'konga keldimi? Qisman qaytarish bajariladi va keshbek ayiriladi.')) return
  act((id) => onlineReturnsApi.received(id), 'Tovar qabul qilindi, qaytarish bajarildi')
}
function refunded() {
  if (!window.confirm('Mijozga pul berildi / o\'tkazildimi?')) return
  act((id) => onlineReturnsApi.refunded(id), 'Pul qaytarilgani belgilandi')
}

const canReceive = computed(() => ['Approved', 'PickupAssigned', 'PickedUp'].includes(selected.value?.status))
const canMarkRefunded = computed(() => selected.value?.status === 'Received' && selected.value?.refundMethod !== 'Bonus')

const returnTotal = computed(() =>
  (selected.value?.items || []).reduce((a, i) => a + (Number(i.unitPrice) || 0) * (Number(i.quantity) || 0), 0)
)

const timeline = computed(() => {
  const r = selected.value
  if (!r) return []
  return [
    ['So\'rov yuborildi', r.createdAt],
    ['Tasdiqlandi', r.approvedAt],
    ['Kuryer biriktirildi', r.pickupAssignedAt],
    ['Kuryer oldi', r.pickedUpAt],
    ['Do\'konga keldi', r.receivedAt],
    ['Pul qaytarildi', r.refundedAt],
    ['Rad etildi', r.rejectedAt],
    ['Bekor qilindi', r.cancelledAt]
  ].filter((t) => t[1])
})

const counts = computed(() => {
  const c = { open: 0, refunded: 0, sum: 0 }
  for (const r of items.value) {
    if (['Requested', 'Approved', 'PickupAssigned', 'PickedUp', 'Received'].includes(r.status)) c.open++
    if (r.status === 'Refunded') { c.refunded++; c.sum += Number(r.refundAmount) || 0 }
  }
  return c
})

watch(status, load)
watch(() => storeFilter.selectedBranchId, load)
onMounted(async () => {
  await storeFilter.ensureLoaded()
  load()
})

defineExpose({ load })
</script>

<template>
  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>

  <div class="card toolbar">
    <select v-model="status" class="select" style="width:220px">
      <option value="">Barcha holatlar</option>
      <option v-for="(v, k) in STATUS" :key="k" :value="k">{{ v.text }}</option>
    </select>
    <span class="muted small">Jarayonda: <b>{{ counts.open }}</b> · Qaytarilgan: <b>{{ counts.refunded }}</b> ({{ fmtNum(counts.sum) }} so'm)</span>
    <span v-if="storeFilter.selectedStore" class="muted small">Do'kon: {{ storeFilter.selectedStore.name }}</span>
  </div>

  <div class="layout tbl-main">
    <div class="card" style="overflow:auto">
      <table class="table">
        <thead>
          <tr><th>#</th><th>Sana</th><th>Buyurtma</th><th>Mijoz</th><th>Sabab</th><th>Usul</th><th class="text-right">Summa</th><th>Holat</th></tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="8" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
          <tr v-else-if="!items.length"><td colspan="8" class="muted" style="text-align:center; padding:24px">Qaytarish so'rovi yo'q</td></tr>
          <tr v-for="r in items" :key="r.id" class="clickable" :class="{ sel: selected?.id === r.id }" @click="open(r)">
            <td style="font-weight:700">{{ r.number }}</td>
            <td class="nowrap">{{ fmtDateTime(r.createdAt) }}</td>
            <td>{{ r.orderNumber ? '#' + r.orderNumber : '—' }}</td>
            <td>{{ r.customerName || '—' }}</td>
            <td class="small">{{ REASON[r.reason] || r.reason }}</td>
            <td class="small">{{ r.method ? METHOD[r.method] || r.method : '—' }}</td>
            <td class="text-right" style="font-weight:600">{{ fmtNum(r.refundAmount) }}</td>
            <td><span class="st" :class="statusOf(r.status).cls">{{ statusOf(r.status).text }}</span></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="selected" class="card detail">
      <div class="detail-head">
        <div>
          <span class="st" :class="statusOf(selected.status).cls">{{ statusOf(selected.status).text }}</span>
          <h3 style="margin:6px 0 0">Qaytarish #{{ selected.number }}</h3>
          <p class="muted small" style="margin:2px 0 0">
            <template v-if="selected.orderNumber">Buyurtma #{{ selected.orderNumber }}</template>
            <template v-if="selected.branchName"> · {{ selected.branchName }}</template>
          </p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <div class="info">
        <div v-if="selected.customerName"><b>{{ selected.customerName }}</b><span v-if="selected.customerPhone"> · {{ selected.customerPhone }}</span></div>
        <div>Sabab: <b>{{ REASON[selected.reason] || selected.reason }}</b></div>
        <div v-if="selected.comment" class="comment">Izoh: {{ selected.comment }}</div>
        <div>Pul qaytarish: {{ REFUND_METHOD[selected.refundMethod] || selected.refundMethod || '—' }}</div>
        <div v-if="selected.method">Usul: {{ METHOD[selected.method] || selected.method }}</div>
        <div v-if="selected.courierName">Kuryer: <b>{{ selected.courierName }}</b><span v-if="selected.courierPhone"> · {{ selected.courierPhone }}</span></div>
      </div>

      <div v-if="selected.rejectReason" class="info info-bad">Rad etish sababi: {{ selected.rejectReason }}</div>

      <div v-if="(selected.photoUrls || []).length">
        <div class="sec-title">Rasmlar</div>
        <div class="photos">
          <a v-for="(p, idx) in selected.photoUrls" :key="idx" :href="mediaUrl(p)" target="_blank" rel="noopener">
            <img :src="mediaUrl(p)" alt="" />
          </a>
        </div>
      </div>

      <div>
        <div class="sec-title">Tovarlar <span v-if="detailLoading" class="muted small">· yuklanmoqda...</span></div>
        <div v-for="i in selected.items || []" :key="i.id || i.onlineOrderItemId" class="item">
          <div>
            <div class="item-name">{{ i.productName }}</div>
            <div class="muted small">{{ fmtNum(i.unitPrice) }} × {{ fmtQty(i.quantity) }}</div>
          </div>
          <div class="item-sum">{{ fmtNum((Number(i.unitPrice) || 0) * (Number(i.quantity) || 0)) }}</div>
        </div>
      </div>

      <div class="totals">
        <div><span>Tovarlar summasi</span><span>{{ fmtNum(returnTotal) }}</span></div>
        <div v-if="Number(selected.loyaltyWithheldAmount) > 0" class="warn"><span>Keshbek ushlab qolindi</span><span>−{{ fmtNum(selected.loyaltyWithheldAmount) }}</span></div>
        <div v-if="Number(selected.refundAmount) > 0" class="grand"><span>Qaytariladigan pul</span><span>{{ fmtNum(selected.refundAmount) }} so'm</span></div>
      </div>

      <!-- Amallar -->
      <div v-if="selected.status === 'Requested'" class="actions">
        <div class="field" style="margin:0">
          <label>Qaytarish usuli</label>
          <select v-model="approveMethod" class="select">
            <option value="CourierPickup">{{ METHOD.CourierPickup }}</option>
            <option value="BringToStore">{{ METHOD.BringToStore }}</option>
          </select>
        </div>
        <button class="btn btn-success" :disabled="busy" @click="approve">Tasdiqlash</button>
        <div class="field" style="margin:8px 0 0">
          <label>Rad etish sababi</label>
          <input v-model="rejectReason" class="input" maxlength="500" />
        </div>
        <button class="btn btn-danger" :disabled="busy" @click="reject">Rad etish</button>
      </div>

      <div v-if="needsCourier" class="actions">
        <div class="field" style="margin:0">
          <label>Kuryer</label>
          <select v-model="courierId" class="select">
            <option value="">— tanlang —</option>
            <option v-for="c in couriers" :key="c.id" :value="c.id">{{ c.name }}<template v-if="c.phone"> · {{ c.phone }}</template> ({{ c.activeOrders || 0 }})</option>
          </select>
        </div>
        <button class="btn" :disabled="busy" @click="assign">{{ selected.status === 'PickupAssigned' ? 'Kuryerni almashtirish' : 'Kuryer biriktirish' }}</button>
      </div>

      <div v-if="canReceive || canMarkRefunded" class="actions">
        <button v-if="canReceive" class="btn" :disabled="busy" @click="received">Do'konga keldi</button>
        <button v-if="canMarkRefunded" class="btn btn-success" :disabled="busy" @click="refunded">Pul berildi</button>
      </div>

      <div v-if="timeline.length">
        <div class="sec-title">Vaqtlar</div>
        <div v-for="t in timeline" :key="t[0]" class="tl"><span class="muted">{{ t[0] }}</span><span>{{ fmtDateTime(t[1]) }}</span></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.layout { display: grid; grid-template-columns: minmax(0, 1fr) 400px; gap: 14px; align-items: start; }
.clickable { cursor: pointer; }
.clickable.sel { background: #f1f5f9; }
.st { display: inline-block; font-size: 12px; font-weight: 700; padding: 2px 8px; border-radius: 999px; white-space: nowrap; }
.st-new { background: #ffedd5; color: #9a3412; }
.st-work { background: #fef3c7; color: #92400e; }
.st-ready { background: #dbeafe; color: #1e40af; }
.st-done { background: #dcfce7; color: #166534; }
.st-bad { background: #fee2e2; color: #991b1b; }
.detail { display: flex; flex-direction: column; gap: 10px; }
.detail-head { display: flex; justify-content: space-between; align-items: flex-start; }
.info { display: flex; flex-direction: column; gap: 3px; background: #f8fafc; border-radius: 8px; padding: 8px 10px; font-size: 14px; }
.info-bad { background: #fef2f2; color: #991b1b; }
.comment { color: #9a3412; }
.sec-title { font-weight: 700; font-size: 13px; margin-bottom: 6px; }
.photos { display: flex; gap: 8px; flex-wrap: wrap; }
.photos img { width: 104px; height: 104px; object-fit: cover; border-radius: 8px; border: 1px solid var(--border); }
.item { display: flex; justify-content: space-between; gap: 8px; padding: 6px 0; border-bottom: 1px solid var(--border); }
.item-name { font-weight: 600; font-size: 14px; }
.item-sum { font-weight: 600; white-space: nowrap; }
.totals > div, .tl { display: flex; justify-content: space-between; gap: 8px; font-size: 14px; padding: 2px 0; }
.totals .grand { font-weight: 700; font-size: 16px; }
.totals .warn { color: #b45309; }
.actions { display: flex; flex-direction: column; gap: 6px; border-top: 1px solid var(--border); padding-top: 10px; }
@media (max-width: 1000px) {
  .layout { grid-template-columns: 1fr; }
}
</style>
