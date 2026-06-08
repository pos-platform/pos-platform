<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { organizationsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const orgs = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const selected = ref(null)
const busy = ref(false)
const orgRoles = ref([]) // org rollari — admin user uchun dropdown

// Yangi tashkilot modali
const showOrgForm = ref(false)
const orgForm = reactive({ name: '', phone: '', note: '', adminUsername: '', adminPassword: '' })

// Admin + Desktop login qo'shish modali
const showAdminForm = ref(false)
const createDesktop = ref(true)
const adminForm = reactive({ username: '', password: '' })
const desktopForm = reactive({ username: '', password: '', role: 'Cashier', licenseMonths: 0 })

// Faqat desktop login qo'shish modali
const showDesktopForm = ref(false)
const desktopOnlyForm = reactive({ username: '', password: '', role: 'Cashier', licenseMonths: 0 })

const roleLabel = { Admin: 'Admin', Manager: 'Menejer', Cashier: 'Kassir', SuperAdmin: 'SuperAdmin' }

const totalUsers = computed(() => orgs.value.reduce((s, o) => s + o.userCount, 0))
const totalLicensed = computed(() => orgs.value.reduce((s, o) => s + o.licensedCount, 0))

const adminUsers = computed(() => selected.value?.users.filter(u => u.role === 'Admin') ?? [])
const desktopUsers = computed(() => selected.value?.users.filter(u => u.role !== 'Admin') ?? [])

function fmtDate(d) { return d ? new Date(d).toLocaleDateString('uz-UZ') : '—' }
function notify(msg) { success.value = msg; setTimeout(() => { success.value = '' }, 2500) }

async function load() {
  loading.value = true
  err.value = ''
  try {
    orgs.value = await organizationsApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function openOrg(o) {
  err.value = ''
  orgRoles.value = []
  try {
    selected.value = await organizationsApi.get(o.id)
    orgRoles.value = await organizationsApi.getRoles(o.id)
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function refreshSelected() {
  if (selected.value) {
    selected.value = await organizationsApi.get(selected.value.id)
    orgRoles.value = await organizationsApi.getRoles(selected.value.id)
  }
  await load()
}

async function createOrg() {
  err.value = ''
  try {
    await organizationsApi.create({
      name: orgForm.name.trim(),
      phone: orgForm.phone.trim() || null,
      note: orgForm.note.trim() || null,
      adminUsername: orgForm.adminUsername.trim(),
      adminPassword: orgForm.adminPassword,
      licenseMonths: 0
    })
    notify(`"${orgForm.name}" tashkiloti yaratildi`)
    Object.assign(orgForm, { name: '', phone: '', note: '', adminUsername: '', adminPassword: '' })
    showOrgForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function toggleOrg(o) {
  busy.value = true
  try {
    if (o.isActive) await organizationsApi.deactivate(o.id)
    else await organizationsApi.activate(o.id)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function createAdminWithDesktop() {
  err.value = ''
  busy.value = true
  try {
    await organizationsApi.createUser(selected.value.id, {
      username: adminForm.username.trim(),
      password: adminForm.password,
      role: 'Admin',
      licenseMonths: 0
    })
    if (createDesktop.value && desktopForm.username.trim()) {
      await organizationsApi.createUser(selected.value.id, {
        username: desktopForm.username.trim(),
        password: desktopForm.password,
        role: desktopForm.role,
        licenseMonths: Number(desktopForm.licenseMonths) || 0
      })
    }
    notify('Login(lar) qo\'shildi')
    Object.assign(adminForm, { username: '', password: '' })
    Object.assign(desktopForm, { username: '', password: '', role: 'Cashier', licenseMonths: 0 })
    createDesktop.value = true
    showAdminForm.value = false
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function createDesktopOnly() {
  err.value = ''
  busy.value = true
  try {
    await organizationsApi.createUser(selected.value.id, {
      username: desktopOnlyForm.username.trim(),
      password: desktopOnlyForm.password,
      role: desktopOnlyForm.role,
      licenseMonths: Number(desktopOnlyForm.licenseMonths) || 0
    })
    notify('Desktop login qo\'shildi')
    Object.assign(desktopOnlyForm, { username: '', password: '', role: 'Cashier', licenseMonths: 0 })
    showDesktopForm.value = false
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function extend(u, months) {
  busy.value = true
  err.value = ''
  try {
    await organizationsApi.extendLicense(u.id, months)
    notify(`${u.username}: +${months} oy faollashtirildi`)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function revoke(u) {
  busy.value = true
  err.value = ''
  try {
    await organizationsApi.revokeLicense(u.id)
    notify(`${u.username}: obuna bekor qilindi`)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function toggleUser(u) {
  busy.value = true
  try {
    if (u.isActive) await organizationsApi.deactivateUser(u.id)
    else await organizationsApi.activateUser(u.id)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function assignRole(u, roleId) {
  busy.value = true
  try {
    await organizationsApi.assignAppRole(u.id, roleId || null)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

function statusOf(u) {
  if (!u.isActive) return { text: 'Passiv (kira olmaydi)', cls: 'st-blocked' }
  if (u.isLicensed) return { text: `To'langan · ${u.daysLeft} kun qoldi`, cls: u.daysLeft <= 5 ? 'st-soon' : 'st-ok' }
  if (u.licensedUntil) return { text: "To'lov muddati tugagan", cls: 'st-expired' }
  return { text: "Hali to'lanmagan", cls: 'st-none' }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Tashkilotlar</h1>
    <button class="btn" @click="showOrgForm = true">+ Yangi tashkilot</button>
  </div>

  <div class="stat-row">
    <div class="stat-card"><div class="stat-label">Tashkilotlar</div><div class="stat-value">{{ orgs.length }}</div></div>
    <div class="stat-card"><div class="stat-label">Jami foydalanuvchilar</div><div class="stat-value">{{ totalUsers }}</div></div>
    <div class="stat-card"><div class="stat-label">Faol obunalar</div><div class="stat-value ok">{{ totalLicensed }}</div></div>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="layout">
    <!-- Tashkilotlar ro'yxati -->
    <div class="card list-card">
      <table class="table">
        <thead><tr><th>Tashkilot</th><th>Telefon</th><th class="text-center">To'lagan kassir</th><th></th></tr></thead>
        <tbody>
          <tr v-for="o in orgs" :key="o.id"
              :class="{ 'row-active': selected && selected.id === o.id }"
              style="cursor:pointer" @click="openOrg(o)">
            <td>
              <strong>{{ o.name }}</strong>
              <span v-if="!o.isActive" class="pill pill-red" style="margin-left:6px">bloklangan</span>
            </td>
            <td class="muted">{{ o.phone || '—' }}</td>
            <td class="text-center">
              <span class="bal-tag" :class="o.licensedCount >= o.userCount ? 'ok' : 'warn'">
                {{ o.licensedCount }} / {{ o.userCount }}
              </span>
            </td>
            <td class="text-right"><button class="btn btn-ghost btn-sm" @click.stop="openOrg(o)">Boshqarish →</button></td>
          </tr>
          <tr v-if="!orgs.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Tashkilot yo'q</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Tafsilot -->
    <div v-if="selected" class="card detail-card">
      <div class="detail-head">
        <div>
          <h3 style="margin:0">{{ selected.name }}</h3>
          <p class="muted" style="margin:2px 0 0">{{ selected.phone || 'telefon yo\'q' }}</p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <div class="org-actions">
        <button class="btn btn-sm" :class="selected.isActive ? 'btn-danger' : 'btn-success'"
                :disabled="busy" @click="toggleOrg(selected)">
          {{ selected.isActive ? 'Tashkilotni bloklash' : 'Blokdan chiqarish' }}
        </button>
      </div>

      <!-- Admin panel logini -->
      <div class="panel-section">
        <div class="panel-section-head">
          <span class="panel-label">Admin panel logini</span>
          <button class="btn btn-xs btn-outline" @click="showAdminForm = true">+ Qo'shish</button>
        </div>
        <p class="panel-hint">Veb admin panelga kirish uchun. To'lov olinmaydi.</p>

        <div v-for="u in adminUsers" :key="u.id" class="user-row" :class="{ 'row-off': !u.isActive }">
          <div class="user-main">
            <div class="user-name">
              {{ u.username }}
              <span class="pill pill-blue">Admin</span>
            </div>
            <div class="user-role-row">
              <select class="select select-sm" :value="u.appRoleId || ''" :disabled="busy"
                      @change="assignRole(u, $event.target.value || null)">
                <option value="">— Rol belgilanmagan —</option>
                <option v-for="r in orgRoles" :key="r.id" :value="r.id">{{ r.name }}</option>
              </select>
            </div>
          </div>
          <div class="user-acts">
            <button class="btn btn-xs" :class="u.isActive ? 'btn-danger' : 'btn-success'" :disabled="busy" @click="toggleUser(u)">
              {{ u.isActive ? 'Passiv' : 'Aktiv' }}
            </button>
          </div>
        </div>
        <p v-if="!adminUsers.length" class="muted empty-msg">Admin login yo'q</p>
      </div>

      <!-- Desktop panel logini -->
      <div class="panel-section">
        <div class="panel-section-head">
          <span class="panel-label">Desktop panel logini</span>
          <button class="btn btn-xs btn-outline" @click="showDesktopForm = true">+ Qo'shish</button>
        </div>
        <p class="panel-hint">Kassir va menejerlar uchun. Oylik to'lov qilsa <b>«💵 +1 oy»</b> bosing.</p>

        <div v-for="u in desktopUsers" :key="u.id" class="user-row" :class="{ 'row-off': !u.isActive }">
          <div class="user-main">
            <div class="user-name">
              {{ u.username }}
              <span class="pill pill-gray">{{ roleLabel[u.role] || u.role }}</span>
            </div>
            <div class="user-status">
              <span class="st-dot" :class="statusOf(u).cls"></span>
              <span>{{ statusOf(u).text }}</span>
              <span v-if="u.licensedUntil" class="muted" style="margin-left:6px">({{ fmtDate(u.licensedUntil) }} gacha)</span>
            </div>
          </div>
          <div class="user-acts">
            <button class="btn btn-success btn-xs" :disabled="busy" @click="extend(u, 1)">💵 +1 oy</button>
            <button class="btn btn-ghost btn-xs" :disabled="busy" @click="extend(u, 3)">+3 oy</button>
            <button v-if="u.licensedUntil" class="btn btn-ghost btn-xs" :disabled="busy" @click="revoke(u)">⨯</button>
            <button class="btn btn-xs" :class="u.isActive ? 'btn-danger' : 'btn-success'" :disabled="busy" @click="toggleUser(u)">
              {{ u.isActive ? 'Passiv' : 'Aktiv' }}
            </button>
          </div>
        </div>
        <p v-if="!desktopUsers.length" class="muted empty-msg">Desktop login yo'q</p>
      </div>
    </div>

    <div v-else class="card detail-card empty-detail">
      <div style="font-size:42px">👈</div>
      <p class="muted" style="margin:8px 0 0; text-align:center">Boshqarish uchun<br />tashkilotni tanlang</p>
    </div>
  </div>

  <!-- Yangi tashkilot modali -->
  <div v-if="showOrgForm" class="modal-overlay" @click.self="showOrgForm = false">
    <div class="modal-box">
      <div class="modal-head"><h3>Yangi tashkilot</h3><button class="modal-x" @click="showOrgForm = false">×</button></div>
      <form class="modal-body" @submit.prevent="createOrg">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="row">
          <div class="field"><label>Tashkilot nomi *</label><input v-model="orgForm.name" class="input" required placeholder="Do'kon / firma nomi" /></div>
          <div class="field"><label>Telefon</label><input v-model="orgForm.phone" class="input" placeholder="+998 ..." /></div>
        </div>
        <div class="field"><label>Izoh</label><input v-model="orgForm.note" class="input" placeholder="Ixtiyoriy" /></div>
        <hr style="border:none;border-top:1px solid var(--border);margin:14px 0" />
        <div class="muted" style="font-weight:600;font-size:13px;margin-bottom:6px">Bosh admin logini (Admin panel)</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="orgForm.adminUsername" class="input" required placeholder="admin login" /></div>
          <div class="field"><label>Parol *</label><input v-model="orgForm.adminPassword" type="text" class="input" required placeholder="kamida 6 belgi" /></div>
        </div>
        <button class="btn" style="width:100%; margin-top:14px">Tashkilot yaratish</button>
      </form>
    </div>
  </div>

  <!-- Admin + Desktop login qo'shish modali -->
  <div v-if="showAdminForm" class="modal-overlay" @click.self="showAdminForm = false">
    <div class="modal-box" style="max-width:500px">
      <div class="modal-head"><h3>Admin panel logini qo'shish</h3><button class="modal-x" @click="showAdminForm = false">×</button></div>
      <form class="modal-body" @submit.prevent="createAdminWithDesktop">
        <div v-if="err" class="alert alert-error">{{ err }}</div>

        <div class="section-label">Admin login (veb panel)</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="adminForm.username" class="input" required /></div>
          <div class="field"><label>Parol *</label><input v-model="adminForm.password" type="text" class="input" required placeholder="kamida 6 belgi" /></div>
        </div>

        <label class="checkbox-row">
          <input type="checkbox" v-model="createDesktop" />
          <span>Desktop panel logini ham yaratish</span>
        </label>

        <template v-if="createDesktop">
          <div class="section-label" style="margin-top:12px">Desktop login (kassir / menejer)</div>
          <div class="row">
            <div class="field"><label>Login *</label><input v-model="desktopForm.username" class="input" :required="createDesktop" /></div>
            <div class="field"><label>Parol *</label><input v-model="desktopForm.password" type="text" class="input" :required="createDesktop" placeholder="kamida 6 belgi" /></div>
          </div>
          <div class="row">
            <div class="field">
              <label>Rol</label>
              <select v-model="desktopForm.role" class="select">
                <option value="Cashier">Kassir</option>
                <option value="Manager">Menejer</option>
              </select>
            </div>
            <div class="field" style="max-width:160px"><label>Obuna (oy)</label><input v-model="desktopForm.licenseMonths" type="number" min="0" class="input" /></div>
          </div>
        </template>

        <button class="btn" style="width:100%; margin-top:16px" :disabled="busy">Qo'shish</button>
      </form>
    </div>
  </div>

  <!-- Faqat Desktop login qo'shish modali -->
  <div v-if="showDesktopForm" class="modal-overlay" @click.self="showDesktopForm = false">
    <div class="modal-box" style="max-width:460px">
      <div class="modal-head"><h3>Desktop panel logini qo'shish</h3><button class="modal-x" @click="showDesktopForm = false">×</button></div>
      <form class="modal-body" @submit.prevent="createDesktopOnly">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="desktopOnlyForm.username" class="input" required /></div>
          <div class="field"><label>Parol *</label><input v-model="desktopOnlyForm.password" type="text" class="input" required placeholder="kamida 6 belgi" /></div>
        </div>
        <div class="row">
          <div class="field">
            <label>Rol</label>
            <select v-model="desktopOnlyForm.role" class="select">
              <option value="Cashier">Kassir</option>
              <option value="Manager">Menejer</option>
            </select>
          </div>
          <div class="field" style="max-width:160px"><label>Obuna (oy)</label><input v-model="desktopOnlyForm.licenseMonths" type="number" min="0" class="input" /></div>
        </div>
        <p class="muted" style="font-size:12px;margin:4px 0 0">0 = faollashtirilmagan (keyin "+1 oy" bilan faollashtirasiz).</p>
        <button class="btn" style="width:100%; margin-top:14px" :disabled="busy">Qo'shish</button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.stat-row { display: flex; gap: 14px; margin-bottom: 16px; flex-wrap: wrap; }
.stat-card { flex: 1; min-width: 170px; background: #fff; border: 1px solid var(--border); border-radius: 12px; padding: 14px 18px; }
.stat-label { font-size: 13px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 26px; font-weight: 800; margin-top: 4px; }
.stat-value.ok { color: #15803d; }

.layout { display: flex; gap: 16px; align-items: flex-start; }
.list-card { flex: 1.3; min-width: 380px; }
.detail-card { flex: 1; min-width: 360px; max-width: 480px; }

.row-active { background: #eff6ff; }
.bal-tag { font-weight: 700; padding: 3px 10px; border-radius: 20px; font-size: 13px; }
.bal-tag.ok { background: #dcfce7; color: #15803d; }
.bal-tag.warn { background: #fef3c7; color: #92400e; }

.detail-head { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.org-actions { display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 14px; }

/* Panel sections */
.panel-section { border: 1px solid var(--border); border-radius: 10px; padding: 12px 14px; margin-bottom: 12px; }
.panel-section-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; }
.panel-label { font-weight: 700; font-size: 13px; }
.panel-hint { font-size: 12px; color: var(--muted); margin: 0 0 10px; }
.empty-msg { text-align: center; padding: 8px 0; font-size: 13px; }

.row-off { opacity: 0.6; }

.user-row { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 8px 0; border-bottom: 1px solid var(--border); }
.user-row:last-child { border-bottom: none; }
.user-main { flex: 1; min-width: 0; }
.user-name { font-weight: 700; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.user-role-row { margin-top: 5px; }
.user-status { font-size: 12.5px; color: var(--muted); display: flex; align-items: center; gap: 5px; margin-top: 3px; flex-wrap: wrap; }
.user-acts { display: flex; gap: 5px; flex-shrink: 0; flex-wrap: wrap; justify-content: flex-end; }

.st-dot { width: 9px; height: 9px; border-radius: 50%; display: inline-block; }
.st-ok { background: #16a34a; }
.st-soon { background: #d97706; }
.st-expired { background: #dc2626; }
.st-none { background: #94a3b8; }
.st-blocked { background: #1e293b; }

.pill-blue { background: #dbeafe; color: #1d4ed8; }
.pill-gray { background: #f1f5f9; color: #475569; }

.btn-xs { padding: 4px 8px; font-size: 12px; }
.btn-success { background: #16a34a; }
.btn-outline { background: transparent; border: 1px solid var(--border); color: var(--text); }

.select-sm { font-size: 12px; padding: 3px 6px; height: auto; }

/* Modal extras */
.section-label { font-weight: 700; font-size: 13px; color: var(--muted); margin-bottom: 8px; }
.checkbox-row { display: flex; align-items: center; gap: 8px; font-size: 14px; cursor: pointer; margin: 12px 0 0; }
.checkbox-row input { width: 16px; height: 16px; cursor: pointer; }

.empty-detail { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 240px; }
</style>
