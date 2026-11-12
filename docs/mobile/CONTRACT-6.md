# Bosqich 6 — API kelishuvi (kuryer, to'lovlar, bonus, promokod, markirovka, hudud)

Barcha agentlar shu kelishuvga amal qiladi. JSON camelCase, enumlar string. Pul `decimal`, `numeric(18,2)`.
Narx/chegirma/bonus/summa faqat serverda hisoblanadi (konstitutsiya II).

## 1. Kuryer akkaunti
- `UserRole.Courier = 5`. Do'kon admini mavjud foydalanuvchilar API'si orqali yaratadi (`role: "Courier"`, telefon va filial majburiy).
  Kuryer litsenziya talab qilmaydi. Kuryer desktop kassaga kira olmaydi (desktop rad etadi).
- Kirish: mavjud `POST /api/auth/login` (username, password, deviceId, deviceName) → xodim JWT (+ refresh, agar mavjud bo'lsa — backend agenti
  aniq javobni hisobotda yozadi). Mobil ilova kuryer tokenini mijoz tokenidan alohida saqlaydi.
- `GET /api/courier/me` → `{ id, name, phone, branchId, branchName, storeName }`.

## 2. Kuryer endpointlari `/api/courier` (`[Authorize(Roles = "Courier")]`, faqat o'ziga biriktirilganlar)
- `GET /api/courier/orders?history=false` → faol (OnTheWay) biriktirilgan buyurtmalar; `history=true` → bugungi yetkazilgan/muvaffaqiyatsiz.
  Element: `{ id, number, status, customerName, customerPhone, addressLine, entrance, floor, apartment, intercom, landmark, comment,
  contactless, latitude, longitude, totalAmount, paymentMethod, paymentStatus, itemCount, dispatchedAt, storeName, branchAddress,
  branchLatitude, branchLongitude }`.
- `GET /api/courier/orders/{id}` → yuqoridagi + `items: [{ productName, quantity, unitPrice, lineTotal }]`.
- `POST /api/courier/orders/{id}/delivered` body `{ cashCollected? }` → naqd bo'lsa Paid va `courierCashCollected` yoziladi;
  o'tkazma/online to'lanmagan bo'lsa 400. Javob: buyurtma elementi.
- `POST /api/courier/orders/{id}/failed` body `{ reason }` → buyurtma `Ready` ga qaytadi, kuryer uziladi, sabab `comment`ga qo'shiladi
  (`[Kuryer] ...`), xodimga xabar.
- SignalR `/hubs/orders`: kuryer ulanganda `courier:{userId}` guruhiga qo'shiladi; xabarlar `CourierOrderAssigned`, `OrderUpdated` (yengil event).

## 3. Xodim (desktop/admin) o'zgarishlari `/api/online-orders`
- `GET /api/online-orders/couriers?branchId=` → `[{ id, name, phone, activeOrders }]` (faol kuryerlar).
- `POST {id}/dispatch` body `{ courierUserId?, courierName?, courierPhone? }` — `courierUserId` berilsa ism/telefon foydalanuvchidan olinadi.
- `OnlineOrderDto` + `courierUserId`, `courierCashCollected`, `courierCashHandedOverAt`, `promoCode`, `promoDiscount`, `bonusUsed`.
- Markirovka: `OnlineOrderItemDto.isMarked` (bor), + `markingCodes: string[]`. `PUT {id}/items` → `PickItemChange.markingCodes?: string[]`
  (markirovkali dona tovar uchun kodlar soni = butun miqdor). `ready` markirovkali qatorlarda kodlar to'liq bo'lmasa 400;
  savdoda har kod alohida qator (miqdor 1).
- Kuryer naqdi: `ready` da Yetkazish + Naqd + to'lanmagan bo'lsa savdo (Naqd) bilan birga kassa harakati **Expense** "Online #N — naqd kuryerda";
  `POST {id}/cash-handover` (`sales.create`) → **Income** "Online #N — kuryer topshirdi" (`courierCashCollected ?? totalAmount`),
  `courierCashHandedOverAt` yoziladi. `GET /api/online-orders/courier-cash?branchId=` → topshirilmagan naqdlar `[{ orderId, number, courierName,
  amount, deliveredAt }]`.
