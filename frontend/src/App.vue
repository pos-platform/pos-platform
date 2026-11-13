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

// Mobil off-canvas menyu holati. Sahifa almashganda avtomatik yopiladi. hugoo =>HUsan
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

// Sidebar menyu 3 ta mantiqiy tipga bo'lingan.
// admin: true bo'lsa faqat Admin ko'radi. group — qaysi bo'limga tegishli.
const navItems = [
  // 1) Savdo va kassa
  { name: 'dashboard', key: 'nav.dashboard', icon: '📊', admin: true, group: 'sales' },
  { name: 'pos', key: 'nav.pos', icon: '🛒', cashierOnly: true, group: 'sales' },
  { name: 'tables', key: 'nav.tables', icon: '🍽️', admin: true, restaurantOnly: true, group: 'sales' },
  { name: 'dishes', key: 'nav.dishes', icon: '🍲', admin: true, restaurantOnly: true, group: 'sales' },
  { name: 'kitchen-issues', key: 'nav.kitchenIssues', icon: '👨‍🍳', admin: true, restaurantOnly: true, group: 'sales' },
  { name: 'sales', key: 'nav.sales', icon: '🧾', perm: 'sales.view', group: 'sales' },
  { name: 'shifts', key: 'nav.shifts', icon: '⏱️', group: 'sales' },
  { name: 'customers', key: 'nav.customers', icon: '🙋', admin: true, perm: 'customers.view', group: 'sales' },
  { name: 'debtors', key: 'nav.debtors', icon: '📕', admin: true, perm: 'debtors.view', group: 'sales' },
  { name: 'promotions', key: 'nav.promotions', icon: '🎉', admin: true, group: 'sales' },
  { name: 'loyalty', key: 'nav.loyalty', icon: '💳', admin: true, perm: 'settings.manage', group: 'sales' },
  { name: 'online-orders', key: 'nav.onlineOrders', icon: '📦', admin: true, perm: 'sales.view', storeOnly: true, group: 'sales' },
  { name: 'promo-codes', key: 'nav.promoCodes', icon: '🎟️', admin: true, perm: 'settings.manage', storeOnly: true, group: 'sales' },
  { name: 'online-store', key: 'nav.onlineStore', icon: '📱', admin: true, perm: 'settings.manage', storeOnly: true, group: 'sales' },
  { name: 'online-marketing', key: 'nav.onlineMarketing', icon: '📣', admin: true, perm: 'settings.manage', storeOnly: true, group: 'sales' },

  // 2) Katalog va ombor
  { name: 'products', key: 'nav.products', icon: '📦', perm: 'products.view', group: 'catalog' },
  { name: 'categories', key: 'nav.categories', icon: '🏷️', admin: true, perm: 'categories.view', group: 'catalog' },
  { name: 'stores', key: 'nav.stores', icon: '🏪', admin: true, perm: 'branches.view', group: 'catalog' },
  { name: 'suppliers', key: 'nav.suppliers', icon: '🚚', admin: true, perm: 'suppliers.view', group: 'catalog' },
  { name: 'stock-receipts', key: 'nav.stockReceipts', icon: '📥', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'stock-transfers', key: 'nav.stockTransfers', icon: '🔄', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'write-offs', key: 'nav.writeOffs', icon: '🗑️', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'inventories', key: 'nav.inventories', icon: '🔢', admin: true, perm: 'inventory.view', group: 'catalog' },

  // 3) Moliya va boshqaruv
  { name: 'expenses', key: 'nav.expenses', icon: '💸', admin: true, perm: 'expenses.view', group: 'finance' },
  { name: 'payments', key: 'nav.payments', icon: '💳', admin: true, perm: 'cashbox.view', group: 'finance' },
  { name: 'cash-movements', key: 'nav.cashMovements', icon: '💵', admin: true, perm: 'cashbox.view', group: 'finance' },
  { name: 'analytics', key: 'nav.analytics', icon: '📈', admin: true, perm: 'reports.view', group: 'finance' },
  { name: 'reports', key: 'nav.reports', icon: '📊', admin: true, perm: 'reports.view', group: 'finance' },
  { name: 'users', key: 'nav.users', icon: '👥', admin: true, perm: 'users.view', group: 'finance' },
  // Rollar boshqaruvi faqat SuperAdmin'da (superNavItems → admin-roles). Org admin ko'rmaydi.
  { name: 'settings', key: 'nav.settings', icon: '⚙️', admin: true, perm: 'settings.manage', group: 'finance' }
]

