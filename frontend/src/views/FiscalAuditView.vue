<script setup>
import { ref, computed, onMounted } from 'vue'
import { auditApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const fails = ref([])
const loading = ref(false)
const err = ref('')
const onlyUnresolved = ref(false) // hozircha barcha xatolar; kelajakda holat bo'yicha filtr

/** Fiskal moduldan qaytgan xom xato matnini kassir tushunadigan sababga o'giradi. */
function fiscalHint(msg) {
  const m = (msg || '').toLowerCase()
  if (m.includes('time is in the past') || m.includes('receipt time'))
    return "Chek vaqti moduldagi oxirgi chekdan orqada — kompyuter soati/vaqt mintaqasini tekshiring."
  if (m.includes('send_all_receipts_first') || m.includes('locked_sync'))
    return "Fiskal modul to'lgan — cheklar OFD serverga sinxronlanishi kerak (internetni tekshiring)."
  if (m.includes('unsupported applet') || m.includes('65525'))
    return "Modul appleti (0400) mos drayver bilan ishlamayapti — FiscalDriveService ulanishini tekshiring."
  if ((m.includes('list') && m.includes("bo'sh")) || m.includes('topilmadi'))
    return "Fiskal modul ulanmagan yoki drayver ko'rmayapti — fleshka/kabelni tekshiring."
  if (m.includes('spic') || m.includes('mxik'))
    return "Mahsulot MXIK/SPIC kodi noto'g'ri yoki yo'q — tovar kartochkasini to'ldiring."
  if (m.includes('http') || m.includes('connection') || m.includes('refused'))
    return "Drayver (FiscalDriveService) javob bermayapti — dastur ishga tushganini tekshiring."
  return '' // notanish xato — faqat xom matn ko'rsatiladi
}

const shown = computed(() => fails.value)

function fmt(d) { return new Date(d).toLocaleString('uz-UZ') }
function money(n) { return Number(n).toLocaleString('uz-UZ') }

async function load() {
  loading.value = true
  err.value = ''
  try {
    fails.value = await auditApi.fiscalFailures(200)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Fiskal audit</h1>
    <button class="btn btn-ghost btn-sm" @click="load">↻ Yangilash</button>
  </div>
  <p class="muted sub">
    Fiskalizatsiya muvaffaqiyatsiz tugagan cheklar — QR kod chiqmagan savdolar, fiskal modul
    qaytargan aniq xato matni bilan (barcha tashkilotlar bo'yicha).
  </p>

  <div v-if="err" class="alert alert-error">{{ err }}</div>

  <div class="card">
    <table class="table">
      <thead>
        <tr>
          <th>Vaqt</th><th>Chek</th><th>Tashkilot</th><th>Kassa</th>
          <th style="text-align:right">Summa</th><th>Xato sababi</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="f in shown" :key="f.saleId">
          <td class="muted nowrap">{{ fmt(f.failedAt) }}</td>
          <td class="nowrap"><b>{{ f.receiptNumber }}</b></td>
          <td class="muted">{{ f.organizationName || '—' }}</td>
          <td class="muted">{{ f.terminalId || '—' }}</td>
          <td class="nowrap" style="text-align:right">{{ money(f.totalAmount) }}</td>
          <td>
            <div class="fiscal-raw">{{ f.errorMessage || 'Xato matni saqlanmagan' }}</div>
            <div v-if="fiscalHint(f.errorMessage)" class="fiscal-hint">↳ {{ fiscalHint(f.errorMessage) }}</div>
          </td>
        </tr>
        <tr v-if="!shown.length && !loading">
          <td colspan="6" class="muted" style="text-align:center;padding:28px">
            Fiskalizatsiya xatosi yo'q 🎉
          </td>
        </tr>
        <tr v-if="loading">
          <td colspan="6" class="muted" style="text-align:center;padding:28px">Yuklanmoqda…</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.sub { margin: 2px 0 16px; font-size: 13.5px; }
.nowrap { white-space: nowrap; }
.fiscal-raw { font-family: ui-monospace, 'Cascadia Code', monospace; font-size: 12.5px; color: #b91c1c; word-break: break-word; }
.fiscal-hint { font-size: 12.5px; color: var(--muted); margin-top: 3px; }
</style>
