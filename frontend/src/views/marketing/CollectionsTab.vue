<script setup>
import { ref, reactive, onMounted } from 'vue'
import { onlineMarketingApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'
import ImageInput from './ImageInput.vue'
import ProductSearch from './ProductSearch.vue'
import { toLocalInput, fromLocalInput, periodText, stateOf, fmtNum, moved } from './marketingUtil'
import AppIcon from '../../components/AppIcon.vue'

// Kolleksiyalar ("Yozgi tanlov", "Nonushta uchun" ...) — 9.1.
defineProps({
  products: { type: Array, default: () => [] },
  stores: { type: Array, default: () => [] }
})
const emit = defineEmits(['changed'])

const api = onlineMarketingApi.collections
const items = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const showModal = ref(false)
const editingId = ref(null)
const deleteTarget = ref(null)
const detailLoading = ref(false)

const blank = () => ({
  title: '',
  description: '',
  imageUrl: '',
  startsAt: '',
  endsAt: '',
  branchId: '',
  sortOrder: 0,
  showOnHome: true,
  isActive: true,
  products: [] // [{ id, name, imageUrl, price }]
})
const form = reactive(blank())

const countOf = (c) => c.products?.length ?? c.productCount ?? c.productIds?.length ?? '—'

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
  Object.assign(form, blank(), { sortOrder: items.value.length })
  error.value = ''
  showModal.value = true
}

function fill(c) {
  Object.assign(form, {
    title: c.title || '',
    description: c.description || '',
    imageUrl: c.imageUrl || '',
    startsAt: toLocalInput(c.startsAt),
    endsAt: toLocalInput(c.endsAt),
    branchId: c.branchId || '',
    sortOrder: c.sortOrder ?? 0,
    showOnHome: c.showOnHome !== false,
    isActive: c.isActive,
    products: (c.products || []).map((p) => ({ id: p.id, name: p.name, imageUrl: p.imageUrl, price: p.price }))
  })
}

