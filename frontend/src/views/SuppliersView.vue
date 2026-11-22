<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { suppliersApi, paymentsApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import AppIcon from '../components/AppIcon.vue'

const suppliers = ref([])
const payments = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)
const search = ref('')

// Edit supplier modal
const showEditModal = ref(false)
const editForm = reactive({ id: null, name: '', phone: '', contactPerson: '', address: '' })

// Add supplier modal
const showAddModal = ref(false)
const addForm = reactive({ name: '', phone: '', contactPerson: '', address: '' })

// Pay supplier modal
const showPayModal = ref(false)
const payForm = reactive({ supplierId: '', supplierName: '', amount: 0, comment: '' })

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [s, p] = await Promise.all([suppliersApi.list(), paymentsApi.list()])
    suppliers.value = s
    payments.value = p
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

const filtered = computed(() => {
  if (!search.value.trim()) return suppliers.value
  const q = search.value.trim().toLowerCase()
  return suppliers.value.filter(
    (s) =>
      s.name.toLowerCase().includes(q) ||
      (s.phone || '').toLowerCase().includes(q) ||
      (s.contactPerson || '').toLowerCase().includes(q)
  )
})

function fmt(n) { return Number(n || 0).toLocaleString() }

function openAdd() {
  Object.assign(addForm, { name: '', phone: '', contactPerson: '', address: '' })
  showAddModal.value = true
}

