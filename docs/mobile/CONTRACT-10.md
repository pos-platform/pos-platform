# Bosqich 10 — Oldindan buyurtma, keshbek boshqaruvi, sotuv logi, xatoliklar, qaytarish (kelishuv)

Qaror (2026-11-14, foydalanuvchi): yopiq do'konga ham buyurtma tushsin (ochilganda kassaga/admin panelga chiqadi); do'kon egasi
keshbekni boshqaradi va o'zgarishlar tarixi yuritiladi; har bir sotilgan tovar logi; xatoliklar SuperAdmin'da ko'rinadi;
qaytarishda shu tovar uchun berilgan keshbek ayiriladi, chek bo'yicha qisman qaytarish; kuryer qaytarishda qatnashadi (pastda yechim).
JSON — camelCase, enum — string, vaqt — UTC ISO, pul — decimal. Bitta migratsiya: `AddReturnsAndErrors`.
Eski klientlar buzilmasin: yangi maydonlar ixtiyoriy, eski so'rovlar (masalan, body'siz refund) avvalgidek ishlaydi.

---

## 10.1 — Yopiq do'konga buyurtma (oldindan buyurtma)

- `OnlineStoreSettings.AcceptWhenClosed` bool (standart **true**). `IsAcceptingOrders=false` (kassada o'chirilgan) yoki bloklangan — avvalgidek rad.
- Do'kon yopiq va "imkon qadar tez" tanlansa: buyurtma qabul qilinadi, `OnlineOrder.IsPreorder = true`,
  `OnlineOrder.ScheduledFor` = keyingi ochilish vaqti (UTC+5 ish vaqti, tungi smena hisobga olinadi). Vaqt tanlansa — mavjud qoida.
- `ShopStoreDto` + quote javobiga: `acceptWhenClosed`, `nextOpenAt` (yopiq bo'lsa keyingi ochilish, UTC). Quote: yopiq + `acceptWhenClosed`
  bo'lsa `canOrder=true`, `message` = "Do'kon yopiq. Buyurtma 09:00 da ko'rib chiqiladi".
- `ShopOrderDto`/`OnlineOrderDto` ga `isPreorder`, `scheduledFor`.
- Fon xizmati (har daqiqa): `ScheduledFor` kelgan va hali `New` bo'lgan oldindan buyurtmalar uchun filial guruhiga SignalR `OrderCreated`
  qayta yuboriladi (kassada ovoz chiqadi), `PreorderNotifiedAt` qo'yiladi. Kassa yopiq bo'lsa ham buyurtma ro'yxatda turadi —
  desktop oynasi ochilganda `GET /api/online-orders?active=true` bilan yuklanadi, admin panelda ham ko'rinadi.
- Rezerv va SLA hisoblari `ScheduledFor` dan boshlanadi.
- **Mobil**: sarlavhada "Yopiq · 09:00 da ochiladi"; checkout'da "imkon qadar tez" o'rniga "Do'kon ochilganda (09:00)"; tasdiq ekranida izoh.
- **Desktop**: oldindan buyurtmalar kartasida "🌙 Oldindan · 09:00" belgisi; `ScheduledFor` kelguncha alohida tartibda (pastda), kelganda ovoz.
- **Admin panel**: OnlineOrders'da shu belgi va filtr "Oldindan buyurtmalar"; online do'kon sozlamalarida `acceptWhenClosed` tugmasi.

## 10.2 — Keshbekni boshqarish va tarix

- Do'kon egasi (tashkilot admini, ruxsat — mavjud `/api/loyalty/settings` bilan bir xil) LoyaltyView'da foiz, minimal summa, ball bilan
  to'lash chegarasi, muddat, kategoriya stavkalari va darajalarni boshqaradi (bor), **o'zgarishlar tarixi** qo'shiladi:
- `LoyaltySettingsChange` (tenant): `ChangedAt`, `UserId`, `UserName`, `Field` (masalan `cashbackPercent`, `category:<nom>`, `tier:<nom>`),
  `OldValue?`, `NewValue?`. `PUT /api/loyalty/settings` har o'zgargan maydon uchun yozadi.
- `GET /api/loyalty/settings/history?page=&pageSize=` → `{ items: [{ changedAt, userName, field, oldValue, newValue }], total }`.
- `GET /api/loyalty/transactions?from=&to=&type=&customerId=&search=&page=&pageSize=` → `{ items: [{ id, createdAt, customerId, customerName,
  customerPhone (maskalangan), type, points, balanceAfter, source, saleId?, receiptNumber?, onlineOrderId?, note, userName? }], total,
  totals: { earned, redeemed, refunded, expired, adjusted } }` — barcha mijozlar bo'yicha bonus harakatlari jurnali.
- `POST /api/loyalty/customers/{customerId}/adjust` `{ points (±), reason }` — qo'lda tuzatish (`Adjust`), sabab majburiy. (Bor bo'lsa — qayta ishlatiladi.)
- **Admin panel**: LoyaltyView'ga tablar **Sozlamalar** (bor) · **Bonus harakatlari** (jurnal, filtr, Excel) · **O'zgarishlar tarixi**; mijoz kartasida "Ball tuzatish".

## 10.3 — Sotilgan tovarlar logi

Yangi jadval shart emas: `SaleItem` — log. Har qator uchun hisoblanadi:
- `GET /api/reports/sold-items?from=&to=&branchId=&productId=&categoryId=&cashierId=&customerId=&source=Pos|Online|All&type=Sale|Refund|All&search=&page=&pageSize=`
  → `{ items: [{ saleId, saleItemId, soldAt, receiptNumber, type, source, branchName, cashierName, customerName?, productId, productName,
  barcode, quantity, refundedQuantity, unitPrice, discountAmount, lineTotal, costPrice, profit, paymentMethod, cashbackEarned, fiscalStatus,
  onlineOrderNumber? }], total, totals: { quantity, revenue, profit, refunds } }`.
  `cashbackEarned` — savdo keshbeki qatorlarga summasi bo'yicha taqsimlanadi. `source=Online` — `ClientSaleId` `online:` bilan boshlanadi.
- `GET /api/reports/sold-items/export` (xuddi shu filtrlar, xlsx — mavjud export uslubida).
- **Admin panel**: "Sotuvlar logi" sahifasi (`/sold-items`, Hisobotlar yonida, ruxsat `reports`/`sales.view` — mavjud hisobot ruxsati):
  filtrlar, jadval, chekni ochish (mavjud savdo tafsiloti), Excel.

## 10.4 — Xatoliklar SuperAdmin'da

- `SystemError` (global, tenant filtrisiz): `Fingerprint` (source+type+message shabloni+joy xeshi), `Source` `Api|Background|Fiscal|Desktop|Mobile|Agent`,
  `Level` `Warning|Error|Critical`, `Message` (≤1000), `Details?` (stack, ≤16000), `Path?`, `Method?`, `StatusCode?`, `OrganizationId?`,
  `OrganizationName?`, `BranchId?`, `UserId?`, `UserName?`, `AppVersion?`, `Device?`, `RequestId?`, `FirstSeenAt`, `LastSeenAt`, `Count`,
  `ResolvedAt?`, `ResolvedBy?`. Bir xil fingerprint ochiq (hal qilinmagan) bo'lsa — yangi qator emas, `Count++`, `LastSeenAt` yangilanadi.
  Hal qilingandan keyin qayta chiqsa — qayta ochiladi (`ResolvedAt=null`).
- Yoziladi: `ExceptionHandlingMiddleware` (500), fon xizmatlari xatolari, fiskal yuborish xatolari, push xatolari (Warning).
  Yozish xatosi asosiy so'rovni buzmaydi (alohida scope, try/catch). Sezgir ma'lumot (token, parol, karta) yozilmaydi.
- `POST /api/client-errors` — desktop (xodim tokeni), mobil (mijoz tokeni yoki tokensiz), agent: `{ source, level, message, details?, appVersion?,
  device?, context? }`; rate limit (IP/daqiqa 30), o'lcham chegaralari; tashkilot/foydalanuvchi tokendan olinadi.
- SuperAdmin: `GET /api/admin/errors?source=&level=&organizationId=&status=open|resolved|all&search=&from=&to=&page=` →
  `{ items: [{ id, source, level, message, organizationName, count, firstSeenAt, lastSeenAt, resolvedAt }], total, openCount }`;
  `GET /api/admin/errors/{id}` (to'liq); `POST /api/admin/errors/{id}/resolve`, `POST /api/admin/errors/resolve-bulk` `{ ids }`;
  `GET /api/admin/errors/summary` → `{ openCount, last24h, bySource: {..} }`.
- Tozalash: hal qilinganlar 30 kun, qolganlar 90 kundan keyin (mavjud tozalash fon xizmatiga).
- **Admin panel (SuperAdmin)**: "Xatoliklar" sahifasi (`/admin/errors`), menyuda ochiq xatolar soni belgisi; filtr, guruhlangan ro'yxat,
  tafsilot (stack nusxa olish), "Hal qilindi".
- **Desktop**: `DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` + fiskal/printer xatolari →
  `client-errors` (oflaynda lokal navbat, keyin yuboriladi; dublikat 1 daqiqada bir marta).
- **Mobil**: `FlutterError.onError`, `PlatformDispatcher.instance.onError` → `client-errors` (Crashlytics bilan birga; dublikat cheklovi).

## 10.5 — Qaytarish (chek bo'yicha, keshbek ayirilishi) va kuryer

### Qisman qaytarish (kassa)
- `POST /api/sales/{id}/refund` body ixtiyoriy: `{ items?: [{ saleItemId, quantity }], reason? }`. Body yo'q/`items` bo'sh — **to'liq** (avvalgidek).
- `SaleItem.RefundedQuantity` (decimal). Qaytariladigan miqdor ≤ sotilgan − qaytarilgan. Bitta savdoga bir nechta qisman qaytarish mumkin.
- `SaleStatus` ga `PartiallyRefunded = 4`; hammasi qaytarilsa `Refunded`. `SaleDto.canRefund` — qaytariladigan miqdor qolgan bo'lsa true;
  `SaleItemDto` ga `refundedQuantity`, `refundableQuantity`. Refund `Sale` (`Type=Refund`) faqat qaytarilgan qatorlar bilan, fiskal return cheki.
- **Keshbek**: qaytarilgan qatorlarga to'g'ri keladigan keshbek = asl `LoyaltyPointsEarned` × (qaytarilgan qatorlar summasi / keshbek hisoblangan
  summa) (oxirgi qaytarishda qoldiq to'liq). Shuncha ball mijozdan ayiriladi (`LoyaltyTransactionType.Refund`).
  **Mijoz balansi yetmasa** (ballni ishlatib bo'lgan) — yetmagan qism **qaytariladigan puldan ushlab qolinadi**:
  `refund.LoyaltyWithheldAmount`. Ball bilan to'langan qism ham shu nisbatda ballga qaytariladi (pul emas).
  Fiskal chekda tovarlar to'liq narxda, ushlab qolingan qism asl ball bilan to'lov qanday aks etgan bo'lsa shunday aks etadi
  (`SaleService` dagi mavjud redeem mexanizmiga mos); chop etiladigan chekda "Keshbek ushlab qolindi: X" qatori.
- Refund javobi (`SaleDto`) ga: `loyaltyPointsReversed`, `loyaltyWithheldAmount`, `refundCashAmount` (mijozga beriladigan pul).
- `POST /api/sales/{id}/refund-preview` (xuddi shu body) → `{ items, refundTotal, pointsToReverse, pointsAvailable, withheldAmount, cashToReturn,
  redeemedPointsReturned }` — kassir tasdiqlashdan oldin ko'radi.
- Ombor qoldig'i faqat qaytarilgan miqdorga qaytadi. Smena hisobotida qaytarilgan pul = `refundCashAmount`.
- **Desktop RefundWindow**: chek raqami/skaner → qatorlar ro'yxati (belgilash + miqdor, "Hammasi"), sabab, preview (qaytariladigan summa,
  ayiriladigan keshbek, ushlab qolinadigan pul, beriladigan pul) → tasdiq → refund cheki chop etiladi.

### Online buyurtmani qaytarish — yechim
Kuryer **qaror qabul qilmaydi**, faqat ikki holatda qatnashadi:
1. **Eshik oldida rad etish** (yetkazishda): mijoz tovarni olmaydi (shikast, noto'g'ri, sifatsiz). Kuryer ilovasida `delivered` paytida
   `rejectedItems: [{ itemId, quantity, reason }]` beriladi → buyurtma summasi qayta hisoblanadi, kuryer kamroq naqd oladi, shu buyurtma
   `Sale` siga avtomatik **qisman qaytarish** (yuqoridagi qoida; keshbek faqat qolgan tovarlarga beriladi — Delivered'da hisoblanadi).
   Tovarni kuryer do'konga qaytaradi: desktopda "Kuryer qaytargan tovarlar" ro'yxati → "Qabul qilindi" (ombor rezervi/qoldig'i shu yerda qaytadi).
   Xodim `OnlineOrderDto` ga: `rejectedItems: [{ itemId, productName, quantity, reason }]`, `rejectedReturnedAt?`. `GET /api/online-orders/rejected-pending?branchId=`
   (rad etilgan, hali qabul qilinmagan buyurtmalar), `POST /api/online-orders/{id}/rejected-received` (do'konga keldi → qoldiq qaytadi, `rejectedReturnedAt`).
2. **Yetkazilgandan keyin qaytarish so'rovi** — mobil → do'kon tasdiqlaydi → qaytarishni **kuryer olib ketadi yoki mijoz do'konga olib keladi**.

- `OnlineStoreSettings`: `ReturnsEnabled` (std true), `ReturnDays` (std 3), `ReturnPickupByCourier` (std true).
- `OnlineReturn` (tenant): `OnlineOrderId`, `ShopUserId`, `Number`, `Status` `Requested|Approved|PickupAssigned|PickedUp|Received|Refunded|Rejected|Cancelled`,
  `Method` `CourierPickup|BringToStore`, `Reason` `Damaged|WrongItem|Quality|Expired|Missing|Other`, `Comment?`, `PhotoUrls` (≤3),
  `RefundMethod` `Cash|Transfer|Bonus`, `CourierId?`, `RejectReason?`, `RefundSaleId?`, `RefundAmount`, `LoyaltyWithheldAmount`, vaqt belgilari;
  `OnlineReturnItem`: `OnlineOrderItemId`, `ProductName`, `Quantity`, `UnitPrice`.
- Mijoz: `POST /api/shop/orders/{id}/returns` (multipart: `items` JSON, `reason`, `comment`, `refundMethod`, `photos[]`) — faqat Delivered,
  `ReturnDays` ichida, miqdor ≤ olingan − qaytarilgan; `GET /api/shop/returns`, `GET /api/shop/returns/{id}`, `POST /api/shop/returns/{id}/cancel`
  (faqat Requested). `ShopOrderDto.canReturn`.
- Xodim (`sales.view` ko'rish, `sales.create` amal): `GET /api/online-returns?status=&branchId=`, `GET /{id}`, `POST /{id}/approve` `{ method }`,
  `POST /{id}/reject` `{ reason }`, `POST /{id}/assign-courier` `{ courierId }`, `POST /{id}/received` (tovar do'konga keldi → qisman refund
  bajariladi, keshbek ayiriladi, `RefundMethod=Bonus` bo'lsa pul o'rniga ball), `POST /{id}/refunded` (pul berildi/o'tkazildi).
- Kuryer: `GET /api/courier/orders` javobiga qaytarish vazifalari (`kind: "delivery"|"return_pickup"`), `POST /api/courier/returns/{id}/picked-up`
  (tovar olindi). Naqd qaytarish kuryer orqali **qilinmaydi** (pul do'kon tomonidan, `received` dan keyin — naqd do'konda yoki o'tkazma/bonus).
- SignalR: xodimga `ReturnCreated`/`ReturnUpdated`, mijozga `ReturnStatusChanged`; push `type=return_status` `{ returnId }`.
- **Mobil**: buyurtma sahifasida "Qaytarish" (tovar va miqdor tanlash, sabab, izoh, 3 ta rasm, pul qaytarish usuli) → "Qaytarishlarim" ro'yxati
  va holat ekrani. Kuryer rejimida: yetkazishda "Ba'zi tovarlarni rad etdi" (qator, miqdor, sabab) va "Qaytarish olib ketish" vazifalari.
- **Desktop**: Online oynada **Qaytarishlar** bo'limi (so'rovlar, rasmlar, tasdiq/rad, kuryer biriktirish, "Do'konga keldi", "Pul berildi")
  va "Kuryer qaytargan tovarlar" (eshik oldida rad etilganlar).
- **Admin panel**: OnlineOrders'da "Qaytarishlar" tabi (ro'yxat, tafsilot, rasmlar, amallar), online do'kon sozlamalarida qaytarish sozlamalari;
  savdolar sahifasida qisman qaytarilgan holat va qaytarilgan miqdorlar.

## 10.6 — Kuryerga taklif: push, qabul qilish, kim olgani ko'rinadi

Qaror (foydalanuvchi): buyurtma tayyorlansa filial kuryerlariga ilovadan xabar boradi, kuryer "Qabul qilaman"/"Rad etaman" bosadi,
**birinchi qabul qilgan** oladi, boshqalarda u yo'qoladi; kassada va admin panelda qaysi kuryer olgani ko'rinadi. Qo'lda biriktirish (mavjud `dispatch`) qoladi.

- `OnlineStoreSettings.CourierOfferMode` `Off|Broadcast` (std `Broadcast`), `CourierOfferTimeoutSec` (std 120).
- **Kuryer qurilma tokeni**: `StaffDeviceToken` (UserId, Token, Platform, UpdatedAt; token unikal). `PUT /api/courier/device-token` `{ token, platform }`,
  `DELETE /api/courier/device-token` `{ token }` (logout). Push yuborish mavjud FCM servisi orqali.
- `CourierOffer` (tenant): `OnlineOrderId`, `CourierUserId`, `Status` `Pending|Accepted|Declined|Expired|Cancelled`, `CreatedAt`, `RespondedAt?`.
- Taklif yuboriladi: buyurtma yetkazish turi bo'lsa, `Picking → Ready` (yoki `AwaitingTransfer` → to'lov tushgach `Ready`) bo'lganda va kuryer
  biriktirilmagan bo'lsa — filialning faol kuryerlariga (aktiv, `branchId` mos). Shuningdek xodim `POST /api/online-orders/{id}/offer-couriers`
  (qayta yuborish). Push `type=courier_offer` `{ orderId }` "Yangi buyurtma #1023 · 2.4 km · 85 000 so'm", SignalR `courier:{id}` ga `CourierOffer`.
- `GET /api/courier/offers` → `[{ offerId, orderId, number, branchName, address, distanceKm?, totalAmount, paymentMethod, itemCount, expiresAt }]`.
- `POST /api/courier/offers/{offerId}/accept` → **atomik** (birinchisi yutadi: `CourierUserId` null bo'lsagina; concurrency token yoki
  shartli UPDATE). Yutsa: buyurtmaga `CourierUserId/Name/Phone` yoziladi, status `Ready` da qoladi (kuryer do'kondan olib ketganda xodim
  `dispatch` bosadi yoki kuryer `POST /api/courier/orders/{id}/picked-up` → `OnTheWay`), boshqa takliflar `Cancelled`, ularga SignalR
  `CourierOfferClosed` `{ orderId }`. Kech qolgan — 409 `{ error: "Buyurtmani boshqa kuryer oldi" }`.
- `POST /api/courier/offers/{offerId}/decline` `{ reason? }`. Hamma rad etsa yoki `timeout` o'tsa — taklif `Expired`, xodimga SignalR
  `OrderUpdated` + `courierOfferStatus = "NoCourier"` (kassir qo'lda biriktiradi yoki qayta yuboradi).
- Xodim `OnlineOrderDto` ga: `courierOfferStatus` `None|Pending|Accepted|NoCourier`, `courierAcceptedAt?`, `courierName/phone` (bor),
  `courierOffers: [{ courierName, status, respondedAt }]` (tafsilotda).
- Mijozga: kuryer qabul qilganda push "Kuryer {ism} buyurtmangizni oladi" (mavjud holat push'lari uslubida), `ShopOrderDto` da kuryer ismi/telefoni.
- `GET /api/courier/orders` endi qabul qilingan `Ready` buyurtmalarni ham qaytaradi (`status`), kuryer ilovasida "Do'kondan olish" → `picked-up`.
- **Mobil (kuryer rejimi)**: FCM token yuborish (kirganda/yangilanganda, chiqishda o'chirish); push/SignalR kelganda to'liq ekranli taklif kartasi
  (raqam, manzil, masofa, summa, to'lov turi, ortga sanash) — "Qabul qilaman"/"Rad etaman"; "Takliflar" ro'yxati; 409 da "Boshqa kuryer oldi";
  push bosilsa taklif ochiladi; qabul qilingan buyurtmada "Do'kondan oldim" tugmasi.
- **Desktop**: buyurtma kartasida kuryer holati: "⏳ Kuryer kutilmoqda (3/5 rad)", "✅ Ali Valiyev qabul qildi 14:32", "⚠ Kuryer topilmadi" +
  "Qayta yuborish" va mavjud qo'lda biriktirish; tafsilotda takliflar ro'yxati; SignalR `OrderUpdated` bilan yangilanadi.
- **Admin panel**: OnlineOrders'da kuryer ustuni va holati, tafsilotda takliflar tarixi; online do'kon sozlamalarida `courierOfferMode`,
  `courierOfferTimeoutSec`.

## Keyinga qoldirildi
Payme/Click orqali avtomatik pul qaytarish, qaytarishga kuryer naqd berishi, qaytarish uchun alohida fiskal "aksessuar" holatlari.
