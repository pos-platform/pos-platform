using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>Restoran stol xaritasi — stol tanlab buyurtma oynasini ochadi.</summary>
public partial class TablesWindow : Window
{
    private ShiftDto? _shift;

    public TablesWindow()
    {
        InitializeComponent();
        var logo = App.CreateLogoIcon();
        Icon = logo;
        TopLogoImage.Source = logo;
        UserText.Text = $"{AppState.Username} ({AppState.Role})";
        ShowLicenseReminder();
        Loaded += async (_, _) =>
        {
            await LoadShiftAsync();
            await LoadTablesAsync();
        };
    }

    /// <summary>Obuna muddati 7 kun ichida tugayotgan bo'lsa yuqorida eslatma ko'rsatadi.
    /// Doimiy litsenziya bo'lsa yoki muddat ko'p qolgan bo'lsa — ko'rsatilmaydi.</summary>
    private void ShowLicenseReminder()
    {
        var daysLeft = AppState.LicenseDaysLeft;
        if (daysLeft is null || daysLeft.Value > 7) return;

        var until = AppState.LicensedUntil?.ToLocalTime().ToString("dd.MM.yyyy");
        LicenseBannerText.Text = daysLeft.Value <= 0
            ? "⚠ Hurmatli mijoz! Obunangiz muddati bugun tugaydi. Tizim to'xtab qolmasligi uchun to'lovni amalga oshiring."
            : $"⚠ Hurmatli mijoz! Obunangiz tugashiga {daysLeft.Value} kun qoldi ({until} gacha). " +
              "Tizimdan uzluksiz foydalanish uchun to'lovni o'z vaqtida amalga oshiring.";
        LicenseBanner.Visibility = Visibility.Visible;
    }

    private async Task LoadShiftAsync()
    {
        try { _shift = await AppState.Backend.GetCurrentShiftAsync(); }
        catch { _shift = null; }
        RenderShiftBar();
    }

    private void RenderShiftBar()
    {
        if (_shift is not null)
        {
            ShiftInfo.Text = $"Smena ochiq · boshlang'ich naqd {_shift.OpeningCash:N0}";
            OpenShiftPanel.Visibility = Visibility.Collapsed;
            CloseShiftButton.Visibility = Visibility.Visible;
        }
        else
        {
            ShiftInfo.Text = "Smena yopiq — hisob yopish uchun smenani oching";
            OpenShiftPanel.Visibility = Visibility.Visible;
            CloseShiftButton.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadTablesAsync()
    {
        try
        {
            var tables = await AppState.Backend.GetTablesAsync();
            TablesControl.ItemsSource = tables;
            EmptyText.Visibility = tables.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Table_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not TableDto table) return;
        var win = new OrderWindow(table) { Owner = this };
        win.ShowDialog();
        // Buyurtma oynasidan qaytgach stollar holatini yangilaymiz.
        await LoadTablesAsync();
    }

    private async void OpenShift_Click(object sender, RoutedEventArgs e)
    {
        var cash = ParseDecimal(OpeningCashBox.Text);
        try
        {
            _shift = await AppState.Backend.OpenShiftAsync(cash);
            RenderShiftBar();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void CloseShift_Click(object sender, RoutedEventArgs e)
    {
        var input = PromptInput("Sanab chiqilgan naqd summasi:", "Smenani yopish", "0");
        if (input is null) return;
        try
        {
            var closed = await AppState.Backend.CloseShiftAsync(ParseDecimal(input), printedLocally: true);
            _shift = null;
            RenderShiftBar();

            if (closed is not null) PrintShiftReportSafe(closed);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    /// <summary>Smena yakuni (Z-hisobot) chekini chop etadi; xato smena yopilishini bekor qilmaydi.</summary>
    private void PrintShiftReportSafe(ShiftDto shift)
    {
        try
        {
            new ReceiptPrinter().PrintShiftReport(new ShiftReportData
            {
                Shift = shift,
                StoreName = AppState.StoreName,
                Stir = AppState.OrgStir,
                Address = AppState.BranchAddress,
                Kassa = AppState.TerminalId,
                Cashier = AppState.CashierDisplay
            }, AppState.PrinterName);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Smena hisoboti chop etilmadi: " + ex.Message,
                "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadTablesAsync();

    private void Settings_Click(object sender, RoutedEventArgs e)
        => new SettingsWindow { Owner = this }.ShowDialog();

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }

    private string? PromptInput(string message, string title, string initial)
    {
        var dlg = new Window
        {
            Title = title, Width = 340, Height = 170, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) });
        var box = new TextBox { Text = initial, Height = 30, Padding = new Thickness(6, 4, 6, 4) };
        panel.Children.Add(box);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        string? result = null;
        var ok = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Bekor", Width = 80, Height = 30, IsCancel = true };
        ok.Click += (_, _) => { result = box.Text; dlg.DialogResult = true; };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        dlg.Content = panel;
        box.Focus();
        box.SelectAll();
        return dlg.ShowDialog() == true ? result : null;
    }

    private static decimal ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim().Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
