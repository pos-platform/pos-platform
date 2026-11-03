# Desktop POS yangi versiyasini chiqaradi (Velopack): build + vpk pack (+ ixtiyoriy: serverga yuklash).
#
# Natija — desktop\publish\releases\ :
#   releases.win.json              <- kassalar shuni o'qib yangi versiyani ko'radi (serverga OXIRIDA yuklanadi)
#   PosCloud-<ver>-full.nupkg      <- to'liq paket (~75 MB)
#   PosCloud-<ver>-delta.nupkg     <- faqat o'zgarishlar (odatda < 1 MB) — oldingi versiya serverdan olinadi
#   PosCloud-win-Setup.exe         <- yangi kassaga o'rnatish va eski kassalarni o'tkazish uchun
# O'tish davri uchun eski format ham yasaladi (Velopack'ga hali o'tmagan 1.0.8 va undan eski
# kassalar api/desktop-update orqali ko'radi). Hamma kassalar o'tgach -SkipLegacy bilan ishlating:
#   desktop\publish\PosPlatform.Desktop.zip + version.json
#
# Ishlatish:
#   .\publish-desktop-update.ps1 -Version 1.0.9            # faqat paketlarni yasaydi
#   .\publish-desktop-update.ps1 -Version 1.0.9 -Upload    # yasab, serverga ham yuklaydi
#
# Server manzilini har safar yozmaslik uchun shu papkada publish-target.json yarating
# (git'ga tushmaydi — publish-target.example.json dan nusxa oling).
#
# Yangilanish majburiy emas: kassada ⚙ belgisi chiqadi, kassir Sozlamalar → "Yangilash" ni bosadi.
param(
    [Parameter(Mandatory = $true)][string]$Version,
    # Paketlarni serverga scp orqali yuklaydi (SSH kalitingiz sozlangan bo'lishi kerak).
    [switch]$Upload,
    # Eski formatdagi zip + version.json yasalmasin (barcha kassalar Velopack'ga o'tgach).
    [switch]$SkipLegacy,
    [string]$Server,
    # Serverdagi Velopack papkasi (backend ContentRoot/updates/velopack).
    [string]$VelopackPath,
    # Shu papkaning tashqi manzili — delta uchun oldingi versiya shu yerdan olinadi.
    [string]$FeedUrl,
    # Eski format: backend ContentRoot/updates/desktop.
    [string]$RemotePath
)

# Velopack faqat semver (x.y.z) qabul qiladi.
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Versiya x.y.z ko'rinishida bo'lsin (masalan 1.0.9)"
    exit 1
}

$cfgPath = Join-Path $PSScriptRoot "publish-target.json"
if (Test-Path $cfgPath) {
    $cfg = Get-Content $cfgPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $Server)       { $Server = $cfg.server }
    if (-not $VelopackPath) { $VelopackPath = $cfg.velopackPath }
    if (-not $FeedUrl)      { $FeedUrl = $cfg.feedUrl }
    if (-not $RemotePath)   { $RemotePath = $cfg.remotePath }
}
if (-not $FeedUrl) { $FeedUrl = "https://poscloud.uz/updates/velopack" }

# UpdateService.PackId bilan bir xil bo'lishi shart — o'rnatish papkasi ham shu nomda.
$packId      = "PosCloud"
$mainExe     = "PosPlatform.Desktop.exe"
$proj        = Join-Path $PSScriptRoot "PosCloud.Desktop\PosCloud.Desktop.csproj"
$icon        = Join-Path $PSScriptRoot "PosCloud.Desktop\Resources\app.ico"
$publishRoot = Join-Path $PSScriptRoot "publish"
$appDir      = Join-Path $publishRoot "app"                       # vaqtinchalik build chiqishi
$releasesDir = Join-Path $publishRoot "releases"                  # Velopack paketlari
$zip         = Join-Path $publishRoot "PosPlatform.Desktop.zip"   # eski format, DOIMIY nom
$metaPath    = Join-Path $publishRoot "version.json"

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null

