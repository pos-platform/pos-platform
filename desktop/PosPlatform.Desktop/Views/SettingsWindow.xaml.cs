using System.Printing;
using System.Windows;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        BaseUrlBox.Text = AppState.BaseUrl;
        PrinterEnabledBox.IsChecked = AppState.PrinterEnabled;

        // O'rnatilgan printerlarni yuklaymiz.
        try
        {
            using var server = new LocalPrintServer();
            foreach (var q in server.GetPrintQueues())
                PrinterCombo.Items.Add(q.Name);
        }
        catch { /* printerlar olinmadi — qo'lda yozish mumkin */ }

        PrinterCombo.Text = AppState.PrinterName ?? "";
        TerminalIdBox.Text = AppState.TerminalId ?? "";
        LegalAddressBox.Text = AppState.LegalAddress ?? "";
        PaymeLinkBox.Text = AppState.PaymeLink ?? "";
        TransferCardBox.Text = AppState.TransferCard ?? "";

        // COM port skaner
        foreach (var port in BarcodeScanner.GetAvailablePorts())
            ScannerPortCombo.Items.Add(port);
        ScannerPortCombo.Text = AppState.ScannerComPort ?? "";
        BaudRateBox.Text = AppState.ScannerBaudRate > 0 ? AppState.ScannerBaudRate.ToString() : "9600";

        VersionText.Text = $"Joriy versiya: {UpdateService.CurrentVersion}";
        _ = LoadUpdateInfoAsync();
    }

    /// <summary>Serverdan yangi versiyani tekshirib, "Yangilash" tugmasini ko'rsatadi.</summary>
    private async Task LoadUpdateInfoAsync()
    {
        var upd = await UpdateService.CheckAsync();
        if (upd is null)
        {
            VersionText.Text = $"Joriy versiya: {UpdateService.CurrentVersion} — eng so'nggi versiya";
            return;
        }

        VersionText.Text = $"Joriy versiya: {UpdateService.CurrentVersion}. " +
                           $"Yangi versiya mavjud: {upd.Version} ({upd.SizeBytes / 1024.0 / 1024.0:0.#} MB)";
        UpdateButtonText.Text = $"⬇ Yangilash — v{upd.Version}";
        UpdateButton.Visibility = Visibility.Visible;
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        var upd = UpdateService.Available;
        if (upd is null) return;

        var ok = MessageBox.Show(this,
            $"Yangi {upd.Version} versiyasi o'rnatilsinmi?\nDastur yangilanib, avtomatik qayta ochiladi.",
            "Yangilash", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        UpdateButton.IsEnabled = false;
        UpdateButtonText.Text = "Yuklab olinmoqda...";
        UpdateProgress.Visibility = Visibility.Visible;
        try
        {
            var progress = new Progress<double>(p => UpdateProgress.Value = p);
            // Muvaffaqiyatli tugasa ilova o'zi yopilib, yangi versiya ochiladi.
            await UpdateService.DownloadAndInstallAsync(progress);
        }
        catch (Exception ex)
        {
            UpdateButton.IsEnabled = true;
            UpdateButtonText.Text = $"⬇ Yangilash — v{upd.Version}";
            UpdateProgress.Visibility = Visibility.Collapsed;
            ShowMessage("Yangilanmadi: " + ex.Message, error: true);
        }
    }

    // Sozlamalarni saqlash — fiskal QR rejimi (ExternalFiscalDevice) o'zgarmaydi,
    // joriy qiymat saqlab qolinadi.
    private void Save_Click(object sender, RoutedEventArgs e) => Save(externalFiscal: AppState.ExternalFiscalDevice);

    private void Save(bool externalFiscal)
    {
        var baseUrl = BaseUrlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ShowMessage("Backend manzilini kiriting", error: true);
            return;
        }

        var settings = new Settings
        {
            BaseUrl = baseUrl,
            PrinterEnabled = PrinterEnabledBox.IsChecked == true,
            PrinterName = string.IsNullOrWhiteSpace(PrinterCombo.Text) ? null : PrinterCombo.Text.Trim(),
            TerminalId = string.IsNullOrWhiteSpace(TerminalIdBox.Text) ? null : TerminalIdBox.Text.Trim(),
            ExternalFiscalDevice = externalFiscal,
            ScannerComPort = string.IsNullOrWhiteSpace(ScannerPortCombo.Text) ? null : ScannerPortCombo.Text.Trim(),
            ScannerBaudRate = int.TryParse(BaudRateBox.Text, out var br) && br > 0 ? br : 9600,
            LegalAddress = string.IsNullOrWhiteSpace(LegalAddressBox.Text) ? null : LegalAddressBox.Text.Trim(),
            PaymeLink = string.IsNullOrWhiteSpace(PaymeLinkBox.Text) ? null : PaymeLinkBox.Text.Trim(),
            TransferCard = string.IsNullOrWhiteSpace(TransferCardBox.Text) ? null : TransferCardBox.Text.Trim()
        };

        try
        {
            SettingsService.Save(settings);
        }
        catch (Exception ex)
        {
            ShowMessage("Saqlanmadi: " + ex.Message, error: true);
            return;
        }

        var urlChanged = !string.Equals(AppState.BaseUrl, settings.BaseUrl, StringComparison.OrdinalIgnoreCase);
        AppState.BaseUrl = settings.BaseUrl;
        AppState.PrinterEnabled = settings.PrinterEnabled;
        AppState.PrinterName = settings.PrinterName;
        AppState.TerminalId = settings.TerminalId;
        AppState.ExternalFiscalDevice = settings.ExternalFiscalDevice;
        var scannerChanged = AppState.ScannerComPort != settings.ScannerComPort
                             || AppState.ScannerBaudRate != settings.ScannerBaudRate;
        AppState.ScannerComPort = settings.ScannerComPort;
        AppState.ScannerBaudRate = settings.ScannerBaudRate;
        AppState.LegalAddress = settings.LegalAddress;
        AppState.PaymeLink = settings.PaymeLink;
        AppState.TransferCard = settings.TransferCard;
        if (urlChanged) AppState.RebuildBackend();
        if (scannerChanged) AppState.RestartScanner();

        DialogResult = true;
        Close();
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var current = CurrentPasswordBox.Password;
        var next = NewPasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (string.IsNullOrEmpty(current))
        {
            ShowMessage("Joriy parolni kiriting", error: true);
            return;
        }
        if (next.Length < 6)
        {
            ShowMessage("Yangi parol kamida 6 belgidan iborat bo'lishi kerak", error: true);
            return;
        }
        if (next != confirm)
        {
            ShowMessage("Yangi parol va tasdiq mos kelmadi", error: true);
            return;
        }

        try
        {
            await AppState.Backend.ChangePasswordAsync(current, next);
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            ShowMessage("Parol muvaffaqiyatli o'zgartirildi", error: false);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void TestPrint_Click(object sender, RoutedEventArgs e)
    {
        var sample = new ReceiptData
        {
            ReceiptNumber = "TEST-0001",
            StoreName = AppState.StoreName,
            Stir = AppState.OrgStir,
            Address = AppState.BranchAddress,
            LegalAddress = string.IsNullOrWhiteSpace(LegalAddressBox.Text) ? AppState.LegalAddress : LegalAddressBox.Text.Trim(),
            Kassa = AppState.TerminalId,
            Cashier = AppState.CashierDisplay,
            Payment = "Cash",
            Total = 10000,
            Paid = 10000,
            Change = 0,
            Lines = new()
            {
                new ReceiptLine { Name = "Test mahsulot", Quantity = 1, Price = 10000, LineTotal = 10000 }
            }
        };
        try
        {
            var printerName = string.IsNullOrWhiteSpace(PrinterCombo.Text) ? null : PrinterCombo.Text.Trim();
            new ReceiptPrinter().Print(sample, printerName);
            ShowMessage("Test chek yuborildi", error: false);
        }
        catch (Exception ex)
        {
            ShowMessage("Chop etilmadi: " + ex.Message, error: true);
        }
    }

    private void ShowMessage(string text, bool error)
    {
        MsgText.Text = text;
        MsgText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x16, 0xA3, 0x4A));
        MsgText.Visibility = Visibility.Visible;
    }
}
