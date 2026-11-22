<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')
const year = new Date().getFullYear()

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
    <aside class="side">
      <div class="side-brand">
        <img src="/logo.png" alt="Logo" />
        <span>PosCloud</span>
      </div>

      <div class="side-mid">
        <h2>Savdoni bitta tizimda<br />boshqaring</h2>
        <p>Kassa, ombor, mijozlar va hisobotlar — barcha filiallaringiz uchun yagona bulutli platforma.</p>
        <ul class="feats">
          <li><span class="fi"><AppIcon name="cart" :size="18" /></span>Tezkor kassa va chek chop etish</li>
          <li><span class="fi"><AppIcon name="package" :size="18" /></span>Ombor qoldiqlari va kirim-chiqim nazorati</li>
          <li><span class="fi"><AppIcon name="barChart" :size="18" /></span>Real vaqtdagi savdo hisobotlari</li>
          <li><span class="fi"><AppIcon name="shield" :size="18" /></span>Rollar bo'yicha xavfsiz kirish</li>
        </ul>
      </div>

      <p class="side-foot">© {{ year }} PosCloud — savdo boshqaruv tizimi</p>
    </aside>

    <main class="main">
      <div class="login-card">
        <div class="brand">
          <img class="brand-logo" src="/logo.png" alt="Logo" />
          <h1>Tizimga kirish</h1>
          <p class="sub">Davom etish uchun login va parolingizni kiriting</p>
        </div>

        <form @submit.prevent="submit">
          <div v-if="error" class="err">
            <AppIcon name="alert" :size="16" />
            <span>{{ error }}</span>
          </div>

          <div class="field">
            <label for="lg-user">Foydalanuvchi nomi</label>
            <div class="ctl">
              <AppIcon class="lead" name="user" :size="18" />
              <input id="lg-user" v-model="username" class="input" autocomplete="username"
                     placeholder="Loginni kiriting" required autofocus />
            </div>
          </div>

          <div class="field">
            <label for="lg-pwd">Parol</label>
            <div class="ctl">
              <AppIcon class="lead" name="lock" :size="18" />
              <input
                id="lg-pwd"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                class="input"
                autocomplete="current-password"
                placeholder="Parolni kiriting"
                required
              />
              <button type="button" class="eye" @click="showPassword = !showPassword"
                      :title="showPassword ? 'Yashirish' : 'Ko\'rsatish'">
                <AppIcon :name="showPassword ? 'eyeOff' : 'eye'" :size="18" />
              </button>
            </div>
          </div>

          <button class="login-btn" :disabled="loading">
            <span v-if="loading" class="spin"></span>
            {{ loading ? 'Kirilmoqda...' : 'Kirish' }}
          </button>
        </form>

        <p class="foot">
          <AppIcon name="lock" :size="13" />
          Ulanish shifrlangan va himoyalangan
        </p>
      </div>
    </main>
  </div>
</template>

<style scoped>
.login-screen {
  position: fixed;
  inset: 0;
  display: grid;
  grid-template-columns: minmax(380px, 46%) 1fr;
  background: #f4f6f9;
  overflow: auto;
}