// Bo'lim tartibi, sarlavhalari va vizual belgilari (ikonka)
const navGroups = [
  { id: 'sales', label: 'nav.groupSales', icon: '🛒' },
  { id: 'catalog', label: 'nav.groupCatalog', icon: '📦' },
  { id: 'finance', label: 'nav.groupFinance', icon: '📊' }
]

// SuperAdmin (platforma egasi) faqat tashkilotlarni boshqaradi.
const superNavItems = [
  { name: 'organizations', key: 'nav.organizations', icon: '🏢' },
  { name: 'admin-roles', key: 'nav.roles', icon: '🔑' },
  { name: 'audit', key: 'nav.audit', icon: '🛡️' },
  { name: 'fiscal-audit', key: 'nav.fiscalAudit', icon: '🧾' },
  { name: 'admin-online-stores', key: 'nav.onlineStores', icon: '📱' }
]

function canSee(i) {
  if (i.admin && auth.role !== 'Admin') return false
  if (i.cashierOnly && auth.role === 'Admin') return false
  if (i.restaurantOnly && !auth.isRestaurant) return false
  if (i.storeOnly && auth.isRestaurant) return false
  if (i.perm && !auth.hasPermission(i.perm)) return false
  return true
}

// Guruhlangan, ruxsatga qarab filtrlangan menyu. Bo'sh guruhlar ko'rsatilmaydi.
const visibleGroups = computed(() => {
  return navGroups
    .map((g) => ({ ...g, items: navItems.filter((i) => i.group === g.id && canSee(i)) }))
    .filter((g) => g.items.length > 0)
})

// Bo'limlarni ochib-yopish (akkordeon). Holati localStorage'da saqlanadi.
const COLLAPSE_KEY = 'pos_nav_collapsed'
function loadCollapsed() {
  try { return JSON.parse(localStorage.getItem(COLLAPSE_KEY)) || {} } catch { return {} }
}
const collapsedGroups = ref(loadCollapsed())
function isCollapsed(id) { return !!collapsedGroups.value[id] }
function toggleGroup(id) {
  collapsedGroups.value = { ...collapsedGroups.value, [id]: !collapsedGroups.value[id] }
  localStorage.setItem(COLLAPSE_KEY, JSON.stringify(collapsedGroups.value))
}

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
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosCloud' }}</span>
      </div>
    </header>

    <!-- Drawer ochiq bo'lganda orqa fon -->
    <div v-if="showNav && mobileNavOpen" class="nav-backdrop" @click="mobileNavOpen = false"></div>

    <aside v-if="showNav" class="sidebar">
      <div class="sidebar-brand">
        <img src="/logo.png" alt="Logo" class="brand-img" />
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosCloud' }}</span>
        <button class="sidebar-close" aria-label="Yopish" @click="mobileNavOpen = false">×</button>
      </div>

      <nav class="side-nav">
        <!-- SuperAdmin: guruhsiz oddiy ro'yxat -->
        <template v-if="auth.isSuperAdmin">
          <RouterLink
            v-for="item in superNavItems"
            :key="item.name"
            :to="{ name: item.name }"
            class="side-link"
          >
            <span class="side-ico">{{ item.icon }}</span>
            <span class="side-label">{{ t(item.key) }}</span>
          </RouterLink>
        </template>

        <!-- Admin/kassir: 3 ta tipga bo'lingan, ochilib-yopiladigan menyu -->
        <template v-else>
          <div
            v-for="g in visibleGroups"
            :key="g.id"
            class="side-group"
            :class="{ collapsed: isCollapsed(g.id) }"
          >
            <button type="button" class="side-group-label" @click="toggleGroup(g.id)">
              <span class="side-group-ico">{{ g.icon }}</span>
              <span class="side-group-text">{{ t(g.label) }}</span>
              <span class="side-group-chevron">▾</span>
            </button>
            <div v-show="!isCollapsed(g.id)" class="side-group-items">
              <RouterLink
                v-for="item in g.items"
                :key="item.name"
                :to="{ name: item.name }"
                class="side-link"
              >
                <span class="side-ico">{{ item.icon }}</span>
                <span class="side-label">{{ t(item.key) }}</span>
              </RouterLink>
            </div>
          </div>
        </template>
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
          <div class="side-avatar">{{ (auth.username || '?').charAt(0).toUpperCase() }}</div>
          <div class="side-user-meta">
            <div class="side-username">{{ auth.username }}</div>
            <span class="badge">{{ auth.role }}</span>
          </div>
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
