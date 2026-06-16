<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { dishesApi, productsApi, categoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const dishes = ref([])
const categories = ref([])
const goods = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)

// Yangi/tahrir ovqat formasi
const showForm = ref(false)
const editingId = ref(null)
const form = reactive({ name: '', categoryId: '', price: 0, prepMinutes: null })

function resetForm() {
  editingId.value = null
  form.name = ''
  form.price = 0
  form.prepMinutes = null
  if (categories.value.length) form.categoryId = categories.value[0].id
}

function openCreate() {
  resetForm()
  showForm.value = true
}

function openEditDish(d) {
  editingId.value = d.id
  form.name = d.name
  form.categoryId = d.categoryId
  form.price = d.price
  form.prepMinutes = d.prepMinutes
  showForm.value = true
}

async function removeDish(d) {
  if (!confirm(`"${d.name}" ovqatini o'chirasizmi?`)) return
  err.value = ''
  try {
    await productsApi.remove(d.id)
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

// Retsept tahriri
const recipeDish = ref(null)
const recipeLines = ref([]) // { product, quantity }
const goodSearch = ref('')

const goodResults = computed(() => {
  const q = goodSearch.value.trim().toLowerCase()
  if (!q) return []
  return goods.value
    .filter((p) => p.name.toLowerCase().includes(q))
    .slice(0, 8)
})

function fmt(n) { return Number(n || 0).toLocaleString() }

async function load() {
  loading.value = true
  err.value = ''
  try {
    const [d, c, p] = await Promise.all([dishesApi.list(), categoriesApi.list(), productsApi.list()])
    dishes.value = d
    categories.value = c
    goods.value = p.filter((x) => x.productType === 'Good')
    if (!form.categoryId && c.length) form.categoryId = c[0].id
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function createDish() {
  if (!form.name.trim()) { err.value = 'Ovqat nomini kiriting'; return }
  if (!form.categoryId) { err.value = 'Kategoriya tanlang'; return }
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const payload = {
      name: form.name.trim(),
      productType: 'Dish',
      price: Number(form.price) || 0,
      prepMinutes: form.prepMinutes ? Number(form.prepMinutes) : null,
      categoryId: form.categoryId,
      isVat: true,
      quantity: 0
    }
    if (editingId.value) {
      await productsApi.update(editingId.value, payload)
      success.value = 'Ovqat yangilandi'
    } else {
      await productsApi.create(payload)
      success.value = 'Ovqat qo\'shildi'
    }
    resetForm()
    showForm.value = false
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function openRecipe(dish) {
  recipeDish.value = dish
  recipeLines.value = (dish.ingredients || []).map((i) => ({
    product: goods.value.find((g) => g.id === i.ingredientProductId) || { id: i.ingredientProductId, name: i.ingredientName },
    quantity: i.quantity
  }))
  goodSearch.value = ''
}

function addGood(p) {
  if (recipeLines.value.find((l) => l.product.id === p.id)) { goodSearch.value = ''; return }
  recipeLines.value.push({ product: p, quantity: 1 })
  goodSearch.value = ''
}
function removeGood(id) { recipeLines.value = recipeLines.value.filter((l) => l.product.id !== id) }

async function saveRecipe() {
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    await dishesApi.setRecipe(recipeDish.value.id, recipeLines.value.map((l) => ({
      ingredientProductId: l.product.id,
      quantity: Number(l.quantity)
    })))
    success.value = `"${recipeDish.value.name}" retsepti saqlandi`
    recipeDish.value = null
    await load()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Ovqatlar (menyu)</h1>
    <button class="btn" @click="showForm ? (showForm = false) : openCreate()">+ Yangi ovqat</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="success" class="alert alert-success">{{ success }}</div>

  <form v-if="showForm" class="card" style="margin-bottom:16px" @submit.prevent="createDish">
    <h3 style="margin-top:0">{{ editingId ? 'Ovqatni tahrirlash' : 'Yangi ovqat' }}</h3>
    <div class="row" style="gap:12px; flex-wrap:wrap">
      <div class="field"><label>Nomi</label><input v-model="form.name" class="input" placeholder="Osh" /></div>
      <div class="field"><label>Kategoriya</label><select v-model="form.categoryId" class="input"><option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option></select></div>
      <div class="field"><label>Narx</label><input v-model="form.price" type="number" min="0" class="input" style="width:140px" /></div>
      <div class="field"><label>Tayyorlash (daq)</label><input v-model="form.prepMinutes" type="number" min="0" class="input" style="width:120px" /></div>
    </div>
    <button class="btn" :disabled="saving">{{ saving ? '...' : (editingId ? 'Saqlash' : 'Qo\'shish') }}</button>
  </form>

  <div class="card">
    <table class="table">
      <thead><tr><th>Ovqat</th><th class="text-right">Narx</th><th>Retsept (tovarlar)</th><th></th></tr></thead>
      <tbody>
        <tr v-for="d in dishes" :key="d.id">
          <td><strong>{{ d.name }}</strong> <span class="muted">{{ d.categoryName }}</span></td>
          <td class="text-right">{{ fmt(d.price) }}</td>
          <td class="muted">{{ d.ingredients.length ? d.ingredients.map(i => i.ingredientName).join(', ') : '— retsept yo\'q' }}</td>
          <td class="text-right" style="white-space:nowrap">
            <button class="btn btn-ghost btn-sm" @click="openRecipe(d)">🧾 Retsept</button>
            <button class="btn btn-ghost btn-sm" @click="openEditDish(d)">✏️</button>
            <button class="btn btn-ghost btn-sm" @click="removeDish(d)">🗑️</button>
          </td>
        </tr>
        <tr v-if="!dishes.length && !loading"><td colspan="4" class="muted" style="text-align:center;padding:24px">Ovqat yo'q</td></tr>
      </tbody>
    </table>
  </div>

  <!-- Retsept modal -->
  <div v-if="recipeDish" class="modal-backdrop" @click.self="recipeDish = null">
    <div class="modal-box card">
      <h3 style="margin-top:0">Retsept — {{ recipeDish.name }}</h3>
      <p class="muted">1 ta ovqatga ketadigan tovarlarni belgilang. Bu ombordan oshpazga berishda oldindan to'ldiriladi.</p>

      <div class="field" style="position:relative">
        <label>Tovar qo'shish</label>
        <input v-model="goodSearch" class="input" placeholder="Tovar qidirish..." />
        <div v-if="goodResults.length" class="search-pop">
          <div v-for="p in goodResults" :key="p.id" class="search-item" @click="addGood(p)">{{ p.name }}</div>
        </div>
      </div>

      <table class="table" style="margin:12px 0">
        <thead><tr><th>Tovar</th><th style="width:140px">Miqdor</th><th></th></tr></thead>
        <tbody>
          <tr v-for="l in recipeLines" :key="l.product.id">
            <td>{{ l.product.name }}</td>
            <td><input v-model="l.quantity" type="number" min="0" step="0.001" class="input" /></td>
            <td class="text-right"><button class="btn btn-ghost btn-sm" @click="removeGood(l.product.id)">✕</button></td>
          </tr>
          <tr v-if="!recipeLines.length"><td colspan="3" class="muted">Tovar qo'shilmagan</td></tr>
        </tbody>
      </table>

      <div class="row" style="justify-content:flex-end; gap:8px">
        <button class="btn btn-ghost" @click="recipeDish = null">Bekor</button>
        <button class="btn" :disabled="saving" @click="saveRecipe">{{ saving ? '...' : 'Retseptni saqlash' }}</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.search-pop { position:absolute; top:100%; left:0; right:0; z-index:5; background:#fff; border:1px solid var(--border); border-radius:8px; box-shadow:var(--shadow); max-height:260px; overflow:auto; }
.search-item { padding:9px 12px; cursor:pointer; }
.search-item:hover { background:var(--bg); }
.modal-backdrop { position:fixed; inset:0; background:rgba(0,0,0,.4); display:flex; align-items:flex-start; justify-content:center; padding-top:60px; z-index:50; }
.modal-box { width:100%; max-width:560px; }
</style>
