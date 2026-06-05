<script setup>
import { ref, watch, onMounted } from 'vue'
import { salesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'

const storeFilter = useStoreFilterStore()

const sales = ref([])
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
    const params = storeFilter.selectedBranchId ? { branchId: storeFilter.selectedBranchId } : undefined
    sales.value = await salesApi.list(params)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Sidebar'dan do'kon o'zgarsa ro'yxatni qayta yuklaydi
watch(() => storeFilter.selectedBranchId, () => load())

onMounted(load)

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
