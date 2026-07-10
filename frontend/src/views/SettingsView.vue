<script setup>
import { ref } from 'vue'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const err = ref('')

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
</script>

<template>
  <div class="page-head">
    <h1>Sozlamalar</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <!-- Parolni o'zgartirish -->
  <div class="card" style="max-width:760px">
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
</template>

<style scoped>
.pw-form { display:flex; flex-direction:column; gap:10px; max-width:360px; }
.pw-field { display:flex; flex-direction:column; gap:4px; }
.pw-field label { font-size:13px; color:var(--text-muted); font-weight:600; }
</style>
