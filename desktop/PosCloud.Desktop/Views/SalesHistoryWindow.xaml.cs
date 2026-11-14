using System.Windows;
using System.Windows.Media;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>Tanlangan kun savdolari ro'yxati: kunlik jami va chekni qayta chop etish.</summary>
public partial class SalesHistoryWindow : Window
{
    private const string AllTerminals = "Barcha kassalar";

    private DateTime _day = DateTime.Today;
    private List<SaleListDto> _allSales = new();
    private bool _suppressFilterEvent;

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
            _allSales = await AppState.Backend.GetSalesByDayAsync(_day);
            RebuildTerminalFilter();
            ApplyFilter();
            HideMessage();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    /// <summary>
    /// Kun savdolarida uchragan kassalar ro'yxatini tanlagichga yozadi.
    /// Standart tanlov — shu PC'ning o'z kassasi (har kassa avval o'z savdolarini ko'radi);
    /// kassa ID sozlanmagan bo'lsa yoki foydalanuvchi boshqa kassani tanlagan bo'lsa, o'sha saqlanadi.
    /// </summary>
    private void RebuildTerminalFilter()
    {
        var previous = TerminalFilter.SelectedItem as string;
        var terminals = _allSales
            .Select(s => s.TerminalLabel)
            .Distinct()
            .OrderBy(t => t == SaleListDto.NoTerminal) // "Kassasiz" oxirida
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Shu PC'ning kassasi — ro'yxatda savdosi bo'lmasa ham tanlanishi uchun qo'shiladi.
        var own = string.IsNullOrWhiteSpace(AppState.TerminalId) ? null : AppState.TerminalId!.Trim();
        if (own is not null && !terminals.Contains(own))
            terminals.Insert(0, own);

        _suppressFilterEvent = true;
        TerminalFilter.ItemsSource = new[] { AllTerminals }.Concat(terminals).ToList();
        TerminalFilter.SelectedItem =
            previous is not null && (previous == AllTerminals || terminals.Contains(previous)) ? previous
            : own ?? AllTerminals;
        _suppressFilterEvent = false;
    }

    /// <summary>Tanlangan kassa bo'yicha ro'yxat va jamilarni qayta hisoblaydi.</summary>
    private void ApplyFilter()
    {
        var selected = TerminalFilter.SelectedItem as string ?? AllTerminals;
        var visible = selected == AllTerminals
            ? _allSales
            : _allSales.Where(s => s.TerminalLabel == selected).ToList();

        SalesList.ItemsSource = visible;
        UpdateSummary(visible);
        UpdateTerminalBreakdown(selected == AllTerminals);
    }

    /// <summary>Kassalar kesimi jadvali — faqat "Barcha kassalar" tanlanganda va 2+ kassa bo'lsa ko'rinadi.</summary>
    private void UpdateTerminalBreakdown(bool showAll)
    {
        var groups = _allSales
            .GroupBy(s => s.TerminalLabel)
            .OrderBy(g => g.Key == SaleListDto.NoTerminal)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!showAll || groups.Count < 2)
        {
            TerminalBreakdownPanel.Visibility = Visibility.Collapsed;
            TerminalBreakdown.ItemsSource = null;
            return;
        }

        TerminalBreakdown.ItemsSource = groups.Select(g => TerminalTotals.From(g.Key, g)).ToList();
        TerminalBreakdownPanel.Visibility = Visibility.Visible;
    }

    private void TerminalFilter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressFilterEvent) return;
        ApplyFilter();
    }

    /// <summary>Bitta kassa bo'yicha kunlik jamilar (kesim jadvali qatori).</summary>
    private sealed class TerminalTotals
    {
        public string Terminal { get; init; } = "";
        public int Count { get; init; }
        public decimal Cash { get; init; }
        public decimal Card { get; init; }
        public decimal Transfer { get; init; }
        public decimal Net { get; init; }

        public string CountLabel => Count.ToString();
        public string CashLabel => Cash.ToString("N0");
        public string CardLabel => Card.ToString("N0");
        public string TransferLabel => Transfer.ToString("N0");
        public string NetLabel => Net.ToString("N0");

        public static TerminalTotals From(string terminal, IEnumerable<SaleListDto> sales)
        {
            int count = 0;
            decimal cash = 0, card = 0, transfer = 0, debt = 0, refunds = 0;
            foreach (var s in sales)
            {
                if (s.Type == "Refund") { refunds += s.TotalAmount; continue; }
                count++;
                switch (s.PaymentMethod)
                {
                    case "Card": card += s.TotalAmount; break;
                    case "Transfer": transfer += s.TotalAmount; break;
                    case "Debt": debt += s.TotalAmount; break;
                    default: cash += s.TotalAmount; break; // Cash va Mixed
                }
            }
            return new TerminalTotals
            {
                Terminal = terminal,
                Count = count,
                Cash = cash,
                Card = card,
                Transfer = transfer,
                Net = cash + card + transfer + debt - refunds
            };
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
            new ReceiptPreviewWindow(receipt, ReceiptViewMode.History) { Owner = this }.ShowDialog();
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

    /// <summary>
    /// Tarixdan qayta chop etish uchun chek — sof NUSXA: fiskal maydonlar (QR, FB, RS, chek raqami)
    /// ataylab olinmaydi. Sabab: savdo chop etilgandan keyin fiskalizatsiya bo'lgan bo'lishi mumkin,
    /// u holda nusxada asl qog'ozda bo'lmagan QR paydo bo'lardi. Tarix o'tmishni ko'rsatadi, hujjat
    /// yaratmaydi. Fiskal chek kerak bo'lsa — asl chek yoki /admin/fiscal-audit orqali.
    /// </summary>
    internal static ReceiptData ToReceiptData(SaleDto s) => new()
    {
        ReceiptNumber = s.ReceiptNumber,
        Date = s.CreatedAt.ToLocalTime(),
        StoreName = AppState.StoreName,
        Cashier = AppState.CashierDisplay,
        Payment = s.PaymentMethod,
        Total = s.TotalAmount,
        Paid = s.PaidAmount,
        Change = s.ChangeAmount,
        CardAmount = s.CardAmount,
        SaleId = s.Id,
        LoyaltyPointsEarned = s.LoyaltyPointsEarned,
        LoyaltyPointsRedeemed = s.LoyaltyPointsRedeemed,
        IsRefund = s.Type == "Refund",
        LoyaltyWithheldAmount = s.LoyaltyWithheldAmount ?? 0,
        LoyaltyPointsReversed = s.LoyaltyPointsReversed ?? 0,
        RefundCashAmount = s.RefundCashAmount,
        Lines = s.Items.Select(i => new ReceiptLine
        {
            Name = i.ProductName,
            Quantity = i.Quantity,
            Price = i.UnitPrice,
            LineTotal = i.LineTotal,
            Discount = i.DiscountAmount,
            MxikCode = i.MxikCode,
            Unit = i.Unit,
            IsVat = i.VatPercent > 0,
            Barcode = i.Barcode,
            CommitentTin = i.CommitentTin
        }).ToList()
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
        // Qisman qaytarish (10.5): qatorlar, miqdor, sabab va preview RefundWindow'da.
        var dlg = new RefundWindow(sale.Id) { Owner = this };
        dlg.ShowDialog();
        if (dlg.Refunded is { } refund)
            ShowMessage($"✓ Qaytarildi — chek {refund.ReceiptNumber}", error: false);
        await LoadAsync();
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
