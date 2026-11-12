<script setup>
import { ref, reactive, onMounted } from 'vue'
import { promoCodesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// Online do'kon promokodlari (mobil ilovada checkout'da kiritiladi).
// Chegirma faqat serverda hisoblanadi — bu yerda faqat sozlash.
const codes = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

const showModal = ref(false)
const editingId = ref(null)
const deleteTarget = ref(null)

const blank = () => ({
  code: '',
  kind: 'percent', // percent | amount
  discountPercent: 10,
  discountAmount: '',
  minOrderAmount: 0,
  validFrom: '',
  validTo: '',
  maxUses: '',
  perUserLimit: 1,
  isActive: true
})
const form = reactive(blank())

const numOrNull = (v) => (v === '' || v === null || v === undefined ? null : Number(v))
const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('uz-UZ') : '')

function periodText(p) {
  if (!p.validFrom && !p.validTo) return 'Muddatsiz'
  return `${fmtDate(p.validFrom) || '…'} – ${fmtDate(p.validTo) || '…'}`
}

function discountText(p) {
  if (p.discountPercent != null) return `−${Number(p.discountPercent)}%`
  return `−${fmtNum(p.discountAmount)} so'm`
}

// Hozir amalda: faol, muddat ichida, limit tugamagan
function stateOf(p) {
  if (!p.isActive) return { text: "O'chiq", cls: 'pill-gray' }
  const now = new Date()
  if (p.validTo && new Date(p.validTo) < now) return { text: 'Muddati tugagan', cls: 'pill-red' }
  if (p.maxUses != null && p.usedCount >= p.maxUses) return { text: 'Limit tugagan', cls: 'pill-red' }
  if (p.validFrom && new Date(p.validFrom) > now) return { text: 'Rejada', cls: 'pill-amber' }
  return { text: 'Amalda', cls: 'pill-green' }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    codes.value = await promoCodesApi.list()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, blank())
  showModal.value = true
}

function openEdit(p) {
  editingId.value = p.id
  Object.assign(form, {
    code: p.code,
    kind: p.discountPercent != null ? 'percent' : 'amount',
    discountPercent: p.discountPercent ?? 10,
    discountAmount: p.discountAmount ?? '',
    minOrderAmount: p.minOrderAmount ?? 0,
    validFrom: p.validFrom ? p.validFrom.substring(0, 10) : '',
    validTo: p.validTo ? p.validTo.substring(0, 10) : '',
    maxUses: p.maxUses ?? '',
    perUserLimit: p.perUserLimit ?? 1,
    isActive: p.isActive
  })
  showModal.value = true
}

