<#
.SYNOPSIS
  PosPlatform PostgreSQL bazasini zaxiralaydi (pg_dump) va eski zaxiralarni tozalaydi.

.DESCRIPTION
  Har kuni ishga tushirish uchun mo'ljallangan (register-backup-task.ps1 orqali rejalashtiriladi).
  Zaxiralar gzip bilan siqilgan .dump (custom format) ko'rinishida saqlanadi.

.PARAMETER BackupDir
  Zaxira fayllari saqlanadigan papka. Standart: C:\PosPlatformBackups

.PARAMETER RetentionDays
  Necha kunlik zaxira saqlansin (undan eskilari o'chiriladi). Standart: 14

.EXAMPLE
  .\backup-db.ps1 -BackupDir "D:\Backups\pos" -RetentionDays 30
#>
param(
    [string]$BackupDir = "C:\PosPlatformBackups",
    [int]$RetentionDays = 14,
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432,
    [string]$DbName = "pos_platform",
    [string]$DbUser = "postgres"
)

$ErrorActionPreference = "Stop"

# Parolni muhit o'zgaruvchisidan oling — skriptga yozmang!
# Masalan: setx PGPASSWORD "kuchli_parol"  (yoki rejalashtirilgan vazifa kontekstida)
if (-not $env:PGPASSWORD) {
    Write-Warning "PGPASSWORD muhit o'zgaruvchisi o'rnatilmagan. pg_dump parol so'rashi mumkin."
}

# pg_dump ni topish (PATH'da bo'lmasligi mumkin)
$pgDump = (Get-Command pg_dump -ErrorAction SilentlyContinue).Source
if (-not $pgDump) {
    $candidate = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_dump.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($candidate) { $pgDump = $candidate.FullName }
}
if (-not $pgDump) { throw "pg_dump topilmadi. PostgreSQL bin papkasini PATH'ga qo'shing." }

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

$stamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$outFile = Join-Path $BackupDir "$DbName`_$stamp.dump"

Write-Host "Zaxira boshlandi -> $outFile"
& $pgDump --host=$DbHost --port=$DbPort --username=$DbUser --format=custom --compress=9 --file=$outFile $DbName
if ($LASTEXITCODE -ne 0) { throw "pg_dump xato kodi: $LASTEXITCODE" }

$sizeMb = [math]::Round((Get-Item $outFile).Length / 1MB, 2)
Write-Host "Zaxira tayyor: $outFile ($sizeMb MB)"

# Eski zaxiralarni tozalash
$cutoff = (Get-Date).AddDays(-$RetentionDays)
$old = Get-ChildItem $BackupDir -Filter "$DbName`_*.dump" | Where-Object { $_.LastWriteTime -lt $cutoff }
foreach ($f in $old) {
    Remove-Item $f.FullName -Force
    Write-Host "O'chirildi (eski): $($f.Name)"
}

Write-Host "Tugadi. Tiklash: pg_restore --clean --if-exists -d $DbName <fayl.dump>"
