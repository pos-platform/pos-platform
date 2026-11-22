<script setup>
import { ref, reactive, onMounted } from 'vue'
import { onlineMarketingApi } from '../../api/catalog'
import { errorMessage } from '../../api/http'
import LinkPicker from './LinkPicker.vue'
import AppIcon from '../../components/AppIcon.vue'
import {
  toLocalInput, fromLocalInput, fmtDateTime, fmtNum, linkText, validateLink, linkValueFor
} from './marketingUtil'

// Push kampaniyalar (9.4): mijozlar qurilmalariga xabar. Kuniga ko'pi bilan 3 ta (server tekshiradi).
const props = defineProps({
  products: { type: Array, default: () => [] },
  categories: { type: Array, default: () => [] },
  collections: { type: Array, default: () => [] },
  stores: { type: Array, default: () => [] }
})

const STATUS = {
  Draft: { text: 'Qoralama', cls: 'pill-gray' },
  Scheduled: { text: 'Rejalashtirilgan', cls: 'pill-amber' },
  Sending: { text: 'Yuborilmoqda', cls: 'pill-amber' },
  Sent: { text: 'Yuborildi', cls: 'pill-green' },
  Failed: { text: 'Xato', cls: 'pill-red' }
}
const statusOf = (s) => STATUS[s] || { text: s, cls: 'pill-gray' }
const editable = (c) => c.status === 'Draft' || c.status === 'Scheduled'

const api = onlineMarketingApi.pushCampaigns
const items = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const showModal = ref(false)
const editingId = ref(null)
const deleteTarget = ref(null)
const sendTarget = ref(null)
const sending = ref(false)

const blank = () => ({ title: '', body: '', linkType: 'None', linkValue: '', branchId: '', scheduledAt: '' })
const form = reactive(blank())

async function load() {
  loading.value = true
  error.value = ''
  try {
    const list = await api.list()
    items.value = [...list].sort((a, b) => new Date(b.createdAt || b.scheduledAt || 0) - new Date(a.createdAt || a.scheduledAt || 0))
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, blank())
  error.value = ''
  showModal.value = true
}

function openEdit(c) {
  editingId.value = c.id
  Object.assign(form, {
    title: c.title || '',
    body: c.body || '',
    linkType: c.linkType || 'None',
    linkValue: c.linkValue || '',
    branchId: c.branchId || '',
    scheduledAt: toLocalInput(c.scheduledAt)
  })
  error.value = ''
  showModal.value = true
}