/* ---------- Chap brend panel ---------- */
.side {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 40px 56px;
  color: #e2e8f0;
  background:
    radial-gradient(900px 500px at 110% -10%, rgba(56, 120, 255, .28), transparent 60%),
    radial-gradient(700px 400px at -20% 110%, rgba(20, 184, 166, .18), transparent 60%),
    linear-gradient(160deg, #0b1324 0%, #0f1d3a 55%, #10224a 100%);
  overflow: hidden;
}
.side::before {
  content: '';
  position: absolute; inset: 0;
  background-image:
    linear-gradient(rgba(255,255,255,.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255,255,255,.04) 1px, transparent 1px);
  background-size: 44px 44px;
  -webkit-mask-image: linear-gradient(180deg, #000 0%, transparent 85%);
  mask-image: linear-gradient(180deg, #000 0%, transparent 85%);
  pointer-events: none;
}
.side > * { position: relative; }

.side-brand { display: flex; align-items: center; gap: 12px; }
.side-brand img {
  width: 44px; height: 44px; object-fit: contain;
  background: #fff; border-radius: 10px; padding: 5px;
}
.side-brand span { font-size: 20px; font-weight: 700; color: #fff; letter-spacing: -.01em; }

.side-mid { max-width: 460px; }
.side-mid h2 {
  margin: 0 0 14px; font-size: 34px; line-height: 1.2; font-weight: 700;
  color: #fff; letter-spacing: -.02em;
}
.side-mid > p { margin: 0 0 30px; font-size: 15px; line-height: 1.6; color: #a5b4cc; }

.feats { list-style: none; margin: 0; padding: 0; display: grid; gap: 14px; }
.feats li { display: flex; align-items: center; gap: 12px; font-size: 14.5px; color: #cbd5e1; }
.fi {
  flex: none; width: 34px; height: 34px; border-radius: 9px;
  display: flex; align-items: center; justify-content: center;
  background: rgba(255,255,255,.07); border: 1px solid rgba(255,255,255,.1); color: #7fb0ff;
}

.side-foot { margin: 0; font-size: 12.5px; color: #64748b; }

/* ---------- O'ng forma ---------- */
.main {
  display: flex; align-items: center; justify-content: center;
  padding: 32px 20px;
}
.login-card { width: 100%; max-width: 400px; }

.brand { margin-bottom: 30px; }
.brand-logo { display: none; width: 56px; height: 56px; object-fit: contain; margin-bottom: 14px; }
.brand h1 { margin: 0; font-size: 28px; font-weight: 700; color: #0f172a; letter-spacing: -.02em; }
.brand .sub { margin: 8px 0 0; color: #64748b; font-size: 14.5px; }

.err {
  display: flex; align-items: center; gap: 8px;
  padding: 11px 14px; margin-bottom: 18px;
  border-radius: 10px; font-size: 13.5px;
  color: #b42318; background: #fef3f2; border: 1px solid #fecdca;
}

.field { margin-bottom: 18px; }
.field label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 7px; }

.ctl { position: relative; }
.ctl .lead {
  position: absolute; left: 14px; top: 50%; transform: translateY(-50%);
  color: #94a3b8; pointer-events: none;
}
.ctl .input {
  width: 100%; height: 50px; font-size: 15px;
  padding: 0 46px 0 44px;
  background: #fff; border: 1px solid #d5dbe4; border-radius: 10px;
  transition: border-color .15s, box-shadow .15s;
}
.ctl .input:hover { border-color: #b8c2d1; }
.ctl .input:focus {
  outline: none; border-color: #2563eb;
  box-shadow: 0 0 0 4px rgba(37, 99, 235, .12);
}
.ctl:focus-within .lead { color: #2563eb; }

.eye {
  position: absolute; right: 7px; top: 50%; transform: translateY(-50%);
  width: 36px; height: 36px; border: none; background: transparent;
  cursor: pointer; border-radius: 8px;
  display: flex; align-items: center; justify-content: center; color: #64748b;
}
.eye:hover { background: #f1f5f9; color: #0f172a; }

.login-btn {
  width: 100%; height: 50px; margin-top: 8px;
  display: flex; align-items: center; justify-content: center; gap: 10px;
  border: none; border-radius: 10px; cursor: pointer;
  font-size: 15.5px; font-weight: 600; color: #fff; letter-spacing: .01em;
  background: linear-gradient(180deg, #2563eb, #1d4ed8);
  box-shadow: 0 1px 2px rgba(15,23,42,.1), 0 6px 16px -6px rgba(37,99,235,.55);
  transition: filter .15s, transform .05s;
}
.login-btn:hover:not(:disabled) { filter: brightness(1.07); }
.login-btn:active:not(:disabled) { transform: translateY(1px); }
.login-btn:disabled { opacity: .7; cursor: default; }

.spin {
  width: 16px; height: 16px; border-radius: 50%;
  border: 2px solid rgba(255,255,255,.4); border-top-color: #fff;
  animation: spin .7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.foot {
  display: flex; align-items: center; justify-content: center; gap: 6px;
  margin: 26px 0 0; color: #94a3b8; font-size: 12.5px;
}

/* ---------- Mobil ---------- */
@media (max-width: 900px) {
  .login-screen { grid-template-columns: 1fr; }
  .side { display: none; }
  .main { padding: 24px 16px; }
  .login-card {
    background: #fff; border: 1px solid #e2e8f0; border-radius: 14px;
    padding: 32px 24px; box-shadow: 0 10px 30px -12px rgba(15,23,42,.15);
  }
  .brand { text-align: center; }
  .brand-logo { display: inline-block; }
}
</style>
