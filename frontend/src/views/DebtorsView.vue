<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { debtorsApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const debtors = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const showForm = ref(false)
const form = reactive({ name: '', phone: '', comment: '', initialDebt: 0 })

const selected = ref(null) // debtor detail with transactions
const txn = reactive({ type: 'Payment', amount: 0, comment: '' })
const busy = ref(false)

const search = ref('')

const totalDebt = computed(() => debtors.value.reduce((s, d) => s + Number(d.balance), 0))
const debtorsWithDebt = computed(() => debtors.value.filter(d => Number(d.balance) > 0).length)

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return debtors.value
  return debtors.value.filter(d =>
    d.name.toLowerCase().includes(q) || (d.phone || '').toLowerCase().includes(q))
})

// Yangi balansni oldindan ko'rsatish (amal tasdiqdan oldin)
const previewBalance = computed(() => {
  if (!selected.value) return 0
  const amt = Number(txn.amount) || 0
  return Number(selected.value.balance) + (txn.type === 'Debt' ? amt : -amt)
})

function fmt(n) { return Number(n || 0).toLocaleString('uz-UZ') }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    debtors.value = await debtorsApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function create() {
  err.value = ''
  success.value = ''
  try {
    await debtorsApi.create({
      name: form.name.trim(),
      phone: form.phone.trim() || null,
      comment: form.comment.trim() || null,
      initialDebt: Number(form.initialDebt) || 0
    })
    success.value = `"${form.name}" qo'shildi`
    Object.assign(form, { name: '', phone: '', comment: '', initialDebt: 0 })
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function openDetail(d) {
  err.value = ''
  Object.assign(txn, { type: 'Payment', amount: 0, comment: '' })
  try {
    selected.value = await debtorsApi.get(d.id)
  } catch (e) {
    err.value = errorMessage(e)
  }
}

function setQuick(v) { txn.amount = v }

async function addTxn() {
  if (!Number(txn.amount) || Number(txn.amount) <= 0) return
  busy.value = true
  err.value = ''
  success.value = ''
  try {
    await debtorsApi.addTransaction(selected.value.id, {
      type: txn.type,
      amount: Number(txn.amount),
      comment: txn.comment.trim() || null
    })
    success.value = txn.type === 'Debt' ? 'Qarz qo\'shildi' : 'To\'lov qabul qilindi'
    txn.amount = 0
    txn.comment = ''
    selected.value = await debtorsApi.get(selected.value.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

const txnLabel = { Debt: 'Qarz', Payment: "To'lov" }

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Qarzdorlar</h1>
    <button class="btn" @click="showForm = !showForm">+ Yangi qarzdor</button>
  </div>

  <!-- Umumiy ko'rsatkichlar -->
  <div class="stat-row">
    <div class="stat-card">
      <div class="stat-label">Jami qarz</div>
      <div class="stat-value danger">{{ fmt(totalDebt) }} <span class="cur">so'm</span></div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Qarzdorlar soni</div>
      <div class="stat-value">{{ debtors.length }}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Qarzi borlar</div>
      <div class="stat-value warn">{{ debtorsWithDebt }}</div>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="create">
    <h3 style="margin-top:0">Yangi qarzdor</h3>
    <div class="row">
      <div class="field"><label>Ism *</label><input v-model="form.name" class="input" required placeholder="Mijoz ismi" /></div>
      <div class="field"><label>Telefon</label><input v-model="form.phone" class="input" placeholder="+998 90 123 45 67" /></div>
      <div class="field"><label>Boshlang'ich qarz</label><input v-model="form.initialDebt" type="number" min="0" class="input" /></div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" placeholder="Ixtiyoriy" /></div>
    <div style="display:flex; gap:8px">
      <button class="btn">Saqlash</button>
      <button type="button" class="btn btn-ghost" @click="showForm = false">Bekor qilish</button>
    </div>
  </form>

  <div class="layout">
    <!-- Ro'yxat -->
    <div class="card list-card">
      <div class="list-search">
        <input v-model="search" class="input" placeholder="Ism yoki telefon bo'yicha qidirish" />
      </div>
      <table class="table">
        <thead><tr><th>Mijoz</th><th>Telefon</th><th class="text-right">Qarzi</th><th></th></tr></thead>
        <tbody>
          <tr v-for="d in filtered" :key="d.id"
              :class="{ 'row-active': selected && selected.id === d.id }"
              style="cursor:pointer" @click="openDetail(d)">
            <td><strong>{{ d.name }}</strong></td>
            <td class="muted">{{ d.phone || '—' }}</td>
            <td class="text-right">
              <span v-if="Number(d.balance) > 0" class="bal-tag danger">{{ fmt(d.balance) }}</span>
              <span v-else class="bal-tag ok">to'langan</span>
            </td>
            <td class="text-right"><button class="btn btn-ghost btn-sm" @click.stop="openDetail(d)">Batafsil →</button></td>
          </tr>
          <tr v-if="!filtered.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Qarzdor topilmadi</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Tafsilot -->
    <div v-if="selected" class="card detail-card">
      <div class="detail-head">
        <div>
          <h3 style="margin:0">{{ selected.name }}</h3>
          <p class="muted" style="margin:2px 0 0">{{ selected.phone || 'telefon kiritilmagan' }}</p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <!-- Joriy qarz -->
      <div class="balance-box" :class="Number(selected.balance) > 0 ? 'is-debt' : 'is-ok'">
        <div class="balance-label">{{ Number(selected.balance) > 0 ? 'Joriy qarz' : 'Qarzi yo\'q' }}</div>
        <div class="balance-amount">{{ fmt(selected.balance) }} <span class="cur">so'm</span></div>
      </div>

      <!-- Amal tanlash -->
      <div class="seg">
        <button type="button" class="seg-btn" :class="{ 'seg-pay': txn.type === 'Payment' }"
                @click="txn.type = 'Payment'"><AppIcon name="minus" :size="14" style="display:inline-block; vertical-align:-2px" /> To'lov qabul qilish</button>
        <button type="button" class="seg-btn" :class="{ 'seg-debt': txn.type === 'Debt' }"
                @click="txn.type = 'Debt'"><AppIcon name="plus" :size="14" style="display:inline-block; vertical-align:-2px" /> Qarz qo'shish</button>
      </div>

      <div class="field" style="margin-top:12px">
        <label>Summa</label>
        <input v-model="txn.amount" type="number" min="0" class="input input-lg" placeholder="0" />
      </div>
      <div class="quick">
        <button type="button" class="chip" @click="setQuick(10000)">10 000</button>
        <button type="button" class="chip" @click="setQuick(50000)">50 000</button>
        <button type="button" class="chip" @click="setQuick(100000)">100 000</button>
        <button type="button" class="chip" v-if="Number(selected.balance) > 0" @click="setQuick(selected.balance)">To'liq qarz</button>
      </div>

      <div class="field" style="margin-top:10px"><label>Izoh</label><input v-model="txn.comment" class="input" placeholder="Ixtiyoriy" /></div>

      <!-- Yangi balans preview -->
      <div v-if="Number(txn.amount) > 0" class="preview">
        Amaldan keyin qarz:
        <strong :style="{ color: previewBalance > 0 ? 'var(--danger)' : 'var(--success)' }">{{ fmt(previewBalance) }} so'm</strong>
      </div>

      <button class="btn btn-lg" :class="txn.type === 'Debt' ? 'btn-danger' : 'btn-success'"
              style="width:100%; margin-top:12px" :disabled="busy || !Number(txn.amount)" @click="addTxn">
        {{ busy ? '...' : (txn.type === 'Debt' ? 'Qarz qo\'shish' : "To'lovni saqlash") }}
      </button>

      <hr style="border:none;border-top:1px solid var(--border);margin:16px 0" />
      <div class="muted" style="font-weight:600;font-size:13px;margin-bottom:8px">Harakatlar tarixi</div>
      <div v-for="t in selected.transactions" :key="t.id" class="hist">
        <div class="hist-left">
          <span class="pill" :class="t.type === 'Debt' ? 'pill-red' : 'pill-green'">{{ txnLabel[t.type] }}</span>
          <div class="hist-meta">
            <div>{{ fmtDate(t.createdAt) }}</div>
            <div v-if="t.comment" class="muted" style="font-size:12px">{{ t.comment }}</div>
          </div>
        </div>
        <span class="hist-amt" :class="t.type === 'Debt' ? 'danger' : 'ok'">
          {{ t.type === 'Debt' ? '+' : '−' }}{{ fmt(t.amount) }}
        </span>
      </div>
      <p v-if="!selected.transactions.length" class="muted" style="text-align:center;padding:12px 0">Hali harakat yo'q</p>
    </div>

    <!-- Tanlanmagan holat -->
    <div v-else class="card detail-card empty-detail">
      <p class="muted" style="margin:8px 0 0; text-align:center">Tafsilotlarni ko'rish va<br />to'lov qabul qilish uchun<br />ro'yxatdan qarzdorni tanlang</p>
    </div>
  </div>
</template>

<style scoped>
.stat-row { display: flex; gap: 14px; margin-bottom: 16px; flex-wrap: wrap; }
.stat-card { flex: 1; min-width: 180px; background: var(--card, #fff); border: 1px solid var(--border); border-radius: 12px; padding: 14px 18px; }
.stat-label { font-size: 13px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 26px; font-weight: 800; margin-top: 4px; }
.stat-value.danger { color: var(--danger); }
.stat-value.warn { color: #d97706; }
.stat-value .cur { font-size: 15px; font-weight: 600; color: var(--muted); }

.layout { display: flex; gap: 16px; align-items: flex-start; }
.list-card { flex: 1.4; min-width: 380px; }
.detail-card { flex: 1; min-width: 340px; max-width: 420px; }

.list-search { margin-bottom: 12px; }
.row-active { background: #eff6ff; }
.bal-tag { font-weight: 700; padding: 3px 10px; border-radius: 20px; font-size: 13px; }
.bal-tag.danger { background: #fee2e2; color: var(--danger); }
.bal-tag.ok { background: #dcfce7; color: #15803d; }

.detail-head { display: flex; justify-content: space-between; align-items: flex-start; }

.balance-box { border-radius: 12px; padding: 16px; margin: 14px 0; text-align: center; }
.balance-box.is-debt { background: #fef2f2; border: 1px solid #fecaca; }
.balance-box.is-ok { background: #f0fdf4; border: 1px solid #bbf7d0; }
.balance-label { font-size: 13px; font-weight: 600; color: var(--muted); }
.balance-amount { font-size: 30px; font-weight: 800; margin-top: 4px; }
.balance-box.is-debt .balance-amount { color: var(--danger); }
.balance-box.is-ok .balance-amount { color: #15803d; }
.balance-amount .cur { font-size: 16px; font-weight: 600; color: var(--muted); }

.seg { display: flex; gap: 8px; }
.seg-btn { flex: 1; padding: 11px 8px; border-radius: 10px; border: 1.5px solid var(--border); background: #fff; cursor: pointer; font-weight: 700; font-size: 13px; color: var(--muted); transition: .12s; }
.seg-btn.seg-pay { border-color: #16a34a; background: #16a34a; color: #fff; }
.seg-btn.seg-debt { border-color: var(--danger); background: var(--danger); color: #fff; }

.input-lg { font-size: 22px; font-weight: 700; padding: 10px 14px; text-align: right; }

.quick { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
.chip { padding: 6px 12px; border-radius: 20px; border: 1px solid var(--border); background: #f8fafc; cursor: pointer; font-size: 13px; font-weight: 600; }
.chip:hover { background: #eef2f7; }

.preview { margin-top: 12px; padding: 10px 12px; background: #f8fafc; border-radius: 8px; font-size: 14px; text-align: center; }

.btn-lg { padding: 14px; font-size: 16px; font-weight: 700; }
.btn-success { background: #16a34a; }

.hist { display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid var(--border); }
.hist:last-child { border-bottom: none; }
.hist-left { display: flex; align-items: center; gap: 10px; }
.hist-meta { font-size: 13px; }
.hist-amt { font-weight: 700; font-size: 15px; }
.hist-amt.danger { color: var(--danger); }
.hist-amt.ok { color: #15803d; }

.empty-detail { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 260px; }
</style>