async function submit() {
  error.value = ''
  success.value = ''
  const title = form.title.trim()
  const body = form.body.trim()
  if (!title || !body) { error.value = 'Sarlavha va matnni kiriting.'; return }
  const le = validateLink(form.linkType, form.linkValue)
  if (le) { error.value = le; return }
  const scheduledAt = fromLocalInput(form.scheduledAt)
  if (scheduledAt && new Date(scheduledAt) < new Date()) { error.value = 'Yuborish vaqti kelajakda bo\'lsin (yoki bo\'sh — qoralama).'; return }
  const payload = {
    title,
    body,
    linkType: form.linkType,
    linkValue: linkValueFor(form.linkType, form.linkValue),
    branchId: form.branchId || null,
    scheduledAt
  }
  saving.value = true
  try {
    if (editingId.value) await api.update(editingId.value, payload)
    else await api.create(payload)
    success.value = scheduledAt ? `"${title}" ${fmtDateTime(scheduledAt)} da yuboriladi` : `"${title}" qoralama sifatida saqlandi`
    showModal.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function confirmSend() {
  const c = sendTarget.value
  if (!c) return
  sending.value = true
  error.value = ''
  success.value = ''
  try {
    await api.send(c.id)
    success.value = `"${c.title}" yuborilmoqda`
    sendTarget.value = null
    await load()
  } catch (e) {
    sendTarget.value = null
    error.value = errorMessage(e)
  } finally {
    sending.value = false
  }
}

async function confirmDelete() {
  const c = deleteTarget.value
  if (!c) return
  try {
    await api.remove(c.id)
    deleteTarget.value = null
    await load()
  } catch (e) {
    deleteTarget.value = null
    error.value = errorMessage(e)
  }
}

const lookups = () => ({ products: props.products, categories: props.categories, collections: props.collections })

onMounted(load)
</script>

<template>
  <div class="tab-head">
    <div class="muted small">
      Xabar shu tashkilot mijozlariga (bonus karta bog'langan yoki buyurtma bergan) yuboriladi; filial tanlansa — shu filialda buyurtma berganlarga.
      Kuniga ko'pi bilan 3 ta kampaniya.
    </div>
    <div class="nowrap">
      <button class="btn btn-ghost btn-sm" :disabled="loading" @click="load">↻</button>
      <button class="btn" @click="openCreate">+ Yangi xabar</button>
    </div>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card tbl-main" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th>Xabar</th>
          <th>Havola</th>
          <th>Filial</th>
          <th>Vaqt</th>
          <th>Holat</th>
          <th class="text-right">Yuborildi</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="7" class="muted center">Yuklanmoqda...</td></tr>
        <tr v-for="c in items" v-else :key="c.id">
          <td>
            <div style="font-weight:700">{{ c.title }}</div>
            <div class="muted small body">{{ c.body }}</div>
          </td>
          <td class="small">{{ linkText(c.linkType, c.linkValue, lookups()) }}</td>
          <td class="small">{{ c.branchName || (c.branchId ? '—' : 'Barcha filiallar') }}</td>
          <td class="small nowrap">
            <template v-if="c.sentAt">{{ fmtDateTime(c.sentAt) }}</template>
            <template v-else-if="c.scheduledAt">{{ fmtDateTime(c.scheduledAt) }} ga</template>
            <span v-else class="muted">—</span>
          </td>
          <td><span class="pill" :class="statusOf(c.status).cls">{{ statusOf(c.status).text }}</span></td>
          <td class="text-right">{{ c.status === 'Sent' || c.sentCount ? fmtNum(c.sentCount) : '—' }}</td>
          <td class="text-right nowrap">
            <template v-if="editable(c)">
              <div class="row-actions">
                <button class="btn btn-sm" @click="sendTarget = c">Hozir yuborish</button>
                <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(c)"><AppIcon name="edit" :size="15" /></button>
                <button class="btn btn-icon danger" title="O'chirish" @click="deleteTarget = c"><AppIcon name="trash" :size="15" /></button>
              </div>
            </template>
          </td>
        </tr>
        <tr v-if="!loading && !items.length"><td colspan="7" class="muted center">Xabar yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:600px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Xabarni tahrirlash' : 'Yangi push xabar' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="field">
          <label>Sarlavha ({{ form.title.length }}/60)</label>
          <input v-model="form.title" class="input" maxlength="60" required />
        </div>
        <div class="field">
          <label>Matn ({{ form.body.length }}/200)</label>
          <textarea v-model="form.body" class="input" rows="3" maxlength="200" required></textarea>
        </div>

        <LinkPicker
          v-model:link-type="form.linkType"
          v-model:link-value="form.linkValue"
          :products="products"
          :categories="categories"
          :collections="collections"
        />

        <div class="row">
          <div class="field">
            <label>Filial</label>
            <select v-model="form.branchId" class="select">
              <option value="">Barcha mijozlar</option>
              <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
          <div class="field">
            <label>Yuborish vaqti (bo'sh — qoralama)</label>
            <input v-model="form.scheduledAt" type="datetime-local" class="input" />
          </div>
        </div>

        <div class="preview">
          <div class="pv-title">{{ form.title || 'Sarlavha' }}</div>
          <div class="pv-body">{{ form.body || 'Xabar matni' }}</div>
        </div>

        <button class="btn" style="width:100%" :disabled="saving">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </form>
    </div>
  </div>

  <div v-if="sendTarget" class="modal-overlay" @click.self="sendTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Hozir yuborish</h3>
        <button class="modal-x" @click="sendTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ sendTarget.title }}</b>" xabarini hozir mijozlarga yuborasizmi? Bu amalni qaytarib bo'lmaydi.</p>
        <div class="del-actions">
          <button class="btn btn-ghost" @click="sendTarget = null">Bekor qilish</button>
          <button class="btn" :disabled="sending" @click="confirmSend">{{ sending ? '...' : 'Yuborish' }}</button>
        </div>
      </div>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Xabarni o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.title }}</b>" xabarini o'chirasizmi?</p>
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
.center { text-align: center; padding: 24px; }
.tab-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.body { max-width: 360px; white-space: pre-line; }
.preview { background: #f1f5f9; border-radius: 12px; padding: 10px 12px; margin-bottom: 14px; }
.pv-title { font-weight: 700; font-size: 14px; }
.pv-body { font-size: 13px; color: #334155; white-space: pre-line; }
</style>
