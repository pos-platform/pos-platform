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
      role: saved.role || null
    }
  },
  getters: {
    isAuthenticated: (state) => !!state.token
  },
  actions: {
    async login(username, password) {
      const { data } = await http.post('/api/auth/login', { username, password })
      this.token = data.token
      this.username = data.username
      this.role = data.role
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({ token: data.token, username: data.username, role: data.role })
      )
    },
    logout() {
      this.token = null
      this.username = null
      this.role = null
      localStorage.removeItem(STORAGE_KEY)
    }
  }
})
