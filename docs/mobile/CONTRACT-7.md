# Bosqich 7 — Cashback va bonus karta (online + do'kondagi xarid)

JSON camelCase, enumlar string. Ball = so'm (1 ball = 1 so'm), `numeric(18,2)`, hisoblashda pastga yaxlitlanadi (butun so'm).
Bonus balansi do'kon (tashkilot) bo'yicha alohida — mavjud `Customer.LoyaltyPoints`. Mijoz akkaunti global (`ShopUser`), har do'konda `Customer.ShopUserId`.

## 1. Sozlamalar (tashkilot bo'yicha) — yangi `LoyaltySettings` (har tashkilotga bitta, yo'q bo'lsa standart qiymatlar)
`{ enabled: true, cashbackPercent: 1, minSaleAmount: 0, maxRedeemPercent: 50, pointsExpireDays: null (faolsizlik kunlari, null — muddatsiz),
   signupBonus: 0, earnOnDiscountedItems: true, categoryRates: [{ categoryId, categoryName, percent }] }`
- `GET /api/loyalty/settings`, `PUT /api/loyalty/settings` (`settings.manage`; `categoryRates` to'liq ro'yxat sifatida almashtiriladi).
- Validatsiya: foizlar 0..100, maxRedeemPercent 0..100, pointsExpireDays null yoki 30..3650.

## 2. Ball yozish (SaleService — hozirgi qotirilgan 1% o'rniga)
- Savdoda mijoz bo'lsa va `enabled`: har qator uchun `percent = categoryRates[category] ?? cashbackPercent`; `earnOnDiscountedItems=false` bo'lsa chegirmali
  (aksiya yoki promo/bonus chegirmasi bor) qatorlar hisobga olinmaydi; `ProductType.Service` qatorlari hisobga olinmaydi; savdo jami `< minSaleAmount` bo'lsa ball yo'q.
  Baza — qatorning chegirmadan keyingi summasi. Online buyurtma ham shu yo'l bilan (Tayyor'da Sale) ball oladi.
- Qaytarish (refund) — mavjud mantiq ball'ni qaytarib oladi; agar bo'lmasa qo'shing (manfiy tranzaksiya; balans manfiy bo'lib ketmasin — 0 gacha).
- `LoyaltyTransaction` ga `Type` (`Earn|Redeem|Expire|Adjust|Signup|Refund`) va `Source` (`Store|Online`) qo'shiladi (eski yozuvlar: musbat → Earn, manfiy → Redeem).
- `Customer.LastActivityAt` (har ball harakatida yangilanadi).
- Muddat: fon vazifa kuniga bir marta — `pointsExpireDays` bor tashkilotlarda `LastActivityAt < now − N kun` va `LoyaltyPoints > 0` → Expire tranzaksiya, balans 0.

## 3. Do'konda tanish — dinamik QR bonus karta
- `GET /api/me/bonus-card?branchId=` (mijoz) → `{ storeName, points, qrPayload, expiresAt }`; `qrPayload = "PCB1." + base64url(payload) + "." + base64url(HMAC-SHA256)`,
  payload `{ u: shopUserId, o: organizationId, e: unixExpiry(+90 s) }`; kalit — konfiguratsiya `Loyalty:QrSigningKey` (bo'lmasa JWT kalitidan HKDF/derivatsiya). Stateless.
  Shu so'rovda do'konda `Customer` bo'lmasa — yaratiladi (ShopUser'ga bog'lab; telefon bo'yicha mavjud mijoz topilsa bog'lanadi) va `signupBonus` bir marta beriladi.
- `GET /api/me/bonus/stores` → `[{ organizationId, branchId (birinchi faol online filial yoki null), storeName, logoUrl, points }]` (mijoz bog'langan barcha do'konlar).
- `GET /api/me/bonus/history?organizationId=&page=&pageSize=` → PagedResult `{ id, createdAt, type, source, points, amount, comment, receiptNumber }`.
- Xodim: `POST /api/loyalty/resolve` body `{ code }` (`sales.create`) → `{ customerId, name, maskedPhone, points, maxRedeemPercent }`.
  Kod imzosi/muddati tekshiriladi, `o` joriy tashkilot bo'lishi shart; Customer topiladi/yaratiladi. Xato: 400 "Bonus karta kodi eskirgan — mijoz ilovada QR'ni yangilasin".

## 4. Kassada ball bilan to'lash
- `CreateSaleRequest.RedeemPoints` (decimal, ixtiyoriy) — `CustomerId` bilan birga. Server: ≤ mijoz balansi, ≤ jami × maxRedeemPercent/100; qatorlarga chegirma sifatida
  mutanosib taqsimlanadi (fiskal chek to'g'ri), Redeem tranzaksiya. Javob `SaleDto` + `loyaltyPointsEarned` (bor), `loyaltyPointsRedeemed`.
- Ball bilan to'langan qism uchun ham cashback beriladimi: yo'q (baza — chegirmadan keyingi summa, avtomatik).

## 5. Admin panel
- Mijozlar sahifasi: ball balansi, "ilova" belgisi (`shopUserId` bor), tarix (mavjud bo'lsa kengaytiriladi) — `GET /api/customers/{id}/loyalty-history` → yuqoridagi tarix elementlari.
- Hisobot: `GET /api/loyalty/report?from=&to=` → `{ earned, redeemed, expired, activeCustomers, outstandingPoints }`.
