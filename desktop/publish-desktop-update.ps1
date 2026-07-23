# Desktop POS yangi versiyasini tayyorlaydi: build + zip (+ ixtiyoriy: to'g'ridan-to'g'ri serverga yuklash).
#
# Chiqish DOIM bir xil ikkita fayl (versiya fayl nomida emas, version.json ichida):
#   desktop\publish\PosPlatform.Desktop.zip
#   desktop\publish\version.json      -> {"version":"1.0.6","sizeBytes":...,"builtAt":"..."}
# Har ishga tushganda desktop\publish ichidagi ESKI zip va build papkalari o'chiriladi —
# proyektda hech qachon bittadan ortiq zip yotmaydi.
#
# Ishlatish:
#   .\publish-desktop-update.ps1 -Version 1.0.6            # faqat zip yasaydi
#   .\publish-desktop-update.ps1 -Version 1.0.6 -Upload    # zip yasab, serverga ham yuklaydi
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

$proj      = Join-Path $PSScriptRoot "PosPlatform.Desktop\PosPlatform.Desktop.csproj"
$publishRoot = Join-Path $PSScriptRoot "publish"
$outDir    = Join-Path $publishRoot "app"                       # vaqtinchalik build chiqishi
$zip       = Join-Path $publishRoot "PosPlatform.Desktop.zip"   # DOIMIY nom
$metaPath  = Join-Path $publishRoot "version.json"

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null

# ── Eski chiqishlarni tozalash ──────────────────────────────────────────────
# Versiya nomli eski ziplar va build papkalari (PosPlatform.Desktop-1.0.5 ...) qolmasin.
Get-ChildItem $publishRoot -Filter "*.zip" -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        Write-Host "Eski zip o'chirildi: $($_.Name)" -ForegroundColor DarkGray
        Remove-Item $_.FullName -Force
    }
Get-ChildItem $publishRoot -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        Write-Host "Eski build papkasi o'chirildi: $($_.Name)" -ForegroundColor DarkGray
        Remove-Item $_.FullName -Recurse -Force
    }

# ── Build ───────────────────────────────────────────────────────────────────
dotnet publish $proj -c Release -r win-x64 --self-contained $SelfContained /p:Version=$Version -o $outDir
if ($LASTEXITCODE -ne 0) { exit 1 }

Compress-Archive -Path "$outDir\*" -DestinationPath $zip
if (-not (Test-Path $zip)) { Write-Error "Zip yaratilmadi."; exit 1 }

# Build papkasi endi kerak emas — zipdan boshqa hech narsa qolmaydi.
Remove-Item $outDir -Recurse -Force

$meta = [ordered]@{
    version   = $Version
    fileName  = "PosPlatform.Desktop.zip"
    sizeBytes = (Get-Item $zip).Length
    builtAt   = (Get-Date).ToString("o")
}
$meta | ConvertTo-Json | Set-Content -Path $metaPath -Encoding utf8

Write-Host ""
Write-Host "Tayyor: $zip  (versiya $Version)"
Write-Host "        $metaPath"

if (-not $Upload) {
    Write-Host "Serverga yuklash: shu buyruqni -Upload bilan qayta ishga tushiring,"
    Write-Host "yoki qo'lda ikkala faylni: <backend ContentRoot>/updates/desktop/ ga ko'chiring"
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

# Avval zip (yarim yuklangan holatda eski version.json ko'rsatib turishi uchun),
# to'liq tushgach version.json — shunda klient hech qachon buzuq zipni olmaydi.
scp $zip "${Server}:${RemotePath}/PosPlatform.Desktop.zip.tmp"
if ($LASTEXITCODE -ne 0) { Write-Error "scp muvaffaqiyatsiz — zip yuklanmadi."; exit 1 }
ssh $Server "mv '$RemotePath/PosPlatform.Desktop.zip.tmp' '$RemotePath/PosPlatform.Desktop.zip'"
if ($LASTEXITCODE -ne 0) { Write-Error "Serverda zip nomini almashtirib bo'lmadi."; exit 1 }

scp $metaPath "${Server}:${RemotePath}/version.json"
if ($LASTEXITCODE -ne 0) { Write-Error "version.json yuklanmadi — server eski versiyani ko'rsatadi."; exit 1 }

# Serverda qolgan eski versiya nomli ziplar (o'tish davridan) — tozalaymiz.
ssh $Server "rm -f '$RemotePath'/PosPlatform.Desktop-*.zip"

Write-Host "Yuklandi: ${RemotePath}/PosPlatform.Desktop.zip (versiya $Version)"

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
                Write-Warning "Server boshqa versiyani qaytardi — version.json yangilanmagan bo'lishi mumkin."
            }
        }
    }
    catch {
        Write-Warning "Tekshirib bo'lmadi: $($_.Exception.Message)"
    }
}
