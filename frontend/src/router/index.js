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
  {
    path: '/admin/roles',
    name: 'admin-roles',
    component: () => import('../views/SuperAdminRolesView.vue'),
    meta: { superAdmin: true }
  },
  {
    path: '/admin/audit',
    name: 'audit',
    component: () => import('../views/AuditView.vue'),
    meta: { superAdmin: true }
  },
  { path: '/', redirect: { name: 'dashboard' } },
  { path: '/dashboard', name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { adminOnly: true } },
  { path: '/pos', name: 'pos', component: () => import('../views/PosView.vue') },
  { path: '/products', name: 'products', component: () => import('../views/ProductsView.vue') },
  {
    path: '/categories',
    name: 'categories',
    component: () => import('../views/CategoriesView.vue'),
    meta: { adminOnly: true }
  },
  { path: '/sales', name: 'sales', component: () => import('../views/SalesView.vue') },
  { path: '/shifts', name: 'shifts', component: () => import('../views/ShiftsView.vue') },
  {
    path: '/users',
    name: 'users',
    component: () => import('../views/UsersView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/roles',
    name: 'roles',
    component: () => import('../views/RolesView.vue'),
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
    path: '/inventories',
    name: 'inventories',
    component: () => import('../views/InventoriesView.vue'),
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
  },
  {
    path: '/reports',
    name: 'reports',
    component: () => import('../views/ReportsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/stock-transfers',
    name: 'stock-transfers',
    component: () => import('../views/StockTransfersView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/cash-movements',
    name: 'cash-movements',
    component: () => import('../views/CashMovementsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/customers',
    name: 'customers',
    component: () => import('../views/CustomersView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/cashbox/:id',
    name: 'cashbox',
    component: () => import('../views/CashboxView.vue'),
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

  // SuperAdmin faqat tashkilotlar sahifasida ishlaydi.
  if (auth.isSuperAdmin) {
    if (to.name === 'login' || !to.meta.superAdmin) {
      return to.name === 'organizations' ? true : { name: 'organizations' }
    }
    return true
  }

  if (to.meta.superAdmin) {
    return { name: 'dashboard' }
  }

  if (to.name === 'login' && auth.isAuthenticated) {
    return auth.role === 'Admin' ? { name: 'dashboard' } : { name: 'pos' }
  }

  // Admin POS terminaliga kira olmaydi — faqat admin paneli.
  if (to.name === 'pos' && auth.role === 'Admin') {
    return { name: 'dashboard' }
  }

  // Admin-only sahifalar — faqat Admin roli kirishi mumkin.
  if (to.meta.adminOnly && auth.role !== 'Admin') {
    return { name: 'pos' }
  }

  return true
})

export default router
