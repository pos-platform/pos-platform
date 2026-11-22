<script setup>
import { ref, computed, onMounted } from 'vue'
import { cashMovementsApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const movements = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const showForm = ref(false)
const form = ref({ type: 'Income', amount: '', comment: '' })

const typeLabel = (t) => ({ Income: 'Kirim', Expense: 'Chiqim', Incassation: 'Inkassatsiya' })[t] || t
const typeColor = (t) => ({ Income: 'var(--success)', Expense: 'var(--danger)', Incassation: 'var(--warning)' })[t] || ''
const sign = (t) => t === 'Income' ? '+' : '−'

const totalIncome = computed(() => movements.value.filter(m => m.type === 'Income').reduce((s, m) => s + Number(m.amount), 0))
const totalExpense = computed(() => movements.value.filter(m => m.type !== 'Income').reduce((s, m) => s + Number(m.amount), 0))

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true; err.value = ''
  try { movements.value = await cashMovementsApi.list() }
  catch (e) { err.value = errorMessage(e) }
  finally { loading.value = false }
}

async function create() {
  err.value = ''; success.value = ''
  if (!Number(form.value.amount)) { err.value = 'Summani kiriting'; return }
  try {
    await cashMovementsApi.create({ type: form.value.type, amount: Number(form.value.amount), comment: form.value.comment || null })
    success.value = `${typeLabel(form.value.type)} qo'shildi`
    form.value = { type: 'Income', amount: '', comment: '' }
    showForm.value = false
    await load()
  } catch (e) { err.value = errorMessage(e) }
}

async function remove(m) {
  if (!confirm(`Bu ${typeLabel(m.type).toLowerCase()}ni o'chirasizmi?`)) return
  err.value = ''
  try { await cashMovementsApi.remove(m.id); await load() }
  catch (e) { err.value = errorMessage(e) }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Kassa harakatlari</h1>
    <div style="display:flex; gap:12px; align-items:center">
      <span class="muted">Kirim: <strong style="color:var(--success)">{{ fmt(totalIncome) }}</strong></span>
      <span class="muted">Chiqim: <strong style="color:var(--danger)">{{ fmt(totalExpense) }}</strong></span>
      <button class="btn" @click="showForm = !showForm">+ Yozuv</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div v-if="showForm" class="card" style="margin-bottom:16px">
    <h3 style="margin-top:0">Yangi harakat</h3>
    <div class="row">
      <div class="field">
        <label>Turi</label>
        <select v-model="form.type" class="input">
          <option value="Income">Kirim (naqd kirim)</option>
          <option value="Expense">Chiqim (kassa xarajat)</option>
          <option value="Incassation">Inkassatsiya (pul inkasso)</option>
        </select>
      </div>
      <div class="field"><label>Summa</label><input v-model="form.amount" type="number" min="0.01" step="0.01" class="input" /></div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>
    <button class="btn" @click="create">Saqlash</button>
    <button class="btn btn-ghost" style="margin-left:8px" @click="showForm=false">Bekor</button>
  </div>

  <div class="card tbl-main">
    <table class="table">
      <thead><tr><th>Turi</th><th>Izoh</th><th>Sana</th><th class="text-right">Summa</th><th></th></tr></thead>
      <tbody>
        <tr v-for="m in movements" :key="m.id">
          <td><span class="badge" :style="{background: typeColor(m.type), color:'#fff'}">{{ typeLabel(m.type) }}</span></td>
          <td class="muted">{{ m.comment || '—' }}</td>
          <td class="muted">{{ fmtDate(m.createdAt) }}</td>
          <td class="text-right" :style="{color: typeColor(m.type)}">
            {{ sign(m.type) }} {{ fmt(m.amount) }}
          </td>
          <td class="text-right"><button class="btn btn-icon danger" title="O'chirish" @click="remove(m)"><AppIcon name="trash" :size="15" /></button></td>
        </tr>
        <tr v-if="!movements.length && !loading">
          <td colspan="5" class="muted" style="text-align:center;padding:24px">Harakatlar yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
