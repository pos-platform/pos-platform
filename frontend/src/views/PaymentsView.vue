<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { paymentsApi, suppliersApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const payments = ref([])
const suppliers = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const showForm = ref(false)
const form = reactive({ supplierId: '', amount: 0, comment: '' })

const total = computed(() => payments.value.reduce((s, p) => s + Number(p.amount), 0))
const selectedSupplier = computed(() => suppliers.value.find((s) => s.id === form.supplierId))

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [p, s] = await Promise.all([paymentsApi.list(), suppliersApi.list()])
    payments.value = p
    suppliers.value = s
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function create() {
  err.value = ''
  success.value = ''
  if (!form.supplierId) { err.value = 'Yetkazib beruvchini tanlang'; return }
  if (!Number(form.amount)) { err.value = 'Summani kiriting'; return }
  try {
    await paymentsApi.create({ supplierId: form.supplierId, amount: Number(form.amount), comment: form.comment.trim() || null })
    success.value = 'To\'lov saqlandi'
    Object.assign(form, { supplierId: '', amount: 0, comment: '' })
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>To'lovlar (Платежи)</h1>
    <div style="display:flex; gap:12px; align-items:center">
      <span class="muted">Jami to'langan: <strong>{{ fmt(total) }}</strong></span>
      <button class="btn" @click="showForm = !showForm">+ To'lov</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="create">
    <h3 style="margin-top:0">Yetkazib beruvchiga to'lov</h3>
    <div class="row">
      <div class="field">
        <label>Yetkazib beruvchi</label>
        <select v-model="form.supplierId" class="select" required>
          <option value="" disabled>Tanlang...</option>
          <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
        <span v-if="selectedSupplier" class="muted" style="font-size:12px">
          Joriy qarz: {{ fmt(selectedSupplier.balance) }} so'm
        </span>
      </div>
      <div class="field">
        <label>Summa</label>
        <input v-model="form.amount" type="number" min="0" class="input" required />
      </div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>
    <button class="btn">Saqlash</button>
  </form>

  <div class="card">
    <table class="table">
      <thead><tr><th>Yetkazib beruvchi</th><th>Sana</th><th>Izoh</th><th class="text-right">Summa</th></tr></thead>
      <tbody>
        <tr v-for="p in payments" :key="p.id">
          <td><strong>{{ p.supplierName }}</strong></td>
          <td class="muted">{{ fmtDate(p.createdAt) }}</td>
          <td class="muted">{{ p.comment || '—' }}</td>
          <td class="text-right" style="color:var(--success)">{{ fmt(p.amount) }}</td>
        </tr>
        <tr v-if="!payments.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">To'lov yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>
