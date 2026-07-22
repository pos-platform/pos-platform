<#
.SYNOPSIS
    "Fiskal modul topilmadi (FiscalDrive/List bo'sh)" xatosining sababini aniqlaydi.

.DESCRIPTION
    Kassa kompyuterida ishga tushiriladi. Fiskal modul zanjirining har bir bo'g'inini
    ketma-ket tekshiradi va aynan qaysi bo'g'in uzilganini aytadi:

        fleshka(smart-karta) -> o'quvchi(reader) -> Windows Smart Card xizmati
        -> FiscalDriveService (port 3449) -> agent

    Administrator huquqi shart emas (xizmatni ishga tushirishdan tashqari).

.NOTES
    Ishga tushirish:
        Set-ExecutionPolicy -Scope Process Bypass -Force
        .\check-fiscal-module.ps1
#>

$ErrorActionPreference = 'Continue'
$fdsUrl = 'http://127.0.0.1:3449'
$problems = New-Object System.Collections.ArrayList

function Write-Step($n, $text) { Write-Host "`n[$n] $text" -ForegroundColor Cyan }
function Write-Ok($text)       { Write-Host "    OK  - $text" -ForegroundColor Green }
function Write-Bad($text)      { Write-Host "    XATO - $text" -ForegroundColor Red; [void]$problems.Add($text) }
function Write-Note($text)     { Write-Host "        $text" -ForegroundColor Gray }

Write-Host "=== Fiskal modul tashxisi ===" -ForegroundColor Cyan
Write-Host "Kompyuter: $env:COMPUTERNAME   Vaqt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# ── 1. Windows Smart Card xizmati ────────────────────────────────────────────
Write-Step 1 "Windows Smart Card xizmati (SCardSvr)"
$scard = Get-Service -Name SCardSvr -ErrorAction SilentlyContinue
if ($null -eq $scard) {
    Write-Bad "SCardSvr xizmati topilmadi (Windows komponenti yo'q?)"
} elseif ($scard.Status -ne 'Running') {
    Write-Bad "SCardSvr ISHLAMAYAPTI (holat: $($scard.Status)) — modul shu sababli ko'rinmaydi"
    Write-Note "Tuzatish (administrator PowerShell):"
    Write-Note "  Set-Service SCardSvr -StartupType Automatic; Start-Service SCardSvr"
} else {
    Write-Ok "ishlayapti"
}

# ── 2. Smart-karta o'quvchilari (fiskal modul shu orqali ko'rinadi) ──────────
Write-Step 2 "Smart-karta o'quvchilari (Smart card readers)"
$readers = Get-PnpDevice -Class SmartCardReader -ErrorAction SilentlyContinue
if (-not $readers) {
    Write-Bad "Hech qanday smart-karta o'quvchi topilmadi — fiskal fleshka ULANMAGAN yoki drayveri yo'q"
    Write-Note "Fleshkani orqa (anakart) USB portiga to'g'ridan-to'g'ri ulang, hub/uzaytirgichsiz."
    Write-Note "Qurilma menejerida 'Other devices' da sariq '!' bor-yo'qligini tekshiring — bo'lsa drayver kerak."
} else {
    foreach ($r in $readers) {
        if ($r.Status -eq 'OK') { Write-Ok "$($r.FriendlyName)" }
        else {
            Write-Bad "$($r.FriendlyName) — holat: $($r.Status)"
            Write-Note "Drayver muammosi. Qurilma menejerida o'chirib-yoqing yoki drayverni qayta o'rnating."
        }
    }
}

# ── 3. Noma'lum / muammoli USB qurilmalar ────────────────────────────────────
Write-Step 3 "Muammoli qurilmalar (sariq '!')"
$bad = Get-PnpDevice -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'Error' -or $_.Status -eq 'Unknown' }
if ($bad) {
    foreach ($b in $bad) { Write-Bad "$($b.FriendlyName) [$($b.Class)] — $($b.Status)" }
    Write-Note "Bular orasida fiskal o'quvchi bo'lishi mumkin — drayverini o'rnating."
} else {
    Write-Ok "muammoli qurilma yo'q"
}

