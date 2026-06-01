# PosPlatform — Backend

O'zbekiston uchun POS + web CRM tizimining backend asosi.
.NET 8 (ASP.NET Core Web API), EF Core + PostgreSQL, JWT autentifikatsiya.

## Arxitektura (4 qat: toza arxitektura)

```
backend/
└── src/
    ├── PosPlatform.Domain          # Entitylar, enumlar (hech narsaga bog'liq emas)
    ├── PosPlatform.Application      # DTO, interfeyslar, biznes servislar (use-case)
    ├── PosPlatform.Infrastructure   # EF Core DbContext, PostgreSQL, JWT, BCrypt, DI
    └── PosPlatform.Api              # Controllerlar, JWT sozlamasi, Swagger, Program.cs
```

Bog'liqlik yo'nalishi: `Api → Infrastructure → Application → Domain`.

### Asosiy entitylar
- **Product** — `MxikCode` (soliq uchun majburiy MXIK), `Barcode`, `Price`,
  `Unit` (Piece/Kilogram), `IsWeighed`, `PluNumber` (tarozi), `Tare`, `CategoryId`
- **Category**, **Branch**, **User** (`Role`: Admin/Manager/Cashier)

## Talablar
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 14+ (ishlab turgan instans)

## 1. Sozlash

`src/PosPlatform.Api/appsettings.json` ichida:

- **ConnectionStrings:DefaultConnection** — o'z PostgreSQL ma'lumotlaringizga moslang.
- **Jwt:Key** — productionda **kamida 32 belgi**li kuchli maxfiy kalitga almashtiring
  (hozir placeholder turibdi, manba kodga maxfiy kalit commit qilinmasin).

## 2. EF Core migration

EF Core CLI vositasini (bir marta) o'rnating:

```bash
dotnet tool install --global dotnet-ef
```

Migration `Init` allaqachon yaratilgan. Bazaga qo'llash (DB yaratiladi):

```bash
cd backend
dotnet ef database update --project src/PosPlatform.Infrastructure --startup-project src/PosPlatform.Api
```

Yangi migration qo'shish (entity o'zgartirgandan keyin):

```bash
dotnet ef migrations add <Nomi> --project src/PosPlatform.Infrastructure --startup-project src/PosPlatform.Api
```

## 3. Ishga tushirish

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/PosPlatform.Api
```

Swagger: <http://localhost:5000/swagger>

## Endpointlar

| Metod | Yo'l                   | Himoya       | Tavsif                                |
|-------|------------------------|--------------|---------------------------------------|
| POST  | `/api/auth/register`   | ochiq        | Ro'yxatdan o'tish → JWT qaytaradi     |
| POST  | `/api/auth/login`      | ochiq        | Kirish → JWT qaytaradi                |
| GET   | `/api/products`        | `[Authorize]`| Barcha mahsulotlar                    |
| GET   | `/api/products/{id}`   | `[Authorize]`| Id bo'yicha mahsulot                  |
| POST  | `/api/products`        | `[Authorize]`| Yangi mahsulot                        |
| GET   | `/api/sales`           | `[Authorize]`| Barcha savdolar (yangisi birinchi)    |
| GET   | `/api/sales/{id}`      | `[Authorize]`| Savdo (qatorlar + chek bilan)         |
| POST  | `/api/sales`           | `[Authorize]`| Yangi savdo (ochiq smena talab qiladi) |
| POST  | `/api/sales/{id}/refund` | `[Authorize]`| Savdoni qaytarish (fiskal return cheki) |
| GET   | `/api/shifts/current`  | `[Authorize]`| Joriy kassirning ochiq smenasi        |
| POST  | `/api/shifts/open`     | `[Authorize]`| Smena ochish (boshlang'ich naqd)      |
| POST  | `/api/shifts/close`    | `[Authorize]`| Smena yopish (naqd sanab, hisobot)    |
| GET   | `/api/shifts`          | `[Authorize]`| Smenalar tarixi                       |
| GET   | `/api/shifts/{id}`     | `[Authorize]`| Smena (hisobot bilan)                 |

Himoyalangan endpointlar uchun: `Authorization: Bearer <token>`.

## CORS
Vue dev server `http://localhost:5173` uchun ruxsat berilgan.

