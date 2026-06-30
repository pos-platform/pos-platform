<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { organizationsApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const orgs = ref([])
const totalCount = ref(0)
const totalPages = ref(0)
const currentPage = ref(1)
const PAGE_SIZE = 10
const loading = ref(false)
const err = ref('')
const success = ref('')

// Ogohlantirishlar (to'lov qilinmagan / muddati yaqin)
const alerts = ref({ unpaid: [], expiringSoon: [] })
const alertModal = ref(null) // 'unpaid' | 'expiringSoon' | null

const selected = ref(null)
const busy = ref(false)
const orgRoles = ref([])
const orgStores = ref([]) // tanlangan tashkilotning do'konlari

// Yangi tashkilot modali
const showOrgForm = ref(false)
const orgForm = reactive({ name: '', phone: '', note: '', adminUsername: '', adminPassword: '', storeCount: 1 })

const STORE_COUNTS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

// Admin + Desktop login qo'shish modali
const showAdminForm = ref(false)
const createDesktop = ref(true)
const adminForm = reactive({ username: '', password: '' })
const desktopForm = reactive({ username: '', password: '', fullName: '', phone: '', role: 'Cashier', licenseMonths: 0, branchId: '' })

// Faqat desktop login qo'shish modali
const showDesktopForm = ref(false)
const desktopOnlyForm = reactive({ username: '', password: '', fullName: '', phone: '', role: 'Cashier', licenseMonths: 0, branchId: '' })

// Desktop foydalanuvchini tahrirlash modali
const showEditForm = ref(false)
const editForm = reactive({ id: '', username: '', fullName: '', phone: '', role: 'Cashier', branchId: '', newPassword: '' })

const roleLabel = { Admin: 'Admin', Manager: 'Menejer', Cashier: 'Kassir', SuperAdmin: 'SuperAdmin' }

const totalUsers = computed(() => orgs.value.reduce((s, o) => s + o.userCount, 0))
const totalLicensed = computed(() => orgs.value.reduce((s, o) => s + o.licensedCount, 0))
const totalOrgs = computed(() => totalCount.value || orgs.value.length)

const adminUsers = computed(() => selected.value?.users.filter(u => u.role === 'Admin') ?? [])
const desktopUsers = computed(() => selected.value?.users.filter(u => u.role !== 'Admin') ?? [])

function fmtDate(d) { return d ? new Date(d).toLocaleDateString('uz-UZ') : '—' }
function notify(msg) { success.value = msg; setTimeout(() => { success.value = '' }, 2500) }

async function load() {
  loading.value = true
  err.value = ''
  try {
    const result = await organizationsApi.list(currentPage.value, PAGE_SIZE)
    orgs.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
  // Ogohlantirishlar alohida — xato bo'lsa asosiy ro'yxatga ta'sir qilmasin
  try {
    alerts.value = await organizationsApi.getAlerts()
  } catch {
    // silent
  }
}

async function goToPage(p) {
  if (p < 1 || p > totalPages.value) return
  currentPage.value = p
  await load()
}

async function openOrg(o) {
  err.value = ''
  orgRoles.value = []
  orgStores.value = []
  try {
    ;[selected.value, orgRoles.value, orgStores.value] = await Promise.all([
      organizationsApi.get(o.id),
      organizationsApi.getRoles(o.id),
      organizationsApi.getStores(o.id)
    ])
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function refreshSelected() {
  if (selected.value) {
    ;[selected.value, orgRoles.value, orgStores.value] = await Promise.all([
      organizationsApi.get(selected.value.id),
      organizationsApi.getRoles(selected.value.id),
      organizationsApi.getStores(selected.value.id)
    ])
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
      licenseMonths: 0,
      storeCount: Number(orgForm.storeCount) || 1
    })
    notify(`"${orgForm.name}" tashkiloti yaratildi`)
    Object.assign(orgForm, { name: '', phone: '', note: '', adminUsername: '', adminPassword: '', storeCount: 1 })
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
      const dp = {
        username: desktopForm.username.trim(),
        password: desktopForm.password,
        fullName: desktopForm.fullName.trim(),
        phone: desktopForm.phone.trim(),
        role: desktopForm.role,
        licenseMonths: Number(desktopForm.licenseMonths) || 0
      }
      if (desktopForm.branchId) dp.branchId = desktopForm.branchId
      await organizationsApi.createUser(selected.value.id, dp)
    }
    notify('Login(lar) qo\'shildi')
    Object.assign(adminForm, { username: '', password: '' })
    Object.assign(desktopForm, { username: '', password: '', fullName: '', phone: '', role: 'Cashier', licenseMonths: 0, branchId: '' })
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
    const payload = {
      username: desktopOnlyForm.username.trim(),
      password: desktopOnlyForm.password,
      fullName: desktopOnlyForm.fullName.trim(),
      phone: desktopOnlyForm.phone.trim(),
      role: desktopOnlyForm.role,
      licenseMonths: Number(desktopOnlyForm.licenseMonths) || 0
    }
    if (desktopOnlyForm.branchId) payload.branchId = desktopOnlyForm.branchId
    await organizationsApi.createUser(selected.value.id, payload)
    notify('Desktop login qo\'shildi')
    Object.assign(desktopOnlyForm, { username: '', password: '', fullName: '', phone: '', role: 'Cashier', licenseMonths: 0, branchId: '' })
    showDesktopForm.value = false
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

function openEdit(u) {
  err.value = ''
  Object.assign(editForm, {
    id: u.id,
    username: u.username,
    fullName: u.fullName || '',
    phone: u.phone || '',
    role: u.role,
    branchId: u.branchId || '',
    newPassword: ''
  })
  showEditForm.value = true
}

async function saveEdit() {
  err.value = ''
  busy.value = true
  try {
    const payload = {
      username: editForm.username.trim(),
      fullName: editForm.fullName.trim(),
      phone: editForm.phone.trim(),
      role: editForm.role,
      branchId: editForm.branchId || null
    }
    if (editForm.newPassword.trim()) payload.newPassword = editForm.newPassword.trim()
    await organizationsApi.updateUser(editForm.id, payload)
    notify('Foydalanuvchi yangilandi')
    showEditForm.value = false
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

async function setLifetime(u) {
  if (!confirm(`"${u.username}" doimiy (bir martalik to'liq sotib olingan) litsenziyaga o'tkazilsinmi? Obuna hech qachon tugamaydi.`)) return
  busy.value = true
  err.value = ''
  try {
    await organizationsApi.setLifetime(u.id)
    notify(`${u.username}: doimiy litsenziya yoqildi`)
    await refreshSelected()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function unsetLifetime(u) {
  if (!confirm(`"${u.username}" doimiy litsenziyasi bekor qilinsinmi? Oylik obunaga qaytadi.`)) return
  busy.value = true
  err.value = ''
  try {
    await organizationsApi.unsetLifetime(u.id)
    notify(`${u.username}: doimiy litsenziya bekor qilindi`)
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
  if (u.isLifetime) return { text: '♾ Doimiy litsenziya', cls: 'st-ok' }
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
    <div class="stat-card"><div class="stat-label">Tashkilotlar</div><div class="stat-value">{{ totalOrgs }}</div></div>
    <div class="stat-card"><div class="stat-label">Jami kassirlar</div><div class="stat-value">{{ totalUsers }}</div></div>
    <div class="stat-card"><div class="stat-label">Faol obunalar</div><div class="stat-value ok">{{ totalLicensed }}</div></div>
    <div class="stat-card stat-card-warn" :class="{ 'stat-card-clickable': alerts.unpaid.length }" @click="alerts.unpaid.length && (alertModal = 'unpaid')">
      <div class="stat-label">To'lov qilinmagan</div>
      <div class="stat-value warn">{{ alerts.unpaid.length }}</div>
      <div v-if="alerts.unpaid.length" class="stat-hint">tashkilot · bosing</div>
    </div>
    <div class="stat-card stat-card-danger" :class="{ 'stat-card-clickable': alerts.expiringSoon.length }" @click="alerts.expiringSoon.length && (alertModal = 'expiringSoon')">
      <div class="stat-label">Muddati yaqin (≤7 kun)</div>
      <div class="stat-value danger">{{ alerts.expiringSoon.length }}</div>
      <div v-if="alerts.expiringSoon.length" class="stat-hint">tashkilot · bosing</div>
    </div>
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

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="pagination">
        <button class="pg-btn" :disabled="currentPage <= 1" @click="goToPage(currentPage - 1)">‹</button>
        <template v-for="p in totalPages" :key="p">
          <button
            class="pg-btn"
            :class="{ 'pg-active': p === currentPage }"
            @click="goToPage(p)"
          >{{ p }}</button>
        </template>
        <button class="pg-btn" :disabled="currentPage >= totalPages" @click="goToPage(currentPage + 1)">›</button>
        <span class="pg-info">{{ (currentPage - 1) * PAGE_SIZE + 1 }}–{{ Math.min(currentPage * PAGE_SIZE, totalCount) }} / {{ totalCount }}</span>
      </div>
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
              <span class="status-pill" :class="u.isActive ? 'sp-on' : 'sp-off'">
                {{ u.isActive ? 'Faol' : 'Passiv' }}
              </span>
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
              {{ u.isActive ? "O'chirish" : 'Faollashtirish' }}
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
              <span v-if="u.fullName" class="muted" style="font-weight:500">· {{ u.fullName }}</span>
              <span class="pill pill-gray">{{ roleLabel[u.role] || u.role }}</span>
              <span v-if="u.branchName" class="store-tag">🏪 {{ u.branchName }}</span>
              <span class="status-pill" :class="u.isActive ? 'sp-on' : 'sp-off'">
                {{ u.isActive ? 'Faol' : 'Passiv' }}
              </span>
            </div>
            <div class="user-status">
              <span class="st-dot" :class="statusOf(u).cls"></span>
              <span>{{ statusOf(u).text }}</span>
              <span v-if="u.phone" class="muted" style="margin-left:6px">📞 {{ u.phone }}</span>
              <span v-if="u.licensedUntil" class="muted" style="margin-left:6px">({{ fmtDate(u.licensedUntil) }} gacha)</span>
            </div>
          </div>
          <div class="user-acts">
            <template v-if="u.isLifetime">
              <span class="pill pill-lifetime">♾ Doimiy</span>
              <button class="btn btn-ghost btn-xs" :disabled="busy" title="Doimiy litsenziyani bekor qilish" @click="unsetLifetime(u)">Bekor</button>
            </template>
            <template v-else>
              <button class="btn btn-success btn-xs" :disabled="busy" @click="extend(u, 1)">💵 +1 oy</button>
              <button class="btn btn-ghost btn-xs" :disabled="busy" @click="extend(u, 3)">+3 oy</button>
              <button class="btn btn-ghost btn-xs" :disabled="busy" title="Doimiy (bir martalik to'liq sotib olish)" @click="setLifetime(u)">♾ Doimiy</button>
              <button v-if="u.licensedUntil" class="btn btn-ghost btn-xs" :disabled="busy" @click="revoke(u)">⨯</button>
            </template>
            <button class="btn btn-ghost btn-xs" :disabled="busy" title="Tahrirlash" @click="openEdit(u)">✎ Tahrirlash</button>
            <button
              class="btn btn-xs"
              :class="u.isActive ? 'btn-danger' : 'btn-success'"
              :disabled="busy || (!u.isActive && !selected.isActive)"
              :title="!u.isActive && !selected.isActive ? 'Tashkilot bloklangan — avval tashkilotni faollashtiring' : ''"
              @click="toggleUser(u)"
            >
              {{ u.isActive ? "O'chirish" : 'Faollashtirish' }}
            </button>
          </div>
        </div>
        <p v-if="!desktopUsers.length" class="muted empty-msg">Desktop login yo'q</p>
        <p v-if="!selected.isActive && desktopUsers.length" class="muted" style="font-size:12px; margin:6px 0 0; color:var(--danger)">
          ⚠️ Tashkilot bloklangan — kassirlarni faollashtirish uchun avval tashkilotni faollashtiring.
        </p>
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
        <div class="row">
          <div class="field" style="flex:2"><label>Izoh</label><input v-model="orgForm.note" class="input" placeholder="Ixtiyoriy" /></div>
          <div class="field" style="flex:1;min-width:140px">
            <label>Do'kon soni *</label>
            <select v-model="orgForm.storeCount" class="select">
              <option v-for="n in STORE_COUNTS" :key="n" :value="n">{{ n }} ta do'kon</option>
            </select>
          </div>
        </div>
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
            <div class="field"><label>Ism sharifi</label><input v-model="desktopForm.fullName" class="input" placeholder="Masalan: Aliyev Vali" /></div>
            <div class="field"><label>Telefon</label><input v-model="desktopForm.phone" class="input" placeholder="+998 90 123 45 67" /></div>
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
          <div v-if="orgStores.length" class="field">
            <label>Do'kon</label>
            <select v-model="desktopForm.branchId" class="select">
              <option value="">— Belgilanmagan —</option>
              <option v-for="s in orgStores" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
        </template>

        <button class="btn" style="width:100%; margin-top:16px" :disabled="busy">Qo'shish</button>
      </form>
    </div>
  </div>

  <!-- Ogohlantirishlar modali -->
  <div v-if="alertModal" class="modal-overlay" @click.self="alertModal = null">
    <div class="modal-box" style="max-width:600px">
      <div class="modal-head">
        <h3>
          <template v-if="alertModal === 'unpaid'">To'lov qilinmagan kassirlar</template>
          <template v-else>Muddati yaqin tugayotgan obunalar (≤7 kun)</template>
        </h3>
        <button class="modal-x" @click="alertModal = null">×</button>
      </div>
      <div class="modal-body" style="max-height:70vh;overflow-y:auto;padding-top:0">
        <template v-for="item in (alertModal === 'unpaid' ? alerts.unpaid : alerts.expiringSoon)" :key="item.orgId">
          <div class="alert-org-block">
            <div class="alert-org-name">
              {{ item.orgName }}
              <span v-if="item.orgPhone" class="muted" style="font-weight:400;font-size:13px"> · {{ item.orgPhone }}</span>
            </div>
            <div v-for="u in item.users" :key="u.userId" class="alert-user-row">
              <span class="alert-username">{{ u.username }}</span>
              <span class="pill pill-gray" style="font-size:11px">{{ u.role === 'Cashier' ? 'Kassir' : u.role === 'Manager' ? 'Menejer' : u.role }}</span>
              <span v-if="u.branchName" class="store-tag">{{ u.branchName }}</span>
              <template v-if="alertModal === 'unpaid'">
                <span class="alert-status st-none">Hali to'lanmagan</span>
              </template>
              <template v-else>
                <span class="alert-status" :class="u.daysLeft <= 3 ? 'st-expired' : 'st-soon'">
                  {{ u.daysLeft }} kun qoldi · {{ fmtDate(u.licensedUntil) }} gacha
                </span>
              </template>
            </div>
          </div>
        </template>
        <p v-if="(alertModal === 'unpaid' ? alerts.unpaid : alerts.expiringSoon).length === 0" class="muted" style="text-align:center;padding:24px">
          Ma'lumot yo'q
        </p>
      </div>
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
          <div class="field"><label>Ism sharifi</label><input v-model="desktopOnlyForm.fullName" class="input" placeholder="Masalan: Aliyev Vali" /></div>
          <div class="field"><label>Telefon</label><input v-model="desktopOnlyForm.phone" class="input" placeholder="+998 90 123 45 67" /></div>
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
        <div v-if="orgStores.length" class="field" style="margin-top:8px">
          <label>Do'kon</label>
          <select v-model="desktopOnlyForm.branchId" class="select">
            <option value="">— Belgilanmagan —</option>
            <option v-for="s in orgStores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <p class="muted" style="font-size:12px;margin:4px 0 0">0 = faollashtirilmagan (keyin "+1 oy" bilan faollashtirasiz).</p>
        <button class="btn" style="width:100%; margin-top:14px" :disabled="busy">Qo'shish</button>
      </form>
    </div>
  </div>

  <!-- Desktop foydalanuvchini tahrirlash modali -->
  <div v-if="showEditForm" class="modal-overlay" @click.self="showEditForm = false">
    <div class="modal-box" style="max-width:460px">
      <div class="modal-head"><h3>Foydalanuvchini tahrirlash</h3><button class="modal-x" @click="showEditForm = false">×</button></div>
      <form class="modal-body" @submit.prevent="saveEdit">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div class="row">
          <div class="field"><label>Login *</label><input v-model="editForm.username" class="input" required /></div>
          <div class="field">
            <label>Rol</label>
            <select v-model="editForm.role" class="select">
              <option value="Cashier">Kassir</option>
              <option value="Manager">Menejer</option>
            </select>
          </div>
        </div>
        <div class="row">
          <div class="field"><label>Ism sharifi</label><input v-model="editForm.fullName" class="input" placeholder="Masalan: Aliyev Vali" /></div>
          <div class="field"><label>Telefon</label><input v-model="editForm.phone" class="input" placeholder="+998 90 123 45 67" /></div>
        </div>
        <div v-if="orgStores.length" class="field" style="margin-top:8px">
          <label>Do'kon</label>
          <select v-model="editForm.branchId" class="select">
            <option value="">— Belgilanmagan —</option>
            <option v-for="s in orgStores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <div class="field" style="margin-top:8px">
          <label>Yangi parol</label>
          <input v-model="editForm.newPassword" type="text" class="input" placeholder="Bo'sh qoldirsangiz — o'zgarmaydi (kamida 6 belgi)" />
        </div>
        <button class="btn" style="width:100%; margin-top:14px" :disabled="busy">Saqlash</button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.stat-row { display: flex; gap: 14px; margin-bottom: 16px; flex-wrap: wrap; }
.stat-card { flex: 1 1 140px; background: #fff; border: 1px solid var(--border); border-radius: 12px; padding: 14px 18px; }
.stat-card-clickable { cursor: pointer; transition: box-shadow 0.15s, transform 0.1s; }
.stat-card-clickable:hover { box-shadow: 0 2px 10px rgba(0,0,0,0.10); transform: translateY(-1px); }
.stat-card-warn { border-color: #fcd34d; background: #fffbeb; }
.stat-card-danger { border-color: #fca5a5; background: #fff1f1; }
.stat-label { font-size: 13px; color: var(--muted); font-weight: 600; }
.stat-value { font-size: 26px; font-weight: 800; margin-top: 4px; }
.stat-value.ok { color: #15803d; }
.stat-value.warn { color: #b45309; }
.stat-value.danger { color: #dc2626; }
.stat-hint { font-size: 11px; color: var(--muted); margin-top: 2px; }

.alert-org-block { border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px; margin: 10px 0; }
.alert-org-name { font-weight: 700; font-size: 14px; margin-bottom: 8px; }
.alert-user-row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; padding: 4px 0; border-top: 1px solid var(--border); }
.alert-username { font-weight: 600; font-size: 13px; }
.alert-status { font-size: 12px; font-weight: 600; }
.alert-status.st-none { color: #94a3b8; }
.alert-status.st-soon { color: #d97706; }
.alert-status.st-expired { color: #dc2626; }

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

.user-row { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 8px 10px; padding: 10px 0; border-bottom: 1px solid var(--border); }
.user-row:last-child { border-bottom: none; }
.user-main { flex: 1 1 260px; min-width: 0; }
.user-name { font-weight: 700; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.user-role-row { margin-top: 5px; }
.user-status { font-size: 12.5px; color: var(--muted); display: flex; align-items: center; gap: 5px; margin-top: 3px; flex-wrap: wrap; }
.user-acts { display: flex; gap: 5px; flex-shrink: 0; flex-wrap: wrap; justify-content: flex-end; }
/* Tor panelda amallar to'liq qatorni egallab, pastga tushadi (chapdan) */
@media (max-width: 900px) {
  .user-acts { flex: 1 1 100%; justify-content: flex-start; }
}

.st-dot { width: 9px; height: 9px; border-radius: 50%; display: inline-block; }
.st-ok { background: #16a34a; }
.st-soon { background: #d97706; }
.st-expired { background: #dc2626; }
.st-none { background: #94a3b8; }
.st-blocked { background: #1e293b; }

.pill-blue { background: #dbeafe; color: #1d4ed8; }
.pill-gray { background: #f1f5f9; color: #475569; }
.pill-lifetime { background: #ede9fe; color: #6d28d9; font-weight: 700; }

.btn-xs { padding: 4px 8px; font-size: 12px; }
.btn-success { background: #16a34a; }
.btn-outline { background: transparent; border: 1px solid var(--border); color: var(--text); }

.select-sm { font-size: 12px; padding: 3px 6px; height: auto; }

.status-pill {
  font-size: 11px;
  font-weight: 600;
  padding: 1px 7px;
  border-radius: 10px;
}
.sp-on  { background: #dcfce7; color: #15803d; }
.sp-off { background: #fee2e2; color: #dc2626; }

.store-tag {
  font-size: 11px;
  font-weight: 500;
  color: #1d4ed8;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 10px;
  padding: 1px 7px;
}

.pagination {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 12px 16px 4px;
  flex-wrap: wrap;
}
.pg-btn {
  min-width: 32px;
  height: 32px;
  padding: 0 8px;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: #fff;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: var(--text);
  transition: background 0.15s;
}
.pg-btn:hover:not(:disabled) { background: var(--surface); }
.pg-btn:disabled { opacity: 0.4; cursor: default; }
.pg-active { background: var(--primary) !important; color: #fff !important; border-color: var(--primary) !important; }
.pg-info { font-size: 12px; color: var(--text-muted); margin-left: 6px; }

/* Modal extras */
.section-label { font-weight: 700; font-size: 13px; color: var(--muted); margin-bottom: 8px; }
.checkbox-row { display: flex; align-items: center; gap: 8px; font-size: 14px; cursor: pointer; margin: 12px 0 0; }
.checkbox-row input { width: 16px; height: 16px; cursor: pointer; }

.empty-detail { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 240px; }
</style>
