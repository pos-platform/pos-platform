<script setup>
import { ref, computed, onMounted } from 'vue'
import { customersApi, loyaltyApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const customers = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const showForm = ref(false)
const editItem = ref(null)
const adjustItem = ref(null)
const adjustPoints = ref(0)
const adjustComment = ref('')

const form = ref({ name: '', phone: '', email: '', address: '', discountPercent: 0, comment: '' })

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }

const search = ref('')
const filtered = computed(() => {
  if (!search.value) return customers.value
  const q = search.value.toLowerCase()
  return customers.value.filter(c => c.name.toLowerCase().includes(q) || (c.phone || '').includes(q))
})

async function load() {
  loading.value = true; err.value = ''
  try { customers.value = await customersApi.list() }
  catch (e) { err.value = errorMessage(e) }
  finally { loading.value = false }
}

function openNew() { editItem.value = null; form.value = { name: '', phone: '', email: '', address: '', discountPercent: 0, comment: '' }; showForm.value = true }
function openEdit(c) {
  editItem.value = c
  form.value = { name: c.name, phone: c.phone || '', email: c.email || '', address: c.address || '', discountPercent: c.discountPercent, comment: c.comment || '' }
  showForm.value = true
}

async function save() {
  err.value = ''; success.value = ''
  if (!form.value.name.trim()) { err.value = 'Ism kiriting'; return }
  try {
    if (editItem.value) {
      await customersApi.update(editItem.value.id, { ...form.value, isActive: editItem.value.isActive })
      success.value = 'Mijoz yangilandi'
    } else {
      await customersApi.create(form.value)
      success.value = 'Mijoz qo\'shildi'
    }
    showForm.value = false; editItem.value = null
    await load()
  } catch (e) { err.value = errorMessage(e) }
}

async function doAdjust() {
  err.value = ''; success.value = ''
  try {
    await customersApi.adjustPoints(adjustItem.value.id, { points: Number(adjustPoints.value), comment: adjustComment.value || null })
    success.value = 'Ball yangilandi'
    adjustItem.value = null; adjustPoints.value = 0; adjustComment.value = ''
    await load()
  } catch (e) { err.value = errorMessage(e) }
}

async function remove(c) {
  if (!confirm(`"${c.name}" mijozni o'chirasizmi?`)) return
  err.value = ''
  try { await customersApi.remove(c.id); await load() }
  catch (e) { err.value = errorMessage(e) }
}

// Bonus tarixi modali
const historyItem = ref(null)
const history = ref([])
const historyLoading = ref(false)
const historyErr = ref('')
const TYPE_LABELS = {
  Earn: 'Cashback',
  Redeem: 'Sarflandi',
  Expire: "Muddati o'tdi",
  Adjust: 'Tuzatish',
  Signup: "Ro'yxatdan o'tish bonusi",
  Refund: 'Qaytarish'
}
const SOURCE_LABELS = { Store: "Do'kon", Online: 'Online' }

