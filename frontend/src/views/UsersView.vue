<script setup>
import { ref, onMounted } from 'vue'
import { usersApi, storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const users = ref([])
const stores = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try {
    ;[users.value, stores.value] = await Promise.all([usersApi.list(), storesApi.list()])
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
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
    <!-- Ro'yxat -->
    <div class="card" style="flex: 1; min-width: 360px">
      <div v-if="loading" style="color: var(--text-muted); padding: 16px">Yuklanmoqda...</div>
      <table v-else class="table">
        <thead>
          <tr>
            <th>Foydalanuvchi</th>
            <th>Rol</th>
            <th v-if="stores.length">Do'kon</th>
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
            <td v-if="stores.length" class="muted" style="font-size:13px">
              {{ u.branchName || '—' }}
            </td>
            <td>
              <span class="pill" :class="u.isActive ? 'pill-green' : 'pill-red'">
                {{ u.isActive ? 'Faol' : 'Bloklangan' }}
              </span>
            </td>
            <td class="text-right" style="white-space: nowrap">
              <button class="btn btn-ghost btn-sm" @click="resetPassword(u)">Parol</button>
            </td>
          </tr>
        </tbody>
      </table>
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
