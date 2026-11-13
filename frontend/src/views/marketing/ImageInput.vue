<script setup>
import { ref } from 'vue'
import { productsApi } from '../../api/catalog'
import { errorMessage, mediaUrl } from '../../api/http'

// Rasm yuklash (mavjud mahsulot rasmi endpointi → { url }). v-model — nisbiy URL.
const props = defineProps({
  modelValue: { type: String, default: '' },
  wide: { type: Boolean, default: false }
})
const emit = defineEmits(['update:modelValue', 'error'])
const uploading = ref(false)

async function onFile(ev) {
  const file = ev.target.files?.[0]
  if (!file) return
  uploading.value = true
  try {
    const res = await productsApi.uploadImage(file)
    emit('update:modelValue', res.url)
  } catch (e) {
    emit('error', errorMessage(e))
  } finally {
    uploading.value = false
    ev.target.value = ''
  }
}
</script>

<template>
  <div class="img-row">
    <div class="img" :class="{ wide }"><img v-if="props.modelValue" :src="mediaUrl(props.modelValue)" alt="" /></div>
    <input type="file" accept="image/*" :disabled="uploading" @change="onFile" />
    <span v-if="uploading" class="muted small">Yuklanmoqda...</span>
    <button v-if="props.modelValue" type="button" class="btn btn-ghost btn-sm" @click="emit('update:modelValue', '')">Olib tashlash</button>
  </div>
</template>

<style scoped>
.img-row { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.img { width: 56px; height: 56px; border-radius: 10px; background: #f1f5f9; overflow: hidden; flex-shrink: 0; }
.img.wide { width: 120px; height: 56px; }
.img img { width: 100%; height: 100%; object-fit: cover; }
.small { font-size: 12px; }
</style>
