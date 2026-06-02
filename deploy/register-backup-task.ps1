<#
.SYNOPSIS
  Kunlik avtomatik zaxira uchun Windows rejalashtirilgan vazifa (Scheduled Task) yaratadi.

.DESCRIPTION
  Har kuni belgilangan vaqtda backup-db.ps1 ni ishga tushiradi.
  Administrator PowerShell'da bir marta ishga tushiring.

.PARAMETER Time
  Zaxira vaqti (HH:mm). Standart: 02:30 (tunda).

.EXAMPLE
  .\register-backup-task.ps1 -Time "03:00" -BackupDir "D:\Backups\pos"
#>
param(
    [string]$Time = "02:30",
    [string]$BackupDir = "C:\PosPlatformBackups",
    [int]$RetentionDays = 14
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "backup-db.ps1"
if (-not (Test-Path $scriptPath)) { throw "backup-db.ps1 topilmadi: $scriptPath" }

$taskName = "PosPlatform-DailyBackup"

$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -BackupDir `"$BackupDir`" -RetentionDays $RetentionDays"

$trigger = New-ScheduledTaskTrigger -Daily -At $Time

# SYSTEM hisobida ishlaydi (parol shart emas). PGPASSWORD ni mashina darajasida o'rnating:
#   setx /M PGPASSWORD "kuchli_parol"
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger `
    -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "Rejalashtirilgan vazifa yaratildi: '$taskName' — har kuni $Time da."
Write-Host "DIQQAT: SYSTEM hisobi uchun parolni o'rnating:  setx /M PGPASSWORD \"kuchli_parol\""
Write-Host "Sinash: Start-ScheduledTask -TaskName '$taskName'"
