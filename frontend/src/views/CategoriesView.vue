<script setup>
import { ref, onMounted } from 'vue'
import { categoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const categories = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')

const showModal = ref(false)
const editId = ref(null)
const formName = ref('')
const formNote = ref('')
const saving = ref(false)

const deleteTarget = ref(null)
const deleting = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    categories.value = await categoriesApi.list()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editId.value = null
  formName.value = ''
  formNote.value = ''
  error.value = ''
  showModal.value = true
}

function openEdit(cat) {
  editId.value = cat.id
  formName.value = cat.name
  formNote.value = cat.note || ''
  error.value = ''
  showModal.value = true
}

async function submit() {
  saving.value = true
  error.value = ''
  try {
    const payload = { name: formName.value.trim(), note: formNote.value.trim() || null }
    if (editId.value) {
      const updated = await categoriesApi.update(editId.value, payload)
      const idx = categories.value.findIndex((c) => c.id === updated.id)
      if (idx >= 0) categories.value[idx] = updated
      success.value = `"${updated.name}" yangilandi`
    } else {
      const created = await categoriesApi.create(payload)
      categories.value.push(created)
      success.value = `"${created.name}" qo'shildi`
    }
    showModal.value = false
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function confirmDelete(cat) {
  deleteTarget.value = cat
}

function cancelDelete() {
  if (deleting.value) return
  deleteTarget.value = null
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  error.value = ''
  try {
    await categoriesApi.remove(deleteTarget.value.id)
    categories.value = categories.value.filter((c) => c.id !== deleteTarget.value.id)
    deleteTarget.value = null
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Kategoriyalar</h1>
    <span class="muted">{{ categories.length }} ta</span>
    <button class="btn" style="margin-left:auto" @click="openCreate">+ Kategoriya qo'shish</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card">
    <div v-if="loading" class="muted" style="padding:32px; text-align:center">Yuklanmoqda...</div>
    <table v-else class="table">
      <thead>
        <tr>
          <th>Nomi</th>
          <th>Izoh</th>
          <th class="text-right">Mahsulotlar soni</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="!categories.length">
          <td colspan="4" class="muted" style="text-align:center; padding:32px">Kategoriya yo'q</td>
        </tr>
        <tr v-for="cat in categories" :key="cat.id">
          <td><strong>{{ cat.name }}</strong></td>
          <td class="muted" style="font-size:13px">{{ cat.note || '—' }}</td>
          <td class="text-right muted" style="font-size:13px">{{ cat.productCount }}</td>
          <td class="text-right" style="white-space:nowrap">
            <button class="btn btn-ghost btn-sm" @click="openEdit(cat)">✏️</button>
            <button
              class="btn btn-ghost btn-sm"
              :disabled="cat.productCount > 0"
              :title="cat.productCount > 0 ? 'Mahsulotlar mavjud — o\'chirib bo\'lmaydi' : 'O\'chirish'"
              @click="confirmDelete(cat)"
            >🗑️</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>{{ editId ? 'Kategoriyani tahrirlash' : 'Yangi kategoriya' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>
        <div class="field">
          <label>Nomi *</label>
          <input v-model="formName" class="input" required autofocus />
        </div>
        <div class="field">
          <label>Izoh (ixtiyoriy)</label>
          <input v-model="formNote" class="input" />
        </div>
        <button class="btn" style="width:100%; margin-top:8px" :disabled="saving || !formName.trim()">
          {{ saving ? 'Saqlanmoqda...' : (editId ? 'Saqlash' : 'Qo\'shish') }}
        </button>
      </form>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="cancelDelete">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Kategoriyani o'chirish</h3>
        <button class="modal-x" @click="cancelDelete">×</button>
      </div>
      <div class="modal-body">
        <div class="del-warn">⚠️</div>
        <p style="text-align:center; margin:0 0 6px">
          <strong>"{{ deleteTarget.name }}"</strong> kategoriyasini o'chirmoqchimisiz?
        </p>
        <div class="del-actions">
          <button class="btn btn-ghost" :disabled="deleting" @click="cancelDelete">Bekor qilish</button>
          <button class="btn btn-danger" :disabled="deleting" @click="doDelete">
            {{ deleting ? 'O\'chirilmoqda...' : 'O\'chirish' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
