<script setup>
import { ref, reactive, onMounted } from 'vue'
import { usersApi, rolesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const users = ref([])
const roles = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

const blank = () => ({ username: '', password: '', appRoleId: '' })
const form = reactive(blank())

// Rol o'zgartirish modali
const roleModal = ref(null) // { user, roleId }

async function load() {
  loading.value = true
  error.value = ''
  try {
    ;[users.value, roles.value] = await Promise.all([usersApi.list(), rolesApi.list()])
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
    const payload = {
      username: form.username.trim(),
      password: form.password,
      role: roleName(form.appRoleId) || 'Cashier'
    }
    if (form.appRoleId) payload.appRoleId = form.appRoleId
    const u = await usersApi.create(payload)
    success.value = `"${u.username}" (${u.appRoleName || u.role}) yaratildi`
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

function openRoleModal(u) {
  roleModal.value = { user: u, roleId: u.appRoleId || '' }
}

async function saveRole() {
  if (!roleModal.value?.roleId) return
  error.value = ''
  try {
    await usersApi.assignRole(roleModal.value.user.id, roleModal.value.roleId)
    success.value = `"${roleModal.value.user.username}" roli yangilandi`
    roleModal.value = null
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

function roleName(id) {
  return roles.value.find((r) => r.id === id)?.name || ''
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
        <select v-model="form.appRoleId" class="select">
          <option value="">— Tanlang —</option>
          <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
        </select>
      </div>
      <button class="btn" style="width: 100%" :disabled="saving">
        {{ saving ? 'Saqlanmoqda...' : 'Yaratish' }}
      </button>
    </form>

    <!-- Ro'yxat -->
    <div class="card" style="flex: 2; min-width: 360px">
      <div v-if="loading" style="color: var(--text-muted); padding: 16px">Yuklanmoqda...</div>
      <table v-else class="table">
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
            <td>
              <span class="role-badge">{{ u.appRoleName || u.role }}</span>
            </td>
            <td>
              <span class="pill" :class="u.isActive ? 'pill-green' : 'pill-red'">
                {{ u.isActive ? 'Faol' : 'Bloklangan' }}
              </span>
            </td>
            <td class="text-right" style="white-space: nowrap">
              <button class="btn btn-ghost btn-sm" @click="openRoleModal(u)">Rol</button>
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

  <!-- Rol o'zgartirish modali -->
  <div v-if="roleModal" class="modal-overlay" @click.self="roleModal = null">
    <div class="modal" style="width: 340px">
      <h3 style="margin-top: 0">"{{ roleModal.user.username }}" — rol o'zgartirish</h3>
      <div class="field">
        <label>Yangi rol</label>
        <select v-model="roleModal.roleId" class="select">
          <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
        </select>
      </div>
      <div style="display:flex; gap:8px; margin-top: 16px; justify-content:flex-end">
        <button class="btn btn-ghost" @click="roleModal = null">Bekor</button>
        <button class="btn" @click="saveRole">Saqlash</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.role-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  background: var(--surface);
  border: 1px solid var(--border);
  color: var(--text-muted);
}
</style>
