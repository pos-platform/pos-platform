using System.Windows;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>Tanlangan kun savdolari ro'yxati: kunlik jami va chekni qayta chop etish.</summary>
public partial class SalesHistoryWindow : Window
{
    private DateTime _day = DateTime.Today;

    public SalesHistoryWindow()
    {
        InitializeComponent();
        DayPicker.SelectedDate = _day;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var sales = await AppState.Backend.GetSalesByDayAsync(_day);
            SalesList.ItemsSource = sales;
            UpdateSummary(sales);
            HideMessage();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private void UpdateSummary(List<SaleListDto> sales)
    {
        int count = 0;
        decimal cash = 0, card = 0, transfer = 0, debt = 0, refunds = 0;
        // To'lov turi bo'yicha QR (fiskal) chek berilgan/berilmagan kesim
        decimal qrCash = 0, qrCard = 0, qrTransfer = 0, qrMixed = 0;
        decimal plainCash = 0, plainCard = 0, plainTransfer = 0, plainMixed = 0;
        foreach (var s in sales)
        {
            if (s.Type == "Refund")
            {
                refunds += s.TotalAmount;
                continue;
            }
            count++;
            switch (s.PaymentMethod)
            {
                case "Card": card += s.TotalAmount; break;
                case "Transfer": transfer += s.TotalAmount; break;
                case "Debt": debt += s.TotalAmount; break;
                default: cash += s.TotalAmount; break; // Cash va Mixed
            }

            switch (s.PaymentMethod)
            {
                case "Card":
                    if (s.HasQr) qrCard += s.TotalAmount; else plainCard += s.TotalAmount;
                    break;
                case "Transfer": // fiskalda karta sifatida ro'yxatdan o'tadi
                    if (s.HasQr) qrTransfer += s.TotalAmount; else plainTransfer += s.TotalAmount;
                    break;
                case "Mixed":
                    if (s.HasQr) qrMixed += s.TotalAmount; else plainMixed += s.TotalAmount;
                    break;
                case "Debt":
                    break; // Nasiya alohida ko'rsatiladi
                default:
                    if (s.HasQr) qrCash += s.TotalAmount; else plainCash += s.TotalAmount;
                    break;
            }
        }

        CountText.Text = count.ToString();
        CashText.Text = cash.ToString("N0");
        CardText.Text = card.ToString("N0");
        TransferText.Text = transfer.ToString("N0");
        DebtText.Text = debt.ToString("N0");
        RefundText.Text = "− " + refunds.ToString("N0");
        NetText.Text = (cash + card + transfer + debt - refunds).ToString("N0");

        QrCashText.Text = qrCash.ToString("N0");
        QrCardText.Text = qrCard.ToString("N0");
        QrTransferText.Text = qrTransfer.ToString("N0");
        QrMixedText.Text = qrMixed.ToString("N0");
        PlainCashText.Text = plainCash.ToString("N0");
        PlainCardText.Text = plainCard.ToString("N0");
        PlainTransferText.Text = plainTransfer.ToString("N0");
        PlainMixedText.Text = plainMixed.ToString("N0");
    }

    private async void Day_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DayPicker.SelectedDate is DateTime d && d.Date != _day)
        {
            _day = d.Date;
            await LoadAsync();
        }
    }

    private void PrevDay_Click(object sender, RoutedEventArgs e) => DayPicker.SelectedDate = _day.AddDays(-1);
    private void NextDay_Click(object sender, RoutedEventArgs e) => DayPicker.SelectedDate = _day.AddDays(1);
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (SalesList.SelectedItem is not SaleListDto row)
        {
            ShowMessage("Avval savdoni tanlang", error: true);
            return;
        }

        PrintButton.IsEnabled = false;
        try
        {
            var sale = await AppState.Backend.GetSaleAsync(row.Id);
            if (sale is null)
            {
                ShowMessage("Savdo topilmadi", error: true);
                return;
            }

            var receipt = ToReceiptData(sale);
            new ReceiptPreviewWindow(receipt) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
        finally
        {
            PrintButton.IsEnabled = true;
        }
    }

    private static ReceiptData ToReceiptData(SaleDto s) => new()
    {
        ReceiptNumber = s.ReceiptNumber,
        Date = s.CreatedAt.ToLocalTime(),
        StoreName = AppState.StoreName,
        Cashier = AppState.CashierDisplay,
        Payment = s.PaymentMethod,
        Total = s.TotalAmount,
        Paid = s.PaidAmount,
        Change = s.ChangeAmount,
        SaleId = s.Id,
        Lines = s.Items.Select(i => new ReceiptLine
        {
            Name = i.ProductName,
            Quantity = i.Quantity,
            Price = i.UnitPrice,
            LineTotal = i.LineTotal
        }).ToList(),
        QrCodeUrl = s.Receipt?.QrCodeUrl,
        FiscalSign = s.Receipt?.FiscalSign,
        FiscalModuleId = s.Receipt?.FiscalModuleId,
        FiscalRegistrationNumber = s.Receipt?.FiscalRegistrationNumber,
        FiscalReceiptNumber = s.Receipt?.FiscalReceiptNumber
    };

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

    private void HideMessage() => MessageText.Visibility = Visibility.Collapsed;
}