async function submit() {
  error.value = ''
  success.value = ''
  const code = form.code.trim().toUpperCase()
  if (!/^[A-Z0-9_-]{3,32}$/.test(code)) {
    error.value = 'Kod 3–32 belgi: lotin harflari, raqam, "-" yoki "_".'
    return
  }
  const pct = form.kind === 'percent' ? numOrNull(form.discountPercent) : null
  const amt = form.kind === 'amount' ? numOrNull(form.discountAmount) : null
  if (form.kind === 'percent' && (pct === null || pct <= 0 || pct > 100)) {
    error.value = 'Chegirma foizi 0 dan katta va 100 dan oshmasligi kerak.'
    return
  }
  if (form.kind === 'amount' && (amt === null || amt <= 0)) {
    error.value = 'Chegirma summasini kiriting.'
    return
  }
  if (form.validFrom && form.validTo && form.validFrom > form.validTo) {
    error.value = 'Boshlanish sanasi tugash sanasidan keyin bo\'lmasin.'
    return
  }
  const payload = {
    code,
    discountPercent: pct,
    discountAmount: amt,
    minOrderAmount: Number(form.minOrderAmount) || 0,
    // Toshkent vaqti (UTC+5) bo'yicha kun boshi/oxiri
    validFrom: form.validFrom ? `${form.validFrom}T00:00:00+05:00` : null,
    validTo: form.validTo ? `${form.validTo}T23:59:59+05:00` : null,
    maxUses: numOrNull(form.maxUses),
    perUserLimit: Math.max(0, Number(form.perUserLimit) || 0),
    isActive: form.isActive
  }
  saving.value = true
  try {
    if (editingId.value) await promoCodesApi.update(editingId.value, payload)
    else await promoCodesApi.create(payload)
    success.value = `"${code}" promokodi saqlandi`
    showModal.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  const p = deleteTarget.value
  if (!p) return
  error.value = ''
  try {
    await promoCodesApi.remove(p.id)
    deleteTarget.value = null
    await load()
  } catch (e) {
    deleteTarget.value = null
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Promokodlar</h1>
      <div class="muted small">Mijoz mobil ilovada buyurtma berishda kiritadi. Chegirma mahsulotlar summasiga qo'llanadi.</div>
    </div>
    <button class="btn" @click="openCreate">+ Yangi promokod</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th>Kod</th>
          <th class="text-right">Chegirma</th>
          <th class="text-right">Min. buyurtma</th>
          <th>Muddat</th>
          <th class="text-right">Ishlatilgan</th>
          <th class="text-right">Mijozga</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="8" class="muted" style="text-align:center; padding:24px">Yuklanmoqda...</td></tr>
        <tr v-for="p in codes" v-else :key="p.id">
          <td><strong class="code">{{ p.code }}</strong></td>
          <td class="text-right"><span class="pill pill-red">{{ discountText(p) }}</span></td>
          <td class="text-right">{{ Number(p.minOrderAmount) > 0 ? fmtNum(p.minOrderAmount) : '—' }}</td>
          <td class="muted nowrap">{{ periodText(p) }}</td>
          <td class="text-right">{{ p.usedCount }}<span class="muted"> / {{ p.maxUses ?? '∞' }}</span></td>
          <td class="text-right muted">{{ p.perUserLimit > 0 ? `${p.perUserLimit} marta` : 'cheklovsiz' }}</td>
          <td><span class="pill" :class="stateOf(p).cls">{{ stateOf(p).text }}</span></td>
          <td class="text-right nowrap">
            <button class="btn btn-ghost btn-sm" @click="openEdit(p)">Tahrirlash</button>
            <button class="btn btn-ghost btn-sm" @click="deleteTarget = p">✕</button>
          </td>
        </tr>
        <tr v-if="!loading && !codes.length">
          <td colspan="8" class="muted" style="text-align:center; padding:24px">Promokod yo'q</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Yaratish / tahrirlash -->
  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:520px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Promokodni tahrirlash' : 'Yangi promokod' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="field">
          <label>Kod</label>
          <input
            :value="form.code"
            class="input code"
            required
            maxlength="32"
            placeholder="Masalan: YOZ2026"
            @input="form.code = $event.target.value.toUpperCase()"
          />
        </div>

        <div class="field">
          <label>Chegirma turi</label>
          <div class="toggle-row">
            <label class="toggle"><input v-model="form.kind" type="radio" value="percent" /> Foiz (%)</label>
            <label class="toggle"><input v-model="form.kind" type="radio" value="amount" /> Summa (so'm)</label>
          </div>
        </div>

        <div class="row">
          <div v-if="form.kind === 'percent'" class="field">
            <label>Chegirma, %</label>
            <input v-model="form.discountPercent" type="number" min="0.01" max="100" step="any" class="input" required />
          </div>
          <div v-else class="field">
            <label>Chegirma, so'm</label>
            <input v-model="form.discountAmount" type="number" min="1" step="any" class="input" required />
          </div>
          <div class="field">
            <label>Minimal buyurtma, so'm</label>
            <input v-model="form.minOrderAmount" type="number" min="0" class="input" />
          </div>
        </div>

        <div class="row">
          <div class="field">
            <label>Boshlanish (bo'sh — darhol)</label>
            <input v-model="form.validFrom" type="date" class="input" />
          </div>
          <div class="field">
            <label>Tugash (bo'sh — muddatsiz)</label>
            <input v-model="form.validTo" type="date" class="input" :min="form.validFrom || undefined" />
          </div>
        </div>

        <div class="row">
          <div class="field">
            <label>Jami ishlatish limiti (bo'sh — cheklovsiz)</label>
            <input v-model="form.maxUses" type="number" min="1" class="input" />
          </div>
          <div class="field">
            <label>Bir mijozga (0 — cheklovsiz)</label>
            <input v-model="form.perUserLimit" type="number" min="0" class="input" />
          </div>
        </div>

        <label class="toggle" style="margin:6px 0 14px"><input v-model="form.isActive" type="checkbox" /> Faol</label>

        <button class="btn" style="width:100%" :disabled="saving">
          {{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}
        </button>
      </form>
    </div>
  </div>

  <!-- O'chirishni tasdiqlash -->
  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Promokodni o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.code }}</b>" promokodini o'chirasizmi? Avval berilgan buyurtmalardagi chegirma o'zgarmaydi.</p>
        <div class="del-actions">
          <button class="btn btn-ghost" @click="deleteTarget = null">Bekor qilish</button>
          <button class="btn btn-danger" @click="confirmDelete">O'chirish</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.nowrap { white-space: nowrap; }
.code { font-family: ui-monospace, Consolas, monospace; letter-spacing: 0.04em; }
</style>
