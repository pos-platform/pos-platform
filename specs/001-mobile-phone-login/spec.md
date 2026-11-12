# Feature Specification: Mobil ilovaga telefon raqam bilan kirish

**Feature Branch**: `001-mobile-phone-login`

**Created**: 2026-11-12

**Status**: Draft

**Input**: User description: "Mobil ilova (Flutter, mobile/) — 4.2 Kirish: telefon +998 va 5 xonali kod bilan kirish, tokenlarni xavfsiz saqlash va avtomatik yangilash, \"Hozircha kirmasdan ko'rish\". Backend tayyor: POST /api/customer-auth/send-otp|verify|refresh|logout (docs/mobile/PLAN.md, 1.3). Dizayn: artifact H9qqdTExBhCdqrnFEYfGva, project/Login.dc.html."

**Bog'liq hujjatlar**: `docs/mobile/PLAN.md` (4.2-qadam; server tomoni 1.3 tayyor va sinalgan),
dizayn — "Kirish" ekrani (Login).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Telefon raqam va kod bilan kirish (Priority: P1)

Xaridor ilovani ochadi, O'zbekiston telefon raqamini (+998 avtomatik turadi) kiritadi va kod so'raydi.
Unga 5 xonali kod keladi (Telegram orqali yoki SMS). Kodni kiritgach ilovaga kiradi va bosh sahifaga
o'tadi. Agar bu raqam bilan avval kirmagan bo'lsa, akkaunt avtomatik ochiladi — alohida ro'yxatdan o'tish yo'q.

**Why this priority**: Buyurtma berish, buyurtmalarni kuzatish va profil — hammasi kirishni talab qiladi.
Kirishsiz ilovaning asosiy qiymati ishlamaydi.

**Independent Test**: Toza o'rnatilgan ilovada raqam kiritib, kelgan kodni yozib, bosh sahifaga o'tish;
ilovani yopib qayta ochganda qayta kod so'ramasligi.

**Acceptance Scenarios**:

1. **Given** ilova birinchi marta ochilgan, **When** xaridor to'g'ri raqam kiritib "kod olish"ni bossa, **Then** kod yuborilgani,
   qaysi raqamga yuborilgani (qisman yashirilgan, masalan +998 90 *** ** 67) va qayta yuborishgacha qolgan vaqt ko'rsatiladi.
2. **Given** kod yuborilgan, **When** xaridor 5 xonali to'g'ri kodni kiritsa, **Then** u tizimga kiradi va bosh sahifaga o'tadi
   (5-raqam kiritilishi bilan avtomatik tekshiriladi, alohida tugma bosish shart emas).
3. **Given** xaridor noto'g'ri kod kiritgan, **When** tekshiruv natijasi kelsa, **Then** xato va qolgan urinishlar soni ko'rsatiladi,
   kod maydoni tozalanadi.
4. **Given** xaridor avval kirgan, **When** ilovani yopib qayta ochsa, **Then** kod so'ralmaydi — to'g'ridan-to'g'ri bosh sahifa ochiladi.
5. **Given** qayta yuborish vaqti tugagan, **When** xaridor "qayta yuborish"ni bossa, **Then** yangi kod yuboriladi va hisoblagich qayta boshlanadi.
6. **Given** Telegram orqali kod kelmagan, **When** xaridor "SMS orqali yuborish"ni tanlasa, **Then** kod SMS bilan yuboriladi.

---

### User Story 2 - Kirmasdan katalogni ko'rish (Priority: P2)

Xaridor hali ro'yxatdan o'tishni xohlamaydi. "Hozircha kirmasdan ko'rish"ni bosib, do'kon va mahsulotlarni
ko'radi. Faqat buyurtma berish, buyurtmalar yoki profil kabi shaxsiy qismlarga o'tganda kirish so'raladi;
kirgandan keyin o'sha joyga qaytariladi.

**Why this priority**: Yangi xaridorni darhol ro'yxatdan o'tishga majburlash uni yo'qotadi; avval mahsulotni
ko'rsatish konversiyani oshiradi. Dizaynda bu tugma bor.

**Independent Test**: Kirish ekranida "Hozircha kirmasdan ko'rish"ni bosib bosh sahifaga o'tish; Profil yoki Buyurtmalar
tabiga bosilganda kirish ekrani chiqishi; kirgach Profil tabiga qaytish.

**Acceptance Scenarios**:

