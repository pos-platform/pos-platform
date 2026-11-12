<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { onlineOrdersApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

// Online (mobil ilova) buyurtmalar — faqat ko'rish va hisobot. Amallar kassada (desktop) bajariladi.

const storeFilter = useStoreFilterStore()

const orders = ref([])
const loading = ref(false)
const err = ref('')
const selected = ref(null)
const detailLoading = ref(false)

function toDateStr(d) {
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}
function addDays(dateStr, days) {
  const d = new Date(`${dateStr}T00:00:00`)
  d.setDate(d.getDate() + days)
  return toDateStr(d)
}

const dateFrom = ref(addDays(toDateStr(new Date()), -6))
const dateTo = ref(toDateStr(new Date()))
const status = ref('')

const STATUS = {
  New: { text: 'Yangi', cls: 'st-new' },
  Accepted: { text: 'Qabul qilindi', cls: 'st-work' },
  Picking: { text: 'Yig\'ilmoqda', cls: 'st-work' },
  Ready: { text: 'Tayyor', cls: 'st-ready' },
  AwaitingTransfer: { text: 'O\'tkazma kutilmoqda', cls: 'st-ready' },
  ReadyForPickup: { text: 'Olib ketishga tayyor', cls: 'st-ready' },
  OnTheWay: { text: 'Yo\'lda', cls: 'st-ready' },
  Delivered: { text: 'Yetkazildi', cls: 'st-done' },
  Rejected: { text: 'Rad etildi', cls: 'st-bad' },
  Cancelled: { text: 'Bekor qilindi', cls: 'st-bad' }
}
const PAY = { Cash: 'Naqd', Card: 'Terminal', Transfer: 'O\'tkazma' }
const PAY_STATUS = { Paid: 'to\'langan', Pending: 'kutilmoqda', Unpaid: 'to\'lanmagan', Refunded: 'qaytarilgan' }
const POLICY = { ReplaceSimilar: 'O\'xshashiga almashtirish', Remove: 'Olib tashlash', CallMe: 'Mijozga qo\'ng\'iroq' }

const statusOf = (s) => STATUS[s] || { text: s, cls: 'st-work' }
const typeText = (o) => (o.fulfillmentType === 'Delivery' ? 'Yetkazish' : 'Olib ketish')
const payText = (o) => `${PAY[o.paymentMethod] || o.paymentMethod} · ${PAY_STATUS[o.paymentStatus] || o.paymentStatus}`

function fmtNum(n) {
  return Math.round(Number(n) || 0).toLocaleString('ru-RU')
}
function fmtDateTime(v) {
  if (!v) return '—'
  const d = new Date(v)
  return d.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function fmtQty(n) {
  return Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}

/** "+998901234567" → "+998 90 *** ** 67" (jadvalda; to'liq raqam faqat tafsilotda). */
function maskPhone(p) {
  if (!p) return ''
  const d = String(p).replace(/\D/g, '')
  if (d.length === 12 && d.startsWith('998')) return `+998 ${d.slice(3, 5)} *** ** ${d.slice(10)}`
  if (d.length <= 2) return '*'.repeat(d.length)
  return '*'.repeat(d.length - 2) + d.slice(-2)
}

function addressText(o) {
  if (o.fulfillmentType !== 'Delivery') return 'Olib ketish (do\'kondan)'
  const parts = []
  if (o.addressLine) parts.push(o.addressLine)
  if (o.entrance) parts.push(`${o.entrance}-podyezd`)
  if (o.floor) parts.push(`${o.floor}-qavat`)
  if (o.apartment) parts.push(`${o.apartment}-xonadon`)
  if (o.intercom) parts.push(`domofon ${o.intercom}`)
  if (o.landmark) parts.push(`mo'ljal: ${o.landmark}`)
  return parts.join(', ') || '—'
}

async function load() {
  if (!dateFrom.value || !dateTo.value) return
  loading.value = true
  err.value = ''
  try {
    // Backend 'to' kunini o'zi qamrab oladi (shu kun ham kiradi) — tugash sanasi o'zgarishsiz yuboriladi.
    const params = { active: false, from: dateFrom.value, to: dateTo.value }
    if (status.value) params.status = status.value
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    const list = await onlineOrdersApi.list(params)
    orders.value = [...list].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    if (selected.value && !orders.value.some((o) => o.id === selected.value.id)) selected.value = null
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function open(o) {
  selected.value = o
  detailLoading.value = true
  try {
    const full = await onlineOrdersApi.get(o.id)
    if (selected.value?.id === o.id) selected.value = full
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

const summary = computed(() => {
  let deliveredSum = 0
  let deliveredCount = 0
  let cancelled = 0
  for (const o of orders.value) {
    if (o.status === 'Delivered') {
      deliveredSum += Number(o.totalAmount) || 0
      deliveredCount++
    } else if (o.status === 'Cancelled' || o.status === 'Rejected') {
      cancelled++
    }
  }
  return { count: orders.value.length, deliveredSum, deliveredCount, cancelled }
})

// Tafsilot: qator holati (yig'ilgan / olib tashlangan / almashtirilgan)
const itemRows = computed(() => {
  const items = selected.value?.items || []
  const byId = Object.fromEntries(items.map((i) => [i.id, i]))
  return items.map((i) => {
    const notes = []
    if (i.isRemoved) notes.push({ text: 'olib tashlandi', cls: 'n-bad' })
    else if (i.pickedQuantity != null && Number(i.pickedQuantity) !== Number(i.quantity))
      notes.push({ text: `olindi ${fmtQty(i.pickedQuantity)} (so'ralgan ${fmtQty(i.quantity)})`, cls: 'n-warn' })
    if (i.replacesItemId) {
      const old = byId[i.replacesItemId]
      notes.push({ text: `almashtirish: ${old ? old.productName : 'boshqa mahsulot'} o'rniga`, cls: 'n-info' })
    }
    // Markirovka: skanerlangan kodlar soni
    if (i.isMarked) {
      const n = i.markingCodes?.length || 0
      notes.push({ text: n ? `markirovka: ${n} ta kod` : 'markirovkali (kod yo\'q)', cls: n ? 'n-info' : 'n-warn' })
    }
    const qty = i.isRemoved ? 0 : (i.pickedQuantity ?? i.quantity)
    return { ...i, notes, qty }
  })
})

// Kuryer naqd olib yetkazgan, lekin kassaga hali topshirmagan
const courierCashPending = computed(() => {
  const o = selected.value
  return !!o && 'courierCashHandedOverAt' in o && o.status === 'Delivered' && o.paymentMethod === 'Cash' && o.fulfillmentType === 'Delivery' &&
    !!o.courierName && !o.courierCashHandedOverAt
})

const timeline = computed(() => {
  const o = selected.value
  if (!o) return []
  return [
    ['Yaratildi', o.createdAt],
    ['Qabul qilindi', o.acceptedAt],
    ['Yig\'ish boshlandi', o.pickingStartedAt],
    ['Tayyor', o.readyAt],
    ['To\'lov tasdiqlandi', o.paymentConfirmedAt],
    ['Kuryerga berildi', o.dispatchedAt],
    ['Yetkazildi / topshirildi', o.deliveredAt],
    ['Bekor / rad', o.cancelledAt]
  ].filter((t) => t[1])
})

watch([dateFrom, dateTo, status], () => load())
watch(() => storeFilter.selectedBranchId, () => load())

onMounted(async () => {
  await storeFilter.ensureLoaded()
  load()
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Online buyurtmalar</h1>
      <div class="muted small">Mobil ilovadan kelgan buyurtmalar. Buyurtma bilan ishlash — kassadagi "Online" oynasida.</div>
    </div>
    <button class="btn btn-ghost btn-sm" :disabled="loading" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="card toolbar">
    <div class="field-inline">
      <span class="muted small">Sana</span>
      <input v-model="dateFrom" type="date" class="input date-in" :max="dateTo" />
      <span class="muted">—</span>
      <input v-model="dateTo" type="date" class="input date-in" :min="dateFrom" />
    </div>
    <select v-model="status" class="select" style="width:200px">
      <option value="">Barcha holatlar</option>
      <option v-for="(v, k) in STATUS" :key="k" :value="k">{{ v.text }}</option>
    </select>
    <span v-if="storeFilter.selectedStore" class="muted small">Do'kon: {{ storeFilter.selectedStore.name }}</span>
  </div>

  <div class="summary">
    <div class="card stat">
      <div class="stat-label">Buyurtmalar</div>
      <div class="stat-value">{{ summary.count }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Yetkazilgan summa</div>
      <div class="stat-value">{{ fmtNum(summary.deliveredSum) }}</div>
      <div class="muted small">{{ summary.deliveredCount }} ta buyurtma</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Bekor / rad etilgan</div>
      <div class="stat-value" style="color: var(--danger)">{{ summary.cancelled }}</div>
    </div>
  </div>

  <div class="layout">
    <div class="card" style="overflow:auto">
      <table class="table">
        <thead>
          <tr>
            <th>#</th>
            <th>Sana</th>
            <th>Mijoz</th>
            <th>Telefon</th>
            <th>Turi</th>
            <th>To'lov</th>
            <th class="text-right">Summa</th>
            <th>Holat</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="8" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
          <tr v-else-if="!orders.length"><td colspan="8" class="muted" style="text-align:center; padding:24px">Buyurtma yo'q</td></tr>
          <tr v-for="o in orders" :key="o.id" class="clickable" :class="{ sel: selected?.id === o.id }" @click="open(o)">
            <td style="font-weight:700">{{ o.number }}</td>
            <td class="nowrap">{{ fmtDateTime(o.createdAt) }}</td>
            <td>{{ o.customerName }}</td>
            <td class="muted nowrap">{{ maskPhone(o.customerPhone) }}</td>
            <td>{{ typeText(o) }}</td>
            <td class="small">{{ payText(o) }}</td>
            <td class="text-right" style="font-weight:600">{{ fmtNum(o.totalAmount) }}</td>
            <td><span class="st" :class="statusOf(o.status).cls">{{ statusOf(o.status).text }}</span></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="selected" class="card detail">
      <div class="detail-head">
        <div>
          <span class="st" :class="statusOf(selected.status).cls">{{ statusOf(selected.status).text }}</span>
          <h3 style="margin:6px 0 0">Buyurtma #{{ selected.number }}</h3>
          <p class="muted small" style="margin:2px 0 0">{{ selected.branchName || '' }}</p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <div class="info">
        <div><b>{{ selected.customerName }}</b> · {{ selected.customerPhone }}</div>
        <div>{{ typeText(selected) }}: {{ addressText(selected) }}<span v-if="selected.contactless"> · kontaktsiz</span></div>
        <div v-if="selected.scheduledFor">Vaqt: {{ fmtDateTime(selected.scheduledFor) }} ga</div>
        <div v-if="selected.comment" class="comment">Izoh: {{ selected.comment }}</div>
        <div class="muted small">Tugasa: {{ POLICY[selected.outOfStockPolicy] || selected.outOfStockPolicy }}</div>
      </div>

      <div v-if="selected.courierName || selected.courierPhone || selected.courierUserId" class="info">
        <div>
          Kuryer: <b>{{ selected.courierName || '—' }}</b> <span v-if="selected.courierPhone">· {{ selected.courierPhone }}</span>
          <span v-if="selected.courierUserId" class="muted small"> · ilova orqali</span>
        </div>
        <!-- Kuryer naqdi: olingan summa va kassaga topshirilganligi -->
        <div v-if="selected.courierCashCollected != null || courierCashPending" class="small">
          <template v-if="selected.courierCashCollected != null">Naqd olindi: <b>{{ fmtNum(selected.courierCashCollected) }}</b> so'm · </template>
          <span v-if="selected.courierCashHandedOverAt" style="color:#166534">kassaga topshirildi {{ fmtDateTime(selected.courierCashHandedOverAt) }}</span>
          <span v-else style="color:#b45309">kassaga hali topshirilmagan</span>
        </div>
      </div>

      <div v-if="selected.cancelReason" class="info info-bad">Sabab: {{ selected.cancelReason }}</div>

      <div>
        <div class="sec-title">Mahsulotlar <span v-if="detailLoading" class="muted small">· yuklanmoqda...</span></div>
        <div v-for="i in itemRows" :key="i.id" class="item" :class="{ removed: i.isRemoved }">
          <div class="item-main">
            <div class="item-name">{{ i.productName }}</div>
            <div class="muted small">{{ i.barcode || '—' }} · {{ fmtNum(i.unitPrice) }} × {{ fmtQty(i.qty) }}</div>
            <div v-for="(n, idx) in i.notes" :key="idx" class="note" :class="n.cls">{{ n.text }}</div>
          </div>
          <div class="item-sum">{{ fmtNum(i.isRemoved ? 0 : i.lineTotal) }}</div>
        </div>
      </div>

      <div class="totals">
        <div><span>Mahsulotlar</span><span>{{ fmtNum(selected.itemsTotal) }}</span></div>
        <div v-if="Number(selected.promoDiscount) > 0" class="disc">
          <span>Promokod<span v-if="selected.promoCode" class="code"> {{ selected.promoCode }}</span></span>
          <span>−{{ fmtNum(selected.promoDiscount) }}</span>
        </div>
        <div v-else-if="selected.promoCode" class="muted small"><span>Promokod</span><span class="code">{{ selected.promoCode }}</span></div>
        <div v-if="Number(selected.bonusUsed) > 0" class="disc"><span>Bonus ballar</span><span>−{{ fmtNum(selected.bonusUsed) }}</span></div>
        <div v-if="Number(selected.deliveryFee) > 0"><span>Yetkazish</span><span>{{ fmtNum(selected.deliveryFee) }}</span></div>
        <div class="grand"><span>Jami</span><span>{{ fmtNum(selected.totalAmount) }} so'm</span></div>
        <div class="muted small"><span>To'lov</span><span>{{ payText(selected) }}</span></div>
        <div v-if="selected.receiptNumber" class="muted small"><span>Chek raqami</span><span>{{ selected.receiptNumber }}</span></div>
      </div>

      <div>
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
.field-inline { display: flex; align-items: center; gap: 6px; }
.date-in { width: 150px; }
.summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin-bottom: 12px; }
.stat-label { font-size: 12px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 22px; font-weight: 700; margin-top: 4px; }
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
.item { display: flex; justify-content: space-between; gap: 8px; padding: 6px 0; border-bottom: 1px solid var(--border); }
.item.removed .item-name { text-decoration: line-through; color: var(--muted); }
.item-name { font-weight: 600; font-size: 14px; }
.item-sum { font-weight: 600; white-space: nowrap; }
.note { font-size: 12px; font-weight: 600; margin-top: 2px; }
.n-bad { color: #b91c1c; }
.n-warn { color: #b45309; }
.n-info { color: #1d4ed8; }
.totals > div, .tl { display: flex; justify-content: space-between; gap: 8px; font-size: 14px; padding: 2px 0; }
.totals .grand { font-weight: 700; font-size: 16px; }
.totals .disc { color: #166534; }
.code { font-family: ui-monospace, Consolas, monospace; }
@media (max-width: 1000px) {
  .layout { grid-template-columns: 1fr; }
  .summary { grid-template-columns: 1fr; }
}
</style>
