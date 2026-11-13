# Bosqich 9 — Marketing va qulayliklar (kelishuv)

Qaror (2026-11-14, foydalanuvchi): ilovaga reklama/aksiya, Top 10 va global ilovalardagi (Lavka, Uzum Tezkor, Korzinka Go,
Getir, Samokat, Instacart) qulayliklar qo'shiladi. 4 band: 9.1 → 9.4. Bu fayl — backend, admin panel, desktop va mobil
o'rtasidagi **yagona kelishuv**. JSON — camelCase, enumlar — string, vaqt — UTC ISO, pul — `number` (decimal).

Allaqachon bor (qayta qilinmaydi): promokod (`OnlineOrder.PromoCode*`, checkout'da), aksiya foizi (`Promotion`,
`ShopProductDto.oldPrice/discountPercent`), bonus karta/keshbek, qayta buyurtma, rejalashtirilgan yetkazish, push (FCM).

Umumiy qoidalar: `Shop*` servislarida `OrganizationId`/`BranchId` qo'lda filtrlanadi (PLAN.md "Arxitektura qoidalari").
Rasm yuklash — mavjud `POST /api/products/image` (javob `{ url }`, `/uploads/...`). Bitta migratsiya: `AddMarketing`.
Ko'rinadigan do'kon / mahsulot qoidalari — `ShopCatalogService` dagi mavjud yordamchilar bilan bir xil.

---

## 9.1 — Reklama, aksiyalar, Top 10

### Domen
- `ShopBanner` (tenant): `OrganizationId`, `BranchId?` (null — barcha filiallar), `Title` (≤80), `Subtitle?` (≤160),
  `ImageUrl?`, `BackgroundColor?` (`#RRGGBB`), `LinkType` enum `None|Product|Category|Collection|Discounts|Url`,
  `LinkValue?` (Product/Category/Collection — Guid matni, Url — https), `StartsAt?`, `EndsAt?`, `SortOrder` int, `IsActive`.
- `ShopCollection` (tenant): `OrganizationId`, `BranchId?`, `Title` (≤80), `Description?`, `ImageUrl?`, `StartsAt?`, `EndsAt?`,
  `SortOrder`, `IsActive`, `ShowOnHome` bool; `ShopCollectionItem`: `CollectionId`, `ProductId`, `SortOrder`.

### Mijoz API (tokensiz; token bo'lsa `buyAgain` to'ldiriladi)
- `GET /api/shop/stores/{branchId}/home` →
  ```
  { banners: ShopBannerDto[], topProducts: ShopProductDto[] (≤10), collections: ShopCollectionDto[] (ShowOnHome, har birida ≤10 product),
    discountedCount: int, buyAgain: ShopProductDto[] (≤12, 9.2), stories: ShopStoryDto[] (9.3) }
  ```
  `ShopBannerDto { id, title, subtitle, imageUrl, backgroundColor, linkType, linkValue, endsAt }` — faqat faol va muddat ichida, `sortOrder` bo'yicha.
  `ShopCollectionDto { id, title, description, imageUrl, endsAt, products: ShopProductDto[] }`.
- `GET /api/shop/stores/{branchId}/top?days=7&limit=10` → `ShopProductDto[]` — filialda oxirgi `days` kunda sotilgan **miqdor**
  bo'yicha (Sale + Delivered OnlineOrder), faqat ko'rinadigan va qoldig'i bor; 10 dan kam bo'lsa 30 kunlik bilan to'ldiriladi.
  `ShopProductDto` ga yangi maydon: `rank: int?` (faqat top javobida 1..10).
- `GET /api/shop/stores/{branchId}/collections/{id}` → `ShopCollectionDto` (barcha mahsulotlar).
- `GET /api/shop/stores/{branchId}/products?discounted=true` — mavjud ro'yxatga filtr: faqat aksiyadagi mahsulotlar (aksiya sahifasi).
- `ShopProductDto` ga yangi maydon: `discountEndsAt: string?` — qo'llangan `Promotion.EndDate` (ortga sanash uchun).

