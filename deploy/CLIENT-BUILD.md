# Kassa kompyuteri uchun build/publish — Desktop + Agent

Bu yo'riqnoma **kassir kompyuteriga** (Windows) o'rnatiladigan ikki ilova uchun:
- **Desktop** (`PosCloud.Desktop`) — kassir sotuv interfeysi (WPF)
- **Agent** (`PosCloud.Agent`) — lokal fiskal modul + chek printeri bilan ishlaydi

Ikkalasi ham cloud backend `https://poscloud.uz` ga ulanadi (allaqachon sozlangan).

> **Build qayerda qilinadi?** Sizning ishchi (developer) mashinangizda **.NET 8 SDK** bilan
> publish qilasiz, natija papkasini kassa kompyuteriga ko'chirasiz. Kassa
> kompyuterida **.NET o'rnatish shart emas** — `--self-contained` runtime'ni ichiga oladi.

---

## 1. Desktop (kassir ilovasi) — o'rnatish

Desktop **Velopack** installer bilan o'rnatiladi va keyin o'zi yangilanadi
(chiqarish: `DEPLOY-CHECKLIST.md` → 6-bo'lim, `desktop\publish-desktop-update.ps1`).

- Kassa kompyuterida yuklab oling va ishga tushiring:
  `https://poscloud.uz/updates/velopack/PosCloud-win-Setup.exe`
  (yoki dev mashinadagi `desktop\publish\releases\PosCloud-win-Setup.exe` ni ko'chiring).
- Admin huquqi kerak emas: dastur `%LocalAppData%\PosCloud` ga o'rnatiladi, ish stoli va
  Start menyuga **PosCloud** yorlig'i qo'yiladi va o'zi ochiladi → login → `admin` yoki kassir hisobi.
- Yangi versiya chiqqanda ⚙ tugmasida qizil belgi paydo bo'ladi → Sozlamalar → **⬇ Yangilash**.
- O'chirish: Windows "Programs and Features" → PosCloud.

> **Printer/skaner** sozlamalari ilovaning ichidagi **Settings** oynasidan
> (`%APPDATA%\PosPlatform\settings.json` ga saqlanadi — yangilanishda yo'qolmaydi).
> Har kassa o'z printeriga moslaydi.

---

## 2. Agent (fiskal + printer) — publish

```powershell
dotnet publish agent/PosCloud.Agent/PosCloud.Agent.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish/agent
```

Natija: `publish/agent/` — `PosCloud.Agent.exe` + `appsettings.json`.
Papkani kassa kompyuteriga ko'chiring (masalan `C:\PosCloud\Agent`).

### Agent sozlamalari (MUHIM) — `appsettings.json`

| Sozlama | Qiymat |
|---------|--------|
| `Agent:BackendBaseUrl` | `https://poscloud.uz` (tayyor) |
| `Agent:ApiKey` | **Backend serverdagi `Agent__ApiKey` bilan AYNI bir xil bo'lishi shart** |
| `Agent:FiscalMode` | `YtModule` (YT.UZ haqiqiy soliq QR) yoki test uchun `Simulated` |
| `Agent:FiscalDriveApiUrl` | `http://127.0.0.1:3448/rpc/api` (YT modul lokal) |
| `Agent:PrinterMode` | `Network` (IP:9100), `Usb` (Windows printer nomi) yoki `Console` (test) |
| `Agent:PrinterAddress` / `PrinterName` | printer turiga qarab |

> ⚠️ **ApiKey** — bu maxfiy kalit. Backend env'dagi `Agent__ApiKey` qiymatini shu yerga
> qo'ying, aks holda agent `401` oladi va cheklarni ololmaydi.

---

## 3. Agent'ni avtomatik ishga tushirish (Windows)

Agent doim ishlab turishi kerak (kassa yoqilganda o'zi boshlanishi uchun). Ikki yo'l:

### A) Task Scheduler — "at logon" (oddiy)

```powershell
$exe = "C:\PosCloud\Agent\PosCloud.Agent.exe"
schtasks /Create /TN "PosCloudAgent" /TR "$exe" /SC ONLOGON /RL HIGHEST /F
# Darhol ishga tushirish:
schtasks /Run /TN "PosCloudAgent"
```

### B) Windows Service (NSSM bilan — barqarorroq)

[NSSM](https://nssm.cc/) yuklab oling, so'ng:

```powershell
nssm install PosCloudAgent "C:\PosCloud\Agent\PosCloud.Agent.exe"
nssm set PosCloudAgent AppDirectory "C:\PosCloud\Agent"
nssm set PosCloudAgent Start SERVICE_AUTO_START
nssm start PosCloudAgent
```

> Agent fon-servis (Worker) — UI yo'q. Ishlayotganini loglardan yoki backendda
> savdo qilib (chek chiqishidan) tekshiriladi.

---

## 4. Tekshirish (kassa kompyuterida)

- [ ] Internet bor, `https://poscloud.uz` brauzerda ochiladi.
- [ ] Desktop ilova ochiladi, login bo'ladi (cloud'dagi hisob bilan).
- [ ] Test savdo qiling → Agent uni fiskalizatsiya qilib chek chiqaradi (printer ulangan bo'lsa).
- [ ] Backendda savdoning `Receipt` holati `Registered` ga o'tadi.

---

## 5. Yangilanish (keyingi versiyalar)

Kod o'zgargach, har kassada faqat **exe'ni almashtiring**:
1. Developer mashinasida yana `dotnet publish ...` (yuqoridagi buyruqlar).
2. Eski `.exe` (va o'zgargan fayllar) o'rniga yangisini ko'chiring.
3. Agent service'ni qayta ishga tushiring (`nssm restart PosCloudAgent` yoki Task'ni qayta `Run`).

> **appsettings.json**'ni almashtirmang (har kassaning printer/ApiKey sozlamasi unda turadi) —
> faqat `.exe` va kod fayllarini yangilang. Agar config strukturasi o'zgarsa — qo'lda moslang.
