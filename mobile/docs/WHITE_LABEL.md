# Har do'konga alohida ilova (white-label)

Kelishuv: `docs/mobile/CONTRACT-8.md`.

> **2026-11-13:** standart build endi Xo‘ja Market (`STORE_SLUG` standarti `xoja-market`, qizil `#D81F26`, `assets/branding`). Marketplace — `--dart-define=STORE_SLUG=`. Kod bazasi bitta (`mobile/`); har do'kon o'z nomi, ikonkasi, rangi va paket ID'si bilan
alohida ilova sifatida chiqariladi. `STORE_SLUG` berilmagan build — marketplace rejimi ("PosCloud Market", barcha do'konlar).

## Ilova do'kon rejimida qanday ishlaydi

- `--dart-define=STORE_SLUG=<slug>` → ochilishda `GET /api/shop/store-app/<slug>` olinadi va `shared_preferences`da keshlanadi
  (keyingi ochilish internetsiz ham keshdan; javob fonda yangilanadi).
- Filial bitta bo'lsa — avtomatik tanlanadi (do'kon almashtirish tugmasi yo'q); bir nechta bo'lsa — faqat shu do'kon filiallari
  (`GET /api/shop/stores?organizationId=`). Marketplace do'konlar ro'yxati ko'rsatilmaydi.
- Server 404 (do'kon o'chirilgan/bloklangan) yoki ilovada ko'rinadigan filial yo'q → "Do'kon vaqtincha ishlamayapti" ekrani
  ("Qayta urinish"; kuryer kirishi ochiq qoladi).
- Tema asosiy rangi — `brandColor` (juda och rang oq matn o'qilishi uchun avtomatik to'qlashtiriladi); build paytida `BRAND_COLOR`
  ham beriladi — birinchi ochilishda server javobi kelguncha shu rang.
- Splash va kirish ekranida do'kon logosi va `appName`; bonus kartada faqat shu do'kon.
- `GET /api/app/config` ga `store=<slug>` qo'shiladi — do'konning o'z versiya qiymatlari (majburiy yangilash, store havolasi).

## Yangi do'kon qo'shish

### 1. SuperAdmin (admin panel → Online do'konlar → do'kon → "Mobil ilova")

| Maydon | Qoida | Misol |
|---|---|---|
| Slug | `^[a-z0-9-]{3,40}$`, unik | `baraka-market` |
| Ilova nomi | ≤ 30 belgi (telefon ekranida ko'rinadi) | `Baraka Market` |
| Paket ID | `uz.poscloud.store.baraka` ko'rinishida, unik. **Play/App Store'ga chiqqandan keyin o'zgarmaydi** | |
| Brend rangi | `#RRGGBB` | `#1D4ED8` |
| Android/iOS build raqamlari, store havolalari | Majburiy/yumshoq yangilash uchun (har relizdan keyin yangilang) | |

Do'kon logosi (online do'kon sozlamasidagi) ilovada splash/kirish ekranida ko'rinadi.

### 2. Fayllar: `mobile/stores/<slug>/` (repoga KIRADI)

| Fayl | Majburiy | Izoh |
|---|---|---|
| `store.json` | ha | pastda |
| `icon.png` | ha | 1024×1024 PNG, iOS uchun shaffof fon bo'lmasin (alfa olib tashlanadi) |
| `icon_foreground.png` | tavsiya | Android adaptiv ikonka old qatlami (1024×1024, logo markazdagi ~66% doirada). Bo'lmasa `icon.png` ishlatiladi — chetlari kesilishi mumkin |
| `splash.png` | yo'q | Splash rasmi (masalan 288×288, shaffof). Bo'lmasa `icon.png` |
| `google-services.json` | yo'q | Faqat umumiy fayl o'rniga alohida kerak bo'lsa (pastda, Firebase) |
| `GoogleService-Info.plist` | iOS uchun ha | Shu do'kon iOS ilovasining Firebase fayli |

```json
{
  "slug": "baraka-market",
  "appName": "Baraka Market",
  "androidPackageId": "uz.poscloud.store.baraka",
  "iosBundleId": "uz.poscloud.store.baraka",
  "brandColor": "#1D4ED8",
  "splashColor": "#1D4ED8",
  "versionName": "1.0.0",
  "buildNumber": 1
}
```

`slug` papka nomi bilan bir xil bo'lishi va admin paneldagi slug bilan mos kelishi shart. Namuna: `stores/example/`.

### 3. Skript: `tool/store.ps1` (Windows PowerShell 5.1 yoki pwsh)

```powershell
cd mobile
# Faqat sozlash (ikonka, nom, paket ID) — build'siz:
powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug baraka-market -Build none
# Play uchun AAB:
powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug baraka-market -Build appbundle
# APK (sinov uchun), boshqa server:
powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug baraka-market -Build apk -ApiBaseUrl https://test.poscloud.uz
# iOS (faqat macOS, pwsh):
pwsh tool/store.ps1 -Slug baraka-market -Build ios
# Asl holatga (PosCloud Market) qaytarish — har do'kon buildidan keyin:
powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug default -Build none
```

Parametrlar: `-Slug`, `-Build none|apk|appbundle|ios` (standart `none`), `-ApiBaseUrl` (standart `https://poscloud.uz`),
`-SentryDsn` (ixtiyoriy), `-ExtraArgs` (flutter build'ga qo'shimcha).

Skript nima qiladi:
1. `store.json` va rasmlarni tekshiradi (slug, paket ID, rang, versiya formati, `icon.png` 1024×1024).
2. `android/store.properties` (`applicationId`, `appLabel`) yozadi — `android/app/build.gradle.kts` o'qiydi
   (`applicationId`, `manifestPlaceholders["appLabel"]` → `AndroidManifest.xml` `android:label="${appLabel}"`). Fayl repoga kirmaydi.
3. `ios/Flutter/Store.xcconfig` (`STORE_BUNDLE_ID`, `APP_DISPLAY_NAME`) yozadi. Runner target'da
   `PRODUCT_BUNDLE_IDENTIFIER = $(STORE_BUNDLE_ID)`, Info.plist'da `CFBundleDisplayName = $(APP_DISPLAY_NAME)`.
   Repodagi standart qiymatlar: `uz.poscloud.poscloudMarket` / `PosCloud Market`.
4. Firebase fayllarini joylaydi (pastda).
5. Vaqtinchalik yaml bilan ikonka (`dart run flutter_launcher_icons -f ...`) va splash
   (`dart run flutter_native_splash:create --path=...`) generatsiya qiladi; yaml o'chiriladi.
   (Yaml nomi ataylab `flutter_launcher_icons-<slug>.yaml` emas — bu nomni paket flavor deb tushunadi.)
6. `-Build` berilsa: `flutter build <apk|appbundle|ipa> --release --build-name=<versionName> --build-number=<buildNumber>
   --dart-define=STORE_SLUG=.. --dart-define=API_BASE_URL=.. --dart-define=BRAND_COLOR=.. --dart-define=APP_NAME=..`.

`-Slug default`: `android/store.properties` o'chiriladi, `Store.xcconfig` standartga qaytadi, Firebase fayllari asl holatiga
qaytadi, ikonka/splash `assets/branding` dan (pubspec.yaml sozlamalari) qayta generatsiya qilinadi.

> **Muhim:** do'kon buildidan keyin generatsiya qilingan ikonka/splash fayllari (`android/app/src/main/res`, `ios/Runner/Assets.xcassets`)
> o'zgargan bo'ladi — commit qilishdan oldin `-Slug default` ni ishga tushiring (aks holda repoga do'kon ikonkasi tushadi).
> Imzo: barcha do'kon ilovalari bitta `android/key.properties` upload kaliti bilan imzolanishi mumkin (Play App Signing har ilova uchun
> alohida ilova kalitini saqlaydi). Batafsil: `docs/RELEASE.md`.

### 4. Firebase (push)

Bitta Firebase loyihasi; server o'zgarmaydi (`Push:Fcm:*` — bitta service account hamma ilovalarga yuboradi).

- **Android:** Firebase Console → Project settings → *Add app* → Android, package name = `androidPackageId`.
  So'ng `google-services.json` ni **qayta yuklab oling** — u loyihadagi **barcha** Android ilovalarni o'z ichiga oladi —
  va `android/app/google-services.json` ga qo'ying (eskisini almashtiring). Skript fayl ichida paket ID yo'qligini ko'rsa ogohlantiradi:
  google-services plagini mos `package_name` topmasa build xato beradi.
  Alohida fayl kerak bo'lsa (boshqa loyiha) — `stores/<slug>/google-services.json`: skript uni `android/app/` ga ko'chiradi,
  asl faylni `google-services.json.default` ga saqlaydi, `-Slug default` qaytaradi.
- **iOS:** *Add app* → iOS, Bundle ID = `iosBundleId`; `GoogleService-Info.plist` har ilovaga alohida — `stores/<slug>/GoogleService-Info.plist`
  ga qo'ying, skript uni `ios/Runner/` ga ko'chiradi. **Birinchi marta** faylni Xcode orqali Runner target'ga qo'shing
  (Add Files → target: Runner) — keyin skript faqat mazmunini almashtiradi. APNs `.p8` kaliti — do'kon Apple akkauntidagi kalit,
  Firebase'da shu iOS ilovaga yuklanadi (`docs/PUSH.md`).

### 5. Versiyalash (har do'kon alohida)

- Har do'konning `versionName`/`buildNumber` i `stores/<slug>/store.json` da; har yuklashda `buildNumber` ni oshiring
  (Play/App Store bir xil raqamni qabul qilmaydi). `pubspec.yaml` versiyasi faqat marketplace ilovasi uchun.
- Relizdan keyin admin panelda shu do'konning Android/iOS `latest build`, `latest version`, store havolasini yangilang;
  eski versiyalarni majburan yangilatish — `min build`.

## Play Market qoidalari

- Barcha do'kon ilovalarini **bitta developer akkauntidan** chiqarish mumkin (yoki do'konning o'z akkauntidan).
- **Repetitive content / spam** siyosati: bir xil ko'rinishdagi ko'p ilovalar rad etilishi mumkin. Har ilova uchun **noyob**
  nom, ikonka, tavsif (do'kon haqida, filiallar, yetkazish hududi), **o'z skrinshotlari** (shu do'kon mahsulotlari bilan) va
  feature graphic tayyorlang. Tavsifda ilova aynan shu do'konniki ekanini yozing.
- Har ilova uchun: maxfiylik siyosati URL, Data safety, kontent reytingi, test uchun kirish ma'lumotlari (`docs/RELEASE.md` §5).
- Paket ID nashrdan keyin o'zgarmaydi.

## App Store qoidalari (Guideline 4.2.6)

- Apple shablon asosidagi (white-label) ilovalarni **ilova egasi (do'kon)ning o'z developer akkauntidan** chiqarishni talab qiladi;
  bitta akkauntdan ko'p do'kon ilovasi 4.2.6 bo'yicha rad etiladi.
- Shuning uchun: do'kon Apple Developer Program'ga (tashkilot sifatida, D-U-N-S bilan) a'zo bo'ladi va bizni App Store Connect'ga
  (Admin/App Manager) taklif qiladi yoki Team ID ni beradi. Build shu **Team ID** bilan imzolanadi (Xcode → Signing → Team,
  yoki Codemagic'da do'kon akkaunti API kaliti) va do'kon akkauntiga yuklanadi.
- Bundle ID (`iosBundleId`) do'kon akkauntida ro'yxatdan o'tkaziladi; APNs kaliti ham shu akkauntda.

## Har yangi do'kon uchun qisqa ro'yxat

1. Admin panel: slug, ilova nomi, paket ID, rang, logo.
2. `stores/<slug>/store.json`, `icon.png`, (`icon_foreground.png`, `splash.png`).
3. Firebase: Android ilova qo'shish → `google-services.json` ni qayta yuklab `android/app/` ga; iOS ilova → `stores/<slug>/GoogleService-Info.plist`.
4. `tool\store.ps1 -Slug <slug> -Build appbundle` → Play Console'da yangi ilova (noyob listing, skrinshotlar) → internal testing.
5. iOS: do'konning Apple akkaunti/Team ID → macOS'da `pwsh tool/store.ps1 -Slug <slug> -Build ios` → TestFlight.
6. `tool\store.ps1 -Slug default` (repo toza holatga), admin panelda build raqamlari va store havolalari.