async function openHistory(c) {
  historyItem.value = c; history.value = []; historyErr.value = ''; historyLoading.value = true
  try {
    const r = await loyaltyApi.customerHistory(c.id, { page: 1, pageSize: 200 })
    // Massiv yoki PagedResult — ikkalasini ham qabul qilamiz
    history.value = Array.isArray(r) ? r : (r?.items || [])
  } catch (e) { historyErr.value = errorMessage(e) }
  finally { historyLoading.value = false }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Mijozlar</h1>
    <div style="display:flex; gap:12px; align-items:center">
      <input v-model="search" class="input" placeholder="Qidirish (ism, tel)..." style="width:200px" />
      <button class="btn" @click="openNew">+ Mijoz</button>
    </div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Mijoz formi -->
  <div v-if="showForm" class="card" style="margin-bottom:16px">
    <h3 style="margin-top:0">{{ editItem ? 'Tahrirlash' : 'Yangi mijoz' }}</h3>
    <div class="row">
      <div class="field"><label>Ism *</label><input v-model="form.name" class="input" required /></div>
      <div class="field"><label>Telefon</label><input v-model="form.phone" class="input" placeholder="+998..." /></div>
    </div>
    <div class="row">
      <div class="field"><label>Email</label><input v-model="form.email" class="input" type="email" /></div>
      <div class="field"><label>Chegirma (%)</label><input v-model="form.discountPercent" class="input" type="number" min="0" max="100" /></div>
    </div>
    <div class="field"><label>Manzil</label><input v-model="form.address" class="input" /></div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>
    <button class="btn" @click="save">Saqlash</button>
    <button class="btn btn-ghost" style="margin-left:8px" @click="showForm=false">Bekor</button>
  </div>

  <!-- Ball sozlash modali -->
  <div v-if="adjustItem" class="modal-bg" @click.self="adjustItem=null">
    <div class="modal-box">
      <h3 style="margin-top:0">{{ adjustItem.name }} — ball sozlash</h3>
      <p class="muted">Hozirgi ball: <strong>{{ fmt(adjustItem.loyaltyPoints) }}</strong></p>
      <div class="field"><label>Ball (musbat = qo'shish, manfiy = ayirish)</label><input v-model="adjustPoints" type="number" class="input" /></div>
      <div class="field"><label>Izoh</label><input v-model="adjustComment" class="input" /></div>
      <button class="btn" @click="doAdjust">Saqlash</button>
      <button class="btn btn-ghost" style="margin-left:8px" @click="adjustItem=null">Bekor</button>
    </div>
  </div>

  <!-- Bonus tarixi modali -->
  <div v-if="historyItem" class="modal-bg" @click.self="historyItem=null">
    <div class="modal-box" style="width:760px">
      <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:8px">
        <h3 style="margin:0">{{ historyItem.name }} — bonus tarixi</h3>
        <button class="btn btn-ghost btn-sm" @click="historyItem=null">✕</button>
      </div>
      <p class="muted" style="margin-top:0">Balans: <strong style="color:var(--success)">{{ fmt(historyItem.loyaltyPoints) }}</strong> ball (1 ball = 1 so'm)</p>
      <div v-if="historyErr" class="alert alert-error">{{ historyErr }}</div>
      <div style="max-height:60vh; overflow:auto">
        <table class="table">
          <thead><tr><th>Sana</th><th>Turi</th><th>Manba</th><th class="text-right">Ball</th><th class="text-right">Summa</th><th>Chek</th><th>Izoh</th></tr></thead>
          <tbody>
            <tr v-if="historyLoading"><td colspan="7" class="muted" style="text-align:center;padding:16px">Yuklanmoqda...</td></tr>
            <tr v-for="h in history" v-else :key="h.id">
              <td class="muted" style="white-space:nowrap">{{ fmtDate(h.createdAt) }}</td>
              <td>{{ TYPE_LABELS[h.type] || h.type }}</td>
              <td class="muted">{{ SOURCE_LABELS[h.source] || h.source || '—' }}</td>
              <td class="text-right" :style="{ color: Number(h.points) >= 0 ? 'var(--success)' : 'var(--danger)', fontWeight: 600 }">
                {{ Number(h.points) > 0 ? '+' : '' }}{{ fmt(h.points) }}
              </td>
              <td class="text-right">{{ h.amount != null ? fmt(h.amount) : '—' }}</td>
              <td class="muted">{{ h.receiptNumber || '—' }}</td>
              <td class="muted">{{ h.comment || '' }}</td>
            </tr>
            <tr v-if="!historyLoading && !history.length">
              <td colspan="7" class="muted" style="text-align:center;padding:16px">Tarix bo'sh</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div class="card">
    <table class="table">
      <thead><tr><th>Ism</th><th>Telefon</th><th class="text-right">Ball</th><th class="text-right">Chegirma</th><th>Sana</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in filtered" :key="c.id">
          <td><strong>{{ c.name }}</strong><span v-if="c.shopUserId" title="Mobil ilovadan foydalanadi" style="margin-left:6px">📱</span><span v-if="!c.isActive" class="badge badge-danger" style="margin-left:6px">Faol emas</span></td>
          <td class="muted">{{ c.phone || '—' }}</td>
          <td class="text-right" style="color:var(--success)">{{ fmt(c.loyaltyPoints) }}</td>
          <td class="text-right">{{ c.discountPercent }}%</td>
          <td class="muted">{{ fmtDate(c.createdAt) }}</td>
          <td class="text-right" style="white-space:nowrap">
            <button class="btn btn-ghost btn-sm" @click="openHistory(c)">Bonus tarixi</button>
            <button class="btn btn-ghost btn-sm" style="margin-left:4px" @click="adjustItem=c; adjustPoints=0; adjustComment=''">Ball</button>
            <button class="btn btn-ghost btn-sm" @click="openEdit(c)" style="margin:0 4px">✎</button>
            <button class="btn btn-ghost btn-sm" @click="remove(c)">✕</button>
          </td>
        </tr>
        <tr v-if="!filtered.length && !loading">
          <td colspan="6" class="muted" style="text-align:center;padding:24px">Mijoz topilmadi</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.modal-bg { position:fixed; inset:0; background:rgba(0,0,0,.5); display:flex; align-items:center; justify-content:center; z-index:1000 }
.modal-box { background:#fff; border-radius:8px; padding:24px; min-width:360px; max-width:95vw }
</style>
