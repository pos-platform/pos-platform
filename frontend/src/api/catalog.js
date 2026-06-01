import http from './http'

export const productsApi = {
  list: () => http.get('/api/products').then((r) => r.data),
  get: (id) => http.get(`/api/products/${id}`).then((r) => r.data),
  create: (payload) => http.post('/api/products', payload).then((r) => r.data)
}

export const categoriesApi = {
  list: () => http.get('/api/categories').then((r) => r.data),
  create: (payload) => http.post('/api/categories', payload).then((r) => r.data)
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
  list: () => http.get('/api/sales').then((r) => r.data),
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
  get: (id) => http.get(`/api/shifts/${id}`).then((r) => r.data)
}
