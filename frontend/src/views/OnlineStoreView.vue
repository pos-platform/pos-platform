<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { onlineStoreApi } from '../api/catalog'
import { errorMessage } from '../api/http'

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
  refereeBonus: 0,
  // Bosqich 10.1 / 10.5
  acceptWhenClosed: true,
  returnsEnabled: true,
  returnDays: 3,
  returnPickupByCourier: true,
  // Bosqich 10.6
  courierOfferMode: 'Broadcast',
  courierOfferTimeoutSec: 120
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
    refereeBonus: v.refereeBonus ?? 0,
    acceptWhenClosed: v.acceptWhenClosed !== false,
    returnsEnabled: v.returnsEnabled !== false,
    returnDays: v.returnDays ?? 3,
    returnPickupByCourier: v.returnPickupByCourier !== false,
    courierOfferMode: v.courierOfferMode || 'Broadcast',
    courierOfferTimeoutSec: v.courierOfferTimeoutSec ?? 120
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
  const returnDays = Number(form.returnDays)
  if (form.returnsEnabled && !(Number.isInteger(returnDays) && returnDays >= 1 && returnDays <= 30)) {
    err.value = 'Qaytarish muddati 1–30 kun bo\'lishi kerak.'
    return
  }
  const offerTimeout = Number(form.courierOfferTimeoutSec)
  if (form.courierOfferMode === 'Broadcast' && !(Number.isInteger(offerTimeout) && offerTimeout >= 30 && offerTimeout <= 1800)) {
    err.value = 'Kuryer taklifi kutish vaqti 30–1800 soniya bo\'lishi kerak.'
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
      refereeBonus: refeeBonus,
      acceptWhenClosed: form.acceptWhenClosed,
      returnsEnabled: form.returnsEnabled,
      returnDays: Number.isFinite(returnDays) && returnDays > 0 ? returnDays : 3,
      returnPickupByCourier: form.returnPickupByCourier,
      courierOfferMode: form.courierOfferMode,
      courierOfferTimeoutSec: Number.isFinite(offerTimeout) && offerTimeout > 0 ? offerTimeout : 120
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

onMounted(load)
</script>

<template>
  <div class="page-head">
    <h1>Online do'kon</h1>
  </div>
  <p class="muted lead">Mobil ilovadagi do'koningiz qanday ishlashini shu yerda sozlaysiz. O'zgartirgach pastdagi <b>Saqlash</b> tugmasini bosing.</p>

  <div v-if="err" class="alert alert-error">{{ err }}</div>
  <div v-if="ok" class="alert alert-success">{{ ok }}</div>
  <div v-if="loading" class="muted">Yuklanmoqda...</div>

  <template v-if="s">
    <!-- Holat -->
    <div class="card status" :class="{ on: s.isVisible, blocked: s.isBlocked }">
      <div class="status-dot"></div>
      <div class="status-body">
        <div class="status-title">{{ statusText }}</div>
        <div class="status-meta">
          <span>To'lov usullari: <b>{{ [s.allowCash && 'naqd', s.allowTransfer && 'kartaga o\'tkazma', s.allowCardOnDelivery && 'kuryer terminali', s.allowPayme && 'Payme', s.allowClick && 'Click'].filter(Boolean).join(', ') || 'yo\'q' }}</b></span>
          <span v-if="s.paidUntil">Tarif: <b>{{ new Date(s.paidUntil).toLocaleDateString('uz-UZ') }}</b> gacha</span>
        </div>
        <div class="hint">Ilovaga chiqish, bloklash va to'lov usullarini platforma boshqaradi.</div>
      </div>
      <button v-if="!s.isEnabled && !s.isRequested && !s.isBlocked" class="btn" :disabled="busy" @click="requestListing">
        Ilovaga chiqish so'rovi
      </button>
    </div>

    <form class="settings" @submit.prevent="save">
      <!-- Buyurtma qabul -->
      <section class="card sec">
        <label class="sw-row main">
          <span class="sw-text">
            <span class="sw-title">Hozir buyurtma qabul qilinadi</span>
            <span class="hint">O'chirsangiz, do'kon ilovada ko'rinadi, lekin mijozlar yangi buyurtma bera olmaydi (masalan, vaqtincha tanaffus).</span>
          </span>
          <input v-model="form.isAcceptingOrders" type="checkbox" class="sw" />
        </label>
      </section>

      <!-- Do'kon haqida -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Do'kon haqida</h3>
          <p>Mijoz ilovada ko'radigan ma'lumotlar.</p>
        </div>
        <div class="grid2">
          <div class="field">
            <label>Telefon raqam</label>
            <input v-model="form.phone" class="input" maxlength="20" placeholder="+998 90 123 45 67" />
            <span class="hint">Mijoz buyurtma bo'yicha shu raqamga qo'ng'iroq qiladi.</span>
          </div>
          <div class="field">
            <label>Qisqa tavsif</label>
            <textarea v-model="form.description" class="input" rows="2" maxlength="1000" placeholder="Masalan: Oziq-ovqat va maishiy mollar"></textarea>
          </div>
        </div>
      </section>

      <!-- Ish vaqti -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Ish vaqti</h3>
          <p>Ikkalasini bo'sh qoldirsangiz — do'kon kun bo'yi (24/7) ochiq hisoblanadi.</p>
        </div>
        <div class="grid3">
          <div class="field">
            <label>Ochilish vaqti</label>
            <input v-model="form.opensAt" type="time" class="input" />
          </div>
          <div class="field">
            <label>Yopilish vaqti</label>
            <input v-model="form.closesAt" type="time" class="input" />
          </div>
        </div>
        <label class="sw-row">
          <span class="sw-text">
            <span class="sw-title">Yopiq paytda ham buyurtma olish</span>
            <span class="hint">Mijoz kechasi buyurtma beradi — u ertalab do'kon ochilganda kassaga chiqadi.</span>
          </span>
          <input v-model="form.acceptWhenClosed" type="checkbox" class="sw" />
        </label>
      </section>

      <!-- Yetkazish -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Yetkazib berish va olib ketish</h3>
          <p>Mijoz buyurtmani qanday olishi mumkin.</p>
        </div>
        <div class="grid2">
          <label class="sw-row boxed">
            <span class="sw-text">
              <span class="sw-title">Yetkazib berish</span>
              <span class="hint">Kuryer mijoz manziliga olib boradi.</span>
            </span>
            <input v-model="form.deliveryEnabled" type="checkbox" class="sw" />
          </label>
          <label class="sw-row boxed">
            <span class="sw-text">
              <span class="sw-title">Do'kondan olib ketish</span>
              <span class="hint">Mijoz o'zi kelib oladi.</span>
            </span>
            <input v-model="form.pickupEnabled" type="checkbox" class="sw" />
          </label>
        </div>

        <template v-if="form.deliveryEnabled">
          <div class="sub">Narxlar</div>
          <div class="grid3">
            <div class="field">
              <label>Minimal buyurtma summasi</label>
              <div class="addon"><input v-model="form.minOrderAmount" type="number" min="0" class="input" /><span>so'm</span></div>
              <span class="hint">Bundan kam summaga buyurtma berib bo'lmaydi.</span>
            </div>
            <div class="field">
              <label>Yetkazish narxi</label>
              <div class="addon"><input v-model="form.deliveryFee" type="number" min="0" class="input" /><span>so'm</span></div>
              <span class="hint">Har bir buyurtmaga qo'shiladi.</span>
            </div>
            <div class="field">
              <label>Bepul yetkazish chegarasi</label>
              <div class="addon"><input v-model="form.freeDeliveryFrom" type="number" min="0" class="input" placeholder="bo'sh" /><span>so'm</span></div>
              <span class="hint">Buyurtma shu summadan oshsa yetkazish bepul. Bo'sh — doim pullik.</span>
            </div>
          </div>

          <div class="sub">Vaqt va hudud</div>
          <div class="grid3">
            <div class="field">
              <label>Yetkazish vaqti (taxminan)</label>
              <div class="range">
                <div class="addon"><input v-model="form.deliveryMinMinutes" type="number" min="5" max="1440" class="input" placeholder="30" /><span>daq</span></div>
                <span class="dash">—</span>
                <div class="addon"><input v-model="form.deliveryMaxMinutes" type="number" min="5" max="1440" class="input" placeholder="60" /><span>daq</span></div>
              </div>
              <span class="hint">Ilovada «30–60 daqiqada yetkaziladi» deb ko'rinadi.</span>
            </div>
            <div class="field">
              <label>Yetkazish radiusi</label>
              <div class="addon"><input v-model="form.deliveryRadiusKm" type="number" min="0.1" max="500" step="0.1" class="input" placeholder="cheklovsiz" /><span>km</span></div>
              <span class="hint">Do'kondan bundan uzoq manzilga buyurtma qabul qilinmaydi. Bo'sh — cheklovsiz.</span>
            </div>
            <div class="field">
              <label>Avtomatik bekor qilish</label>
              <div class="addon"><input v-model="form.autoCancelMinutes" type="number" min="0" max="1440" class="input" /><span>daq</span></div>
              <span class="hint">Xodim shu vaqt ichida buyurtmani qabul qilmasa, u bekor qilinadi. 0 — o'chiq.</span>
            </div>
          </div>

          <div class="sub">Soliq (fiskal chek)</div>
          <div class="grid3">
            <div class="field">
              <label>Yetkazish xizmati MXIK kodi</label>
              <input v-model="form.deliveryServiceMxikCode" class="input" maxlength="64" inputmode="numeric" placeholder="17 xonali kod" />
              <span class="hint">Kiritilsa, yetkazish narxi chekda alohida qator bo'lib chiqadi. Bo'sh — chekka kirmaydi.</span>
            </div>
          </div>
        </template>
      </section>

      <!-- Kuryer -->
      <section v-if="form.deliveryEnabled" class="card sec">
        <div class="sec-head">
          <h3>Kuryerga buyurtma berish</h3>
          <p>Buyurtma yig'ilib tayyor bo'lganda kuryer qanday tanlanadi.</p>
        </div>
        <div class="choice">
          <label class="opt" :class="{ act: form.courierOfferMode === 'Broadcast' }">
            <input v-model="form.courierOfferMode" type="radio" value="Broadcast" />
            <span>
              <b>Avtomatik</b>
              <span class="hint">Filialning barcha kuryerlariga taklif boradi, birinchi qabul qilgan oladi.</span>
            </span>
          </label>
          <label class="opt" :class="{ act: form.courierOfferMode === 'Off' }">
            <input v-model="form.courierOfferMode" type="radio" value="Off" />
            <span>
              <b>Qo'lda</b>
              <span class="hint">Kuryerni xodim o'zi biriktiradi.</span>
            </span>
          </label>
        </div>
        <div v-if="form.courierOfferMode === 'Broadcast'" class="grid3">
          <div class="field">
            <label>Kuryer javobini kutish</label>
            <div class="addon"><input v-model="form.courierOfferTimeoutSec" type="number" min="30" max="1800" class="input" /><span>soniya</span></div>
            <span class="hint">Hech kim qabul qilmasa, xodimga xabar beriladi.</span>
          </div>
        </div>
      </section>

      <!-- Kartaga o'tkazma -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Kartaga o'tkazma orqali to'lov</h3>
          <p>Mijoz pulni sizning kartangizga o'tkazadi. Karta raqami bo'sh bo'lsa, bu usul ilovada chiqmaydi.</p>
        </div>
        <div class="grid2">
          <div class="field">
            <label>Karta raqami</label>
            <input v-model="form.transferCardNumber" class="input" maxlength="32" placeholder="8600 0000 0000 0000" />
            <span class="hint">Mijozga buyurtma yig'ilgandan keyin ko'rsatiladi.</span>
          </div>
          <div class="field">
            <label>Karta egasining ismi</label>
            <input v-model="form.transferCardHolder" class="input" maxlength="200" placeholder="ALIYEV VALI" />
          </div>
        </div>
        <label class="sw-row">
          <span class="sw-text">
            <span class="sw-title">Pul tushganini tasdiqlamaguncha kuryerga bermaslik</span>
            <span class="hint">Xodim o'tkazmani tekshirib tasdiqlagandan keyingina buyurtma yo'lga chiqadi.</span>
          </span>
          <input v-model="form.requireTransferBeforeDispatch" type="checkbox" class="sw" />
        </label>
      </section>

      <!-- Bonus -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Bonuslar</h3>
          <p>Bonus ballar «Cashback va bonus» tizimidan olinadi. 1 ball = 1 so'm.</p>
        </div>
        <label class="sw-row">
          <span class="sw-text">
            <span class="sw-title">Bonus ball bilan to'lash</span>
            <span class="hint">Mijoz to'plagan ballari bilan buyurtmaning bir qismini to'laydi.</span>
          </span>
          <input v-model="form.bonusPayEnabled" type="checkbox" class="sw" />
        </label>
        <div v-if="form.bonusPayEnabled" class="grid3 indent">
          <div class="field">
            <label>Ko'pi bilan qancha qismini</label>
            <div class="addon"><input v-model="form.bonusMaxPercent" type="number" min="0" max="100" class="input" /><span>%</span></div>
            <span class="hint">Masalan, 30% — 100 000 so'mlik buyurtmaning 30 000 so'migacha.</span>
          </div>
        </div>

        <div class="divider"></div>

        <label class="sw-row">
          <span class="sw-text">
            <span class="sw-title">Do'stni taklif qilish</span>
            <span class="hint">Mijoz do'stini ilovaga taklif qilsa, ikkalasiga bonus beriladi. Bonus yangi mijozning birinchi buyurtmasi yetkazilganda yoziladi.</span>
          </span>
          <input v-model="form.referralEnabled" type="checkbox" class="sw" />
        </label>
        <div v-if="form.referralEnabled" class="grid3 indent">
          <div class="field">
            <label>Taklif qilgan mijozga</label>
            <div class="addon"><input v-model="form.referrerBonus" type="number" min="0" class="input" /><span>ball</span></div>
          </div>
          <div class="field">
            <label>Yangi kelgan mijozga</label>
            <div class="addon"><input v-model="form.refereeBonus" type="number" min="0" class="input" /><span>ball</span></div>
          </div>
        </div>
      </section>

      <!-- Qaytarish -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Tovarni qaytarish</h3>
        </div>
        <label class="sw-row">
          <span class="sw-text">
            <span class="sw-title">Mijoz ilovadan qaytarish so'rovi yubora oladi</span>
            <span class="hint">So'rov sizga keladi, siz tasdiqlaysiz yoki rad etasiz.</span>
          </span>
          <input v-model="form.returnsEnabled" type="checkbox" class="sw" />
        </label>
        <template v-if="form.returnsEnabled">
          <div class="grid3 indent">
            <div class="field">
              <label>Qaytarish muddati</label>
              <div class="addon"><input v-model="form.returnDays" type="number" min="1" max="30" class="input" /><span>kun</span></div>
              <span class="hint">Yetkazilgan kundan boshlab.</span>
            </div>
          </div>
          <label class="sw-row indent">
            <span class="sw-text">
              <span class="sw-title">Qaytariladigan tovarni kuryer olib ketadi</span>
              <span class="hint">O'chiq bo'lsa, mijoz tovarni o'zi do'konga olib keladi.</span>
            </span>
            <input v-model="form.returnPickupByCourier" type="checkbox" class="sw" />
          </label>
        </template>
      </section>

      <!-- Yordam -->
      <section class="card sec">
        <div class="sec-head">
          <h3>Yordam</h3>
          <p>Ilovadagi «Yordam» tugmasi bosilganda mijoz shu kontaktlarga yo'naltiriladi.</p>
        </div>
        <div class="grid2">
          <div class="field">
            <label>Yordam telefoni</label>
            <input v-model="form.supportPhone" class="input" maxlength="20" placeholder="+998 90 123 45 67" />
          </div>
          <div class="field">
            <label>Telegram username</label>
            <div class="addon pre"><span>@</span><input v-model="form.supportTelegram" class="input" maxlength="64" placeholder="mening_dokonim" /></div>
          </div>
        </div>
      </section>

      <div class="note">
        💡 Qaysi mahsulot ilovada ko'rinishini <b>«Mahsulotlar»</b> bo'limida har bir mahsulotdagi <b>«Ilovada»</b> tugmasi bilan belgilaysiz.
      </div>

      <div class="savebar">
        <span class="muted small">O'zgarishlar faqat «Saqlash» bosilgandan keyin kuchga kiradi.</span>
        <button class="btn" :disabled="busy">{{ busy ? 'Saqlanmoqda...' : 'Saqlash' }}</button>
      </div>
    </form>
  </template>
