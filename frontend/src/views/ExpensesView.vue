<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { expensesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const expenses = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const showForm = ref(false)
const form = reactive({ title: '', amount: 0, comment: '' })

const total = computed(() => expenses.value.reduce((s, e) => s + Number(e.amount), 0))

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    expenses.value = await expensesApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function create() {
  err.value = ''
  success.value = ''
  if (!Number(form.amount)) { err.value = 'Summani kiriting'; return }
  try {
    await expensesApi.create({ title: form.title.trim(), amount: Number(form.amount), comment: form.comment.trim() || null })
    success.value = 'Xarajat qo\'shildi'
    Object.assign(form, { title: '', amount: 0, comment: '' })
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function remove(e) {
  if (!window.confirm(`"${e.title}" xarajatini o'chirasizmi?`)) return
  err.value = ''
  try {
    await expensesApi.remove(e.id)
    await load()
  } catch (ex) {
    err.value = errorMessage(ex)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Xarajatlar</h1>
    <div style="display:flex; gap:12px; align-items:center">
      <span class="muted">Jami: <strong>{{ fmt(total) }}</strong></span>
      <button class="btn" @click="showForm = !showForm">+ Xarajat</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="create">
    <h3 style="margin-top:0">Yangi xarajat</h3>
    <div class="row">
      <div class="field"><label>Nomi</label><input v-model="form.title" class="input" required placeholder="Ijara, oylik..." /></div>
      <div class="field"><label>Summa</label><input v-model="form.amount" type="number" min="0" class="input" required /></div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>
    <button class="btn">Saqlash</button>
  </form>

  <div class="card">
    <table class="table">
      <thead><tr><th>Nomi</th><th>Sana</th><th>Izoh</th><th class="text-right">Summa</th><th></th></tr></thead>
      <tbody>
        <tr v-for="e in expenses" :key="e.id">
          <td><strong>{{ e.title }}</strong></td>
          <td class="muted">{{ fmtDate(e.createdAt) }}</td>
          <td class="muted">{{ e.comment || '—' }}</td>
          <td class="text-right" style="color:var(--danger)">− {{ fmt(e.amount) }}</td>
          <td class="text-right"><button class="btn btn-icon danger" title="O'chirish" @click="remove(e)"><AppIcon name="trash" :size="15" /></button></td>
        </tr>
        <tr v-if="!expenses.length && !loading"><td colspan="5" class="muted" style="text-align:center;padding:24px">Xarajat yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>
