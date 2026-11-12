<script setup>
import { ref, reactive, onMounted } from 'vue'
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

// Rol nomlari (UserRole enum → o'zbekcha)
const ROLE_LABEL = { Admin: 'Admin', Manager: 'Menejer', Cashier: 'Kassir', Courier: 'Kuryer', SuperAdmin: 'SuperAdmin' }

// Yangi kuryer: litsenziya va kassa ID kerak emas (kassaga kira olmaydi, faqat mobil ilova).
// Kassir/menejer loginlari platforma tomonidan (obuna bilan) yaratiladi.
const showCourierForm = ref(false)
const saving = ref(false)
const blankCourier = () => ({ username: '', password: '', fullName: '', phone: '', branchId: '' })
const courierForm = reactive(blankCourier())

async function createCourier() {
  error.value = ''
  success.value = ''
  const phone = courierForm.phone.trim()
  if (phone.replace(/\D/g, '').length < 9) {
    error.value = 'Kuryer telefon raqamini to\'liq kiriting.'
    return
  }
  if (!courierForm.branchId) {
    error.value = 'Kuryer qaysi do\'konga (filialga) biriktirilishini tanlang.'
    return
  }
  saving.value = true
  try {
    const u = await usersApi.create({
      username: courierForm.username.trim(),
      password: courierForm.password,
      fullName: courierForm.fullName.trim() || null,
      phone,
      role: 'Courier',
      branchId: courierForm.branchId
    })
    success.value = `Kuryer "${u.username}" yaratildi`
    Object.assign(courierForm, blankCourier())
    showCourierForm.value = false
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
    <div style="display:flex; align-items:center; gap:12px">
      <span class="muted">{{ users.length }} ta</span>
      <button v-if="auth.hasPermission('users.create')" class="btn btn-sm" @click="showCourierForm = !showCourierForm">+ Kuryer</button>
    </div>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start">
    <!-- Yangi kuryer (faqat mobil ilova; litsenziya/kassa ID yo'q) -->
    <form v-if="showCourierForm" class="card" style="flex: 1; min-width: 300px; max-width: 360px" @submit.prevent="createCourier">
      <h3 style="margin-top: 0">Yangi kuryer</h3>
      <p class="muted" style="font-size:12px; margin:0 0 10px">
        Kuryer mobil ilovadan login/parol bilan kiradi. Kassaga kira olmaydi, obuna (litsenziya) talab qilinmaydi.
      </p>
      <div class="field">
        <label>Login *</label>
        <input v-model="courierForm.username" class="input" required minlength="3" />
      </div>
      <div class="field">
        <label>Parol *</label>
        <input v-model="courierForm.password" type="text" class="input" required minlength="6" placeholder="kamida 6 belgi" />
      </div>
      <div class="field">
        <label>Ism sharifi</label>
        <input v-model="courierForm.fullName" class="input" maxlength="200" placeholder="Masalan: Bekzod" />
      </div>
      <div class="field">
        <label>Telefon *</label>
        <input v-model="courierForm.phone" class="input" required maxlength="20" placeholder="+998 90 123 45 67" />
      </div>
      <div class="field">
        <label>Do'kon (filial) *</label>
        <select v-model="courierForm.branchId" class="select" required>
          <option value="" disabled>— Tanlang —</option>
          <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      <button class="btn" style="width: 100%" :disabled="saving">
        {{ saving ? 'Saqlanmoqda...' : 'Yaratish' }}
      </button>
    </form>

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
              <span class="role-badge" :class="{ courier: u.role === 'Courier' }">{{ u.role === 'Courier' ? 'Kuryer' : (u.appRoleName || ROLE_LABEL[u.role] || u.role) }}</span>
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
              <!-- Kuryerni do'kon admini o'zi bloklaydi/yoqadi (litsenziyasiz) -->
              <button
                v-if="u.role === 'Courier'"
                class="btn btn-ghost btn-sm"
                @click="toggleActive(u)"
              >{{ u.isActive ? 'Bloklash' : 'Yoqish' }}</button>
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
.role-badge.courier {
  background: #ecfeff;
  border-color: #a5f3fc;
  color: #155e75;
}
</style>
