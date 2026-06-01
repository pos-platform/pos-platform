<script setup>
import { ref, reactive, onMounted } from 'vue'
import { storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const stores = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const showStoreForm = ref(false)
const storeForm = reactive({ name: '', address: '' })
const cashboxName = reactive({}) // storeId -> input value

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

async function createStore() {
  err.value = ''
  success.value = ''
  try {
    await storesApi.createStore({ name: storeForm.name.trim(), address: storeForm.address.trim() || null })
    success.value = `"${storeForm.name}" do'koni qo'shildi`
    storeForm.name = ''
    storeForm.address = ''
    showStoreForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function toggleStore(s) {
  err.value = ''
  try {
    if (s.isActive) await storesApi.deactivateStore(s.id)
    else await storesApi.activateStore(s.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function addCashbox(s) {
  const name = (cashboxName[s.id] || '').trim()
  if (!name) return
  err.value = ''
  try {
    await storesApi.addCashbox({ branchId: s.id, name })
    cashboxName[s.id] = ''
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function toggleCashbox(c) {
  err.value = ''
  try {
    if (c.isActive) await storesApi.deactivateCashbox(c.id)
    else await storesApi.activateCashbox(c.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Do'konlar</h1>
    <button class="btn" @click="showStoreForm = !showStoreForm">+ Do'kon qo'shish</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Yangi do'kon formasi -->
  <form v-if="showStoreForm" class="card" style="margin-bottom: 16px" @submit.prevent="createStore">
    <h3 style="margin-top: 0">Yangi do'kon</h3>
    <div class="row">
      <div class="field">
        <label>Nomi</label>
        <input v-model="storeForm.name" class="input" required />
      </div>
      <div class="field">
        <label>Manzil</label>
        <input v-model="storeForm.address" class="input" />
      </div>
    </div>
    <button class="btn">Saqlash</button>
  </form>

  <!-- Do'konlar -->
  <div v-for="s in stores" :key="s.id" class="card" style="margin-bottom: 14px">
    <div class="row" style="align-items: center; justify-content: space-between">
      <div>
        <h3 style="margin: 0">{{ s.name }}</h3>
        <span class="muted" v-if="s.address">{{ s.address }}</span>
      </div>
      <div style="display: flex; align-items: center; gap: 12px">
        <span class="muted">Balans: <strong>{{ Number(s.balance).toLocaleString() }}</strong></span>
        <span class="pill" :class="s.isActive ? 'pill-green' : 'pill-gray'">
          {{ s.isActive ? 'Aktiv' : 'O\'chiq' }}
        </span>
        <button class="btn btn-ghost btn-sm" @click="toggleStore(s)">
          {{ s.isActive ? 'O\'chirish' : 'Yoqish' }}
        </button>
      </div>
    </div>

    <hr style="border: none; border-top: 1px solid var(--border); margin: 12px 0" />

    <div class="muted" style="font-weight: 600; font-size: 13px; margin-bottom: 8px">Kassalar</div>
    <table class="table" style="margin-bottom: 12px">
      <tbody>
        <tr v-for="c in s.cashboxes" :key="c.id">
          <td>{{ c.name }}</td>
          <td class="text-right muted">Balans: {{ Number(c.balance).toLocaleString() }}</td>
          <td style="width: 110px">
            <span class="pill" :class="c.isActive ? 'pill-green' : 'pill-gray'">
              {{ c.isActive ? 'Aktiv' : 'O\'chiq' }}
            </span>
          </td>
          <td class="text-right" style="width: 110px">
            <button class="btn btn-ghost btn-sm" @click="toggleCashbox(c)">
              {{ c.isActive ? 'O\'chirish' : 'Yoqish' }}
            </button>
          </td>
        </tr>
        <tr v-if="!s.cashboxes.length">
          <td colspan="4" class="muted">Kassa yo'q</td>
        </tr>
      </tbody>
    </table>

    <div class="row" style="max-width: 360px">
      <input v-model="cashboxName[s.id]" class="input" placeholder="Yangi kassa nomi" style="flex: 1" />
      <button class="btn btn-ghost" @click="addCashbox(s)">+ Kassa</button>
    </div>
  </div>

  <p v-if="!stores.length && !loading" class="muted">Do'kon yo'q</p>
</template>
