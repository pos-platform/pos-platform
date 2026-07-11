<script setup>
import { computed, watch, ref } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from './stores/auth'
import { useStoreFilterStore } from './stores/storeFilter'
import { usersApi } from './api/catalog'

const auth = useAuthStore()
const storeFilter = useStoreFilterStore()
const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()

function setLang(lang) {
  locale.value = lang
  localStorage.setItem('lang', lang)
}

const showNav = computed(() => auth.isAuthenticated && route.name !== 'login')

// Mobil off-canvas menyu holati. Sahifa almashganda avtomatik yopiladi.
const mobileNavOpen = ref(false)
watch(() => route.name, () => { mobileNavOpen.value = false })

// ===== Obuna (to'lov) eslatmasi — org admin uchun =====
// Kassir/menejer obunasi 7 kun ichida tugasa, tugagan yoki to'lanmagan bo'lsa eslatadi.
const licenseAlerts = ref([])
const licenseDismissed = ref(false)

async function loadLicenseAlerts() {
  licenseAlerts.value = []
  if (!auth.isAuthenticated || auth.isSuperAdmin || auth.role !== 'Admin') return
  try {
    const data = await usersApi.licenseSummary()
    licenseAlerts.value = data.needsAttention || []
  } catch {
    // jim — eslatma asosiy ish jarayoniga xalal bermasin
  }
}

const showLicenseBanner = computed(
  () => !licenseDismissed.value && licenseAlerts.value.length > 0 && route.name !== 'login'
)

// Banner uchun matn (eng shoshilinch holatga qarab).
const licenseBannerText = computed(() => {
  const items = licenseAlerts.value
  if (!items.length) return ''
  const ru = locale.value === 'ru'
  const parts = items.map((u) => {
    if (u.status === 'unpaid')
      return ru ? `«${u.username}» — не оплачено` : `«${u.username}» — to'lanmagan`
    if (u.status === 'expired')
      return ru ? `«${u.username}» — срок истёк` : `«${u.username}» — muddati tugagan`
    const d = u.daysLeft
    return ru ? `«${u.username}» — осталось ${d} дн.` : `«${u.username}» — ${d} kun qoldi`
  })
  const head = ru
    ? 'Уважаемый клиент! Не забудьте оплатить подписку, чтобы избежать отключения: '
    : "Hurmatli mijoz! Tizim to'xtab qolmasligi uchun obunani to'lashni unutmang: "
  return head + parts.join(' · ')
})

// Foydalanuvchi tizimga kirganida do'konlar ro'yxatini + obuna eslatmalarini yuklaydi
watch(
  () => auth.isAuthenticated,
  (v) => {
    if (v && !auth.isSuperAdmin) storeFilter.ensureLoaded()
    licenseDismissed.value = false
    loadLicenseAlerts()
  },
  { immediate: true }
)

// Sidebar menyu. admin: true bo'lsa faqat Admin ko'radi.
const navItems = [
  { name: 'dashboard', key: 'nav.dashboard', icon: '📊', admin: true },
  { name: 'pos', key: 'nav.pos', icon: '🛒', cashierOnly: true },
  { name: 'tables', key: 'nav.tables', icon: '🍽️', admin: true, restaurantOnly: true },
  { name: 'dishes', key: 'nav.dishes', icon: '🍲', admin: true, restaurantOnly: true },
  { name: 'kitchen-issues', key: 'nav.kitchenIssues', icon: '👨‍🍳', admin: true, restaurantOnly: true },
  { name: 'stores', key: 'nav.stores', icon: '🏪', admin: true, perm: 'branches.view' },
  { name: 'products', key: 'nav.products', icon: '📦', perm: 'products.view' },
  { name: 'categories', key: 'nav.categories', icon: '🏷️', admin: true, perm: 'categories.view' },
  { name: 'sales', key: 'nav.sales', icon: '🧾', perm: 'sales.view' },
  { name: 'shifts', key: 'nav.shifts', icon: '⏱️' },
  { name: 'suppliers', key: 'nav.suppliers', icon: '🚚', admin: true, perm: 'suppliers.view' },
  { name: 'stock-receipts', key: 'nav.stockReceipts', icon: '📥', admin: true, perm: 'inventory.view' },
  { name: 'debtors', key: 'nav.debtors', icon: '📕', admin: true, perm: 'debtors.view' },
  { name: 'expenses', key: 'nav.expenses', icon: '💸', admin: true, perm: 'expenses.view' },
  { name: 'payments', key: 'nav.payments', icon: '💳', admin: true, perm: 'cashbox.view' },
  { name: 'write-offs', key: 'nav.writeOffs', icon: '🗑️', admin: true, perm: 'inventory.view' },
  { name: 'inventories', key: 'nav.inventories', icon: '🔢', admin: true, perm: 'inventory.view' },
  { name: 'analytics', key: 'nav.analytics', icon: '📈', admin: true, perm: 'reports.view' },
  { name: 'reports', key: 'nav.reports', icon: '📊', admin: true, perm: 'reports.view' },
  { name: 'promotions', key: 'nav.promotions', icon: '🏷️', admin: true },
  { name: 'stock-transfers', key: 'nav.stockTransfers', icon: '🔄', admin: true, perm: 'inventory.view' },
  { name: 'cash-movements', key: 'nav.cashMovements', icon: '💵', admin: true, perm: 'cashbox.view' },
  { name: 'customers', key: 'nav.customers', icon: '🙋', admin: true, perm: 'customers.view' },
  { name: 'users', key: 'nav.users', icon: '👥', admin: true, perm: 'users.view' },
  // Rollar boshqaruvi faqat SuperAdmin'da (superNavItems → admin-roles). Org admin ko'rmaydi.
  { name: 'settings', key: 'nav.settings', icon: '⚙️', admin: true, perm: 'settings.manage' }
]

