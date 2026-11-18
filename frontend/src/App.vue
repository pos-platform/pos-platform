<script setup>
import { computed, watch, ref, onBeforeUnmount } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from './stores/auth'
import { useStoreFilterStore } from './stores/storeFilter'
import { usersApi, adminErrorsApi } from './api/catalog'
import AppIcon from './components/AppIcon.vue'

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

// Sidebar: chapda bo'limlar (ikonka), yonida tanlangan bo'lim sahifalari.
// admin: true bo'lsa faqat Admin ko'radi. group — qaysi bo'limga tegishli.
const navItems = [
  // 1) Savdo
  { name: 'dashboard', key: 'nav.dashboard', icon: 'dashboard', admin: true, group: 'sales' },
  { name: 'pos', key: 'nav.pos', icon: 'cart', cashierOnly: true, group: 'sales' },
  { name: 'sales', key: 'nav.sales', icon: 'receipt', perm: 'sales.view', group: 'sales' },
  { name: 'shifts', key: 'nav.shifts', icon: 'clock', group: 'sales' },
  { name: 'customers', key: 'nav.customers', icon: 'users', admin: true, perm: 'customers.view', group: 'sales' },
  { name: 'debtors', key: 'nav.debtors', icon: 'book', admin: true, perm: 'debtors.view', group: 'sales' },

  // 2) Ombor
  { name: 'products', key: 'nav.products', icon: 'package', perm: 'products.view', group: 'catalog' },
  { name: 'stock-receipts', key: 'nav.stockReceipts', icon: 'inbox', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'write-offs', key: 'nav.writeOffs', icon: 'trash', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'stock-transfers', key: 'nav.stockTransfers', icon: 'transfer', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'inventories', key: 'nav.inventories', icon: 'clipboardCheck', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'stock-turnover', key: 'nav.stockTurnover', icon: 'list', admin: true, perm: 'inventory.view', group: 'catalog' },
  { name: 'categories', key: 'nav.categories', icon: 'tag', admin: true, perm: 'categories.view', group: 'catalog' },
  { name: 'suppliers', key: 'nav.suppliers', icon: 'truck', admin: true, perm: 'suppliers.view', group: 'catalog' },

  // 3) Marketing
  { name: 'promotions', key: 'nav.promotions', icon: 'percent', admin: true, group: 'marketing' },
  { name: 'promo-codes', key: 'nav.promoCodes', icon: 'ticket', admin: true, perm: 'settings.manage', group: 'marketing' },
  { name: 'loyalty', key: 'nav.loyalty', icon: 'star', admin: true, perm: 'settings.manage', group: 'marketing' },

  // 4) Mobil ilova (onlayn do'kon)
  { name: 'online-orders', key: 'nav.onlineOrders', icon: 'bag', admin: true, perm: 'sales.view', group: 'mobile' },
  { name: 'online-store', key: 'nav.onlineStore', icon: 'phone', admin: true, perm: 'settings.manage', group: 'mobile' },
  { name: 'online-marketing', key: 'nav.onlineMarketing', icon: 'megaphone', admin: true, perm: 'settings.manage', group: 'mobile' },

  // 4) Moliya
  { name: 'payments', key: 'nav.payments', icon: 'wallet', admin: true, perm: 'cashbox.view', group: 'finance' },
  { name: 'cash-movements', key: 'nav.cashMovements', icon: 'banknote', admin: true, perm: 'cashbox.view', group: 'finance' },
  { name: 'expenses', key: 'nav.expenses', icon: 'trendDown', admin: true, perm: 'expenses.view', group: 'finance' },

  // 5) Hisobotlar
  { name: 'reports', key: 'nav.reports', icon: 'barChart', admin: true, perm: 'reports.view', group: 'reports' },
  { name: 'analytics', key: 'nav.analytics', icon: 'trendUp', admin: true, perm: 'reports.view', group: 'reports' },
  { name: 'sold-items', key: 'nav.soldItems', icon: 'fileText', admin: true, perm: 'reports.view', group: 'reports' },

  // 6) Sozlamalar
  { name: 'stores', key: 'nav.stores', icon: 'store', admin: true, perm: 'branches.view', group: 'settings' },
  { name: 'users', key: 'nav.users', icon: 'user', admin: true, perm: 'users.view', group: 'settings' },
  // Rollar boshqaruvi faqat SuperAdmin'da (superNavItems → admin-roles). Org admin ko'rmaydi.
  { name: 'settings', key: 'nav.settings', icon: 'settings', admin: true, perm: 'settings.manage', group: 'settings' }
]

