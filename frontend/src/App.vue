<script setup>
import { computed } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const showNav = computed(() => auth.isAuthenticated && route.name !== 'login')

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="app">
    <header v-if="showNav" class="topbar">
      <div class="brand">PosPlatform</div>
      <nav class="nav">
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'dashboard' }" class="nav-link">Главный</RouterLink>
        <RouterLink :to="{ name: 'pos' }" class="nav-link">Savdo</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'stores' }" class="nav-link">Do'konlar</RouterLink>
        <RouterLink :to="{ name: 'products' }" class="nav-link">Mahsulotlar</RouterLink>
        <RouterLink :to="{ name: 'sales' }" class="nav-link">Savdolar</RouterLink>
        <RouterLink :to="{ name: 'shifts' }" class="nav-link">Smena</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'suppliers' }" class="nav-link">Yetkazib beruvchilar</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'stock-receipts' }" class="nav-link">Накладная</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'debtors' }" class="nav-link">Qarzdorlar</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'expenses' }" class="nav-link">Xarajatlar</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'payments' }" class="nav-link">Платежи</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'write-offs' }" class="nav-link">Списание</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'promotions' }" class="nav-link">Aksiyalar</RouterLink>
        <RouterLink v-if="auth.role === 'Admin'" :to="{ name: 'users' }" class="nav-link">Foydalanuvchilar</RouterLink>
      </nav>
      <div class="user">
        <span class="user-name">{{ auth.username }}</span>
        <span class="badge">{{ auth.role }}</span>
        <button class="btn btn-ghost" @click="logout">Chiqish</button>
      </div>
    </header>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>
