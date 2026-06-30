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

        // COM port skaner
        foreach (var port in BarcodeScanner.GetAvailablePorts())
            ScannerPortCombo.Items.Add(port);
        ScannerPortCombo.Text = AppState.ScannerComPort ?? "";
        BaudRateBox.Text = AppState.ScannerBaudRate > 0 ? AppState.ScannerBaudRate.ToString() : "9600";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
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
            ScannerComPort = string.IsNullOrWhiteSpace(ScannerPortCombo.Text) ? null : ScannerPortCombo.Text.Trim(),
            ScannerBaudRate = int.TryParse(BaudRateBox.Text, out var br) && br > 0 ? br : 9600
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
        var scannerChanged = AppState.ScannerComPort != settings.ScannerComPort
                             || AppState.ScannerBaudRate != settings.ScannerBaudRate;
        AppState.ScannerComPort = settings.ScannerComPort;
        AppState.ScannerBaudRate = settings.ScannerBaudRate;
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
