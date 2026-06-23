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
      roleName: saved.roleName || null,
      appRoleId: saved.appRoleId || null,
      permissions: saved.permissions || [],
      organizationId: saved.organizationId || null,
      organizationName: saved.organizationName || null,
      businessType: saved.businessType || 'Store',
      licensedUntil: saved.licensedUntil || null
    }
  },
  getters: {
    isAuthenticated: (state) => !!state.token,
    isSuperAdmin: (state) => state.role === 'SuperAdmin',
    isAdmin: (state) => state.role === 'Admin',
    isRestaurant: (state) => state.businessType === 'Restaurant',
    hasPermission: (state) => (code) => {
      if (state.role === 'SuperAdmin') return true
      return state.permissions.includes(code)
    }
  },
  actions: {
    async login(username, password) {
      const { data } = await http.post('/api/auth/login', { username, password })
      this.token = data.token
      this.username = data.username
      this.role = data.role
      this.roleName = data.roleName || null
      this.appRoleId = data.appRoleId || null
      this.permissions = data.permissions || []
      this.organizationId = data.organizationId || null
      this.organizationName = data.organizationName || null
      this.businessType = data.businessType || 'Store'
      this.licensedUntil = data.licensedUntil || null
      this.persist()
    },
    setBusinessType(businessType) {
      this.businessType = businessType || 'Store'
      this.persist()
    },
    async changePassword(currentPassword, newPassword) {
      await http.post('/api/auth/change-password', { currentPassword, newPassword })
    },
    persist() {
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          token: this.token,
          username: this.username,
          role: this.role,
          roleName: this.roleName,
          appRoleId: this.appRoleId,
          permissions: this.permissions,
          organizationId: this.organizationId,
          organizationName: this.organizationName,
          businessType: this.businessType,
          licensedUntil: this.licensedUntil
        })
      )
    },
    logout() {
      this.token = null
      this.username = null
      this.role = null
      this.roleName = null
      this.appRoleId = null
      this.permissions = []
      this.organizationId = null
      this.organizationName = null
      this.businessType = 'Store'
      this.licensedUntil = null
      localStorage.removeItem(STORAGE_KEY)
    }
  }
})
