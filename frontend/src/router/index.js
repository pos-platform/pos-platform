import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  {
    path: '/admin/organizations',
    name: 'organizations',
    component: () => import('../views/OrganizationsView.vue'),
    meta: { superAdmin: true }
  },
  { path: '/', redirect: { name: 'dashboard' } },
  { path: '/dashboard', name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { adminOnly: true } },
  { path: '/pos', name: 'pos', component: () => import('../views/PosView.vue') },
  { path: '/products', name: 'products', component: () => import('../views/ProductsView.vue') },
  { path: '/sales', name: 'sales', component: () => import('../views/SalesView.vue') },
  { path: '/shifts', name: 'shifts', component: () => import('../views/ShiftsView.vue') },
  {
    path: '/users',
    name: 'users',
    component: () => import('../views/UsersView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/promotions',
    name: 'promotions',
    component: () => import('../views/PromotionsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/stores',
    name: 'stores',
    component: () => import('../views/StoresView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/suppliers',
    name: 'suppliers',
    component: () => import('../views/SuppliersView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/stock-receipts',
    name: 'stock-receipts',
    component: () => import('../views/StockReceiptsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/debtors',
    name: 'debtors',
    component: () => import('../views/DebtorsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/expenses',
    name: 'expenses',
    component: () => import('../views/ExpensesView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/write-offs',
    name: 'write-offs',
    component: () => import('../views/WriteOffsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/payments',
    name: 'payments',
    component: () => import('../views/PaymentsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/analytics',
    name: 'analytics',
    component: () => import('../views/AnalyticsView.vue'),
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

  // SuperAdmin faqat tashkilotlar sahifasida ishlaydi — boshqa hamma yo'l shu yerga yo'naltiriladi.
  if (auth.isSuperAdmin) {
    if (to.name === 'login' || !to.meta.superAdmin) {
      return to.name === 'organizations' ? true : { name: 'organizations' }
    }
    return true
  }

  // SuperAdmin bo'lmaganlar super-admin sahifasiga kira olmaydi.
  if (to.meta.superAdmin) {
    return { name: 'pos' }
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
