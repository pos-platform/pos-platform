<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { rolesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// ------- Ma'lumotlar -------
const roles = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')

// Tanlangan rol (o'ng panel)
const selected = ref(null)
const editing = reactive({ name: '', permissions: [] })
const saving = ref(false)
const deleting = ref(false)

// Yangi rol formi
const showCreate = ref(false)
const creating = ref(false)
const createForm = reactive({ name: '', permissions: [] })

// ------- Permission katalogi (backend bilan mos) -------
const PERMISSION_CATEGORIES = [
  {
    label: 'Foydalanuvchilar',
    perms: [
      { code: 'users.view',     label: 'Ko\'rish' },
      { code: 'users.create',   label: 'Yaratish' },
      { code: 'users.edit',     label: 'Tahrirlash' },
      { code: 'users.activate', label: 'Faollashtirish/bloklash' }
    ]
  },
  {
    label: 'Rollar',
    perms: [
      { code: 'roles.view',   label: 'Ko\'rish' },
      { code: 'roles.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Filiallar',
    perms: [
      { code: 'branches.view',   label: 'Ko\'rish' },
      { code: 'branches.create', label: 'Yaratish' },
      { code: 'branches.edit',   label: 'Tahrirlash' },
      { code: 'branches.delete', label: 'O\'chirish' }
    ]
  },
  {
    label: 'Mahsulotlar',
    perms: [
      { code: 'products.view',   label: 'Ko\'rish' },
      { code: 'products.create', label: 'Yaratish' },
      { code: 'products.edit',   label: 'Tahrirlash' },
      { code: 'products.delete', label: 'O\'chirish' }
    ]
  },
  {
    label: 'Kategoriyalar',
    perms: [
      { code: 'categories.view',   label: 'Ko\'rish' },
      { code: 'categories.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Sotuvlar',
    perms: [
      { code: 'sales.view',   label: 'Ko\'rish' },
      { code: 'sales.create', label: 'Sotuv qilish' },
      { code: 'sales.refund', label: 'Qaytarish' }
    ]
  },
  {
    label: 'Hisobotlar',
    perms: [
      { code: 'reports.view', label: 'Ko\'rish' }
    ]
  },
  {
    label: 'Kassa',
    perms: [
      { code: 'cashbox.view',   label: 'Ko\'rish' },
      { code: 'cashbox.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Inventar',
    perms: [
      { code: 'inventory.view',   label: 'Ko\'rish' },
      { code: 'inventory.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Yetkazib beruvchilar',
    perms: [
      { code: 'suppliers.view',   label: 'Ko\'rish' },
      { code: 'suppliers.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Mijozlar',
    perms: [
      { code: 'customers.view',   label: 'Ko\'rish' },
      { code: 'customers.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Qarzdorlar',
    perms: [
      { code: 'debtors.view',   label: 'Ko\'rish' },
      { code: 'debtors.manage', label: 'Boshqarish' }
    ]
  },
  {
    label: 'Xarajatlar',
    perms: [
      { code: 'expenses.view',   label: 'Ko\'rish' },
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

// ------- API chaqiruvlar -------
async function load() {
  loading.value = true
  error.value = ''
  try {
    roles.value = await rolesApi.list()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function selectRole(role) {
  selected.value = role
  editing.name = role.name
  editing.permissions = [...role.permissions]
  success.value = ''
  error.value = ''
}

function toggleEditPerm(code) {
  const idx = editing.permissions.indexOf(code)
  if (idx === -1) editing.permissions.push(code)
  else editing.permissions.splice(idx, 1)
}

function toggleCreatePerm(code) {
  const idx = createForm.permissions.indexOf(code)
  if (idx === -1) createForm.permissions.push(code)
  else createForm.permissions.splice(idx, 1)
}

function setCategoryAll(cat, target, val) {
  for (const p of cat.perms) {
    const idx = target.indexOf(p.code)
    if (val && idx === -1) target.push(p.code)
    if (!val && idx !== -1) target.splice(idx, 1)
  }
}

function isCategoryAllChecked(cat, target) {
  return cat.perms.every((p) => target.includes(p.code))
}

async function saveEdit() {
  if (!selected.value) return
  saving.value = true
  error.value = ''
  try {
    const updated = await rolesApi.update(selected.value.id, {
      name: editing.name.trim(),
      permissions: editing.permissions
    })
    const idx = roles.value.findIndex((r) => r.id === updated.id)
    if (idx !== -1) roles.value[idx] = updated
    selected.value = updated
    success.value = `"${updated.name}" saqlandi`
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function deleteRole() {
  if (!selected.value || selected.value.isSystem) return
  if (!confirm(`"${selected.value.name}" rolini o'chirmoqchimisiz?`)) return
  deleting.value = true
  error.value = ''
  try {
    await rolesApi.remove(selected.value.id)
    roles.value = roles.value.filter((r) => r.id !== selected.value.id)
    selected.value = null
    success.value = 'Rol o\'chirildi'
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    deleting.value = false
  }
}

async function create() {
  creating.value = true
  error.value = ''
  try {
    const role = await rolesApi.create({
      name: createForm.name.trim(),
      permissions: createForm.permissions
    })
    roles.value.push(role)
    showCreate.value = false
    createForm.name = ''
    createForm.permissions = []
    success.value = `"${role.name}" yaratildi`
    selectRole(role)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    creating.value = false
  }
}

const editPermCount = computed(() => editing.permissions.length)
const totalPermCount = computed(() =>
  PERMISSION_CATEGORIES.reduce((s, c) => s + c.perms.length, 0)
)

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Rollar va ruxsatlar</h1>
    <span class="muted">{{ roles.length }} ta rol</span>
    <button class="btn" style="margin-left: auto" @click="showCreate = true">+ Yangi rol</button>
  </div>

  <div v-if="error" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="row" style="align-items: flex-start; gap: 16px">
    <!-- Chap: rollar ro'yxati -->
    <div class="card" style="flex: 0 0 220px; padding: 0; overflow: hidden">
      <div v-if="loading" style="padding: 16px; color: var(--text-muted)">Yuklanmoqda...</div>
      <div v-else>
        <button
          v-for="role in roles"
          :key="role.id"
          class="role-item"
          :class="{ active: selected?.id === role.id }"
          @click="selectRole(role)"
        >
          <span class="role-name">{{ role.name }}</span>
          <span v-if="role.isSystem" class="pill pill-blue" style="font-size: 10px">tizim</span>
        </button>
      </div>
    </div>

    <!-- O'ng: rol detay / tahrirlash -->
    <div v-if="selected" class="card" style="flex: 1">
      <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px">
        <div>
          <input
            v-model="editing.name"
            class="input"
            style="font-size:18px; font-weight:600; width:260px"
            :disabled="selected.isSystem"
          />
        </div>
        <div style="display:flex; gap:8px; align-items:center">
          <span class="muted" style="font-size:12px">{{ editPermCount }} / {{ totalPermCount }} ruxsat</span>
          <button
            v-if="!selected.isSystem"
            class="btn btn-ghost btn-sm"
            :disabled="deleting"
            @click="deleteRole"
          >O'chirish</button>
          <button class="btn btn-sm" :disabled="saving" @click="saveEdit">
            {{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}
          </button>
        </div>
      </div>

      <!-- Permission checkboxlar kategoriya bo'yicha -->
      <div class="perm-grid">
        <div v-for="cat in PERMISSION_CATEGORIES" :key="cat.label" class="perm-cat">
          <div class="perm-cat-head">
            <label class="perm-cat-label">
              <input
                type="checkbox"
                :checked="isCategoryAllChecked(cat, editing.permissions)"
                @change="setCategoryAll(cat, editing.permissions, $event.target.checked)"
              />
              {{ cat.label }}
            </label>
          </div>
          <label
            v-for="p in cat.perms"
            :key="p.code"
            class="perm-row"
          >
            <input
              type="checkbox"
              :checked="editing.permissions.includes(p.code)"
              @change="toggleEditPerm(p.code)"
            />
            {{ p.label }}
          </label>
        </div>
      </div>
    </div>

    <div v-else-if="!loading" class="card" style="flex: 1; color: var(--text-muted); text-align:center; padding: 48px">
      Rolni tanlang yoki yangi yarating
    </div>
  </div>

  <!-- Yangi rol modal -->
  <div v-if="showCreate" class="modal-overlay" @click.self="showCreate = false">
    <div class="modal" style="width: 760px; max-height: 90vh; overflow-y: auto">
      <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px">
        <h3 style="margin:0">Yangi rol</h3>
        <button class="btn btn-ghost btn-sm" @click="showCreate = false">✕</button>
      </div>

      <div class="field" style="margin-bottom:16px">
        <label>Rol nomi</label>
        <input v-model="createForm.name" class="input" style="max-width:300px" placeholder="Masalan: Operator" required />
      </div>

      <div class="perm-grid">
        <div v-for="cat in PERMISSION_CATEGORIES" :key="cat.label" class="perm-cat">
          <div class="perm-cat-head">
            <label class="perm-cat-label">
              <input
                type="checkbox"
                :checked="isCategoryAllChecked(cat, createForm.permissions)"
                @change="setCategoryAll(cat, createForm.permissions, $event.target.checked)"
              />
              {{ cat.label }}
            </label>
          </div>
          <label
            v-for="p in cat.perms"
            :key="p.code"
            class="perm-row"
          >
            <input
              type="checkbox"
              :checked="createForm.permissions.includes(p.code)"
              @change="toggleCreatePerm(p.code)"
            />
            {{ p.label }}
          </label>
        </div>
      </div>

      <div style="display:flex; gap:8px; margin-top:20px; justify-content:flex-end">
        <button class="btn btn-ghost" @click="showCreate = false">Bekor</button>
        <button class="btn" :disabled="creating || !createForm.name.trim()" @click="create">
          {{ creating ? 'Yaratilmoqda...' : 'Yaratish' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.role-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 16px;
  width: 100%;
  text-align: left;
  background: transparent;
  border: none;
  border-bottom: 1px solid var(--border);
  cursor: pointer;
  color: var(--text);
  transition: background 0.15s;
}
.role-item:hover { background: var(--surface); }
.role-item.active { background: var(--primary-light, #e8f0fe); font-weight: 600; }
.role-name { flex: 1; }

.perm-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 12px;
}
.perm-cat {
  border: 1px solid var(--border);
  border-radius: 8px;
  overflow: hidden;
}
.perm-cat-head {
  background: var(--surface);
  padding: 8px 12px;
  border-bottom: 1px solid var(--border);
}
.perm-cat-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}
.perm-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  font-size: 13px;
  cursor: pointer;
}
.perm-row:hover { background: var(--surface); }

.pill-blue { background: #dbeafe; color: #1d4ed8; }
</style>
