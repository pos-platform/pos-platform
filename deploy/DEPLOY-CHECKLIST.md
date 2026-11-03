# PosCloud — Deploy checklist (devops uchun)

Bir sahifalik amaliy ro'yxat. Batafsil izoh: [README.md](README.md).
Ikki yo'l bor — **A) Linux + systemd** yoki **B) Docker**. Bittasini tanlang.

---

## 0. Server tayyorlash (umumiy)

- [ ] Server: Ubuntu 22.04+ (yoki Windows Server)
- [ ] Domen DNS `A` yozuvi serverga yo'naltirilgan: **`poscloud.uz`** (va `www.poscloud.uz`)
- [ ] Portlar: tashqaridan faqat **80/443** ochiq; **5000 faqat localhost**
- [ ] **PostgreSQL 14+** o'rnatilgan va ishlayapti

---

## 1. Baza

- [ ] `pos_platform` bazasini yarating:
  ```bash
  sudo -u postgres createdb pos_platform
  ```
- [ ] Cheklangan user yarating (skript tayyor) — avval `CHANGE_ME_strong_password` ni almashtiring:
  ```bash
  sudo -u postgres psql -d postgres -f deploy/db-restricted-user.sql
  ```
- [ ] Connection string'da shu user'dan foydalaning (superuser `postgres` emas).
- [ ] **Migratsiya qo'lda kerak emas** — ilova birinchi ishga tushganda o'zi qo'llaydi (+ admin/superadmin seed qiladi).

---

## 2. Maxfiy sozlamalar (env) — MAJBURIY

