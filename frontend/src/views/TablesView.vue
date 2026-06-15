<script setup>
import { ref, reactive, onMounted } from 'vue'
import { tablesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const tables = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)

const showForm = ref(false)
const editingId = ref(null)
const form = reactive({ name: '', hall: '', seats: 4, isActive: true })

function resetForm() {
  editingId.value = null
  form.name = ''
  form.hall = ''
  form.seats = 4
  form.isActive = true
}

function openCreate() {
  resetForm()
  showForm.value = true
}

function openEdit(t) {
  editingId.value = t.id
  form.name = t.name
  form.hall = t.hall || ''
  form.seats = t.seats
  form.isActive = t.isActive
  showForm.value = true
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    tables.value = await tablesApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function submit() {
  if (!form.name.trim()) { err.value = 'Stol nomini kiriting'; return }
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const payload = { name: form.name.trim(), hall: form.hall.trim() || null, seats: Number(form.seats) || 0, isActive: form.isActive }
    if (editingId.value) await tablesApi.update(editingId.value, payload)
    else await tablesApi.create(payload)
    success.value = 'Saqlandi'
    showForm.value = false
    resetForm()
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function remove(t) {
  if (!confirm(`"${t.name}" stolini o'chirasizmi?`)) return
  err.value = ''
  try {
    await tablesApi.remove(t.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

function fmt(n) { return Number(n || 0).toLocaleString() }

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Stollar</h1>
    <button class="btn" @click="showForm ? (showForm = false) : openCreate()">+ Yangi stol</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="submit">
    <h3 style="margin-top:0">{{ editingId ? 'Stolni tahrirlash' : 'Yangi stol' }}</h3>
    <div class="row" style="gap:12px; flex-wrap:wrap">
      <div class="field"><label>Nomi/raqami</label><input v-model="form.name" class="input" placeholder="12" /></div>
      <div class="field"><label>Zal</label><input v-model="form.hall" class="input" placeholder="1-zal" /></div>
      <div class="field"><label>O'rinlar</label><input v-model="form.seats" type="number" min="0" class="input" style="width:100px" /></div>
      <div class="field"><label>Faol</label><select v-model="form.isActive" class="input"><option :value="true">Ha</option><option :value="false">Yo'q</option></select></div>
    </div>
    <button class="btn" :disabled="saving">{{ saving ? '...' : 'Saqlash' }}</button>
  </form>

  <div class="card">
    <table class="table">
      <thead><tr><th>Stol</th><th>Zal</th><th>O'rinlar</th><th>Holat</th><th class="text-right">Ochiq buyurtma</th><th></th></tr></thead>
      <tbody>
        <tr v-for="t in tables" :key="t.id">
          <td><strong>{{ t.name }}</strong></td>
          <td class="muted">{{ t.hall || '—' }}</td>
          <td>{{ t.seats }}</td>
          <td>
            <span class="badge" :style="{ background: t.status === 'Occupied' ? '#ffe9e0' : '#e6f7ec', color: t.status === 'Occupied' ? '#c0392b' : '#1c8a4d' }">
              {{ t.status === 'Occupied' ? 'Band' : 'Bo\'sh' }}
            </span>
          </td>
          <td class="text-right">{{ t.openOrderTotal ? fmt(t.openOrderTotal) + ' so\'m' : '—' }}</td>
          <td class="text-right">
            <button class="btn btn-ghost btn-sm" @click="openEdit(t)">✏️</button>
            <button class="btn btn-ghost btn-sm" @click="remove(t)">🗑️</button>
          </td>
        </tr>
        <tr v-if="!tables.length && !loading"><td colspan="6" class="muted" style="text-align:center;padding:24px">Stol yo'q</td></tr>
      </tbody>
    </table>
  </div>
</template>
