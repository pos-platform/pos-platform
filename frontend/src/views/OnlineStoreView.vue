<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { onlineStoreApi } from '../api/catalog'
import { errorMessage, mediaUrl } from '../api/http'

// Do'kon admini: mobil ilovadagi online do'kon sozlamalari.
// Ilovaga chiqish, bloklash va to'lov usullarini SuperAdmin boshqaradi (bu yerda faqat ko'rinadi).
const s = ref(null)
const loading = ref(false)
const busy = ref(false)
const err = ref('')
const ok = ref('')

const form = reactive({
  isAcceptingOrders: true,
  displayName: '',
  description: '',
  logoUrl: '',
  phone: '',
  deliveryEnabled: true,
  pickupEnabled: true,
  minOrderAmount: 0,
  deliveryFee: 0,
  freeDeliveryFrom: '',
  deliveryMinMinutes: '',
  deliveryMaxMinutes: '',
  autoCancelMinutes: 30,
  opensAt: '',
  closesAt: '',
  transferCardNumber: '',
  transferCardHolder: '',
  requireTransferBeforeDispatch: true,
  deliveryRadiusKm: '',
  deliveryServiceMxikCode: '',
  bonusPayEnabled: false,
  bonusMaxPercent: 0,
  // Bosqich 9.3 / 9.4
  supportPhone: '',
  supportTelegram: '',
  referralEnabled: false,
  referrerBonus: 0,
  refereeBonus: 0
})

const statusText = computed(() => {
  const v = s.value
  if (!v) return ''
  if (v.isBlocked) return `Bloklangan: ${v.blockReason || ''}`
  if (v.isVisible) return 'Do\'koningiz ilovada ko\'rinmoqda'
  if (v.isEnabled) return 'Yoqilgan, lekin tarif muddati tugagan yoki tashkilot nofaol'
  if (v.isRequested) return 'So\'rov yuborilgan — platforma ko\'rib chiqmoqda'
  return 'Do\'kon ilovada emas'
})

