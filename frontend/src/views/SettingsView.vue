<script setup>
import { ref, onMounted } from 'vue'
import { myOrganizationApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const org = ref(null)
const loading = ref(false)
const saving = ref(false)
const err = ref('')
const success = ref('')

async function load() {
  loading.value = true
  err.value = ''
  try {
    org.value = await myOrganizationApi.get()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function setType(type) {
  if (saving.value || !org.value || org.value.businessType === type) return
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    org.value = await myOrganizationApi.setBusinessType(type)
    auth.setBusinessType(org.value.businessType)
    success.value = org.value.businessType === 'Restaurant'
      ? 'Restoran rejimi yoqildi. Stollar/Ovqatlar/Oshpazga berish menyusi paydo bo\'ldi.'
      : 'Do\'kon rejimi yoqildi.'
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Sozlamalar</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card" style="max-width:640px">
    <h3 style="margin-top:0">Faoliyat turi</h3>
    <p class="muted">Tashkilotingiz qanday ishlashini tanlang. Restoran rejimida stol boshqaruvi, ovqat menyusi (retsept) va ombordan oshpazga berish modullari yoqiladi.</p>

    <div v-if="loading" class="muted">Yuklanmoqda...</div>
    <div v-else-if="org" class="mode-cards">
      <button
        class="mode-card"
        :class="{ active: org.businessType === 'Store' }"
        :disabled="saving"
        @click="setType('Store')"
      >
        <div class="mode-ico">🏪</div>
        <div class="mode-title">Do'kon</div>
        <div class="mode-desc">Chakana savdo, ombor, kassa</div>
      </button>
      <button
        class="mode-card"
        :class="{ active: org.businessType === 'Restaurant' }"
        :disabled="saving"
        @click="setType('Restaurant')"
      >
        <div class="mode-ico">🍽️</div>
        <div class="mode-title">Restoran</div>
        <div class="mode-desc">Stollar, ovqat menyusi, oshxona</div>
      </button>
    </div>
  </div>
</template>

<style scoped>
.mode-cards { display:flex; gap:16px; margin-top:12px; }
.mode-card { flex:1; padding:20px; border:2px solid var(--border); border-radius:12px; background:transparent; cursor:pointer; text-align:center; transition:all .15s; }
.mode-card:hover:not(:disabled) { border-color:var(--primary); }
.mode-card.active { border-color:var(--primary); background:rgba(0,120,255,.06); }
.mode-card:disabled { opacity:.6; cursor:default; }
.mode-ico { font-size:42px; }
.mode-title { font-size:18px; font-weight:600; margin-top:8px; }
.mode-desc { font-size:12px; color:var(--text-muted); margin-top:4px; }
</style>
