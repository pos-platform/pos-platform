import http from './http'

export const productsApi = {
  list: (params) => http.get('/api/products', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/products/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/products', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/products/${id}`, payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/products/${id}`).then((r) => r.data),
  uploadImage: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return http.post('/api/products/image', fd).then((r) => r.data) // { url }
  },
  generateBarcode: () => http.get('/api/products/generate-barcode').then((r) => r.data.barcode),
  nextPlu: () => http.get('/api/products/next-plu').then((r) => r.data.plu),
  importExcel: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return http.post('/api/products/import', fd).then((r) => r.data) // { created, failed, errors }
  },
  // MXIK takliflarini haqiqiy kodga o'tkazish (bo'sh ro'yxat = hammasi) → { updated }
  applyMxikSuggestions: (productIds = []) =>
    http.post('/api/products/mxik-suggestions/apply', { productIds }).then((r) => r.data),
  // Ommaviy "ilovada ko'rsatish/yashirish" → { updated }
  setShowOnline: (productIds, showOnline) =>
    http.put('/api/products/show-online', { productIds, showOnline }).then((r) => r.data),
  exportExcel: () => http.get('/api/products/export', { responseType: 'blob' }).then((r) => r.data),
  importTemplate: () => http.get('/api/products/import-template', { responseType: 'blob' }).then((r) => r.data),
  importTemplateTxt: () =>
    http.get('/api/products/import-template-txt', { responseType: 'blob' }).then((r) => r.data)
}

