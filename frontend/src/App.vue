<script setup>
import { computed, watch } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from './stores/auth'
import { useStoreFilterStore } from './stores/storeFilter'

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

// Foydalanuvchi tizimga kirganida do'konlar ro'yxatini yuklaydi
watch(
  () => auth.isAuthenticated,
  (v) => { if (v && !auth.isSuperAdmin) storeFilter.ensureLoaded() },
  { immediate: true }
)

// Sidebar menyu. admin: true bo'lsa faqat Admin ko'radi.
const navItems = [
  { name: 'dashboard', key: 'nav.dashboard', icon: '📊', admin: true },
  { name: 'pos', key: 'nav.pos', icon: '🛒', cashierOnly: true },
  { name: 'stores', key: 'nav.stores', icon: '🏪', admin: true },
  { name: 'products', key: 'nav.products', icon: '📦' },
  { name: 'categories', key: 'nav.categories', icon: '🏷️', admin: true },
  { name: 'sales', key: 'nav.sales', icon: '🧾' },
  { name: 'shifts', key: 'nav.shifts', icon: '⏱️' },
  { name: 'suppliers', key: 'nav.suppliers', icon: '🚚', admin: true },
  { name: 'stock-receipts', key: 'nav.stockReceipts', icon: '📥', admin: true },
  { name: 'debtors', key: 'nav.debtors', icon: '📕', admin: true },
  { name: 'expenses', key: 'nav.expenses', icon: '💸', admin: true },
  { name: 'payments', key: 'nav.payments', icon: '💳', admin: true },
  { name: 'write-offs', key: 'nav.writeOffs', icon: '🗑️', admin: true },
  { name: 'inventories', key: 'nav.inventories', icon: '🔢', admin: true },
  { name: 'analytics', key: 'nav.analytics', icon: '📈', admin: true },
  { name: 'reports', key: 'nav.reports', icon: '📊', admin: true },
  { name: 'promotions', key: 'nav.promotions', icon: '🏷️', admin: true },
  { name: 'stock-transfers', key: 'nav.stockTransfers', icon: '🔄', admin: true },
  { name: 'cash-movements', key: 'nav.cashMovements', icon: '💵', admin: true },
  { name: 'customers', key: 'nav.customers', icon: '🙋', admin: true },
  { name: 'users', key: 'nav.users', icon: '👥', admin: true },
  { name: 'roles', key: 'nav.roles', icon: '🔑', admin: true }
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
  <div class="app-shell" :class="{ 'no-nav': !showNav }">
    <aside v-if="showNav" class="sidebar">
      <div class="sidebar-brand">
        <img src="/logo.png" alt="Logo" class="brand-img" />
        <span>{{ auth.isSuperAdmin ? 'Platforma' : 'PosPlatform' }}</span>
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
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.side-lang { display:flex; gap:6px; margin-bottom:8px; }
.lang-btn { flex:1; padding:4px; border:1px solid var(--border); border-radius:4px; background:transparent; cursor:pointer; font-size:11px; color:var(--text-muted); }
.lang-btn.active { background:var(--primary); color:#fff; border-color:var(--primary); }
</style>
