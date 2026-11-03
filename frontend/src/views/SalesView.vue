<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { salesApi, fiscalModulesApi } from '../api/catalog'
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
  return toDateStr(new Date())
}
/** Date → lokal YYYY-MM-DD (UTC ga siljib ketmasligi uchun offset ayiriladi). */
function toDateStr(d) {
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}
/** Sanani kun bo'yicha siljitadi (YYYY-MM-DD → YYYY-MM-DD). */
function addDays(dateStr, days) {
  const d = new Date(`${dateStr}T00:00:00`)
  d.setDate(d.getDate() + days)
  return toDateStr(d)
}

// Davr: boshlanish va tugash sanasi (ikkalasi ham qamrab olinadi).
const dateFrom = ref(todayStr())
const dateTo = ref(todayStr())

/** Davrdagi kunlar soni — 1 bo'lsa bir kunlik hisobot. */
const dayCount = computed(() => {
  const a = new Date(`${dateFrom.value}T00:00:00`)
  const b = new Date(`${dateTo.value}T00:00:00`)
  return Math.max(1, Math.round((b - a) / 86400000) + 1)
})
const isRange = computed(() => dayCount.value > 1)

/** Davr sarlavhasi: bir kun bo'lsa bitta sana, aks holda oraliq. */
const periodLabel = computed(() =>
  isRange.value ? `${dateFrom.value} — ${dateTo.value} (${dayCount.value} kun)` : dateFrom.value
)

/** Tayyor davrlar — bir bosishda tanlash uchun. */
function setPreset(preset) {
  const today = todayStr()
  if (preset === 'today') { dateFrom.value = today; dateTo.value = today }
  else if (preset === 'yesterday') { const y = addDays(today, -1); dateFrom.value = y; dateTo.value = y }
  else if (preset === 'week') { dateFrom.value = addDays(today, -6); dateTo.value = today }
  else if (preset === 'month') { dateFrom.value = `${today.slice(0, 7)}-01`; dateTo.value = today }
}
function isPreset(preset) {
  const today = todayStr()
  if (preset === 'today') return dateFrom.value === today && dateTo.value === today
  if (preset === 'yesterday') return dateFrom.value === addDays(today, -1) && dateTo.value === dateFrom.value
  if (preset === 'week') return dateFrom.value === addDays(today, -6) && dateTo.value === today
  if (preset === 'month') return dateFrom.value === `${today.slice(0, 7)}-01` && dateTo.value === today
  return false
}

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
const paymentLabel = {
  Cash: 'Naqd',
  Card: 'Karta',
  Mixed: 'Aralash',
  Debt: 'Nasiya',
  Transfer: "O'tkazma"
}

function fmtTime(d) {
  return new Date(d).toLocaleTimeString('uz-UZ', { hour: '2-digit', minute: '2-digit' })
}
/** Ro'yxatda: bir kunlik hisobotda faqat vaqt, davr bo'yicha esa sana ham. */
function fmtWhen(d) {
  if (!isRange.value) return fmtTime(d)
  const dt = new Date(d)
  return `${dt.toLocaleDateString('uz-UZ', { day: '2-digit', month: '2-digit' })} ${fmtTime(d)}`
}
function fmtNum(n) {
  return Number(n || 0).toLocaleString()
}

