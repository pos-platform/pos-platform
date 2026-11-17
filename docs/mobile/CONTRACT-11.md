# Bosqich 11.8 — Retseptlar, oilaviy ro'yxat, ovozli qidiruv (kelishuv)

Qaror (2026-11-17, foydalanuvchi): retseptlarni tashkilot admini admin panelda yaratadi; oilaviy ro'yxat — egasi a'zoni telefon
raqami bilan qo'shadi; ovozli qidiruv. **Xo'ja Plus va kuryerga choy puli — qilinmaydi.**
JSON — camelCase, vaqt — UTC ISO, miqdor/pul — decimal. Bitta migratsiya: `AddRecipesAndShoppingLists`.
Eski klientlar buzilmasin: `ShopHomeDto` ga faqat yangi ixtiyoriy maydon qo'shiladi.

---

## 1. Retseptlar

**Domen** (tenant, `ShopMarketing.cs` yonida):
- `ShopRecipe`: `BranchId?` (null — barcha filiallar), `Title` (≤80, majburiy), `Description?` (≤1000), `ImageUrl?` (≤500),
  `CookMinutes?` (1–1440), `Servings?` (1–50), `Steps` (≤8000; qadamlar `\n` bilan ajratiladi), `SortOrder`, `IsActive` (true),
  `ShowOnHome` (false), `Items`.
- `ShopRecipeItem`: `RecipeId`, `ProductId`, `Quantity` (decimal > 0, standart 1; tarozi tovari — kg), `Note?` (≤80, masalan "1 stakan"),
  `SortOrder`. Retseptda ko'pi bilan 50 ta mahsulot.

**Admin API** (`OnlineMarketingController`, o'qish — `sales.view`, o'zgartirish — `settings.manage`):
- `GET /api/online-store/recipes` → `RecipeAdminDto[]`; `GET /api/online-store/recipes/{id}`;
  `POST` / `PUT /{id}` body `SaveRecipeRequest { branchId?, title, description?, imageUrl?, cookMinutes?, servings?, steps: string[],
  items: [{ productId, quantity, note? }], sortOrder, isActive, showOnHome }`; `DELETE /{id}`.
- `RecipeAdminDto { id, branchId?, branchName?, title, description?, imageUrl?, cookMinutes?, servings?, steps[], sortOrder, isActive,
  showOnHome, items: [{ productId, name, imageUrl?, price, isWeighted, quantity, note? }] }`.