# ── 4. FiscalDriveService jarayoni va porti ──────────────────────────────────
Write-Step 4 "FiscalDriveService (drayver dasturi, port 3449)"
$proc = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match 'Fiscal|FDS' }
if ($proc) { foreach ($p in $proc) { Write-Ok "jarayon: $($p.ProcessName) (PID $($p.Id))" } }
else       { Write-Bad "FiscalDriveService jarayoni topilmadi — dastur ishga tushmagan" }

$port = Get-NetTCPConnection -LocalPort 3449 -State Listen -ErrorAction SilentlyContinue
if ($port) { Write-Ok "port 3449 tinglanmoqda" }
else       { Write-Bad "port 3449 YOPIQ — FiscalDriveService ishlamayapti" }

# ── 5. Asosiy test: /FiscalDrive/List ────────────────────────────────────────
Write-Step 5 "Modul ro'yxati: POST $fdsUrl/FiscalDrive/List"
try {
    $list = Invoke-RestMethod -Method Post -Uri "$fdsUrl/FiscalDrive/List" -TimeoutSec 10
    if ($null -eq $list -or @($list).Count -eq 0) {
        Write-Bad "Ro'yxat BO'SH — drayver ishlayapti, lekin modulni ko'rmayapti"
        Write-Note "Ya'ni muammo yuqoridagi 1-3 bosqichlarda: fleshka ulanmagan, o'quvchi drayveri yo'q,"
        Write-Note "yoki Smart Card xizmati o'chiq. Fleshkani chiqarib-qayta ulab, skriptni qayta ishlating."
    } else {
        foreach ($m in @($list)) {
            Write-Ok "MODUL TOPILDI: FactoryID=$($m.FactoryID)  applet=$($m.AppletVersion)  reader=$($m.ReaderName)"
            if ($m.AppletVersion -ne '0400') {
                Write-Note "DIQQAT: applet '$($m.AppletVersion)' — bu loyiha 0400 uchun sozlangan."
            }
        }
    }
} catch {
    Write-Bad "So'rov bajarilmadi: $($_.Exception.Message)"
    Write-Note "FiscalDriveService ishga tushganini va 3449-portda ekanini tekshiring."
}

# ── 6. Agent sozlamasi (terminal mosligi) ────────────────────────────────────
Write-Step 6 "Agent sozlamasi (appsettings.json)"
$cfg = Get-ChildItem -Path "C:\" -Filter 'appsettings.json' -Recurse -ErrorAction SilentlyContinue |
       Where-Object { $_.FullName -match 'Agent' } | Select-Object -First 1
if ($cfg) {
    Write-Ok "topildi: $($cfg.FullName)"
    try {
        $json = Get-Content $cfg.FullName -Raw | ConvertFrom-Json
        Write-Note "FiscalMode        = $($json.Agent.FiscalMode)"
        Write-Note "FiscalTerminalId  = $($json.Agent.FiscalTerminalId)"
        Write-Note "FiscalFactoryId   = $($json.Agent.FiscalFactoryId)"
        Write-Note "FiscalDriveServiceUrl = $($json.Agent.FiscalDriveServiceUrl)"
        Write-Note "-> FiscalTerminalId shu kassaning nomi (mas. KASSA-2) bilan bir xil bo'lishi shart."
    } catch { Write-Note "JSON o'qilmadi: $($_.Exception.Message)" }
} else {
    Write-Note "appsettings.json topilmadi — agent papkasini qo'lda tekshiring."
}

# ── Xulosa ───────────────────────────────────────────────────────────────────
Write-Host "`n=== XULOSA ===" -ForegroundColor Cyan
if ($problems.Count -eq 0) {
    Write-Host "Muammo topilmadi — modul ko'rinmoqda. Xato saqlansa, agent logini tekshiring." -ForegroundColor Green
} else {
    Write-Host "Topilgan muammolar ($($problems.Count)):" -ForegroundColor Red
    $i = 1
    foreach ($p in $problems) { Write-Host "  $i) $p" -ForegroundColor Red; $i++ }
    Write-Host "`nYuqoridan pastga qarab tuzating — birinchi muammo ko'pincha qolganlarining sababi." -ForegroundColor Yellow
}
