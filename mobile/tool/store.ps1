<#
.SYNOPSIS
  Do'kon (white-label) ilovasini sozlaydi va (ixtiyoriy) yig'adi. Kelishuv: docs/mobile/CONTRACT-8.md.
  Hujjat: mobile/docs/WHITE_LABEL.md.

.DESCRIPTION
  stores/<slug>/store.json ni o'qiydi va tekshiradi, so'ng:
    - android/store.properties (applicationId, appLabel) - android/app/build.gradle.kts o'qiydi;
    - ios/Flutter/Store.xcconfig (STORE_BUNDLE_ID, APP_DISPLAY_NAME);
    - ikonka va splash (vaqtinchalik yaml orqali flutter_launcher_icons / flutter_native_splash);
    - Firebase fayllari (ixtiyoriy): stores/<slug>/google-services.json, stores/<slug>/GoogleService-Info.plist;
    - ixtiyoriy: flutter build (--dart-define=STORE_SLUG, API_BASE_URL, BRAND_COLOR, APP_NAME, --build-name/--build-number).
  -Slug default - hammasini standart "PosCloud Market" holatiga qaytaradi.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug example -Build appbundle
  powershell -ExecutionPolicy Bypass -File tool\store.ps1 -Slug default -Build none
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Slug,
    [ValidateSet('none', 'apk', 'appbundle', 'ios')][string]$Build = 'none',
    [string]$ApiBaseUrl = 'https://poscloud.uz',
    # Ixtiyoriy: Sentry DSN (bo'sh - Sentry o'chiq).
    [string]$SentryDsn = '',
    # flutter build ga qo'shimcha argumentlar (masalan: --obfuscate --split-debug-info=build/symbols).
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

# mobile/ papkasi (skript mobile/tool/ ichida).
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$Utf8 = New-Object System.Text.UTF8Encoding $false
function Write-Text([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText((Join-Path $Root $Path), $Text, $Utf8)
}
function Fail([string]$Message) {
    Write-Host "XATO: $Message" -ForegroundColor Red
    exit 1
}
function Step([string]$Message) { Write-Host "==> $Message" -ForegroundColor Cyan }

# Tashqi buyruq (flutter/dart) - xato kodi bo'lsa to'xtaydi.
function Invoke-Tool([string]$Exe, [string[]]$ToolArgs) {
    Write-Host "    $Exe $($ToolArgs -join ' ')" -ForegroundColor DarkGray
    & $Exe @ToolArgs
    if ($LASTEXITCODE -ne 0) { Fail "$Exe xato bilan tugadi (kod $LASTEXITCODE)." }
}

# PNG o'lchami (IHDR sarlavhasidan; System.Drawing kerak emas).
function Get-PngSize([string]$Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24 -or $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47) {
        Fail "$Path PNG fayl emas."
    }
    $w = ([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]
    $h = ([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23]
    return @($w, $h)
}

# Java .properties ISO-8859-1 da o'qiladi - ASCII bo'lmagan belgilar \uXXXX ko'rinishida yoziladi.
function ConvertTo-PropertiesValue([string]$Value) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $Value.ToCharArray()) {
        $code = [int]$ch
        if ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($code -lt 0x20 -or $code -gt 0x7E) { [void]$sb.Append(('\u{0:x4}' -f $code)) }
        else { [void]$sb.Append($ch) }
    }
    return $sb.ToString()
}

# Firebase faylini almashtirish: asl fayl *.default.* ga saqlanadi (yo'q bo'lsa - *.default.none belgisi).
function Set-FirebaseFile([string]$Source, [string]$Target) {
    $backup = "$Target.default"
    $none = "$Target.default.none"
    if (-not (Test-Path $backup) -and -not (Test-Path $none)) {
        if (Test-Path $Target) { Copy-Item $Target $backup } else { New-Item -ItemType File $none | Out-Null }
    }
    Copy-Item $Source $Target -Force
    Write-Host "    $Source -> $Target"
}
function Restore-FirebaseFile([string]$Target) {
    $backup = "$Target.default"
    $none = "$Target.default.none"
    if (Test-Path $backup) {
        Move-Item $backup $Target -Force
        Write-Host "    $Target asl holatga qaytarildi"
    }
    elseif (Test-Path $none) {
        if (Test-Path $Target) { Remove-Item $Target -Force }
        Remove-Item $none -Force
        Write-Host "    $Target o'chirildi (asl holatda yo'q edi)"
    }
}

$StorePropsPath = 'android/store.properties'
$XcconfigPath = 'ios/Flutter/Store.xcconfig'
$AndroidFirebase = 'android/app/google-services.json'
$IosFirebase = 'ios/Runner/GoogleService-Info.plist'