function fill(v) {
  s.value = v
  Object.assign(form, {
    isAcceptingOrders: v.isAcceptingOrders,
    displayName: v.displayName || '',
    description: v.description || '',
    logoUrl: v.logoUrl || '',
    phone: v.phone || '',
    deliveryEnabled: v.deliveryEnabled,
    pickupEnabled: v.pickupEnabled,
    minOrderAmount: v.minOrderAmount,
    deliveryFee: v.deliveryFee,
    freeDeliveryFrom: v.freeDeliveryFrom ?? '',
    deliveryMinMinutes: v.deliveryMinMinutes ?? '',
    deliveryMaxMinutes: v.deliveryMaxMinutes ?? '',
    autoCancelMinutes: v.autoCancelMinutes ?? 30,
    opensAt: v.opensAt ? v.opensAt.substring(0, 5) : '',
    closesAt: v.closesAt ? v.closesAt.substring(0, 5) : '',
    transferCardNumber: v.transferCardNumber || '',
    transferCardHolder: v.transferCardHolder || '',
    requireTransferBeforeDispatch: v.requireTransferBeforeDispatch,
    deliveryRadiusKm: v.deliveryRadiusKm ?? '',
    deliveryServiceMxikCode: v.deliveryServiceMxikCode || '',
    bonusPayEnabled: !!v.bonusPayEnabled,
    bonusMaxPercent: v.bonusMaxPercent ?? 0,
    supportPhone: v.supportPhone || '',
    supportTelegram: v.supportTelegram || '',
    referralEnabled: !!v.referralEnabled,
    referrerBonus: v.referrerBonus ?? 0,
    refereeBonus: v.refereeBonus ?? 0
  })
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    fill(await onlineStoreApi.get())
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

// Bo'sh maydon — null.
const numOrNull = (v) => (v === '' || v === null || v === undefined ? null : Number(v))

async function save() {
  err.value = ''
  ok.value = ''
  // Yetkazish vaqti: ikkalasi birga yoki hech biri, 5..1440, "dan" <= "gacha".
  const dMin = numOrNull(form.deliveryMinMinutes)
  const dMax = numOrNull(form.deliveryMaxMinutes)
  if ((dMin === null) !== (dMax === null)) {
    err.value = 'Yetkazish vaqtining ikkala qiymatini kiriting yoki ikkalasini bo\'sh qoldiring.'
    return
  }
  if (dMin !== null && (dMin < 5 || dMax > 1440 || dMin > dMax)) {
    err.value = 'Yetkazish vaqti 5–1440 daqiqa oralig\'ida, "dan" ≤ "gacha" bo\'lishi kerak.'
    return
  }
  const autoCancel = numOrNull(form.autoCancelMinutes) ?? 0
  if (autoCancel < 0 || autoCancel > 1440) {
    err.value = 'Avtomatik bekor qilish 0–1440 daqiqa bo\'lishi kerak.'
    return
  }
  // Yetkazish radiusi: bo'sh — cheklovsiz
  const radius = numOrNull(form.deliveryRadiusKm)
  if (radius !== null && (radius <= 0 || radius > 500)) {
    err.value = 'Yetkazish radiusi 0 dan katta (500 km gacha) bo\'lishi yoki bo\'sh qolishi kerak.'
    return
  }
  // MXIK — faqat raqamlar (odatda 17 xonali), fiskal chek uchun
  const mxik = form.deliveryServiceMxikCode.replace(/\s/g, '')
  if (mxik && !/^\d{5,64}$/.test(mxik)) {
    err.value = 'Yetkazish xizmati MXIK kodi faqat raqamlardan iborat bo\'lishi kerak (odatda 17 xonali).'
    return
  }
  const bonusPct = Number(form.bonusMaxPercent) || 0
  if (bonusPct < 0 || bonusPct > 100) {
    err.value = 'Bonus bilan to\'lash ulushi 0–100% bo\'lishi kerak.'
    return
  }
  // Telegram: username, "@" va t.me havolasisiz
  const tg = form.supportTelegram.trim().replace(/^https?:\/\/(t\.me|telegram\.me)\//i, '').replace(/^@/, '')
  if (tg && !/^[A-Za-z0-9_]{4,32}$/.test(tg)) {
    err.value = 'Telegram username 4–32 belgi: lotin harflari, raqam va "_" ("@" siz).'
    return
  }
  const refBonus = Number(form.referrerBonus) || 0
  const refeeBonus = Number(form.refereeBonus) || 0
  if (refBonus < 0 || refeeBonus < 0) {
    err.value = 'Taklif bonuslari manfiy bo\'lmasin.'
    return
  }
  if (form.referralEnabled && refBonus === 0 && refeeBonus === 0) {
    err.value = '"Do\'stni taklif qilish" yoqilgan — kamida bitta bonusni kiriting.'
    return
  }
  busy.value = true
  try {
    fill(await onlineStoreApi.update({
      isAcceptingOrders: form.isAcceptingOrders,
      displayName: form.displayName.trim() || null,
      description: form.description.trim() || null,
      logoUrl: form.logoUrl || null,
      phone: form.phone.trim() || null,
      deliveryEnabled: form.deliveryEnabled,
      pickupEnabled: form.pickupEnabled,
      minOrderAmount: Number(form.minOrderAmount) || 0,
      deliveryFee: Number(form.deliveryFee) || 0,
      freeDeliveryFrom: form.freeDeliveryFrom === '' || form.freeDeliveryFrom === null ? null : Number(form.freeDeliveryFrom),
      deliveryMinMinutes: dMin,
      deliveryMaxMinutes: dMax,
      autoCancelMinutes: autoCancel,
      opensAt: form.opensAt ? `${form.opensAt}:00` : null,
      closesAt: form.closesAt ? `${form.closesAt}:00` : null,
      transferCardNumber: form.transferCardNumber.trim() || null,
      transferCardHolder: form.transferCardHolder.trim() || null,
      requireTransferBeforeDispatch: form.requireTransferBeforeDispatch,
      deliveryRadiusKm: radius,
      deliveryServiceMxikCode: mxik || null,
      bonusPayEnabled: form.bonusPayEnabled,
      bonusMaxPercent: bonusPct,
      supportPhone: form.supportPhone.trim() || null,
      supportTelegram: tg || null,
      referralEnabled: form.referralEnabled,
      referrerBonus: refBonus,
      refereeBonus: refeeBonus
    }))
    ok.value = 'Saqlandi.'
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
    fill(await onlineStoreApi.requestListing())
    ok.value = 'So\'rov yuborildi. Platforma tasdiqlagach do\'koningiz ilovada chiqadi.'
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function onLogo(ev) {
  const file = ev.target.files?.[0]
  if (!file) return
  try {
    const res = await onlineStoreApi.uploadLogo(file)
    form.logoUrl = res.url
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    ev.target.value = ''
  }
}

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Online do'kon</h1>
  </div>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>
  <div v-if="loading" class="muted">Yuklanmoqda...</div>

  <template v-if="s">
    <div class="card status" :class="{ on: s.isVisible, blocked: s.isBlocked }">
      <div>
        <div style="font-weight:700">{{ statusText }}</div>
        <div class="muted small">
          To'lov usullari (platforma belgilaydi):
          {{ [s.allowCash && 'naqd', s.allowTransfer && 'kartaga o\'tkazma', s.allowCardOnDelivery && 'kuryer terminali', s.allowPayme && 'Payme', s.allowClick && 'Click'].filter(Boolean).join(', ') || 'yo\'q' }}
          <span v-if="s.paidUntil"> · tarif {{ new Date(s.paidUntil).toLocaleDateString('uz-UZ') }} gacha</span>
        </div>
      </div>
      <button v-if="!s.isEnabled && !s.isRequested && !s.isBlocked" class="btn" :disabled="busy" @click="requestListing">
        Ilovaga chiqish so'rovi
      </button>
    </div>

    <form class="card form" @submit.prevent="save">
      <label class="row big"><input v-model="form.isAcceptingOrders" type="checkbox" /> Hozir buyurtma qabul qilinadi</label>

      <h3>Do'kon</h3>
      <div class="grid2">
        <div class="field">
          <label>Ilovadagi nomi (bo'sh — tashkilot nomi)</label>
          <input v-model="form.displayName" class="input" maxlength="200" />
        </div>
        <div class="field">
          <label>Telefon (mijozlar qo'ng'iroq qiladi)</label>
          <input v-model="form.phone" class="input" maxlength="20" placeholder="+998 90 123 45 67" />
        </div>
      </div>
      <div class="field">
        <label>Tavsif</label>
        <textarea v-model="form.description" class="input" rows="2" maxlength="1000"></textarea>
      </div>
      <div class="field">
        <label>Logo</label>
        <div class="logo-row">
          <div class="logo"><img v-if="form.logoUrl" :src="mediaUrl(form.logoUrl)" alt="" /></div>
          <input type="file" accept="image/*" @change="onLogo" />
          <button v-if="form.logoUrl" type="button" class="btn btn-ghost btn-sm" @click="form.logoUrl = ''">Olib tashlash</button>
        </div>
      </div>
      <div class="grid2">
        <div class="field">
          <label>Ochiladi</label>
          <input v-model="form.opensAt" type="time" class="input" />
        </div>
        <div class="field">
          <label>Yopiladi (bo'sh — kun bo'yi)</label>
          <input v-model="form.closesAt" type="time" class="input" />
        </div>
      </div>

      <h3>Yetkazish va olib ketish</h3>
      <label class="row"><input v-model="form.deliveryEnabled" type="checkbox" /> Yetkazib berish</label>
      <label class="row"><input v-model="form.pickupEnabled" type="checkbox" /> Do'kondan olib ketish</label>
      <div class="grid3">
        <div class="field">
          <label>Minimal buyurtma, so'm</label>
          <input v-model="form.minOrderAmount" type="number" min="0" class="input" />
        </div>
        <div class="field">
          <label>Yetkazish narxi, so'm</label>
          <input v-model="form.deliveryFee" type="number" min="0" class="input" />
        </div>
        <div class="field">
          <label>Bepul yetkazish (dan), so'm</label>
          <input v-model="form.freeDeliveryFrom" type="number" min="0" class="input" placeholder="bo'sh — doim pullik" />
        </div>
      </div>
      <div class="grid3">
        <div class="field">
          <label>Yetkazish vaqti, daqiqa: dan</label>
          <input v-model="form.deliveryMinMinutes" type="number" min="5" max="1440" class="input" placeholder="masalan, 30" />
        </div>
        <div class="field">
          <label>gacha</label>
          <input v-model="form.deliveryMaxMinutes" type="number" min="5" max="1440" class="input" placeholder="masalan, 60" />
        </div>
        <div class="field">
          <label>Javobsiz buyurtmani avtomatik bekor qilish, daqiqa (0 — o'chiq)</label>
          <input v-model="form.autoCancelMinutes" type="number" min="0" max="1440" class="input" />
        </div>
      </div>

      <div class="grid2">
        <div class="field">
          <label>Yetkazish radiusi, km (bo'sh — cheklovsiz)</label>
          <input v-model="form.deliveryRadiusKm" type="number" min="0.1" max="500" step="0.1" class="input" placeholder="masalan, 7" />
          <span class="muted small">Do'kondan shu masofadan uzoq manzilga buyurtma qabul qilinmaydi. Manzil koordinatasi bo'lmasa — ogohlantirish bilan ruxsat.</span>
        </div>
        <div class="field">
          <label>Yetkazish xizmati MXIK kodi</label>
          <input v-model="form.deliveryServiceMxikCode" class="input" maxlength="64" inputmode="numeric" placeholder="17 xonali kod" />
          <span class="muted small">Kiritilsa, yetkazish narxi fiskal chekka "Yetkazib berish xizmati" qatori sifatida kiradi. Bo'sh — chekka kirmaydi.</span>
        </div>
      </div>

      <h3>Bonus bilan to'lash</h3>
      <label class="row"><input v-model="form.bonusPayEnabled" type="checkbox" /> Mijoz to'plagan bonus ballari bilan to'lay oladi (1 ball = 1 so'm)</label>
      <div v-if="form.bonusPayEnabled" class="grid3">
        <div class="field">
          <label>Mahsulotlar summasining ko'pi bilan, %</label>
          <input v-model="form.bonusMaxPercent" type="number" min="0" max="100" class="input" />
        </div>
      </div>

      <h3>Yordam (ilovadagi "Yordam" tugmasi)</h3>
      <div class="grid2">
        <div class="field">
          <label>Yordam telefoni</label>
          <input v-model="form.supportPhone" class="input" maxlength="20" placeholder="+998 90 123 45 67" />
        </div>
        <div class="field">
          <label>Telegram (username, "@" siz)</label>
          <input v-model="form.supportTelegram" class="input" maxlength="64" placeholder="mening_dokonim" />
        </div>
      </div>

      <h3>Do'stni taklif qilish</h3>
      <label class="row"><input v-model="form.referralEnabled" type="checkbox" /> Mijoz do'stini taklif qilsa, ikkalasiga bonus ball beriladi</label>
      <div v-if="form.referralEnabled" class="grid3">
        <div class="field">
          <label>Taklif qilganga, ball</label>
          <input v-model="form.referrerBonus" type="number" min="0" class="input" />
        </div>
        <div class="field">
          <label>Taklif qilinganga (yangi mijoz), ball</label>
          <input v-model="form.refereeBonus" type="number" min="0" class="input" />
        </div>
      </div>
      <p v-if="form.referralEnabled" class="muted small" style="margin:0">
        Bonus yangi mijozning birinchi buyurtmasi yetkazilganda yoziladi (1 ball = 1 so'm, "Cashback va bonus" tizimi orqali).
      </p>

      <h3>Kartaga o'tkazma</h3>
      <p class="muted small" style="margin:0">Karta raqami mijozga buyurtma yig'ilgandan keyin ko'rsatiladi. Raqam bo'lmasa o'tkazma ilovada chiqmaydi.</p>
      <div class="grid2">
        <div class="field">
          <label>Karta raqami</label>
          <input v-model="form.transferCardNumber" class="input" maxlength="32" placeholder="8600 0000 0000 0000" />
        </div>
        <div class="field">
          <label>Karta egasi</label>
          <input v-model="form.transferCardHolder" class="input" maxlength="200" />
        </div>
      </div>
      <label class="row">
        <input v-model="form.requireTransferBeforeDispatch" type="checkbox" />
        O'tkazma kuryerga berishdan oldin tasdiqlanishi shart
      </label>

      <div>
        <button class="btn" :disabled="busy">{{ busy ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </div>
      <p class="muted small" style="margin:0">
        Qaysi mahsulot ilovada ko'rinishi — "Mahsulotlar" bo'limida har bir mahsulotdagi "Ilovada" tugmasi bilan belgilanadi.
      </p>
    </form>
  </template>
</template>

<style scoped>
.status { display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap; margin-bottom:12px; border-left:4px solid #94a3b8; }
.status.on { border-left-color:#16a34a; }
.status.blocked { border-left-color:#dc2626; }
.form { display:flex; flex-direction:column; gap:10px; max-width:860px; }
.form h3 { margin:10px 0 0; font-size:15px; }
.row { display:flex; align-items:center; gap:8px; font-size:14px; }
.row.big { font-weight:700; font-size:15px; }
.small { font-size:12px; }
.grid2 { display:grid; grid-template-columns:1fr 1fr; gap:10px; }
.grid3 { display:grid; grid-template-columns:repeat(3, 1fr); gap:10px; }
.field { display:flex; flex-direction:column; gap:4px; }
.field label { font-size:13px; color:var(--text-muted); font-weight:600; }
.logo-row { display:flex; align-items:center; gap:10px; flex-wrap:wrap; }
.logo { width:56px; height:56px; border-radius:12px; background:#f1f5f9; overflow:hidden; }
.logo img { width:100%; height:100%; object-fit:cover; }
@media (max-width: 700px) { .grid2, .grid3 { grid-template-columns:1fr; } }
</style>
