<#
.SYNOPSIS
  PosPlatform backend'ni /health endpoint orqali tekshiradi.
  Muammo aniqlansa — email yoki Telegram xabari yuboradi.

.DESCRIPTION
  Har N daqiqada Windows Task Scheduler orqali ishga tushiring.
  Monitoring skripti: GET http://localhost:5000/health → "Healthy" bo'lmasa alert.

.PARAMETER Url
  Health check URL. Standart: http://localhost:5000/health

.PARAMETER AlertEmail
  Muammo aniqlansa email yuboriladigan manzil. Bo'sh = email yo'q.

.PARAMETER SmtpServer
  SMTP server. Standart: smtp.gmail.com

.PARAMETER SmtpPort
  SMTP port. Standart: 587

.PARAMETER SmtpUser
  SMTP foydalanuvchi (yuboruvchi email).

.PARAMETER SmtpPass
  SMTP parol (muhit o'zgaruvchisi SMTP_PASS orqali berish tavsiya etiladi).

.PARAMETER TelegramBotToken
  Telegram bot token. Bo'sh = Telegram yo'q.

.PARAMETER TelegramChatId
  Telegram chat/user ID. Bo'sh = Telegram yo'q.

.PARAMETER StateFile
  Oxirgi holatni saqlash fayli (ketma-ket alertlarni oldini oladi).
  Standart: C:\PosPlatformBackups\health-state.txt

.EXAMPLE
  # Faqat lokal tekshirish
  .\check-health.ps1

  # Email bilan
  .\check-health.ps1 -AlertEmail "admin@company.com" -SmtpUser "bot@gmail.com" -SmtpPass "app_password"

  # Telegram bilan
  .\check-health.ps1 -TelegramBotToken "123456:ABCDEF..." -TelegramChatId "-1001234567890"

.NOTES
  Task Scheduler uchun:
    schtasks /create /tn "PosHealthCheck" /tr "powershell -NonInteractive -File C:\pos\deploy\check-health.ps1 -AlertEmail ..." /sc minute /mo 5 /ru SYSTEM
#>

param(
    [string]$Url              = "http://localhost:5000/health",
    [string]$AlertEmail       = "",
    [string]$SmtpServer       = "smtp.gmail.com",
    [int]$SmtpPort            = 587,
    [string]$SmtpUser         = "",
    [string]$SmtpPass         = $env:SMTP_PASS,
    [string]$TelegramBotToken = "",
    [string]$TelegramChatId   = "",
    [string]$StateFile        = "C:\PosPlatformBackups\health-state.txt"
)

$ErrorActionPreference = "SilentlyContinue"

$hostname   = $env:COMPUTERNAME
$now        = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$wasHealthy = $true

if (Test-Path $StateFile) {
    $wasHealthy = (Get-Content $StateFile -Raw).Trim() -eq "healthy"
}

# ------ Tekshirish ------
$status  = "unknown"
$detail  = ""
try {
    $response = Invoke-WebRequest -Uri $Url -TimeoutSec 10 -UseBasicParsing
    $status  = if ($response.StatusCode -eq 200) { "healthy" } else { "unhealthy" }
    $detail  = $response.Content
}
catch {
    $status = "down"
    $detail = $_.Exception.Message
}

$isHealthy = $status -eq "healthy"

# ------ Holat faylini yangilash ------
New-Item -ItemType Directory -Force -Path (Split-Path $StateFile) | Out-Null
$status | Set-Content $StateFile -Encoding UTF8

Write-Host "[$now] Status: $status | $Url"

# ------ Alert faqat holat o'zgarganda yuboriladi ------
$shouldAlert = (-not $isHealthy -and $wasHealthy) -or ($isHealthy -and -not $wasHealthy)

if (-not $shouldAlert) { exit 0 }

$emoji   = if ($isHealthy) { "✅" } else { "🔴" }
$subject = "$emoji PosPlatform [$hostname] — $($status.ToUpper())"
$body    = @"
PosPlatform health holati o'zgardi

Server  : $hostname
Vaqt    : $now
Status  : $($status.ToUpper())
URL     : $Url
Detail  : $detail
"@

# ------ Email ------
if ($AlertEmail -and $SmtpUser -and $SmtpPass) {
    try {
        $cred = New-Object System.Management.Automation.PSCredential(
            $SmtpUser,
            (ConvertTo-SecureString $SmtpPass -AsPlainText -Force)
        )
        Send-MailMessage `
            -To $AlertEmail -From $SmtpUser `
            -Subject $subject -Body $body `
            -SmtpServer $SmtpServer -Port $SmtpPort `
            -UseSsl -Credential $cred
        Write-Host "Email yuborildi: $AlertEmail"
    }
    catch {
        Write-Warning "Email yuborilmadi: $_"
    }
}

# ------ Telegram ------
if ($TelegramBotToken -and $TelegramChatId) {
    try {
        $text = "$subject`n`n$body"
        $tgUrl = "https://api.telegram.org/bot$TelegramBotToken/sendMessage"
        Invoke-RestMethod -Uri $tgUrl -Method Post -ContentType "application/json" -Body (
            @{ chat_id = $TelegramChatId; text = $text } | ConvertTo-Json
        ) | Out-Null
        Write-Host "Telegram xabari yuborildi: $TelegramChatId"
    }
    catch {
        Write-Warning "Telegram xabari yuborilmadi: $_"
    }
}

exit 0
