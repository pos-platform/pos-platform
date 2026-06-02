<script setup>
import { computed } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const showNav = computed(() => auth.isAuthenticated && route.name !== 'login')

// Sidebar menyu. admin: true bo'lsa faqat Admin ko'radi.
const navItems = [
  { name: 'dashboard', label: 'Главный', icon: '📊', admin: true },
  { name: 'pos', label: 'Savdo', icon: '🛒' },
  { name: 'stores', label: "Do'konlar", icon: '🏪', admin: true },
  { name: 'products', label: 'Mahsulotlar', icon: '📦' },
  { name: 'sales', label: 'Savdolar', icon: '🧾' },
  { name: 'shifts', label: 'Smena', icon: '⏱️' },
  { name: 'suppliers', label: 'Yetkazib beruvchilar', icon: '🚚', admin: true },
  { name: 'stock-receipts', label: 'Накладная', icon: '📥', admin: true },
  { name: 'debtors', label: 'Qarzdorlar', icon: '📕', admin: true },
  { name: 'expenses', label: 'Xarajatlar', icon: '💸', admin: true },
  { name: 'payments', label: 'Платежи', icon: '💳', admin: true },
  { name: 'write-offs', label: 'Списание', icon: '🗑️', admin: true },
  { name: 'analytics', label: 'Аналитика', icon: '📈', admin: true },
  { name: 'promotions', label: 'Aksiyalar', icon: '🏷️', admin: true },
  { name: 'users', label: 'Foydalanuvchilar', icon: '👥', admin: true }
]

// SuperAdmin (platforma egasi) faqat tashkilotlarni boshqaradi.
const superNavItems = [
  { name: 'organizations', label: 'Tashkilotlar', icon: '🏢' }
]

const visibleNav = computed(() => {
  if (auth.isSuperAdmin) return superNavItems
  return navItems.filter((i) => !i.admin || auth.role === 'Admin')
})

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="app-shell" :class="{ 'no-nav': !showNav }">
    <aside v-if="showNav" class="sidebar">
      <div class="sidebar-brand">{{ auth.isSuperAdmin ? 'Platforma' : 'PosPlatform' }}</div>

      <nav class="side-nav">
        <RouterLink
          v-for="item in visibleNav"
          :key="item.name"
          :to="{ name: item.name }"
          class="side-link"
        >
          <span class="side-ico">{{ item.icon }}</span>
          <span class="side-label">{{ item.label }}</span>
        </RouterLink>
      </nav>

      <div class="sidebar-foot">
        <div class="side-user">
          <div class="side-username">{{ auth.username }}</div>
          <span class="badge">{{ auth.role }}</span>
        </div>
        <button class="btn btn-ghost btn-sm" style="width:100%; margin-top:8px" @click="logout">Chiqish</button>
      </div>
    </aside>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>
