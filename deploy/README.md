# PosPlatform — Production deploy qo'llanmasi

Bu papkada sotuvga/ishlatishga chiqarish (production) uchun kerakli sozlama va skriptlar.

## 1. Maxfiy sozlamalar (MAJBURIY)

Ilova `Production` muhitida **placeholder sirlar bilan ishga tushmaydi** (ataylab — xavfsizlik uchun).
Quyidagilarni muhit o'zgaruvchilari orqali bering (appsettings.json'ni tahrir qilmang, sirlarni git'ga commit qilmang):

```powershell
setx /M ASPNETCORE_ENVIRONMENT "Production"
setx /M Jwt__Key "<kamida 32 belgilik tasodifiy maxfiy kalit>"
setx /M ConnectionStrings__DefaultConnection "Host=localhost;Port=5432;Database=pos_platform;Username=pos_app;Password=<kuchli_parol>"
setx /M Seed__AdminPassword "<kuchli_parol>"
setx /M Seed__SuperAdminPassword "<kuchli_parol>"
setx /M Agent__ApiKey "<tasodifiy_kalit>"
```

Tasodifiy kalit yaratish: `[Convert]::ToBase64String((1..48 | % {Get-Random -Max 256}))`

Birinchi kirgandan keyin `admin` va `superadmin` parollarini almashtiring.

## 2. HTTPS — reverse proxy (nginx)

Ilova ichki tarmoqda **faqat HTTP, faqat localhost**da ishlaydi; TLS'ni nginx terminatsiya qiladi.

1. Ilovani localhost:5000 ga bog'lang (`ASPNETCORE_URLS=http://127.0.0.1:5000`).
2. `nginx.conf` namunasini moslang (domen + sertifikat yo'llari) va nginx'ga ulang.
3. Sertifikat: [certbot](https://certbot.eff.org/) (Let's Encrypt) bilan bepul oling.

Ilova `ForwardedHeaders` (X-Forwarded-Proto / X-Forwarded-For) ni o'qiydi, shuning uchun
HTTPS sxema va haqiqiy client IP (rate-limit/audit uchun) to'g'ri aniqlanadi.

> Windows/IIS ishlatsangiz: ARR + URL Rewrite bilan ham xuddi shu sxema (reverse proxy + TLS).

## 3. Kunlik avtomatik zaxira (backup)

```powershell
# Administrator PowerShell:
setx /M PGPASSWORD "<postgres_parol>"
.\register-backup-task.ps1 -Time "02:30" -BackupDir "D:\Backups\pos" -RetentionDays 30
```

- `backup-db.ps1` — `pg_dump` (custom format, gzip) bilan zaxira oladi, eskisini tozalaydi.
- Tiklash: `pg_restore --clean --if-exists -d pos_platform <fayl.dump>`
- Zaxiralarni boshqa diskka/serverga ham nusxalashni tavsiya qilamiz.

## 4. Ichiga o'rnatilgan xavfsizlik (kod darajasida)

- **Ko'p-ijarachilik izolyatsiyasi** — har tashkilot faqat o'z ma'lumotini ko'radi (EF global query filter).
- **Obuna tekshiruvi har so'rovda** — muddati tugagan/bloklangan foydalanuvchi darhol 401 oladi (faqat login emas).
- **Login rate-limit** — bir IP'dan daqiqasiga 10 urinish (brute-force himoyasi), 429 qaytaradi.
- **Audit jurnali** — login (muvaffaqiyat/xato), obuna va hisob o'zgarishlari yoziladi; SuperAdmin `/api/audit` orqali ko'radi.
- **Production sir-qo'riqchisi** — yuqorida (1-bo'lim).

## 5. Tavsiya etiladigan keyingi qadamlar

- PostgreSQL uchun alohida cheklangan huquqli foydalanuvchi (postgres superuser emas).
- Zaxiralarni masofaviy joyga (boshqa server/bulut) replikatsiya qilish.
- Monitoring/alert (ilova ishlayotganini kuzatish).
