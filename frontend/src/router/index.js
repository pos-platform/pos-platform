import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  { path: '/', redirect: { name: 'pos' } },
  { path: '/pos', name: 'pos', component: () => import('../views/PosView.vue') },
  { path: '/products', name: 'products', component: () => import('../views/ProductsView.vue') },
  { path: '/sales', name: 'sales', component: () => import('../views/SalesView.vue') },
  { path: '/shifts', name: 'shifts', component: () => import('../views/ShiftsView.vue') },
  {
    path: '/users',
    name: 'users',
    component: () => import('../views/UsersView.vue'),
    meta: { adminOnly: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Auth guard: himoyalangan sahifalar uchun token shart.
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'pos' }
  }
  // Admin-only sahifalar.
  if (to.meta.adminOnly && auth.role !== 'Admin') {
    return { name: 'pos' }
  }
  return true
})

export default router
