# PosCloud Market — reliz qo'llanmasi

> Har do'konga alohida ilova (white-label): `docs/WHITE_LABEL.md` (`tool/store.ps1`).

Ilova ID: `uz.xojamarket.app` (Android `applicationId` va iOS bundle id).

> **DIQQAT:** Play Market'ga birinchi marta yuklangandan keyin ilova ID'sini **o'zgartirib bo'lmaydi**.
> O'zgartirilsa, bu Play uchun butunlay boshqa ilova bo'ladi (reyting, foydalanuvchilar, yangilanishlar yo'qoladi).

## 1. Android: upload kalitini yaratish (bir marta)

Kalit va parollar **hech qachon repoga qo'yilmaydi** (`key.properties`, `*.jks`, `*.keystore` — `.gitignore`da).

```bash
keytool -genkey -v -keystore upload-keystore.jks \
  -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

- `keytool` JDK bilan keladi (Android Studio: `<Android Studio>/jbr/bin/keytool`).
- Faylni `mobile/android/upload-keystore.jks` ga qo'ying (yoki repodan tashqaridagi xavfsiz joyga).
- Kalit faylini va parollarni parol menejerida / xavfsiz zaxirada saqlang.
  Play App Signing yoqilgan bo'lsa, upload kaliti yo'qolsa Play Console orqali tiklash mumkin, lekin bu vaqt oladi.

## 2. `key.properties`

`mobile/android/key.properties.example` dan nusxa oling:

```bash
cp android/key.properties.example android/key.properties
```

```properties
storeFile=../upload-keystore.jks   # android/app papkasiga nisbatan yoki to'liq yo'l
storePassword=...
keyAlias=upload
keyPassword=...
```

`android/app/build.gradle.kts` bu faylni avtomatik o'qiydi. Fayl **bo'lmasa** release build debug kalit bilan
imzolanadi (dev/CI ishlashi uchun) — bunday AAB'ni Play Console **qabul qilmaydi**.

CI'da: keystore'ni base64 qilib maxfiy o'zgaruvchida saqlang, build oldidan faylga yozib `key.properties` yarating.

## 3. Versiya

`pubspec.yaml`:

```yaml
version: 1.2.0+5
```

- `1.2.0` — versionName (foydalanuvchiga ko'rinadi).
- `5` — build raqami (Android `versionCode`, iOS `CFBundleVersion`). **Har bir yuklashda oshirilishi shart**,
  Play bir xil raqamni qayta qabul qilmaydi.
- Majburiy yangilash (`/api/app/config`) aynan **build raqami** bilan solishtiradi (`minSupportedBuild`, `latestBuild`).

## 4. Build

```bash
cd mobile
flutter build appbundle --release \
  --dart-define=API_BASE_URL=https://poscloud.uz \
  --dart-define=SENTRY_DSN=https://<kalit>@<org>.ingest.sentry.io/<loyiha> \
  --dart-define=APP_ENV=production
```

Natija: `build/app/outputs/bundle/release/app-release.aab`.

- `API_BASE_URL` berilmasa ilova emulyator manziliga (`10.0.2.2`) ulanadi — release'da **albatta bering**.
- `SENTRY_DSN` ixtiyoriy: bo'sh bo'lsa Sentry o'chiq. `APP_ENV` berilmasa release'da `production`.
- Sentry'ga shaxsiy ma'lumot yuborilmaydi (`sendDefaultPii = false`).

## 5. Play Console: internal testing

1. Play Console → **Create app**: nomi "PosCloud Market", til, ilova/bepul, deklaratsiyalar.
2. **App integrity → Play App Signing** — Google boshqaradigan imzo (tavsiya etiladi); biz faqat upload kalit bilan imzolaymiz.
3. **Testing → Internal testing → Create new release** → `app-release.aab` yuklang, reliz izohini yozing.
4. **Testers** — testerlar email ro'yxatini qo'shing, taklif havolasini ularga yuboring.
5. Majburiy bo'limlarni to'ldiring: maxfiylik siyosati URL, Data safety, kontent reytingi, maqsadli auditoriya,
   ilova kirish ma'lumotlari (agar test uchun login kerak bo'lsa — test telefon raqami/kodi).
6. Do'kon sahifasi: ikonka 512×512, feature graphic 1024×500, skrinshotlar.
7. Internal'dan keyin → Closed/Open testing → Production.

Yangi versiyadan keyin backend'da `/api/app/config` sozlamalarini yangilang: `latestBuild`, `latestVersion`,
kerak bo'lsa `minSupportedBuild` (eski versiyalarni majburan yangilatish uchun).

## 6. iOS (5.3, qisqacha)

iOS build faqat macOS'da (Xcode) yoki **Codemagic** kabi bulutli CI'da yig'iladi.
Kerak: Apple Developer hisobi ($99/yil), bundle id `uz.xojamarket.app`, sertifikat va provisioning profile
(Codemagic avtomatik boshqara oladi), App Store Connect'da ilova → **TestFlight** orqali test.
Build buyrug'i: `flutter build ipa --release --dart-define=...` (yuqoridagi bilan bir xil define'lar).
