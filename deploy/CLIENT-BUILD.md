# Kassa kompyuteri uchun build/publish — Desktop + Agent

Bu yo'riqnoma **kassir kompyuteriga** (Windows) o'rnatiladigan ikki ilova uchun:
- **Desktop** (`PosPlatform.Desktop`) — kassir sotuv interfeysi (WPF)
- **Agent** (`PosPlatform.Agent`) — lokal fiskal modul + chek printeri bilan ishlaydi

Ikkalasi ham cloud backend `https://poscloud.uz` ga ulanadi (allaqachon sozlangan).

> **Build qayerda qilinadi?** Sizning ishchi (developer) mashinangizda **.NET 8 SDK** bilan
> publish qilasiz, natija papkasini kassa kompyuteriga ko'chirasiz. Kassa
> kompyuterida **.NET o'rnatish shart emas** — `--self-contained` runtime'ni ichiga oladi.

---

## 1. Desktop (kassir ilovasi) — publish

Developer mashinasida (loyiha ildizidan):

```powershell
dotnet publish desktop/PosPlatform.Desktop/PosPlatform.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish/desktop
```

Natija: `publish/desktop/` — ichida `PosPlatform.Desktop.exe` + `appsettings.json`.

- `appsettings.json` allaqachon `BaseUrl = https://poscloud.uz`.
- Butun `publish/desktop` papkani kassa kompyuteriga ko'chiring (masalan `C:\PosPlatform\Desktop`).
- `PosPlatform.Desktop.exe` ni ishga tushiring → login oynasi → `admin` yoki kassir hisobi.
- Ish stoli yorlig'i (shortcut) yarating: `.exe` ustiga o'ng tugma → "Send to → Desktop".

> **Printer/skaner** sozlamalari ilovaning ichidagi **Settings** oynasidan yoki
> `appsettings.json` (`Printer:Name = POS-80`) orqali. Har kassa o'z printeriga moslaydi.

---

## 2. Agent (fiskal + printer) — publish

```powershell
dotnet publish agent/PosPlatform.Agent/PosPlatform.Agent.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish/agent
```

Natija: `publish/agent/` — `PosPlatform.Agent.exe` + `appsettings.json`.
Papkani kassa kompyuteriga ko'chiring (masalan `C:\PosPlatform\Agent`).

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
$exe = "C:\PosPlatform\Agent\PosPlatform.Agent.exe"
schtasks /Create /TN "PosPlatformAgent" /TR "$exe" /SC ONLOGON /RL HIGHEST /F
# Darhol ishga tushirish:
schtasks /Run /TN "PosPlatformAgent"
```

### B) Windows Service (NSSM bilan — barqarorroq)

[NSSM](https://nssm.cc/) yuklab oling, so'ng:

```powershell
nssm install PosPlatformAgent "C:\PosPlatform\Agent\PosPlatform.Agent.exe"
nssm set PosPlatformAgent AppDirectory "C:\PosPlatform\Agent"
nssm set PosPlatformAgent Start SERVICE_AUTO_START
nssm start PosPlatformAgent
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
3. Agent service'ni qayta ishga tushiring (`nssm restart PosPlatformAgent` yoki Task'ni qayta `Run`).

> **appsettings.json**'ni almashtirmang (har kassaning printer/ApiKey sozlamasi unda turadi) —
> faqat `.exe` va kod fayllarini yangilang. Agar config strukturasi o'zgarsa — qo'lda moslang.
