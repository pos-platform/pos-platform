<script setup>
import { ref, computed, watch } from 'vue'
import { salesApi } from '../../api/catalog'
import { errorMessage } from '../../api/http'

// Bosqich 10.3 / 10.5: chek tafsiloti (qatorlar, qaytarilgan miqdorlar) va chek bo'yicha qisman qaytarish.
// Savdolar sahifasi va "Sotuvlar logi" sahifasida qayta ishlatiladi.
const props = defineProps({
  saleId: { type: String, required: true },
  allowRefund: { type: Boolean, default: false },
  startRefund: { type: Boolean, default: false }
})
const emit = defineEmits(['close', 'refunded'])

const sale = ref(null)
const loading = ref(false)
const err = ref('')

const PAY = { Cash: 'Naqd', Card: 'Karta', Mixed: 'Aralash', Debt: 'Nasiya', Transfer: "O'tkazma" }
const STATUS = {
  Completed: { text: 'Yakunlangan', cls: 'pill-green' },
  Refunded: { text: 'Qaytarilgan', cls: 'pill-amber' },
  PartiallyRefunded: { text: 'Qisman qaytarilgan', cls: 'pill-amber' },
  Open: { text: 'Ochiq', cls: 'pill-gray' },
  Cancelled: { text: 'Bekor qilingan', cls: 'pill-red' }
}
const statusOf = (s) => STATUS[s] || { text: s || '—', cls: 'pill-gray' }

