# PosPlatform.Agent — Lokal fiskal/printer agenti

Kassir kompyuterida ishlaydigan lokal .NET 8 worker. Backenddan fiskalizatsiya
kutayotgan cheklarni **pull** qiladi, fiskalizatsiya qiladi, chekni chop etadi va
natijani backendga qaytaradi. Hozir qurilmalar **simulyatsiya** qilinadi.

## Nega lokal agent?
Fiskal modul (FDS / Rahmat MK), ESC/POS printer va tarozi — bular lokal qurilmalar.
Web backend ularga to'g'ridan-to'g'ri ula olmaydi. Agent NAT ortida turgani uchun
backendga **o'zi murojaat qiladi** (pull), push emas.

## Arxitektura (kengaytirish nuqtalari)

| Seam (interfeys)   | Hozirgi impl.            | Keyin                              |
|--------------------|-------------------------|------------------------------------|
| `IFiscalDevice`    | `SimulatedFiscalDevice` | FDS / Rahmat MK SDK yoki HTTP      |
| `IReceiptPrinter`  | `ConsoleReceiptPrinter` | ESC/POS (USB/tarmoq) printer       |
| _(yangi)_ tarozi   | —                       | PLU bazasini yuklash qurilmasi     |

`FiscalWorker` asosiy halqani boshqaradi: `GET /api/fiscal/pending` →
har chek uchun `IFiscalDevice.FiscalizeAsync` + `IReceiptPrinter.PrintAsync` →
`POST /api/fiscal/{saleId}/result`.

## Sozlash

`appsettings.json`:
- **Agent:BackendBaseUrl** — backend manzili (masalan `http://localhost:5000`)
- **Agent:ApiKey** — backenddagi `Agent:ApiKey` bilan **bir xil** bo'lishi shart
- **Agent:PollIntervalSeconds**, **BatchSize**, **FiscalModuleId**

## Ishga tushirish

Avval backend ishlab tursin, so'ng:

```bash
cd agent/PosPlatform.Agent
dotnet run
```

Tekshirish: backendda `POST /api/sales` bilan savdo yarating — agent uni
fiskalizatsiya qilib, konsolga chekni chop etadi va savdoning `Receipt` holati
`Registered` ga o'tadi (`GET /api/sales/{id}` bilan ko'ring).
