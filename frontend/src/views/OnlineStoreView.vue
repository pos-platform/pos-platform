<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { onlineStoreApi } from '../api/catalog'
import { errorMessage } from '../api/http'

// Online do'kon — ko'rish sahifasi. Tahrirlash alohida sahifada (/online-store/edit).
// Ilovaga chiqish, bloklash va to'lov usullarini SuperAdmin boshqaradi (bu yerda faqat ko'rinadi).
const route = useRoute()
const router = useRouter()
const s = ref(null)
const loading = ref(false)
const busy = ref(false)
const err = ref('')
const ok = ref(route.query.saved ? 'Sozlamalar saqlandi.' : '')
if (route.query.saved) router.replace({ query: {} })

const statusText = computed(() => {
  const v = s.value
  if (!v) return ''
  if (v.isBlocked) return `Bloklangan: ${v.blockReason || ''}`
  if (v.isVisible) return 'Do\'koningiz ilovada ko\'rinmoqda'
  if (v.isEnabled) return 'Yoqilgan, lekin tarif muddati tugagan yoki tashkilot nofaol'
  if (v.isRequested) return 'So\'rov yuborilgan — platforma ko\'rib chiqmoqda'
  return 'Do\'kon ilovada emas'
})

const fmt = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')
const yesNo = (b) => (b ? 'Ha' : 'Yo\'q')
const hm = (t) => (t ? t.substring(0, 5) : '')
const blank = (v) => v === null || v === undefined || v === ''

const sections = computed(() => {
  const v = s.value
  if (!v) return []
  const delivery = v.deliveryEnabled
  return [
    {
      title: 'Do\'kon haqida',
      rows: [
        ['Telefon raqam', v.phone || '—'],
        ['Qisqa tavsif', v.description || '—']
      ]
    },
    {
      title: 'Ish vaqti',
      rows: [
        ['Ish vaqti', v.opensAt || v.closesAt ? `${hm(v.opensAt) || '—'} – ${hm(v.closesAt) || '—'}` : '24/7'],
        ['Yopiq paytda ham buyurtma olish', yesNo(v.acceptWhenClosed !== false)]
      ]
    },
    {
      title: 'Yetkazib berish va olib ketish',
      rows: [
        ['Yetkazib berish', yesNo(delivery)],
        ['Do\'kondan olib ketish', yesNo(v.pickupEnabled)],
        ...(delivery
          ? [
              ['Minimal buyurtma summasi', `${fmt(v.minOrderAmount)} so'm`],
              ['Yetkazish narxi', `${fmt(v.deliveryFee)} so'm`],
              ['Bepul yetkazish chegarasi', blank(v.freeDeliveryFrom) ? 'Doim pullik' : `${fmt(v.freeDeliveryFrom)} so'm dan`],
              ['Yetkazish vaqti', blank(v.deliveryMinMinutes) ? '—' : `${v.deliveryMinMinutes}–${v.deliveryMaxMinutes} daqiqa`],
              ['Yetkazish radiusi', blank(v.deliveryRadiusKm) ? 'Cheklovsiz' : `${v.deliveryRadiusKm} km`],
              ['Avtomatik bekor qilish', Number(v.autoCancelMinutes) ? `${v.autoCancelMinutes} daqiqa` : 'O\'chiq'],
              ['Yetkazish xizmati MXIK', v.deliveryServiceMxikCode || '—'],
              ['Kuryer tanlash', v.courierOfferMode === 'Off' ? 'Qo\'lda' : `Avtomatik (${v.courierOfferTimeoutSec ?? 120} soniya kutish)`]
            ]
          : [])
      ]
    },
    {
      title: 'Kartaga o\'tkazma',
      rows: [
        ['Karta', v.transferCardNumber ? `${v.transferCardNumber}${v.transferCardHolder ? ' — ' + v.transferCardHolder : ''}` : 'Kiritilmagan (usul ko\'rinmaydi)'],
        ['Pul tushmaguncha kuryerga bermaslik', yesNo(v.requireTransferBeforeDispatch)]
      ]
    },
    {
      title: 'Bonuslar',
      rows: [
        ['Bonus ball bilan to\'lash', v.bonusPayEnabled ? `Ha, ko'pi bilan ${v.bonusMaxPercent ?? 0}%` : 'Yo\'q'],
        ['Do\'stni taklif qilish', v.referralEnabled ? `Ha — taklif qilganga ${fmt(v.referrerBonus)}, yangi mijozga ${fmt(v.refereeBonus)} ball` : 'Yo\'q']
      ]
    },
    {
      title: 'Tovarni qaytarish',
      rows: [
        ['Qaytarish so\'rovi', v.returnsEnabled !== false ? `Ha, ${v.returnDays ?? 3} kun ichida` : 'Yo\'q'],
        ...(v.returnsEnabled !== false ? [['Kuryer olib ketadi', yesNo(v.returnPickupByCourier !== false)]] : [])
      ]
    },
    {
      title: 'Yordam',
      rows: [
        ['Yordam telefoni', v.supportPhone || '—'],
        ['Telegram', v.supportTelegram ? '@' + v.supportTelegram : '—']
      ]
    }
  ]
})

