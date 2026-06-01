<script setup>
import { ref, reactive, onMounted } from 'vue'
import { suppliersApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const suppliers = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)

const blank = () => ({ id: null, name: '', phone: '', address: '' })
const form = reactive(blank())
const editing = ref(false)

async function load() {
  loading.value = true
  err.value = ''
  try {
    suppliers.value = await suppliersApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function startEdit(s) {
  form.id = s.id
  form.name = s.name
  form.phone = s.phone || ''
  form.address = s.address || ''
  editing.value = true
}
function cancelEdit() {
  Object.assign(form, blank())
  editing.value = false
}

async function submit() {
  err.value = ''
  success.value = ''
  saving.value = true
  try {
    const payload = { name: form.name.trim(), phone: form.phone.trim() || null, address: form.address.trim() || null }
    if (editing.value) {
      await suppliersApi.update(form.id, payload)
      success.value = 'Yangilandi'
    } else {
      await suppliersApi.create(payload)
      success.value = `"${payload.name}" qo'shildi`
    }
    cancelEdit()
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function toggle(s) {
  err.value = ''
  try {
    if (s.isActive) await suppliersApi.deactivate(s.id)
    else await suppliersApi.activate(s.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Yetkazib beruvchilar</h1>
    <span class="muted">{{ suppliers.length }} ta</span>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start">
    <!-- Forma -->
    <form class="card" style="flex: 1; min-width: 300px; max-width: 360px" @submit.prevent="submit">
      <h3 style="margin-top: 0">{{ editing ? 'Tahrirlash' : 'Yangi yetkazib beruvchi' }}</h3>
      <div class="field">
        <label>Nomi</label>
        <input v-model="form.name" class="input" required />
      </div>
      <div class="field">
        <label>Telefon</label>
        <input v-model="form.phone" class="input" placeholder="+998..." />
      </div>
      <div class="field">
        <label>Manzil</label>
        <input v-model="form.address" class="input" />
      </div>
      <div class="row">
        <button class="btn" style="flex: 1" :disabled="saving">
          {{ saving ? '...' : (editing ? 'Saqlash' : 'Qo\'shish') }}
        </button>
        <button v-if="editing" type="button" class="btn btn-ghost" @click="cancelEdit">Bekor</button>
      </div>
    </form>

    <!-- Ro'yxat -->
    <div class="card" style="flex: 2; min-width: 380px">
      <table class="table">
        <thead>
          <tr>
            <th>Nomi</th>
            <th>Telefon</th>
            <th class="text-right">Qarz (balans)</th>
            <th>Holat</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in suppliers" :key="s.id">
            <td><strong>{{ s.name }}</strong><div class="muted" style="font-size:12px" v-if="s.address">{{ s.address }}</div></td>
            <td class="muted">{{ s.phone || '—' }}</td>
            <td class="text-right">{{ Number(s.balance).toLocaleString() }}</td>
            <td>
              <span class="pill" :class="s.isActive ? 'pill-green' : 'pill-gray'">
                {{ s.isActive ? 'Aktiv' : 'O\'chiq' }}
              </span>
            </td>
            <td class="text-right">
              <button class="btn btn-ghost btn-sm" @click="startEdit(s)">Tahrir</button>
              <button class="btn btn-ghost btn-sm" @click="toggle(s)">{{ s.isActive ? 'O\'chirish' : 'Yoqish' }}</button>
            </td>
          </tr>
          <tr v-if="!suppliers.length && !loading">
            <td colspan="5" class="muted" style="text-align:center; padding:24px">Yetkazib beruvchi yo'q</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
