import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Vue dev server 5173-portda (backend CORS shu portga ruxsat bergan).
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173
  }
})
