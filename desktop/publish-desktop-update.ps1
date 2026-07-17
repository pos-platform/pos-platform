# Desktop POS yangi versiyasini tayyorlaydi: build + zip.
# Ishlatish:  .\publish-desktop-update.ps1 -Version 1.0.1
# Natija:     desktop\publish\PosPlatform.Desktop-1.0.1.zip
# Keyin zipni serverdagi backend ContentRoot/updates/desktop/ papkasiga qo'ying —
# hamma kassalar ⚙ belgisida yangilanishni ko'rib, o'zi yangilanadi.
param(
    [Parameter(Mandatory = $true)][string]$Version,
    # Klientlarda .NET 8 o'rnatilmagan deb hisoblaymiz — runtime zip ichida bo'ladi.
    [bool]$SelfContained = $true
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
Write-Host "Serverga yuklang: <backend ContentRoot>/updates/desktop/PosPlatform.Desktop-$Version.zip"
