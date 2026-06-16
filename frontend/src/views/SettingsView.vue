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
    <h1>Sozlamalar — filiallar rejimi</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

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
.seg { display:inline-flex; border:1px solid var(--border); border-radius:8px; overflow:hidden; }
.seg-btn { padding:8px 14px; border:0; background:transparent; cursor:pointer; font-size:13px; color:var(--text-muted); }
.seg-btn.active { background:var(--primary); color:#fff; }
.seg-btn:disabled { opacity:.6; cursor:default; }
</style>