- Yetkazish narxi fiskal chekda: `OnlineStoreSettings.DeliveryServiceMxikCode` kiritilgan va `deliveryFee > 0` bo'lsa savdoga
  "Yetkazib berish xizmati" qatori qo'shiladi (`ProductType.Service = 2`, tashkilotga bitta yashirin mahsulot, ombordan ayirilmaydi,
  POS/katalog ro'yxatlarida chiqmaydi). Kod kiritilmagan bo'lsa — avvalgidek chekka kirmaydi.

## 4. Mijoz: promokod, bonus, hudud, online to'lov
- `POST /api/shop/orders/quote` va `POST /api/shop/orders` qo'shimcha maydonlar: `promoCode?`, `useBonus` (bool), `addressId?` (quote'da ham).
  Quote javobiga: `promoDiscount`, `promoMessage` (yaroqsiz bo'lsa sabab), `bonusAvailable`, `bonusUsed`, `deliveryAvailable` (bool),
  `deliveryMessage`, `distanceKm`. `totalAmount = itemsTotal − promoDiscount − bonusUsed + deliveryFee`.
- Bonus: do'kondagi `Customer.LoyaltyPoints` (1 ball = 1 so'm). `bonusPayEnabled` va `bonusMaxPercent` (mahsulotlar summasidan %) bo'yicha.
  Ball "Tayyor"da yechiladi (LoyaltyTransaction manfiy); savdo qatorlariga chegirma sifatida taqsimlanadi (fiskal chek to'g'ri bo'lishi uchun).
- `GET /api/me/bonus?branchId=` → `{ points }` (kirgan mijoz).
- Promokod: tashkilot bo'yicha `PromoCode { code, discountPercent?, discountAmount?, minOrderAmount, validFrom?, validTo?, maxUses?, usedCount,
  perUserLimit, isActive }`. Yaratishda `usedCount++`, bekor/rad etilganda qaytariladi. Chegirma "Tayyor"da savdo qatorlariga taqsimlanadi.
- Hudud: `OnlineStoreSettings.DeliveryRadiusKm` (null — cheklovsiz). Manzil koordinatasi yo'q bo'lsa — ruxsat (ogohlantirish matni bilan);
  radiusdan tashqari bo'lsa `deliveryAvailable=false`, buyurtma 400.
- Online to'lov (Payme/Click) — yakuniy summa ma'lum bo'lgach: `ShopOrderDto` + `canPayOnline`, `onlineProviders: ["Payme","Click"]`
  (do'kon `allowPayme/allowClick` va buyurtma to'lanmagan, holat Ready/AwaitingTransfer/ReadyForPickup/OnTheWay, `SaleId` bor).
  `POST /api/shop/orders/{id}/pay` body `{ provider: "Payme"|"Click" }` → `{ paymentUrl }` (mavjud `OnlinePaymentService` orqali, SaleId bo'yicha).
  To'lov tasdiqlanganda (mavjud callback) buyurtma `PaymentStatus=Paid`, AwaitingTransfer → Ready/ReadyForPickup, xabar/push.
  Checkout'da to'lov usuli sifatida "Payme/Click (yig'ilgach)" — `paymentMethod: "Transfer"` + `onlinePaymentProvider: "Payme"|"Click"`
  (karta raqami ko'rsatilmaydi, o'rniga to'lov tugmasi).
- `ShopStoreDto` + `allowPayme`, `allowClick`, `bonusPayEnabled`, `bonusMaxPercent`, `deliveryRadiusKm`.

## 5. Sozlamalar
- SuperAdmin (`UpdateOnlineStoreAdminRequest`/DTO): `allowPayme`, `allowClick`.
- Do'kon admini (`UpdateOnlineStoreSettingsRequest`/DTO): `deliveryRadiusKm`, `deliveryServiceMxikCode`, `bonusPayEnabled`, `bonusMaxPercent` (0..100).
- Promokodlar: `GET/POST /api/promo-codes`, `PUT/DELETE /api/promo-codes/{id}` (`settings.manage`).
