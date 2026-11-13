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
- [x] **1.9 SuperAdmin**: `/api/admin/online-stores` (yoqish, tasdiqlash, bloklash, to'lov usullari, tavsiya, tarif muddati);
      do'kon admini: `GET/PUT /api/online-store/settings`.
- [x] **1.10 Fon vazifalar**: eskirgan OTP/refresh tokenlarni tozalash.

## Bosqich 2 — Desktop (WPF)

- [x] 2.1 SignalR klient, "Online buyurtmalar" oynasi (4 ustun), ovozli bildirishnoma.
- [x] 2.2 Yig'ish paneli: skaner bilan belgilash, almashtirish/olib tashlash, yig'ish varag'ini chop etish.
- [x] 2.3 "Tayyor" → `Sale` + fiskal chek; "O'tkazma kutilmoqda" va "To'lov tushdi".
- [x] 2.4 "Buyurtma qabul qilish" yoqish/o'chirish, kuryer naqd pulini qabul qilish.

## Bosqich 3 — Admin panel (Vue)

- [x] 3.1 SuperAdmin: online do'konlar ro'yxati va sozlamalari.
- [x] 3.2 Do'kon admini: online sozlamalar (karta, ish vaqti, minimal summa), mahsulotda `ShowOnline`, rasmlar.

## Bosqich 4 — Flutter MVP (`mobile/`)

- [x] 4.1 Loyiha: `flutter create`, paketlar (riverpod, go_router, dio, freezed, secure_storage), tema, i18n (uz/ru).
- [x] 4.2 Kirish (telefon + kod), token saqlash va yangilash.
- [x] 4.3 Do'kon tanlash, katalog, qidiruv, mahsulot sahifasi.
- [x] 4.4 Savatcha, rasmiylashtirish (yetkazish/olib ketish, naqd/o'tkazma).
- [x] 4.5 Buyurtmalarim, kuzatish, o'tkazma ekrani (karta raqami nusxa + chek rasmi).
- [x] 4.6 Profil, manzillar (xarita), akkauntni o'chirish.
- [x] 4.7 Push (FCM): `DeviceToken`, holat xabarlari. (kod tayyor — Firebase fayllari qo'yilgach ishlaydi, `mobile/docs/PUSH.md`)

## Bosqich 5 — Chiqarish

- [x] 5.1 Ikonka, splash, maxfiylik siyosati sahifasi.
- [ ] 5.2 Android: imzolash, Play internal testing. (gradle sozlamasi va `mobile/docs/RELEASE.md` tayyor — keystore, Play akkaunt egasidan)
- [ ] 5.3 iOS: Codemagic yoki Mac, TestFlight.
- [x] 5.4 Monitoring (Crashlytics/Sentry), majburiy yangilash (`/api/app/config`).

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
- 2026-11-12 — 1.9 — `OnlineStoreService`; `AdminOnlineStoresController` (`/api/admin/online-stores`, `Roles=SuperAdmin`): `GET ?filter=all|requested|enabled|blocked&search=`
  (barcha tashkilotlar, Store filiallar soni, 30 kunlik buyurtmalar; so'rov yuborganlar birinchi), `GET/PUT /{orgId}` (yoqish, bloklash — sabab majburiy,
  tavsiya, tartib, `PaidUntil`, to'lov usullari — yoqilganda kamida bittasi; yoqilganda `IsRequested` tushadi).
  `OnlineStoreController` (`/api/online-store`): `GET settings` (`sales.view`), `PUT settings` (`settings.manage`: qabul, nom, tavsif, logo, telefon,
  yetkazish/olib ketish — kamida bittasi, minimal summa, yetkazish narxi, bepul chegarasi, ish vaqti — ikkalasi birga, karta 16–20 raqam, holder,
  o'tkazma dispatchdan oldin), `POST request` (ilovaga chiqish so'rovi), `PUT accepting` (`sales.create` — kassadagi tugma, 2.4 uchun).
  Sozlama yozuvi kerak bo'lganda yaratiladi (standart — ko'rinmaydi). `IsVisible` hisoblanadi.
  O'tkazma faqat karta raqami kiritilgan bo'lsa ishlaydi (katalog `allowTransfer` va buyurtma tekshiruvi).
  Tekshiruv: `dotnet build` 0 xato. **Runtime sinalmagan.**
- 2026-11-12 — 1.10 — `ShopCleanupService` (Api, `BackgroundService`; Infrastructure'da Hosting paketi yo'qligi uchun Api'da): ishga tushgandan 2 daq keyin,
  so'ng har 6 soatda — OTP 1 kundan eski, refresh token muddati 1 kundan oldin tugagan yoki 30 kundan oldin bekor qilingan → `ExecuteDeleteAsync`.
  Xato loglanadi, servis to'xtamaydi. `dotnet ef migrations has-pending-model-changes` — 1.6–1.10 da model o'zgarmagan, migratsiya kerak emas.
  Tekshiruv: `dotnet build` 0 xato. **Runtime sinalmagan.**
  Keyinga: uzoq vaqt `New` holatida qolgan buyurtmalarni avtomatik bekor qilish (mijozga xabar bilan), o'chirilgan akkauntlarni yakuniy tozalash.
  Keyingisi: 2.1 desktop.
- 2026-11-12 — 2.1 — Desktop: `Microsoft.AspNetCore.SignalR.Client` 8.0.11; `Services/OnlineOrdersHub` (token `AppState.Token`, avtomatik qayta ulanish,
  to'liq uzilsa har 30 s urinish, ulangach ro'yxat API'dan qayta olinadi; yangi buyurtmada `SystemSounds.Exclamation`). Kassa oynasida "🛒 Online" tugmasi
  + "Yangi" soni badge'i — faqat do'kon rejimida va SuperAdmin do'konni yoqqan bo'lsa (`GET /api/online-store/settings`). `Views/OnlineOrdersWindow`:
  4 ustun (Yangi / Yig'ilmoqda = Accepted+Picking / Tayyor = Ready+AwaitingTransfer+ReadyForPickup / Yo'lda), kartochkada vaqt, tur, to'lov, mijoz, summa;
  real vaqt holati sarlavhada. Modellar `Models/OnlineOrderModels.cs`, API `BackendClient` (online-orders, online-store).
- 2026-11-12 — 2.2 — Yig'ish paneli: skaner maydoni (klaviatura skaneri) va COM skaner (`AppState.ScanOverride` — oyna ochiq turganda kod savatchaga
  emas, shu oynaga ketadi) qatorni belgilaydi, "Yig'ildi x / y"; belgilar faqat kassada (`CheckedItems`, oyna yopilsa ham saqlanadi).
  Qator: "Miqdor" (tarozi/kam), "Almashtirish" (yangi shtrix-kod — kesh `Offline.LoadProducts()` dan, miqdor), "Yo'q" (olib tashlash; mijoz siyosati ko'rsatiladi);
  "Mahsulot qo'shish". "Yig'ish varag'i" — `PrintDialog` + `FlowDocument` (☐, nom, shtrix-kod, miqdor). Oddiy so'rov oynasi `Views/PromptWindow.cs`.
- 2026-11-12 — 2.3 — "Tayyor · fiskal chek": belgilanmagan qator bo'lsa ogohlantirish → `POST ready` (`terminalId` bilan) → `GET /api/sales/{saleId}` →
  `ReceiptPreviewWindow` (Fiscal) — chek oddiy savdodagi kabi (`SalesHistoryWindow.ToReceiptData` `internal` qilindi). "To'lov tushdi" (AwaitingTransfer,
  Ready/ReadyForPickup/OnTheWay'da o'tkazma to'lanmagan bo'lsa), "O'tkazma cheki" (rasm brauzerda), "Chek" (qayta ko'rish).
- 2026-11-12 — 2.4 — Sarlavhada "Buyurtma qabul qilish yoqilgan" (`PUT /api/online-store/accepting`, xato bo'lsa qaytariladi). "Kuryerga berildi" (dispatch),
  "Yetkazildi · kuryer pulini oldim" (naqd bo'lsa summa bilan tasdiqlash → `delivered`, server Paid qiladi), "Mijozga topshirildi" (olib ketish).
  Tekshiruv (2.1–2.4): `dotnet build` 0 xato (4 ogohlantirish — eski `UpdateService`). **Runtime sinalmagan.**
  Keyinga: kuryer ismi/raqami (dizaynda "Kuryer: Bekzod"), kuryer naqd pulini smenaga alohida "kirim" qilish (hozir savdo "Tayyor"da naqd deb yoziladi),
  yig'ishda markirovka (DataMatrix) skanerlash, javon raqami ("Javon A-3" — modelda yo'q), ovozni takrorlash (qabul qilinmaguncha),
  mijoz telefonini yashirish (dizaynda `+998 90 *** ** 67`).
- 2026-11-12 — 3.1 — Vue: `AdminOnlineStoresView` (`/admin/online-stores`, SuperAdmin menyusida "Online do'konlar"): filtr (barchasi / so'rov yuborgan /
  ilovada / bloklangan), qidiruv, jadval (holat, Store filiallar, tarif muddati, 30 kunlik buyurtma, qabul); tanlanganda yon panel — yoqish, bloklash + sabab,
  tavsiya, tartib, tarif muddati (kun oxirigacha), to'lov usullari (karta raqami yo'q bo'lsa ogohlantirish), ish vaqti/yetkazish ma'lumoti.
  API: `onlineStoresAdminApi`. Tarjima: `nav.onlineStores` (uz/ru).
- 2026-11-12 — 3.2 — Vue: `OnlineStoreView` (`/online-store`, Admin + `settings.manage`, faqat do'kon rejimida — `storeOnly`): holat kartasi
  (ilovada / so'rov / bloklangan, platforma bergan to'lov usullari, tarif), "Ilovaga chiqish so'rovi", qabul qilish, nom, telefon, tavsif, logo
  (yuklash `/api/products/image` orqali), ish vaqti, yetkazish/olib ketish, minimal summa, yetkazish narxi, bepul chegarasi, karta raqami/egasi,
  o'tkazma dispatchdan oldin. Mahsulot formasiga "Ilovada" (`showOnline`) tugmasi; backend `CreateProductDto.ShowOnline` (`bool?`:
  yaratishda null → true, tahrirda null → o'zgarmaydi — eski klientlar yashirilgan mahsulotni qayta ochib yubormasligi uchun), `ProductDto.ShowOnline`.
  Mahsulot rasmlari — mavjud yuklash (bitta rasm).
  Tekshiruv: `npm run build` muvaffaqiyatli, backend `dotnet build` 0 xato. **Runtime sinalmagan.**
  Keyinga: mahsulotga bir nechta rasm (karusel), ommaviy "ilovada ko'rsatish/yashirish", admin panelda online buyurtmalar ro'yxati/hisoboti.
- 2026-11-12 — 4.2 — Mobil kirish: `core/api/api_client.dart` (Dio + `SessionManager`: token oldindan yangilanadi, refresh BITTA vaqtda bitta —
  umumiy `Future`; 401 da boshqa so'rov allaqachon yangilagan bo'lsa qayta refresh qilinmaydi; refresh 401/400 → sessiya tozalanadi),
  `api_exception.dart` (server `{error}`, `Retry-After`), `features/auth/` — `TokenStorage` (flutter_secure_storage, `tokenStorageProvider` testda almashtiriladi),
  `AuthRepository` (interceptorsiz Dio), `AuthController` (unknown/signedOut/guest/signedIn), `LoginScreen` (+998 formatlash, kod kataklari — uzunlik serverdan,
  SMS avtoto'ldirish, qayta yuborish taymeri, Telegram → "SMS orqali yuborish", raqamni o'zgartirish, 429 da taymer), `SplashScreen`.
  Router: `refreshListenable` + redirect (splash/login/ichkari; mehmon katalogga kiradi). Test yangilandi (xotiradagi TokenStorage).
  Tekshiruv: `flutter analyze` 0 muammo. **Runtime sinalmagan.**
  Keyinga: ism so'rash ekrani (`profileCompleted=false`) — 4.6 da; foydalanish shartlari/maxfiylik havolalari — 5.1.
- 2026-11-12 — 4.3–4.6 (parallel agentlar, lead integratsiya qildi) —
  4.3 `features/catalog/`: modellar (ish vaqti serverdagi kabi, tungi smena), `CatalogRepository`, do'kon tanlash (`storeSelectionProvider`,
  branchId secure storage'da; savatchada boshqa do'kon tovari bo'lsa tozalashni so'raydi), `HomeScreen` (qidiruv 400 ms, bepul yetkazish banneri,
  kategoriya chiplari, tartiblash, 2 ustunli cheksiz setka, pastki "Savatcha" paneli), `StoresScreen`, `ProductScreen` (eski narx, −%, qoldiq, stepper).
  4.4 `features/cart/` (`cartControllerProvider`: tarozi 0.1 kg, dona butun; `cartQuoteProvider` — o'zgarishda 400 ms debounce bilan `/quote`, eski so'rov bekor),
  `CartScreen` (summalar faqat serverdan, qator muammolari, minimal summa, mehmon → kirish), `features/checkout/` (yetkazish/olib ketish sozlamaga qarab,
  manzil tanlash + yangi manzil `/address-new` — root navigator, tab almashmaydi; "imkon qadar tez" yoki bugun/ertaga 1 soatlik oraliq ish vaqtida,
  UTC+5→UTC; ruxsat etilgan to'lov usullari; tugasa siyosati; izoh; kontaktsiz → POST → `/orders/<id>`).
  4.5 `features/orders/`: ro'yxat (Faol/Tarix, sahifalash), kuzatish (5 bo'lak progress, timeline, sabab kartasi, "Tarkibi", bekor qilish faqat `canCancel`,
  `tel:` qo'ng'iroq), o'tkazma bo'limi (karta nusxa, `image_picker` bilan chek ≤5 MB), SignalR `signalr_netcore` (`orderUpdatesProvider` — HomeShell'da
  doim kuzatiladi) + 30 s polling zaxira. Paketlar: url_launcher, image_picker, signalr_netcore.
  4.6 `features/profile/`: profil (ism banneri, til — keyinga, chiqish, akkauntni o'chirish "O'CHIRISH" so'zi bilan), shaxsiy ma'lumotlar, manzillar CRUD
  (`ShopAddress`, `addressesProvider`; Uy/Ish/Boshqa).
  Lead: `router.dart` barcha marshrutlar, `placeholder_screen.dart` o'chirildi, Android `INTERNET` ruxsati (release'da yo'q edi) va `tel:` `<queries>`,
  iOS `NSPhotoLibraryUsageDescription`, test yangilandi. Tekshiruv: `flutter analyze` 0 muammo, `flutter test` 1/1. **Runtime sinalmagan.**
  Keyinga: geolokatsiya (masofa), savatchani qurilmada saqlash, promokod/bonus/Payme/Click, idempotency key, "60–90 daqiqa", mahsulot karuseli/brend/jadval,
  sevimlilar, "Qayta buyurtma" (savatchaga ulash), fiskal chek QR, ETA, kuryer ma'lumoti, chekni kamera bilan olish, xarita (manzil), til tanlash,
  avatar, telefonni almashtirish, ism kiritishga majburiy yo'naltirish.
- 2026-11-12 — 4.7 (backend qismi) — `ShopDeviceToken` + migratsiya `AddShopDeviceTokens`; `POST /api/me/devices` {token, platform, locale} (upsert, 10 tagacha),
  `DELETE /api/me/devices/{token}`, logout'da ixtiyoriy `deviceToken`, akkaunt o'chirilganda tokenlar o'chadi, 90 kun ko'rinmagan token tozalanadi.
  `IOnlineOrderNotifier.OrderChangedAsync(..., pushToCustomer)` — faqat do'kon holatni haqiqatan o'zgartirganda; bounded `OrderPushQueue` + `OrderPushService`
  (BackgroundService) → `ShopPushService` (uz/ru matnlar) → `FcmPushSender` (HTTP v1, RS256 JWT qo'lda; sozlanmasa `LogPushSender`); yaroqsiz token o'chiriladi.
  Config: `Push:Fcm:ProjectId`, `Push__Fcm__ServiceAccountJson|ServiceAccountPath`, `AndroidChannelId=orders`.
  Tekshiruv: `dotnet build` 0 xato, `has-pending-model-changes` — o'zgarish yo'q. **Runtime sinalmagan.**
  Qoldi (4.7 mobil): Firebase loyihasi, `google-services.json`/`GoogleService-Info.plist`, APNs kaliti; ilovada `firebase_messaging`, kirgach/token yangilanganda
  `POST /api/me/devices`, chiqishda `deviceToken`, Android kanal "orders". Keyingisi: 4.7 mobil (Firebase kerak) yoki 5.1.
- 2026-11-12 — 4.7 mobil / 5.1 / 5.2 / 5.4 / "keyinga" qismlari (parallel agentlar, lead yakuniy tekshiruv) —
  Backend: `OnlineOrdersExtras` migratsiyasi — `OnlineOrder.CourierName/CourierPhone/PickingStartedAt/ClientRequestId` (unik (ShopUserId, ClientRequestId)),
  `OnlineStoreSettings.DeliveryMinMinutes/DeliveryMaxMinutes/AutoCancelMinutes` (standart 30, 0 — o'chiq); buyurtma idempotentligi; `ShopOrderDto` + kuryer, chek raqami,
  `fiscalQrUrl`, `estimatedDeliveryFrom/To`; `ShopStoreDto` + yetkazish daqiqalari; xodim `dispatch {courierName, courierPhone}`, yangi `POST cancel` (Ready..OnTheWay,
  `sales.refund`, `SaleService.RefundAsync`, ochiq smena kerak, Paid→Refunded), `GET ?status=&to=` (to — shu kun ham kiradi); `OnlineOrderAutoCancelService` (har daqiqa,
  shartli UPDATE — xodim qabul qilgan bo'lsa ustun); `PUT /api/products/show-online`; `GET /api/app/config` (`MobileApp` sozlamasi: min/latest build, storeUrl, maintenance).
  Frontend: `public/privacy.html`, `terms.html` (uz/ru; TODO: support email, rekvizitlar yurist bilan), `OnlineOrdersView` (`/online-orders`, faqat ko'rish), mahsulotlarda
  ommaviy "Ilovada ko'rsatish/yashirish" va filtr, `OnlineStoreView` + yetkazish vaqti va avtomatik bekor qilish. Lead: sana oralig'i `to+1` xatosi tuzatildi.
  Desktop: kuryer so'rovi (sessiyada eslab qoladi), "Bekor qilish (qaytarish)", "Yangi" bor ekan ovoz har 30 s, kartada telefon yashirilgan.
  Mobil: push (`firebase_core/messaging`, `flutter_local_notifications`, kanal "orders"; Firebase config bo'lmasa jim o'chadi; `POST /api/me/devices`, chiqishda deviceToken,
  bosilganda `/orders/<id>`; google-services plagini faqat json bo'lsa qo'llanadi; `docs/PUSH.md`), til tanlash (saqlanadi, `Accept-Language`), ism so'rash (kirgach bir marta,
  "Keyinroq"), geolokatsiya (do'konlar masofa bo'yicha), savatcha qurilmada saqlanadi, "Qayta buyurtma", `clientRequestId`, kuzatishda kuryer/ETA/chek QR/qaytarish matni,
  chek uchun kamera, manzil xaritada (flutter_map + OSM, kalitsiz), yetkazish vaqti matni; ikonka/splash (`assets/branding`), maxfiylik/shartlar havolalari (login, profil),
  Android imzolash (`key.properties` bo'lsa release, aks holda debug; `docs/RELEASE.md`), majburiy/yumshoq yangilash va texnik ishlar ekrani, Sentry (faqat `SENTRY_DSN` bilan).
  Tekshiruv: backend `dotnet build` 0 xato + pending model o'zgarish yo'q; desktop 0 xato; `npm run build` OK; `flutter analyze` 0; `flutter test` 1/1.
  **Runtime sinalmagan; Android SDK yo'qligi sababli APK/Gradle yig'ilmagan.**
  Egasidan: Firebase (google-services.json, GoogleService-Info.plist, APNs .p8, server `Push__Fcm__ServiceAccountJson`), upload keystore + key.properties, Play Console,
  Sentry DSN, `dotnet ef database update` (AddShopDeviceTokens, OnlineOrdersExtras), support email va huquqiy rekvizitlar, Android SDK'da bir marta build.
  Qolgan keyinga: yetkazish narxi fiskal chekda (MXIK xizmat), kuryer naqdini smenaga alohida kirim, markirovka online yig'ishda, mahsulot karuseli/brend,
  promokod/bonus/Payme/Click, reverse geocoding va yetkazish hududi, sevimlilar, avatar, telefonni almashtirish, Redis backplane, 5.3 iOS.
- 2026-11-12 — Android emulyatorda birinchi ishga tushirish. Android Studio + emulyator `Medium_Phone_API_37.0` (cold boot ~100 s).
  Build tuzatishlari: `android/gradle.properties` → `kotlin.incremental=false` (loyiha D:, pub kesh C: — Kotlin kesh xatosi);
  `app/build.gradle.kts` → `compileSdk = maxOf(flutter, 37)` (flutter_local_notifications, androidx.core 1.19),
  `minSdk = maxOf(flutter, 26)` (sentry_flutter → Android 8.0+). Debug APK yig'ildi va o'rnatildi; kirish ekrani ochildi.
  Kuzatilgan: backend o'chiq bo'lsa splash ~15–20 s turadi (so'rov timeout'i — qisqartirish kerak); telefon maydonida
  "+998" raqamdan yuqoriroq turibdi (vertikal tekislash); Firebase sozlanmagan — push o'chiq (google-services.json kerak).

## Bosqich 6 — Kuryer, to'lovlar, bonus, promokod, markirovka, hudud (kelishuv: `docs/mobile/CONTRACT-6.md`)

- [x] 6.1 Kuryer akkaunti (`UserRole.Courier`), `/api/courier`, kuryer rejimi mijoz ilovasi ichida, Navigator, marshrut.
- [x] 6.2 Kuryer naqdi (Tayyor'da Expense, topshirganda Income), yetkazish xizmati fiskal chekda (`ProductType.Service`).
- [x] 6.3 Markirovka (DataMatrix) online yig'ishda.
- [x] 6.4 Promokod, bonus bilan to'lash, yetkazish radiusi, Payme/Click (yig'ilgach), xaritada joylashuv + manzil aniqlash.

- 2026-11-12 — 6.1–6.4 (5 parallel agent, lead kelishuv yozdi va yakuniy tekshirdi) —
  Backend: migratsiya `OnlineShopPhase6`; `UserRole.Courier=5` (`POST /api/users` + `fullName`, `phone`; kuryerga telefon+filial majburiy, litsenziya/qurilma bog'lash yo'q,
  JWT 24 soat, refresh yo'q — 401 da qayta login); `CourierController` (`me`, `orders?history=`, `orders/{id}`, `delivered {cashCollected}`, `failed {reason}` → Ready'ga qaytadi);
  SignalR `courier:{userId}` (`CourierOrderAssigned`); xodim `GET couriers`, `dispatch {courierUserId}`, markirovka (`markingCodes` to'liq ro'yxat, takror rad, kerakli soni
  Ceiling(miqdor), Tayyor'da har kod alohida qator), `cash-handover` (faqat Delivered + ochiq smena), `courier-cash`; Tayyor'da promokod qayta hisoblanadi, bonus joriy ball bilan,
  chegirma savdo qatorlariga mutanosib taqsimlanadi; "Yetkazib berish xizmati" (`ProductType.Service`, MXIK sozlamadan, POS/katalog/hisobotlardan yashirilgan);
  mijoz quote/create: `promoCode`, `useBonus`, `addressId` (quote tokenli bo'lsa bonus/limit/hudud), `deliveryRadiusKm`, `onlinePaymentProvider`; `POST /api/shop/orders/{id}/pay`
  (mavjud OnlinePaymentService, SaleId bo'yicha) → to'lov tasdiqlansa Paid + AwaitingTransfer'dan chiqadi; `GET /api/me/bonus`; `PromoCode` + `/api/promo-codes` CRUD.
  Kelishuvdan farqlar: `canPayOnline` faqat Transfer'da; Tayyor'dan keyin bekor qilinsa bonus qaytariladi va topshirilmagan kuryer naqdi uchun tuzatuvchi Income;
  promokod MaxUses parallel so'rovlarda 1–2 ga oshishi mumkin; yetkazish xizmati IsVat=true.
  Desktop: kuryer tanlash oynasi (+ qo'lda), DataMatrix skanerlash (GTIN bo'yicha qatorga biriktirish, "Markirovka kodi" tugmasi, kodlar to'liq bo'lmasa Tayyor bloklanadi),
  "Kuryer pullari (N) · summa" oynasi, promokod/bonus tafsilotda, Courier roli kassaga kira olmaydi.
  Admin panel: "+ Kuryer" (UsersView), SuperAdmin desktop login formasida Kuryer, `PromoCodesView` (`/promo-codes`), OnlineStoreView: radius, yetkazish MXIK, bonus %; SA: Payme/Click;
  OnlineOrdersView: kuryer naqdi, promokod, bonus, markirovka.
  Mobil: kuryer rejimi (`features/courier/`: alohida sessiya, "Kuryer sifatida kirish", faol/bugun, masofa bo'yicha tartib, Yandex Navigator / Google Maps, "Marshrut (hammasi)"
  9 nuqtagacha, qo'ng'iroq, Yetkazildi (naqd summa) / Yetkazib bo'lmadi (sabab), SignalR + 30 s polling); mijoz: promokod (savatcha), bonus kaliti, hudud xabari va masofa,
  Payme/Click tanlovi va kuzatishda to'lov tugmalari (ilovaga qaytganda yangilanadi), xaritada "Joylashuvimni aniqlash", Nominatim reverse/qidiruv (User-Agent, ≥1.1 s).
  Tekshiruv: backend 0 xato + pending model yo'q; desktop 0 xato; `npm run build` OK; `flutter analyze` 0; `flutter test` 1/1. **Runtime sinalmagan.**
  Egasidan: Payme/Click merchant sozlamalari (mavjud to'lov konfiguratsiyasi), `dotnet ef database update` (OnlineShopPhase6).

## Bosqich 7 — Cashback va bonus karta (kelishuv: `docs/mobile/CONTRACT-7.md`)

- [x] 7.1 Cashback sozlamalari (foiz, kategoriya foizi, minimal summa, maks. ball bilan to'lash, muddat, ro'yxatdan o'tish bonusi), yagona `LoyaltyRules`.
- [x] 7.2 Dinamik QR bonus karta (90 s, HMAC), kassada skanerlash, ball bilan to'lash, chekda ball.
- [x] 7.3 Ilovada "Bonus" tabi (pastki menyu, o'rtada), balans va tarix; admin panelda sozlamalar, hisobot, mijoz tarixi.
- [x] 7.4 Online buyurtma cashback'i faqat "Yetkazildi"da (xodim yoki kuryer), push "+N ball cashback".

- 2026-11-13 — 7.1–7.4 — Backend: migratsiya `LoyaltyCashback` (`LoyaltySettings`, `LoyaltyCategoryRates`, `LoyaltyTransaction.Type/Source`, `Customer.LastActivityAt`,
  `Sale.LoyaltyPointsRedeemed`, eski yozuvlar backfill), `LoyaltyRules` (SaleService create/pay/refund, online, adjust), `LoyaltyExpiryService` (kunlik, faolsizlik bo'yicha),
  `LoyaltyQrSigner` (`Loyalty:QrSigningKey`, bo'sh bo'lsa JWT kalitidan), `ShopCustomerLinker`, `/api/loyalty/{settings,resolve,report}`, `/api/me/{bonus-card,bonus/stores,bonus/history}`,
  `/api/customers/{id}/loyalty-history`, `CreateSaleRequest.RedeemPoints` (oflayn kech kelsa qisqartiriladi, rad etilmaydi); online Earn faqat Delivered'da
  (`OnlineOrderCashback`, idempotent), `ShopOrderDto.cashbackEarned`. Farqlar: online ball bilan to'lash chegarasi `OnlineStoreSettings.BonusMaxPercent`; `enabled=false` faqat yozishni to'xtatadi.
  Desktop: "PCB1." skanerlash → mijoz chipi (har savdo tabida), "Ball bilan to'lash" (Hammasi), chekda ball qatorlari (FlowDocument + ESC/POS), oflayn navbatda ham.
  Admin panel: `LoyaltyView` (`/loyalty`), mijozlarda 📱 belgisi va "Bonus tarixi". Mobil: `features/bonus/` (QR avto-yangilanish, yorqinlik, tarix), pastki menyuda "Bonus" tabi,
  QR kesh + oldindan yuklash (tez ochilishi uchun).
  Tekshiruv: hammasi build/analyze/test toza. **Runtime sinalmagan.** Egasidan: `dotnet ef database update` (LoyaltyCashback).

## Bosqich 8 — Har do'konga alohida ilova (white-label) (kelishuv: `docs/mobile/CONTRACT-8.md`)

- [x] 8.1 Backend: do'kon ilova sozlamalari (slug, nom, paket ID, rang, build/store), `GET /api/shop/store-app/{slug}`, do'konga xos `app/config`.
- [x] 8.2 Mobil: `STORE_SLUG` do'kon rejimi (faqat shu do'kon filiallari, brend rangi/logo/nom, kesh), `tool/store.ps1`, `stores/<slug>/`, `docs/WHITE_LABEL.md`.
- [x] 8.3 Admin panel: SuperAdmin "Mobil ilova" bo'limi (ikonka ko'rinishi, build buyrug'i).

- 2026-11-13 — 8.1–8.3 — Qaror (foydalanuvchi): har do'konga alohida ilova (do'konlar mijozni boshqa do'konga ketib qolishidan xavotirda).
  Backend: migratsiya `StoreWhiteLabel` (12 maydon, Slug/AppPackageId unik), `PUT /api/admin/online-stores/{orgId}/app`, `store-app/{slug}` (404 — "Do'kon vaqtincha ishlamayapti"),
  `stores?organizationId=`, `app/config?store=` (har qiymat alohida almashadi).
  Mobil: `AppConfig.storeSlug/isStoreMode` (+ BRAND_COLOR, APP_NAME dart-define), `core/store_app/` (kesh, avtomatik filial, unavailable ekran), brend rangi (kontrast ≥4.5),
  Android `store.properties` → applicationId + `${appLabel}`, iOS `Store.xcconfig` → `$(STORE_BUNDLE_ID)`, `$(APP_DISPLAY_NAME)`; `tool/store.ps1` (example/default sinaldi, default asl holatga qaytaradi);
  `stores/example/`. Admin panel: "Mobil ilova" bo'limi (slug/paket taklifi, rang, Android/iOS build va havolalar, ikonka preview, buyruq nusxasi).
  Tekshiruv: backend 0 xato + pending yo'q; desktop 0; `npm run build` OK; `flutter analyze` 0; `flutter test` 2/2. **Runtime sinalmagan, Android/iOS build qilinmagan.**
  Egasidan har yangi do'kon uchun: admin panel maydonlari, `stores/<slug>/` (store.json, icon 1024), Firebase'ga Android/iOS app qo'shish (google-services.json yangilash,
  GoogleService-Info.plist), `tool\store.ps1 -Slug <slug> -Build appbundle`, Play'da noyob listing; iOS — do'konning o'z Apple akkaunti. Commitdan oldin `-Slug default`.
  Bir martalik: Android SDK'da oddiy va do'kon buildini tekshirish; `dotnet ef database update` (StoreWhiteLabel).
- 2026-11-13 — 4.7 Firebase — loyiha `xujamarket`, `mobile/android/app/google-services.json` qo'yildi; `flutter build apk --debug` OK, google-services plagini qo'llandi.
  Backend ProjectId service account'dan olinadi. Qoldi: serverda `Push__Fcm__ServiceAccountPath`, qurilmada push sinovi; iOS (plist + APNs) keyin.
- 2026-11-13 — Xo‘ja Market brendi — standart build bitta do'kon rejimida (`STORE_SLUG` standarti `xoja-market`, `APP_NAME`, `BRAND_COLOR=#D81F26`), ilova nomi "Xo‘ja Market" (arb, gradle, Store.xcconfig),
  ranglar (qizil + to'q ko'k matn), ikonka/splash/`logo.png` logodan; `StoreLogo` endi ichki asset. Paket ID o'zgarmadi. `flutter analyze` 0, emulyatorda ko'rildi.
  Qoldi: admin panelda do'konga slug `xoja-market` berish, backend ishga tushirish.