1. **Given** kirish ekrani, **When** xaridor "Hozircha kirmasdan ko'rish"ni bossa, **Then** bosh sahifa ochiladi.
2. **Given** xaridor kirmagan, **When** u kirishni talab qiladigan qismga o'tsa, **Then** kirish ekrani ochiladi.
3. **Given** kirish shu tarzda so'ralgan, **When** xaridor muvaffaqiyatli kirsa, **Then** u o'tmoqchi bo'lgan qismga qaytariladi.
4. **Given** xaridor kirmasdan ko'rgan va ilovani yopgan, **When** qayta ochsa, **Then** yana kirish ekrani emas, bosh sahifa ochiladi
   (tanlovi eslab qolinadi).

---

### User Story 3 - Uzoq vaqt kirgan holatda qolish va chiqish (Priority: P3)

Xaridor bir marta kirgach, haftalar davomida qayta kod so'ralmasdan foydalanadi. Sessiya fonda jimgina yangilanadi.
Profil sahifasida "Chiqish"ni bosganda shu qurilmadagi sessiya yopiladi. Sessiya serverda bekor qilingan
(masalan akkaunt bloklangan yoki uzoq vaqt ishlatilmagan) bo'lsa, xaridor xato ko'rmasdan kirish ekraniga qaytariladi.

**Why this priority**: Har safar kod so'rash xaridorni bezdiradi va SMS xarajatini oshiradi; lekin bu P1 dan keyin keladi.

