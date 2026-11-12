# PosCloud Mobil ilova — ish rejasi

Bu fayl ishning yagona manbai. Har yangi chatda shu fayldan boshlang:
"`docs/mobile/PLAN.md` ni o'qi va keyingi belgilanmagan qadamni davom ettir".

To'liq qo'llanma: https://claude.ai/artifact/5782SYvpnDwzUVbxFKtUmC
Dizayn maketi: https://claude.ai/artifact/H9qqdTExBhCdqrnFEYfGva

## Dizayn (maketdagi ekranlar)

Uslub: fon `#F5F4EF`, asosiy yashil `#0B6B45` (hover `#08523A`), matn `#16201B`, ikkilamchi `#5B6660`,
chegara `#E4E2DA`; shrift **Manrope** (400–800); burchaklar 14–20px; tugma/input balandligi 56px; telefon 390×844.

| Ekran (maketda) | Flutter qadami | API |
|---|---|---|
| Login — telefon +998, **5 xonali** kod, "qayta yuborish 0:42", "Hozircha kirmasdan ko'rish" | 4.2 | 1.3 |
| Main — do'kon, kategoriyalar, mahsulotlar | 4.3 | 1.4 |
| Product | 4.3 | 1.4 |
| Cart, Checkout | 4.4 | 1.6 |
| Orders, Tracking | 4.5 | 1.6 |
| Profile, PersonalInfo | 4.6 | 1.5 |
| Addresses, AddressEdit | 4.6 | 1.5 |
| BonusCard | keyingi bosqich | — |
| Desktop (kassadagi online buyurtmalar oynasi) | 2.1–2.3 | 1.7–1.8 |

Maketdan "Hozircha kirmasdan ko'rish" → katalog API (1.4) tokensiz ochiq bo'lishi kerak.

## Qabul qilingan qarorlar

- Ilova: **Flutter (Dart)**, bitta ilova — "PosCloud Market". Do'konlarni **SuperAdmin** boshqaradi.
- Backend: mavjud .NET 8 API, alohida server yo'q.
- Kirish: telefon + kod. Avval **Telegram Gateway**, bo'lmasa **SMS** (Eskiz).
- To'lov (1-bosqich): **naqd** va **kartaga o'tkazma** (kassir "To'lov tushdi"ni bosadi). Payme/Click keyin.
- O'tkazma yig'ishdan keyin, kuryerga berishdan oldin (do'kon sozlamasi bilan o'zgaradi).
- Fiskal chek: mavjud PosCloud agenti. "Tayyor"da oddiy `Sale` yaratiladi.
- Kuryer: do'konning o'z kuryeri yoki olib ketish.
- Bonus: do'kon belgilaydi (keyingi bosqich).

## Arxitektura qoidalari (kod yozishda shunga amal qiling)

