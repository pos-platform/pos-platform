<script setup>
import { computed } from 'vue'
import ProductSearch from './ProductSearch.vue'
import { LINK_TYPES } from './marketingUtil'

// Havola turi + qiymati: v-model:link-type, v-model:link-value.
const props = defineProps({
  linkType: { type: String, default: 'None' },
  linkValue: { type: String, default: '' },
  products: { type: Array, default: () => [] },
  categories: { type: Array, default: () => [] },
  collections: { type: Array, default: () => [] }
})
const emit = defineEmits(['update:linkType', 'update:linkValue'])

function setType(t) {
  emit('update:linkType', t)
  emit('update:linkValue', '')
}

const selectedProduct = computed(() =>
  props.linkType === 'Product' ? props.products.find((p) => p.id === props.linkValue) : null
)
</script>

<template>
  <div class="row">
    <div class="field" style="max-width:220px">
      <label>Bosilganda ochiladi</label>
      <select :value="linkType || 'None'" class="select" @change="setType($event.target.value)">
        <option v-for="l in LINK_TYPES" :key="l.value" :value="l.value">{{ l.text }}</option>
      </select>
    </div>

    <div v-if="linkType === 'Product'" class="field">
      <label>Mahsulot</label>
      <div v-if="selectedProduct || linkValue" class="chosen">
        <span>{{ selectedProduct?.name || linkValue }}</span>
        <button type="button" class="btn btn-ghost btn-sm" @click="emit('update:linkValue', '')">✕</button>
      </div>
      <ProductSearch v-else :products="products" @select="(p) => emit('update:linkValue', p.id)" />
    </div>

    <div v-else-if="linkType === 'Category'" class="field">
      <label>Kategoriya</label>
      <select :value="linkValue" class="select" @change="emit('update:linkValue', $event.target.value)">
        <option value="">— tanlang —</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <div v-else-if="linkType === 'Collection'" class="field">
      <label>Kolleksiya</label>
      <select :value="linkValue" class="select" @change="emit('update:linkValue', $event.target.value)">
        <option value="">— tanlang —</option>
        <option v-for="c in collections" :key="c.id" :value="c.id">{{ c.title }}</option>
      </select>
      <span v-if="!collections.length" class="muted small">Avval "Kolleksiyalar" tabida kolleksiya yarating.</span>
    </div>

    <div v-else-if="linkType === 'Url'" class="field">
      <label>Havola</label>
      <input :value="linkValue" class="input" maxlength="500" placeholder="https://..." @input="emit('update:linkValue', $event.target.value)" />
    </div>
  </div>
</template>

<style scoped>
.chosen { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 6px 10px; border: 1px solid var(--border); border-radius: 10px; background: var(--surface-2, #f8fafc); font-weight: 600; font-size: 13px; }
.small { font-size: 12px; }
</style>