# ── Eski chiqishlarni tozalash ──────────────────────────────────────────────
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
# Klientlarda .NET 8 o'rnatilmagan deb hisoblaymiz — runtime paket ichida bo'ladi.
dotnet publish $proj -c Release -r win-x64 --self-contained true /p:Version=$Version -o $appDir
if ($LASTEXITCODE -ne 0) { exit 1 }

# ── Velopack paketlari ──────────────────────────────────────────────────────
# vpk — lokal dotnet tool (desktop\dotnet-tools.json), shu papkadan chaqiriladi.
Push-Location $PSScriptRoot
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "vpk o'rnatilmadi (dotnet tool restore)."; exit 1 }

    # Serverdagi oxirgi to'liq paket — vpk undan delta yasaydi. Birinchi chiqarishda server bo'sh bo'ladi.
    Write-Host ""
    Write-Host "Oldingi versiya olinmoqda (delta uchun): $FeedUrl"
    dotnet vpk download http --url $FeedUrl -o $releasesDir --allowEmptyChannel
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Oldingi versiya olinmadi — delta yasalmaydi, kassalar to'liq paketni yuklaydi."
    }

    dotnet vpk pack --packId $packId --packVersion $Version --runtime win-x64 `
        --packDir $appDir --mainExe $mainExe --packTitle "PosCloud" --icon $icon --outputDir $releasesDir
    if ($LASTEXITCODE -ne 0) { Write-Error "vpk pack muvaffaqiyatsiz."; exit 1 }
}
finally {
    Pop-Location
}

# ── Eski format (o'tish davri) ──────────────────────────────────────────────
if (-not $SkipLegacy) {
    Compress-Archive -Path "$appDir\*" -DestinationPath $zip
    if (-not (Test-Path $zip)) { Write-Error "Zip yaratilmadi."; exit 1 }

    $meta = [ordered]@{
        version   = $Version
        fileName  = "PosPlatform.Desktop.zip"
        sizeBytes = (Get-Item $zip).Length
        builtAt   = (Get-Date).ToString("o")
    }
    $meta | ConvertTo-Json | Set-Content -Path $metaPath -Encoding utf8
}

# Build papkasi endi kerak emas.
Remove-Item $appDir -Recurse -Force

Write-Host ""
Write-Host "Tayyor: $releasesDir  (versiya $Version)"
if (-not $SkipLegacy) { Write-Host "        $zip  (eski kassalar uchun)" }

if (-not $Upload) {
    Write-Host "Serverga yuklash: shu buyruqni -Upload bilan qayta ishga tushiring."
    return
}

# ── Serverga yuklash ────────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($Server) -or [string]::IsNullOrWhiteSpace($VelopackPath)) {
    Write-Host "Server manzili berilmagan. Ikki yo'l bor:" -ForegroundColor Yellow
    Write-Host "  1) publish-target.json yarating (publish-target.example.json dan nusxa oling)"
    Write-Host "  2) yoki parametr bilan:"
    Write-Host "     -Server user@poscloud.uz -VelopackPath /opt/posplatform/updates/velopack"
    Write-Error "Server / VelopackPath berilmagan - yuklash bekor qilindi."
    exit 1
}

Write-Host ""
Write-Host "Serverga yuklanmoqda -> ${Server}:${VelopackPath}"

ssh $Server "mkdir -p '$VelopackPath'"
if ($LASTEXITCODE -ne 0) { Write-Error "ssh ulanmadi yoki papka yaratilmadi."; exit 1 }

# Faqat shu safar yasalgan fayllar (assets.win.json ro'yxati); Portable zip kerak emas.
# Oldingi to'liq paket serverda allaqachon bor — qayta yuklanmaydi.
$assets = Get-Content (Join-Path $releasesDir "assets.win.json") -Raw | ConvertFrom-Json
$newFiles = @($assets | Where-Object { $_.Type -ne "Portable" } | ForEach-Object { $_.RelativeFileName })

# Har fayl avval .tmp nomi bilan, hammasi tushgach nomi almashtiriladi — kassa yarim faylni olmaydi.
# releases.win.json eng oxirida: u yangilanmaguncha kassalar yangi versiyani ko'rmaydi.
foreach ($name in $newFiles + @("RELEASES", "releases.win.json")) {
    scp (Join-Path $releasesDir $name) "${Server}:${VelopackPath}/$name.tmp"
    if ($LASTEXITCODE -ne 0) { Write-Error "scp muvaffaqiyatsiz — $name yuklanmadi."; exit 1 }
    ssh $Server "mv '$VelopackPath/$name.tmp' '$VelopackPath/$name'"
    if ($LASTEXITCODE -ne 0) { Write-Error "Serverda $name nomini almashtirib bo'lmadi."; exit 1 }
}

# releases.win.json da qolmagan eski paketlarni o'chiramiz — serverda 2 ta to'liq + 1 delta qoladi.
$keep = @(Get-ChildItem $releasesDir -Filter "*.nupkg" | ForEach-Object { $_.Name })
$stale = @(ssh $Server "ls -1 '$VelopackPath'" | Where-Object { $_ -like "*.nupkg" -and $keep -notcontains $_ })
if ($stale.Count -gt 0) {
    ssh $Server ("rm -f " + (($stale | ForEach-Object { "'$VelopackPath/$_'" }) -join " "))
    Write-Host "Eski paketlar o'chirildi: $($stale -join ', ')" -ForegroundColor DarkGray
}

Write-Host "Yuklandi: ${VelopackPath} (versiya $Version)"

# ── Eski format yuklash (o'tish davri) ──────────────────────────────────────
if (-not $SkipLegacy) {
    if ([string]::IsNullOrWhiteSpace($RemotePath)) {
        Write-Warning "RemotePath berilmagan — eski kassalar uchun zip yuklanmadi."
    }
    else {
        Write-Host ""
        Write-Host "Eski format yuklanmoqda -> ${Server}:${RemotePath}"
        ssh $Server "mkdir -p '$RemotePath'"
        # Avval zip, to'liq tushgach version.json — eski kassa buzuq zipni olmaydi.
        scp $zip "${Server}:${RemotePath}/PosPlatform.Desktop.zip.tmp"
        if ($LASTEXITCODE -ne 0) { Write-Error "scp muvaffaqiyatsiz — zip yuklanmadi."; exit 1 }
        ssh $Server "mv '$RemotePath/PosPlatform.Desktop.zip.tmp' '$RemotePath/PosPlatform.Desktop.zip'"
        if ($LASTEXITCODE -ne 0) { Write-Error "Serverda zip nomini almashtirib bo'lmadi."; exit 1 }
        scp $metaPath "${Server}:${RemotePath}/version.json"
        if ($LASTEXITCODE -ne 0) { Write-Error "version.json yuklanmadi — eski kassalar eski versiyani ko'radi."; exit 1 }
        ssh $Server "rm -f '$RemotePath'/PosPlatform.Desktop-*.zip"
    }
}

# ── Tekshirish: server endi shu versiyani qaytaryaptimi? ────────────────────
Write-Host ""
try {
    $resp = Invoke-WebRequest "$FeedUrl/releases.win.json" -UseBasicParsing -TimeoutSec 25
    if ($resp.Content -match ('"Version":"' + [regex]::Escape($Version) + '"')) {
        Write-Host "OK — kassalar endi $Version ni ko'radi (⚙ Sozlamalar → Yangilash)." -ForegroundColor Green
    }
    else {
        Write-Warning "releases.win.json da $Version topilmadi — VelopackPath backend ContentRoot/updates/velopack ga mos kelmasligi mumkin."
    }
}
catch {
    Write-Warning "Tekshirib bo'lmadi ($FeedUrl/releases.win.json): $($_.Exception.Message)"
}