// Bo'limlar: to'liq nom (panel sarlavhasi), qisqa nom (chap ustun) va ikonka
const navGroups = [
  { id: 'sales', label: 'nav.groupSales', short: 'nav.railSales', icon: 'cart' },
  { id: 'catalog', label: 'nav.groupCatalog', short: 'nav.railCatalog', icon: 'package' },
  { id: 'marketing', label: 'nav.groupMarketing', short: 'nav.railMarketing', icon: 'percent' },
  { id: 'mobile', label: 'nav.groupMobile', short: 'nav.railMobile', icon: 'phone' },
  { id: 'finance', label: 'nav.groupFinance', short: 'nav.railFinance', icon: 'wallet' },
  { id: 'reports', label: 'nav.groupReports', short: 'nav.railReports', icon: 'trendUp' },
  { id: 'settings', label: 'nav.groupSettings', short: 'nav.railSettings', icon: 'settings' }
]

// SuperAdmin (platforma egasi) faqat tashkilotlarni boshqaradi.
const superNavItems = [
  { name: 'organizations', key: 'nav.organizations', icon: 'building' },
  { name: 'admin-roles', key: 'nav.roles', icon: 'key' },
  { name: 'audit', key: 'nav.audit', icon: 'shield' },
  { name: 'fiscal-audit', key: 'nav.fiscalAudit', icon: 'fileText' },
  { name: 'admin-online-stores', key: 'nav.onlineStores', icon: 'phone' },
  { name: 'admin-errors', key: 'nav.systemErrors', icon: 'bug', badge: 'errors' }
]

// Bosqich 10.4: SuperAdmin menyusida ochiq xatoliklar soni (60 soniyada yangilanadi).
const openErrorCount = ref(0)
let errorTimer = null
async function loadErrorSummary() {
  if (!auth.isAuthenticated || !auth.isSuperAdmin) { openErrorCount.value = 0; return }
  try {
    const s = await adminErrorsApi.summary()
    openErrorCount.value = Number(s?.openCount) || 0
  } catch {
    // jim — belgi ishlamasa ham menyu ishlaydi
  }
}
function badgeCount(item) {
  return item.badge === 'errors' ? openErrorCount.value : 0
}
watch(
  () => auth.isAuthenticated && auth.isSuperAdmin,
  (on) => {
    clearInterval(errorTimer)
    errorTimer = null
    openErrorCount.value = 0
    if (on) {
      loadErrorSummary()
      errorTimer = setInterval(loadErrorSummary, 60000)
    }
  },
  { immediate: true }
)
// Xatoliklar sahifasida hal qilinganda belgini darhol yangilash uchun
window.addEventListener('admin-errors-changed', loadErrorSummary)
onBeforeUnmount(() => {
  clearInterval(errorTimer)
  window.removeEventListener('admin-errors-changed', loadErrorSummary)
})

function canSee(i) {
  if (i.admin && auth.role !== 'Admin') return false
  if (i.cashierOnly && auth.role === 'Admin') return false
  if (i.perm && !auth.hasPermission(i.perm)) return false
  return true
}

// Bo'limlar (SuperAdmin uchun bitta "Platforma" bo'limi), ruxsatga qarab filtrlangan.
const visibleGroups = computed(() => {
  if (auth.isSuperAdmin) {
    return [{ id: 'platform', label: 'nav.groupPlatform', short: 'nav.railPlatform', icon: 'shield', items: superNavItems }]
  }
  return navGroups
    .map((g) => ({ ...g, items: navItems.filter((i) => i.group === g.id && canSee(i)) }))
    .filter((g) => g.items.length > 0)
})
const allVisibleItems = computed(() => visibleGroups.value.flatMap((g) => g.items))

