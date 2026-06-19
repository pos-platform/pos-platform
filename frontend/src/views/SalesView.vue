<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { salesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()

const sales = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const refundingId = ref(null)
const printingId = ref(null)

// Standart — bugungi kun (lokal sana, YYYY-MM-DD)
function todayStr() {
  const d = new Date()
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}
const date = ref(todayStr())

const fiscalPill = {
  NotSent: 'pill-gray',
  Queued: 'pill-amber',
  Registered: 'pill-green',
  Failed: 'pill-red'
}
const fiscalLabel = {
  NotSent: 'Yuborilmagan',
  Queued: 'Navbatda',
  Registered: 'Fiskal',
  Failed: 'Xato'
}
const paymentLabel = { Cash: 'Naqd', Card: 'Karta', Mixed: 'Aralash', Debt: 'Nasiya' }

function fmtTime(d) {
  return new Date(d).toLocaleTimeString('uz-UZ', { hour: '2-digit', minute: '2-digit' })
}
function fmtNum(n) {
  return Number(n || 0).toLocaleString()
}

// Keyingi kun (sana-only, YYYY-MM-DD) — backend [from, to) UTC kalendar kuni sifatida oladi
function nextDayStr(dateStr) {
  const d = new Date(`${dateStr}T00:00:00`)
  d.setDate(d.getDate() + 1)
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = { from: date.value, to: nextDayStr(date.value) }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    sales.value = await salesApi.list(params)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Kunlik jami: faqat sotuvlar soni, naqd/karta/nasiya, qaytarishlar va sof tushum
const summary = computed(() => {
  let count = 0
  let cash = 0
  let card = 0
  let debt = 0
  let refunds = 0
  for (const s of sales.value) {
    const amt = Number(s.totalAmount || 0)
    if (s.type === 'Refund') {
      refunds += amt
      continue
    }
    count += 1
    if (s.paymentMethod === 'Cash') cash += amt
    else if (s.paymentMethod === 'Card') card += amt
    else if (s.paymentMethod === 'Debt') debt += amt
    else if (s.paymentMethod === 'Mixed') cash += amt
  }
  const gross = cash + card + debt
  return { count, cash, card, debt, refunds, net: gross - refunds }
})

watch(() => storeFilter.selectedBranchId, () => load())
watch(date, () => load())

onMounted(load)

function shiftDay(days) {
  const d = new Date(`${date.value}T00:00:00`)
  d.setDate(d.getDate() + days)
  const off = d.getTimezoneOffset()
  date.value = new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}

async function refund(sale) {
  if (!window.confirm(`"${sale.receiptNumber}" chekni qaytarasizmi?`)) return
  error.value = ''
  success.value = ''
  refundingId.value = sale.id
  try {
    const r = await salesApi.refund(sale.id)
    success.value = `Qaytarildi — return cheki ${r.receiptNumber}`
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    refundingId.value = null
  }
}

// Chekni to'liq yuklab, brauzer print dialogi orqali chop etadi (80mm chek shakli).
async function printReceipt(saleRow) {
  printingId.value = saleRow.id
  error.value = ''
  try {
    const sale = await salesApi.get(saleRow.id)
    openReceiptWindow(sale)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    printingId.value = null
  }
}

function openReceiptWindow(sale) {
  const isRefund = sale.type === 'Refund'
  const itemsHtml = (sale.items || [])
    .map(
      (i) => `<tr>
      <td>${i.productName}</td>
      <td style="text-align:right">${Number(i.quantity).toLocaleString()}</td>
      <td style="text-align:right">${Number(i.unitPrice).toLocaleString()}</td>
      <td style="text-align:right">${Number(i.lineTotal).toLocaleString()}</td>
    </tr>`
    )
    .join('')

  const rc = sale.receipt
  const fiscalHtml =
    rc && rc.fiscalStatus === 'Registered'
      ? `<hr/>
  <div class="center bold">*** FISKAL CHEK ***</div>
  ${rc.fiscalReceiptNumber ? `<div class="center">Fiskal raqam: ${rc.fiscalReceiptNumber}</div>` : ''}
  ${rc.fiscalSign ? `<div class="center">FP: ${rc.fiscalSign}</div>` : ''}
  ${rc.qrCodeUrl ? `<div class="center" style="word-break:break-all;font-size:10px">${rc.qrCodeUrl}</div><div class="center muted">ofd.soliq.uz orqali tekshiring</div>` : ''}`
      : ''

  const w = window.open('', '_blank', 'width=380,height=640')
  w.document.write(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8"/>
  <title>Chek ${sale.receiptNumber}</title>
  <style>
    * { margin:0; padding:0; box-sizing:border-box; }
    body { font-family: monospace; font-size: 13px; padding: 16px; }
    h2 { text-align:center; font-size:15px; margin-bottom:4px; }
    .center { text-align:center; }
    .muted { color:#666; }
    .bold { font-weight:bold; }
    hr { border:none; border-top:1px dashed #999; margin:8px 0; }
    table { width:100%; border-collapse:collapse; }
    th { text-align:left; padding:3px 0; border-bottom:1px solid #ccc; }
    td { padding:3px 0; vertical-align:top; }
    .row { display:flex; justify-content:space-between; margin:3px 0; }
    .total { font-weight:bold; font-size:14px; }
    @page { size: 80mm auto; margin: 4mm; }
    @media print { body { padding:4px; } }
  </style>
</head>
<body>
  <h2>PosPlatform</h2>
  ${isRefund ? '<div class="center bold">QAYTARISH CHEKI</div>' : ''}
  <div class="center muted">${new Date(sale.createdAt || Date.now()).toLocaleString('uz-UZ')}</div>
  <div class="center">Chek: <strong>${sale.receiptNumber}</strong></div>
  <hr/>
  <table>
    <thead><tr>
      <th>Nomi</th>
      <th style="text-align:right">Soni</th>
      <th style="text-align:right">Narx</th>
      <th style="text-align:right">Jami</th>
    </tr></thead>
    <tbody>${itemsHtml}</tbody>
  </table>
  <hr/>
  <div class="row total"><span>Jami:</span><span>${isRefund ? '− ' : ''}${Number(sale.totalAmount).toLocaleString()} so'm</span></div>
  ${Number(sale.discountAmount) > 0 ? `<div class="row"><span>Chegirma:</span><span>${Number(sale.discountAmount).toLocaleString()} so'm</span></div>` : ''}
  <div class="row"><span>To'lov:</span><span>${paymentLabel[sale.paymentMethod] || sale.paymentMethod}</span></div>
  ${sale.paymentMethod === 'Cash' && !isRefund ? `<div class="row"><span>Qaytim:</span><span>${Number(sale.changeAmount).toLocaleString()} so'm</span></div>` : ''}
  ${fiscalHtml}
  <hr/>
  <div class="center muted">Xarid uchun rahmat!</div>
  <script>setTimeout(function(){window.print();},300);<\/script>
</body>
</html>`)
  w.document.close()
}
</script>

<template>
  <div class="page-head">
    <h1>Kunlik savdolar</h1>
    <div style="display: flex; gap: 8px; align-items: center">
      <button class="btn btn-ghost btn-sm" @click="shiftDay(-1)" title="Oldingi kun">‹</button>
      <input type="date" class="input" style="width: 160px" v-model="date" />
      <button class="btn btn-ghost btn-sm" @click="shiftDay(1)" title="Keyingi kun">›</button>
      <button class="btn btn-ghost btn-sm" @click="load">Yangilash</button>
    </div>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Kunlik jami -->
  <div class="summary-grid">
    <div class="card stat">
      <div class="stat-label">Savdolar</div>
      <div class="stat-value">{{ summary.count }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Naqd</div>
      <div class="stat-value">{{ fmtNum(summary.cash) }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Karta</div>
      <div class="stat-value">{{ fmtNum(summary.card) }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Nasiya</div>
      <div class="stat-value">{{ fmtNum(summary.debt) }}</div>
    </div>
    <div class="card stat">
      <div class="stat-label">Qaytarish</div>
      <div class="stat-value" style="color: var(--danger)">− {{ fmtNum(summary.refunds) }}</div>
    </div>
    <div class="card stat stat-net">
      <div class="stat-label">Sof tushum</div>
      <div class="stat-value">{{ fmtNum(summary.net) }}</div>
    </div>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Chek</th>
          <th>Tur</th>
          <th>Vaqt</th>
          <th>To'lov</th>
          <th class="text-right">Jami</th>
          <th>Fiskal</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in sales" :key="s.id">
          <td><strong>{{ s.receiptNumber }}</strong></td>
          <td>
            <span v-if="s.type === 'Refund'" class="pill pill-red">Qaytarish</span>
            <span v-else-if="s.status === 'Refunded'" class="pill pill-amber">Qaytarilgan</span>
            <span v-else class="pill pill-gray">Sotuv</span>
          </td>
          <td class="muted">{{ fmtTime(s.createdAt) }}</td>
          <td>{{ paymentLabel[s.paymentMethod] || s.paymentMethod }}</td>
          <td class="text-right" :style="{ color: s.type === 'Refund' ? 'var(--danger)' : 'inherit' }">
            {{ s.type === 'Refund' ? '− ' : '' }}{{ fmtNum(s.totalAmount) }}
          </td>
          <td>
            <span class="pill" :class="fiscalPill[s.receipt?.fiscalStatus] || 'pill-gray'">
              {{ fiscalLabel[s.receipt?.fiscalStatus] || '—' }}
            </span>
          </td>
          <td class="text-right" style="white-space: nowrap">
            <button
              class="btn btn-ghost btn-sm"
              :disabled="printingId === s.id"
              @click="printReceipt(s)"
              title="Chekni chop etish"
            >
              {{ printingId === s.id ? '...' : '🖨 Chek' }}
            </button>
            <button
              v-if="s.type === 'Sale' && s.status !== 'Refunded'"
              class="btn btn-ghost btn-sm"
              :disabled="refundingId === s.id"
              @click="refund(s)"
            >
              {{ refundingId === s.id ? '...' : 'Qaytarish' }}
            </button>
          </td>
        </tr>
        <tr v-if="!sales.length && !loading">
          <td colspan="7" class="muted" style="text-align: center; padding: 24px">
            Bu kunda savdo yo'q
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.summary-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 12px;
  margin-bottom: 16px;
}
.stat {
  padding: 14px 16px;
}
.stat-label {
  font-size: 12px;
  color: var(--muted, #6b7280);
  margin-bottom: 6px;
}
.stat-value {
  font-size: 20px;
  font-weight: 700;
}
.stat-net {
  background: #ecfdf5;
}
@media (max-width: 980px) {
  .summary-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>