</template>

<style scoped>
.lead { margin: -6px 0 16px; font-size: 14px; }
.small { font-size: 12px; }
.hint { display: block; font-size: 12px; color: var(--text-muted, #64748b); font-weight: 400; line-height: 1.4; }

/* Holat kartasi */
.status { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; margin-bottom: 16px; border-left: 4px solid #94a3b8; }
.status.on { border-left-color: #16a34a; }
.status.blocked { border-left-color: #dc2626; }
.status-dot { width: 12px; height: 12px; border-radius: 50%; background: #94a3b8; flex: none; }
.status.on .status-dot { background: #16a34a; box-shadow: 0 0 0 4px rgba(22,163,74,.15); }
.status.blocked .status-dot { background: #dc2626; box-shadow: 0 0 0 4px rgba(220,38,38,.15); }
.status-body { flex: 1; min-width: 220px; display: flex; flex-direction: column; gap: 4px; }
.status-title { font-weight: 700; font-size: 16px; }
.status-meta { display: flex; gap: 18px; flex-wrap: wrap; font-size: 13px; }

/* Bo'limlar */
.settings { display: flex; flex-direction: column; gap: 16px; width: 100%; padding-bottom: 8px; }
.sec { display: flex; flex-direction: column; gap: 14px; }
.sec-head h3 { margin: 0; font-size: 16px; }
.sec-head p { margin: 4px 0 0; font-size: 13px; color: var(--text-muted, #64748b); }
.sub { font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted, #64748b); margin-top: 4px; padding-top: 12px; border-top: 1px dashed var(--border, #e2e8f0); }
.divider { height: 1px; background: var(--border, #e2e8f0); }
.indent { padding-left: 16px; border-left: 3px solid var(--primary-soft, #e0e7ff); }

.grid2 { display: grid; grid-template-columns: repeat(2, 1fr); gap: 14px; }
.grid3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }
.field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.field label { font-size: 13px; font-weight: 600; }

/* Birlik qo'shimchasi (so'm, km...) */
.addon { display: flex; align-items: stretch; }
.addon .input { flex: 1; min-width: 0; border-top-right-radius: 0; border-bottom-right-radius: 0; }
.addon > span { display: flex; align-items: center; padding: 0 10px; font-size: 13px; color: var(--text-muted, #64748b); background: var(--surface-2, #f1f5f9); border: 1px solid var(--border, #e2e8f0); border-left: 0; border-radius: 0 8px 8px 0; white-space: nowrap; }
.addon.pre .input { border-radius: 0 8px 8px 0; }
.addon.pre > span { border-left: 1px solid var(--border, #e2e8f0); border-right: 0; border-radius: 8px 0 0 8px; }
.range { display: flex; align-items: center; gap: 6px; }
.range .addon { flex: 1; min-width: 0; }
.dash { color: var(--text-muted, #64748b); }

/* Switch qatorlari */
.sw-row { display: flex; align-items: center; justify-content: space-between; gap: 16px; cursor: pointer; }
.sw-row.boxed { border: 1px solid var(--border, #e2e8f0); border-radius: 10px; padding: 12px 14px; }
.sw-text { display: flex; flex-direction: column; gap: 2px; }
.sw-title { font-size: 14px; font-weight: 600; }
.sw-row.main .sw-title { font-size: 16px; font-weight: 700; }
.sw { appearance: none; -webkit-appearance: none; flex: none; width: 42px; height: 24px; border-radius: 999px; background: #cbd5e1; position: relative; cursor: pointer; transition: background .15s; margin: 0; }
.sw::after { content: ''; position: absolute; top: 3px; left: 3px; width: 18px; height: 18px; border-radius: 50%; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,.2); transition: transform .15s; }
.sw:checked { background: var(--primary, #1f4fd1); }
.sw:checked::after { transform: translateX(18px); }
.sw:focus-visible { outline: 2px solid var(--primary, #1f4fd1); outline-offset: 2px; }

/* Radio tanlov */
.choice { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; }
.opt { display: flex; gap: 10px; align-items: flex-start; border: 1px solid var(--border, #e2e8f0); border-radius: 10px; padding: 12px 14px; cursor: pointer; font-size: 14px; }
.opt input { margin-top: 3px; }
.opt.act { border-color: var(--primary, #1f4fd1); background: var(--primary-soft, #eef2ff); }

.note { font-size: 13px; padding: 12px 14px; border-radius: 10px; background: var(--primary-soft, #eef2ff); }

/* Saqlash paneli */
.savebar { position: sticky; bottom: 0; z-index: 5; display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; padding: 12px 16px; background: var(--surface, #fff); border: 1px solid var(--border, #e2e8f0); border-radius: 12px; box-shadow: 0 -4px 16px rgba(15,23,42,.06); }
.savebar .btn { min-width: 140px; }

@media (max-width: 900px) { .grid3 { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 640px) { .grid2, .grid3, .choice { grid-template-columns: 1fr; } }
</style>
