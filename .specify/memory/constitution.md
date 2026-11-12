<!--
Sync Impact Report
- Version: template → 1.0.0 (birinchi to'ldirish)
- Principles: I. Ko'p ijarachilik va mijoz izolyatsiyasi; II. Server — haqiqat manbai;
  III. Mavjud oqimlarni qayta ishlatish; IV. Road map bo'yicha qadamma-qadam; V. Tekshiruv darajasi
- Added sections: Texnologik cheklovlar; Ish jarayoni
- Removed sections: yo'q
- Templates: plan/spec/tasks shablonlari o'zgartirilmadi (konstitutsiyani runtime'da o'qiydi)
- Deferred TODOs: yo'q
-->

# PosCloud Constitution

## Core Principles

### I. Ko'p ijarachilik va mijoz izolyatsiyasi

- Xodim so'rovlari global tenant filtri (`OrganizationId`) orqali cheklanadi; bu filtr chetlab o'tilmaydi.
- Mobil mijoz JWT'sida tashkilot yo'q — shuning uchun `Shop*` servislarida HAR so'rovda `OrganizationId`/`BranchId`
  qo'lda filtrlanadi va `ShopUser`, `ShopOtpCode`, `ShopRefreshToken` kabi global yozuvlarda `IgnoreQueryFilters()` ishlatiladi.
- Mijoz faqat o'z `ShopUserId` yozuvlarini ko'radi va o'zgartiradi.
- Mijoz tokeni alohida audience (`Jwt:ShopAudience`) va `Shop` sxemasi bilan imzolanadi; xodim endpointlari uni qabul qilmaydi,
  mijoz endpointlari esa xodim tokenini qabul qilmaydi.

**Sabab**: bir tashkilot ma'lumoti boshqasiga yoki mijozga sizib chiqishi — eng og'ir xato.

### II. Server — haqiqat manbai

- Narx, chegirma (aksiya), qoldiq, buyurtma summasi va yetkazish narxi faqat serverda hisoblanadi; klient (mobil, desktop, web)
  yuborgan narx yoki summaga ishonilmaydi.
- Pul summalari `decimal`, bazada `numeric(18,2)`; miqdor `numeric(18,3)`.
- Sirlar (JWT kaliti, Telegram/Eskiz, Payme/Click) kodda emas, muhit o'zgaruvchilarida; productionda placeholder bilan ishga tushmaydi.

**Sabab**: klient o'zgartirilishi mumkin; pul va ombor xatosi to'g'ridan-to'g'ri zarar.

### III. Mavjud oqimlarni qayta ishlatish

- Online buyurtma "Tayyor" bo'lganda mavjud `SaleService` orqali oddiy `Sale` yaratiladi — fiskal chek, ombor va hisobotlar
  alohida yozilmaydi.
- Fiskal chek mavjud PosCloud agenti orqali; aksiyalar `PromotionCalculator` orqali.
- Yangi qatlam/abstraksiya faqat mavjudi yetmaganda qo'shiladi.

**Sabab**: bitta mantiqning ikki nusxasi vaqt o'tib bir-biridan farqlanadi.

### IV. Road map bo'yicha qadamma-qadam

- Ishning yagona manbai — `docs/mobile/PLAN.md`. Har doim birinchi belgilanmagan qadam bajariladi.
- Har qadamdan keyin: qadam `[x]` bilan belgilanadi va jurnalga bir qator yoziladi (sana — qadam — nima qilindi — nima qoldi —
  qanday tekshirildi).
- Ekran bilan bog'liq qadamdan oldin dizayn maketidagi tegishli ekran TO'LIQ o'qiladi
  (https://claude.ai/artifact/H9qqdTExBhCdqrnFEYfGva, `project/<Ekran>.dc.html`).
- Dizaynda bor, lekin qadamga kirmagan narsa jurnalda "keyinga" deb yoziladi, jimgina tashlab ketilmaydi.

**Sabab**: ish ko'p chatlarga bo'linadi; har yangi chat PLAN.md dan davom etadi.

### V. Tekshiruv darajasi halol aytiladi

- Har qadam kamida kompilyatsiya bilan tekshiriladi: backend `dotnet build` (0 xato), mobil `flutter analyze` (0 muammo),
  frontend `npm run build`, desktop `dotnet build`.
- Foydalanuvchi ruxsati bilan (2026-11-12) qadamlar runtime test qilinmasdan bajariladi; bunday holatda jurnalda
  "runtime sinalmagan" deb aniq yoziladi.
- Sinalmagan narsa "ishlaydi" deb aytilmaydi.

**Sabab**: tekshirilmagan qismni bilish keyingi sinov va xatoni topishni tezlashtiradi.

## Texnologik cheklovlar

- **Backend**: .NET 8, ASP.NET Core, EF Core 8 + PostgreSQL (Npgsql), Clean Architecture
  (`Domain` / `Application` / `Infrastructure` / `Api`). Migratsiya har model o'zgarishida `dotnet ef migrations add`.
- **Desktop kassa**: WPF (.NET), Velopack yangilanish; online buyurtmalar SignalR orqali.
- **Admin panel**: Vue (frontend/), backend `wwwroot` dan beriladi.
- **Mobil**: Flutter (`mobile/`), Riverpod, go_router, dio, flutter_secure_storage, freezed; `package:material_ui` (Flutter 3.47+);
  tillar: o'zbek (asosiy) va rus; shrift Manrope ilova ichida.
- Xatolar foydalanuvchiga o'zbek tilida, tushunarli; ichki texnik tafsilot oshkor qilinmaydi.

## Ish jarayoni

- Javoblar va hujjatlar o'zbek tilida; kod identifikatorlari ingliz tilida, izohlar o'zbek tilida (mavjud uslub).
- Commit, push yoki PR faqat foydalanuvchi so'raganda.
- Kontekst kattalashganda foydalanuvchiga yangi chat ochish taklif qilinadi; davom etish PLAN.md orqali.
- Spec Kit ishlatilsa: `specs/NNN-.../` dagi spec/plan/tasks shu konstitutsiyaga mos bo'lishi shart.

## Governance

- Bu konstitutsiya boshqa ish odatlaridan ustun. O'zgartirish: foydalanuvchi roziligi bilan, versiya va sana yangilanadi.
- Versiya: MAJOR — tamoyil olib tashlansa yoki ma'nosi o'zgarsa; MINOR — yangi tamoyil/bo'lim; PATCH — aniqlashtirish.
- Har reja (`/speckit-plan`) va kod ko'rib chiqishda I–V tamoyillarga muvofiqlik tekshiriladi.

**Version**: 1.0.0 | **Ratified**: 2026-11-12 | **Last Amended**: 2026-11-12
