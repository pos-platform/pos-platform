<script setup>
import { ref, onMounted } from 'vue'
import { productsApi, categoriesApi, onlineMarketingApi } from '../api/catalog'
import { errorMessage } from '../api/http'
import { useStoreFilterStore } from '../stores/storeFilter'
import BannersTab from './marketing/BannersTab.vue'
import CollectionsTab from './marketing/CollectionsTab.vue'
import RecipesTab from './marketing/RecipesTab.vue'
import StoriesTab from './marketing/StoriesTab.vue'
import PushCampaignsTab from './marketing/PushCampaignsTab.vue'

// Bosqich 9 (CONTRACT-9): mobil ilovadagi reklama — bannerlar, kolleksiyalar, stories, push xabarlar.
const TABS = [
  { id: 'banners', text: 'Bannerlar' },
  { id: 'collections', text: 'Kolleksiyalar' },
  { id: 'recipes', text: 'Retseptlar' },
  { id: 'stories', text: 'Stories' },
  { id: 'push', text: 'Push xabarlar' }
]
const tab = ref('banners')

const storeFilter = useStoreFilterStore()
const products = ref([])
const categories = ref([])
const collections = ref([])
const err = ref('')

async function loadCollections() {
  try {
    collections.value = await onlineMarketingApi.collections.list()
  } catch (e) {
    err.value = errorMessage(e)
  }
}

onMounted(async () => {
  try {
    const [p, c] = await Promise.all([productsApi.list(), categoriesApi.list(), storeFilter.ensureLoaded()])
    products.value = Array.isArray(p) ? p : p?.items || []
    categories.value = Array.isArray(c) ? c : c?.items || []
  } catch (e) {
    err.value = errorMessage(e)
  }
  loadCollections()
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>Ilova reklamasi</h1>
      <div class="muted small">Mobil ilova bosh sahifasidagi bannerlar, kolleksiyalar, stories va mijozlarga push xabarlar.</div>
    </div>
  </div>

  <div class="row" style="gap:8px; margin-bottom:12px">
    <button
      v-for="t in TABS"
      :key="t.id"
      class="btn btn-ghost btn-sm"
      :class="{ 'btn-active': tab === t.id }"
      @click="tab = t.id"
    >{{ t.text }}</button>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <BannersTab
    v-if="tab === 'banners'"
    :products="products"
    :categories="categories"
    :collections="collections"
    :stores="storeFilter.stores"
  />
  <CollectionsTab
    v-else-if="tab === 'collections'"
    :products="products"
    :stores="storeFilter.stores"
    @changed="loadCollections"
  />
  <RecipesTab
    v-else-if="tab === 'recipes'"
    :products="products"
    :stores="storeFilter.stores"
  />
  <StoriesTab
    v-else-if="tab === 'stories'"
    :products="products"
    :categories="categories"
    :collections="collections"
    :stores="storeFilter.stores"
  />
  <PushCampaignsTab
    v-else
    :products="products"
    :categories="categories"
    :collections="collections"
    :stores="storeFilter.stores"
  />
</template>

<style scoped>
.small { font-size: 12px; }
.btn-active { background: var(--primary); color: #fff; }
</style>