async function openEdit(c) {
  editingId.value = c.id
  error.value = ''
  fill(c)
  showModal.value = true
  detailLoading.value = true
  try {
    const full = await api.get(c.id)
    if (editingId.value === c.id) fill(full)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

function addProduct(p) {
  if (form.products.some((x) => x.id === p.id)) return
  form.products.push({ id: p.id, name: p.name, imageUrl: p.imageUrl, price: p.price })
}
function moveProduct(i, dir) {
  form.products = moved(form.products, i, dir)
}

async function submit() {
  error.value = ''
  success.value = ''
  const title = form.title.trim()
  if (!title) { error.value = 'Nomini kiriting.'; return }
  if (form.startsAt && form.endsAt && form.startsAt > form.endsAt) { error.value = 'Boshlanish tugashdan keyin bo\'lmasin.'; return }
  if (!form.products.length) { error.value = 'Kamida bitta mahsulot qo\'shing.'; return }
  const payload = {
    title,
    description: form.description.trim() || null,
    imageUrl: form.imageUrl || null,
    startsAt: fromLocalInput(form.startsAt),
    endsAt: fromLocalInput(form.endsAt),
    branchId: form.branchId || null,
    sortOrder: Number(form.sortOrder) || 0,
    showOnHome: form.showOnHome,
    isActive: form.isActive,
    productIds: form.products.map((p) => p.id)
  }
  saving.value = true
  try {
    if (editingId.value) await api.update(editingId.value, payload)
    else await api.create(payload)
    success.value = `"${title}" kolleksiyasi saqlandi`
    showModal.value = false
    await load()
    emit('changed')
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  const c = deleteTarget.value
  if (!c) return
  try {
    await api.remove(c.id)
    deleteTarget.value = null
    await load()
    emit('changed')
  } catch (e) {
    deleteTarget.value = null
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <div class="tab-head">
    <div class="muted small">Tanlangan mahsulotlar to'plami. "Bosh sahifada" belgilanganlari ilova bosh sahifasida lenta bo'lib chiqadi.</div>
    <button class="btn" @click="openCreate">+ Yangi kolleksiya</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th class="text-right" style="width:60px">№</th>
          <th>Kolleksiya</th>
          <th class="text-right">Mahsulotlar</th>
          <th>Bosh sahifada</th>
          <th>Filial</th>
          <th>Muddat</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="8" class="muted center">Yuklanmoqda...</td></tr>
        <tr v-for="c in items" v-else :key="c.id">
          <td class="text-right muted">{{ c.sortOrder }}</td>
          <td>
            <div class="cell">
              <div class="thumb"><img v-if="c.imageUrl" :src="mediaUrl(c.imageUrl)" alt="" /></div>
              <div>
                <div style="font-weight:700">{{ c.title }}</div>
                <div v-if="c.description" class="muted small">{{ c.description }}</div>
              </div>
            </div>
          </td>
          <td class="text-right">{{ countOf(c) }}</td>
          <td>{{ c.showOnHome ? 'Ha' : 'Yo\'q' }}</td>
          <td class="small">{{ c.branchName || (c.branchId ? '—' : 'Barcha filiallar') }}</td>
          <td class="muted small nowrap">{{ periodText(c.startsAt, c.endsAt) }}</td>
          <td><span class="pill" :class="stateOf(c).cls">{{ stateOf(c).text }}</span></td>
          <td class="text-right nowrap">
            <div class="row-actions">
              <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(c)"><AppIcon name="edit" :size="15" /></button>
              <button class="btn btn-icon danger" title="O'chirish" @click="deleteTarget = c"><AppIcon name="trash" :size="15" /></button>
            </div>
          </td>
        </tr>
        <tr v-if="!loading && !items.length"><td colspan="8" class="muted center">Kolleksiya yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:680px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Kolleksiyani tahrirlash' : 'Yangi kolleksiya' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="row">
          <div class="field" style="flex:3">
            <label>Nomi</label>
            <input v-model="form.title" class="input" maxlength="80" required />
          </div>
          <div class="field" style="flex:1">
            <label>Tartib raqami</label>
            <input v-model="form.sortOrder" type="number" min="0" class="input" />
          </div>
        </div>
        <div class="field">
          <label>Tavsif</label>
          <textarea v-model="form.description" class="input" rows="2" maxlength="500"></textarea>
        </div>
        <div class="field">
          <label>Rasm</label>
          <ImageInput v-model="form.imageUrl" @error="(m) => (error = m)" />
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

        <div class="field">
          <label>Filial</label>
          <select v-model="form.branchId" class="select">
            <option value="">Barcha filiallar</option>
            <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>

        <div class="toggle-row">
          <label class="toggle"><input v-model="form.showOnHome" type="checkbox" /> Bosh sahifada</label>
          <label class="toggle"><input v-model="form.isActive" type="checkbox" /> Faol</label>
        </div>

        <div class="field" style="margin-top:8px">
          <label>Mahsulotlar ({{ form.products.length }}) <span v-if="detailLoading" class="muted">· yuklanmoqda...</span></label>
          <ProductSearch :products="products" :exclude="form.products.map((p) => p.id)" @select="addProduct" />
          <div class="plist">
            <div v-for="(p, i) in form.products" :key="p.id" class="pitem">
              <span class="pnum muted">{{ i + 1 }}</span>
              <span class="pimg"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></span>
              <span class="pname">{{ p.name }}</span>
              <span class="muted small">{{ p.price != null ? fmtNum(p.price) : '' }}</span>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === 0" @click="moveProduct(i, -1)">↑</button>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === form.products.length - 1" @click="moveProduct(i, 1)">↓</button>
              <button type="button" class="btn btn-icon danger" title="Olib tashlash" @click="form.products.splice(i, 1)"><AppIcon name="x" :size="15" /></button>
            </div>
            <div v-if="!form.products.length" class="muted small" style="padding:8px 0">Mahsulot qo'shilmagan</div>
          </div>
        </div>

        <button class="btn" style="width:100%" :disabled="saving || detailLoading">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </form>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Kolleksiyani o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.title }}</b>" kolleksiyasini o'chirasizmi? Unga bog'langan banner havolalari ishlamay qoladi.</p>
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
.thumb { width: 44px; height: 44px; border-radius: 8px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.thumb img { width: 100%; height: 100%; object-fit: cover; }
.plist { max-height: 300px; overflow: auto; margin-top: 6px; }
.pitem { display: flex; align-items: center; gap: 6px; padding: 4px 0; border-bottom: 1px solid var(--border); font-size: 13px; }
.pnum { width: 22px; text-align: right; }
.pimg { width: 28px; height: 28px; border-radius: 6px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.pimg img { width: 100%; height: 100%; object-fit: cover; }
.pname { flex: 1; font-weight: 600; }
</style>