async function load() {
  loading.value = true
  err.value = ''
  try {
    s.value = await onlineStoreApi.get()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Serverdan kelgan sozlamalarni PUT uchun to'liq payloadga aylantirish (faqat bitta maydon o'zgaradi)
function payloadFrom(v, overrides) {
  const keys = [
    'isAcceptingOrders', 'displayName', 'description', 'logoUrl', 'phone', 'deliveryEnabled', 'pickupEnabled',
    'minOrderAmount', 'deliveryFee', 'freeDeliveryFrom', 'deliveryMinMinutes', 'deliveryMaxMinutes', 'autoCancelMinutes',
    'opensAt', 'closesAt', 'transferCardNumber', 'transferCardHolder', 'requireTransferBeforeDispatch', 'deliveryRadiusKm',
    'deliveryServiceMxikCode', 'bonusPayEnabled', 'bonusMaxPercent', 'supportPhone', 'supportTelegram', 'referralEnabled',
    'referrerBonus', 'refereeBonus', 'acceptWhenClosed', 'returnsEnabled', 'returnDays', 'returnPickupByCourier',
    'courierOfferMode', 'courierOfferTimeoutSec'
  ]
  const p = {}
  for (const k of keys) p[k] = v[k] === undefined || v[k] === '' ? null : v[k]
  return { ...p, ...overrides }
}

async function toggleAccepting() {
  const next = !s.value.isAcceptingOrders
  const q = next
    ? 'Buyurtma qabul qilish yoqilsinmi?'
    : 'Buyurtma qabul qilish to\'xtatilsinmi? Do\'kon ilovada ko\'rinadi, lekin yangi buyurtma berib bo\'lmaydi.'
  if (!window.confirm(q)) return
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    s.value = await onlineStoreApi.update(payloadFrom(s.value, { isAcceptingOrders: next }))
    ok.value = next ? 'Buyurtma qabul qilish yoqildi.' : 'Buyurtma qabul qilish to\'xtatildi.'
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function requestListing() {
  busy.value = true
  err.value = ''
  ok.value = ''
  try {
    s.value = await onlineStoreApi.requestListing()
    ok.value = 'So\'rov yuborildi. Platforma tasdiqlagach do\'koningiz ilovada chiqadi.'
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

const edit = () => router.push({ name: 'online-store-edit' })

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Online do'kon</h1>
  </div>
  <p class="muted lead">Mobil ilovadagi do'koningiz sozlamalari. O'zgartirish uchun <b>Tahrirlash</b> tugmasini bosing.</p>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>
  <div v-if="loading" class="muted">Yuklanmoqda...</div>

  <template v-if="s">
    <!-- Holat + buyurtma qabul (bitta qator) -->
    <div class="card top" :class="{ on: s.isVisible, blocked: s.isBlocked }">
      <div class="status-dot"></div>
      <div class="top-body">
        <div class="status-title">{{ statusText }}</div>
        <div class="status-meta">
          <span>To'lov: <b>{{ [s.allowCash && 'naqd', s.allowTransfer && 'kartaga o\'tkazma', s.allowCardOnDelivery && 'kuryer terminali', s.allowPayme && 'Payme', s.allowClick && 'Click'].filter(Boolean).join(', ') || 'yo\'q' }}</b></span>
          <span v-if="s.paidUntil">Tarif: <b>{{ new Date(s.paidUntil).toLocaleDateString('uz-UZ') }}</b> gacha</span>
        </div>
        <div class="hint">Ilovaga chiqish, bloklash va to'lov usullarini platforma boshqaradi.</div>
      </div>
      <div class="top-accept">
        <span class="pill" :class="s.isAcceptingOrders ? 'on' : 'off'">{{ s.isAcceptingOrders ? 'Faol' : 'Passiv' }}</span>
        <div>
          <div class="head-title">{{ s.isAcceptingOrders ? 'Buyurtma qabul qilinmoqda' : 'Buyurtma qabul qilinmayapti' }}</div>
          <div class="hint">Passiv bo'lsa, ilovada ko'rinadi, lekin buyurtma berib bo'lmaydi.</div>
        </div>
      </div>
      <div class="head-actions">
        <button v-if="!s.isEnabled && !s.isRequested && !s.isBlocked" class="btn" :disabled="busy" @click="requestListing">
          Ilovaga chiqish so'rovi
        </button>
        <button class="btn btn-ghost" :class="{ danger: s.isAcceptingOrders }" :disabled="busy" @click="toggleAccepting">
          {{ busy ? '...' : s.isAcceptingOrders ? 'Passiv qilish' : 'Faollashtirish' }}
        </button>
        <button class="btn" @click="edit">Tahrirlash</button>
      </div>
    </div>

    <!-- Joriy sozlamalar: ixcham ustunlar -->
    <div class="grid">
      <div v-for="sec in sections" :key="sec.title" class="card sec">
        <div class="sec-title">{{ sec.title }}</div>
        <div v-for="[k, val] in sec.rows" :key="k" class="kv">
          <span class="muted">{{ k }}</span>
          <strong>{{ val }}</strong>
        </div>
      </div>
    </div>
  </template>
</template>

<style scoped>
.lead { margin: -6px 0 12px; font-size: 14px; }
.hint { display: block; font-size: 12px; color: var(--text-muted, #64748b); font-weight: 400; line-height: 1.35; }

.top { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; margin-bottom: 12px; padding-top: 12px; padding-bottom: 12px; border-left: 4px solid #94a3b8; }
.top.on { border-left-color: #16a34a; }
.top.blocked { border-left-color: #dc2626; }
.status-dot { width: 12px; height: 12px; border-radius: 50%; background: #94a3b8; flex: none; }
.top.on .status-dot { background: #16a34a; box-shadow: 0 0 0 4px rgba(22,163,74,.15); }
.top.blocked .status-dot { background: #dc2626; box-shadow: 0 0 0 4px rgba(220,38,38,.15); }
.top-body { flex: 1; min-width: 220px; display: flex; flex-direction: column; gap: 2px; }
.status-title { font-weight: 700; font-size: 15px; }
.status-meta { display: flex; gap: 18px; flex-wrap: wrap; font-size: 13px; }
.top-accept { display: flex; align-items: center; gap: 10px; padding-left: 14px; border-left: 1px solid var(--border, #e5e7eb); min-width: 240px; }
.head-title { font-weight: 700; font-size: 14px; }
.head-actions { display: flex; gap: 8px; }
.pill { font-size: 12px; font-weight: 700; padding: 3px 10px; border-radius: 999px; flex: none; }
.pill.on { background: #dcfce7; color: #166534; }
.pill.off { background: #fee2e2; color: #991b1b; }

.grid { columns: 3 320px; column-gap: 12px; }
.sec { break-inside: avoid; margin-bottom: 12px; padding-top: 12px; padding-bottom: 8px; }
.sec-title { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted, #64748b); margin-bottom: 4px; }
.kv { display: flex; justify-content: space-between; gap: 12px; padding: 5px 0; font-size: 13px; border-top: 1px solid var(--border, #eef0f3); }
.kv:first-of-type { border-top: 0; }
.kv strong { text-align: right; }
</style>
