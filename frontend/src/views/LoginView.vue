<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { errorMessage } from '../api/http'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const username = ref('admin')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(username.value, password.value)
    const redirect = route.query.redirect || { name: 'pos' }
    router.push(redirect)
  } catch (e) {
    error.value = errorMessage(e, 'Kirib bo\'lmadi')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-wrap">
    <form class="card login-card" @submit.prevent="submit">
      <h1>PosPlatform</h1>
      <p class="sub">Tizimga kirish</p>

      <div v-if="error" class="alert alert-error">{{ error }}</div>

      <div class="field">
        <label>Foydalanuvchi nomi</label>
        <input v-model="username" class="input" autocomplete="username" required />
      </div>
      <div class="field">
        <label>Parol</label>
        <input v-model="password" type="password" class="input" autocomplete="current-password" required />
      </div>

      <button class="btn" style="width: 100%" :disabled="loading">
        {{ loading ? 'Kirilmoqda...' : 'Kirish' }}
      </button>
    </form>
  </div>
</template>