# ---------------------------------------------------------------- default
if ($Slug -eq 'default') {
    Step 'Standart holatga qaytarish (PosCloud Market)'
    if (Test-Path $StorePropsPath) { Remove-Item $StorePropsPath -Force; Write-Host "    $StorePropsPath o'chirildi" }
    Write-Text $XcconfigPath @"
// Do'kon (white-label) sozlamalari. tool/store.ps1 qayta yozadi; ``-Slug default`` - shu standart holatga qaytaradi.
// Runner target'ning PRODUCT_BUNDLE_IDENTIFIER = `$(STORE_BUNDLE_ID) (project.pbxproj), Info.plist CFBundleDisplayName = `$(APP_DISPLAY_NAME).
STORE_BUNDLE_ID = uz.poscloud.poscloudMarket
APP_DISPLAY_NAME = PosCloud Market

"@
    Restore-FirebaseFile $AndroidFirebase
    Restore-FirebaseFile $IosFirebase

    Step 'Ikonka va splash (assets/branding, pubspec.yaml sozlamalari)'
    Invoke-Tool 'dart' @('run', 'flutter_launcher_icons')
    Invoke-Tool 'dart' @('run', 'flutter_native_splash:create')

    if ($Build -ne 'none') {
        Step "flutter build ($Build) - marketplace rejimi"
        $defines = @("--dart-define=API_BASE_URL=$ApiBaseUrl")
        if ($SentryDsn) { $defines += "--dart-define=SENTRY_DSN=$SentryDsn" }
        $target = if ($Build -eq 'ios') { 'ipa' } else { $Build }
        Invoke-Tool 'flutter' (@('build', $target, '--release') + $defines + $ExtraArgs)
    }
    Write-Host 'Tayyor: standart holat.' -ForegroundColor Green
    exit 0
}

# ---------------------------------------------------------------- store.json tekshiruvi
if ($Slug -notmatch '^[a-z0-9-]{3,40}$') { Fail "Slug noto'g'ri: '$Slug' (faqat a-z, 0-9, '-', 3-40 belgi)." }
$StoreDir = "stores/$Slug"
$JsonPath = "$StoreDir/store.json"
if (-not (Test-Path $JsonPath)) { Fail "$JsonPath topilmadi." }

# PS 5.1 Get-Content standart kodlashda o'qiydi - UTF-8 ni aniq ko'rsatamiz.
$cfg = [System.IO.File]::ReadAllText((Join-Path $Root $JsonPath), $Utf8) | ConvertFrom-Json

function Get-Field([string]$Name, [bool]$Required = $true) {
    $prop = $cfg.PSObject.Properties[$Name]
    $value = if ($null -ne $prop -and $null -ne $prop.Value) { "$($prop.Value)".Trim() } else { '' }
    if ($Required -and -not $value) { Fail "store.json: '$Name' maydoni majburiy." }
    return $value
}

$jsonSlug = Get-Field 'slug'
$appName = Get-Field 'appName'
$androidId = Get-Field 'androidPackageId'
$iosId = Get-Field 'iosBundleId'
$brandColor = Get-Field 'brandColor'
$splashColor = Get-Field 'splashColor' $false
$versionName = Get-Field 'versionName'
$buildNumber = Get-Field 'buildNumber'

