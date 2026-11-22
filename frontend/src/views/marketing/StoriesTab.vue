<script setup>
import { ref, reactive, onMounted } from 'vue'
import { onlineMarketingApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'
import ImageInput from './ImageInput.vue'
import LinkPicker from './LinkPicker.vue'
import AppIcon from '../../components/AppIcon.vue'
import {
  toLocalInput, fromLocalInput, periodText, stateOf, linkText, validateLink, linkValueFor, moved
} from './marketingUtil'

// Stories (9.3): muqova + slaydlar (rasm, izoh, havola, davomiylik).
const props = defineProps({
  products: { type: Array, default: () => [] },
  categories: { type: Array, default: () => [] },
  collections: { type: Array, default: () => [] },
  stores: { type: Array, default: () => [] }
})

const api = onlineMarketingApi.stories
const items = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const showModal = ref(false)
const editingId = ref(null)
const deleteTarget = ref(null)
const detailLoading = ref(false)

const blankSlide = () => ({ id: null, imageUrl: '', caption: '', linkType: 'None', linkValue: '', durationSec: 6 })
const blank = () => ({
  title: '',
  coverImageUrl: '',
  startsAt: '',
  endsAt: '',
  branchId: '',
  isActive: true,
  slides: [blankSlide()]
})
const form = reactive(blank())

const slideCount = (s) => s.slides?.length ?? s.slideCount ?? '—'

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

function fill(s) {
  Object.assign(form, {
    title: s.title || '',
    coverImageUrl: s.coverImageUrl || '',
    startsAt: toLocalInput(s.startsAt),
    endsAt: toLocalInput(s.endsAt),
    branchId: s.branchId || '',
    isActive: s.isActive,
    slides: [...(s.slides || [])]
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
      .map((x) => ({
        id: x.id || null,
        imageUrl: x.imageUrl || '',
        caption: x.caption || '',
        linkType: x.linkType || 'None',
        linkValue: x.linkValue || '',
        durationSec: x.durationSec ?? 6
      }))
  })
}

async function openEdit(s) {
  editingId.value = s.id
  error.value = ''
  fill(s)
  showModal.value = true
  detailLoading.value = true
  try {
    const full = await api.get(s.id)
    if (editingId.value === s.id) fill(full)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

function moveSlide(i, dir) {
  form.slides = moved(form.slides, i, dir)
}

async function submit() {
  error.value = ''
  success.value = ''
  const title = form.title.trim()
  if (!title) { error.value = 'Sarlavhani kiriting.'; return }
  if (!form.coverImageUrl) { error.value = 'Muqova rasmini yuklang.'; return }
  if (form.startsAt && form.endsAt && form.startsAt > form.endsAt) { error.value = 'Boshlanish tugashdan keyin bo\'lmasin.'; return }
  if (!form.slides.length) { error.value = 'Kamida bitta slayd qo\'shing.'; return }
  for (const [i, sl] of form.slides.entries()) {
    if (!sl.imageUrl) { error.value = `${i + 1}-slayd: rasm yuklang.`; return }
    const d = Number(sl.durationSec)
    if (!Number.isInteger(d) || d < 3 || d > 15) { error.value = `${i + 1}-slayd: davomiylik 3–15 soniya.`; return }
    const le = validateLink(sl.linkType, sl.linkValue)
    if (le) { error.value = `${i + 1}-slayd: ${le}`; return }
  }
  const current = items.value.find((x) => x.id === editingId.value)
  const payload = {
    title,
    coverImageUrl: form.coverImageUrl,
    startsAt: fromLocalInput(form.startsAt),
    endsAt: fromLocalInput(form.endsAt),
    branchId: form.branchId || null,
    sortOrder: current ? current.sortOrder ?? 0 : items.value.length,
    isActive: form.isActive,
    slides: form.slides.map((sl, i) => ({
      id: sl.id || undefined,
      imageUrl: sl.imageUrl,
      caption: sl.caption.trim() || null,
      linkType: sl.linkType,
      linkValue: linkValueFor(sl.linkType, sl.linkValue),
      durationSec: Number(sl.durationSec),
      sortOrder: i
    }))
  }
  saving.value = true
  try {
    if (editingId.value) await api.update(editingId.value, payload)
    else await api.create(payload)
    success.value = `"${title}" story saqlandi`
    showModal.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
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
  const s = deleteTarget.value
  if (!s) return
  try {
    await api.remove(s.id)
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
    <div class="muted small">Bosh sahifadagi doirachalar. Mijoz bosganda slaydlar to'liq ekranda ketma-ket ko'rsatiladi.</div>
    <button class="btn" @click="openCreate">+ Yangi story</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card tbl-main" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th style="width:70px">Tartib</th>
          <th>Story</th>
          <th class="text-right">Slaydlar</th>
          <th>Filial</th>
          <th>Muddat</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="7" class="muted center">Yuklanmoqda...</td></tr>
        <tr v-for="(s, i) in items" v-else :key="s.id">
          <td class="nowrap">
            <button class="btn btn-ghost btn-sm" :disabled="i === 0" @click="move(i, -1)">↑</button>
            <button class="btn btn-ghost btn-sm" :disabled="i === items.length - 1" @click="move(i, 1)">↓</button>
          </td>
          <td>
            <div class="cell">
              <div class="cover"><img v-if="s.coverImageUrl" :src="mediaUrl(s.coverImageUrl)" alt="" /></div>
              <b>{{ s.title }}</b>
            </div>
          </td>
          <td class="text-right">{{ slideCount(s) }}</td>
          <td class="small">{{ s.branchName || (s.branchId ? '—' : 'Barcha filiallar') }}</td>
          <td class="muted small nowrap">{{ periodText(s.startsAt, s.endsAt) }}</td>
          <td><span class="pill" :class="stateOf(s).cls">{{ stateOf(s).text }}</span></td>
          <td class="text-right nowrap">
            <div class="row-actions">
              <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(s)"><AppIcon name="edit" :size="15" /></button>
              <button class="btn btn-icon danger" title="O'chirish" @click="deleteTarget = s"><AppIcon name="trash" :size="15" /></button>
            </div>
          </td>
        </tr>
        <tr v-if="!loading && !items.length"><td colspan="7" class="muted center">Story yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:720px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Storyni tahrirlash' : 'Yangi story' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="row">
          <div class="field">
            <label>Sarlavha (doiracha ostida)</label>
            <input v-model="form.title" class="input" maxlength="40" required />
          </div>
          <div class="field">
            <label>Filial</label>
            <select v-model="form.branchId" class="select">
              <option value="">Barcha filiallar</option>
              <option v-for="st in stores" :key="st.id" :value="st.id">{{ st.name }}</option>
            </select>
          </div>
        </div>
        <div class="field">
          <label>Muqova</label>
          <ImageInput v-model="form.coverImageUrl" @error="(m) => (error = m)" />
        </div>
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
        <label class="toggle" style="margin:0 0 10px"><input v-model="form.isActive" type="checkbox" /> Faol</label>

        <div class="sec-title">Slaydlar <span v-if="detailLoading" class="muted small">· yuklanmoqda...</span></div>
        <div v-for="(sl, i) in form.slides" :key="i" class="slide">
          <div class="slide-head">
            <b>{{ i + 1 }}-slayd</b>
            <span>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === 0" @click="moveSlide(i, -1)">↑</button>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === form.slides.length - 1" @click="moveSlide(i, 1)">↓</button>
              <button type="button" class="btn btn-icon danger" title="Olib tashlash" @click="form.slides.splice(i, 1)"><AppIcon name="x" :size="15" /></button>
            </span>
          </div>
          <div class="field">
            <label>Rasm (vertikal)</label>
            <ImageInput v-model="sl.imageUrl" @error="(m) => (error = m)" />
          </div>
          <div class="row">
            <div class="field" style="flex:3">
              <label>Izoh</label>
              <input v-model="sl.caption" class="input" maxlength="200" />
            </div>
            <div class="field" style="flex:1">
              <label>Davomiylik, s (3–15)</label>
              <input v-model.number="sl.durationSec" type="number" min="3" max="15" class="input" />
            </div>
          </div>
          <LinkPicker
            v-model:link-type="sl.linkType"
            v-model:link-value="sl.linkValue"
            :products="products"
            :categories="categories"
            :collections="collections"
          />
          <div v-if="sl.linkType && sl.linkType !== 'None'" class="muted small">{{ linkText(sl.linkType, sl.linkValue, lookups()) }}</div>
        </div>
        <button type="button" class="btn btn-ghost" style="margin-bottom:14px" @click="form.slides.push(blankSlide())">+ Slayd qo'shish</button>

        <button class="btn" style="width:100%" :disabled="saving || detailLoading">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </form>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Storyni o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.title }}</b>" story'sini o'chirasizmi?</p>
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
.cell { display: flex; align-items: center; gap: 10px; }
.cover { width: 44px; height: 44px; border-radius: 50%; background: #f1f5f9; overflow: hidden; flex-shrink: 0; border: 2px solid var(--primary); }
.cover img { width: 100%; height: 100%; object-fit: cover; }
.sec-title { font-weight: 700; font-size: 14px; margin: 6px 0 8px; }
.slide { border: 1px solid var(--border); border-radius: 10px; padding: 10px 12px; margin-bottom: 10px; }
.slide-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
</style>
