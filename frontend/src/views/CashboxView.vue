<script setup>
import { localDateStr } from '../utils/date'
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storesApi, shiftsApi, salesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const route = useRoute()
const router = useRouter()

const cashboxId = route.params.id

const store = ref(null)      
const cashbox = ref(null)
const shifts = ref([])
const sales = ref([])
const loading = ref(true)
const err = ref('')

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return d ? new Date(d).toLocaleString('uz-UZ') : '—' }
function today() { return localDateStr() }

const todaySales = computed(() => {
  const t = today()
  return sales.value.filter(
    (s) => s.type !== 'Refund' && s.createdAt && localDateStr(new Date(s.createdAt)) === t
  )
})

const todayStats = computed(() => {
  const list = todaySales.value
  return {
    count: list.length,
    total: list.reduce((s, x) => s + Number(x.totalAmount || 0), 0),
    // Aralash to'lov: karta qismi — cardAmount, naqd — qolgani.
    cash: list.reduce((s, x) => s + (x.paymentMethod === 'Cash' ? Number(x.totalAmount || 0)
      : x.paymentMethod === 'Mixed' ? Number(x.totalAmount || 0) - Number(x.cardAmount || 0) : 0), 0),
    card: list.reduce((s, x) => s + (x.paymentMethod === 'Card' ? Number(x.totalAmount || 0)
      : x.paymentMethod === 'Mixed' ? Number(x.cardAmount || 0) : 0), 0),
    transfer: list.filter((x) => x.paymentMethod === 'Transfer').reduce((s, x) => s + Number(x.totalAmount || 0), 0),
    debt: list.filter((x) => x.paymentMethod === 'Debt').reduce((s, x) => s + Number(x.totalAmount || 0), 0)
  }
})

const branchShifts = computed(() =>
  shifts.value
    .filter((s) => cashbox.value && s.branchId === store.value?.id)
    .slice(0, 20)
)

