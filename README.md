# PosCloud

O'zbekiston uchun **POS + web CRM** tizimi. Kassa savdosi, mahsulot/kategoriya
boshqaruvi, soliq (MXIK / QQS) hisobi va kelajakda fiskal (FDS / Rahmat MK),
chek printer (ESC/POS) hamda tarozi (PLU) integratsiyalari.

## Tarkib

| Qism | Texnologiya | Papka | Tavsif |
|------|-------------|-------|--------|
| **Backend** | .NET 8 Web API, EF Core, PostgreSQL, JWT | [`backend/`](backend) | REST API, 4 qatlamli toza arxitektura |
| **Frontend** | Vue 3 + Vite | [`frontend/`](frontend) | Kassir/admin web interfeysi |
| **Agent** | .NET 8 worker | [`agent/`](agent) | Lokal fiskal/printer agenti (hozir simulyatsiya) |

## Tez ishga tushirish (lokal)

Talablar: **.NET 8 SDK**, **Node.js 18+**, **PostgreSQL 14+**.

### 1. Backend
```bash
cd backend
dotnet run --project src/PosCloud.Api
```
Ishga tushganda avtomatik: migratsiyalar qo'llanadi + boshlang'ich **admin** va
**"Umumiy"** kategoriyasi yaratiladi. Swagger: <http://localhost:5000/swagger>.

> Connection string va Jwt:Key — `backend/src/PosCloud.Api/appsettings.json`.

### 2. Frontend
```bash
cd frontend
npm install
npm run dev
```
<http://localhost:5173> → kirish: **admin / Admin12345!**

### 3. Agent (ixtiyoriy, fiskal simulyatsiya)
```bash
cd agent/PosCloud.Agent
dotnet run
```
Savdo yaratilganda agent uni "fiskalizatsiya" qiladi va chekni konsolga chop etadi.

## Oqim

```
Frontend (Vue) ──HTTP──> Backend (.NET API) ──> PostgreSQL
                              ▲
                              │ pull (X-Agent-Key)
                        Agent (lokal) ──> FDS / printer / tarozi (kelajakda)
```

## Holat

✅ Auth (JWT, BCrypt), mahsulot/kategoriya CRUD, savdo oqimi (QQS, chek raqami),
fiskal seam + lokal agent halqasi, Vue interfeys (login/savdo/mahsulot/savdolar).

🔜 Tarozi (PLU) integratsiyasi, smena (kassa sessiyasi) + refund, foydalanuvchi
boshqaruvi, haqiqiy FDS/ESC/POS implementatsiyalari, deploy (Docker).

Batafsil: [backend/README.md](backend/README.md) · [frontend/README.md](frontend/README.md) · [agent/README.md](agent/README.md)

## Litsenziya
Xususiy (proprietary) — barcha huquqlar himoyalangan.
