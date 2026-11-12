# Bosqich 8 — Har do'konga alohida ilova (white-label)

Qaror (2026-11-12, foydalanuvchi): har do'kon o'z nomi, ikonkasi, rangi va paket ID'si bilan alohida ilova sifatida chiqariladi.
Kod bazasi bitta (`mobile/`). `STORE_SLUG` berilmagan build — ichki/test uchun marketplace rejimi (hozirgi).

## Backend
- `OnlineStoreSettings` yangi maydonlar (SuperAdmin boshqaradi): `Slug` (unik, `^[a-z0-9-]{3,40}$`), `AppName` (≤30), `AppPackageId`
  (`^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$`, unik), `BrandColor` (`#RRGGBB`), `AndroidMinBuild`, `AndroidLatestBuild`, `AndroidLatestVersion`,
  `AndroidStoreUrl`, `IosMinBuild`, `IosLatestBuild`, `IosLatestVersion`, `IosStoreUrl` (hammasi ixtiyoriy).
- `PUT /api/admin/online-stores/{organizationId}/app` (SuperAdmin) body — yuqoridagi maydonlar; `GET /api/admin/online-stores/{organizationId}` javobiga ham qo'shiladi.
- `GET /api/shop/store-app/{slug}` (anonim) → `{ organizationId, slug, appName, brandColor, logoUrl, description, phone, branches: ShopStoreDto[] }`
  (faqat ilovada ko'rinadigan filiallar; do'kon o'chirilgan/bloklangan bo'lsa 404 `{error}` — ilova "Do'kon vaqtincha ishlamayapti" ko'rsatadi).
- `GET /api/shop/stores?organizationId=` — ixtiyoriy filtr (shu tashkilot filiallari).
- `GET /api/app/config?platform=&version=&build=&store=<slug>` — `store` berilsa va do'konda qiymatlar bo'lsa, ular global `MobileApp` qiymatlari o'rniga ishlatiladi.
- Push: bitta Firebase loyihasi, har do'kon ilovasi shu loyihada alohida Android/iOS app sifatida ro'yxatdan o'tadi (server o'zgarmaydi).

## Mobil
- `--dart-define=STORE_SLUG=<slug>` → "do'kon rejimi": ilova ochilganda `store-app/{slug}` olinadi (keshlanadi); filial bitta bo'lsa avtomatik tanlanadi,
  bir nechta bo'lsa faqat shu do'kon filiallari tanlanadi; do'konlar ro'yxati, "boshqa do'kon" va marketplace elementlari ko'rinmaydi;
  tema asosiy rangi `brandColor`; sarlavha va "Bonus karta"da do'kon nomi/logosi; `app/config` ga `store` yuboriladi.
- Do'kon fayllari: `mobile/stores/<slug>/store.json`
  `{ "slug", "appName", "androidPackageId", "iosBundleId", "brandColor", "splashColor", "versionName", "buildNumber" }` + `icon.png` (1024),
  `icon_foreground.png` (ixtiyoriy), `splash.png` (ixtiyoriy; bo'lmasa icon'dan).
- Skript `mobile/tool/store.ps1 -Slug <slug> [-Build apk|appbundle|ios|none]`: store.json o'qiydi → Android `applicationId`/label (gradle `android/store.properties`
  orqali), iOS bundle id/display name (`ios/Flutter/Store.xcconfig`), ikonka va splash generatsiyasi (vaqtinchalik yaml), so'ng
  `flutter build ... --dart-define=STORE_SLUG=.. --dart-define=API_BASE_URL=.. --build-name/--build-number`. `-Slug default` — asl holatga qaytaradi.
- Hujjat: `mobile/docs/WHITE_LABEL.md` (yangi do'kon qo'shish, Firebase app qo'shish, Play/App Store qoidalari).

## Admin panel (SuperAdmin)
- `AdminOnlineStoresView` tafsilotida "Mobil ilova" bo'limi: slug, ilova nomi, paket ID, brend rangi (color picker), Android/iOS build raqamlari va store havolalari,
  do'kon logosi ko'rinishi, build buyrug'i namunasi (`.\tool\store.ps1 -Slug <slug> -Build appbundle`) nusxa olish tugmasi bilan.