const openShift = computed(() => branchShifts.value.find((s) => s.status === 'Open'))

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [stores, allShifts, allSales] = await Promise.all([
      storesApi.list(),
      shiftsApi.list(),
      salesApi.list({})
    ])
    for (const s of stores) {
      const cb = s.cashboxes.find((c) => c.id === cashboxId)
      if (cb) {
        store.value = s
        cashbox.value = cb
        break
      }
    }
    shifts.value = allShifts
    sales.value = allSales.filter((s) => store.value && s.branchId === store.value.id)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div v-if="loading" style="padding:40px; text-align:center; color:var(--muted)">Yuklanmoqda...</div>
  <div v-else-if="!cashbox" style="padding:40px; text-align:center; color:var(--muted)">
    Kassa topilmadi.
    <button class="btn btn-ghost btn-sm" style="margin-left:12px" @click="router.push({ name: 'stores' })">← Orqaga</button>
  </div>
  <template v-else>
    <div class="page-head">
      <div>
        <button class="btn btn-ghost btn-sm" style="margin-bottom:6px" @click="router.push({ name: 'stores' })">← Do'konlar</button>
        <h1 style="margin:0">{{ cashbox.name }}</h1>
        <div class="muted" style="font-size:13px; margin-top:2px">{{ store.name }} · {{ store.address || '' }}</div>
      </div>
      <div style="display:flex; gap:10px; align-items:center">
        <span class="pill" :class="cashbox.isActive ? 'pill-green' : 'pill-gray'">
          {{ cashbox.isActive ? 'Aktiv' : 'O\'chiq' }}
        </span>
        <span v-if="openShift" class="pill pill-amber">Smena ochiq</span>
      </div>
    </div>

    <div v-if="err" class="alert alert-error">{{ err }}</div>

    <!-- KPI cards -->
    <div class="kpi-row" style="margin-bottom:16px">
      <div class="card kpi-card">
        <div class="kpi-val" style="color:var(--primary)">{{ fmt(cashbox.balance) }}</div>
        <div class="kpi-lbl">Balans (so'm)</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-val">{{ todayStats.count }}</div>
        <div class="kpi-lbl">Bugun sotuvlar</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-val" style="color:var(--success)">{{ fmt(todayStats.total) }}</div>
        <div class="kpi-lbl">Bugun tushum</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-val">{{ fmt(todayStats.cash) }}</div>
        <div class="kpi-lbl">Naqd</div>
      </div>
      <div class="card kpi-card">
        <div class="kpi-val">{{ fmt(todayStats.card) }}</div>
        <div class="kpi-lbl">Karta</div>
      </div>
      <div class="card kpi-card" v-if="todayStats.debt">
        <div class="kpi-val" style="color:var(--danger)">{{ fmt(todayStats.debt) }}</div>
        <div class="kpi-lbl">Nasiya</div>
      </div>
    </div>

    <!-- Joriy smena -->
    <div v-if="openShift" class="card" style="margin-bottom:16px">
      <h3 style="margin-top:0; color:var(--amber)">Ochiq smena</h3>
      <div style="display:grid; grid-template-columns:repeat(auto-fill,minmax(180px,1fr)); gap:12px">
        <div><div class="muted" style="font-size:12px">Kassir</div><strong>{{ openShift.cashierName || '—' }}</strong></div>
        <div><div class="muted" style="font-size:12px">Ochilgan</div><strong>{{ fmtDate(openShift.openedAt) }}</strong></div>
        <div><div class="muted" style="font-size:12px">Sotuv</div><strong>{{ fmt(openShift.totalSales) }} so'm</strong></div>
        <div><div class="muted" style="font-size:12px">Boshlangich naqd</div><strong>{{ fmt(openShift.openingCash) }} so'm</strong></div>
      </div>
    </div>

    <!-- Smena tarixi -->
    <div class="card">
      <h3 style="margin-top:0">Smena tarixi (oxirgi 20)</h3>
      <table class="table">
        <thead>
          <tr>
            <th>Kassir</th>
            <th>Ochilgan</th>
            <th>Yopilgan</th>
            <th>Holat</th>
            <th class="text-right">Sotuv</th>
            <th class="text-right">Naqd</th>
            <th class="text-right">Karta</th>
            <th class="text-right">Farq</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in branchShifts" :key="s.id">
            <td>{{ s.cashierName || '—' }}</td>
            <td class="muted" style="font-size:13px">{{ fmtDate(s.openedAt) }}</td>
            <td class="muted" style="font-size:13px">{{ fmtDate(s.closedAt) }}</td>
            <td>
              <span class="pill" :class="s.status === 'Open' ? 'pill-amber' : 'pill-gray'">
                {{ s.status === 'Open' ? 'Ochiq' : 'Yopilgan' }}
              </span>
            </td>
            <td class="text-right">{{ fmt(s.totalSales) }}</td>
            <td class="text-right muted">{{ fmt(s.cashSalesNet) }}</td>
            <td class="text-right muted">{{ fmt(s.cardSalesNet) }}</td>
            <td class="text-right">
              <span v-if="s.status === 'Closed'" :style="{ color: s.cashDifference < 0 ? 'var(--danger)' : 'var(--success)' }">
                {{ s.cashDifference >= 0 ? '+' : '' }}{{ fmt(s.cashDifference) }}
              </span>
              <span v-else class="muted">—</span>
            </td>
          </tr>
          <tr v-if="!branchShifts.length">
            <td colspan="8" class="muted" style="text-align:center; padding:24px">Smena tarixi yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Bugungi sotuvlar -->
    <div class="card" style="margin-top:16px">
      <h3 style="margin-top:0">Bugungi sotuvlar ({{ todaySales.length }} ta)</h3>
      <table class="table">
        <thead>
          <tr>
            <th>Vaqt</th>
            <th>To'lov</th>
            <th class="text-right">Summa</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in todaySales.slice(0, 50)" :key="s.id">
            <td class="muted" style="font-size:13px">{{ fmtDate(s.createdAt) }}</td>
            <td>
              <span class="pill"
                :class="{
                  'pill-green': s.paymentMethod === 'Cash',
                  'pill-blue': s.paymentMethod === 'Card',
                  'pill-amber': s.paymentMethod === 'Transfer',
                  'pill-gray': s.paymentMethod === 'Debt'
                }">
                {{ s.paymentMethod === 'Cash' ? 'Naqd' : s.paymentMethod === 'Card' ? 'Karta' : s.paymentMethod === 'Transfer' ? 'O\'tkazma' : 'Nasiya' }}
              </span>
            </td>
            <td class="text-right" style="font-weight:500">{{ fmt(s.totalAmount) }}</td>
          </tr>
          <tr v-if="!todaySales.length">
            <td colspan="3" class="muted" style="text-align:center; padding:24px">Bugun savdo yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>
  </template>
</template>

<style scoped>
.kpi-row {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 12px;
}
.kpi-card { padding: 18px; }
.kpi-val { font-size: 22px; font-weight: 800; line-height: 1.1; }
.kpi-lbl { color: var(--muted); margin-top: 6px; font-size: 13px; font-weight: 600; }
</style>
