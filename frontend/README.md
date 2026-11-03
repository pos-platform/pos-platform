# PosCloud — Frontend (Vue 3)

POS + CRM tizimining web interfeysi. Vue 3 + Vite, sodda custom CSS (UI kutubxonasiz),
Pinia (holat) va vue-router. Backend REST API bilan ishlaydi.

## Sahifalar
- **Kirish** (`/login`) — JWT bilan autentifikatsiya
- **Savdo** (`/pos`) — kassir ekrani: mahsulot tanlash, savatcha, to'lov, qaytim
- **Mahsulotlar** (`/products`) — ro'yxat + yangi mahsulot/kategoriya qo'shish
- **Savdolar** (`/sales`) — savdolar tarixi va fiskal holat

## Sozlash
`.env.development` faylida backend manzili:

```
VITE_API_BASE=http://localhost:5000
```

(Backend CORS `http://localhost:5173` ga ruxsat bergan — Vite shu portda ishlaydi.)

## Ishga tushirish

```bash
cd frontend
npm install
npm run dev
```

So'ng <http://localhost:5173> ga kiring. Boshlang'ich admin (backend seed qiladi):
**admin / Admin12345!** — birinchi kirishdan keyin parolni almashtiring.

## Production build

```bash
npm run build      # dist/ papkasiga statik fayllar
npm run preview    # build'ni lokal sinab ko'rish
```

`dist/` ni istalgan statik hostda (nginx, IIS, ...) joylashtiring; backend manzilini
`.env.production` dagi `VITE_API_BASE` orqali belgilang.
