import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import router from './router'
import App from './App.vue'
import './assets/styles.css'
import './assets/fancy-inputs.css'
import { installFancyInputs } from './utils/fancyInputs'
import uz from './locales/uz.json'
import ru from './locales/ru.json'

const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('lang') || 'uz',
  fallbackLocale: 'uz',
  messages: { uz, ru }
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(i18n)
app.mount('#app')
installFancyInputs()
