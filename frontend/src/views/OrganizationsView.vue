<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { organizationsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const orgs = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')

const selected = ref(null) // tashkilot tafsiloti (foydalanuvchilar bilan)
const busy = ref(false)

// Yangi tashkilot modali
const showOrgForm = ref(false)
const orgForm = reactive({ name: '', phone: '', note: '', adminUsername: '', adminPassword: '', licenseMonths: 1 })

// Yangi foydalanuvchi modali
const showUserForm = ref(false)
const userForm = reactive({ username: '', password: '', role: 'Cashier', licenseMonths: 0 })

const roleLabel = { Admin: 'Admin', Manager: 'Menejer', Cashier: 'Kassir', SuperAdmin: 'SuperAdmin' }

const totalUsers = computed(() => orgs.value.reduce((s, o) => s + o.userCount, 0))
const totalLicensed = computed(() => orgs.value.reduce((s, o) => s + o.licensedCount, 0))

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
  try {
    selected.value = await organizationsApi.get(o.id)
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function refreshSelected() {
  if (selected.value) selected.value = await organizationsApi.get(selected.value.id)
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
      licenseMonths: Number(orgForm.licenseMonths) || 0
    })
    notify(`"${orgForm.name}" tashkiloti yaratildi`)
    Object.assign(orgForm, { name: '', phone: '', note: '', adminUsername: '', adminPassword: '', licenseMonths: 1 })
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

async function createUser() {
  err.value = ''
  busy.value = true
  try {
    await organizationsApi.createUser(selected.value.id, {
      username: userForm.username.trim(),
      password: userForm.password,
      role: userForm.role,
      licenseMonths: Number(userForm.licenseMonths) || 0
    })
    notify('Foydalanuvchi qo\'shildi')
    Object.assign(userForm, { username: '', password: '', role: 'Cashier', licenseMonths: 0 })
    showUserForm.value = false
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

function statusOf(u) {
  if (!u.isActive) return { text: 'Bloklangan', cls: 'st-blocked' }
  if (u.isLicensed) return { text: `${u.daysLeft} kun qoldi`, cls: u.daysLeft <= 5 ? 'st-soon' : 'st-ok' }
  if (u.licensedUntil) return { text: 'Muddati tugagan', cls: 'st-expired' }
  return { text: 'Faollashtirilmagan', cls: 'st-none' }
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
        <thead><tr><th>Tashkilot</th><th>Telefon</th><th class="text-center">Obuna</th><th></th></tr></thead>
        <tbody>
          <tr v-for="o in orgs" :key="o.id"
              :class="{ 'row-active': selected && selected.id === o.id }"
              style="cursor:pointer" @click="openOrg(o)">
            <td>
              <strong>{{ o.name }}</strong>
              <span v-if="!o.isActive" class="pill pill-red" style="margin-left:6px">bloklangan</span>
            </td>
            <td class="muted">{{ o.phone || '—' }}</td>
            <td class="text-center"><span class="bal-tag ok">{{ o.licensedCount }}/{{ o.userCount }}</span></td>
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
        <button class="btn btn-sm" @click="showUserForm = true">+ Kassir / foydalanuvchi</button>
      </div>

      <div class="muted" style="font-weight:600;font-size:13px;margin:16px 0 8px">
        Foydalanuvchilar ({{ selected.users.length }})
      </div>

      <div v-for="u in selected.users" :key="u.id" class="user-row">
        <div class="user-main">
          <div class="user-name">
            {{ u.username }}
            <span class="pill" :class="u.role === 'Admin' ? 'pill-blue' : 'pill-gray'">{{ roleLabel[u.role] || u.role }}</span>
          </div>
          <div class="user-status">
            <span class="st-dot" :class="statusOf(u).cls"></span>
            <span>{{ statusOf(u).text }}</span>
            <span v-if="u.licensedUntil" class="muted" style="margin-left:6px">({{ fmtDate(u.licensedUntil) }} gacha)</span>
          </div>
        </div>
        <div class="user-acts">
          <button class="btn btn-success btn-xs" :disabled="busy" @click="extend(u, 1)" title="1 oyga faollashtirish/uzaytirish">+1 oy</button>
          <button class="btn btn-ghost btn-xs" :disabled="busy" @click="extend(u, 3)">+3 oy</button>
          <button v-if="u.licensedUntil" class="btn btn-ghost btn-xs" :disabled="busy" @click="revoke(u)" title="Obunani bekor qilish">⨯</button>
          <button class="btn btn-ghost btn-xs" :disabled="busy" @click="toggleUser(u)">
            {{ u.isActive ? 'Bloklash' : 'Faollashtirish' }}
          </button>
        </div>
      </div>
      <p v-if="!selected.users.length" class="muted" style="text-align:center;padding:12px 0">Foydalanuvchi yo'q</p>
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
        <div class="muted" style="font-weight:600;font-size:13px;margin-bottom:6px">Bosh admin (do'kon egasi)</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="orgForm.adminUsername" class="input" required placeholder="admin login" /></div>
          <div class="field"><label>Parol *</label><input v-model="orgForm.adminPassword" type="text" class="input" required placeholder="kamida 6 belgi" /></div>
          <div class="field" style="max-width:140px"><label>Obuna (oy)</label><input v-model="orgForm.licenseMonths" type="number" min="0" class="input" /></div>
        </div>
        <button class="btn" style="width:100%; margin-top:14px">Tashkilot yaratish</button>
      </form>
    </div>
  </div>

  <!-- Yangi foydalanuvchi modali -->
  <div v-if="showUserForm" class="modal-overlay" @click.self="showUserForm = false">
    <div class="modal-box" style="max-width:460px">
      <div class="modal-head"><h3>Foydalanuvchi qo'shish</h3><button class="modal-x" @click="showUserForm = false">×</button></div>
      <form class="modal-body" @submit.prevent="createUser">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="userForm.username" class="input" required /></div>
          <div class="field"><label>Parol *</label><input v-model="userForm.password" type="text" class="input" required placeholder="kamida 6 belgi" /></div>
        </div>
        <div class="row">
          <div class="field">
            <label>Rol</label>
            <select v-model="userForm.role" class="select">
              <option value="Cashier">Kassir</option>
              <option value="Manager">Menejer</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          <div class="field" style="max-width:160px"><label>Boshlang'ich obuna (oy)</label><input v-model="userForm.licenseMonths" type="number" min="0" class="input" /></div>
        </div>
        <p class="muted" style="font-size:12px;margin:4px 0 0">0 = faollashtirilmagan (keyin to'lov olib "+1 oy" bilan faollashtirasiz).</p>
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
.detail-card { flex: 1; min-width: 360px; max-width: 460px; }

.row-active { background: #eff6ff; }
.bal-tag { font-weight: 700; padding: 3px 10px; border-radius: 20px; font-size: 13px; }
.bal-tag.ok { background: #dcfce7; color: #15803d; }

.detail-head { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.org-actions { display: flex; gap: 8px; flex-wrap: wrap; }

.user-row { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 10px 0; border-bottom: 1px solid var(--border); }
.user-row:last-child { border-bottom: none; }
.user-name { font-weight: 700; display: flex; align-items: center; gap: 8px; }
.user-status { font-size: 12.5px; color: var(--muted); display: flex; align-items: center; gap: 5px; margin-top: 3px; }
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

.empty-detail { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 240px; }
</style>
