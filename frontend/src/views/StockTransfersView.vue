<script setup>
import { ref, computed, onMounted } from 'vue'
import { stockTransfersApi, storesApi, newRequestId } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const transfers = ref([])
const branches = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const showForm = ref(false)

const form = ref({ fromBranchId: '', toBranchId: '', comment: '', items: [] })
const productLine = ref({ productId: '', name: '', quantity: 1 })
const saving = ref(false)
// Ikki marta yuborishdan himoya: bitta forma = bitta kalit
let requestId = newRequestId()

const statusLabel = (s) => ({ Pending: 'Kutilmoqda', Sent: 'Yuborildi', Received: 'Qabul qilindi', Cancelled: 'Bekor' })[s] || s
const statusClass = (s) => ({ Sent: 'badge-info', Received: 'badge-success', Cancelled: 'badge-danger', Pending: 'badge-warn' })[s] || ''

function fmt(n) { return Number(n || 0).toLocaleString() }
function fmtDate(d) { return d ? new Date(d).toLocaleString('uz-UZ') : '—' }

async function load() {
  loading.value = true; err.value = ''
  try {
    const [trs, br] = await Promise.all([stockTransfersApi.list(), storesApi.list()])
    transfers.value = trs
    branches.value = br
  } catch (e) { err.value = errorMessage(e) }
  finally { loading.value = false }
}

function addItem() {
  if (!productLine.value.productId.trim()) { err.value = 'Mahsulot ID kiriting'; return }
  form.value.items.push({ productId: productLine.value.productId.trim(), quantity: Number(productLine.value.quantity) })
  productLine.value = { productId: '', name: '', quantity: 1 }
}

function removeItem(i) { form.value.items.splice(i, 1) }

async function create() {
  err.value = ''; success.value = ''
  if (!form.value.fromBranchId || !form.value.toBranchId) { err.value = "Manba va maqsad filialni tanlang"; return }
  if (!form.value.items.length) { err.value = 'Kamida bitta mahsulot qo\'shing'; return }
  if (saving.value) return
  saving.value = true
  try {
    await stockTransfersApi.create({
      fromBranchId: form.value.fromBranchId,
      toBranchId: form.value.toBranchId,
      comment: form.value.comment || null,
      clientRequestId: requestId,
      items: form.value.items
    })
    success.value = 'Transfer yaratildi va yuborildi'
    requestId = newRequestId()
    showForm.value = false
    form.value = { fromBranchId: '', toBranchId: '', comment: '', items: [] }
    await load()
  } catch (e) { err.value = errorMessage(e) }
  finally { saving.value = false }
}

async function receive(id) {
  if (!confirm('Transferni qabul qilasizmi? Tovar omborda paydo bo\'ladi.')) return
  err.value = ''
  try { await stockTransfersApi.receive(id); await load() }
  catch (e) { err.value = errorMessage(e) }
}

async function cancel(id) {
  if (!confirm('Transferni bekor qilasizmi?')) return
  err.value = ''
  try { await stockTransfersApi.cancel(id); await load() }
  catch (e) { err.value = errorMessage(e) }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Filiallararo transfer</h1>
    <button class="btn" @click="showForm = !showForm">+ Yangi transfer</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div v-if="showForm" class="card" style="margin-bottom:16px">
    <h3 style="margin-top:0">Yangi transfer</h3>
    <div class="row">
      <div class="field">
        <label>Manba filial</label>
        <select v-model="form.fromBranchId" class="input">
          <option value="">— tanlang —</option>
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
      <div class="field">
        <label>Maqsad filial</label>
        <select v-model="form.toBranchId" class="input">
          <option value="">— tanlang —</option>
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
    </div>
    <div class="field"><label>Izoh</label><input v-model="form.comment" class="input" /></div>

    <h4>Mahsulotlar</h4>
    <div class="row">
      <div class="field"><label>Mahsulot ID (UUID)</label><input v-model="productLine.productId" class="input" placeholder="xxxxxxxx-..." /></div>
      <div class="field"><label>Miqdor</label><input v-model="productLine.quantity" type="number" min="0.001" step="0.001" class="input" /></div>
      <div class="field" style="align-self:flex-end"><button class="btn btn-sm" @click="addItem">+ Qo'sh</button></div>
    </div>

    <table v-if="form.items.length" class="table" style="margin-bottom:12px">
      <thead><tr><th>Mahsulot ID</th><th>Miqdor</th><th></th></tr></thead>
      <tbody>
        <tr v-for="(item, i) in form.items" :key="i">
          <td class="muted" style="font-size:12px">{{ item.productId }}</td>
          <td>{{ item.quantity }}</td>
          <td><button class="btn btn-icon danger" title="Olib tashlash" @click="removeItem(i)"><AppIcon name="x" :size="15" /></button></td>
        </tr>
      </tbody>
    </table>

    <button class="btn" :disabled="saving" @click="create">{{ saving ? '...' : 'Yuborish' }}</button>
    <button class="btn btn-ghost" style="margin-left:8px" @click="showForm=false">Bekor</button>
  </div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Raqam</th><th>Manba</th><th>Maqsad</th><th>Status</th>
          <th class="text-right">Summa</th><th>Sana</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="t in transfers" :key="t.id">
          <td><strong>{{ t.number }}</strong></td>
          <td>{{ t.fromBranchName || '—' }}</td>
          <td>{{ t.toBranchName || '—' }}</td>
          <td><span :class="['badge', statusClass(t.status)]">{{ statusLabel(t.status) }}</span></td>
          <td class="text-right">{{ fmt(t.totalCost) }}</td>
          <td class="muted">{{ fmtDate(t.createdAt) }}</td>
          <td class="text-right" style="white-space:nowrap">
            <button v-if="t.status === 'Sent'" class="btn btn-sm" @click="receive(t.id)" style="margin-right:4px">Qabul</button>
            <button v-if="t.status !== 'Received' && t.status !== 'Cancelled'" class="btn btn-ghost btn-sm" @click="cancel(t.id)">Bekor</button>
          </td>
        </tr>
        <tr v-if="!transfers.length && !loading">
          <td colspan="7" class="muted" style="text-align:center;padding:24px">Transfer yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
