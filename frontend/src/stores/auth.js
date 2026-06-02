import { defineStore } from 'pinia'
import http from '../api/http'

const STORAGE_KEY = 'pos_auth'

function load() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY)) || {}
  } catch {
    return {}
  }
}

export const useAuthStore = defineStore('auth', {
  state: () => {
    const saved = load()
    return {
      token: saved.token || null,
      username: saved.username || null,
      role: saved.role || null,
      organizationId: saved.organizationId || null,
      organizationName: saved.organizationName || null,
      licensedUntil: saved.licensedUntil || null
    }
  },
  getters: {
    isAuthenticated: (state) => !!state.token,
    isSuperAdmin: (state) => state.role === 'SuperAdmin',
    isAdmin: (state) => state.role === 'Admin'
  },
  actions: {
    async login(username, password) {
      const { data } = await http.post('/api/auth/login', { username, password })
      this.token = data.token
      this.username = data.username
      this.role = data.role
      this.organizationId = data.organizationId || null
      this.organizationName = data.organizationName || null
      this.licensedUntil = data.licensedUntil || null
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          token: data.token,
          username: data.username,
          role: data.role,
          organizationId: this.organizationId,
          organizationName: this.organizationName,
          licensedUntil: this.licensedUntil
        })
      )
    },
    logout() {
      this.token = null
      this.username = null
      this.role = null
      this.organizationId = null
      this.organizationName = null
      this.licensedUntil = null
      localStorage.removeItem(STORAGE_KEY)
    }
  }
})
