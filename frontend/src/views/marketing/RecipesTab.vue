<script setup>
import { ref, reactive, onMounted } from 'vue'
import { onlineMarketingApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'
import ImageInput from './ImageInput.vue'
import ProductSearch from './ProductSearch.vue'
import { fmtNum, moved } from './marketingUtil'
import AppIcon from '../../components/AppIcon.vue'

// Retseptlar (bosqich 11.8, docs/mobile/CONTRACT-11.md §1): ilovada "Bugun nima pishiramiz?" va "hammasini savatga".
defineProps({
  products: { type: Array, default: () => [] },
  stores: { type: Array, default: () => [] }
})

const api = onlineMarketingApi.recipes
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
  cookMinutes: '',
  servings: '',
  branchId: '',
  sortOrder: 0,
  showOnHome: true,
  isActive: true,
  steps: [''],
  items: [] // [{ productId, name, imageUrl, price, isWeighted, quantity, note }]
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
  Object.assign(form, blank(), { sortOrder: items.value.length })
  error.value = ''
  showModal.value = true
}

function fill(r) {
  Object.assign(form, {
    title: r.title || '',
    description: r.description || '',
    imageUrl: r.imageUrl || '',
    cookMinutes: r.cookMinutes ?? '',
    servings: r.servings ?? '',
    branchId: r.branchId || '',
    sortOrder: r.sortOrder ?? 0,
    showOnHome: !!r.showOnHome,
    isActive: r.isActive,
    steps: r.steps?.length ? [...r.steps] : [''],
    items: (r.items || []).map((i) => ({ ...i }))
  })
}

async function openEdit(r) {
  editingId.value = r.id
  error.value = ''
  fill(r)
  showModal.value = true
  detailLoading.value = true
  try {
    const full = await api.get(r.id)
    if (editingId.value === r.id) fill(full)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    detailLoading.value = false
  }
}

function addProduct(p) {
  if (form.items.some((x) => x.productId === p.id)) return
  form.items.push({
    productId: p.id,
    name: p.name,
    imageUrl: p.imageUrl,
    price: p.price,
    isWeighted: !!(p.isWeighed ?? p.isWeighted),
    quantity: 1,
    note: ''
  })
}
const moveItem = (i, dir) => { form.items = moved(form.items, i, dir) }
const moveStep = (i, dir) => { form.steps = moved(form.steps, i, dir) }

const numOrNull = (v) => (v === '' || v == null ? null : Number(v))

