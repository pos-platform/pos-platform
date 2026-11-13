<script setup>
import { ref, reactive, onMounted } from 'vue'
import { onlineMarketingApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'
import ImageInput from './ImageInput.vue'
import LinkPicker from './LinkPicker.vue'
import {
  toLocalInput, fromLocalInput, periodText, stateOf, linkText, validateLink, linkValueFor, moved
} from './marketingUtil'

// Bosh sahifa banner karuseli (9.1).
const props = defineProps({
  products: { type: Array, default: () => [] },
  categories: { type: Array, default: () => [] },
  collections: { type: Array, default: () => [] },
  stores: { type: Array, default: () => [] }
})

const api = onlineMarketingApi.banners
const items = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const showModal = ref(false)
const editingId = ref(null)
const deleteTarget = ref(null)

const blank = () => ({
  title: '',
  subtitle: '',
  imageUrl: '',
  useColor: false,
  backgroundColor: '#16a34a',
  linkType: 'None',
  linkValue: '',
  startsAt: '',
  endsAt: '',
  branchId: '',
  isActive: true
})
const form = reactive(blank())

async function load() {
  loading.value = true
  error.value = ''
  try {
    const list = await api.list()
    items.value = [...list].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
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

function openEdit(b) {
  editingId.value = b.id
  Object.assign(form, {
    title: b.title || '',
    subtitle: b.subtitle || '',
    imageUrl: b.imageUrl || '',
    useColor: !!b.backgroundColor,
    backgroundColor: b.backgroundColor || '#16a34a',
    linkType: b.linkType || 'None',
    linkValue: b.linkValue || '',
    startsAt: toLocalInput(b.startsAt),
    endsAt: toLocalInput(b.endsAt),
    branchId: b.branchId || '',
    isActive: b.isActive
  })
  error.value = ''
  showModal.value = true
}

async function submit() {
  error.value = ''
  success.value = ''
  const title = form.title.trim()
  if (!title) { error.value = 'Sarlavhani kiriting.'; return }
  if (form.useColor && !/^#[0-9a-fA-F]{6}$/.test(form.backgroundColor)) { error.value = 'Rang #RRGGBB ko\'rinishida bo\'lsin.'; return }
  const linkErr = validateLink(form.linkType, form.linkValue)
  if (linkErr) { error.value = linkErr; return }
  if (form.startsAt && form.endsAt && form.startsAt > form.endsAt) { error.value = 'Boshlanish tugashdan keyin bo\'lmasin.'; return }
  const current = items.value.find((x) => x.id === editingId.value)
  const payload = {
    title,
    subtitle: form.subtitle.trim() || null,
    imageUrl: form.imageUrl || null,
    backgroundColor: form.useColor ? form.backgroundColor.toUpperCase() : null,
    linkType: form.linkType,
    linkValue: linkValueFor(form.linkType, form.linkValue),
    startsAt: fromLocalInput(form.startsAt),
    endsAt: fromLocalInput(form.endsAt),
    branchId: form.branchId || null,
    sortOrder: current ? current.sortOrder ?? 0 : items.value.length,
    isActive: form.isActive
  }
  saving.value = true
  try {
    if (editingId.value) await api.update(editingId.value, payload)
    else await api.create(payload)
    success.value = `"${title}" banneri saqlandi`
    showModal.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function toggleActive(b) {
  error.value = ''
  try {
    await api.update(b.id, {
      title: b.title, subtitle: b.subtitle ?? null, imageUrl: b.imageUrl ?? null, backgroundColor: b.backgroundColor ?? null,
      linkType: b.linkType || 'None', linkValue: b.linkValue ?? null, startsAt: b.startsAt ?? null, endsAt: b.endsAt ?? null,
      branchId: b.branchId ?? null, sortOrder: b.sortOrder ?? 0, isActive: !b.isActive
    })
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function move(i, dir) {
  const next = moved(items.value, i, dir)
  if (next === items.value) return
  items.value = next
  try {
    await api.reorder(next.map((x) => x.id))
  } catch (e) {
    error.value = errorMessage(e)
    await load()
  }
}

async function confirmDelete() {
  const b = deleteTarget.value
  if (!b) return
  try {
    await api.remove(b.id)
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
    <div class="muted small">Ilova bosh sahifasidagi karusel. Tartib — ↑↓ tugmalari bilan. Banner bo'lmasa standart "bepul yetkazish" banneri chiqadi.</div>
    <button class="btn" @click="openCreate">+ Yangi banner</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th style="width:70px">Tartib</th>
          <th>Banner</th>
          <th>Havola</th>
          <th>Filial</th>
          <th>Muddat</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="7" class="muted center">Yuklanmoqda...</td></tr>
        <tr v-for="(b, i) in items" v-else :key="b.id">
          <td class="nowrap">
            <button class="btn btn-ghost btn-sm" :disabled="i === 0" @click="move(i, -1)">↑</button>
            <button class="btn btn-ghost btn-sm" :disabled="i === items.length - 1" @click="move(i, 1)">↓</button>
          </td>
          <td>
            <div class="banner-cell">
              <div class="thumb" :style="{ background: b.backgroundColor || '#f1f5f9' }">
                <img v-if="b.imageUrl" :src="mediaUrl(b.imageUrl)" alt="" />
              </div>
              <div>
                <div style="font-weight:700">{{ b.title }}</div>
                <div v-if="b.subtitle" class="muted small">{{ b.subtitle }}</div>
              </div>
            </div>
          </td>
          <td class="small">{{ linkText(b.linkType, b.linkValue, lookups()) }}</td>
          <td class="small">{{ b.branchName || (b.branchId ? '—' : 'Barcha filiallar') }}</td>
          <td class="muted small nowrap">{{ periodText(b.startsAt, b.endsAt) }}</td>
          <td>
            <span class="pill clickable" :class="stateOf(b).cls" title="Yoqish / o'chirish" @click="toggleActive(b)">{{ stateOf(b).text }}</span>
          </td>
          <td class="text-right nowrap">
            <button class="btn btn-ghost btn-sm" @click="openEdit(b)">Tahrirlash</button>
            <button class="btn btn-ghost btn-sm" @click="deleteTarget = b">✕</button>
          </td>
        </tr>
        <tr v-if="!loading && !items.length"><td colspan="7" class="muted center">Banner yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:640px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Bannerni tahrirlash' : 'Yangi banner' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="field">
          <label>Sarlavha</label>
          <input v-model="form.title" class="input" maxlength="80" required />
        </div>
        <div class="field">
          <label>Qo'shimcha matn</label>
          <input v-model="form.subtitle" class="input" maxlength="160" />
        </div>
        <div class="field">
          <label>Rasm</label>
          <ImageInput v-model="form.imageUrl" wide @error="(m) => (error = m)" />
        </div>
        <div class="field">
          <label class="toggle"><input v-model="form.useColor" type="checkbox" /> Fon rangi</label>
          <div v-if="form.useColor" class="color-row">
            <input v-model="form.backgroundColor" type="color" class="color" />
            <input v-model="form.backgroundColor" class="input" maxlength="7" style="max-width:120px" />
          </div>
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
            <label>Boshlanish (bo'sh — darhol)</label>
            <input v-model="form.startsAt" type="datetime-local" class="input" />
          </div>
          <div class="field">
            <label>Tugash (bo'sh — muddatsiz)</label>
            <input v-model="form.endsAt" type="datetime-local" class="input" :min="form.startsAt || undefined" />
          </div>
        </div>

        <div class="field">
          <label>Filial</label>
          <select v-model="form.branchId" class="select">
            <option value="">Barcha filiallar</option>
            <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>

        <label class="toggle" style="margin:6px 0 14px"><input v-model="form.isActive" type="checkbox" /> Faol</label>

        <button class="btn" style="width:100%" :disabled="saving">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </form>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Bannerni o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.title }}</b>" bannerini o'chirasizmi?</p>
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
.clickable { cursor: pointer; }
.tab-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.banner-cell { display: flex; align-items: center; gap: 10px; }
.thumb { width: 96px; height: 44px; border-radius: 8px; overflow: hidden; flex-shrink: 0; }
.thumb img { width: 100%; height: 100%; object-fit: cover; }
.color-row { display: flex; align-items: center; gap: 8px; }
.color { width: 44px; height: 36px; padding: 0; border: 1px solid var(--border); border-radius: 8px; background: none; cursor: pointer; }
</style>