## Savdo oqimi (Sale / Receipt)

- **Sale** — chek raqami (`R-yyyyMMdd-####`), kassir (JWT'dan), to'lov turi,
  jami/chegirma/QQS/qaytim summalari.
- **SaleItem** — har bir qator mahsulot nomi, MXIK, narx va QQS bilan **snapshot**
  qilinadi (keyin mahsulot o'zgarsa ham chek o'zgarmaydi). Chakana narx QQS'ni
  o'z ichiga oladi (standart 12%).
- **Receipt** — savdo bilan 1:1, fiskal maydonlar (`FiscalSign`, `QrCodeUrl`, …)
  uchun. `FiscalStatus` boshида `NotSent`.

### Fiskal oqim (lokal agent bilan)

Savdo yakunlanganda `IFiscalReceiptService` chekni **Queued** holatiga qo'yadi.
Lokal .NET agent (`agent/PosPlatform.Agent`) backenddan kutayotgan cheklarni
**pull** qiladi (agent NAT ortida turgani uchun push emas), fiskalizatsiya qiladi,
chop etadi va natijani qaytaradi:

```
Savdo → Receipt(Queued)
                 ▲  GET /api/fiscal/pending        (agent tortib oladi)
   Agent ────────┘
   Agent → FDS/Rahmat MK + ESC/POS printer (hozir simulyatsiya)
                 │  POST /api/fiscal/{saleId}/result
                 ▼
        Receipt(Registered: FiscalSign, QrCodeUrl, …)
```

Agent endpointlari **API-kalit** bilan himoyalangan (`X-Agent-Key` sarlavhasi,
`appsettings.json` → `Agent:ApiKey`). Agentni ishga tushirish uchun
[../agent/PosPlatform.Agent](../agent/PosPlatform.Agent) ga qarang.

| Metod | Yo'l                          | Himoya       | Tavsif                          |
|-------|-------------------------------|--------------|---------------------------------|
| GET   | `/api/fiscal/pending`         | `X-Agent-Key`| Fiskalizatsiya kutayotgan cheklar |
| POST  | `/api/fiscal/{saleId}/result` | `X-Agent-Key`| Fiskal natijani qaytarish       |

## Smena (kassa sessiyasi) + Refund

- **CashShift** — kassir smenani boshlang'ich naqd bilan ochadi; savdolar shu
  smenaga bog'lanadi. **Savdo qilish uchun ochiq smena shart.** Yopishda naqd
  sanaladi va hisobot chiqadi: sotuvlar, qaytarishlar, naqd/karta (sof),
  kutilgan naqd (`OpeningCash + CashSalesNet`) va **farq** (`Counted - Expected`).
- **Refund** — `POST /api/sales/{id}/refund`: asl savdoga teskari `Refund` turidagi
  yangi savdo (RR-... cheki) yaratiladi, asl savdo `Refunded` bo'ladi, va fiskal
  **return cheki** ham agent navbatiga qo'yiladi. Ikki marta qaytarish bloklanadi.

## Keyingi qadamlar (rejalashtirilgan)
- Agentdagi `SimulatedFiscalDevice`ni haqiqiy FDS / Rahmat MK SDK bilan almashtirish
- `ConsoleReceiptPrinter`ni haqiqiy ESC/POS printer (USB/tarmoq) bilan almashtirish
- Tarozi (PLU bazasini yuklash) integratsiyasi — agentga yangi qurilma seam'i
- Foydalanuvchi boshqaruvi (admin kassir qo'shadi), qisman refund, deploy (Docker)
