<#
.SYNOPSIS
  PosCloud — lokal backup yaratadi va masofaviy serverga yuklaydi.

.DESCRIPTION
  1. pg_dump orqali lokal .dump fayl yaratadi (backup-db.ps1 mantiqiga o'xshash).
  2. Faylni SFTP yoki SMB (tarmoq papkasi) orqali masofaviy serverga yuklaydi.
  3. Lokal va masofaviy eski zaxiralarni tozalaydi.

.PARAMETER BackupDir
  Lokal vaqtinchalik papka. Standart: C:\PosPlatformBackups

.PARAMETER RemotePath
  Masofaviy papka yo'li. SFTP bo'lsa: sftp://user@host:/backups/pos
  SMB bo'lsa: \\backup-server\pos-backups
  Standart: (bo'sh — faqat lokal saqlaydi)

.PARAMETER SftpKeyFile
  SFTP autentifikatsiya uchun private key fayli (RSA/Ed25519).
  WinSCP yoki OpenSSH ishlatilsa kerak.

.PARAMETER RetentionDays
  Lokal va masofaviy eski fayllarni qancha kundan keyin o'chirish. Standart: 14

.EXAMPLE
  # SMB tarmoq papkasiga
  .\backup-db-remote.ps1 -RemotePath "\\192.168.1.10\backups\pos"

  # SFTP (WinSCP CLI kerak: https://winscp.net/eng/download.php)
  .\backup-db-remote.ps1 -RemotePath "sftp://backup@192.168.1.10:/backups/pos" -SftpKeyFile "C:\keys\backup_key.ppk"
#>
param(
    [string]$BackupDir     = "C:\PosPlatformBackups",
    [string]$RemotePath    = "",
    [string]$SftpKeyFile   = "",
    [int]$RetentionDays    = 14,
    [string]$DbHost        = "localhost",
    [int]$DbPort           = 5432,
    [string]$DbName        = "pos_platform",
    [string]$DbUser        = "postgres"
)

$ErrorActionPreference = "Stop"

if (-not $env:PGPASSWORD) {
    Write-Warning "PGPASSWORD o'rnatilmagan. pg_dump parol so'rashi mumkin."
}

# ------ 1. pg_dump topish ------
$pgDump = (Get-Command pg_dump -ErrorAction SilentlyContinue).Source
if (-not $pgDump) {
    $candidate = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_dump.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($candidate) { $pgDump = $candidate.FullName }
}
if (-not $pgDump) { throw "pg_dump topilmadi. PostgreSQL bin papkasini PATH'ga qo'shing." }

# ------ 2. Lokal backup yaratish ------
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp   = Get-Date -Format "yyyy-MM-dd_HHmmss"
$outFile = Join-Path $BackupDir "$DbName`_$stamp.dump"

Write-Host "Zaxira boshlandi -> $outFile"
& $pgDump --host=$DbHost --port=$DbPort --username=$DbUser --format=custom --compress=9 --file=$outFile $DbName
if ($LASTEXITCODE -ne 0) { throw "pg_dump xato: $LASTEXITCODE" }

$sizeMb = [math]::Round((Get-Item $outFile).Length / 1MB, 2)
Write-Host "Lokal zaxira tayyor: $outFile ($sizeMb MB)"

# ------ 3. Masofaviy yuklash ------
if ($RemotePath) {
    if ($RemotePath.StartsWith("sftp://")) {
        # WinSCP CLI orqali SFTP
        $winScp = (Get-Command winscp.com -ErrorAction SilentlyContinue).Source
        if (-not $winScp) {
            $winScp = Get-ChildItem "C:\Program Files (x86)\WinSCP\WinSCP.com",
                                    "C:\Program Files\WinSCP\WinSCP.com" -ErrorAction SilentlyContinue |
                Select-Object -First 1 -ExpandProperty FullName
        }
        if (-not $winScp) { throw "WinSCP topilmadi. https://winscp.net dan yuklab oling." }

        $keyParam = if ($SftpKeyFile) { "-privatekey=`"$SftpKeyFile`"" } else { "" }
        $remoteFile = $RemotePath.TrimEnd('/') + "/" + (Split-Path $outFile -Leaf)

        $script = @"
open $RemotePath $keyParam -hostkey=*
put "$outFile" "$remoteFile"
exit
"@
        $tmpScript = [System.IO.Path]::GetTempFileName()
        $script | Set-Content $tmpScript -Encoding UTF8

        Write-Host "SFTP yuklash: $remoteFile"
        & $winScp /script=$tmpScript
        Remove-Item $tmpScript -Force
        if ($LASTEXITCODE -ne 0) { throw "WinSCP SFTP xato: $LASTEXITCODE" }
        Write-Host "SFTP yuklash muvaffaqiyatli."
    }
    else {
        # SMB tarmoq papkasiga nusxalash
        if (-not (Test-Path $RemotePath)) {
            try { New-Item -ItemType Directory -Force -Path $RemotePath | Out-Null }
            catch { throw "Masofaviy papkaga ulanib bo'lmadi: $RemotePath. $_" }
        }
        $dest = Join-Path $RemotePath (Split-Path $outFile -Leaf)
        Write-Host "SMB nusxalash: $dest"
        Copy-Item $outFile $dest -Force
        Write-Host "SMB nusxalash muvaffaqiyatli."

        # Masofaviy eski fayllarni tozalash (SMB)
        $cutoff = (Get-Date).AddDays(-$RetentionDays)
        Get-ChildItem $RemotePath -Filter "$DbName`_*.dump" |
            Where-Object { $_.LastWriteTime -lt $cutoff } |
            ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Masofaviydan o'chirildi: $($_.Name)" }
    }
}
else {
    Write-Warning "RemotePath berilmagan — faqat lokal zaxira saqlandi."
}

# ------ 4. Lokal eski fayllarni tozalash ------
$cutoff = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem $BackupDir -Filter "$DbName`_*.dump" |
    Where-Object { $_.LastWriteTime -lt $cutoff } |
    ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Lokaldan o'chirildi: $($_.Name)" }

Write-Host "Backup jarayoni tugadi."
Write-Host "Tiklash: pg_restore --clean --if-exists -d $DbName <fayl.dump>"
