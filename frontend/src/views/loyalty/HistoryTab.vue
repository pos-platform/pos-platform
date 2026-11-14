<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { loyaltyApi } from '../../api/catalog'
import { errorMessage } from '../../api/http'

// Bosqich 10.2: cashback sozlamalari o'zgarishlari tarixi (kim, qachon, nima o'zgardi).
const FIELD_LABELS = {
  enabled: 'Tizim yoqilgan',
  cashbackPercent: 'Cashback foizi',
  minSaleAmount: 'Minimal savdo summasi',
  maxRedeemPercent: "Ball bilan to'lash chegarasi, %",
  pointsExpireDays: 'Ball muddati (kun)',
  signupBonus: "Ro'yxatdan o'tish bonusi",
  earnOnDiscountedItems: 'Chegirmali tovarlarga ham'
}
function fieldText(f) {
  if (!f) return '—'
  if (f.startsWith('category:')) return `Kategoriya stavkasi: ${f.slice(9)}`
  if (f.startsWith('tier:')) return `Daraja: ${f.slice(5)}`
  return FIELD_LABELS[f] || f
}
const valText = (v) => (v === null || v === undefined || v === '' ? '—' : v === 'true' ? 'ha' : v === 'false' ? "yo'q" : v)

const page = ref(1)
const pageSize = 50
const data = ref({ items: [], total: 0 })
const loading = ref(false)
const err = ref('')
const pages = computed(() => Math.max(1, Math.ceil((data.value.total || 0) / pageSize)))
const fmtDate = (d) => (d ? new Date(d).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '')

async function load() {
  loading.value = true
  err.value = ''
  try {
    const r = await loyaltyApi.settingsHistory({ page: page.value, pageSize })
    data.value = { items: Array.isArray(r) ? r : r?.items || [], total: Array.isArray(r) ? r.length : r?.total || 0 }
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

watch(page, load)
onMounted(load)
defineExpose({ load })
</script>

<template>
  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div class="card" style="overflow:auto">
    <table class="table">
      <thead><tr><th>Sana</th><th>Xodim</th><th>Maydon</th><th>Eski qiymat</th><th>Yangi qiymat</th></tr></thead>
      <tbody>
        <tr v-if="loading"><td colspan="5" class="muted" style="text-align:center; padding:20px">Yuklanmoqda...</td></tr>
        <tr v-else-if="!data.items.length"><td colspan="5" class="muted" style="text-align:center; padding:20px">O'zgarishlar yo'q</td></tr>
        <tr v-for="(h, idx) in data.items" v-else :key="idx">
          <td class="muted" style="white-space:nowrap">{{ fmtDate(h.changedAt) }}</td>
          <td>{{ h.userName || '—' }}</td>
          <td><strong>{{ fieldText(h.field) }}</strong></td>
          <td class="muted">{{ valText(h.oldValue) }}</td>
          <td>{{ valText(h.newValue) }}</td>
        </tr>
      </tbody>
    </table>
    <div v-if="pages > 1" class="pager">
      <button class="btn btn-ghost btn-sm" :disabled="page <= 1 || loading" @click="page--">‹</button>
      <span class="muted small">{{ page }} / {{ pages }}</span>
      <button class="btn btn-ghost btn-sm" :disabled="page >= pages || loading" @click="page++">›</button>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.pager { display:flex; justify-content:center; align-items:center; gap:10px; padding:12px; }
</style>
