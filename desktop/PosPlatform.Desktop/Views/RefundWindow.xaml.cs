using System.Windows;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class RefundWindow : Window
{
    public RefundWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var sales = await AppState.Backend.GetSalesAsync();
            SalesList.ItemsSource = sales;
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void DoRefund_Click(object sender, RoutedEventArgs e)
    {
        if (SalesList.SelectedItem is not SaleListDto sale)
        {
            ShowMessage("Avval savdoni tanlang", error: true);
            return;
        }
        if (!sale.CanRefund)
        {
            ShowMessage("Bu savdoni qaytarib bo'lmaydi (allaqachon qaytarilgan yoki refund cheki).", error: true);
            return;
        }
        if (MessageBox.Show($"{sale.ReceiptNumber} chekni qaytarasizmi?", "Tasdiqlash",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        RefundButton.IsEnabled = false;
        try
        {
            var refund = await AppState.Backend.RefundAsync(sale.Id);
            ShowMessage($"✓ Qaytarildi — chek {refund.ReceiptNumber}", error: false);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
        finally
        {
            RefundButton.IsEnabled = true;
        }
    }

    private void ShowMessage(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x16, 0xA3, 0x4A));
        MessageText.Visibility = Visibility.Visible;
    }
}