// Blob'ni fayl sifatida yuklab olish.
export function downloadBlob(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

export const categoriesApi = {
  list: () => http.get('/api/categories').then((r) => r.data),
  create: (payload) => http.post('/api/categories', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/categories/${id}`, payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/categories/${id}`).then((r) => r.data)
}

export const organizationsApi = {
  list: (page = 1, pageSize = 10) =>
    http.get('/api/organizations', { params: { page, pageSize } }).then((r) => r.data),
  get: (id) => http.get(`/api/organizations/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/organizations', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/organizations/${id}`, payload).then((r) => r.data),
  activate: (id) => http.post(`/api/organizations/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/organizations/${id}/deactivate`).then((r) => r.data),
  createUser: (id, payload) =>
    http.post(`/api/organizations/${id}/users`, payload).then((r) => r.data),
  updateUser: (userId, payload) =>
    http.put(`/api/organizations/users/${userId}`, payload).then((r) => r.data),
  extendLicense: (userId, months) =>
    http.post(`/api/organizations/users/${userId}/extend-license`, { months }).then((r) => r.data),
  revokeLicense: (userId) =>
    http.post(`/api/organizations/users/${userId}/revoke-license`).then((r) => r.data),
  resetDevice: (userId) =>
    http.post(`/api/organizations/users/${userId}/reset-device`).then((r) => r.data),
  activateUser: (userId) =>
    http.post(`/api/organizations/users/${userId}/activate`).then((r) => r.data),
  deactivateUser: (userId) =>
    http.post(`/api/organizations/users/${userId}/deactivate`).then((r) => r.data),
  getRoles: (id) => http.get(`/api/organizations/${id}/roles`).then((r) => r.data),
  getStores: (id) => http.get(`/api/organizations/${id}/stores`).then((r) => r.data),
  assignAppRole: (userId, roleId) =>
    http.put(`/api/organizations/users/${userId}/app-role`, { roleId: roleId ?? null }).then((r) => r.data),
  getAlerts: () => http.get('/api/organizations/alerts').then((r) => r.data),
  setLifetime: (userId) =>
    http.post(`/api/organizations/users/${userId}/set-lifetime`).then((r) => r.data),
  unsetLifetime: (userId) =>
    http.post(`/api/organizations/users/${userId}/unset-lifetime`).then((r) => r.data)
}

export const roleTemplatesApi = {
  list: () => http.get('/api/role-templates').then((r) => r.data),
  update: (id, permissions) => http.put(`/api/role-templates/${id}`, { permissions }).then((r) => r.data),
  create: (name, permissions) => http.post('/api/role-templates', { name, permissions }).then((r) => r.data),
  remove: (id) => http.delete(`/api/role-templates/${id}`).then((r) => r.data)
}

export const auditApi = {
  // params: { take, from, to, action, success, username, ip, search, organizationId }
  recent: (params = {}) =>
    http.get('/api/audit', { params: { take: 300, ...params } }).then((r) => r.data),
  // params: { take, from, to, status, organizationId, terminalId, search, minAmount, maxAmount }
  fiscalFailures: (params = {}) =>
    http.get('/api/audit/fiscal-failures', { params: { take: 200, ...params } }).then((r) => r.data),
  fiscalFilterOptions: () =>
    http.get('/api/audit/fiscal-failures/filters').then((r) => r.data)
}

export const fiscalModulesApi = {
  list: () => http.get('/api/fiscal-modules').then((r) => r.data),
  // ModuleId bo'yicha upsert: { moduleId, ownerName, inn, registrationNumber, terminalId, note }
  save: (payload) => http.post('/api/fiscal-modules', payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/fiscal-modules/${id}`).then((r) => r.data)
}

export const usersApi = {
  list: () => http.get('/api/users').then((r) => r.data),
  create: (payload) => http.post('/api/users', payload).then((r) => r.data),
  resetPassword: (id, newPassword) =>
    http.post(`/api/users/${id}/reset-password`, { newPassword }).then((r) => r.data),
  activate: (id) => http.post(`/api/users/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/users/${id}/deactivate`).then((r) => r.data),
  assignRole: (id, roleId) => http.put(`/api/users/${id}/role`, { roleId }).then((r) => r.data),
  licenseSummary: () => http.get('/api/users/license-summary').then((r) => r.data)
}

export const rolesApi = {
  list: () => http.get('/api/roles').then((r) => r.data),
  get: (id) => http.get(`/api/roles/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/roles', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/roles/${id}`, payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/roles/${id}`).then((r) => r.data)
}

export const salesApi = {
  list: (params) => http.get('/api/sales', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/sales/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/sales', payload).then((r) => r.data),
  // Bosqich 10.5: body ixtiyoriy — { items?: [{ saleItemId, quantity }], reason? }; body yo'q — to'liq qaytarish.
  refund: (id, payload) =>
    (payload ? http.post(`/api/sales/${id}/refund`, payload) : http.post(`/api/sales/${id}/refund`)).then((r) => r.data),
  // → { items, refundTotal, pointsToReverse, pointsAvailable, withheldAmount, cashToReturn, redeemedPointsReturned }
  refundPreview: (id, payload) => http.post(`/api/sales/${id}/refund-preview`, payload || {}).then((r) => r.data)
}

export const shiftsApi = {
  // 204 (ochiq smena yo'q) -> null.
  current: () => http.get('/api/shifts/current').then((r) => (r.status === 204 ? null : r.data)),
  open: (payload) => http.post('/api/shifts/open', payload).then((r) => r.data),
  close: (payload) => http.post('/api/shifts/close', payload).then((r) => r.data),
  list: () => http.get('/api/shifts').then((r) => r.data),
  get: (id) => http.get(`/api/shifts/${id}`).then((r) => r.data),
  requestXReport: () => http.post('/api/shifts/current/request-x-report').then((r) => r.data)
}

export const dashboardApi = {
  summary: (params) => http.get('/api/dashboard/summary', { params }).then((r) => r.data)
}

export const analyticsApi = {
  get: (params) => http.get('/api/analytics', { params }).then((r) => r.data)
}

export const storesApi = {
  list: () => http.get('/api/stores').then((r) => r.data),
  createStore: (payload) => http.post('/api/stores', payload).then((r) => r.data),
  activateStore: (id) => http.post(`/api/stores/${id}/activate`).then((r) => r.data),
  deactivateStore: (id) => http.post(`/api/stores/${id}/deactivate`).then((r) => r.data),
  addCashbox: (payload) => http.post('/api/stores/cashboxes', payload).then((r) => r.data),
  activateCashbox: (id) => http.post(`/api/stores/cashboxes/${id}/activate`).then((r) => r.data),
  deactivateCashbox: (id) => http.post(`/api/stores/cashboxes/${id}/deactivate`).then((r) => r.data),
  assignUser: (storeId, userId) => http.put(`/api/stores/${storeId}/users/${userId}`).then((r) => r.data),
  removeUser: (storeId, userId) => http.delete(`/api/stores/${storeId}/users/${userId}`).then((r) => r.data)
}

export const suppliersApi = {
  list: () => http.get('/api/suppliers').then((r) => r.data),
  create: (payload) => http.post('/api/suppliers', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/suppliers/${id}`, payload).then((r) => r.data),
  activate: (id) => http.post(`/api/suppliers/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/suppliers/${id}/deactivate`).then((r) => r.data)
}

export const stockReceiptsApi = {
  list: () => http.get('/api/stock-receipts').then((r) => r.data),
  get: (id) => http.get(`/api/stock-receipts/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/stock-receipts', payload).then((r) => r.data),
  // Kirimni bekor qilish (storno): qoldiq, partiya va yetkazib beruvchi qarzi teskari o'zgaradi
  cancel: (id) => http.post(`/api/stock-receipts/${id}/cancel`).then((r) => r.data)
}

export const writeOffsApi = {
  list: () => http.get('/api/write-offs').then((r) => r.data),
  create: (payload) => http.post('/api/write-offs', payload).then((r) => r.data),
  cancel: (id) => http.post(`/api/write-offs/${id}/cancel`).then((r) => r.data)
}

// Ombor harakati jurnali: tovar aylanmasi va tovar qoldig'i tarixi
export const stockMovementsApi = {
  // params: { from, to, branchId, productId, search, includeIdle } → { from, to, branchId, rows }
  turnover: (params) => http.get('/api/reports/stock-turnover', { params }).then((r) => r.data),
  // params: { branchId, from, to } → [{ createdAt, type, quantity, balanceAfter, documentNumber, ... }]
  productHistory: (productId, params) =>
    http.get(`/api/products/${productId}/movements`, { params }).then((r) => r.data)
}

// Ikki marta saqlanishdan himoya: forma ochilganda bitta kalit, saqlangach yangisi
export function newRequestId() {
  return (globalThis.crypto && crypto.randomUUID)
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export const inventoriesApi = {
  list: () => http.get('/api/inventories').then((r) => r.data),
  get: (id) => http.get(`/api/inventories/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/inventories', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/inventories/${id}`, payload).then((r) => r.data),
  apply: (id) => http.post(`/api/inventories/${id}/apply`).then((r) => r.data)
}

export const paymentsApi = {
  list: () => http.get('/api/payments').then((r) => r.data),
  create: (payload) => http.post('/api/payments', payload).then((r) => r.data)
}

export const debtorsApi = {
  list: () => http.get('/api/debtors').then((r) => r.data),
  get: (id) => http.get(`/api/debtors/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/debtors', payload).then((r) => r.data),
  addTransaction: (id, payload) => http.post(`/api/debtors/${id}/transactions`, payload).then((r) => r.data)
}

export const reportsApi = {
  sales:       (params) => http.get('/api/reports/sales',        { params }).then((r) => r.data),
  exportSales: (params) => http.get('/api/reports/sales/export', { params, responseType: 'blob' }).then((r) => r.data),
  stock:       (params) => http.get('/api/reports/stock',        { params }).then((r) => r.data),
  exportStock: (params) => http.get('/api/reports/stock/export', { params, responseType: 'blob' }).then((r) => r.data),
  // Bosqich 10.3: sotilgan tovarlar logi → { items, total, totals: { quantity, revenue, profit, refunds } }
  soldItems: (params) => http.get('/api/reports/sold-items', { params }).then((r) => r.data),
  exportSoldItems: (params) => http.get('/api/reports/sold-items/export', { params, responseType: 'blob' }).then((r) => r.data)
}

export const expensesApi = {
  list: () => http.get('/api/expenses').then((r) => r.data),
  create: (payload) => http.post('/api/expenses', payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/expenses/${id}`).then((r) => r.data)
}

export const stockTransfersApi = {
  list: () => http.get('/api/stock-transfers').then((r) => r.data),
  get: (id) => http.get(`/api/stock-transfers/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/stock-transfers', payload).then((r) => r.data),
  receive: (id) => http.post(`/api/stock-transfers/${id}/receive`).then((r) => r.data),
  cancel: (id) => http.post(`/api/stock-transfers/${id}/cancel`).then((r) => r.data)
}

export const cashMovementsApi = {
  list: (params) => http.get('/api/cash-movements', { params }).then((r) => r.data),
  create: (payload) => http.post('/api/cash-movements', payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/cash-movements/${id}`).then((r) => r.data)
}

// Cashback va bonus (CONTRACT-7)
export const loyaltyApi = {
  getSettings: () => http.get('/api/loyalty/settings').then((r) => r.data),
  saveSettings: (payload) => http.put('/api/loyalty/settings', payload).then((r) => r.data),
  // { earned, redeemed, expired, activeCustomers, outstandingPoints }
  report: (params) => http.get('/api/loyalty/report', { params }).then((r) => r.data),
  // Mijoz bonus tarixi → [{ id, createdAt, type, source, points, amount, comment, receiptNumber }] yoki PagedResult
  customerHistory: (customerId, params) =>
    http.get(`/api/customers/${customerId}/loyalty-history`, { params }).then((r) => r.data),
  // Bosqich 10.2: sozlamalar o'zgarishlari tarixi → { items: [{ changedAt, userName, field, oldValue, newValue }], total }
  settingsHistory: (params) => http.get('/api/loyalty/settings/history', { params }).then((r) => r.data),
  // Bonus harakatlari jurnali → { items, total, totals: { earned, redeemed, refunded, expired, adjusted } }
  transactions: (params) => http.get('/api/loyalty/transactions', { params }).then((r) => r.data),
  // Qo'lda tuzatish: { points (±), reason } — sabab majburiy
  adjust: (customerId, payload) => http.post(`/api/loyalty/customers/${customerId}/adjust`, payload).then((r) => r.data)
}

export const customersApi = {
  list: () => http.get('/api/customers').then((r) => r.data),
  get: (id) => http.get(`/api/customers/${id}`).then((r) => r.data),
  byPhone: (phone) => http.get('/api/customers/by-phone', { params: { phone } }).then((r) => r.data),
  create: (payload) => http.post('/api/customers', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/customers/${id}`, payload).then((r) => r.data),
  adjustPoints: (id, payload) => http.post(`/api/customers/${id}/adjust-points`, payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/customers/${id}`).then((r) => r.data)
}

export const labelsApi = {
  print: (payload) => http.post('/api/labels/print', payload).then((r) => r.data)
}

export const myOrganizationApi = {
  get: () => http.get('/api/my-organization').then((r) => r.data)
}

export const onlinePaymentsApi = {
  create: (payload) => http.post('/api/online-payments', payload).then((r) => r.data),
  get: (id) => http.get(`/api/online-payments/${id}`).then((r) => r.data),
  bySale: (saleId) => http.get(`/api/online-payments/by-sale/${saleId}`).then((r) => r.data),
  cancel: (id) => http.post(`/api/online-payments/${id}/cancel`).then((r) => r.data)
}

export const promotionsApi = {
  active: () => http.get('/api/promotions/active').then((r) => r.data),
  list: () => http.get('/api/promotions').then((r) => r.data),
  create: (payload) => http.post('/api/promotions', payload).then((r) => r.data),
  activate: (id) => http.post(`/api/promotions/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/promotions/${id}/deactivate`).then((r) => r.data),
  remove: (id) => http.delete(`/api/promotions/${id}`).then((r) => r.data)
}

// Mahsulot uchun amaldagi aksiyalardan eng katta chegirma foizini topadi
// (backend mantiqining nusxasi — faqat ko'rsatish uchun; rasmiy hisob serverda).
export function discountPercentFor(product, activePromos) {
  let best = 0
  for (const p of activePromos || []) {
    const applies =
      p.productId === product.id ||
      p.categoryId === product.categoryId ||
      (!p.productId && !p.categoryId)
    if (applies && p.discountPercent > best) best = p.discountPercent
  }
  return best
}

// Online do'kon (mobil ilova). SuperAdmin: barcha tashkilotlar; do'kon admini: o'z sozlamalari.
export const onlineStoresAdminApi = {
  list: (params) => http.get('/api/admin/online-stores', { params }).then((r) => r.data),
  get: (orgId) => http.get(`/api/admin/online-stores/${orgId}`).then((r) => r.data),
  update: (orgId, payload) => http.put(`/api/admin/online-stores/${orgId}`, payload).then((r) => r.data),
  // Bosqich 8: do'konning alohida mobil ilovasi (white-label) sozlamalari
  updateApp: (orgId, payload) => http.put(`/api/admin/online-stores/${orgId}/app`, payload).then((r) => r.data)
}

export const onlineStoreApi = {
  get: () => http.get('/api/online-store/settings').then((r) => r.data),
  update: (payload) => http.put('/api/online-store/settings', payload).then((r) => r.data),
  requestListing: () => http.post('/api/online-store/request').then((r) => r.data),
  uploadLogo: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return http.post('/api/products/image', fd).then((r) => r.data) // { url }
  }
}

// Admin panel: online buyurtmalar (faqat ko'rish; amallar kassada)
export const onlineOrdersApi = {
  list: (params) => http.get('/api/online-orders', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/online-orders/${id}`).then((r) => r.data),
  // Bosqich 9.3: { average, count, distribution: {1..5}, tagCounts: {tag:int}, items: [...] }
  ratings: (params) => http.get('/api/online-orders/ratings', { params }).then((r) => r.data),
  // [{ id, name, phone, activeOrders }]
  couriers: (params) => http.get('/api/online-orders/couriers', { params }).then((r) => r.data),
  // Bosqich 10.6: filial kuryerlariga taklifni qayta yuborish
  offerCouriers: (id) => http.post(`/api/online-orders/${id}/offer-couriers`).then((r) => r.data)
}

// Bosqich 10.5: online buyurtmalarni qaytarish (xodim)
export const onlineReturnsApi = {
  list: (params) => http.get('/api/online-returns', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/online-returns/${id}`).then((r) => r.data),
  approve: (id, method) => http.post(`/api/online-returns/${id}/approve`, { method }).then((r) => r.data),
  reject: (id, reason) => http.post(`/api/online-returns/${id}/reject`, { reason }).then((r) => r.data),
  assignCourier: (id, courierId) => http.post(`/api/online-returns/${id}/assign-courier`, { courierId }).then((r) => r.data),
  received: (id) => http.post(`/api/online-returns/${id}/received`).then((r) => r.data),
  refunded: (id) => http.post(`/api/online-returns/${id}/refunded`).then((r) => r.data)
}

// Bosqich 10.4: tizim xatoliklari (SuperAdmin)
export const adminErrorsApi = {
  list: (params) => http.get('/api/admin/errors', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/admin/errors/${id}`).then((r) => r.data),
  resolve: (id) => http.post(`/api/admin/errors/${id}/resolve`).then((r) => r.data),
  resolveBulk: (ids) => http.post('/api/admin/errors/resolve-bulk', { ids }).then((r) => r.data),
  // { openCount, last24h, bySource: {..} }
  summary: () => http.get('/api/admin/errors/summary').then((r) => r.data)
}

// Bosqich 9 (CONTRACT-9): ilova reklamasi — bannerlar, kolleksiyalar, stories, push kampaniyalar.
function crud(base) {
  return {
    list: () => http.get(base).then((r) => r.data),
    get: (id) => http.get(`${base}/${id}`).then((r) => r.data),
    create: (payload) => http.post(base, payload).then((r) => r.data),
    update: (id, payload) => http.put(`${base}/${id}`, payload).then((r) => r.data),
    remove: (id) => http.delete(`${base}/${id}`).then((r) => r.data)
  }
}
export const onlineMarketingApi = {
  banners: { ...crud('/api/online-store/banners'), reorder: (ids) => http.put('/api/online-store/banners/reorder', { ids }).then((r) => r.data) },
  collections: crud('/api/online-store/collections'),
  recipes: crud('/api/online-store/recipes'),
  stories: { ...crud('/api/online-store/stories'), reorder: (ids) => http.put('/api/online-store/stories/reorder', { ids }).then((r) => r.data) },
  pushCampaigns: { ...crud('/api/online-store/push-campaigns'), send: (id) => http.post(`/api/online-store/push-campaigns/${id}/send`).then((r) => r.data) }
}

// Promokodlar (online do'kon, tashkilot bo'yicha). Huquq: settings.manage.
export const promoCodesApi = {
  list: () => http.get('/api/promo-codes').then((r) => r.data),
  create: (payload) => http.post('/api/promo-codes', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/promo-codes/${id}`, payload).then((r) => r.data),
  remove: (id) => http.delete(`/api/promo-codes/${id}`).then((r) => r.data)
}
