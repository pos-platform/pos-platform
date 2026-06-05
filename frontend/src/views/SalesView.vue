<script setup>
import { ref, onMounted } from 'vue'
import { salesApi, storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const sales = ref([])
const stores = ref([])
const filterBranchId = ref('')
const loading = ref(false)
const error = ref('')
const success = ref('')
const refundingId = ref(null)

const fiscalPill = {
  NotSent: 'pill-gray',
  Queued: 'pill-amber',
  Registered: 'pill-green',
  Failed: 'pill-red'
}
const fiscalLabel = {
  NotSent: 'Yuborilmagan',
  Queued: 'Navbatda',
  Registered: 'Fiskal',
  Failed: 'Xato'
}

function fmtDate(d) {
  return new Date(d).toLocaleString('uz-UZ')
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const params = filterBranchId.value ? { branchId: filterBranchId.value } : undefined
    sales.value = await salesApi.list(params)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try { stores.value = await storesApi.list() } catch { /* do'konlar yuklashni e'tiborsiz qoldiramiz */ }
  await load()
})

async function refund(sale) {
  if (!window.confirm(`"${sale.receiptNumber}" chekni qaytarasizmi?`)) return
  error.value = ''
  success.value = ''
  refundingId.value = sale.id
  try {
    const r = await salesApi.refund(sale.id)
    success.value = `Qaytarildi — return cheki ${r.receiptNumber}`
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    refundingId.value = null
  }
}
</script>

<template>
  <div class="page-head">
    <h1>Savdolar</h1>
    <button class="btn btn-ghost btn-sm" @click="load">Yangilash</button>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Do'kon filtri (bir necha filial bo'lsa ko'rinadi) -->
  <div v-if="stores.length > 1" class="card" style="margin-bottom:12px; padding:12px 16px">
    <div style="display:flex; gap:12px; align-items:center; flex-wrap:wrap">
      <select v-model="filterBranchId" class="select" style="min-width:200px" @change="load">
        <option value="">Barcha do'konlar</option>
        <option v-for="b in stores" :key="b.id" :value="b.id">{{ b.name }}</option>
      </select>
      <span class="muted" style="font-size:13px">{{ sales.length }} ta savdo</span>
    </div>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Chek</th>
          <th>Tur</th>
          <th>Sana</th>
          <th>To'lov</th>
          <th class="text-right">Jami</th>
          <th>Fiskal</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in sales" :key="s.id">
          <td><strong>{{ s.receiptNumber }}</strong></td>
          <td>
            <span v-if="s.type === 'Refund'" class="pill pill-red">Qaytarish</span>
            <span v-else-if="s.status === 'Refunded'" class="pill pill-amber">Qaytarilgan</span>
            <span v-else class="pill pill-gray">Sotuv</span>
          </td>
          <td class="muted">{{ fmtDate(s.createdAt) }}</td>
          <td>{{ s.paymentMethod }}</td>
          <td class="text-right" :style="{ color: s.type === 'Refund' ? 'var(--danger)' : 'inherit' }">
            {{ s.type === 'Refund' ? '− ' : '' }}{{ Number(s.totalAmount).toLocaleString() }}
          </td>
          <td>
            <span class="pill" :class="fiscalPill[s.receipt?.fiscalStatus] || 'pill-gray'">
              {{ fiscalLabel[s.receipt?.fiscalStatus] || '—' }}
            </span>
          </td>
          <td class="text-right">
            <button
              v-if="s.type === 'Sale' && s.status !== 'Refunded'"
              class="btn btn-ghost btn-sm"
              :disabled="refundingId === s.id"
              @click="refund(s)"
            >
              {{ refundingId === s.id ? '...' : 'Qaytarish' }}
            </button>
          </td>
        </tr>
        <tr v-if="!sales.length && !loading">
          <td colspan="7" class="muted" style="text-align: center; padding: 24px">
            Hozircha savdo yo'q
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
