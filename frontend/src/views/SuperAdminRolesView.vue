<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { roleTemplatesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const templates = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)
const deleting = ref(false)

const selected = ref(null)
const editing = reactive({ name: '', permissions: [] })

const showCreate = ref(false)
const creating = ref(false)
const createForm = reactive({ name: '', permissions: [] })

const PERMISSION_CATEGORIES = [
  {
    label: 'Foydalanuvchilar',
    perms: [
      { code: 'users.view',     label: "Ko'rish" },
      { code: 'users.create',   label: 'Yaratish' },
      { code: 'users.edit',     label: 'Tahrirlash' },
      { code: 'users.activate', label: 'Faollashtirish/bloklash' }
    ]
  },
  {
    label: 'Rollar',
    perms: [
      { code: 'roles.view',   label: "Ko'rish" },
      { code: 'roles.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Filiallar',
    perms: [
      { code: 'branches.view',   label: "Ko'rish" },
      { code: 'branches.create', label: 'Yaratish' },
      { code: 'branches.edit',   label: 'Tahrirlash' },
      { code: 'branches.delete', label: "O'chirish" }
    ]
  },
  {
    label: 'Mahsulotlar',
    perms: [
      { code: 'products.view',   label: "Ko'rish" },
      { code: 'products.create', label: 'Yaratish' },
      { code: 'products.edit',   label: 'Tahrirlash' },
      { code: 'products.delete', label: "O'chirish" }
    ]
  },
  {
    label: 'Kategoriyalar',
    perms: [
      { code: 'categories.view',   label: "Ko'rish" },
      { code: 'categories.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Sotuvlar',
    perms: [
      { code: 'sales.view',   label: "Ko'rish" },
      { code: 'sales.create', label: 'Sotuv qilish' },
      { code: 'sales.refund', label: 'Qaytarish' }
    ]
  },
  {
    label: 'Hisobotlar',
    perms: [
      { code: 'reports.view', label: "Ko'rish" }
    ]
  },
  {
    label: 'Kassa',
    perms: [
      { code: 'cashbox.view',   label: "Ko'rish" },
      { code: 'cashbox.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Inventar',
    perms: [
      { code: 'inventory.view',   label: "Ko'rish" },
      { code: 'inventory.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Yetkazib beruvchilar',
    perms: [
      { code: 'suppliers.view',   label: "Ko'rish" },
      { code: 'suppliers.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Mijozlar',
    perms: [
      { code: 'customers.view',   label: "Ko'rish" },
      { code: 'customers.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Qarzdorlar',
    perms: [
      { code: 'debtors.view',   label: "Ko'rish" },
      { code: 'debtors.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Xarajatlar',
    perms: [
      { code: 'expenses.view',   label: "Ko'rish" },
      { code: 'expenses.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Sozlamalar',
    perms: [
      { code: 'settings.manage', label: 'Boshqarish' }
    ]
  }
]

const allCodes = computed(() => PERMISSION_CATEGORIES.flatMap(c => c.perms.map(p => p.code)))

async function load() {
  loading.value = true
  err.value = ''
  try {
    templates.value = await roleTemplatesApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function selectTemplate(t) {
  selected.value = t
  editing.name = t.name
  editing.permissions = [...t.permissions]
  err.value = ''
  success.value = ''
}

function togglePerm(target, code) {
  const idx = target.indexOf(code)
  if (idx === -1) target.push(code)
  else target.splice(idx, 1)
}

function setCategoryAll(cat, target, val) {
  for (const p of cat.perms) {
    const idx = target.indexOf(p.code)
    if (val && idx === -1) target.push(p.code)
    if (!val && idx !== -1) target.splice(idx, 1)
  }
}

function isCategoryAll(cat, target) {
  return cat.perms.every(p => target.includes(p.code))
}

function selectAll(target) {
  target.splice(0, target.length, ...allCodes.value)
}

function clearAll(target) {
  target.splice(0, target.length)
}

function notify(msg) { success.value = msg; setTimeout(() => { success.value = '' }, 2500) }

async function save() {
  if (!selected.value) return
  saving.value = true
  err.value = ''
  try {
    const updated = await roleTemplatesApi.update(selected.value.id, editing.permissions)
    const idx = templates.value.findIndex(t => t.id === updated.id)
    if (idx !== -1) templates.value[idx] = updated
    selected.value = updated
    notify(`"${updated.name}" saqlandi`)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function deleteTemplate() {
  if (!selected.value || selected.value.isSystem) return
  if (!confirm(`"${selected.value.name}" shablonini o'chirmoqchimisiz?`)) return
  deleting.value = true
  err.value = ''
  try {
    await roleTemplatesApi.remove(selected.value.id)
    templates.value = templates.value.filter(t => t.id !== selected.value.id)
    selected.value = null
    notify("Shablon o'chirildi")
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    deleting.value = false
  }
}

async function create() {
  creating.value = true
  err.value = ''
  try {
    const t = await roleTemplatesApi.create(createForm.name.trim(), createForm.permissions)
    templates.value.push(t)
    selectTemplate(t)
    Object.assign(createForm, { name: '', permissions: [] })
    showCreate.value = false
    notify(`"${t.name}" yaratildi`)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    creating.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Rollar (shablon)</h1>
    <button class="btn" @click="showCreate = true">+ Yangi rol</button>
  </div>
  <p class="page-desc muted">Har bir yangi tashkilot yaratilganda shu shablonlar asosida rollar avtomatik tuziladi.</p>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="layout">
    <!-- Chap: shablon ro'yxati -->
    <div class="card list-panel">
      <p v-if="loading" class="muted" style="text-align:center;padding:16px">Yuklanmoqda...</p>
      <div v-for="t in templates" :key="t.id"
           class="template-item" :class="{ active: selected && selected.id === t.id }"
           @click="selectTemplate(t)">
        <div class="template-name">
          {{ t.name }}
          <span v-if="t.isSystem" class="pill pill-blue" style="font-size:11px">tizim</span>
        </div>
        <div class="muted" style="font-size:12px">{{ t.permissions.length }} ruxsat</div>
      </div>
      <p v-if="!templates.length && !loading" class="muted" style="text-align:center;padding:16px">Shablon yo'q</p>
    </div>

    <!-- O'ng: tahrirlash paneli -->
    <div v-if="selected" class="card edit-panel">
      <div class="edit-head">
        <div>
          <h3 style="margin:0">{{ selected.name }}</h3>
          <span v-if="selected.isSystem" class="muted" style="font-size:12px">Tizim shabloni — nomini o'zgartirib bo'lmaydi</span>
        </div>
        <div style="display:flex;gap:6px">
          <button v-if="!selected.isSystem" class="btn btn-danger btn-sm" :disabled="deleting" @click="deleteTemplate">
            O'chirish
          </button>
          <button class="btn btn-sm" :disabled="saving" @click="save">
            {{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}
          </button>
        </div>
      </div>

      <div class="perm-toolbar">
        <span class="muted" style="font-size:13px">{{ editing.permissions.length }} / {{ allCodes.length }} ruxsat tanlangan</span>
        <button class="btn btn-ghost btn-xs" @click="selectAll(editing.permissions)">Barchasini tanlash</button>
        <button class="btn btn-ghost btn-xs" @click="clearAll(editing.permissions)">Tozalash</button>
      </div>

      <div class="perm-grid">
        <div v-for="cat in PERMISSION_CATEGORIES" :key="cat.label" class="perm-cat">
          <div class="perm-cat-head">
            <label class="perm-check">
              <input type="checkbox"
                     :checked="isCategoryAll(cat, editing.permissions)"
                     @change="setCategoryAll(cat, editing.permissions, $event.target.checked)" />
              <strong>{{ cat.label }}</strong>
            </label>
          </div>
          <label v-for="p in cat.perms" :key="p.code" class="perm-check perm-item">
            <input type="checkbox"
                   :checked="editing.permissions.includes(p.code)"
                   @change="togglePerm(editing.permissions, p.code)" />
            {{ p.label }}
          </label>
        </div>
      </div>
    </div>

    <div v-else class="card edit-panel empty-panel">
      <div style="font-size:36px">👈</div>
      <p class="muted" style="margin:8px 0 0;text-align:center">Tahrirlash uchun<br />shablonni tanlang</p>
    </div>
  </div>

  <!-- Yangi shablon modali -->
  <div v-if="showCreate" class="modal-overlay" @click.self="showCreate = false">
    <div class="modal-box" style="max-width:680px">
      <div class="modal-head">
        <h3>Yangi rol shabloni</h3>
        <button class="modal-x" @click="showCreate = false">×</button>
      </div>
      <div class="modal-body">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="field" style="margin-bottom:14px">
          <label>Rol nomi *</label>
          <input v-model="createForm.name" class="input" required placeholder="Masalan: Ofitsiant, Oshpaz..." />
        </div>

        <div class="perm-toolbar">
          <span class="muted" style="font-size:13px">{{ createForm.permissions.length }} ruxsat tanlangan</span>
          <button type="button" class="btn btn-ghost btn-xs" @click="selectAll(createForm.permissions)">Barchasini tanlash</button>
          <button type="button" class="btn btn-ghost btn-xs" @click="clearAll(createForm.permissions)">Tozalash</button>
        </div>

        <div class="perm-grid">
          <div v-for="cat in PERMISSION_CATEGORIES" :key="cat.label" class="perm-cat">
            <div class="perm-cat-head">
              <label class="perm-check">
                <input type="checkbox"
                       :checked="isCategoryAll(cat, createForm.permissions)"
                       @change="setCategoryAll(cat, createForm.permissions, $event.target.checked)" />
                <strong>{{ cat.label }}</strong>
              </label>
            </div>
            <label v-for="p in cat.perms" :key="p.code" class="perm-check perm-item">
              <input type="checkbox"
                     :checked="createForm.permissions.includes(p.code)"
                     @change="togglePerm(createForm.permissions, p.code)" />
              {{ p.label }}
            </label>
          </div>
        </div>

        <button class="btn" style="width:100%;margin-top:16px" :disabled="creating || !createForm.name.trim()" @click="create">
          {{ creating ? 'Yaratilmoqda...' : 'Yaratish' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-desc { margin: -8px 0 18px; }

.layout { display: flex; gap: 16px; align-items: flex-start; }
.list-panel { width: 220px; flex-shrink: 0; padding: 8px 0; }
.edit-panel { flex: 1; }

.template-item { padding: 10px 16px; cursor: pointer; border-radius: 6px; transition: background 0.15s; }
.template-item:hover { background: #f8fafc; }
.template-item.active { background: #eff6ff; }
.template-name { font-weight: 600; font-size: 14px; display: flex; align-items: center; gap: 6px; }

.edit-head { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 14px; }
.perm-toolbar { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }

.perm-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 12px; }
.perm-cat { background: #f8fafc; border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px; }
.perm-cat-head { margin-bottom: 8px; }
.perm-check { display: flex; align-items: center; gap: 7px; font-size: 13px; cursor: pointer; line-height: 1.4; }
.perm-item { margin-top: 4px; padding-left: 4px; }
.perm-check input { width: 14px; height: 14px; cursor: pointer; flex-shrink: 0; }

.pill-blue { background: #dbeafe; color: #1d4ed8; }
.btn-xs { padding: 4px 8px; font-size: 12px; }

.empty-panel { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 300px; }
</style>