async function submit() {
  error.value = ''
  success.value = ''
  const title = form.title.trim()
  if (!title) { error.value = 'Nomini kiriting.'; return }
  if (!form.items.length) { error.value = 'Kamida bitta mahsulot qo\'shing.'; return }
  if (form.items.some((i) => !(Number(i.quantity) > 0))) { error.value = 'Miqdor 0 dan katta bo\'lsin.'; return }
  const payload = {
    title,
    description: form.description.trim() || null,
    imageUrl: form.imageUrl || null,
    cookMinutes: numOrNull(form.cookMinutes),
    servings: numOrNull(form.servings),
    branchId: form.branchId || null,
    sortOrder: Number(form.sortOrder) || 0,
    showOnHome: form.showOnHome,
    isActive: form.isActive,
    steps: form.steps.map((s) => s.trim()).filter(Boolean),
    items: form.items.map((i) => ({ productId: i.productId, quantity: Number(i.quantity), note: (i.note || '').trim() || null }))
  }
  saving.value = true
  try {
    if (editingId.value) await api.update(editingId.value, payload)
    else await api.create(payload)
    success.value = `"${title}" retsepti saqlandi`
    showModal.value = false
    await load()
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  const r = deleteTarget.value
  if (!r) return
  try {
    await api.remove(r.id)
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
  <div class="tab-head">
    <div class="muted small">Retsept — ingrediyentlar ro'yxati. Mijoz ilovada bitta tugma bilan hammasini savatga qo'shadi. "Bosh sahifada" belgilangani "Bugun nima pishiramiz?" blokida chiqadi.</div>
    <button class="btn" @click="openCreate">+ Yangi retsept</button>
  </div>

  <div v-if="error && !showModal" class="alert alert-error">{{ error }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <div class="card tbl-main" style="overflow:auto">
    <table class="table">
      <thead>
        <tr>
          <th class="text-right" style="width:60px">№</th>
          <th>Retsept</th>
          <th class="text-right">Mahsulotlar</th>
          <th>Vaqt / porsiya</th>
          <th>Bosh sahifada</th>
          <th>Filial</th>
          <th>Holat</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading"><td colspan="8" class="muted center">Yuklanmoqda...</td></tr>
        <tr v-for="r in items" v-else :key="r.id">
          <td class="text-right muted">{{ r.sortOrder }}</td>
          <td>
            <div class="cell">
              <div class="thumb"><img v-if="r.imageUrl" :src="mediaUrl(r.imageUrl)" alt="" /></div>
              <div>
                <div style="font-weight:700">{{ r.title }}</div>
                <div v-if="r.description" class="muted small clamp">{{ r.description }}</div>
              </div>
            </div>
          </td>
          <td class="text-right">{{ r.items?.length ?? 0 }}</td>
          <td class="small nowrap">{{ r.cookMinutes ? r.cookMinutes + ' daq' : '—' }} · {{ r.servings ? r.servings + ' kishi' : '—' }}</td>
          <td>{{ r.showOnHome ? 'Ha' : 'Yo\'q' }}</td>
          <td class="small">{{ r.branchName || (r.branchId ? '—' : 'Barcha filiallar') }}</td>
          <td><span class="pill" :class="r.isActive ? 'pill-green' : 'pill-gray'">{{ r.isActive ? 'Faol' : 'O\'chirilgan' }}</span></td>
          <td class="text-right nowrap">
            <div class="row-actions">
              <button class="btn btn-icon" title="Tahrirlash" @click="openEdit(r)"><AppIcon name="edit" :size="15" /></button>
              <button class="btn btn-icon danger" title="O'chirish" @click="deleteTarget = r"><AppIcon name="trash" :size="15" /></button>
            </div>
          </td>
        </tr>
        <tr v-if="!loading && !items.length"><td colspan="8" class="muted center">Retsept yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
    <div class="modal-box" style="max-width:760px">
      <div class="modal-head">
        <h3>{{ editingId ? 'Retseptni tahrirlash' : 'Yangi retsept' }}</h3>
        <button class="modal-x" @click="showModal = false">×</button>
      </div>
      <form class="modal-body" @submit.prevent="submit">
        <div v-if="error" class="alert alert-error">{{ error }}</div>

        <div class="row">
          <div class="field" style="flex:3">
            <label>Nomi</label>
            <input v-model="form.title" class="input" maxlength="80" required placeholder="Masalan: Toshkent oshi" />
          </div>
          <div class="field" style="flex:1">
            <label>Tartib raqami</label>
            <input v-model="form.sortOrder" type="number" min="0" class="input" />
          </div>
        </div>
        <div class="field">
          <label>Tavsif</label>
          <textarea v-model="form.description" class="input" rows="2" maxlength="1000"></textarea>
        </div>
        <div class="field">
          <label>Rasm</label>
          <ImageInput v-model="form.imageUrl" @error="(m) => (error = m)" />
        </div>

        <div class="row">
          <div class="field">
            <label>Tayyorlash vaqti (daqiqa)</label>
            <input v-model="form.cookMinutes" type="number" min="1" max="1440" class="input" />
          </div>
          <div class="field">
            <label>Porsiya (kishi)</label>
            <input v-model="form.servings" type="number" min="1" max="50" class="input" />
          </div>
          <div class="field">
            <label>Filial</label>
            <select v-model="form.branchId" class="select">
              <option value="">Barcha filiallar</option>
              <option v-for="s in stores" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
        </div>

        <div class="toggle-row">
          <label class="toggle"><input v-model="form.showOnHome" type="checkbox" /> Bosh sahifada</label>
          <label class="toggle"><input v-model="form.isActive" type="checkbox" /> Faol</label>
        </div>

        <div class="field" style="margin-top:8px">
          <label>Ingrediyentlar ({{ form.items.length }}) <span v-if="detailLoading" class="muted">· yuklanmoqda...</span></label>
          <ProductSearch :products="products" :exclude="form.items.map((p) => p.productId)" @select="addProduct" />
          <div class="plist">
            <div v-for="(p, i) in form.items" :key="p.productId" class="pitem">
              <span class="pnum muted">{{ i + 1 }}</span>
              <span class="pimg"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></span>
              <span class="pname">{{ p.name }} <span class="muted small">{{ p.price != null ? fmtNum(p.price) : '' }}</span></span>
              <input v-model="p.quantity" type="number" min="0.001" step="any" class="input qty" :title="p.isWeighted ? 'kg' : 'dona'" />
              <span class="muted small unit">{{ p.isWeighted ? 'kg' : 'dona' }}</span>
              <input v-model="p.note" class="input note" maxlength="80" placeholder="izoh: 1 stakan" />
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === 0" @click="moveItem(i, -1)">↑</button>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === form.items.length - 1" @click="moveItem(i, 1)">↓</button>
              <button type="button" class="btn btn-icon danger" title="Olib tashlash" @click="form.items.splice(i, 1)"><AppIcon name="x" :size="15" /></button>
            </div>
            <div v-if="!form.items.length" class="muted small" style="padding:8px 0">Mahsulot qo'shilmagan</div>
          </div>
        </div>

        <div class="field" style="margin-top:8px">
          <label>Tayyorlash qadamlari</label>
          <div v-for="(s, i) in form.steps" :key="i" class="step">
            <span class="pnum muted">{{ i + 1 }}.</span>
            <textarea v-model="form.steps[i]" class="input" rows="2" maxlength="500"></textarea>
            <div class="step-btns">
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === 0" @click="moveStep(i, -1)">↑</button>
              <button type="button" class="btn btn-ghost btn-sm" :disabled="i === form.steps.length - 1" @click="moveStep(i, 1)">↓</button>
              <button type="button" class="btn btn-icon danger" title="Olib tashlash" :disabled="form.steps.length === 1" @click="form.steps.splice(i, 1)"><AppIcon name="x" :size="15" /></button>
            </div>
          </div>
          <button type="button" class="btn btn-ghost btn-sm" :disabled="form.steps.length >= 30" @click="form.steps.push('')">+ Qadam</button>
        </div>

        <button class="btn" style="width:100%; margin-top:8px" :disabled="saving || detailLoading">{{ saving ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </form>
    </div>
  </div>

  <div v-if="deleteTarget" class="modal-overlay" @click.self="deleteTarget = null">
    <div class="modal-box" style="max-width:420px">
      <div class="modal-head">
        <h3>Retseptni o'chirish</h3>
        <button class="modal-x" @click="deleteTarget = null">×</button>
      </div>
      <div class="modal-body">
        <p style="margin:0">"<b>{{ deleteTarget.title }}</b>" retseptini o'chirasizmi?</p>
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
.clamp { max-width: 360px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.tab-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.cell { display: flex; align-items: center; gap: 10px; }
.thumb { width: 44px; height: 44px; border-radius: 8px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.thumb img { width: 100%; height: 100%; object-fit: cover; }
.plist { max-height: 320px; overflow: auto; margin-top: 6px; }
.pitem { display: flex; align-items: center; gap: 6px; padding: 4px 0; border-bottom: 1px solid var(--border); font-size: 13px; }
.pnum { width: 22px; text-align: right; flex-shrink: 0; }
.pimg { width: 28px; height: 28px; border-radius: 6px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.pimg img { width: 100%; height: 100%; object-fit: cover; }
.pname { flex: 1; font-weight: 600; min-width: 0; }
.qty { width: 76px; padding: 4px 6px; }
.unit { width: 28px; }
.note { width: 140px; padding: 4px 6px; }
.step { display: flex; align-items: flex-start; gap: 6px; margin-bottom: 6px; }
.step textarea { flex: 1; }
.step-btns { display: flex; flex-direction: column; }
</style>
