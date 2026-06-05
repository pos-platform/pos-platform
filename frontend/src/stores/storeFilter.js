import { defineStore } from 'pinia'
import { storesApi } from '../api/catalog'

const STORAGE_KEY = 'pos_store_filter'

function loadSaved() {
  try { return JSON.parse(localStorage.getItem(STORAGE_KEY)) || {} }
  catch { return {} }
}

export const useStoreFilterStore = defineStore('storeFilter', {
  state: () => {
    const saved = loadSaved()
    return {
      stores: [],
      selectedBranchId: saved.selectedBranchId || '',
      loaded: false
    }
  },
  getters: {
    multiStore: (state) => state.stores.length > 1,
    selectedStore: (state) => state.stores.find((s) => s.id === state.selectedBranchId) || null
  },
  actions: {
    async ensureLoaded() {
      if (this.loaded) return
      try {
        this.stores = await storesApi.list()
        // Saqlangan branchId endi mavjud bo'lmasa reset
        if (this.selectedBranchId && !this.stores.find((s) => s.id === this.selectedBranchId)) {
          this.selectedBranchId = ''
        }
        this.loaded = true
        this._persist()
      } catch { /* tarmoq xatosi — pastroqda qayta urinadi */ }
    },
    select(branchId) {
      this.selectedBranchId = branchId
      this._persist()
    },
    reset() {
      this.stores = []
      this.selectedBranchId = ''
      this.loaded = false
      localStorage.removeItem(STORAGE_KEY)
    },
    _persist() {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ selectedBranchId: this.selectedBranchId }))
    }
  }
})