`appsettings.json` ni tahrir QILMANG. Quyidagilarni environment variable qiling
(placeholder sir bilan Production'da ilova ishga tushmaydi):

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
Jwt__Key=<kamida 32 belgilik tasodifiy kalit>
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=pos_platform;Username=posplatform_app;Password=<kuchli_parol>
Seed__AdminPassword=<kuchli_parol>
Seed__SuperAdminPassword=<kuchli_parol>
Agent__ApiKey=<tasodifiy_kalit>
```

To'lov ishlatilsa qo'shing: `Payme__MerchantId`, `Payme__SecretKey`,
`Click__ServiceId`, `Click__MerchantId`, `Click__SecretKey`.

Tasodifiy kalit (Linux): `openssl rand -base64 48`

---

## 3A. YO'L A — Linux + systemd (Docker'siz)

- [ ] **.NET 8 ASP.NET Core Runtime** o'rnating (SDK shart emas).
- [ ] Frontend build (Node 18+ kerak, faqat build paytida) — natija `frontend/dist`:
  ```bash
  cd frontend && npm ci && npm run build
  # Vite dist/ ga build qiladi; uni backend wwwroot ga ko'chiring:
  rm -rf ../backend/src/PosCloud.Api/wwwroot/* 
  cp -r dist/* ../backend/src/PosCloud.Api/wwwroot/
  ```
- [ ] Backend publish (wwwroot ham publish natijasiga kiradi):
  ```bash
  cd backend
  dotnet publish src/PosCloud.Api -c Release -o /opt/posplatform
  ```
- [ ] `wwwroot/index.html` (Vue build) `/opt/posplatform/wwwroot` ichida ekanini tekshiring.
- [ ] `deploy/posplatform.service` ni `/etc/systemd/system/` ga qo'ying,
      ichidagi env qiymatlarni to'ldiring, keyin:
  ```bash
  sudo systemctl daemon-reload
  sudo systemctl enable --now posplatform
  sudo systemctl status posplatform
  ```

---

## 3B. YO'L B — Docker

- [ ] `deploy/Dockerfile` va `deploy/docker-compose.yml` ishlating.
- [ ] `.env` faylga sirlarni yozing (compose o'qiydi), keyin:
  ```bash
  cd deploy
  docker compose up -d --build
  docker compose logs -f api
  ```
  (Postgres ham compose ichida; tashqi DB ishlatsangiz `db` servisini olib tashlang.)

---

## 4. Nginx (HTTPS) — har ikki yo'l uchun

- [ ] `deploy/nginx.conf` ni `/etc/nginx/sites-available/posplatform` ga nusxalang
      (domen `poscloud.uz` allaqachon sozlangan).
- [ ] TLS sertifikat (apex + www):
  ```bash
  sudo certbot --nginx -d poscloud.uz -d www.poscloud.uz
  ```
- [ ] `sudo nginx -t && sudo systemctl reload nginx`

---

## 5. Tekshirish

- [ ] `curl -f http://127.0.0.1:5000/health` → `Healthy`
- [ ] `https://poscloud.uz` ochiladi, login: `admin` / (siz qo'ygan parol)
- [ ] **Birinchi kirgandan keyin `admin` va `superadmin` parolini almashtiring.**

---

## 6. Kassa (desktop) yangilanishini chiqarish

Kassa ilovasi **Velopack** bilan yangilanadi. Yangilanish **majburiy emas**: kassa serverni
tekshirib ⚙ tugmasida qizil belgi ko'rsatadi, kassir Sozlamalar → **⬇ Yangilash** ni bosganda
yuklab o'rnatadi (delta — odatda bir necha MB) va dastur o'zi qayta ochiladi.

Kassalar `https://poscloud.uz/updates/velopack/releases.win.json` ni o'qiydi. Backend bu
papkani statik beradi (`Program.cs`, auth talab qilinmaydi):

```
<ContentRoot>/updates/velopack/releases.win.json       <- versiyalar ro'yxati (oxirida yuklanadi)
<ContentRoot>/updates/velopack/PosCloud-x.y.z-full.nupkg
<ContentRoot>/updates/velopack/PosCloud-x.y.z-delta.nupkg
<ContentRoot>/updates/velopack/PosCloud-win-Setup.exe  <- yangi kassaga o'rnatish
```

- [ ] Bir marta: `desktop\publish-target.json` yarating (`publish-target.example.json` dan nusxa)
      va `velopackPath` ni moslang:
  - **YO'L A (systemd):** `/opt/posplatform/updates/velopack`
  - **YO'L B (Docker):** `~/pos-platform/deploy/updates/velopack` (compose `./updates` ni bind mount qiladi)
- [ ] `PosCloud.Desktop.csproj` dagi `<Version>` dan katta versiya bilan chiqaring:
  ```powershell
  cd desktop
  .\publish-desktop-update.ps1 -Version 1.0.9 -Upload
  ```
  Skript: build → serverdagi oldingi versiyani oladi → `vpk pack` (full + delta + Setup.exe) →
  har faylni `.tmp` bilan yuklab nomini almashtiradi → `releases.win.json` eng oxirida →
  eski paketlarni o'chiradi → yangi versiya serverda ko'rinishini tekshiradi.
- [ ] Tekshiring:
  ```bash
  curl https://poscloud.uz/updates/velopack/releases.win.json
  # {"Assets":[{"PackageId":"PosCloud","Version":"1.0.9","Type":"Full",...}]}
  ```
- [ ] Kassada: ⚙ Sozlamalar → "Yangi versiya mavjud" → **⬇ Yangilash**.

### O'tish davri: 1.0.8 va undan eski kassalar

Eski kassalar faqat `api/desktop-update` (zip + `version.json`, `<ContentRoot>/updates/desktop/`)
ni biladi. Shuning uchun skript hozircha **eski formatni ham** yasab, `remotePath` ga yuklaydi.
Eski kassa oddiy usulda yangi versiyani oladi, so'ng Sozlamalarda bir martalik o'tishni taklif
qiladi: `PosCloud-win-Setup.exe` yuklanadi va dastur `%LocalAppData%\PosCloud` ga qayta
o'rnatiladi (sozlamalar, parol, oflayn baza `%APPDATA%`/`%LOCALAPPDATA%\PosPlatform` da —
yo'qolmaydi). Eski papkadagi yorliq bosilsa ham dastur yangi o'rnatilgan nusxani ochadi.

Hamma kassalar o'tgach: skriptni `-SkipLegacy` bilan ishlating, `DesktopUpdateController`
va `updates/desktop` papkasini olib tashlash mumkin.

> Docker'da `/app/updates` bind mount qilingan, shuning uchun `docker compose up -d --build`
> dan keyin ham fayllar saqlanadi.

---

## 7. Backup

- [ ] Kunlik `pg_dump` cron (Linux) yoki Task Scheduler (Windows: `register-backup-task.ps1`).
- [ ] Linux misol (cron `02:30`):
  ```bash
  30 2 * * * pg_dump -Fc pos_platform | gzip > /backups/pos_$(date +\%F).dump.gz
  ```
- [ ] Zaxirani boshqa diskka/serverga nusxalang.
