# Push bildirishnomalar (FCM) — sozlash

> Har do'konga alohida ilova (white-label): `docs/WHITE_LABEL.md` (`tool/store.ps1`).

Ilova kodi tayyor (`lib/core/push/push_service.dart`). Firebase fayllari qo'shilmaguncha push **jimgina o'chiq**:
`Firebase.initializeApp` xato beradi, ilova odatdagidek ishlaydi, Android build buzilmaydi.

## 1. Firebase loyihasi

1. https://console.firebase.google.com → loyiha yarating (yoki mavjudini oching).
2. **Android ilova** qo'shing: package name `uz.poscloud.poscloud_market`.
   Debug/release SHA-1 shart emas (faqat FCM uchun).
3. **iOS ilova** qo'shing: Bundle ID — Xcode'dagi `PRODUCT_BUNDLE_IDENTIFIER` (Runner target).

## 2. Fayllarni joylash

| Fayl | Joyi |
|---|---|
| `google-services.json` | `mobile/android/app/google-services.json` |
| `GoogleService-Info.plist` | `mobile/ios/Runner/GoogleService-Info.plist` — **Xcode orqali** Runner target'ga qo'shing (Add Files → "Copy items if needed", target: Runner), aks holda bundle'ga kirmaydi |

Android: `android/app/build.gradle.kts` `google-services.json` borligini tekshiradi va faqat shunda
`com.google.gms.google-services` plaginini qo'llaydi (versiya `android/settings.gradle.kts` da, `apply false`).
Agar Gradle plagin versiyasi AGP bilan mos kelmasa — `settings.gradle.kts` dagi `4.4.4` ni yangilang.

> Fayllarni repoga qo'shish ixtiyoriy (ular maxfiy kalit emas), lekin har muhit (dev/prod) uchun alohida loyiha bo'lsa — CI'da joylang.

## 3. iOS (APNs)

1. Apple Developer → Keys → **APNs Auth Key (.p8)** yarating; Key ID va Team ID ni yozib oling.
2. Firebase Console → Project settings → Cloud Messaging → Apple app configuration → .p8 kalitni yuklang.
3. Xcode → Runner → Signing & Capabilities:
   - **Push Notifications** capability qo'shing;
   - **Background Modes** → "Remote notifications" (Info.plist'da `UIBackgroundModes: remote-notification` allaqachon bor).
4. Simulyatorda APNs tokeni bo'lmasligi mumkin — haqiqiy qurilmada sinang.

## 4. Backend

`Push:Fcm:ProjectId` = Firebase project ID; service account JSON:
`Push__Fcm__ServiceAccountJson` yoki `Push__Fcm__ServiceAccountPath` (Firebase → Project settings → Service accounts →
"Generate new private key"). `Push:Fcm:AndroidChannelId` = `orders` (ilovadagi kanal bilan bir xil).
Sozlanmasa backend xabarlarni faqat logga yozadi (`LogPushSender`).

## Ilova qanday ishlaydi

- Ishga tushganda: ruxsat so'raladi (Android 13+ `POST_NOTIFICATIONS`, iOS), FCM token olinadi.
- Mijoz kirgan bo'lsa → `POST /api/me/devices` `{token, platform: android|ios, locale: uz|ru}`;
  token yangilansa yoki ilova tili o'zgarsa — qayta yuboriladi.
- Chiqishda `POST /api/customer-auth/logout` `{refreshToken, deviceToken}` — server tokenni o'chiradi.
- Android kanal: `orders` (importance high). Ilova ochiq bo'lganda Android'da mahalliy bildirishnoma ko'rsatiladi,
  iOS'da tizim o'zi ko'rsatadi.
- Xabar `data: {type: "order", orderId}` — bosilganda `/orders/<id>` ochiladi (ilova yopiq bo'lsa ham; sessiya o'qilgach).
- Ilova ochiq paytdagi buyurtma xabarlari `PushService.orderEvents` oqimiga ham tushadi.

## Tekshirish

1. Fayllarni joylab, haqiqiy qurilmada ishga tushiring, kiring.
2. Backend logida `POST /api/me/devices` 200 ekanini ko'ring.
3. Kassadan buyurtma holatini o'zgartiring → telefonda bildirishnoma, bosilsa buyurtma sahifasi.