**Mobil API** (`ShopCatalogController`, anonim ham):
- `GET /api/shop/stores/{branchId}/recipes` → `ShopRecipeBriefDto[] { id, title, imageUrl?, cookMinutes?, servings?, itemCount }`
  (faol, shu filial yoki umumiy; `SortOrder` bo'yicha).
- `GET /api/shop/stores/{branchId}/recipes/{id}` → `ShopRecipeDto { id, title, description?, imageUrl?, cookMinutes?, servings?,
  steps[], items: [{ product: ShopProductDto?, productId, name, quantity, note?, available: bool }] }` —
  filialda sotilmaydigan / qoldig'i yo'q mahsulot `available=false` (ilovada kulrang, savatga qo'shilmaydi).
- `ShopHomeDto.recipes` — `ShowOnHome` retseptlar (≤5, `ShopRecipeBriefDto`).

**Admin panel**: OnlineMarketingView'ga "Retseptlar" tabi (CollectionsTab uslubida): rasm, nom, tavsif, vaqt, porsiya, qadamlar
(qator qo'shish/o'chirish/tartib), mahsulotlar (qidirib qo'shish + miqdor + izoh), filial, faol, bosh sahifada.

**Ilova**: bosh sahifada birinchi `recipes` uchun to'q banner ("Bugun nima pishiramiz?", nom, "N ta mahsulot · bir bosishda"),
bir nechta bo'lsa — "Retseptlar" lentasi; `/home/recipes` ro'yxat, `/home/recipe/:id` sahifa: rasm, vaqt/porsiya, ingredientlar
(belgilash — standart hammasi belgilangan), "Hammasini savatga (N)" (belgilangan va `available` lar, `quantity` bilan), qadamlar.
Bo'sh savatcha izohi: "Retsept tanlang yoki katalogdan boshlang".

## 2. Oilaviy ro'yxat

Ro'yxatlar global (tashkilotga bog'lanmagan, `ShopFavorite` kabi `OrganizationId = null`, `IgnoreQueryFilters` + egasi bo'yicha qo'lda).

**Domen**:
- `ShopList`: `OwnerId` (ShopUser), `Name` (≤60, standart "Oila ro'yxati").
- `ShopListMember`: `ListId`, `Phone` (998XXXXXXXXX, majburiy), `ShopUserId?` (shu raqamli foydalanuvchi bo'lsa — darhol; keyin
  kirsa — so'rovda raqam bo'yicha topiladi va bog'lanadi), `AddedAt`. Ro'yxatda egasidan tashqari ko'pi bilan 10 a'zo.
- `ShopListItem`: `ListId`, `Title` (≤120), `ProductId?`, `Quantity?` (decimal > 0), `IsChecked`, `CheckedAt?`, `CheckedByName?`,
  `AddedByUserId`, `AddedByName`, `SortOrder`. Ro'yxatda ko'pi bilan 200 band.

**Ruxsat**: ro'yxatni egasi va a'zolar ko'radi/bandlarni o'zgartiradi; nomini o'zgartirish, a'zo qo'shish/o'chirish, ro'yxatni
o'chirish — faqat egasi; a'zo o'zini o'chira oladi (chiqish). Boshqalar — 404.

**Mobil API** (`ShopMeController`, token):
- `GET /api/me/lists` → `[{ id, name, isOwner, ownerName, memberCount, itemCount, uncheckedCount }]`. Hech ro'yxat bo'lmasa ham bo'sh.
- `POST /api/me/lists` `{ name? }` → `ShopListDto`; `PUT /api/me/lists/{id}` `{ name }`; `DELETE /api/me/lists/{id}` (egasi).
- `GET /api/me/lists/{id}?branchId=` → `ShopListDto { id, name, isOwner, owner: { name, phone }, members: [{ id, phone, name?,
  joined: bool }], items: [{ id, title, productId?, product: ShopProductDto? (branchId berilsa), quantity?, isChecked, checkedByName?,
  addedByName, createdAt }], updatedAt }`. Bandlar: belgilanmaganlar oldin (`SortOrder`), keyin belgilanganlar.
- `POST /api/me/lists/{id}/members` `{ phone }` (egasi) — o'zi/takror raqam → 400; `DELETE /api/me/lists/{id}/members/{memberId}`
  (egasi yoki shu a'zo).
- `POST /api/me/lists/{id}/items` `{ title, productId?, quantity? }`; `PUT /api/me/lists/{id}/items/{itemId}` `{ title?, quantity?,
  isChecked? }`; `DELETE /api/me/lists/{id}/items/{itemId}`; `POST /api/me/lists/{id}/clear-checked`.
- O'zgarishda boshqa a'zolarga push (mavjud push servisi bo'lsa, "Ro'yxatga X qo'shildi", 5 daqiqada bir martadan ko'p emas) — ixtiyoriy.

**Ilova**: savatcha sarlavhasida "Oilaviy ro'yxat" tugmasi → `/cart/list`. Birinchi kirishda ro'yxat yo'q bo'lsa — avtomatik yaratiladi.
Ekran: a'zolar (doiralar + "Qo'shish" — telefon kiritish sheet'i), band qo'shish maydoni (matn; katalogdan taklif — tanlansa
`productId` bog'lanadi), bandlar (checkbox, kim qo'shgani, o'chirish — surish), "Belgilanganlarni tozalash",
pastda "Savatga o'tkazish (N)" — belgilanmagan va `product` bog'langan bandlar `quantity` (yo'q bo'lsa 1 / birinchi miqdor) bilan.
Ekran ochiq paytda har 15 s yangilanadi. Mehmon — kirish taklifi.

## 3. Ovozli qidiruv

Server o'zgarmaydi. Ilova: `speech_to_text` paketi; bosh sahifa qidiruvidagi to'q doira — mikrofon (skaner maydon ichida kichik
belgi bo'lib qoladi). Bosilganda pastdan sheet: to'lqin animatsiyasi, "Gapiring…", eshitilgan matn jonli; tugagach matn qidiruvga
beriladi (`_runSearch`). Til: ilova tili (`uz_UZ` / `ru_RU`; qurilmada yo'q bo'lsa — standart). Ruxsat rad etilsa yoki tanib olish
mavjud bo'lmasa — snackbar. Android: `RECORD_AUDIO` + `<queries>` ichida `android.speech.RecognitionService`;
iOS: `NSMicrophoneUsageDescription`, `NSSpeechRecognitionUsageDescription`.
