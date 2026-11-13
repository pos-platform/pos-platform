<script setup>
import { ref, computed } from 'vue'
import { mediaUrl } from '../../api/http'
import { fmtNum } from './marketingUtil'

// Mahsulotni nom yoki shtrix-kod bo'yicha qidirib tanlash (ro'yxat ota komponentdan keladi).
const props = defineProps({
  products: { type: Array, default: () => [] },
  exclude: { type: Array, default: () => [] },
  placeholder: { type: String, default: 'Mahsulot qidirish (nom, shtrix-kod)...' }
})
const emit = defineEmits(['select'])

const q = ref('')
const open = ref(false)

const matches = computed(() => {
  const s = q.value.trim().toLowerCase()
  if (!s) return []
  const ex = new Set(props.exclude)
  return props.products
    .filter((p) => !ex.has(p.id) && ((p.name || '').toLowerCase().includes(s) || (p.barcode || '').includes(s)))
    .slice(0, 8)
})

function closeLater() {
  window.setTimeout(() => (open.value = false), 150)
}

function pick(p) {
  emit('select', p)
  q.value = ''
  open.value = false
}
</script>

<template>
  <div class="ps">
    <input
      v-model="q"
      class="input"
      :placeholder="placeholder"
      @focus="open = true"
      @blur="closeLater"
      @keydown.enter.prevent="matches[0] && pick(matches[0])"
    />
    <div v-if="open && matches.length" class="ps-list">
      <button v-for="p in matches" :key="p.id" type="button" class="ps-item" @mousedown.prevent="pick(p)">
        <span class="ps-img"><img v-if="p.imageUrl" :src="mediaUrl(p.imageUrl)" alt="" /></span>
        <span class="ps-name">{{ p.name }}<span v-if="p.showOnline === false" class="muted"> · ilovada emas</span></span>
        <span class="muted">{{ fmtNum(p.price) }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.ps { position: relative; }
.ps-list { position: absolute; z-index: 20; left: 0; right: 0; top: calc(100% + 4px); background: #fff; border: 1px solid var(--border); border-radius: 10px; box-shadow: 0 8px 24px rgba(15, 23, 42, .12); max-height: 300px; overflow: auto; }
.ps-item { display: flex; align-items: center; gap: 8px; width: 100%; padding: 7px 10px; border: 0; background: none; cursor: pointer; text-align: left; font-size: 13px; }
.ps-item:hover { background: var(--surface-2, #f1f5f9); }
.ps-img { width: 28px; height: 28px; border-radius: 6px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.ps-img img { width: 100%; height: 100%; object-fit: cover; }
.ps-name { flex: 1; }
</style>