// SuperAdmin (platforma egasi) faqat tashkilotlarni boshqaradi.
const superNavItems = [
  { name: 'organizations', key: 'nav.organizations', icon: '🏢' },
  { name: 'admin-roles', key: 'nav.roles', icon: '🔑' },
  { name: 'audit', key: 'nav.audit', icon: '🛡️' }
]

const visibleNav = computed(() => {
  if (auth.isSuperAdmin) return superNavItems
  return navItems.filter((i) => {
    if (i.admin && auth.role !== 'Admin') return false
    if (i.cashierOnly && auth.role === 'Admin') return false
    if (i.restaurantOnly && !auth.isRestaurant) return false
    if (i.perm && !auth.hasPermission(i.perm)) return false
    return true
  })
})

function logout() {
  storeFilter.reset()
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="app-shell" :class="{ 'no-nav': !showNav, 'nav-open': mobileNavOpen }">
    <!-- Mobil yuqori panel: hamburger + brend (faqat kichik ekranda ko'rinadi) -->
    <header v-if="showNav" class="mobile-topbar">
      <button class="hamb" aria-label="Menyu" @click="mobileNavOpen = true">☰</button>
      <div class="mt-brand">
        <img src="/logo.png" alt="" />
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosPlatform' }}</span>
      </div>
    </header>

    <!-- Drawer ochiq bo'lganda orqa fon -->
    <div v-if="showNav && mobileNavOpen" class="nav-backdrop" @click="mobileNavOpen = false"></div>

    <aside v-if="showNav" class="sidebar">
      <div class="sidebar-brand">
        <img src="/logo.png" alt="Logo" class="brand-img" />
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosPlatform' }}</span>
        <button class="sidebar-close" aria-label="Yopish" @click="mobileNavOpen = false">×</button>
      </div>

      <nav class="side-nav">
        <RouterLink
          v-for="item in visibleNav"
          :key="item.name"
          :to="{ name: item.name }"
          class="side-link"
        >
          <span class="side-ico">{{ item.icon }}</span>
          <span class="side-label">{{ t(item.key) }}</span>
        </RouterLink>
      </nav>

      <!-- Ko'p filiali bo'lgan Admin uchun global do'kon filtri -->
      <div v-if="!auth.isSuperAdmin && storeFilter.multiStore" class="sidebar-store-filter">
        <div class="side-filter-label">Do'kon</div>
        <select
          :value="storeFilter.selectedBranchId"
          class="side-store-select"
          @change="storeFilter.select($event.target.value)"
        >
          <option value="">Barcha do'konlar</option>
          <option v-for="s in storeFilter.stores" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>

      <div class="sidebar-foot">
        <div class="side-lang">
          <button :class="['lang-btn', { active: locale === 'uz' }]" @click="setLang('uz')">UZ</button>
          <button :class="['lang-btn', { active: locale === 'ru' }]" @click="setLang('ru')">RU</button>
        </div>
        <div class="side-user">
          <div class="side-username">{{ auth.username }}</div>
          <span class="badge">{{ auth.role }}</span>
        </div>
        <button class="btn btn-ghost btn-sm" style="width:100%; margin-top:8px" @click="logout">{{ t('common.logout') }}</button>
      </div>
    </aside>

    <main class="content">
      <!-- Obuna to'lovi eslatmasi (org admin uchun) -->
      <div v-if="showLicenseBanner" class="license-banner">
        <div class="license-marquee">
          <span class="license-text">{{ licenseBannerText }}</span>
          <span class="license-text" aria-hidden="true">{{ licenseBannerText }}</span>
        </div>
        <button class="license-x" title="Yopish" @click="licenseDismissed = true">×</button>
      </div>
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
/* Obuna to'lovi eslatmasi (yuguruvchi qator) */
.license-banner {
  position: relative;
  display: flex;
  align-items: center;
  background: #fff1f1;
  border: 1px solid #fca5a5;
  border-radius: 10px;
  margin: 0 0 14px;
  overflow: hidden;
  height: 40px;
}
.license-marquee {
  display: flex;
  flex: 1;
  min-width: 0;
  white-space: nowrap;
  animation: license-scroll 28s linear infinite;
}
.license-text {
  padding: 0 28px;
  font-weight: 700;
  font-size: 14px;
  color: #dc2626;
}
.license-banner:hover .license-marquee { animation-play-state: paused; }
.license-x {
  flex-shrink: 0;
  border: none;
  background: transparent;
  color: #dc2626;
  font-size: 22px;
  line-height: 1;
  cursor: pointer;
  padding: 0 12px;
}
@keyframes license-scroll {
  0% { transform: translateX(0); }
  100% { transform: translateX(-50%); }
}

.side-lang { display:flex; gap:6px; margin-bottom:8px; }
.lang-btn { flex:1; padding:4px; border:1px solid var(--border); border-radius:4px; background:transparent; cursor:pointer; font-size:11px; color:var(--text-muted); }
.lang-btn.active { background:var(--primary); color:#fff; border-color:var(--primary); }
</style>
