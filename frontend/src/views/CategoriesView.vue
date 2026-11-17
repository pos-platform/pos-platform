<script setup>
import { ref, computed, onMounted } from 'vue'
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
const formParentId = ref('')
const formSortOrder = ref(0)
const formImageUrl = ref('')
const formShowOnline = ref(true)

// Daraxt tartibida: asosiy bo'lim, ostida ichki bo'limlari.
const rows = computed(() => {
  const all = categories.value
  const ids = new Set(all.map((c) => c.id))
  const bySort = (a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name)
  const roots = all.filter((c) => !c.parentId || !ids.has(c.parentId)).sort(bySort)
  return roots.flatMap((r) => [r, ...all.filter((c) => c.parentId === r.id).sort(bySort)])
})
// Ota bo'lim faqat asosiy bo'lim bo'ladi; ichki bo'limi bor bo'limni boshqasiga ko'chirib bo'lmaydi.
const parentOptions = computed(() => categories.value.filter((c) => !c.parentId && c.id !== editId.value))
const editHasChildren = computed(() => !!editId.value && categories.value.some((c) => c.parentId === editId.value))
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
  formParentId.value = ''
  formSortOrder.value = 0
  formImageUrl.value = ''
  formShowOnline.value = true
  error.value = ''
  showModal.value = true
}

function openEdit(cat) {
  editId.value = cat.id
  formName.value = cat.name
  formNote.value = cat.note || ''
  formParentId.value = cat.parentId || ''
  formSortOrder.value = cat.sortOrder ?? 0
  formImageUrl.value = cat.imageUrl || ''
  formShowOnline.value = cat.showOnline !== false
  error.value = ''
  showModal.value = true
}

async function submit() {
  saving.value = true
  error.value = ''
  try {
    const payload = {
      name: formName.value.trim(),
      note: formNote.value.trim() || null,
      parentId: formParentId.value || null,
      sortOrder: Number(formSortOrder.value) || 0,
      imageUrl: formImageUrl.value.trim() || null,
      showOnline: formShowOnline.value
    }
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
          <th>Ilovada</th>
          <th class="text-right">Mahsulotlar soni</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="!categories.length">
          <td colspan="5" class="muted" style="text-align:center; padding:32px">Kategoriya yo'q</td>
        </tr>
        <tr v-for="cat in rows" :key="cat.id">
          <td v-if="cat.parentId" style="padding-left:32px">↳ {{ cat.name }}</td>
          <td v-else><strong>{{ cat.name }}</strong></td>
          <td class="muted" style="font-size:13px">{{ cat.note || '—' }}</td>
          <td style="font-size:13px">{{ cat.showOnline === false ? '🚫 yashirin' : '✓' }}</td>
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
        <div class="field">
          <label>Ota bo'lim</label>
          <select v-model="formParentId" class="input" :disabled="editHasChildren">
            <option value="">— Asosiy bo'lim —</option>
            <option v-for="p in parentOptions" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
          <small v-if="editHasChildren" class="muted">Ichki bo'limlari bor — asosiy bo'lim bo'lib qoladi.</small>
        </div>
        <div class="field">
          <label>Tartib (kichigi oldin)</label>
          <input v-model.number="formSortOrder" type="number" class="input" />
        </div>
        <div class="field">
          <label>Rasm URL (ixtiyoriy)</label>
          <input v-model="formImageUrl" class="input" placeholder="/uploads/..." />
        </div>
        <label style="display:flex; gap:8px; align-items:center; margin:8px 0">
          <input v-model="formShowOnline" type="checkbox" /> Mijoz ilovasida ko'rsatish
        </label>
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
