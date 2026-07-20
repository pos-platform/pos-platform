# Desktop POS yangi versiyasini tayyorlaydi: build + zip (+ ixtiyoriy: to'g'ridan-to'g'ri serverga yuklash).
#
# Ishlatish:
#   .\publish-desktop-update.ps1 -Version 1.0.5            # faqat zip yasaydi
#   .\publish-desktop-update.ps1 -Version 1.0.5 -Upload    # zip yasab, serverga ham yuklaydi
#
# Server manzilini har safar yozmaslik uchun shu papkada publish-target.json yarating
# (git'ga tushmaydi — publish-target.example.json dan nusxa oling):
#   {
#     "server":     "user@poscloud.uz",
#     "remotePath": "/opt/posplatform/updates/desktop",
#     "verifyUrl":  "https://poscloud.uz/api/desktop-update/version"
#   }
# Docker (compose bind mount) bo'lsa remotePath: "~/pos-platform/deploy/updates/desktop"
#
# Yuklangach hamma kassalar ⚙ belgisida yangilanishni ko'rib, o'zi yangilanadi —
# har bir kompyuterga alohida borish shart emas.
param(
    [Parameter(Mandatory = $true)][string]$Version,
    # Klientlarda .NET 8 o'rnatilmagan deb hisoblaymiz — runtime zip ichida bo'ladi.
    [bool]$SelfContained = $true,
    # Zipni serverga scp orqali yuklaydi (SSH kalitingiz sozlangan bo'lishi kerak).
    [switch]$Upload,
    [string]$Server,
    [string]$RemotePath,
    [string]$VerifyUrl
)

if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    Write-Error "Versiya x.y.z ko'rinishida bo'lsin (masalan 1.0.1)"
    exit 1
}

$proj = Join-Path $PSScriptRoot "PosPlatform.Desktop\PosPlatform.Desktop.csproj"
$outDir = Join-Path $PSScriptRoot "publish\PosPlatform.Desktop-$Version"
$zip = Join-Path $PSScriptRoot "publish\PosPlatform.Desktop-$Version.zip"

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

dotnet publish $proj -c Release -r win-x64 --self-contained $SelfContained /p:Version=$Version -o $outDir
if ($LASTEXITCODE -ne 0) { exit 1 }

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$outDir\*" -DestinationPath $zip

Write-Host ""
Write-Host "Tayyor: $zip"

if (-not $Upload) {
    Write-Host "Serverga yuklash: shu buyruqni -Upload bilan qayta ishga tushiring,"
    Write-Host "yoki qo'lda: <backend ContentRoot>/updates/desktop/PosPlatform.Desktop-$Version.zip"
    return
}

# ── Serverga yuklash ────────────────────────────────────────────────────────
$cfgPath = Join-Path $PSScriptRoot "publish-target.json"
if (Test-Path $cfgPath) {
    $cfg = Get-Content $cfgPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $Server)     { $Server = $cfg.server }
    if (-not $RemotePath) { $RemotePath = $cfg.remotePath }
    if (-not $VerifyUrl)  { $VerifyUrl = $cfg.verifyUrl }
}

if ([string]::IsNullOrWhiteSpace($Server) -or [string]::IsNullOrWhiteSpace($RemotePath)) {
    Write-Host "Server manzili berilmagan. Ikki yo'l bor:" -ForegroundColor Yellow
    Write-Host "  1) publish-target.json yarating (publish-target.example.json dan nusxa oling)"
    Write-Host "  2) yoki parametr bilan:"
    Write-Host "     -Server user@poscloud.uz -RemotePath /opt/posplatform/updates/desktop"
    Write-Error "Server / RemotePath berilmagan - yuklash bekor qilindi."
    exit 1
}

Write-Host ""
Write-Host "Serverga yuklanmoqda -> ${Server}:${RemotePath}"

# Papka yo'q bo'lsa yaratamiz (birinchi marta chiqarilayotgan bo'lishi mumkin).
ssh $Server "mkdir -p '$RemotePath'"
if ($LASTEXITCODE -ne 0) { Write-Error "ssh ulanmadi yoki papka yaratilmadi."; exit 1 }

scp $zip "${Server}:${RemotePath}/"
if ($LASTEXITCODE -ne 0) { Write-Error "scp muvaffaqiyatsiz — zip yuklanmadi."; exit 1 }

Write-Host "Yuklandi: ${RemotePath}/PosPlatform.Desktop-$Version.zip"

# ── Tekshirish: server endi shu versiyani qaytaryaptimi? ────────────────────
if (-not [string]::IsNullOrWhiteSpace($VerifyUrl)) {
    Write-Host ""
    try {
        $resp = Invoke-WebRequest $VerifyUrl -UseBasicParsing -TimeoutSec 25
        if ($resp.StatusCode -eq 204) {
            Write-Warning "Server 204 qaytardi — zip ko'rinmayapti. RemotePath backend ContentRoot/updates/desktop ga mos kelmasligi mumkin."
        }
        else {
            Write-Host "Server javobi: $($resp.Content)"
            if ($resp.Content -match [regex]::Escape($Version)) {
                Write-Host "OK — kassalar endi $Version ni ko'radi." -ForegroundColor Green
            }
            else {
                Write-Warning "Server boshqa versiyani qaytardi — yuqoriroq versiya zipi turgan bo'lishi mumkin."
            }
        }
    }
    catch {
        Write-Warning "Tekshirib bo'lmadi: $($_.Exception.Message)"
    }
}