// Sevimli sahifalar (yulduzcha) — localStorage'da saqlanadi
const FAV_KEY = 'pos_nav_favorites'
function loadFavs() {
  try { return JSON.parse(localStorage.getItem(FAV_KEY)) || [] } catch { return [] }
}
const favorites = ref(loadFavs())
function isFav(name) { return favorites.value.includes(name) }
function toggleFav(name) {
  favorites.value = isFav(name) ? favorites.value.filter((n) => n !== name) : [...favorites.value, name]
  try { localStorage.setItem(FAV_KEY, JSON.stringify(favorites.value)) } catch { /* jim */ }
}
const favoriteItems = computed(() =>
  favorites.value.map((n) => allVisibleItems.value.find((i) => i.name === n)).filter(Boolean)
)

// Tanlangan bo'lim: joriy sahifa qaysi bo'limda bo'lsa, o'sha avtomatik ochiladi
const activeGroupId = ref('')
watch(
  [() => route.name, visibleGroups],
  () => {
    const g = visibleGroups.value.find((x) => x.items.some((i) => i.name === route.name))
    if (g) activeGroupId.value = g.id
    else if (activeGroupId.value !== 'fav' && !visibleGroups.value.some((x) => x.id === activeGroupId.value))
      activeGroupId.value = visibleGroups.value[0]?.id || ''
  },
  { immediate: true }
)

// Ikkinchi panelni yig'ish (faqat ikonka ustuni qoladi)
const PANEL_KEY = 'pos_nav_panel_hidden'
const panelHidden = ref((() => { try { return localStorage.getItem(PANEL_KEY) === '1' } catch { return false } })())
function setPanelHidden(v) {
  panelHidden.value = v
  try { localStorage.setItem(PANEL_KEY, v ? '1' : '0') } catch { /* jim */ }
}

