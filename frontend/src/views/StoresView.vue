<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { storesApi, usersApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const router = useRouter()

const stores = ref([])
const allUsers = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const showStoreForm = ref(false)
const storeForm = reactive({ name: '', address: '', businessType: 'Store' })
const cashboxName = reactive({})

// Foydalanuvchi biriktirish
const assignModal = ref(null) // { store }
const assignUserId = ref('')

// Do'konga biriktirilmagan foydalanuvchilar
const unassignedUsers = computed(() => {
  if (!assignModal.value) return []
  const assignedIds = new Set(assignModal.value.store.users.map((u) => u.id))
  return allUsers.value.filter((u) => !assignedIds.has(u.id))
})

async function load() {
  loading.value = true
  err.value = ''
  try {
    ;[stores.value, allUsers.value] = await Promise.all([storesApi.list(), usersApi.list()])
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function createStore() {
  err.value = ''
  success.value = ''
  try {
    await storesApi.createStore({ name: storeForm.name.trim(), address: storeForm.address.trim() || null, businessType: storeForm.businessType })
    success.value = `"${storeForm.name}" filiali qo'shildi`
    storeForm.name = ''
    storeForm.address = ''
    storeForm.businessType = 'Store'
    showStoreForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function toggleStore(s) {
  err.value = ''
  try {
    if (s.isActive) await storesApi.deactivateStore(s.id)
    else await storesApi.activateStore(s.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function addCashbox(s) {
  const name = (cashboxName[s.id] || '').trim()
  if (!name) return
  err.value = ''
  try {
    await storesApi.addCashbox({ branchId: s.id, name })
    cashboxName[s.id] = ''
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function toggleCashbox(c) {
  err.value = ''
  try {
    if (c.isActive) await storesApi.deactivateCashbox(c.id)
    else await storesApi.activateCashbox(c.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

function openAssignModal(s) {
  assignModal.value = { store: s }
  assignUserId.value = ''
}

async function assignUser() {
  if (!assignUserId.value || !assignModal.value) return
  err.value = ''
  try {
    await storesApi.assignUser(assignModal.value.store.id, assignUserId.value)
    success.value = 'Foydalanuvchi do\'konga biriktirildi'
    assignUserId.value = ''
    await load()
    // Modalni yangilash
    const updated = stores.value.find((s) => s.id === assignModal.value.store.id)
    if (updated) assignModal.value = { store: updated }
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function removeUser(store, user) {
  err.value = ''
  try {
    await storesApi.removeUser(store.id, user.id)
    success.value = `"${user.username}" do'kondan chiqarildi`
    await load()
    const updated = stores.value.find((s) => s.id === store.id)
    if (updated && assignModal.value) assignModal.value = { store: updated }
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Do'konlar</h1>
    <button class="btn" @click="showStoreForm = !showStoreForm">+ Do'kon qo'shish</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <!-- Yangi do'kon formasi -->
  <form v-if="showStoreForm" class="card" style="margin-bottom: 16px" @submit.prevent="createStore">
    <h3 style="margin-top: 0">Yangi do'kon</h3>
    <div class="row">
      <div class="field">
        <label>Nomi</label>
        <input v-model="storeForm.name" class="input" required />
      </div>
      <div class="field">
        <label>Manzil</label>
        <input v-model="storeForm.address" class="input" />
      </div>
      <div class="field">
        <label>Faoliyat turi</label>
        <select v-model="storeForm.businessType" class="input">
          <option value="Store">🏪 Do'kon</option>
          <option value="Restaurant">🍽️ Restoran</option>
        </select>
      </div>
    </div>
    <button class="btn">Saqlash</button>
  </form>

  <!-- Do'konlar -->
  <div v-for="s in stores" :key="s.id" class="card" style="margin-bottom: 14px">
    <div class="row" style="align-items: center; justify-content: space-between">
      <div>
        <h3 style="margin: 0">
          {{ s.name }}
          <span class="pill" :class="s.businessType === 'Restaurant' ? 'pill-green' : 'pill-gray'" style="font-size:11px; margin-left:6px">
            {{ s.businessType === 'Restaurant' ? '🍽️ Restoran' : '🏪 Do\'kon' }}
          </span>
        </h3>
        <span class="muted" v-if="s.address">{{ s.address }}</span>
      </div>
      <div style="display: flex; align-items: center; gap: 12px">
        <span class="muted">Balans: <strong>{{ Number(s.balance).toLocaleString() }}</strong></span>
        <span class="pill" :class="s.isActive ? 'pill-green' : 'pill-gray'">
          {{ s.isActive ? 'Aktiv' : 'O\'chiq' }}
        </span>
        <button class="btn btn-ghost btn-sm" @click="toggleStore(s)">
          {{ s.isActive ? 'O\'chirish' : 'Yoqish' }}
        </button>
      </div>
    </div>

    <hr style="border: none; border-top: 1px solid var(--border); margin: 12px 0" />

    <!-- Kassirlar -->
    <div style="display: flex; gap: 24px; align-items: flex-start; flex-wrap: wrap">
      <div style="flex: 1; min-width: 260px">
        <div class="section-label">Xodimlar ({{ s.users.length }})</div>
        <table class="table" style="margin-bottom: 8px">
          <tbody>
            <tr v-for="u in s.users" :key="u.id">
              <td>
                <strong>{{ u.username }}</strong>
              </td>
              <td class="muted">{{ u.roleName || '—' }}</td>
              <td>
                <span class="pill" :class="u.isActive ? 'pill-green' : 'pill-gray'" style="font-size:11px">
                  {{ u.isActive ? 'Faol' : 'Bloklangan' }}
                </span>
              </td>
              <td class="text-right">
                <button class="btn btn-ghost btn-sm" style="color:var(--danger)" @click="removeUser(s, u)">Chiqarish</button>
              </td>
            </tr>
            <tr v-if="!s.users.length">
              <td colspan="4" class="muted">Xodim yo'q</td>
            </tr>
          </tbody>
        </table>
        <button class="btn btn-ghost btn-sm" @click="openAssignModal(s)">+ Xodim biriktirish</button>
      </div>

      <!-- Kassalar -->
      <div style="flex: 1; min-width: 260px">
        <div class="section-label">Kassalar ({{ s.cashboxes.length }})</div>
        <table class="table" style="margin-bottom: 8px">
          <tbody>
            <tr v-for="c in s.cashboxes" :key="c.id">
              <td>{{ c.name }}</td>
              <td class="text-right muted">{{ Number(c.balance).toLocaleString() }}</td>
              <td>
                <span class="pill" :class="c.isActive ? 'pill-green' : 'pill-gray'" style="font-size:11px">
                  {{ c.isActive ? 'Aktiv' : 'O\'chiq' }}
                </span>
              </td>
              <td class="text-right" style="white-space:nowrap">
                <button class="btn btn-ghost btn-sm" @click="router.push({ name: 'cashbox', params: { id: c.id } })">Ko'rish</button>
                <button class="btn btn-ghost btn-sm" @click="toggleCashbox(c)">
                  {{ c.isActive ? 'O\'chirish' : 'Yoqish' }}
                </button>
              </td>
            </tr>
            <tr v-if="!s.cashboxes.length">
              <td colspan="4" class="muted">Kassa yo'q</td>
            </tr>
          </tbody>
        </table>
        <div class="row" style="max-width: 320px">
          <input v-model="cashboxName[s.id]" class="input" placeholder="Yangi kassa nomi" style="flex: 1" />
          <button class="btn btn-ghost" @click="addCashbox(s)">+ Kassa</button>
        </div>
      </div>
    </div>
  </div>

  <p v-if="!stores.length && !loading" class="muted">Do'kon yo'q</p>

  <!-- Xodim biriktirish modali -->
  <div v-if="assignModal" class="modal-overlay" @click.self="assignModal = null">
    <div class="modal" style="width: 380px">
      <h3 style="margin-top: 0">"{{ assignModal.store.name }}" — xodim biriktirish</h3>

      <div v-if="unassignedUsers.length === 0" class="muted" style="margin-bottom: 16px">
        Barcha foydalanuvchilar allaqachon biror do'konga biriktirilgan yoki mavjud emas.
      </div>
      <div v-else class="field">
        <label>Foydalanuvchi</label>
        <select v-model="assignUserId" class="select">
          <option value="">— Tanlang —</option>
          <option v-for="u in unassignedUsers" :key="u.id" :value="u.id">
            {{ u.username }} ({{ u.appRoleName || u.role }})
          </option>
        </select>
      </div>

      <div style="display:flex; gap:8px; margin-top:16px; justify-content:flex-end">
        <button class="btn btn-ghost" @click="assignModal = null">Yopish</button>
        <button class="btn" :disabled="!assignUserId" @click="assignUser">Biriktirish</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.section-label {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 6px;
}
</style>
