<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { errorMessage } from '../api/http'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(username.value, password.value)
    const redirect = route.query.redirect
    if (redirect && redirect !== '/') {
      router.push(redirect)
    } else if (auth.isSuperAdmin) {
      router.push({ name: 'organizations' })
    } else if (auth.role === 'Admin') {
      router.push({ name: 'dashboard' })
    } else {
      router.push({ name: 'pos' })
    }
  } catch (e) {
    error.value = errorMessage(e, 'Kirib bo\'lmadi')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-screen">
    <div class="login-card">
      <div class="brand">
        <div class="brand-logo">
          <img src="/logo.png" alt="Logo" />
        </div>
        <h1>PosPlatform</h1>
        <p class="sub">Hisobingizga kiring</p>
      </div>

      <form @submit.prevent="submit">
        <div v-if="error" class="alert alert-error" style="margin-bottom:14px">{{ error }}</div>

        <div class="field">
          <label>Foydalanuvchi nomi</label>
          <input v-model="username" class="input" autocomplete="username" placeholder="login" required autofocus />
        </div>

        <div class="field">
          <label>Parol</label>
          <div class="pwd">
            <input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              class="input"
              autocomplete="current-password"
              placeholder="parol"
              required
            />
            <button type="button" class="eye" @click="showPassword = !showPassword"
                    :title="showPassword ? 'Yashirish' : 'Ko\'rsatish'">
              {{ showPassword ? '🙈' : '👁' }}
            </button>
          </div>
        </div>

        <button class="btn login-btn" :disabled="loading">
          {{ loading ? 'Kirilmoqda...' : 'Kirish' }}
        </button>
      </form>

      <p class="foot">© PosPlatform — savdo boshqaruv tizimi</p>
    </div>
  </div>
</template>

<style scoped>
.login-screen {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: linear-gradient(135deg, #1B5E20 0%, #1565C0 50%, #2196F3 100%);
  overflow: auto;
}

.login-card {
  width: 100%;
  max-width: 380px;
  background: #fff;
  border-radius: 18px;
  padding: 36px 32px 26px;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.28);
}

.brand { text-align: center; margin-bottom: 24px; }
.brand-logo {
  width: 80px; height: 70px; margin: 0 auto 12px;
  display: flex; align-items: center; justify-content: center;
}
.brand-logo img { width: 100%; height: 100%; object-fit: contain; }
.brand h1 { margin: 0; font-size: 26px; font-weight: 800; color: #1565C0; letter-spacing: -0.5px; }
.brand .sub { margin: 6px 0 0; color: #6b7480; font-size: 14px; }

.field { margin-bottom: 16px; }
.field label { display: block; font-size: 13px; font-weight: 600; color: #475569; margin-bottom: 6px; }
.field .input { height: 46px; font-size: 15px; }

.pwd { position: relative; }
.pwd .input { width: 100%; padding-right: 46px; }
.eye {
  position: absolute; right: 6px; top: 50%; transform: translateY(-50%);
  width: 34px; height: 34px; border: none; background: transparent;
  font-size: 17px; cursor: pointer; border-radius: 8px;
}
.eye:hover { background: #f1f5f9; }

.login-btn {
  width: 100%; height: 48px; font-size: 16px; font-weight: 700; margin-top: 6px;
  border-radius: 10px;
}

.foot { text-align: center; color: #94a3b8; font-size: 12px; margin: 20px 0 0; }
</style>