async function submitAdd() {
  if (!addForm.name.trim()) return
  saving.value = true
  err.value = ''
  try {
    await suppliersApi.create({
      name: addForm.name.trim(),
      phone: addForm.phone.trim() || null,
      contactPerson: addForm.contactPerson.trim() || null,
      address: addForm.address.trim() || null
    })
    success.value = `"${addForm.name}" qo'shildi`
    showAddModal.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function openEdit(s) {
  Object.assign(editForm, {
    id: s.id,
    name: s.name,
    phone: s.phone || '',
    contactPerson: s.contactPerson || '',
    address: s.address || ''
  })
  showEditModal.value = true
}

async function submitEdit() {
  saving.value = true
  err.value = ''
  try {
    await suppliersApi.update(editForm.id, {
      name: editForm.name.trim(),
      phone: editForm.phone.trim() || null,
      contactPerson: editForm.contactPerson.trim() || null,
      address: editForm.address.trim() || null
    })
    success.value = 'Yangilandi'
    showEditModal.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function openPay(s) {
  Object.assign(payForm, { supplierId: s.id, supplierName: s.name, amount: 0, comment: '' })
  showPayModal.value = true
}

async function submitPay() {
  if (!Number(payForm.amount)) { err.value = 'Summani kiriting'; return }
  saving.value = true
  err.value = ''
  try {
    await paymentsApi.create({
      supplierId: payForm.supplierId,
      amount: Number(payForm.amount),
      comment: payForm.comment.trim() || null
    })
    success.value = `To'lov saqlandi`
    showPayModal.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function toggle(s) {
  err.value = ''
  try {
    if (s.isActive) await suppliersApi.deactivate(s.id)
    else await suppliersApi.activate(s.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Yetkazib beruvchilar</h1>
      <div class="muted" style="font-size:13px">{{ suppliers.length }} ta yetkazuvchi</div>
    </div>
    <button class="btn" @click="openAdd">+ Yetkazuvchi qo'shish</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Search -->
  <div class="card" style="margin-bottom:14px; padding:12px 16px">
    <div style="position:relative; max-width:400px">
      <input v-model="search" class="input" placeholder="Nomi yoki telefon bo'yicha qidirish" style="padding-left:36px; width:100%; box-sizing:border-box" />
      <svg style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:#aaa;pointer-events:none" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><line x1="16.5" y1="16.5" x2="22" y2="22"/></svg>
    </div>
  </div>

  <!-- Table -->
  <div class="card tbl-main">
    <table class="table">
      <thead>
        <tr>
          <th>Nomi</th>
          <th>Mas'ul shaxs</th>
          <th>Telefon</th>
          <th class="text-right">Qarz (balans)</th>
          <th>Holat</th>
          <th style="text-align:right">Amal</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td colspan="6" class="muted" style="text-align:center; padding:32px">Yuklanmoqda...</td>
        </tr>
        <tr v-for="s in filtered" :key="s.id">
          <td>
            <strong>{{ s.name }}</strong>
            <div v-if="s.address" class="muted" style="font-size:12px">{{ s.address }}</div>
          </td>
          <td class="muted">{{ s.contactPerson || '—' }}</td>
          <td class="muted">{{ s.phone || '—' }}</td>
          <td class="text-right">
            <span :style="{ color: Number(s.balance) < 0 ? 'var(--danger)' : 'inherit', fontWeight: 600 }">
              {{ fmt(s.balance) }}
            </span>
          </td>
          <td>
            <span class="pill" :class="s.isActive ? 'pill-green' : 'pill-gray'">
              {{ s.isActive ? 'Aktiv' : 'O\'chiq' }}
            </span>
          </td>
          <td class="text-right" style="white-space:nowrap">
            <div class="row-actions">
              <button class="btn btn-icon" title="To'lov qilish" @click="openPay(s)"><AppIcon name="wallet" :size="15" /></button>
              <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(s)"><AppIcon name="edit" :size="15" /></button>
              <button class="btn btn-icon" :class="{ danger: s.isActive }" :title="s.isActive ? 'O\'chirish' : 'Yoqish'" @click="toggle(s)">
                <AppIcon :name="s.isActive ? 'ban' : 'check'" :size="15" />
              </button>
            </div>
          </td>
        </tr>
        <tr v-if="!filtered.length && !loading">
          <td colspan="6" class="muted" style="text-align:center; padding:32px">Yetkazib beruvchi yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Add modal -->
  <div v-if="showAddModal" class="modal-overlay" @click.self="showAddModal = false">
    <div class="modal-box" style="max-width:480px">
      <div class="modal-head">
        <h3>Yangi yetkazib beruvchi</h3>
        <button class="modal-x" @click="showAddModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submitAdd">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="field"><label>Nomi *</label><input v-model="addForm.name" class="input" required autofocus /></div>
        <div class="field"><label>Mas'ul shaxs</label><input v-model="addForm.contactPerson" class="input" placeholder="To'liq ismi" /></div>
        <div class="field"><label>Telefon</label><input v-model="addForm.phone" class="input" placeholder="+998..." /></div>
        <div class="field"><label>Manzil</label><input v-model="addForm.address" class="input" /></div>
        <div style="display:flex; gap:8px; margin-top:4px">
          <button class="btn" style="flex:1" :disabled="saving">{{ saving ? '...' : 'Qo\'shish' }}</button>
          <button type="button" class="btn btn-ghost" @click="showAddModal = false">Bekor</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Edit modal -->
  <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
    <div class="modal-box" style="max-width:480px">
      <div class="modal-head">
        <h3>Tahrirlash</h3>
        <button class="modal-x" @click="showEditModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submitEdit">
        <div class="field"><label>Nomi *</label><input v-model="editForm.name" class="input" required /></div>
        <div class="field"><label>Mas'ul shaxs</label><input v-model="editForm.contactPerson" class="input" /></div>
        <div class="field"><label>Telefon</label><input v-model="editForm.phone" class="input" /></div>
        <div class="field"><label>Manzil</label><input v-model="editForm.address" class="input" /></div>
        <div style="display:flex; gap:8px; margin-top:4px">
          <button class="btn" style="flex:1" :disabled="saving">{{ saving ? '...' : 'Saqlash' }}</button>
          <button type="button" class="btn btn-ghost" @click="showEditModal = false">Bekor</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Pay modal -->
  <div v-if="showPayModal" class="modal-overlay" @click.self="showPayModal = false">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>To'lov — {{ payForm.supplierName }}</h3>
        <button class="modal-x" @click="showPayModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submitPay">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="field"><label>Summa *</label><input v-model="payForm.amount" type="number" min="0" class="input" required autofocus /></div>
        <div class="field"><label>Izoh</label><input v-model="payForm.comment" class="input" /></div>
        <div style="display:flex; gap:8px; margin-top:4px">
          <button class="btn" style="flex:1" :disabled="saving">{{ saving ? '...' : 'To\'lov qilish' }}</button>
          <button type="button" class="btn btn-ghost" @click="showPayModal = false">Bekor</button>
        </div>
      </form>
    </div>
  </div>
</template>
