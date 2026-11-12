<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { onlineStoresAdminApi } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

// SuperAdmin: mobil ilovadagi do'konlar — yoqish (tasdiqlash), bloklash, to'lov usullari, tavsiya, tarif muddati.
const filter = ref('all')
const search = ref('')
const rows = ref([])
const loading = ref(false)
const err = ref('')
const ok = ref('')

const selected = ref(null) // to'liq sozlamalar
const busy = ref(false)
const form = reactive({
  isEnabled: false,
  isBlocked: false,
  blockReason: '',
  isFeatured: false,
  sortOrder: 0,
  paidUntil: '',
  allowCash: true,
  allowTransfer: true,
  allowCardOnDelivery: false,
  allowPayme: false,
  allowClick: false
})

const filters = [
  { id: 'all', label: 'Barchasi' },
  { id: 'requested', label: 'So\'rov yuborgan' },
  { id: 'enabled', label: 'Ilovada' },
  { id: 'blocked', label: 'Bloklangan' }
]

async function load() {
  loading.value = true
  err.value = ''
  try {
    rows.value = await onlineStoresAdminApi.list({ filter: filter.value, search: search.value.trim() || undefined })
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

let timer = null
watch(filter, () => load())
watch(search, () => {
  clearTimeout(timer)
  timer = setTimeout(load, 400)
})

async function open(row) {
  err.value = ''
  ok.value = ''
  try {
    const s = await onlineStoresAdminApi.get(row.organizationId)
    selected.value = s
    Object.assign(form, {
      isEnabled: s.isEnabled,
      isBlocked: s.isBlocked,
      blockReason: s.blockReason || '',
      isFeatured: s.isFeatured,
      sortOrder: s.sortOrder,
      paidUntil: s.paidUntil ? s.paidUntil.substring(0, 10) : '',
      allowCash: s.allowCash,
      allowTransfer: s.allowTransfer,
      allowCardOnDelivery: s.allowCardOnDelivery,
      allowPayme: !!s.allowPayme,
      allowClick: !!s.allowClick
    })
    fillApp(s)
  } catch (e) {
    err.value = errorMessage(e)
  }
}

async function save() {
  if (!selected.value) return
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    const s = await onlineStoresAdminApi.update(selected.value.organizationId, {
      isEnabled: form.isEnabled,
      isBlocked: form.isBlocked,
      blockReason: form.isBlocked ? form.blockReason.trim() : null,
      isFeatured: form.isFeatured,
      sortOrder: Number(form.sortOrder) || 0,
      // Kun oxirigacha amal qiladi (UTC+5 → UTC 18:59).
      paidUntil: form.paidUntil ? `${form.paidUntil}T18:59:59Z` : null,
      allowCash: form.allowCash,
      allowTransfer: form.allowTransfer,
      allowCardOnDelivery: form.allowCardOnDelivery,
      allowPayme: form.allowPayme,
      allowClick: form.allowClick
    })
    selected.value = s
    ok.value = 'Saqlandi.'
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

// ---- Mobil ilova (Bosqich 8, white-label) ----
const SLUG_RE = /^[a-z0-9-]{3,40}$/
const PACKAGE_RE = /^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$/
const HEX_RE = /^#[0-9a-fA-F]{6}$/
const DEFAULT_COLOR = '#2563eb'

const appBusy = ref(false)
const appErr = ref('')
const appOk = ref('')
const copied = ref(false)
const app = reactive({
  slug: '',
  appName: '',
  appPackageId: '',
  brandColor: '',
  androidMinBuild: '',
  androidLatestBuild: '',
  androidLatestVersion: '',
  androidStoreUrl: '',
  iosMinBuild: '',
  iosLatestBuild: '',
  iosLatestVersion: '',
  iosStoreUrl: ''
})

// Lotin/kirill nomni slugga aylantiradi: o'/g'/sh/ch, kichik harf, defis.
const CYR = {
  а: 'a', б: 'b', в: 'v', г: 'g', д: 'd', е: 'e', ё: 'yo', ж: 'j', з: 'z', и: 'i', й: 'y', к: 'k', л: 'l', м: 'm',
  н: 'n', о: 'o', п: 'p', р: 'r', с: 's', т: 't', у: 'u', ф: 'f', х: 'x', ц: 'ts', ч: 'ch', ш: 'sh', щ: 'sh',
  ъ: '', ы: 'i', ь: '', э: 'e', ю: 'yu', я: 'ya', ў: 'o', қ: 'q', ғ: 'g', ҳ: 'h'
}
function slugify(name) {
  let s = (name || '').toLowerCase()
  s = s.replace(/[а-яёўқғҳ]/g, (c) => CYR[c] ?? '')
  // o' / g' (har xil apostroflar) → o / g
  s = s.replace(/([og])['`ʻʼ‘’]/g, '$1')
  s = s.normalize('NFD').replace(/[̀-ͯ]/g, '')
  s = s.replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').replace(/-{2,}/g, '-')
  return s.substring(0, 40).replace(/-+$/, '')
}
function suggestPackage(slug) {
  const base = (slug || '').replace(/-/g, '_')
  if (!base) return ''
  // Paket qismi harf bilan boshlanishi shart
  return `uz.poscloud.${/^[a-z]/.test(base) ? base : 's' + base}`
}

const slugValid = computed(() => !app.slug || SLUG_RE.test(app.slug))
const packageValid = computed(() => !app.appPackageId || PACKAGE_RE.test(app.appPackageId))
const colorValid = computed(() => !app.brandColor || HEX_RE.test(app.brandColor))
const appNameValid = computed(() => app.appName.length <= 30)
const appValid = computed(() => slugValid.value && packageValid.value && colorValid.value && appNameValid.value)
const previewColor = computed(() => (HEX_RE.test(app.brandColor) ? app.brandColor : DEFAULT_COLOR))
const previewName = computed(() => app.appName || selected.value?.displayName || selected.value?.organizationName || 'Ilova')
const logoSrc = computed(() => mediaUrl(selected.value?.logoUrl))
const buildCommand = computed(() => `.\\tool\\store.ps1 -Slug ${app.slug || '<slug>'} -Build appbundle`)

// Color picker faqat to'g'ri hex qabul qiladi
const pickerColor = computed({
  get: () => previewColor.value,
  set: (v) => { app.brandColor = v }
})

function fillApp(s) {
  const v = (x) => (x === null || x === undefined ? '' : String(x))
  Object.assign(app, {
    slug: v(s.slug),
    appName: v(s.appName),
    appPackageId: v(s.appPackageId),
    brandColor: v(s.brandColor),
    androidMinBuild: v(s.androidMinBuild),
    androidLatestBuild: v(s.androidLatestBuild),
    androidLatestVersion: v(s.androidLatestVersion),
    androidStoreUrl: v(s.androidStoreUrl),
    iosMinBuild: v(s.iosMinBuild),
    iosLatestBuild: v(s.iosLatestBuild),
    iosLatestVersion: v(s.iosLatestVersion),
    iosStoreUrl: v(s.iosStoreUrl)
  })
  appErr.value = ''
  appOk.value = ''
  copied.value = false
}

function suggestSlug() {
  app.slug = slugify(selected.value?.displayName || selected.value?.organizationName)
  if (!app.appPackageId) app.appPackageId = suggestPackage(app.slug)
}
function suggestPkg() {
  app.appPackageId = suggestPackage(app.slug)
}
function onSlugBlur() {
  app.slug = app.slug.trim().toLowerCase()
  if (app.slug && !app.appPackageId) app.appPackageId = suggestPackage(app.slug)
}

async function copyCommand() {
  try {
    await navigator.clipboard.writeText(buildCommand.value)
    copied.value = true
    setTimeout(() => { copied.value = false }, 1500)
  } catch {
    appErr.value = 'Nusxa olib bo\'lmadi — qo\'lda belgilang.'
  }
}

async function saveApp() {
  if (!selected.value || !appValid.value) return
  appBusy.value = true
  appErr.value = ''
  appOk.value = ''
  const str = (x) => (x.trim() ? x.trim() : null)
  const int = (x) => {
    const t = String(x).trim()
    return t === '' ? null : Number.parseInt(t, 10)
  }
  try {
    const res = await onlineStoresAdminApi.updateApp(selected.value.organizationId, {
      slug: str(app.slug),
      appName: str(app.appName),
      appPackageId: str(app.appPackageId),
      brandColor: app.brandColor.trim() ? app.brandColor.trim().toUpperCase() : null,
      androidMinBuild: int(app.androidMinBuild),
      androidLatestBuild: int(app.androidLatestBuild),
      androidLatestVersion: str(app.androidLatestVersion),
      androidStoreUrl: str(app.androidStoreUrl),
      iosMinBuild: int(app.iosMinBuild),
      iosLatestBuild: int(app.iosLatestBuild),
      iosLatestVersion: str(app.iosLatestVersion),
      iosStoreUrl: str(app.iosStoreUrl)
    })
    // Javob tafsilot obyekti bo'lsa — yangilaymiz
    if (res && typeof res === 'object' && res.organizationId) {
      selected.value = res
      fillApp(res)
    }
    appOk.value = 'Ilova sozlamalari saqlandi.'
    await load()
  } catch (e) {
    appErr.value = errorMessage(e)
  } finally {
    appBusy.value = false
  }
}

function statusOf(r) {
  if (r.isBlocked) return { text: 'Bloklangan', cls: 'st-blocked' }
  if (r.isEnabled) return { text: 'Ilovada', cls: 'st-on' }
  if (r.isRequested) return { text: 'So\'rov', cls: 'st-req' }
  return { text: 'O\'chiq', cls: 'st-off' }
}

function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('uz-UZ') : '—'
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Online do'konlar</h1>
    <button class="btn btn-ghost btn-sm" @click="load">↻ Yangilash</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>

  <div class="card toolbar">
    <div class="chips">
      <button v-for="f in filters" :key="f.id" class="chip" :class="{ active: filter === f.id }" @click="filter = f.id">
        {{ f.label }}
      </button>
    </div>
    <input v-model="search" class="input" placeholder="Tashkilot nomi..." style="max-width:260px" />
  </div>

  <div class="layout">
    <div class="card" style="overflow:auto">
      <table class="table">
        <thead>
          <tr>
            <th>Tashkilot</th>
            <th>Holat</th>
            <th>Do'konlar</th>
            <th>Tarif</th>
            <th>30 kun buyurtma</th>
            <th>Qabul</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading"><td colspan="6" class="muted">Yuklanmoqda...</td></tr>
          <tr v-else-if="!rows.length"><td colspan="6" class="muted">Ma'lumot yo'q</td></tr>
          <tr v-for="r in rows" :key="r.organizationId" class="clickable"
              :class="{ sel: selected?.organizationId === r.organizationId }" @click="open(r)">
            <td>
              <div style="font-weight:600">{{ r.displayName || r.organizationName }}</div>
              <div v-if="r.displayName" class="muted small">{{ r.organizationName }}</div>
              <div v-if="r.slug || r.appName" class="muted small">
                📱 {{ r.appName || '—' }}<span v-if="r.slug"> · {{ r.slug }}</span>
              </div>
              <div v-if="!r.organizationActive" class="small" style="color:#dc2626">tashkilot bloklangan</div>
            </td>
            <td>
              <span class="st" :class="statusOf(r).cls">{{ statusOf(r).text }}</span>
              <span v-if="r.isFeatured" class="st st-feat">★</span>
            </td>
            <td>{{ r.storeBranchCount }}</td>
            <td>{{ fmtDate(r.paidUntil) }}</td>
            <td>{{ r.ordersLast30Days }}</td>
            <td>{{ r.isAcceptingOrders ? 'Ha' : 'Yo\'q' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="selected" class="card detail">
      <div class="detail-head">
        <div>
          <h3 style="margin:0">{{ selected.displayName || selected.organizationName }}</h3>
          <p class="muted small" style="margin:2px 0 0">
            {{ selected.isVisible ? 'Ilovada ko\'rinmoqda' : 'Ilovada ko\'rinmaydi' }}
            · {{ selected.phone || 'telefon yo\'q' }}
          </p>
        </div>
        <button class="modal-x" @click="selected = null">×</button>
      </div>

      <label class="row"><input v-model="form.isEnabled" type="checkbox" /> Ilovada yoqilgan (tasdiqlangan)</label>
      <label class="row"><input v-model="form.isBlocked" type="checkbox" /> Bloklangan</label>
      <input v-if="form.isBlocked" v-model="form.blockReason" class="input" placeholder="Bloklash sababi (majburiy)" />
      <label class="row"><input v-model="form.isFeatured" type="checkbox" /> Tavsiya etilgan (bosh sahifada yuqorida)</label>

      <div class="grid2">
        <div class="field">
          <label>Tartib raqami</label>
          <input v-model="form.sortOrder" type="number" class="input" />
        </div>
        <div class="field">
          <label>Tarif muddati (bo'sh — cheklovsiz)</label>
          <input v-model="form.paidUntil" type="date" class="input" />
        </div>
      </div>

      <div class="field">
        <label>To'lov usullari</label>
        <label class="row"><input v-model="form.allowCash" type="checkbox" /> Naqd</label>
        <label class="row"><input v-model="form.allowTransfer" type="checkbox" /> Kartaga o'tkazma</label>
        <label class="row"><input v-model="form.allowCardOnDelivery" type="checkbox" /> Kuryer terminali</label>
        <label class="row"><input v-model="form.allowPayme" type="checkbox" /> Payme (buyurtma yig'ilgach online to'lov)</label>
        <label class="row"><input v-model="form.allowClick" type="checkbox" /> Click (buyurtma yig'ilgach online to'lov)</label>
        <p v-if="form.allowTransfer && !selected.transferCardNumber" class="small" style="color:#b45309;margin:4px 0 0">
          Do'kon karta raqamini kiritmagan — o'tkazma ilovada chiqmaydi.
        </p>
      </div>

      <div class="info small muted">
        <div>Ish vaqti: {{ selected.opensAt && selected.closesAt ? `${selected.opensAt.substring(0,5)}–${selected.closesAt.substring(0,5)}` : 'ko\'rsatilmagan' }}</div>
        <div>Yetkazish: {{ selected.deliveryEnabled ? `${selected.deliveryFee.toLocaleString()} so'm` : 'yo\'q' }}
          · Olib ketish: {{ selected.pickupEnabled ? 'ha' : 'yo\'q' }}</div>
        <div>Minimal summa: {{ selected.minOrderAmount.toLocaleString() }} so'm</div>
      </div>

      <button class="btn" :disabled="busy" @click="save">{{ busy ? 'Saqlanmoqda...' : 'Saqlash' }}</button>

      <!-- Mobil ilova (white-label) — alohida saqlanadi -->
      <div class="app-section">
        <h4 style="margin:0">Mobil ilova</h4>
        <div v-if="appErr" class="alert alert-error">{{ appErr }}</div>
        <div v-if="appOk" class="alert alert-success">{{ appOk }}</div>

        <div class="preview">
          <div class="app-icon" :style="{ background: previewColor }">
            <img v-if="logoSrc" :src="logoSrc" alt="logo" />
            <span v-else class="app-letter">{{ previewName.charAt(0).toUpperCase() }}</span>
          </div>
          <div class="app-label">{{ previewName }}</div>
        </div>

        <div class="field">
          <label>Slug</label>
          <div class="inline">
            <input v-model="app.slug" class="input" placeholder="masalan: baraka-market" @blur="onSlugBlur" />
            <button class="btn btn-ghost btn-sm" type="button" title="Nomdan taklif" @click="suggestSlug">↺</button>
          </div>
          <span v-if="!slugValid" class="small bad">3–40 belgi: a-z, 0-9 va "-".</span>
        </div>

        <div class="field">
          <label>Ilova nomi ({{ app.appName.length }}/30)</label>
          <input v-model="app.appName" class="input" maxlength="30" placeholder="Telefonda ko'rinadigan nom" />
          <span v-if="!appNameValid" class="small bad">30 belgidan oshmasin.</span>
        </div>

        <div class="field">
          <label>Paket ID (Android / iOS)</label>
          <div class="inline">
            <input v-model="app.appPackageId" class="input" placeholder="uz.poscloud.baraka_market" />
            <button class="btn btn-ghost btn-sm" type="button" title="Slugdan taklif" :disabled="!app.slug" @click="suggestPkg">↺</button>
          </div>
          <span v-if="!packageValid" class="small bad">Format: uz.poscloud.nom (kichik harf, raqam, "_").</span>
        </div>

        <div class="field">
          <label>Brend rangi</label>
          <div class="inline">
            <input v-model="pickerColor" type="color" class="color" />
            <input v-model="app.brandColor" class="input" placeholder="#RRGGBB" maxlength="7" />
          </div>
          <span v-if="!colorValid" class="small bad">Format: #RRGGBB.</span>
        </div>

        <div class="field">
          <label>Android</label>
          <div class="grid2">
            <input v-model="app.androidMinBuild" type="number" min="0" class="input" placeholder="Min build" />
            <input v-model="app.androidLatestBuild" type="number" min="0" class="input" placeholder="Oxirgi build" />
          </div>
          <input v-model="app.androidLatestVersion" class="input" placeholder="Oxirgi versiya (1.0.0)" />
          <input v-model="app.androidStoreUrl" class="input" placeholder="Google Play havolasi" />
        </div>

        <div class="field">
          <label>iOS</label>
          <div class="grid2">
            <input v-model="app.iosMinBuild" type="number" min="0" class="input" placeholder="Min build" />
            <input v-model="app.iosLatestBuild" type="number" min="0" class="input" placeholder="Oxirgi build" />
          </div>
          <input v-model="app.iosLatestVersion" class="input" placeholder="Oxirgi versiya (1.0.0)" />
          <input v-model="app.iosStoreUrl" class="input" placeholder="App Store havolasi" />
        </div>

        <div class="field">
          <label>Build buyrug'i (mobile/ papkasida)</label>
          <div class="inline">
            <input :value="buildCommand" class="input mono" readonly @focus="$event.target.select()" />
            <button class="btn btn-ghost btn-sm" type="button" @click="copyCommand">{{ copied ? '✓' : 'Nusxa' }}</button>
          </div>
        </div>

        <ul class="hints small muted">
          <li>Ikonka fayllari (icon.png 1024px, store.json) — <code>mobile/stores/{{ app.slug || 'slug' }}/</code></li>
          <li>iOS ilova do'konning o'z Apple Developer akkaunti orqali chiqariladi.</li>
          <li>Slug va paket ID chiqarilgandan keyin o'zgartirilmasin.</li>
        </ul>

        <button class="btn" :disabled="appBusy || !appValid" @click="saveApp">
          {{ appBusy ? 'Saqlanmoqda...' : 'Ilovani saqlash' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.toolbar { display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap; margin-bottom:12px; }
.chips { display:flex; gap:6px; flex-wrap:wrap; }
.chip { border:1px solid var(--border, #e2e8f0); background:#fff; border-radius:999px; padding:6px 12px; cursor:pointer; font-weight:600; font-size:13px; }
.chip.active { background:#0f172a; color:#fff; border-color:#0f172a; }
.layout { display:grid; grid-template-columns: minmax(0, 1fr) 380px; gap:14px; align-items:start; }
.clickable { cursor:pointer; }
.clickable.sel { background:#f1f5f9; }
.small { font-size:12px; }
.st { display:inline-block; font-size:12px; font-weight:700; padding:2px 8px; border-radius:999px; margin-right:4px; }
.st-on { background:#dcfce7; color:#166534; }
.st-req { background:#fef3c7; color:#92400e; }
.st-off { background:#f1f5f9; color:#475569; }
.st-blocked { background:#fee2e2; color:#991b1b; }
.st-feat { background:#e0e7ff; color:#3730a3; }
.detail { display:flex; flex-direction:column; gap:10px; }
.detail-head { display:flex; justify-content:space-between; align-items:flex-start; }
.row { display:flex; align-items:center; gap:8px; font-size:14px; }
.grid2 { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
.field { display:flex; flex-direction:column; gap:4px; }
.field > label:first-child { font-size:13px; color:var(--text-muted); font-weight:600; }
.info { display:flex; flex-direction:column; gap:2px; background:#f8fafc; border-radius:8px; padding:8px 10px; }
.app-section { display:flex; flex-direction:column; gap:10px; border-top:1px solid var(--border, #e2e8f0); padding-top:12px; margin-top:4px; }
.inline { display:flex; gap:6px; align-items:center; }
.inline .input { flex:1; min-width:0; }
.field .input + .input, .field .grid2 + .input { margin-top:6px; }
.bad { color:#dc2626; }
.color { width:42px; height:36px; padding:2px; border:1px solid var(--border, #e2e8f0); border-radius:8px; background:#fff; cursor:pointer; }
.mono { font-family: ui-monospace, Consolas, monospace; font-size:12px; }
.preview { display:flex; flex-direction:column; align-items:center; gap:6px; padding:14px; border-radius:12px;
  background:linear-gradient(160deg, #1e293b, #334155); }
.app-icon { width:64px; height:64px; border-radius:16px; display:flex; align-items:center; justify-content:center; overflow:hidden;
  box-shadow:0 2px 6px rgba(0,0,0,.35); }
.app-icon img { width:78%; height:78%; object-fit:contain; }
.app-letter { color:#fff; font-size:28px; font-weight:700; }
.app-label { color:#fff; font-size:12px; max-width:90px; text-align:center; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.hints { margin:0; padding-left:18px; display:flex; flex-direction:column; gap:2px; }
@media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
</style>
