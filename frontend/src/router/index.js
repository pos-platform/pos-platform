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
  {
    path: '/admin/fiscal-audit',
    name: 'fiscal-audit',
    component: () => import('../views/FiscalAuditView.vue'),
    meta: { superAdmin: true }
  },
  {
    path: '/admin/online-stores',
    name: 'admin-online-stores',
    component: () => import('../views/AdminOnlineStoresView.vue'),
    meta: { superAdmin: true }
  },
  {
    path: '/admin/errors',
    name: 'admin-errors',
    component: () => import('../views/AdminErrorsView.vue'),
    meta: { superAdmin: true }
  },
  { path: '/', redirect: { name: 'dashboard' } },
  { path: '/dashboard', name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { adminOnly: true } },
  { path: '/pos', name: 'pos', component: () => import('../views/PosView.vue') },
  { path: '/products', name: 'products', component: () => import('../views/ProductsView.vue'), meta: { perm: 'products.view' } },
  {
    path: '/categories',
    name: 'categories',
    component: () => import('../views/CategoriesView.vue'),
    meta: { adminOnly: true, perm: 'categories.view' }
  },
  { path: '/sales', name: 'sales', component: () => import('../views/SalesView.vue'), meta: { perm: 'sales.view' } },
  {
    path: '/online-orders',
    name: 'online-orders',
    component: () => import('../views/OnlineOrdersView.vue'),
    meta: { adminOnly: true, perm: 'sales.view' }
  },
  { path: '/shifts', name: 'shifts', component: () => import('../views/ShiftsView.vue') },
  {
    path: '/users',
    name: 'users',
    component: () => import('../views/UsersView.vue'),
    meta: { adminOnly: true, perm: 'users.view' }
  },
  {
    path: '/promotions',
    name: 'promotions',
    component: () => import('../views/PromotionsView.vue'),
    meta: { adminOnly: true }
  },
  {
    path: '/promo-codes',
    name: 'promo-codes',
    component: () => import('../views/PromoCodesView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/loyalty',
    name: 'loyalty',
    component: () => import('../views/LoyaltyView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/loyalty/settings',
    name: 'loyalty-settings',
    component: () => import('../views/loyalty/LoyaltySettingsEditView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/stores',
    name: 'stores',
    component: () => import('../views/StoresView.vue'),
    meta: { adminOnly: true, perm: 'branches.view' }
  },
  {
    path: '/suppliers',
    name: 'suppliers',
    component: () => import('../views/SuppliersView.vue'),
    meta: { adminOnly: true, perm: 'suppliers.view' }
  },
  {
    path: '/stock-receipts',
    name: 'stock-receipts',
    component: () => import('../views/StockReceiptsView.vue'),
    meta: { adminOnly: true, perm: 'inventory.view' }
  },
  {
    path: '/debtors',
    name: 'debtors',
    component: () => import('../views/DebtorsView.vue'),
    meta: { adminOnly: true, perm: 'debtors.view' }
  },
  {
    path: '/expenses',
    name: 'expenses',
    component: () => import('../views/ExpensesView.vue'),
    meta: { adminOnly: true, perm: 'expenses.view' }
  },
  {
    path: '/write-offs',
    name: 'write-offs',
    component: () => import('../views/WriteOffsView.vue'),
    meta: { adminOnly: true, perm: 'inventory.view' }
  },
  {
    path: '/inventories',
    name: 'inventories',
    component: () => import('../views/InventoriesView.vue'),
    meta: { adminOnly: true, perm: 'inventory.view' }
  },
  {
    path: '/stock-turnover',
    name: 'stock-turnover',
    component: () => import('../views/StockTurnoverView.vue'),
    meta: { adminOnly: true, perm: 'inventory.view' }
  },
  {
    path: '/payments',
    name: 'payments',
    component: () => import('../views/PaymentsView.vue'),
    meta: { adminOnly: true, perm: 'cashbox.view' }
  },
  {
    path: '/analytics',
    name: 'analytics',
    component: () => import('../views/AnalyticsView.vue'),
    meta: { adminOnly: true, perm: 'reports.view' }
  },
  {
    path: '/reports',
    name: 'reports',
    component: () => import('../views/ReportsView.vue'),
    meta: { adminOnly: true, perm: 'reports.view' }
  },
  {
    path: '/sold-items',
    name: 'sold-items',
    component: () => import('../views/SoldItemsView.vue'),
    meta: { adminOnly: true, perm: 'reports.view' }
  },
  {
    path: '/stock-transfers',
    name: 'stock-transfers',
    component: () => import('../views/StockTransfersView.vue'),
    meta: { adminOnly: true, perm: 'inventory.view' }
  },
  {
    path: '/cash-movements',
    name: 'cash-movements',
    component: () => import('../views/CashMovementsView.vue'),
    meta: { adminOnly: true, perm: 'cashbox.view' }
  },
  {
    path: '/customers',
    name: 'customers',
    component: () => import('../views/CustomersView.vue'),
    meta: { adminOnly: true, perm: 'customers.view' }
  },
  {
    path: '/cashbox/:id',
    name: 'cashbox',
    component: () => import('../views/CashboxView.vue'),
    meta: { adminOnly: true, perm: 'cashbox.view' }
  },
  {
    path: '/online-store',
    name: 'online-store',
    component: () => import('../views/OnlineStoreView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/online-store/edit',
    name: 'online-store-edit',
    component: () => import('../views/online/OnlineStoreEditView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/online-marketing',
    name: 'online-marketing',
    component: () => import('../views/OnlineMarketingView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
  },
  {
    path: '/settings',
    name: 'settings',
    component: () => import('../views/SettingsView.vue'),
    meta: { adminOnly: true, perm: 'settings.manage' }
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

  // Granular ruxsat talab qiladigan sahifalar — ruxsat bo'lmasa kirish yo'q.
  if (to.meta.perm && !auth.hasPermission(to.meta.perm)) {
    return auth.role === 'Admin' ? { name: 'dashboard' } : { name: 'pos' }
  }

  return true
})

export default router
