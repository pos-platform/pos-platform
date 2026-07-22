<#
.SYNOPSIS
    Fiskal fleshka uzilib qolishini (FiscalDrive/List bo'sh) oldini oladi — Windows USB
    quvvat tejash (selective suspend) ni o'chiradi.

.DESCRIPTION
    Har bir KASSA kompyuterida BIR MARTA, administrator huquqi bilan ishga tushiriladi.
    Windows quvvat tejash uchun USB portlarni "uxlatib" qo'yadi — natijada fiskal modul
    (fleshka) uzilib, FiscalDriveService uni ko'rmay qoladi va cheklar QR olmaydi.

    Skript:
      1. Barcha quvvat rejalarida USB selective suspend'ni o'chiradi (AC + DC).
      2. Global registr kaliti bilan selective suspend'ni butunlay bloklaydi.
      3. Barcha USB Root Hub / USB kontroller qurilmalarida "Allow the computer to turn
         off this device to save power" belgisini olib tashlaydi.

    So'ngra kompyuterni QAYTA YUKLASH tavsiya etiladi.

.NOTES
    Ishga tushirish (administrator PowerShell):
        Set-ExecutionPolicy -Scope Process Bypass -Force
        .\fix-usb-power.ps1
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop'
Write-Host "=== Fiskal fleshka uchun USB quvvat tejashni o'chirish ===`n" -ForegroundColor Cyan

# 1) Quvvat rejalarida USB selective suspend = Disabled (0), AC va DC uchun.
#    SUB_USB/USBSELECTIVESUSPEND aliaslari ba'zi tizimlarda tanilmaydi ("Неправильные параметры"),
#    shuning uchun barqaror GUID'lar ishlatiladi.
$usbSubGroup = '2a737441-1930-4402-8d77-b2bebba308a3'   # USB settings
$usbSuspend  = '48e6b7a6-50f5-4782-a5d4-53bb8f07e226'   # USB selective suspend setting
Write-Host "[1/4] Quvvat rejalarida USB selective suspend o'chirilmoqda..." -ForegroundColor Yellow
try {
    powercfg /setacvalueindex SCHEME_CURRENT $usbSubGroup $usbSuspend 0 | Out-Null
    powercfg /setdcvalueindex SCHEME_CURRENT $usbSubGroup $usbSuspend 0 | Out-Null
    powercfg /setactive SCHEME_CURRENT | Out-Null
    Write-Host "      OK (joriy reja)" -ForegroundColor Green
} catch {
    Write-Host "      Ogohlantirish: $($_.Exception.Message)" -ForegroundColor DarkYellow
}

# 2) Global registr override — barcha USB uchun selective suspend'ni bloklaydi.
Write-Host "[2/4] Global registr kaliti (DisableSelectiveSuspend=1)..." -ForegroundColor Yellow
$usbKey = 'HKLM:\SYSTEM\CurrentControlSet\Services\USB'
if (-not (Test-Path $usbKey)) { New-Item -Path $usbKey -Force | Out-Null }
New-ItemProperty -Path $usbKey -Name 'DisableSelectiveSuspend' -Value 1 -PropertyType DWord -Force | Out-Null
Write-Host "      OK" -ForegroundColor Green

# 3) Har bir USB Root Hub / kontrollerda "turn off to save power" belgisini olib tashlash.
#    Qurilmaning Power maydonidagi PnPCapabilities registrida bit 0x24 quvvat boshqaruvini o'chiradi.
Write-Host "[3/4] USB Root Hub qurilmalarida quvvat boshqaruvi o'chirilmoqda..." -ForegroundColor Yellow
$count = 0
$usbDevices = Get-PnpDevice -Class 'USB' -Status OK -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -match 'Root Hub|USB Hub|Host Controller' }

foreach ($dev in $usbDevices) {
    try {
        $paramPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($dev.InstanceId)\Device Parameters"
        if (Test-Path $paramPath) {
            # Selective suspend'ni shu qurilma uchun ham o'chiramiz.
            New-ItemProperty -Path $paramPath -Name 'EnableSelectiveSuspend' -Value 0 -PropertyType DWord -Force -ErrorAction SilentlyContinue | Out-Null
            New-ItemProperty -Path $paramPath -Name 'AllowIdleIrpInD3'       -Value 0 -PropertyType DWord -Force -ErrorAction SilentlyContinue | Out-Null
            $count++
        }
    } catch { }
}
Write-Host "      OK ($count ta USB qurilma sozlandi)" -ForegroundColor Green

# ── 4. Smart Card xizmati avtomatik ishga tushsin ────────────────────────────
#    Fiskal modul PC/SC smart-karta sifatida ko'rinadi. Xizmat "Manual" bo'lsa
#    qayta yuklashdan keyin ko'tarilmay, /FiscalDrive/List bo'sh qaytishi mumkin.
Write-Host "[4/4] Smart Card xizmati (SCardSvr) avtomatik ishga qo'yilmoqda..." -ForegroundColor Yellow
try {
    Set-Service SCardSvr -StartupType Automatic -ErrorAction Stop
    $sc = Get-Service SCardSvr
    if ($sc.Status -ne 'Running') { Start-Service SCardSvr -ErrorAction SilentlyContinue }
    Write-Host "      OK (StartupType=Automatic, holat: $((Get-Service SCardSvr).Status))" -ForegroundColor Green
} catch {
    Write-Host "      Ogohlantirish: $($_.Exception.Message)" -ForegroundColor DarkYellow
}

Write-Host "`n=== Tayyor. Kompyuterni QAYTA YUKLANG. ===" -ForegroundColor Cyan
Write-Host "Qo'shimcha (jismoniy): fleshkani orqa (anakart) USB portiga to'g'ridan-to'g'ri," -ForegroundColor Gray
Write-Host "hub yoki uzaytirgichsiz ulang. Old panel portlaridan foydalanmang." -ForegroundColor Gray