**Independent Test**: Kirgandan keyin qisqa muddatli ruxsat tugashini kutib (yoki uni sun'iy eskirtirib) har qanday himoyalangan
amalni bajarish — amal muvaffaqiyatli bo'lishi va qayta kod so'ralmasligi; "Chiqish"dan keyin himoyalangan qismga kira olmaslik.

**Acceptance Scenarios**:

1. **Given** xaridor kirgan va qisqa muddatli ruxsat eskirgan, **When** u himoyalangan amal bajarsa, **Then** sessiya fonda yangilanadi
   va amal xatosiz bajariladi.
2. **Given** bir vaqtda bir nechta so'rov ruxsat eskirganini aniqlasa, **When** yangilash bajarilsa, **Then** yangilash faqat bir marta bo'ladi,
   barcha so'rovlar yangi ruxsat bilan davom etadi (sessiya yopilib qolmaydi).
3. **Given** sessiya serverda bekor qilingan, **When** yangilash rad etilsa, **Then** saqlangan kirish ma'lumoti o'chiriladi va kirish ekrani ochiladi.
4. **Given** xaridor kirgan, **When** "Chiqish"ni bossa, **Then** sessiya serverda yopiladi, qurilmadan o'chiriladi va kirish ekrani ochiladi
   (internet yo'q bo'lsa ham qurilmadan o'chiriladi).

---

### Edge Cases

- Raqam to'liq emas (9 raqamdan kam) — "kod olish" tugmasi faol emas.
- Raqam formatda kiritilsa (bo'shliq, tire, "+998" bilan nusxalangan) — to'g'ri tushuniladi.
- Kod juda tez qayta so'ralsa yoki soatlik limit oshsa — server aytgan kutish vaqti ko'rsatiladi, tugma o'sha vaqtgacha faol emas.
- Urinishlar tugasa ("yangi kod so'rang") — kod maydoni bloklanadi, faqat "qayta yuborish" qoladi.
- Kod eskirgan (5 daqiqa) — tegishli xabar va qayta yuborish taklifi.
- Akkaunt bloklangan — tushunarli xabar, kirish ekranida qoladi.
- Internet yo'q yoki server javob bermaydi — "internetni tekshiring" xabari, kiritilgan raqam saqlanib qoladi.
- Xaridor kod kutayotganda raqamni o'zgartirmoqchi — raqamga qaytish mumkin.
- SMS'dagi kodni telefon avtomatik taklif qilsa — kod maydoni uni qabul qiladi.
- Ilova kod kutayotganda fonga o'tib qaytsa — hisoblagich to'g'ri vaqtni ko'rsatadi.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Kirish ekrani dizayndagi "Kirish" ekraniga mos bo'lishi SHART: logotip, "PosCloud Market" sarlavhasi, tavsif,
  +998 prefiksli telefon maydoni, 5 katakli kod maydoni, "Kirish" tugmasi, "Hozircha kirmasdan ko'rish" havolasi,
  foydalanish shartlari va maxfiylik siyosati havolalari.
- **FR-002**: Ilova faqat O'zbekiston raqamini (+998 va 9 raqam) qabul qilishi SHART; raqam o'qishga qulay ko'rinishda guruhlanadi (90 123 45 67).
- **FR-003**: Ilova kod so'rovini yuborishi va server qaytargan kanal, kod uzunligi, amal qilish va qayta yuborish vaqtlariga
  amal qilishi SHART (ilovada qattiq yozilmaydi).
- **FR-004**: Qayta yuborish tugmasi hisoblagich tugaguncha faol bo'lmasligi SHART; server "juda tez" deb rad etsa, u aytgan vaqt ishlatiladi.
- **FR-005**: Xaridor kod Telegram orqali kelmagan holatda SMS orqali yuborishni so'ray olishi SHART.
- **FR-006**: Kod to'liq kiritilganda avtomatik tekshirilishi SHART; tekshiruv davomida qayta yuborish va kiritish bloklanadi.
- **FR-007**: Server xabarlari (noto'g'ri kod, qolgan urinishlar, eskirgan kod, bloklangan akkaunt, limit) xaridorga tushunarli tilda ko'rsatilishi SHART.
- **FR-008**: Kirish ma'lumotlari qurilmaning himoyalangan xotirasida saqlanishi SHART; oddiy sozlamalar faylida saqlanmaydi.
- **FR-009**: Ilova ochilganda saqlangan sessiya bo'lsa kirish ekrani o'tkazib yuborilishi SHART.
- **FR-010**: Qisqa muddatli ruxsat eskirganda ilova uni fonda bir marta yangilashi va to'xtagan so'rovni takrorlashi SHART;
  bir vaqtdagi bir nechta so'rov uchun yangilash parallel bajarilmasligi SHART.
- **FR-011**: Yangilash rad etilsa saqlangan sessiya o'chirilishi va kirish ekrani ochilishi SHART.
- **FR-012**: "Hozircha kirmasdan ko'rish" katalog qismlariga kirishsiz ruxsat berishi; Buyurtmalar, Profil, buyurtma berish
  kirishni talab qilishi SHART; kirgach xaridor o'tmoqchi bo'lgan joyga qaytarilishi SHART.
- **FR-013**: "Chiqish" sessiyani serverda yopishi va qurilmadan o'chirishi SHART; server javob bermasa ham qurilmadan o'chiriladi.
- **FR-014**: Ekrandagi barcha matnlar o'zbek va rus tillarida bo'lishi SHART.
- **FR-015**: Kirish ekrani qulay foydalanish talablariga mos bo'lishi SHART: bosiladigan elementlar kamida 44×44 pt,
  ekran o'quvchi uchun maydon nomlari, telefon klaviaturasi raqamli.

### Key Entities

- **Sessiya**: xaridorning shu qurilmadagi kirgan holati — qisqa muddatli ruxsat, uzoq muddatli yangilash kaliti, ularning tugash vaqtlari
  va xaridor qisqa ma'lumoti (telefon, ism, profil to'liqmi).
- **Kod so'rovi**: kod yuborilgan raqam, kanal (Telegram/SMS), kod uzunligi, amal qilish va qayta yuborish vaqti.
- **Mehmon rejimi**: xaridor kirmasdan ko'rishni tanlagani haqidagi belgi.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Yangi xaridor ilovani ochgandan to bosh sahifagacha (kod kelish vaqtini hisobga olmaganda) 30 soniyadan kam vaqtda kiradi.
- **SC-002**: Kirgan xaridordan 30 kun davomida faol foydalanganda qayta kod so'ralmaydi.
- **SC-003**: Bir vaqtda 5 ta so'rov ruxsat eskirganini aniqlaganda ham sessiya yopilib qolmaydi (0 ta kutilmagan chiqib ketish).
- **SC-004**: Sinov xaridorlarining 90% birinchi urinishda yordamsiz kira oladi.
- **SC-005**: Kirish ma'lumotlari qurilmaning himoyalanmagan xotirasida 0 marta uchraydi (tekshiruvda topilmaydi).

## Assumptions

- Server tomoni (kod yuborish, tekshirish, yangilash, chiqish) tayyor va o'zgarmaydi: kod 5 xonali, 5 daqiqa amal qiladi,
  5 urinish, qayta yuborish 60 soniyadan keyin; yangilash kaliti har safar almashadi va eskisi qayta ishlatilsa rad etiladi.
- Profil to'liq emasligi (ism yo'q) holatida ism so'rash bu qismga kirmaydi — 4.6 "Shaxsiy ma'lumotlar"da qilinadi;
  bu yerda faqat belgi saqlanadi.
- Telefon raqamini almashtirish, push-bildirishnomalar va akkauntni o'chirish bu qismga kirmaydi.
- Foydalanish shartlari va maxfiylik siyosati sahifalari 5.1-qadamda tayyorlanadi; hozircha havolalar mavjud bo'ladi, manzil keyin beriladi.
- Bosh sahifa va boshqa tablar hozircha vaqtinchalik ekranlar (4.1); haqiqiy katalog 4.3 da.
- Ilova Android va iOS uchun; yaqin sinov — Android emulyator va lokal server.