// Qidiruv: yozilsa barcha bo'limlar bo'yicha qidiradi
const navSearch = ref('')
function selectGroup(id) {
  if (activeGroupId.value === id && !panelHidden.value) { setPanelHidden(true); return }
  activeGroupId.value = id
  navSearch.value = ''
  setPanelHidden(false)
}
const activeGroup = computed(() => {
  if (activeGroupId.value === 'fav') return { id: 'fav', label: 'nav.favorites', items: favoriteItems.value }
  return visibleGroups.value.find((g) => g.id === activeGroupId.value) || visibleGroups.value[0] || { items: [] }
})
const panelItems = computed(() => {
  const q = navSearch.value.trim().toLowerCase()
  if (!q) return activeGroup.value.items
  return allVisibleItems.value.filter((i) => t(i.key).toLowerCase().includes(q))
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
      <button class="hamb" aria-label="Menyu" @click="mobileNavOpen = true"><AppIcon name="menu" :size="20" /></button>
      <div class="mt-brand">
        <img src="/logo.png" alt="" />
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosCloud' }}</span>
      </div>
    </header>

    <!-- Drawer ochiq bo'lganda orqa fon -->
    <div v-if="showNav && mobileNavOpen" class="nav-backdrop" @click="mobileNavOpen = false"></div>

    <aside v-if="showNav" class="sidebar" :class="{ 'panel-hidden': panelHidden }">
      <!-- 1-ustun: bo'limlar -->
      <div class="rail">
        <div class="rail-logo"><img src="/logo.png" alt="Logo" /></div>
        <div class="rail-groups">
          <button
            v-if="favoriteItems.length"
            type="button"
            class="rail-btn"
            :class="{ active: activeGroupId === 'fav' && !panelHidden }"
            :title="t('nav.favorites')"
            @click="selectGroup('fav')"
          >
            <span class="rail-ico"><AppIcon name="star" :size="20" /></span>
            <span class="rail-text">{{ t('nav.railFavorites') }}</span>
          </button>
          <button
            v-for="g in visibleGroups"
            :key="g.id"
            type="button"
            class="rail-btn"
            :class="{ active: activeGroupId === g.id && !panelHidden, current: g.items.some((i) => i.name === route.name) }"
            :title="t(g.label)"
            @click="selectGroup(g.id)"
          >
            <span class="rail-ico"><AppIcon :name="g.icon" :size="20" /></span>
            <span class="rail-text">{{ t(g.short) }}</span>
          </button>
        </div>
        <div class="rail-foot">
          <button type="button" class="rail-lang" @click="setLang(locale === 'uz' ? 'ru' : 'uz')" :title="locale === 'uz' ? 'Русский' : 'O‘zbekcha'">
            {{ locale === 'uz' ? 'UZ' : 'RU' }}
          </button>
          <div class="rail-avatar" :title="`${auth.username} · ${auth.role}`">{{ (auth.username || '?').charAt(0).toUpperCase() }}</div>
          <button type="button" class="rail-btn rail-logout" :title="t('common.logout')" @click="logout">
            <span class="rail-ico"><AppIcon name="logout" :size="17" /></span>
            <span class="rail-text">{{ t('nav.railLogout') }}</span>
          </button>
        </div>
      </div>

      <!-- 2-ustun: tanlangan bo'lim sahifalari -->
      <div v-show="!panelHidden" class="nav-panel">
        <div class="panel-head">
          <div class="panel-title-wrap">
            <div class="panel-title">{{ navSearch.trim() ? t('nav.searchResults') : t(activeGroup.label) }}</div>
            <div class="panel-sub">{{ auth.isSuperAdmin ? 'Platforma' : 'PosCloud' }} · {{ auth.username }}</div>
          </div>
          <button type="button" class="panel-collapse" :title="t('nav.hidePanel')" @click="setPanelHidden(true)">‹</button>
          <button class="sidebar-close" aria-label="Yopish" @click="mobileNavOpen = false">×</button>
        </div>

        <div class="panel-search">
          <span class="ps-ico"><AppIcon name="search" :size="15" /></span>
          <input v-model="navSearch" type="search" :placeholder="t('nav.searchPlaceholder')" />
        </div>

        <nav class="side-nav">
          <div v-for="item in panelItems" :key="item.name" class="side-row">
            <RouterLink :to="{ name: item.name }" class="side-link">
              <span class="side-ico"><AppIcon :name="item.icon" :size="17" /></span>
              <span class="side-label">{{ t(item.key) }}</span>
              <span v-if="badgeCount(item)" class="side-badge">{{ badgeCount(item) > 99 ? '99+' : badgeCount(item) }}</span>
            </RouterLink>
            <button
              type="button"
              class="fav-btn"
              :class="{ on: isFav(item.name) }"
              :title="isFav(item.name) ? t('nav.unfavorite') : t('nav.favorite')"
              @click="toggleFav(item.name)"
            >★</button>
          </div>
          <div v-if="!panelItems.length" class="panel-empty">{{ t('nav.nothingFound') }}</div>
        </nav>

        <!-- Ko'p filiali bo'lgan Admin uchun global do'kon filtri -->
        <div v-if="!auth.isSuperAdmin && storeFilter.multiStore" class="sidebar-store-filter">
          <div class="side-filter-label">{{ t('nav.storeFilter') }}</div>
          <select
            :value="storeFilter.selectedBranchId"
            class="side-store-select"
            @change="storeFilter.select($event.target.value)"
          >
            <option value="">{{ t('nav.allStores') }}</option>
            <option v-for="s in storeFilter.stores" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
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

.side-badge { margin-left:auto; min-width:20px; padding:1px 6px; border-radius:999px; background:var(--danger, #dc2626); color:#fff; font-size:11px; font-weight:700; text-align:center; line-height:16px; }
.side-lang { display:flex; gap:6px; margin-bottom:8px; }
.lang-btn { flex:1; padding:4px; border:1px solid var(--border); border-radius:4px; background:transparent; cursor:pointer; font-size:11px; color:var(--text-muted); }
.lang-btn.active { background:var(--primary); color:#fff; border-color:var(--primary); }
</style>
