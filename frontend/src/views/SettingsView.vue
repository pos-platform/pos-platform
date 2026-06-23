<script setup>
import { ref, onMounted } from 'vue'
import { storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const stores = ref([])
const loading = ref(false)
const savingId = ref(null)
const err = ref('')
const success = ''

// ===== Parolni o'zgartirish =====
const pwForm = ref({ current: '', next: '', confirm: '' })
const pwBusy = ref(false)
const pwErr = ref('')
const pwOk = ref('')

async function changePassword() {
  pwErr.value = ''
  pwOk.value = ''
  if (pwForm.value.next.length < 6) {
    pwErr.value = 'Yangi parol kamida 6 belgidan iborat bo\'lishi kerak.'
    return
  }
  if (pwForm.value.next !== pwForm.value.confirm) {
    pwErr.value = 'Yangi parol va tasdiq mos kelmadi.'
    return
  }
  pwBusy.value = true
  try {
    await auth.changePassword(pwForm.value.current, pwForm.value.next)
    pwOk.value = 'Parol muvaffaqiyatli o\'zgartirildi.'
    pwForm.value = { current: '', next: '', confirm: '' }
  } catch (e) {
    pwErr.value = errorMessage(e)
  } finally {
    pwBusy.value = false
  }
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    stores.value = await storesApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function setType(store, type) {
  if (savingId.value || store.businessType === type) return
  savingId.value = store.id
  err.value = ''
  try {
    const updated = await storesApi.setBusinessType(store.id, type)
    store.businessType = updated.businessType
    // Admin navigatsiyasi: tashkilotda kamida bitta restoran filiali bo'lsa restoran menyusi ko'rinadi.
    const anyRestaurant = stores.value.some((s) => s.businessType === 'Restaurant')
    auth.setBusinessType(anyRestaurant ? 'Restaurant' : 'Store')
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    savingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Sozlamalar</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <!-- Parolni o'zgartirish -->
  <div class="card" style="max-width:760px; margin-bottom:16px">
    <h3 style="margin:0 0 4px">Parolni o'zgartirish</h3>
    <p class="muted" style="margin:0 0 12px">Joriy parolingizni tasdiqlab, yangi parol belgilang.</p>
    <div v-if="pwErr" class="alert alert-error">{{ pwErr }}</div>
    <div v-if="pwOk" class="alert alert-success">{{ pwOk }}</div>
    <form class="pw-form" @submit.prevent="changePassword">
      <div class="pw-field">
        <label>Joriy parol</label>
        <input v-model="pwForm.current" type="password" class="input" autocomplete="current-password" required />
      </div>
      <div class="pw-field">
        <label>Yangi parol</label>
        <input v-model="pwForm.next" type="password" class="input" autocomplete="new-password" placeholder="kamida 6 belgi" required />
      </div>
      <div class="pw-field">
        <label>Yangi parol (tasdiq)</label>
        <input v-model="pwForm.confirm" type="password" class="input" autocomplete="new-password" required />
      </div>
      <button class="btn" :disabled="pwBusy" style="align-self:flex-end">{{ pwBusy ? 'Saqlanmoqda...' : 'Parolni saqlash' }}</button>
    </form>
  </div>

  <div class="card" style="max-width:760px">
    <p class="muted">Har bir filial uchun faoliyat turini tanlang. Restoran filialida stol boshqaruvi, ovqat menyusi (retsept) va ombordan oshpazga berish modullari yoqiladi. Bitta tashkilotda do'kon ham, restoran ham bo'lishi mumkin.</p>

    <div v-if="loading" class="muted">Yuklanmoqda...</div>
    <table v-else class="table">
      <thead><tr><th>Filial</th><th class="text-right">Faoliyat turi</th></tr></thead>
      <tbody>
        <tr v-for="s in stores" :key="s.id">
          <td><strong>{{ s.name }}</strong> <span v-if="!s.isActive" class="muted">(faol emas)</span></td>
          <td class="text-right">
            <div class="seg">
              <button :class="['seg-btn', { active: s.businessType === 'Store' }]" :disabled="savingId === s.id" @click="setType(s, 'Store')">🏪 Do'kon</button>
              <button :class="['seg-btn', { active: s.businessType === 'Restaurant' }]" :disabled="savingId === s.id" @click="setType(s, 'Restaurant')">🍽️ Restoran</button>
            </div>
          </td>
        </tr>
        <tr v-if="!stores.length && !loading"><td colspan="2" class="muted" style="text-align:center;padding:24px">Filial yo'q. Avval "Do'konlar" sahifasidan filial qo'shing.</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.pw-form { display:flex; flex-direction:column; gap:10px; max-width:360px; }
.pw-field { display:flex; flex-direction:column; gap:4px; }
.pw-field label { font-size:13px; color:var(--text-muted); font-weight:600; }
.seg { display:inline-flex; border:1px solid var(--border); border-radius:8px; overflow:hidden; }
.seg-btn { padding:8px 14px; border:0; background:transparent; cursor:pointer; font-size:13px; color:var(--text-muted); }
.seg-btn.active { background:var(--primary); color:#fff; }
.seg-btn:disabled { opacity:.6; cursor:default; }
</style>
