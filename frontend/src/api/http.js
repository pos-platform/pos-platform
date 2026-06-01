import axios from 'axios'
import { useAuthStore } from '../stores/auth'
import router from '../router'

export const apiBase = (import.meta.env.VITE_API_BASE || 'http://localhost:5000').replace(/\/$/, '')

const http = axios.create({
  baseURL: apiBase
})

// Backenddan kelgan nisbiy rasm yo'lini to'liq URL'ga aylantiradi.
export function mediaUrl(path) {
  if (!path) return ''
  if (/^https?:\/\//.test(path)) return path
  return apiBase + path
}

// Har bir so'rovga JWT tokenni qo'shamiz.
http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

// 401 bo'lsa — sessiyani tozalab, login sahifasiga yuboramiz.
http.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const auth = useAuthStore()
      auth.logout()
      if (router.currentRoute.value.name !== 'login') {
        router.push({ name: 'login' })
      }
    }
    return Promise.reject(error)
  }
)

// Backend xatolik matnini qulay ko'rinishga keltiradi.
export function errorMessage(error, fallback = 'Xatolik yuz berdi') {
  return error.response?.data?.error || error.response?.data?.title || error.message || fallback
}

export default http