### Do'kon admini API (`[Authorize]`, ruxsat: mavjud online do'kon sozlamalari bilan bir xil)
- `GET/POST /api/online-store/banners`, `PUT/DELETE /api/online-store/banners/{id}`, `PUT /api/online-store/banners/reorder` `{ ids: Guid[] }`.
  Body = domen maydonlari. Javob `BannerAdminDto` = maydonlar + `id`, `branchName?`.
- `GET/POST /api/online-store/collections`, `GET/PUT/DELETE /api/online-store/collections/{id}`;
  body: maydonlar + `productIds: Guid[]` (tartib saqlanadi). Javob: + `products: [{ id, name, imageUrl, price }]`.

### Mobil
- Bosh sahifa `home` dan quriladi: **banner karuseli** (avtomatik 4 s, indikator nuqtalar, bosilganda `linkType` bo'yicha ochiladi:
  Product → mahsulot, Category → kategoriya filtri, Collection → kolleksiya sahifasi, Discounts → aksiya sahifasi, Url → tashqi brauzer).
  Banner bo'lmasa mavjud yashil "bepul yetkazish" banneri qoladi.
- **Top 10** gorizontal lenta: kartochkada katta raqam (1..10).
- Kolleksiyalar — sarlavha + "Hammasi" + gorizontal lenta; kolleksiya sahifasi (grid, `endsAt` bo'lsa ortga sanash).
- **Aksiya sahifasi** (`discounted=true`, grid); bosh sahifada "Aksiyalar (N)" kirishi.
- Mahsulot sahifasida `discountEndsAt` bo'lsa "Aksiya tugashiga 2 kun 4 soat".
- Savatchada **bepul yetkazish progressi** ("Bepul yetkazishgacha 12 000 so'm") — quote'dagi mavjud qiymatdan.

### Admin panel (Vue)
- Yangi bo'lim "Ilova reklamasi" (`/online-marketing`, OnlineStore bo'limi yonida): tablar **Bannerlar** (ro'yxat, drag yoki ↑↓ tartib,
  rasm yuklash, rang, havola turi + qiymat tanlash — mahsulot/kategoriya/kolleksiya qidiruvi, muddat, filial, faol) va **Kolleksiyalar**
  (nom, rasm, muddat, "bosh sahifada", mahsulot qidirib qo'shish/tartiblash).

---

## 9.2 — Sotuvni oshiradigan qulayliklar

### Domen
- `ShopFavorite` (global, IgnoreQueryFilters): `ShopUserId`, `ProductId`, `CreatedAt`; unikal (ShopUserId, ProductId).
- `ShopStockSubscription` (global): `ShopUserId`, `ProductId`, `BranchId`, `CreatedAt`, `NotifiedAt?`; unikal (user, product, branch).

### Mijoz API (`Shop` policy)
- `GET /api/me/favorites?branchId=` → `ShopProductDto[]` (shu filial narxi/qoldig'i; boshqa tashkilot mahsuloti chiqmaydi).
- `PUT /api/me/favorites/{productId}` (204), `DELETE /api/me/favorites/{productId}` (204). Ko'pi bilan 500 ta.
- `GET /api/me/favorites/ids` → `Guid[]` (yurakchalarni tez bo'yash uchun).
- `PUT /api/me/stock-subscriptions` `{ productId, branchId }` (204), `DELETE /api/me/stock-subscriptions/{productId}?branchId=` (204).
  Fon xizmati (har 10 daqiqa): qoldiq (`Quantity − Reserved`) > 0 bo'lsa push `type=back_in_stock`
  `{ productId, branchId }` "«{nom}» yana sotuvda", `NotifiedAt` qo'yiladi (keyin o'chiriladi).
- `ShopProductDto` ga: `isFavorite: bool?` (token bo'lsa), mahsulot tafsilotida `isSubscribed: bool?`.

### Katalog API (tokensiz)
- `GET /api/shop/stores/{branchId}/products/{id}/related?limit=10` → `ShopProductDto[]` — oxirgi 90 kunda shu mahsulot bilan bir
  chekda/buyurtmada eng ko'p birga olinganlar; yetmasa — shu kategoriyadan ommaboplar. O'zi chiqmaydi, faqat qoldig'i borlar.
- `POST /api/shop/stores/{branchId}/cart-suggestions` `{ productIds: Guid[] }` → `ShopProductDto[]` (≤10) — savatdagilar bilan birga
  olinadiganlar (savatdagilar chiqmaydi).
- `GET /api/shop/stores/{branchId}/search-suggest?q=` → `{ products: [{id,name,imageUrl,price}] (≤8), categories: [{id,name}] (≤3) }`.
- Qidiruv yaxshilanadi (mavjud `search`): lotin↔kirill transliteratsiyasi (`"sut"` ↔ `"сут"`, `"кола"` ↔ `"kola"`), so'zlar bo'yicha
  (har so'z nomda bo'lishi kerak), o'/g'/sh/ch variantlari (`o'`, `oʻ`, `o‘`, `o`) normalizatsiyasi.
- `home.buyAgain` (token bo'lsa) — mijozning shu tashkilotdagi oxirgi 180 kunlik Delivered online buyurtmalari + `Customer` savdolaridan
  eng ko'p olingan, hozir qoldig'i bor mahsulotlar.

### Mobil
- Mahsulot kartochkasi va sahifasida ♥ (kirmagan bo'lsa login so'raydi), Profil → "Sevimlilar" ekrani.
- Qoldiq yo'q mahsulotda "Kelganda xabar berish" tugmasi (obuna/bekor qilish); push bosilsa mahsulot ochiladi.
- Mahsulot sahifasida "Bular bilan birga olishadi" lentasi; savatchada "Qo'shib qo'ying" lentasi (`cart-suggestions`).
- Bosh sahifada "Yana olasizmi?" lentasi (`buyAgain`).
- Qidiruv: yozish paytida takliflar (debounce 300 ms), qidiruv tarixi (oxirgi 10 ta, lokal, tozalash), **shtrix-kod skaneri** tugmasi
  (`mobile_scanner`; topilgan kod mavjud `search` ga beriladi, bitta natija bo'lsa mahsulot ochiladi).

---

## 9.3 — Ishonch va servis

### Domen
- `ShopStory` (tenant): `OrganizationId`, `BranchId?`, `Title` (≤40), `CoverImageUrl`, `StartsAt?`, `EndsAt?`, `SortOrder`, `IsActive`;
  `ShopStorySlide`: `StoryId`, `ImageUrl`, `Caption?` (≤200), `LinkType`/`LinkValue` (banner bilan bir xil enum), `DurationSec` (3–15, std 6), `SortOrder`.
- `OnlineOrder` ga: `Rating` int? (1–5), `RatingComment?` (≤500), `RatingTags?` (vergul bilan: `fast|fresh|polite|accurate|late|damaged|missing|wrong_item`),
  `RatedAt?`. "Mahsulot bo'lmasa" — mavjud `OutOfStockPolicy` (`ReplaceSimilar|Remove|CallMe`), yangi maydon qo'shilmaydi.
- `OnlineStoreSettings` ga: `SupportPhone?`, `SupportTelegram?` (username, `@` siz).

### API
- `home.stories`: `ShopStoryDto { id, title, coverImageUrl, slides: [{ id, imageUrl, caption, linkType, linkValue, durationSec }] }`.
- Admin: `GET/POST /api/online-store/stories`, `GET/PUT/DELETE /api/online-store/stories/{id}` (slaydlar bilan birga), `PUT .../stories/reorder`.
- `ShopOrderDto` va xodim `OnlineOrderDto` ga `rating`,
  `ratingComment`, `ratingTags: string[]`, `ratedAt`; `ShopOrderDto.canRate` (Delivered, baholanmagan, 14 kun ichida).
- `POST /api/shop/orders/{id}/rate` `{ rating, comment?, tags?: string[] }` — faqat `canRate`; xodimga SignalR `OrderUpdated`.
- `GET /api/online-orders/ratings?from=&to=&branchId=` (xodim, `sales.view`) →
  `{ average, count, distribution: {1..5}, tagCounts: {tag:int}, items: [{orderId, number, rating, comment, tags, ratedAt, customerName}] }`.
- `ShopStoreDto` ga `supportPhone`, `supportTelegram`; do'kon admini sozlamalariga (`/api/online-store/settings`) shu ikki maydon.

### Mobil
- Bosh sahifada **stories** doirachalari (ko'rilganlari kulrang, lokal); to'liq ekran ko'rish: progress chiziqlari, bosish — keyingi/oldingi,
  ushlab turish — pauza, pastga surish — yopish, slayd havolasi tugmasi.
- Checkout'da "Mahsulot bo'lmasa": Almashtirish / Qo'ng'iroq qilish / Olib tashlash (`outOfStockPolicy`).
- Yetkazilgan buyurtmada baho kartasi (yulduzlar + teglar + izoh); buyurtma ochilganda `canRate` bo'lsa bottom sheet bir marta taklif qiladi.
- Profil va buyurtma sahifasida **"Yordam"**: qo'ng'iroq (`tel:`) va Telegram (`https://t.me/<username>`).

### Desktop
- Online buyurtma kartasi va yig'ish oynasida `outOfStockPolicy` belgisi ("Bo'lmasa: qo'ng'iroq qiling"), yig'ish varag'ida ham.
- Buyurtma tafsilotida baho (⭐ + teglar + izoh) bo'lsa ko'rsatiladi.

### Admin panel
- "Ilova reklamasi" bo'limiga **Stories** tabi (muqova, slaydlar, havola, davomiylik).
- `OnlineOrdersView` da baho ustuni va yangi **"Baholar"** tabi/sahifa (o'rtacha, taqsimot, teglar, izohlar ro'yxati).
- Online do'kon sozlamalarida "Yordam telefoni", "Telegram".

---

## 9.4 — Mijozni qaytarish

### Domen
- `ShopUser` ga `ReferralCode` (6–8 belgi, unikal, birinchi so'rovda yaratiladi).
- `ShopReferral` (tenant): `OrganizationId`, `ReferrerShopUserId`, `RefereeShopUserId` (tashkilot ichida unikal), `CreatedAt`,
  `RewardedAt?`, `ReferrerBonus`, `RefereeBonus`.
- `OnlineStoreSettings` ga: `ReferralEnabled` bool, `ReferrerBonus` decimal, `RefereeBonus` decimal (ball).
- `LoyaltyTier` (tenant, `LoyaltySettings` ostida): `Name` (≤30), `MinSpend` decimal (oxirgi 365 kun), `CashbackPercent`, `SortOrder`.
  Mijozning keshbek foizi = darajasi foizi (daraja yo'q yoki pastroq bo'lsa — mavjud `CashbackPercent`/kategoriya stavkasi; **eng kattasi**).
  Daraja `LoyaltyRules` ichida hisoblanadi (POS savdo va online — ikkalasida bir xil).
- `ShopCartSnapshot` (global): `ShopUserId`, `BranchId`, `ItemsJson`, `UpdatedAt`, `RemindedAt?`; unikal (user, branch).
- `ShopPushCampaign` (tenant): `OrganizationId`, `BranchId?`, `Title` (≤60), `Body` (≤200), `LinkType`/`LinkValue`, `ScheduledAt?`, `SentAt?`,
  `Status` `Draft|Scheduled|Sending|Sent|Failed`, `SentCount`, `CreatedByUserId`.

### API
- `GET /api/me/referral?organizationId=` → `{ code, enabled, referrerBonus, refereeBonus, invitedCount, rewardedCount, shareText }`.
- `POST /api/me/referral/apply` `{ organizationId, code }` — faqat shu tashkilotda hali Delivered buyurtmasi yo'q mijoz, o'zini taklif qila olmaydi;
  xato — 400 `{ error }`. Mukofot: referee'ning shu tashkilotdagi **birinchi** buyurtmasi `Delivered` bo'lganda ikkalasiga ham
  loyalty ball (`LoyaltyTransaction`, turi mavjud enumdan `Adjust` yoki yangi `Referral`), idempotent. Push `type=referral_reward`.
- `GET /api/me/bonus-card` javobiga: `tier: { name, cashbackPercent } | null`, `nextTier: { name, minSpend, remaining } | null`.
- Loyalty sozlamalari (`/api/loyalty/settings`) ga `tiers: [{ id?, name, minSpend, cashbackPercent }]`.
- `PUT /api/me/cart` `{ branchId, items: [{ productId, quantity }] }` (204; bo'sh items — o'chiradi). Fon xizmati (har 15 daqiqa):
  `UpdatedAt` 2–24 soat oldin, `RemindedAt` yo'q, keyin buyurtma berilmagan bo'lsa → push `type=cart_reminder` `{ branchId }` "Savatchangizda
  N ta mahsulot qoldi", kuniga ko'pi bilan 1 ta, faqat 09:00–21:00 (UTC+5).
- Push kampaniyalar (do'kon admini): `GET/POST /api/online-store/push-campaigns`, `PUT/DELETE /{id}` (faqat Draft/Scheduled),
  `POST /{id}/send` (hozir yuborish). Qabul qiluvchilar: shu tashkilotda `Customer.ShopUserId` bog'langan yoki buyurtma bergan mijozlar
  qurilmalari (`BranchId` berilsa — shu filialda buyurtma berganlar). Fon xizmati `ScheduledAt` kelganda yuboradi. Kuniga ko'pi bilan 3 kampaniya.
  Push data: `type=campaign`, `linkType`, `linkValue`, `branchId?`.

### Mobil
- Profil → **"Do'stni taklif qilish"**: kod, nusxa olish, ulashish (`share_plus`), statistikalar; kod kiritish maydoni (yangi mijozga).
- Bonus karta ekranida daraja nomi, foiz va "Gold'gacha 350 000 so'm" progressi.
- Savatcha o'zgarganda `PUT /api/me/cart` (debounce 2 s, faqat kirgan bo'lsa).
- Push turlari: `back_in_stock` → mahsulot, `cart_reminder` → savatcha, `campaign` → havola, `referral_reward` → bonus karta.
- Bosh sahifada oxirgi Delivered buyurtma bo'lsa **"Oxirgi buyurtmani takrorlash"** kartasi (mavjud `reorder`).

### Admin panel
- Loyalty sahifasida **Darajalar** jadvali (nom, minimal xarid, foiz).
- Online do'kon sozlamalarida "Do'stni taklif" (yoqish, ikki bonus).
- "Ilova reklamasi" bo'limiga **Push xabarlar** tabi: yaratish (sarlavha, matn, havola, filial, vaqt), "Hozir yuborish", holat va yuborilganlar soni.

### Desktop
- Mijoz chipida loyalty darajasi nomi (agar `resolve` javobida `tier` bo'lsa — `/api/loyalty/resolve` ga `tierName` qo'shiladi).

---

## Keyinga qoldirildi
"1+1 / 3 ta olsang −15%" (POS va oflayn desktop narx hisobini o'zgartiradi — alohida bosqich), flash sale, kuryerga choy puli,
Payme/Click online, Android home widget, ovozli qidiruv.
