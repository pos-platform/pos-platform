<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { storesApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const router = useRouter()
const stores = ref([])
const loading = ref(false)
const err = ref('')
const filterStore = ref('')
const filterStatus = ref('')

function fmt(n) { return Number(n || 0).toLocaleString() }

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

const allCashboxRows = computed(() => {
  const rows = []
  for (const s of stores.value) {
    if (filterStore.value && s.id !== filterStore.value) continue
    for (const c of s.cashboxes) {
      if (filterStatus.value === 'active' && !c.isActive) continue
      if (filterStatus.value === 'inactive' && c.isActive) continue
      rows.push({ store: s, cashbox: c })
    }
  }
  return rows
})

// Bir do'konning bir nechta kassalari bo'lsa — birinchi kassa store nomini ko'rsatadi
function storeLabel(row, idx) {
  if (idx === 0) return true
  return allCashboxRows.value[idx - 1]?.store.id !== row.store.id
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Kassalar</h1>
      <div class="muted" style="font-size:13px">Do'konlar va kassalar holati</div>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <!-- Filters -->
  <div class="card" style="margin-bottom:14px; padding:14px 18px">
    <div style="display:flex; gap:12px; flex-wrap:wrap; align-items:center">
      <select v-model="filterStore" class="select" style="width:220px">
        <option value="">Barcha do'konlar</option>
        <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
      </select>
      <select v-model="filterStatus" class="select" style="width:180px">
        <option value="">Barcha holat</option>
        <option value="active">Aktiv</option>
        <option value="inactive">O'chiq</option>
      </select>
      <span class="muted" style="font-size:13px; margin-left:auto">
        Jami kassalar: <strong>{{ allCashboxRows.length }}</strong>
      </span>
    </div>
  </div>

  <!-- Table -->
  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Do'kon</th>
          <th>Balans</th>
          <th>Kassa</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td colspan="5" class="muted" style="text-align:center; padding:32px">Yuklanmoqda...</td>
        </tr>
        <template v-for="(row, idx) in allCashboxRows" :key="row.cashbox.id">
          <tr>
            <td>
              <template v-if="storeLabel(row, idx)">
                <strong>{{ row.store.name }}</strong>
                <div v-if="row.store.address" class="muted" style="font-size:12px">{{ row.store.address }}</div>
              </template>
            </td>
            <td>
              <template v-if="storeLabel(row, idx)">
                <strong>{{ fmt(row.store.balance) }}</strong>
              </template>
            </td>
            <td>{{ row.cashbox.name }}</td>
            <td>
              <span class="pill" :class="row.cashbox.isActive ? 'pill-green' : 'pill-gray'">
                {{ row.cashbox.isActive ? 'AKTIV' : 'O\'CHIQ' }}
              </span>
            </td>
            <td class="text-right">
              <button
                class="btn btn-ghost btn-sm"
                @click="router.push({ name: 'cashbox', params: { id: row.cashbox.id } })"
              ><AppIcon name="barChart" :size="15" />Ko'rish</button>
            </td>
          </tr>
        </template>
        <tr v-if="!allCashboxRows.length && !loading">
          <td colspan="5" class="muted" style="text-align:center; padding:32px">Kassa yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
