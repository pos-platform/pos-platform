<script setup>
import { ref, reactive, onMounted } from 'vue'
import { usersApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const users = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

const blank = () => ({ username: '', password: '', role: 'Cashier' })
const form = reactive(blank())

const roleLabel = { Admin: 'Administrator', Manager: 'Menejer', Cashier: 'Kassir' }

async function load() {
  loading.value = true
  error.value = ''
  try {
    users.value = await usersApi.list()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function create() {
  error.value = ''
  success.value = ''
  saving.value = true
  try {
    const u = await usersApi.create({
      username: form.username.trim(),
      password: form.password,
      role: form.role
    })
    success.value = `"${u.username}" (${roleLabel[u.role]}) yaratildi`
    Object.assign(form, blank())
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function toggleActive(u) {
  error.value = ''
  try {
    if (u.isActive) await usersApi.deactivate(u.id)
    else await usersApi.activate(u.id)
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function resetPassword(u) {
  const pwd = window.prompt(`"${u.username}" uchun yangi parol (kamida 6 belgi):`)
  if (!pwd) return
  error.value = ''
  success.value = ''
  try {
    await usersApi.resetPassword(u.id, pwd)
    success.value = `"${u.username}" paroli yangilandi`
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Foydalanuvchilar</h1>
    <span class="muted">{{ users.length }} ta</span>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start">
    <!-- Yangi foydalanuvchi -->
    <form class="card" style="flex: 1; min-width: 300px; max-width: 360px" @submit.prevent="create">
      <h3 style="margin-top: 0">Yangi foydalanuvchi</h3>
      <div class="field">
        <label>Foydalanuvchi nomi</label>
        <input v-model="form.username" class="input" required minlength="3" />
      </div>
      <div class="field">
        <label>Parol</label>
        <input v-model="form.password" type="text" class="input" required minlength="6" />
      </div>
      <div class="field">
        <label>Rol</label>
        <select v-model="form.role" class="select">
          <option value="Cashier">Kassir</option>
          <option value="Manager">Menejer</option>
          <option value="Admin">Administrator</option>
        </select>
      </div>
      <button class="btn" style="width: 100%" :disabled="saving">
        {{ saving ? 'Saqlanmoqda...' : 'Yaratish' }}
      </button>
    </form>

    <!-- Ro'yxat -->
    <div class="card" style="flex: 2; min-width: 360px">
      <table class="table">
        <thead>
          <tr>
            <th>Foydalanuvchi</th>
            <th>Rol</th>
            <th>Holat</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id">
            <td>
              <strong>{{ u.username }}</strong>
              <span v-if="u.username === auth.username" class="muted"> (siz)</span>
            </td>
            <td>{{ roleLabel[u.role] || u.role }}</td>
            <td>
              <span class="pill" :class="u.isActive ? 'pill-green' : 'pill-red'">
                {{ u.isActive ? 'Faol' : 'Bloklangan' }}
              </span>
            </td>
            <td class="text-right">
              <button class="btn btn-ghost btn-sm" @click="resetPassword(u)">Parol</button>
              <button
                v-if="u.username !== auth.username"
                class="btn btn-ghost btn-sm"
                @click="toggleActive(u)"
              >
                {{ u.isActive ? 'Bloklash' : 'Faollashtirish' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