- **Mijoz akkaunti global**: `ShopUser` (telefon bo'yicha, `OrganizationId = null`). Har do'konda mavjud
  `Customer` yozuvi `ShopUserId` orqali bog'lanadi (bonus do'kon bo'yicha alohida).
- **Tenant filtri**: mijoz JWT'sida tashkilot yo'q → global filtr o'chadi. Shuning uchun `Shop*` servislarida
  **har so'rovda `OrganizationId`/`BranchId` ni qo'lda filtrlash shart**. Mijoz faqat o'z `ShopUserId` yozuvlarini ko'radi.
- Mijoz JWT'si xodim JWT'sidan alohida: claim `role=ShopCustomer`, `sub=ShopUserId`. Xodim endpointlariga kira olmaydi.
  Alohida audience (`Jwt:ShopAudience`) va sxema `Shop`. Mijoz endpointlari: `[Authorize(Policy = ShopAuthDefaults.Policy)]`.
  Katalog (1.4) — `[AllowAnonymous]`.
- `ShopUser`/`ShopOtpCode`/`ShopRefreshToken` so'rovlarida `IgnoreQueryFilters()` ishlating (global yozuvlar).
- Refresh token har safar almashadi (rotation). Flutter'da refresh **bitta vaqtda bitta** bo'lsin (dio interceptor'da lock),
  aks holda 30 soniyadan keyin qayta kelgan eski token barcha sessiyalarni yopadi.
- Narx, qoldiq va summa har doim serverda hisoblanadi; ilovadan kelgan narxga ishonilmaydi.
- `OnlineOrder` → "Tayyor"da `Sale` (mavjud `SaleService` orqali) → fiskal chek, ombor, hisobotlar o'zgarmaydi.
- Pul summalari `decimal`, `numeric(18,2)`.

## Buyurtma holatlari

`New → Accepted → Picking → Ready → (AwaitingTransfer) → OnTheWay | ReadyForPickup → Delivered`
Yon holatlar: `Rejected`, `Cancelled`. Mijoz faqat `Picking` gacha bekor qila oladi.
To'lov holati alohida: `Unpaid → Paid → Refunded`.

---

## Bosqich 1 — Backend asosi

- [x] **1.1 Domen**: enumlar, `ShopUser`, `ShopUserAddress`, `ShopRefreshToken`, `OnlineOrder`, `OnlineOrderItem`,
      `OnlineStoreSettings`; `Customer.ShopUserId`, `Product.ShowOnline`, `Branch` koordinatalari; EF konfiguratsiyalar, DbSet'lar.
- [x] **1.2 Migratsiya**: `dotnet ef migrations add AddOnlineShop` va build tekshiruvi.
- [x] **1.3 Mijoz kirishi**: `IOtpSender` (Telegram Gateway + SMS zaxira), OTP saqlash (urinish limiti),
      `POST /api/customer-auth/send-otp | verify | refresh | logout`, mijoz JWT'si.
- [x] **1.4 Katalog API** (tokensiz): `GET /api/shop/stores?lat=&lng=&search=`, `/api/shop/stores/{branchId}`,
      `/api/shop/stores/{branchId}/categories`, `/products?search=&categoryId=&sort=&inStockOnly=&page=&pageSize=`, `/products/{id}`
      (faqat `ShowOnline` va ilovada yoqilgan do'konlar).
- [x] **1.5 Profil va manzillar**: `GET/PUT/DELETE /api/me`, `/api/me/addresses` CRUD.
- [x] **1.6 Buyurtma (mijoz)**: `POST /api/shop/orders`, ro'yxat, bitta, bekor qilish, `transfer-proof`.
- [x] **1.7 Buyurtma (xodim)**: `/api/online-orders` — accept/reject/start-picking/items/ready/payment-received/dispatch/delivered.
      "Ready"da `Sale` yaratish. Ombor rezervi (`BranchStock.Reserved`).
- [x] **1.8 SignalR**: `/hubs/orders`, filial guruhlari; yangi buyurtma va holat o'zgarishi xabarlari.
- [ ] **1.9 SuperAdmin**: `/api/admin/online-stores` (yoqish, tasdiqlash, bloklash, to'lov usullari, tavsiya, tarif muddati);
      do'kon admini: `GET/PUT /api/online-store/settings`.
- [ ] **1.10 Fon vazifalar**: eskirgan OTP/refresh tokenlarni tozalash.

## Bosqich 2 — Desktop (WPF)

- [ ] 2.1 SignalR klient, "Online buyurtmalar" oynasi (4 ustun), ovozli bildirishnoma.
- [ ] 2.2 Yig'ish paneli: skaner bilan belgilash, almashtirish/olib tashlash, yig'ish varag'ini chop etish.
- [ ] 2.3 "Tayyor" → `Sale` + fiskal chek; "O'tkazma kutilmoqda" va "To'lov tushdi".
- [ ] 2.4 "Buyurtma qabul qilish" yoqish/o'chirish, kuryer naqd pulini qabul qilish.

## Bosqich 3 — Admin panel (Vue)

- [ ] 3.1 SuperAdmin: online do'konlar ro'yxati va sozlamalari.
- [ ] 3.2 Do'kon admini: online sozlamalar (karta, ish vaqti, minimal summa), mahsulotda `ShowOnline`, rasmlar.

## Bosqich 4 — Flutter MVP (`mobile/`)

- [x] 4.1 Loyiha: `flutter create`, paketlar (riverpod, go_router, dio, freezed, secure_storage), tema, i18n (uz/ru).
- [ ] 4.2 Kirish (telefon + kod), token saqlash va yangilash.
- [ ] 4.3 Do'kon tanlash, katalog, qidiruv, mahsulot sahifasi.
- [ ] 4.4 Savatcha, rasmiylashtirish (yetkazish/olib ketish, naqd/o'tkazma).
- [ ] 4.5 Buyurtmalarim, kuzatish, o'tkazma ekrani (karta raqami nusxa + chek rasmi).
- [ ] 4.6 Profil, manzillar (xarita), akkauntni o'chirish.
- [ ] 4.7 Push (FCM): `DeviceToken`, holat xabarlari.

## Bosqich 5 — Chiqarish

- [ ] 5.1 Ikonka, splash, maxfiylik siyosati sahifasi.
- [ ] 5.2 Android: imzolash, Play internal testing.
- [ ] 5.3 iOS: Codemagic yoki Mac, TestFlight.
- [ ] 5.4 Monitoring (Crashlytics/Sentry), majburiy yangilash (`/api/app/config`).

## Keyingi bosqichlar (MVP'dan keyin)

Bonus karta (sozlamalar, dinamik QR, kassada skanerlash) · Payme/Click online buyurtmaga · yetkazish hududlari (xarita) ·
saqlangan kartalar · kuryer ilovasi · white-label.

---

## Jurnal

Har qadamdan keyin bir qator yozing: sana — qadam — nima qilindi — nima qoldi.

- 2026-11-11 — 1.1 — domen entity, enum, EF konfiguratsiyalar (`OnlineShopConfiguration.cs`). `HasDefaultValue(true)` ishlatilmadi: bool uchun false yozilmay qoladi.
- 2026-11-11 — 1.2 — `AddOnlineShop` migratsiyasi; `Products.ShowOnline` mavjud qatorlar uchun true. Lokal PostgreSQL ishlamayotgani uchun bazaga qo‘llanmadi (`dotnet ef database update` keyin). Keyingisi: 1.3 mijoz kirishi.
- 2026-11-11 — 1.3 — `CustomerAuthController` (`/api/customer-auth/send-otp|verify|refresh|logout`), `ShopAuthService`,
  `ShopOtpCode` entity + `AddShopOtp` migratsiyasi, `ShopTokenGenerator` (audience `PosCloudShop`, 60 daq; refresh 60 kun, xesh saqlanadi),
  `OtpSender`: Telegram Gateway → Eskiz SMS → dev'da logga (`Otp:LogCodes`). Kod 5 xonali, 5 daqiqa, 5 urinish;
  qayta so'rash 60 s, raqamga soatiga 5, IP'ga soatiga 20 + rate limit `otp` (daqiqasiga 5). 429 + `Retry-After`.
  `LicenseValidationMiddleware` mijozni o'tkazib yuboradi. Build: 0 xato. **Runtime sinalmagan** (lokal PostgreSQL yo'q).
  Qoldi: prod'da `Otp__Telegram__Token`, `Otp__Eskiz__Email/Password` berish va Eskiz'da SMS shablonini tasdiqlatish.
  Keyingisi: 1.4 katalog API.
- 2026-11-12 — 1.4 — `ShopCatalogController` + `ShopCatalogService`. Ko'rinadigan do'kon: filial va tashkilot faol, `BusinessType=Store`,
  `OnlineStoreSettings` yoqilgan, bloklanmagan, `PaidUntil` o'tmagan. Mahsulot: o'z tashkiloti, `ShowOnline`, `ProductType=Good`,
  shu filial yoki umumiy. Narx — aksiya qo'llangan (`PromotionCalculator`), `oldPrice` + `discountPercent` dizayndagi chizilgan narx uchun.
  `available = Quantity − Reserved`. "Ommabop" = oxirgi 30 kunda filialda necha marta sotilgan (`sort=popular`, standart).
  Qidiruv: nom (registrsiz) yoki aniq shtrix-kod. Ish vaqti UTC+5, tungi smena hisobga olinadi; `isOpenNow`.
  EF so'rovlari bazasiz `ToQueryString()` bilan SQL'ga tarjima bo'lishi tekshirildi; real ma'lumotda sinalmagan.
  Dizaynda bor, modelda yo'q (keyin): bir nechta rasm (karusel), brend, "saqlash / yaroqlilik / ishlab chiqaruvchi" jadvali,
  yetkazish vaqti matni ("60–90 daqiqa"), sevimlilar. `imageUrl` nisbiy bo'lishi mumkin — ilova API manzilini qo'shadi.
  Keyingisi: 1.5 profil va manzillar.
- 2026-11-12 — 1.5 — `ShopMeController` (`[Authorize(Policy = ShopAuthDefaults.Policy)]`) + `ShopProfileService`; `User.GetShopUserId()`.
  Profil: ism majburiy, tug'ilgan sana o'tmishda, email kichik harfga. Manzil: ko'pi bilan 10 ta, birinchisi avtomatik asosiy,
  asosiy o'chirilsa eng yangisi asosiy bo'ladi, koordinata ikkalasi birga. Har so'rovda akkaunt faolligi tekshiriladi
  (bloklangan/o'chirilgan → 401, token muddati tugashini kutmaydi). Akkaunt o'chirish: yakunlanmagan buyurtma bo'lsa rad etadi;
  telefon `~<id>` ga almashadi (raqam bo'shaydi), shaxsiy ma'lumot va manzillar o'chadi, sessiyalar yopiladi, `Customer.ShopUserId` uziladi;
  buyurtmalar do'kon hisobi uchun qoladi.
  **Real sinov** (Docker'da vaqtinchalik PostgreSQL, sinovdan keyin o'chirildi): barcha migratsiyalar toza bazaga qo'llandi;
  1.3 — kod yuborish/tekshirish, 429 + Retry-After, noto'g'ri kod, kodni qayta ishlatish, refresh almashuvi, eski refresh rad;
  token ajratilishi — mijoz tokeni xodim endpointida 401, xodim tokeni `/api/me` da 401; 1.4 — do'kon masofasi, o'chirilgan do'kon 404,
  aksiya narxi (15 500 → 13 950, −10%), qoldiq = 26 − 2, yashirin/qoldiqsiz/boshqa filial tovari chiqmaydi, shtrix-kod va registrsiz qidiruv;
  1.5 — profil, manzillar, asosiy almashuvi, akkaunt o'chirish. Hammasi kutilgandek.
  Eslatma: seed toza bazada `admin` yaratmadi, faqat `superadmin` (alohida tekshirish kerak, mobilga aloqasi yo'q).
  Dizaynda bor, keyinga: avatar rasmi, telefon raqamini almashtirish (yangi raqamga kod), to'lov kartalari, sevimlilar,
  bildirishnoma sozlamasi, yetkazish hududi tekshiruvi ("Yetkazish hududida · Chilonzor filiali").
  Keyingisi: 1.6 buyurtma (mijoz).
- 2026-11-12 — 4.1 — (foydalanuvchi so'rovi bilan 1.6–1.10 dan oldin boshlandi). Flutter 3.50 stable `C:\src\flutter` ga o'rnatildi
  (foydalanuvchi PATH'iga qo'shildi). `mobile/`: `flutter create --org uz.poscloud --project-name poscloud_market --platforms android,ios`
  → ilova ID: `uz.poscloud.poscloud_market` (do'konga chiqqandan keyin o'zgarmaydi — kerak bo'lsa hozir almashtirish kerak).
  Paketlar: flutter_riverpod 3, go_router 18, dio 5, flutter_secure_storage 11, freezed/json_serializable (+build_runner),
  intl, flutter_localizations, material_ui/cupertino_ui (Flutter 3.47+ da `package:flutter/material.dart` eskirgan — shu ishlatiladi).
  Tuzilma: `lib/core/` (config, router, theme), `lib/features/<qism>/`, `lib/l10n/` (uz — asosiy, ru).
  Tema dizayndan: `AppColors`, `AppTheme` (Manrope ilova ichida — `assets/fonts`, OFL; internetdan yuklanmaydi).
  Router: StatefulShellRoute — pastki menyu Asosiy/Savatcha/Buyurtmalar/Profil (hozircha "Tez orada" ekranlari).
  API manzili: `--dart-define=API_BASE_URL=...` (standart `http://10.0.2.2:5099` — emulyatordan lokal API); `AppConfig.imageUrl()` nisbiy rasm uchun.
  `flutter analyze` — 0 muammo, `flutter test` — 1/1 o'tdi. **Android SDK yo'q** — telefon/emulyatorda ishga tushirish uchun
  Android Studio o'rnatish kerak. iOS — faqat Mac yoki Codemagic'da (5.3).
  Keyingisi: 4.2 kirish ekrani (backend 1.3 tayyor).
- 2026-11-12 — 1.6 — `ShopOrdersController` (`/api/shop/orders`) + `ShopOrderService`; umumiy qoidalar `OnlineOrderRules`
  (yakuniy holatlar, mijoz bekor qilishi `New|Accepted`, rezerv `Accepted|Picking`da, `AdjustReservationAsync` — 1.7 ham ishlatadi).
  `POST /quote` (tokensiz, savatcha uchun): server narxi (aksiya — katalog bilan bir xil `DiscountedPrice`), qator muammolari
  (`not_found|out_of_stock|not_enough|invalid_quantity`), yetkazish narxi, bepul yetkazishgacha qolgan summa, minimal summa, `canOrder` + `message`.
  `POST /` — quote qayta hisoblanadi, ilova narxi qabul qilinmaydi; to'lov usuli do'kon sozlamasiga qarab (Cash/Transfer/Card), yetkazishda
  saqlangan manzil (`addressId`) nusxalanadi; "imkon qadar tez" — do'kon hozir ochiq bo'lishi kerak, vaqt tanlansa 30 daq–2 kun, ish vaqtida;
  faol buyurtma ko'pi bilan 5; raqam tashkilot ichida 1001 dan, unikal indeks to'qnashuvida qayta urinish; do'kondagi `Customer`
  `ShopUserId` → telefon bo'yicha bog'lanadi yoki yaratiladi. Rezerv yaratishda QO'YILMAYDI (qabulda, 1.7).
  `GET /?active=&page=` ro'yxat (oldindan 3 ta rasm), `GET /{id}` (holat vaqtlari, `canCancel`, karta raqami faqat Transfer va
  `Ready`dan keyin, to'lanmagan bo'lsa), `POST /{id}/cancel` (Accepted'da rezerv bo'shatiladi), `POST /{id}/transfer-proof`
  (multipart rasm 5 MB, `wwwroot/uploads/transfers`). Katalog yordamchilari `internal static` qilindi (qayta ishlatish).
  Tekshiruv: `dotnet build` 0 xato. **Runtime sinalmagan.**
  Keyinga: promokod va bonus bilan to'lash (dizaynda bor), takroriy bosishdan himoya (idempotency key), "Qayta buyurtma" (ilova
  `items`dan savatcha tuzadi — API shart emas), chek rasmlari ommaviy `/uploads` da (GUID nomi bilan; keyin himoyalangan endpoint),
  yetkazish vaqti matni ("60–90 daqiqa"), kuryer ismi/raqami (Tracking), fiskal chek QR (1.7 dan keyin `SaleId` orqali).
- 2026-11-12 — 1.7 — `OnlineOrdersController` (`/api/online-orders`, `[Authorize]` + `sales.view`/`sales.create` — yangi ruxsat qo'shilmadi)
  + `OnlineOrderService`. Tenant filtri ishlaydi; filialga biriktirilgan xodim faqat o'z filialini ko'radi. `GET ?active=true|false&from=&branchId=`,
  `GET /{id}` (qatorda shtrix-kod, `isMarked`, filial qoldig'i). O'tishlar: `accept` (New→Accepted, rezerv qo'yiladi; qoldiq yetmasa ham qabul),
  `reject` (New/Accepted/Picking, sabab majburiy, rezerv bo'shaydi), `start-picking`, `PUT items` (Picking: olingan miqdor, olib tashlash,
  almashtirish/qo'shish — narx server aksiyasi bilan; rezerv qayta hisoblanadi; summa qayta hisoblanadi, yetkazish narxi o'zgarmaydi),
  `ready` (Picking → `SaleService.CreateAsync` orqali oddiy `Sale`: `ClientSaleId = online:{id}` idempotent, `SkipPromotions` —
  narx buyurtmada qotirilgan, `UnitPriceOverride`; rezerv bo'shatiladi, savdo ombordan ayiradi; ochiq smena kerak; keyin
  Transfer+to'lanmagan+(sozlama yoki olib ketish) → `AwaitingTransfer`, olib ketish → `ReadyForPickup`, aks holda `Ready`),
  `payment-received` (Paid; AwaitingTransfer'dan chiqadi), `dispatch` (Ready→OnTheWay, faqat yetkazish), `delivered`
  (OnTheWay/ReadyForPickup; naqd/terminal avtomatik Paid, o'tkazma tasdiqlanmagan bo'lsa rad). `CreateSaleRequest.SkipPromotions` — `[JsonIgnore]`, faqat server ichida.
  Tekshiruv: `dotnet build` 0 xato. **Runtime sinalmagan.**
  Keyinga: yetkazish narxi `Sale`/fiskal chekka kirmaydi (alohida "Yetkazish xizmati" MXIK mahsuloti kerak); markirovkali tovar
  "Tayyor"da DataMatrix talab qiladi — online yig'ishda kod skanerlash yo'q (hozircha SaleService xatosi chiqadi);
  "Ready"dan keyin bekor qilish = savdoni qaytarish (refund) — hali yo'q; naqd savdo "Tayyor" paytida smenaga yoziladi (pul kuryer bilan keyin keladi — 2.4).
- 2026-11-12 — 1.8 — `OrdersHub` (`/hubs/orders`, ikkala sxema: xodim `Bearer` va mijoz `Shop`; token `?access_token=` faqat `/hubs` yo'lida).
  Guruh ulanishda serverda tanlanadi (klient so'ray olmaydi): xodim `branchId` bo'lsa `branch:{id}`, bo'lmasa `org:{id}`; mijoz `shop-user:{id}`.
  `IOnlineOrderNotifier` (Application) → `SignalROnlineOrderNotifier` (Api): xodimga `OrderCreated`/`OrderUpdated`, mijozga `OrderStatusChanged`;
  tarkib yengil `{id, number, branchId, status, paymentStatus, fulfillmentType, totalAmount}` (shaxsiy ma'lumotsiz, enum — string), klient tafsilotni API'dan oladi.
  Xabar xatosi amalni buzmaydi (loglanadi). Chaqiriladi: mijoz yaratish/bekor qilish/chek yuklash, xodimning barcha o'tishlari.
  Tekshiruv: `dotnet build` 0 xato. **Runtime sinalmagan.**
  Keyinga: bir nechta server nusxasi bo'lsa Redis backplane; SuperAdmin (tashkilotsiz) hech qaysi guruhga kirmaydi.
