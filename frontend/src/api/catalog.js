import http from './http'

export const productsApi = {
  list: () => http.get('/api/products').then((r) => r.data),
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
  exportExcel: () => http.get('/api/products/export', { responseType: 'blob' }).then((r) => r.data),
  importTemplate: () => http.get('/api/products/import-template', { responseType: 'blob' }).then((r) => r.data)
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
  create: (payload) => http.post('/api/categories', payload).then((r) => r.data)
}

export const organizationsApi = {
  list: () => http.get('/api/organizations').then((r) => r.data),
  get: (id) => http.get(`/api/organizations/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/organizations', payload).then((r) => r.data),
  update: (id, payload) => http.put(`/api/organizations/${id}`, payload).then((r) => r.data),
  activate: (id) => http.post(`/api/organizations/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/organizations/${id}/deactivate`).then((r) => r.data),
  createUser: (id, payload) =>
    http.post(`/api/organizations/${id}/users`, payload).then((r) => r.data),
  extendLicense: (userId, months) =>
    http.post(`/api/organizations/users/${userId}/extend-license`, { months }).then((r) => r.data),
  revokeLicense: (userId) =>
    http.post(`/api/organizations/users/${userId}/revoke-license`).then((r) => r.data),
  activateUser: (userId) =>
    http.post(`/api/organizations/users/${userId}/activate`).then((r) => r.data),
  deactivateUser: (userId) =>
    http.post(`/api/organizations/users/${userId}/deactivate`).then((r) => r.data)
}

export const auditApi = {
  recent: (take = 200) => http.get('/api/audit', { params: { take } }).then((r) => r.data)
}

export const usersApi = {
  list: () => http.get('/api/users').then((r) => r.data),
  create: (payload) => http.post('/api/users', payload).then((r) => r.data),
  resetPassword: (id, newPassword) =>
    http.post(`/api/users/${id}/reset-password`, { newPassword }).then((r) => r.data),
  activate: (id) => http.post(`/api/users/${id}/activate`).then((r) => r.data),
  deactivate: (id) => http.post(`/api/users/${id}/deactivate`).then((r) => r.data)
}

export const salesApi = {
  list: (params) => http.get('/api/sales', { params }).then((r) => r.data),
  get: (id) => http.get(`/api/sales/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/sales', payload).then((r) => r.data),
  refund: (id) => http.post(`/api/sales/${id}/refund`).then((r) => r.data)
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
  deactivateCashbox: (id) => http.post(`/api/stores/cashboxes/${id}/deactivate`).then((r) => r.data)
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
  create: (payload) => http.post('/api/stock-receipts', payload).then((r) => r.data)
}

export const writeOffsApi = {
  list: () => http.get('/api/write-offs').then((r) => r.data),
  create: (payload) => http.post('/api/write-offs', payload).then((r) => r.data)
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
  stock:       ()       => http.get('/api/reports/stock').then((r) => r.data),
  exportStock: ()       => http.get('/api/reports/stock/export', { responseType: 'blob' }).then((r) => r.data)
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