const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const fmtQty = (n) => Number(n || 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
const fmtDateTime = (v) =>
  v ? new Date(v).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '—'

const refundedQty = (i) => Number(i.refundedQuantity) || 0
const refundableQty = (i) =>
  i.refundableQuantity != null ? Number(i.refundableQuantity) : Math.max(0, Number(i.quantity) - refundedQty(i))

const canRefund = computed(() => {
  const s = sale.value
  if (!s || s.type === 'Refund') return false
  if (s.canRefund != null) return !!s.canRefund
  return s.status !== 'Refunded' && (s.items || []).some((i) => refundableQty(i) > 0)
})

// ===== Qaytarish rejimi =====
const refundMode = ref(false)
const lines = ref([]) // [{ saleItemId, name, max, unitPrice, checked, quantity }]
const reason = ref('')
const preview = ref(null)
const previewLoading = ref(false)
const submitting = ref(false)

function initLines() {
  lines.value = (sale.value?.items || [])
    .filter((i) => refundableQty(i) > 0)
    .map((i) => ({ saleItemId: i.id, name: i.productName, max: refundableQty(i), unitPrice: Number(i.unitPrice) || 0, checked: false, quantity: refundableQty(i) }))
  reason.value = ''
  preview.value = null
}

function enterRefund() {
  initLines()
  refundMode.value = true
}

function selectAll() {
  for (const l of lines.value) { l.checked = true; l.quantity = l.max }
  preview.value = null
}

const selectedItems = computed(() =>
  lines.value
    .filter((l) => l.checked && Number(l.quantity) > 0)
    .map((l) => ({ saleItemId: l.saleItemId, quantity: Math.min(Number(l.quantity), l.max) }))
)
const isFull = computed(
  () => lines.value.length > 0 && lines.value.every((l) => l.checked && Number(l.quantity) >= l.max) &&
    (sale.value?.items || []).every((i) => refundedQty(i) === 0)
)
const lineError = computed(() => {
  for (const l of lines.value) {
    if (!l.checked) continue
    const q = Number(l.quantity)
    if (!(q > 0)) return `"${l.name}" — miqdor 0 dan katta bo'lsin`
    if (q > l.max) return `"${l.name}" — ko'pi bilan ${fmtQty(l.max)}`
  }
  return ''
})

function body() {
  const b = { items: selectedItems.value }
  if (reason.value.trim()) b.reason = reason.value.trim()
  return b
}

async function loadPreview() {
  err.value = ''
  if (lineError.value) { err.value = lineError.value; return }
  if (!selectedItems.value.length) { err.value = 'Qaytariladigan tovarni belgilang'; return }
  previewLoading.value = true
  try {
    preview.value = await salesApi.refundPreview(props.saleId, body())
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    previewLoading.value = false
  }
}

async function confirmRefund() {
  if (!preview.value) return
  if (!window.confirm(`"${sale.value.receiptNumber}" cheki bo'yicha qaytarishni tasdiqlaysizmi?`)) return
  submitting.value = true
  err.value = ''
  try {
    const r = await salesApi.refund(props.saleId, body())
    emit('refunded', r)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    submitting.value = false
  }
}

async function load() {
  loading.value = true
  err.value = ''
  refundMode.value = false
  try {
    sale.value = await salesApi.get(props.saleId)
    if (props.startRefund && props.allowRefund && canRefund.value) enterRefund()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

watch(() => props.saleId, load, { immediate: true })
watch(lines, () => { preview.value = null }, { deep: true })
</script>

<template>
  <div class="modal-overlay" @click.self="emit('close')">
    <div class="modal-box" style="max-width:820px">
      <div class="modal-head">
        <h3>
          Chek {{ sale?.receiptNumber || '' }}
          <span v-if="sale" class="pill" :class="sale.type === 'Refund' ? 'pill-red' : statusOf(sale.status).cls" style="margin-left:8px">
            {{ sale.type === 'Refund' ? 'Qaytarish cheki' : statusOf(sale.status).text }}
          </span>
        </h3>
        <button class="modal-x" @click="emit('close')">×</button>
      </div>
      <div class="modal-body">
        <div v-if="err" class="alert alert-error">{{ err }}</div>
        <div v-if="loading" class="muted">Yuklanmoqda...</div>

        <template v-else-if="sale">
          <div class="meta">
            <div><span class="muted">Vaqt:</span> {{ fmtDateTime(sale.createdAt) }}</div>
            <div><span class="muted">Kassir:</span> {{ sale.cashierFullName || sale.cashierUsername || '—' }}</div>
            <div><span class="muted">Kassa:</span> {{ sale.terminalId || '—' }}</div>
            <div><span class="muted">To'lov:</span> {{ PAY[sale.paymentMethod] || sale.paymentMethod }}</div>
            <div v-if="sale.receipt?.fiscalStatus"><span class="muted">Fiskal:</span> {{ sale.receipt.fiscalStatus }}</div>
            <div v-if="Number(sale.loyaltyPointsEarned) > 0"><span class="muted">Keshbek:</span> +{{ fmtNum(sale.loyaltyPointsEarned) }} ball</div>
            <div v-if="Number(sale.loyaltyPointsRedeemed) > 0"><span class="muted">Ball bilan to'landi:</span> {{ fmtNum(sale.loyaltyPointsRedeemed) }}</div>
          </div>

          <!-- Ko'rish rejimi -->
          <template v-if="!refundMode">
            <div class="scroll-x">
              <table class="table">
                <thead>
                  <tr>
                    <th>Tovar</th>
                    <th class="text-right">Miqdor</th>
                    <th class="text-right">Qaytarilgan</th>
                    <th class="text-right">Narx</th>
                    <th class="text-right">Chegirma</th>
                    <th class="text-right">Summa</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="i in sale.items || []" :key="i.id">
                    <td>
                      <strong>{{ i.productName }}</strong>
                      <div class="muted small">{{ i.barcode || '' }}</div>
                    </td>
                    <td class="text-right">{{ fmtQty(i.quantity) }}</td>
                    <td class="text-right" :style="{ color: refundedQty(i) > 0 ? 'var(--danger)' : '' }">
                      {{ refundedQty(i) > 0 ? fmtQty(refundedQty(i)) : '—' }}
                    </td>
                    <td class="text-right">{{ fmtNum(i.unitPrice) }}</td>
                    <td class="text-right">{{ Number(i.discountAmount) > 0 ? fmtNum(i.discountAmount) : '—' }}</td>
                    <td class="text-right"><strong>{{ fmtNum(i.lineTotal) }}</strong></td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="totals">
              <div v-if="Number(sale.discountAmount) > 0"><span>Chegirma</span><span>−{{ fmtNum(sale.discountAmount) }}</span></div>
              <div class="grand"><span>Jami</span><span>{{ sale.type === 'Refund' ? '− ' : '' }}{{ fmtNum(sale.totalAmount) }} so'm</span></div>
              <div v-if="Number(sale.loyaltyWithheldAmount) > 0"><span>Keshbek ushlab qolindi</span><span>{{ fmtNum(sale.loyaltyWithheldAmount) }}</span></div>
              <div v-if="sale.refundCashAmount != null && sale.type === 'Refund'"><span>Mijozga berilgan pul</span><span>{{ fmtNum(sale.refundCashAmount) }}</span></div>
            </div>
            <div v-if="allowRefund && canRefund" style="margin-top:14px">
              <button class="btn" @click="enterRefund">Qaytarish</button>
            </div>
          </template>

          <!-- Qisman qaytarish rejimi -->
          <template v-else>
            <div class="row" style="justify-content:space-between; align-items:center; margin-bottom:8px">
              <strong>Qaytariladigan tovarlar</strong>
              <button class="btn btn-ghost btn-sm" @click="selectAll">Hammasi</button>
            </div>
            <div class="scroll-x">
              <table class="table">
                <thead>
                  <tr><th style="width:36px"></th><th>Tovar</th><th class="text-right">Qoldi</th><th class="text-right">Narx</th><th style="width:140px">Miqdor</th></tr>
                </thead>
                <tbody>
                  <tr v-for="l in lines" :key="l.saleItemId">
                    <td><input v-model="l.checked" type="checkbox" /></td>
                    <td>{{ l.name }}</td>
                    <td class="text-right">{{ fmtQty(l.max) }}</td>
                    <td class="text-right">{{ fmtNum(l.unitPrice) }}</td>
                    <td><input v-model="l.quantity" type="number" min="0" :max="l.max" step="any" class="input" :disabled="!l.checked" /></td>
                  </tr>
                  <tr v-if="!lines.length"><td colspan="5" class="muted" style="text-align:center; padding:16px">Qaytariladigan tovar qolmagan</td></tr>
                </tbody>
              </table>
            </div>
            <div class="field" style="margin-top:10px">
              <label>Sabab (ixtiyoriy)</label>
              <input v-model="reason" class="input" maxlength="500" />
            </div>

            <div v-if="preview" class="preview">
              <div><span>Qaytariladigan summa</span><b>{{ fmtNum(preview.refundTotal) }}</b></div>
              <div><span>Ayiriladigan keshbek</span><b>{{ fmtNum(preview.pointsToReverse) }} ball</b></div>
              <div class="muted small"><span>Mijoz balansida</span><span>{{ fmtNum(preview.pointsAvailable) }} ball</span></div>
              <div v-if="Number(preview.redeemedPointsReturned) > 0"><span>Ballga qaytariladi</span><b>{{ fmtNum(preview.redeemedPointsReturned) }} ball</b></div>
              <div v-if="Number(preview.withheldAmount) > 0" class="warn"><span>Puldan ushlab qolinadi</span><b>{{ fmtNum(preview.withheldAmount) }}</b></div>
              <div class="grand"><span>Mijozga beriladigan pul</span><b>{{ fmtNum(preview.cashToReturn) }} so'm</b></div>
            </div>

            <div class="row" style="margin-top:14px; gap:8px">
              <button class="btn btn-ghost" :disabled="previewLoading" @click="loadPreview">{{ previewLoading ? '...' : 'Hisoblash' }}</button>
              <button class="btn btn-danger" :disabled="!preview || submitting" @click="confirmRefund">
                {{ submitting ? '...' : (isFull ? "To'liq qaytarish" : 'Qaytarishni tasdiqlash') }}
              </button>
              <button class="btn btn-ghost" @click="refundMode = false">Ortga</button>
            </div>
            <div class="muted small" style="margin-top:6px">Avval "Hisoblash" — keshbek ayirilishi va beriladigan pulni ko'ring.</div>
          </template>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.small { font-size: 12px; }
.scroll-x { overflow-x: auto; }
.meta { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 4px 16px; font-size: 13.5px; margin-bottom: 12px; }
.totals, .preview { margin-top: 10px; display: flex; flex-direction: column; gap: 3px; }
.totals > div, .preview > div { display: flex; justify-content: space-between; gap: 8px; font-size: 14px; }
.totals .grand, .preview .grand { font-weight: 700; font-size: 16px; }
.preview { background: #f8fafc; border-radius: 10px; padding: 10px 12px; }
.preview .warn { color: #b45309; }
</style>
