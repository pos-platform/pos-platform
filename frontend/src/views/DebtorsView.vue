<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { debtorsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const debtors = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const showForm = ref(false)
const form = reactive({ name: '', phone: '', comment: '', initialDebt: 0 })

const selected = ref(null) // debtor detail with transactions
const txn = reactive({ type: 'Debt', amount: 0, comment: '' })
const busy = ref(false)

const totalDebt = computed(() => debtors.value.reduce((s, d) => s + Number(d.balance), 0))

function fmt(n) { return Number(n || 0).toLocaleString() }
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
  try {
    selected.value = await debtorsApi.get(d.id)
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function addTxn() {
  if (!Number(txn.amount)) return
  busy.value = true
  err.value = ''
  try {
    await debtorsApi.addTransaction(selected.value.id, {
      type: txn.type,
      amount: Number(txn.amount),
      comment: txn.comment.trim() || null
    })
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
    <div style="display:flex; gap:12px; align-items:center">
      <span class="muted">Jami qarz: <strong>{{ fmt(totalDebt) }}</strong></span>
      <button class="btn" @click="showForm = !showForm">+ Qarzdor</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="create">
    <h3 style="margin-top:0">Yangi qarzdor</h3>
    <div class="row">
      <div class="field"><label>Ism</label><input v-model="form.name" class="input" required /></div>
      <div class="field"><label>Telefon</label><input v-model="form.phone" class="input" /></div>
      <div class="field"><label>Boshlang'ich qarz</label><input v-model="form.initialDebt" type="number" min="0" class="input" /></div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>
    <button class="btn">Saqlash</button>
  </form>

  <div class="row" style="align-items: flex-start">
    <!-- Ro'yxat -->
    <div class="card" style="flex:2; min-width:360px">
      <table class="table">
        <thead><tr><th>Ism</th><th>Telefon</th><th class="text-right">Qarz</th><th></th></tr></thead>
        <tbody>
          <tr v-for="d in debtors" :key="d.id">
            <td><strong>{{ d.name }}</strong></td>
            <td class="muted">{{ d.phone || '—' }}</td>
            <td class="text-right" :style="{ color: d.balance > 0 ? 'var(--danger)' : 'var(--success)' }">{{ fmt(d.balance) }}</td>
            <td class="text-right"><button class="btn btn-ghost btn-sm" @click="openDetail(d)">Batafsil</button></td>
          </tr>
          <tr v-if="!debtors.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Qarzdor yo'q</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Tafsilot -->
    <div v-if="selected" class="card" style="flex:1; min-width:300px">
      <h3 style="margin-top:0">{{ selected.name }}</h3>
      <p class="muted">{{ selected.phone }}</p>
      <div class="t-row grand"><span>Qarz</span><span :style="{ color: selected.balance > 0 ? 'var(--danger)' : 'var(--success)' }">{{ fmt(selected.balance) }} so'm</span></div>

      <div class="field" style="margin-top:12px">
        <label>Amal</label>
        <select v-model="txn.type" class="select">
          <option value="Debt">Qarz qo'shish</option>
          <option value="Payment">To'lov qabul qilish</option>
        </select>
      </div>
      <div class="row">
        <div class="field" style="flex:1"><label>Summa</label><input v-model="txn.amount" type="number" min="0" class="input" /></div>
      </div>
      <div class="field"><label>Izoh</label><input v-model="txn.comment" class="input" /></div>
      <button class="btn" style="width:100%" :disabled="busy" @click="addTxn">{{ busy ? '...' : 'Saqlash' }}</button>

      <hr style="border:none;border-top:1px solid var(--border);margin:14px 0" />
      <div class="muted" style="font-weight:600;font-size:13px;margin-bottom:6px">Tarix</div>
      <div v-for="t in selected.transactions" :key="t.id" class="cart-line" style="font-size:13px">
        <span class="cl-name">
          <span class="pill" :class="t.type === 'Debt' ? 'pill-red' : 'pill-green'">{{ txnLabel[t.type] }}</span>
          <span class="muted" style="margin-left:6px">{{ fmtDate(t.createdAt) }}</span>
        </span>
        <span>{{ fmt(t.amount) }}</span>
      </div>
      <p v-if="!selected.transactions.length" class="muted">Harakat yo'q</p>
    </div>
  </div>
</template>