if ($jsonSlug -ne $Slug) { Fail "store.json slug ('$jsonSlug') papka nomi ('$Slug') bilan bir xil emas." }
if ($appName.Length -gt 30) { Fail "appName 30 belgidan uzun." }
if ($appName -match '[<>&"\\]') { Fail "appName da < > & `" \ belgilari bo'lmasin." }
if ($androidId -notmatch '^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$') { Fail "androidPackageId noto'g'ri: '$androidId'." }
if ($iosId -notmatch '^[A-Za-z][A-Za-z0-9-]*(\.[A-Za-z0-9-]+)+$') { Fail "iosBundleId noto'g'ri: '$iosId'." }
if ($brandColor -notmatch '^#[0-9A-Fa-f]{6}$') { Fail "brandColor #RRGGBB ko'rinishida bo'lsin: '$brandColor'." }
if (-not $splashColor) { $splashColor = $brandColor }
if ($splashColor -notmatch '^#[0-9A-Fa-f]{6}$') { Fail "splashColor #RRGGBB ko'rinishida bo'lsin: '$splashColor'." }
if ($versionName -notmatch '^\d+\.\d+\.\d+$') { Fail "versionName X.Y.Z ko'rinishida bo'lsin: '$versionName'." }
if ($buildNumber -notmatch '^[1-9]\d{0,9}$') { Fail "buildNumber musbat butun son bo'lsin: '$buildNumber'." }

$icon = "$StoreDir/icon.png"
$iconFg = "$StoreDir/icon_foreground.png"
$splash = "$StoreDir/splash.png"
if (-not (Test-Path $icon)) { Fail "$icon topilmadi (1024x1024 PNG)." }
$size = Get-PngSize (Join-Path $Root $icon)
if ($size[0] -ne 1024 -or $size[1] -ne 1024) { Fail "$icon 1024x1024 bo'lsin (hozir $($size[0])x$($size[1]))." }
$hasFg = Test-Path $iconFg
if ($hasFg) { $null = Get-PngSize (Join-Path $Root $iconFg) }
else { Write-Host "    Ogohlantirish: $iconFg yo'q - adaptiv ikonkada icon.png ishlatiladi (chetlari kesilishi mumkin)." -ForegroundColor Yellow }
$splashImage = if (Test-Path $splash) { $splash } else { $icon }
$null = Get-PngSize (Join-Path $Root $splashImage)

Write-Host "Do'kon: $appName ($Slug) | Android: $androidId | iOS: $iosId | rang: $brandColor | versiya: $versionName+$buildNumber"

# ---------------------------------------------------------------- native sozlamalar
Step 'Android: android/store.properties'
Write-Text $StorePropsPath @"
# tool/store.ps1 yaratgan ($Slug). Qo'lda o'zgartirmang; repoga KIRMAYDI. Standartga: tool/store.ps1 -Slug default
applicationId=$androidId
appLabel=$(ConvertTo-PropertiesValue $appName)

"@

Step 'iOS: ios/Flutter/Store.xcconfig'
Write-Text $XcconfigPath @"
// tool/store.ps1 yaratgan ($Slug). Standartga: tool/store.ps1 -Slug default
STORE_BUNDLE_ID = $iosId
APP_DISPLAY_NAME = $appName

"@

# Firebase: umumiy google-services.json barcha ilovalarni o'z ichiga oladi; do'kon papkasidagisi - ixtiyoriy almashtirish.
if (Test-Path "$StoreDir/google-services.json") {
    Step 'Firebase: google-services.json (do''kon papkasidan)'
    Set-FirebaseFile "$StoreDir/google-services.json" $AndroidFirebase
}
elseif (Test-Path $AndroidFirebase) {
    $gs = [System.IO.File]::ReadAllText((Join-Path $Root $AndroidFirebase), $Utf8)
    if ($gs -notmatch [regex]::Escape("`"package_name`": `"$androidId`"")) {
        Write-Host "    Ogohlantirish: $AndroidFirebase da '$androidId' ilovasi yo'q - Firebase'da Android ilova qo'shib, faylni qayta yuklab oling (aks holda build xato beradi)." -ForegroundColor Yellow
    }
}
if (Test-Path "$StoreDir/GoogleService-Info.plist") {
    Step 'Firebase: GoogleService-Info.plist (do''kon papkasidan)'
    Set-FirebaseFile "$StoreDir/GoogleService-Info.plist" $IosFirebase
}

# ---------------------------------------------------------------- ikonka va splash
# DIQQAT: "flutter_launcher_icons-<nom>.yaml" nomini flutter_launcher_icons flavor deb tushunadi (alohida
# android/app/src/<nom>, AppIcon-<nom> yaratadi) - shuning uchun boshqa nom.
$iconsYaml = "store-icons.$Slug.tmp.yaml"
$splashYaml = "store-splash.$Slug.tmp.yaml"
$fgPath = if ($hasFg) { $iconFg } else { $icon }
try {
    Write-Text $iconsYaml @"
# Vaqtinchalik (tool/store.ps1). O'chiriladi.
flutter_launcher_icons:
  android: true
  ios: true
  image_path: "$icon"
  remove_alpha_ios: true
  min_sdk_android: 21
  adaptive_icon_background: "$brandColor"
  adaptive_icon_foreground: "$fgPath"
"@
    Write-Text $splashYaml @"
# Vaqtinchalik (tool/store.ps1). O'chiriladi.
flutter_native_splash:
  color: "$splashColor"
  image: $splashImage
  fullscreen: false
  android_12:
    color: "$splashColor"
    image: $splashImage
"@
    Step 'Ikonka (flutter_launcher_icons)'
    Invoke-Tool 'dart' @('run', 'flutter_launcher_icons', '-f', $iconsYaml)
    Step 'Splash (flutter_native_splash)'
    Invoke-Tool 'dart' @('run', 'flutter_native_splash:create', "--path=$splashYaml")
}
finally {
    foreach ($f in @($iconsYaml, $splashYaml)) { if (Test-Path $f) { Remove-Item $f -Force } }
}

# ---------------------------------------------------------------- build
if ($Build -ne 'none') {
    if ($Build -eq 'ios' -and [System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Unix) {
        Fail 'iOS build faqat macOS da (Xcode). Sozlamalar yozildi - macOS/CI da: tool/store.ps1 -Slug <slug> -Build ios (pwsh).'
    }
    $defines = @(
        "--dart-define=STORE_SLUG=$Slug",
        "--dart-define=API_BASE_URL=$ApiBaseUrl",
        "--dart-define=BRAND_COLOR=$brandColor",
        "--dart-define=APP_NAME=$appName"
    )
    if ($SentryDsn) { $defines += "--dart-define=SENTRY_DSN=$SentryDsn" }
    $target = if ($Build -eq 'ios') { 'ipa' } else { $Build }
    Step "flutter build $target"
    Invoke-Tool 'flutter' (@('build', $target, '--release', "--build-name=$versionName", "--build-number=$buildNumber") + $defines + $ExtraArgs)
}

Write-Host "Tayyor: $appName ($Slug). Standart holatga qaytarish: tool/store.ps1 -Slug default" -ForegroundColor Green