async function load() {
  // Sana maydoni yarim kiritilgan/tozalangan bo'lsa — so'rov yubormaymiz
  if (!dateFrom.value || !dateTo.value) return
  loading.value = true
  error.value = ''
  try {
    // backend [from, to) oladi — tugash kuni ham qamrab olinishi uchun +1 kun
    const params = { from: dateFrom.value, to: addDays(dateTo.value, 1) }
    if (storeFilter.selectedBranchId) params.branchId = storeFilter.selectedBranchId
    sales.value = await salesApi.list(params)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

/**
 * Chek haqiqatan soliqqa o'tganmi. "QR kodli chek" deb faqat QR kod shakllangan
 * (ofd havolasi bor) yoki soliq fiskal belgisi + chek raqami qaytgan cheklar hisoblanadi.
 * Faqat "Registered" statusi yetarli emas — QR shakllanmagan bo'lsa chek oddiy hisoblanadi.
 */
function isFiscal(s) {
  const r = s.receipt
  if (!r || r.fiscalStatus !== 'Registered') return false
  if (r.qrCodeUrl) return true
  return Boolean(r.fiscalSign && r.fiscalReceiptNumber)
}

/** Status "Registered", lekin QR/fiskal belgi yo'q — tasdiqlanmagan fiskalizatsiya. */
function isUnconfirmedFiscal(s) {
  return s.receipt?.fiscalStatus === 'Registered' && !isFiscal(s)
}

/** Tanlangan davrda tasdiqlanmagan (QR shakllanmagan) cheklar soni. */
const unconfirmedCount = computed(() =>
  terminalSales.value.filter((s) => s.type !== 'Refund' && isUnconfirmedFiscal(s)).length
)

// To'lov turlari — hisobotlarning barcha kesimlarida shu tartibda ko'rsatiladi.
const PAY_KEYS = ['cash', 'card', 'transfer', 'debt', 'mixed']
const PAY_LABELS = {
  cash: 'Naqd',
  card: 'Karta',
  transfer: "O'tkazma",
  debt: 'Nasiya',
  mixed: 'Aralash'
}
const PAY_BY_METHOD = { Cash: 'cash', Card: 'card', Transfer: 'transfer', Debt: 'debt', Mixed: 'mixed' }
function payKey(s) { return PAY_BY_METHOD[s.paymentMethod] || 'cash' }

/** Bo'sh hisob birligi: umumiy + har bir to'lov turi bo'yicha QR/QR-siz taqsimot. */
function emptyBucket(extra = {}) {
  const b = {
    count: 0, total: 0,
    fiscalCount: 0, fiscalSum: 0,
    plainCount: 0, plainSum: 0,
    pay: {},
    ...extra
  }
  for (const k of PAY_KEYS) b.pay[k] = { total: 0, count: 0, fiscal: 0, fiscalCount: 0, plain: 0, plainCount: 0 }
  return b
}

/** Bitta savdoni hisob birligiga qo'shadi (qaytarishlar chaqiruvchida ajratiladi). */
function addSale(b, s) {
  const amt = Number(s.totalAmount || 0)
  const p = b.pay[payKey(s)]
  b.count += 1
  b.total += amt
  p.count += 1
  p.total += amt
  if (isFiscal(s)) {
    b.fiscalCount += 1; b.fiscalSum += amt
    p.fiscalCount += 1; p.fiscal += amt
  } else {
    b.plainCount += 1; b.plainSum += amt
    p.plainCount += 1; p.plain += amt
  }
}

/** Savdolarni keyFn bo'yicha guruhlab, har guruh uchun hisob birligini qaytaradi. */
function groupSales(list, keyFn, metaFn) {
  const map = new Map()
  for (const s of list) {
    if (s.type === 'Refund') continue
    const key = keyFn(s)
    if (key === null) continue
    if (!map.has(key)) map.set(key, emptyBucket(metaFn ? metaFn(s) : {}))
    addSale(map.get(key), s)
  }
  return map
}

// Jadval filtrlari: fiskal holati va kassa (terminal)
const fiscalFilter = ref('all') // all | fiscal | plain
const terminalFilter = ref('all')

function terminalOf(s) { return s.terminalId || '' }
function terminalLabel(t) { return t || "Kassa ko'rsatilmagan" }

/** Tanlangan davrda savdo bo'lgan kassalar ro'yxati (dropdown uchun). */
const terminals = computed(() => {
  const set = new Set(sales.value.map(terminalOf))
  return [...set].sort((a, b) => a.localeCompare(b))
})

/** Kassa filtri qo'llangan cheklar — jami kartalar ham shu asosda hisoblanadi. */
const terminalSales = computed(() =>
  terminalFilter.value === 'all'
    ? sales.value
    : sales.value.filter((s) => terminalOf(s) === terminalFilter.value)
)

// Davr bo'yicha jami: sotuvlar soni, naqd/karta/nasiya, qaytarishlar, sof tushum
// va alohida — fiskal (QR kodli) va fiskalsiz (oddiy) cheklar summasi.
const summary = computed(() => {
  const b = emptyBucket()
  let refunds = 0
  for (const s of terminalSales.value) {
    if (s.type === 'Refund') { refunds += Number(s.totalAmount || 0); continue }
    addSale(b, s)
  }
  return { ...b, refunds, net: b.total - refunds }
})

const shownSales = computed(() =>
  terminalSales.value.filter((s) => {
    if (fiscalFilter.value === 'fiscal') return s.type !== 'Refund' && isFiscal(s)
    if (fiscalFilter.value === 'plain') return s.type !== 'Refund' && !isFiscal(s)
    return true
  })
)

/** Kassalar kesimi: har bir kassa bo'yicha to'lov turlari va QR/QR-siz taqsimot. */
const perTerminal = computed(() =>
  [...groupSales(sales.value, terminalOf, (s) => ({ terminal: terminalOf(s) })).values()]
    .sort((a, b) => a.terminal.localeCompare(b.terminal))
)

// ── Fiskal modul (kassa apparati) × kassir kesimi ───────────────────────────
// Bir nechta kompyuterda alohida fleshka turib, foydalanuvchilar loginlarni
// aralashtirib ishlatganda — qaysi QR kodli chek qaysi modulga va kimga
// tegishli ekanini aniq ko'rsatadi.
function moduleOf(s) { return s.receipt?.fiscalModuleId || '' }
function cashierOf(s) { return s.cashierFullName || s.cashierUsername || '—' }
function moduleLabel(m) { return m || "Modul ID yo'q" }

// Modul kartochkalari: modul ID → { ownerName, inn, registrationNumber, ... }
const moduleCards = ref({})

/** Modulning ism-sharifi (kartochkada saqlangan nomi) — bo'lmasa bo'sh. */
function moduleOwner(m) { return moduleCards.value[m]?.ownerName || '' }

/** Hisobotda ko'rsatiladigan nom: ism-sharif bo'lsa u, aks holda modul ID. */
function moduleTitle(m) { return moduleOwner(m) || moduleLabel(m) }

async function loadModuleCards() {
  try {
    const list = await fiscalModulesApi.list()
    moduleCards.value = Object.fromEntries(list.map((m) => [m.moduleId, m]))
  } catch {
    // kartochkalar yuklanmasa ham hisobot modul ID bilan ishlaydi
  }
}

/** Modulga ism-sharif (va ixtiyoriy INN) biriktiradi. */
async function renameModule(moduleId) {
  const card = moduleCards.value[moduleId] || {}
  const name = window.prompt(
    `Fiskal modul ${moduleLabel(moduleId)} — kimga tegishli (ism-sharif yoki tashkilot nomi)?`,
    card.ownerName || ''
  )
  if (name === null) return
  try {
    const saved = await fiscalModulesApi.save({
      moduleId,
      ownerName: name,
      inn: card.inn ?? null,
      registrationNumber: card.registrationNumber ?? null,
      terminalId: card.terminalId ?? null,
      note: card.note ?? null
    })
    moduleCards.value = { ...moduleCards.value, [moduleId]: saved }
    success.value = 'Fiskal modul nomi saqlandi'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

/** Faqat QR kodli (fiskalizatsiya bo'lgan) savdolar — modul kesimlari uchun asos. */
const fiscalSales = computed(() => sales.value.filter((s) => s.type !== 'Refund' && isFiscal(s)))

/** Fiskal modullar (fleshkalar) kesimi: har bir fleshkadan qancha QR chek chiqqani. */
const perFiscalModule = computed(() => {
  const map = groupSales(
    fiscalSales.value,
    moduleOf,
    (s) => ({ module: moduleOf(s), rs: s.receipt?.fiscalRegistrationNumber || '', terminals: new Set(), cashiers: new Set() })
  )
  for (const s of fiscalSales.value) {
    const row = map.get(moduleOf(s))
    row.terminals.add(terminalLabel(terminalOf(s)))
    row.cashiers.add(cashierOf(s))
  }
  return [...map.values()]
    .map((r) => ({ ...r, terminals: [...r.terminals].sort(), cashiers: [...r.cashiers].sort() }))
    .sort((a, b) => a.module.localeCompare(b.module))
})

const perModule = computed(() => {
  const rows = [...groupSales(
    fiscalSales.value,
    (s) => `${moduleOf(s)}||${cashierOf(s)}||${terminalOf(s)}`,
    (s) => ({
      module: moduleOf(s),
      rs: s.receipt?.fiscalRegistrationNumber || '',
      cashier: cashierOf(s),
      terminal: terminalOf(s)
    })
  ).values()]
  return rows.sort((a, b) =>
    a.module.localeCompare(b.module) ||
    a.cashier.localeCompare(b.cashier) ||
    a.terminal.localeCompare(b.terminal)
  )
})

/**
 * Pastki jadval uchun: har bir fiskal modul alohida blok, ichida kassirlar qatorlari.
 * Shu tarzda ikkita fleshka bir-biriga aralashmay, aniq ajratilib ko'rinadi.
 */
const moduleGroups = computed(() => {
  const groups = new Map()
  for (const r of perModule.value) {
    if (!groups.has(r.module)) {
      const totals = perFiscalModule.value.find((m) => m.module === r.module)
      groups.set(r.module, { module: r.module, rs: r.rs, totals, rows: [] })
    }
    groups.get(r.module).rows.push(r)
  }
  return [...groups.values()]
})

/**
 * Bitta fiskal modul bir nechta kassaning cheklarini fiskalizatsiya qilganmi.
 * Ya'ni o'sha kassada modul bo'lmasa ham cheki boshqa PC dagi fleshkadan o'tgan —
 * agentda "FiscalTerminalId" to'ldirilmagani belgisi.
 */
const crossTerminalModules = computed(() =>
  perFiscalModule.value
    .filter((m) => m.terminals.length > 1)
    .map((m) => ({ title: moduleTitle(m.module), terminals: m.terminals }))
)

/** Bir kassir tanlangan davrda bir nechta fiskal modulda ishlaganmi — aralashib ketgan loginlar. */
const mixedCashiers = computed(() => {
  const byCashier = new Map()
  for (const r of perModule.value) {
    if (!byCashier.has(r.cashier)) byCashier.set(r.cashier, new Set())
    byCashier.get(r.cashier).add(moduleTitle(r.module))
  }
  return [...byCashier.entries()]
    .filter(([, mods]) => mods.size > 1)
    .map(([cashier, mods]) => ({ cashier, modules: [...mods] }))
})

watch(() => storeFilter.selectedBranchId, () => load())

// Boshlanish tugashdan keyin qo'yilsa — ikkinchisi ergashadi (bo'sh oraliq bo'lmasin).
// Sana maydoni tozalab qo'yilganda (bo'sh qiymat) hech narsa o'zgartirilmaydi.
watch(dateFrom, (v) => { if (v && dateTo.value && v > dateTo.value) dateTo.value = v })
watch(dateTo, (v) => { if (v && dateFrom.value && v < dateFrom.value) dateFrom.value = v })

// Ikkala sana bir vaqtda o'zgarganda (preset, siljitish) bitta so'rov ketishi uchun
// yuklash keyingi tikka qoldiriladi.
let reloadTimer = null
watch([dateFrom, dateTo], () => {
  clearTimeout(reloadTimer)
  reloadTimer = setTimeout(load, 0)
})

onMounted(() => { load(); loadModuleCards() })

/** Davrni butunligicha oldinga/orqaga siljitadi (uzunligi saqlanadi). */
function shiftPeriod(step) {
  if (!dateFrom.value || !dateTo.value) return
  const days = step * dayCount.value
  const from = addDays(dateFrom.value, days)
  const to = addDays(dateTo.value, days)
  // oraliq vaqtincha teskari bo'lib qolmasligi uchun tartib bilan yoziladi
  if (days < 0) { dateFrom.value = from; dateTo.value = to }
  else { dateTo.value = to; dateFrom.value = from }
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
  <h2>PosCloud</h2>
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
    <div>
      <h1>{{ isRange ? 'Savdolar hisoboti' : 'Kunlik savdolar' }}</h1>
      <div class="period-note muted">{{ periodLabel }}</div>
    </div>
    <div class="range-bar">
      <button v-for="p in [['today','Bugun'],['yesterday','Kecha'],['week','7 kun'],['month','Bu oy']]"
              :key="p[0]" class="chip chip-sm" :class="{ 'chip-on': isPreset(p[0]) }"
              @click="setPreset(p[0])">
        {{ p[1] }}
      </button>
      <span class="range-sep"></span>
      <button class="btn btn-ghost btn-sm" @click="shiftPeriod(-1)" title="Oldingi davr">‹</button>
      <input type="date" class="input date-in" v-model="dateFrom" :max="dateTo" title="Boshlanish sanasi" />
      <span class="muted">—</span>
      <input type="date" class="input date-in" v-model="dateTo" :min="dateFrom" title="Tugash sanasi" />
      <button class="btn btn-ghost btn-sm" @click="shiftPeriod(1)" title="Keyingi davr">›</button>
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
    <div class="card stat" v-for="k in PAY_KEYS" :key="k">
      <div class="stat-label">{{ PAY_LABELS[k] }}</div>
      <div class="stat-value">{{ fmtNum(summary.pay[k].total) }}</div>
      <div class="stat-sub">
        <span class="qr-yes">QR {{ fmtNum(summary.pay[k].fiscal) }}</span> ·
        <span class="qr-no">QR yo'q {{ fmtNum(summary.pay[k].plain) }}</span>
      </div>
    </div>
    <div class="card stat">
      <div class="stat-label">Qaytarish</div>
      <div class="stat-value" style="color: var(--danger)">− {{ fmtNum(summary.refunds) }}</div>
    </div>
    <div class="card stat stat-net">
      <div class="stat-label">Sof tushum</div>
      <div class="stat-value">{{ fmtNum(summary.net) }}</div>
    </div>

    <!-- Fiskal (QR kodli) va oddiy cheklar — Sof tushum yonidagi bo'sh joyda -->
    <div class="card stat stat-wide stat-fiscal">
      <div class="stat-label">Fiskal chek (QR kodli)</div>
      <div class="stat-value">{{ fmtNum(summary.fiscalSum) }}</div>
      <div class="stat-sub">{{ summary.fiscalCount }} ta chek — QR shakllangan, soliqqa o'tgan</div>
    </div>
    <div class="card stat stat-wide stat-plain">
      <div class="stat-label">Oddiy chek (QR yo'q)</div>
      <div class="stat-value">{{ fmtNum(summary.plainSum) }}</div>
      <div class="stat-sub">
        {{ summary.plainCount }} ta chek — QR chiqmagan
        <template v-if="unconfirmedCount">
          (shundan {{ unconfirmedCount }} tasi "Fiskal" deb belgilangan, lekin QR/fiskal belgi yo'q)
        </template>
      </div>
    </div>
  </div>

  <!-- Kassalar kesimi: har bir to'lov turi QR kodli / QR yo'q bo'lib ajratilgan -->
  <div v-if="perTerminal.length" class="card term-card">
    <div class="term-head">
      Kassalar bo'yicha
      <span class="term-legend">
        har katakda: jami · <span class="qr-yes">QR kodli</span> / <span class="qr-no">QR yo'q</span>
      </span>
    </div>
    <div class="scroll-x">
      <table class="table term-table">
        <thead>
          <tr>
            <th>Kassa</th>
            <th class="text-right">Cheklar</th>
            <th class="text-right">Jami</th>
            <th class="text-right" v-for="k in PAY_KEYS" :key="k">{{ PAY_LABELS[k] }}</th>
            <th class="text-right">Fiskal (QR)</th>
            <th class="text-right">Oddiy chek</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in perTerminal" :key="t.terminal || 'none'"
              class="term-row" :class="{ 'term-on': terminalFilter === t.terminal }"
              @click="terminalFilter = terminalFilter === t.terminal ? 'all' : t.terminal">
            <td><strong>{{ terminalLabel(t.terminal) }}</strong></td>
            <td class="text-right">{{ t.count }}</td>
            <td class="text-right"><strong>{{ fmtNum(t.total) }}</strong></td>
            <td class="text-right" v-for="k in PAY_KEYS" :key="k">
              <div>{{ fmtNum(t.pay[k].total) }}</div>
              <div class="split">
                <span class="qr-yes">{{ fmtNum(t.pay[k].fiscal) }}</span> /
                <span class="qr-no">{{ fmtNum(t.pay[k].plain) }}</span>
              </div>
            </td>
            <td class="text-right" style="color: #15803d">
              {{ fmtNum(t.fiscalSum) }} <span class="muted">({{ t.fiscalCount }})</span>
            </td>
            <td class="text-right" style="color: #c2410c">
              {{ fmtNum(t.plainSum) }} <span class="muted">({{ t.plainCount }})</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- Fiskal modullar (fleshkalar) kesimi -->
  <div class="card term-card">
    <div class="term-head">
      Fiskal modullar (fleshkalar) bo'yicha — QR kodli cheklar
      <span class="term-legend">har bir fleshkadan qancha QR chek chiqqani, to'lov turlari bilan</span>
    </div>
    <div class="scroll-x">
      <table class="table term-table">
        <thead>
          <tr>
            <th>Fiskal modul</th>
            <th>RS raqami</th>
            <th>Kassalar</th>
            <th class="text-right">QR cheklar</th>
            <th class="text-right">Jami</th>
            <th class="text-right" v-for="k in PAY_KEYS" :key="k">{{ PAY_LABELS[k] }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in perFiscalModule" :key="m.module || 'none'">
            <td>
              <strong>{{ moduleOwner(m.module) || moduleLabel(m.module) }}</strong>
              <button class="name-btn" title="Ism-sharifni kiritish/o'zgartirish"
                      @click="renameModule(m.module)">✎</button>
              <div v-if="moduleOwner(m.module)" class="mod-note">{{ moduleLabel(m.module) }}</div>
            </td>
            <td class="muted">{{ m.rs || '—' }}</td>
            <td class="muted">{{ m.terminals.join(', ') }}</td>
            <td class="text-right">{{ m.fiscalCount }}</td>
            <td class="text-right"><strong>{{ fmtNum(m.total) }}</strong></td>
            <td class="text-right" v-for="k in PAY_KEYS" :key="k">
              {{ fmtNum(m.pay[k].total) }}
              <div v-if="m.pay[k].count" class="split muted">{{ m.pay[k].count }} ta</div>
            </td>
          </tr>
          <tr v-if="!perFiscalModule.length && !loading">
            <td colspan="10" class="muted" style="text-align: center; padding: 24px">
              {{ isRange ? "Tanlangan davrda QR kodli (fiskal) chek yo'q" : "Bu kunda QR kodli (fiskal) chek yo'q" }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- Fiskal modul (kassa apparati) × kassir kesimi — faqat QR kodli cheklar -->
  <div class="card term-card mod-card">
    <div class="term-head">
      Fiskal modul (kassa apparati) va kassirlar bo'yicha — QR kodli cheklar
      <span class="term-legend">
        qaysi fleshkadan qaysi kassir nomidan qancha chek o'tgani
      </span>
    </div>

    <div v-if="crossTerminalModules.length" class="alert alert-warn mix-warn">
      <strong>Diqqat:</strong> quyidagi fiskal modul bir nechta kassaning cheklarini fiskalizatsiya qilgan —
      <span v-for="(m, i) in crossTerminalModules" :key="m.title">
        <b>{{ m.title }}</b> ({{ m.terminals.join(', ') }}){{ i < crossTerminalModules.length - 1 ? '; ' : '' }}
      </span>.
      Ya'ni modulsiz kassaning cheklari boshqa kompyuterdagi fleshkadan o'tib ketgan —
      har bir agentda <code>Agent:FiscalTerminalId</code> ni o'sha kassa ID siga to'ldiring.
    </div>

    <div v-if="mixedCashiers.length" class="alert alert-warn mix-warn">
      <strong>Diqqat:</strong> quyidagi kassirlar {{ isRange ? 'tanlangan davrda' : 'bir kunda' }} bir nechta fiskal modulda ishlagan —
      <span v-for="(m, i) in mixedCashiers" :key="m.cashier">
        <b>{{ m.cashier }}</b> ({{ m.modules.join(', ') }}){{ i < mixedCashiers.length - 1 ? '; ' : '' }}
      </span>
    </div>

    <div class="scroll-x">
      <table class="table term-table">
        <thead>
          <tr>
            <th>Kassa</th>
            <th>Kassir</th>
            <th class="text-right">QR cheklar</th>
            <th class="text-right">Jami</th>
            <th class="text-right" v-for="k in PAY_KEYS" :key="k">{{ PAY_LABELS[k] }}</th>
          </tr>
        </thead>
        <tbody v-for="g in moduleGroups" :key="g.module || 'none'" class="mod-group">
          <tr class="mod-group-head">
            <td colspan="2">
              <strong>{{ moduleOwner(g.module) || moduleLabel(g.module) }}</strong>
              <button class="name-btn" title="Ism-sharifni kiritish/o'zgartirish"
                      @click="renameModule(g.module)">✎</button>
              <span class="muted mod-meta">
                modul: {{ moduleLabel(g.module) }}<template v-if="g.rs"> · RS: {{ g.rs }}</template>
              </span>
            </td>
            <td class="text-right"><strong>{{ g.totals?.fiscalCount || 0 }}</strong></td>
            <td class="text-right"><strong>{{ fmtNum(g.totals?.total || 0) }}</strong></td>
            <td class="text-right" v-for="k in PAY_KEYS" :key="k">
              <strong>{{ fmtNum(g.totals?.pay[k].total || 0) }}</strong>
            </td>
          </tr>
          <tr v-for="r in g.rows" :key="`${r.module}|${r.cashier}|${r.terminal}`">
            <td class="muted">{{ terminalLabel(r.terminal) }}</td>
            <td>{{ r.cashier }}</td>
            <td class="text-right">{{ r.fiscalCount }}</td>
            <td class="text-right">{{ fmtNum(r.total) }}</td>
            <td class="text-right" v-for="k in PAY_KEYS" :key="k">{{ fmtNum(r.pay[k].total) }}</td>
          </tr>
        </tbody>
        <tbody v-if="!moduleGroups.length && !loading">
          <tr>
            <td colspan="9" class="muted" style="text-align: center; padding: 24px">
              {{ isRange ? "Tanlangan davrda QR kodli (fiskal) chek yo'q" : "Bu kunda QR kodli (fiskal) chek yo'q" }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <div class="filters">
    <button v-for="ff in [['all','Hammasi'],['fiscal','Faqat fiskal (QR)'],['plain','Faqat oddiy chek']]"
            :key="ff[0]" class="chip" :class="{ 'chip-on': fiscalFilter === ff[0] }"
            @click="fiscalFilter = ff[0]">
      {{ ff[1] }}
    </button>
    <select v-if="terminals.length > 1" class="input sel" v-model="terminalFilter">
      <option value="all">Kassa: hammasi</option>
      <option v-for="t in terminals" :key="t || 'none'" :value="t">{{ terminalLabel(t) }}</option>
    </select>
    <span class="muted count">{{ shownSales.length }} ta ko'rsatilmoqda</span>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Chek</th>
          <th>Tur</th>
          <th>Kassa</th>
          <th>Kassir</th>
          <th>Vaqt</th>
          <th>To'lov</th>
          <th class="text-right">Jami</th>
          <th>Fiskal</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in shownSales" :key="s.id">
          <td><strong>{{ s.receiptNumber }}</strong></td>
          <td>
            <span v-if="s.type === 'Refund'" class="pill pill-red">Qaytarish</span>
            <span v-else-if="s.status === 'Refunded'" class="pill pill-amber">Qaytarilgan</span>
            <span v-else class="pill pill-gray">Sotuv</span>
          </td>
          <td class="muted">{{ s.terminalId || '—' }}</td>
          <td class="muted">{{ s.cashierFullName || s.cashierUsername || '—' }}</td>
          <td class="muted">{{ fmtWhen(s.createdAt) }}</td>
          <td>
            {{ paymentLabel[s.paymentMethod] || s.paymentMethod }}
            <span v-if="s.transferSource" class="muted src">({{ s.transferSource }})</span>
          </td>
          <td class="text-right" :style="{ color: s.type === 'Refund' ? 'var(--danger)' : 'inherit' }">
            {{ s.type === 'Refund' ? '− ' : '' }}{{ fmtNum(s.totalAmount) }}
          </td>
          <td>
            <span class="pill" :class="fiscalPill[s.receipt?.fiscalStatus] || 'pill-gray'">
              {{ fiscalLabel[s.receipt?.fiscalStatus] || '—' }}
            </span>
            <div class="qr-note" :class="isFiscal(s) ? 'qr-yes' : 'qr-no'">
              <template v-if="isFiscal(s)">QR kodli chek</template>
              <template v-else-if="isUnconfirmedFiscal(s)">QR shakllanmagan — soliqqa o'tgani tasdiqlanmagan</template>
              <template v-else>QR yo'q — oddiy chek</template>
            </div>
            <div v-if="isFiscal(s) && s.receipt?.fiscalModuleId" class="mod-note">
              Modul: {{ s.receipt.fiscalModuleId }}
            </div>
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
        <tr v-if="!shownSales.length && !loading">
          <td colspan="9" class="muted" style="text-align: center; padding: 24px">
            {{ sales.length
              ? "Tanlangan filtr bo'yicha chek yo'q"
              : (isRange ? "Tanlangan davrda savdo yo'q" : "Bu kunda savdo yo'q") }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>

</template>

<style scoped>
.period-note {
  font-size: 12.5px;
  margin-top: 2px;
}
.range-bar {
  display: flex;
  gap: 6px;
  align-items: center;
  flex-wrap: wrap;
}
.range-sep {
  width: 1px;
  height: 22px;
  background: var(--border);
  margin: 0 4px;
}
.date-in {
  width: 150px;
}
.chip-sm {
  padding: 5px 12px;
  font-size: 12.5px;
}
.summary-grid {
  display: grid;
  /* 1-qator: Savdolar + 5 to'lov turi + Qaytarish = 7 ta */
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}
/* Sof tushum yonidagi bo'sh joyni to'ldiruvchi keng kartalar */
.stat-wide {
  grid-column: span 3;
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
.stat-sub {
  font-size: 12px;
  color: var(--muted, #6b7280);
  margin-top: 4px;
}
.stat-fiscal {
  background: #eff6ff;
  border-left: 3px solid #2563eb;
}
.stat-plain {
  background: #fff7ed;
  border-left: 3px solid #f97316;
}

.filters {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
  flex-wrap: wrap;
  align-items: center;
}
.chip {
  padding: 6px 14px;
  border-radius: 20px;
  border: 1px solid var(--border);
  background: #f8fafc;
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
  color: var(--muted);
}
.chip-on {
  background: #2563eb;
  color: #fff;
  border-color: #2563eb;
}
.count {
  font-size: 12.5px;
  margin-left: auto;
}
.sel {
  width: auto;
  padding: 6px 10px;
  font-size: 13px;
}
.term-card {
  margin-bottom: 14px;
  padding: 4px 0 0;
}
.term-head {
  font-size: 13px;
  font-weight: 700;
  color: var(--muted, #6b7280);
  padding: 10px 16px 2px;
}
.term-row {
  cursor: pointer;
}
.term-row:hover {
  background: #f8fafc;
}
.term-on {
  background: #eff6ff;
}
.src {
  font-size: 11.5px;
}
.term-legend {
  font-weight: 400;
  font-size: 11.5px;
  margin-left: 8px;
}
.split {
  font-size: 11.5px;
  margin-top: 2px;
}
.scroll-x {
  overflow-x: auto;
}
.mod-card {
  margin-top: 16px;
}
.mod-note {
  font-size: 11px;
  color: var(--muted, #6b7280);
  margin-top: 2px;
}
.name-btn {
  border: none;
  background: transparent;
  cursor: pointer;
  font-size: 12px;
  color: #2563eb;
  padding: 0 4px;
}
.mod-group + .mod-group .mod-group-head td {
  border-top: 2px solid var(--border);
}
.mod-group-head td {
  background: #f1f5f9;
}
.mod-meta {
  font-size: 11.5px;
  margin-left: 10px;
}
.mix-warn {
  margin: 8px 16px 4px;
  font-size: 13px;
  background: #fffbeb;
  border: 1px solid #fde68a;
  color: #92400e;
  border-radius: 8px;
  padding: 8px 12px;
}
.term-table th,
.term-table td {
  white-space: nowrap;
}
.qr-note {
  font-size: 11.5px;
  margin-top: 3px;
}
.qr-yes {
  color: #15803d;
}
.qr-no {
  color: #c2410c;
}
@media (max-width: 1280px) {
  .summary-grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
  .stat-wide {
    grid-column: span 2;
  }
}
@media (max-width: 980px) {
  .summary-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
  .stat-wide {
    grid-column: span 3;
  }
}
@media (max-width: 640px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .stat-wide {
    grid-column: span 2;
  }
}
</style>
