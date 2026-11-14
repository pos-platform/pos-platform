using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>
/// Chek bo'yicha qaytarish (CONTRACT-10 §10.5): chek raqami/skaner → qatorlar (belgilash + miqdor, "Hammasi"),
/// sabab, preview (qaytariladigan summa, ayiriladigan keshbek, ushlab qolinadigan va beriladigan pul) →
/// tasdiq → refund cheki chop etiladi.
/// </summary>
public partial class RefundWindow : Window
{
    private readonly Guid? _initialSaleId;
    private List<SaleListDto> _sales = new();
    private SaleDto? _sale;
    private List<RefundRow> _rows = new();
    private RefundPreviewDto? _preview;
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private CancellationTokenSource? _previewCts;
    private Action<string>? _prevScan;
    private bool _busy;

    /// <summary>Eski backend refund-preview'ni bilmaydi — u holda faqat to'liq qaytarishga ruxsat (body e'tiborsiz qoladi).</summary>
    private bool _previewUnsupported;

    /// <summary>Muvaffaqiyatli qaytarish natijasi (oyna yopilgach chaqiruvchi uchun).</summary>
    public SaleDto? Refunded { get; private set; }

    public RefundWindow() : this(null) { }

    public RefundWindow(Guid? saleId)
    {
        InitializeComponent();
        _initialSaleId = saleId;
        _previewTimer.Tick += async (_, _) =>
        {
            _previewTimer.Stop();
            await RunPreviewAsync();
        };
        Loaded += async (_, _) =>
        {
            _prevScan = AppState.ScanOverride;
            AppState.ScanOverride = code => Dispatcher.Invoke(() =>
            {
                SearchBox.Text = code.Trim();
                _ = FindAndOpenAsync(SearchBox.Text);
            });
            await LoadAsync();
            if (_initialSaleId is { } id) await OpenSaleAsync(id);
            else SearchBox.Focus();
        };
        Closed += (_, _) =>
        {
            AppState.ScanOverride = _prevScan;
            _previewCts?.Cancel();
        };
    }

    // ───────────── savdolar ro'yxati ─────────────

    private async Task LoadAsync()
    {
        try
        {
            _sales = (await AppState.Backend.GetSalesAsync()).Where(s => s.Type == "Sale").ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim();
        SalesList.ItemsSource = string.IsNullOrEmpty(q)
            ? _sales
            : _sales.Where(s => s.ReceiptNumber.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private async void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await FindAndOpenAsync(SearchBox.Text);
    }

    private async Task FindAndOpenAsync(string text)
    {
        var q = text.Trim();
        if (q.Length == 0) return;
        var match = _sales.FirstOrDefault(s => string.Equals(s.ReceiptNumber, q, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            var partial = _sales.Where(s => s.ReceiptNumber.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            if (partial.Count == 1) match = partial[0];
        }
        if (match is null && Guid.TryParse(q, out var gid)) { await OpenSaleAsync(gid); return; }
        if (match is null)
        {
            ShowMessage($"\"{q}\" chek topilmadi (ro'yxatdagi savdolar orasida).", error: true);
            return;
        }
        SalesList.SelectedItem = match;
    }

    private async void SalesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SalesList.SelectedItem is SaleListDto s && s.Id != _sale?.Id)
            await OpenSaleAsync(s.Id);
    }

    // ───────────── tanlangan savdo ─────────────

    private async Task OpenSaleAsync(Guid id)
    {
        try
        {
            var sale = await AppState.Backend.GetSaleAsync(id);
            if (sale is null)
            {
                ShowMessage("Savdo topilmadi.", error: true);
                return;
            }
            ShowSale(sale);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private void ShowSale(SaleDto sale)
    {
        _sale = sale;
        _preview = null;
        _previewUnsupported = false;
        HideMessage();
        EmptyText.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        SaleTitle.Text = $"Chek {sale.ReceiptNumber} · {sale.TotalAmount:N0} so'm";
        var status = sale.Status switch
        {
            "Refunded" => "to'liq qaytarilgan",
            "PartiallyRefunded" => "qisman qaytarilgan",
            _ => "sotuv"
        };
        SaleSubtitle.Text = $"{sale.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm} · {status}" +
                            (sale.LoyaltyPointsEarned > 0 ? $" · keshbek +{sale.LoyaltyPointsEarned:N0} ball" : "") +
                            (sale.LoyaltyPointsRedeemed > 0 ? $" · ball bilan to'langan {sale.LoyaltyPointsRedeemed:N0}" : "");

        _rows = sale.Items.Select(i => new RefundRow(i, SchedulePreview)).ToList();
        ItemsList.ItemsSource = _rows;
        ReasonBox.Text = "";
        ClearPreview();

        if (sale.Type != "Sale" || sale.CanRefund == false || _rows.All(r => !r.CanRefund))
            ShowMessage("Bu chekda qaytariladigan tovar qolmagan.", error: true);
    }

    private void All_Click(object sender, RoutedEventArgs e)
    {
        foreach (var r in _rows.Where(r => r.CanRefund))
            r.SelectAll();
        SchedulePreview();
    }

    // ───────────── preview ─────────────

    private void SchedulePreview()
    {
        RefundButton.IsEnabled = false;
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private RefundRequest? BuildRequest(out string? error)
    {
        error = null;
        var items = new List<RefundItemRequest>();
        foreach (var r in _rows.Where(r => r.IsChecked))
        {
            if (!r.TryGetQuantity(out var qty) || qty <= 0)
            {
                error = $"{r.Item.ProductName}: miqdor noto'g'ri.";
                return null;
            }
            if (qty > r.Max)
            {
                error = $"{r.Item.ProductName}: ko'pi bilan {r.Max:0.###} qaytarish mumkin.";
                return null;
            }
            items.Add(new RefundItemRequest { SaleItemId = r.Item.Id, Quantity = qty });
        }
        if (items.Count == 0) return null;
        return new RefundRequest { Items = items, Reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? null : ReasonBox.Text.Trim() };
    }

    private bool IsFullRefund(RefundRequest req) =>
        _rows.Where(r => r.CanRefund).All(r => req.Items.Any(i => i.SaleItemId == r.Item.Id && i.Quantity >= r.Max));

    private async Task RunPreviewAsync()
    {
        if (_sale is null) return;
        var req = BuildRequest(out var error);
        if (req is null)
        {
            ClearPreview();
            if (error is not null) PvNote.Text = error;
            return;
        }

        // Lokal taxminiy summa (server javobi kelguncha / eski backend uchun).
        var local = req.Items.Sum(i =>
        {
            var it = _sale.Items.First(x => x.Id == i.SaleItemId);
            return it.Quantity > 0 ? Math.Round(it.LineTotal / it.Quantity * i.Quantity, 2) : 0;
        });

        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();
        try
        {
            var pv = await AppState.Backend.RefundPreviewAsync(_sale.Id, req, cts.Token);
            if (cts.IsCancellationRequested) return;
            _preview = pv;
            PvTotal.Text = $"{pv.RefundTotal:N0} so'm";
            PvPoints.Text = pv.PointsToReverse > 0
                ? $"−{pv.PointsToReverse:N0}" + (pv.PointsAvailable is { } av ? $" (balansda {av:N0})" : "")
                : "0";
            PvWithheld.Text = $"{pv.WithheldAmount:N0} so'm";
            PvCash.Text = $"{pv.CashToReturn:N0} so'm";
            PvNote.Text = pv.RedeemedPointsReturned is > 0
                ? $"Ball bilan to'langan qism ballga qaytadi: +{pv.RedeemedPointsReturned:N0} ball"
                : pv.WithheldAmount > 0 ? "Mijoz keshbek ballarini ishlatib bo'lgan — yetmagan qism puldan ushlab qolinadi." : "";
            RefundButton.IsEnabled = true;
        }
        catch (OperationCanceledException) { }
        catch (ApiException api) when (api.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
        {
            // Eski backend: qisman qaytarish yo'q — faqat to'liq chek qaytariladi.
            _previewUnsupported = true;
            _preview = null;
            PvTotal.Text = $"{local:N0} so'm (taxminiy)";
            PvPoints.Text = PvWithheld.Text = "—";
            PvCash.Text = $"{local:N0} so'm";
            var full = IsFullRefund(req);
            PvNote.Text = full
                ? "Server qisman qaytarishni qo'llamaydi — chek to'liq qaytariladi."
                : "Server qisman qaytarishni qo'llamaydi — faqat \"Hammasi\" (to'liq qaytarish) mumkin.";
            RefundButton.IsEnabled = full;
        }
        catch (ApiException api)
        {
            ClearPreview();
            PvNote.Text = api.Message;
        }
        catch (Exception ex)
        {
            ClearPreview();
            PvTotal.Text = $"{local:N0} so'm (taxminiy)";
            PvNote.Text = "Hisob-kitob olinmadi: " + ex.Message;
        }
    }

    private void ClearPreview()
    {
        _preview = null;
        PvTotal.Text = PvPoints.Text = PvWithheld.Text = PvCash.Text = "—";
        PvNote.Text = "";
        RefundButton.IsEnabled = false;
    }

    // ───────────── qaytarish ─────────────

    private async void DoRefund_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _sale is null) return;
        var req = BuildRequest(out var error);
        if (req is null)
        {
            ShowMessage(error ?? "Qaytariladigan qatorlarni belgilang.", error: true);
            return;
        }

        var cash = _preview?.CashToReturn;
        var confirm = $"{_sale.ReceiptNumber} chek bo'yicha {req.Items.Count} ta qator qaytarilsinmi?" +
                      (cash is { } c ? $"\n\nMijozga beriladigan pul: {c:N0} so'm" : "") +
                      (_preview is { WithheldAmount: > 0 } p ? $"\nKeshbek ushlab qolinadi: {p.WithheldAmount:N0} so'm" : "");
        if (MessageBox.Show(confirm, "Qaytarishni tasdiqlash", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _busy = true;
        RefundButton.IsEnabled = false;
        try
        {
            // Eski backend body'ni e'tiborsiz qoldiradi — faqat to'liq qaytarishda shu yerga yetamiz.
            var refund = _previewUnsupported
                ? await AppState.Backend.RefundAsync(_sale.Id)
                : await AppState.Backend.RefundAsync(_sale.Id, req);
            Refunded = refund;
            ShowMessage($"✓ Qaytarildi — chek {refund.ReceiptNumber}" +
                        (refund.RefundCashAmount is { } rc ? $" · mijozga {rc:N0} so'm" : ""), error: false);
            PrintRefundReceipt(refund, req.Reason);
            await LoadAsync();
            await OpenSaleAsync(_sale.Id);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
            RefundButton.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }

    private void PrintRefundReceipt(SaleDto refund, string? reason)
    {
        var receipt = SalesHistoryWindow.ToReceiptData(refund);
        receipt.IsRefund = true;
        receipt.RefundReason = reason;
        receipt.Cashier = AppState.CashierDisplay;
        receipt.Stir = AppState.OrgStir;
        receipt.Address = AppState.BranchAddress;
        receipt.LegalAddress = AppState.LegalAddress;
        receipt.Kassa = AppState.TerminalId;
        receipt.ExternalFiscalDevice = AppState.ExternalFiscalDevice;
        // Javobda bo'lmasa — preview qiymatlari.
        if (refund.LoyaltyWithheldAmount is null && _preview is not null) receipt.LoyaltyWithheldAmount = _preview.WithheldAmount;
        if (refund.LoyaltyPointsReversed is null && _preview is not null) receipt.LoyaltyPointsReversed = _preview.PointsToReverse;
        if (refund.RefundCashAmount is null && _preview is not null) receipt.RefundCashAmount = _preview.CashToReturn;

        if (AppState.PrinterEnabled)
        {
            try
            {
                new ReceiptPrinter().Print(receipt, AppState.PrinterName, showCodes: true);
                return;
            }
            catch (Exception ex)
            {
                ShowMessage("Qaytarildi, lekin chek chop etilmadi: " + ex.Message, error: true);
            }
        }
        new ReceiptPreviewWindow(receipt, ReceiptViewMode.History) { Owner = this }.ShowDialog();
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

    /// <summary>Qaytarish ro'yxatidagi qator: belgilash va miqdor (≤ refundableQuantity).</summary>
    private sealed class RefundRow : INotifyPropertyChanged
    {
        private readonly Action _changed;
        private bool _isChecked;
        private string _qtyText;

        public RefundRow(SaleItemDto item, Action changed)
        {
            Item = item;
            _changed = changed;
            Max = item.EffectiveRefundable;
            _qtyText = Max.ToString("0.###", CultureInfo.CurrentCulture);
        }

        public SaleItemDto Item { get; }
        public decimal Max { get; }
        public bool CanRefund => Max > 0;

        public string InfoText =>
            $"Sotilgan {Item.Quantity:0.###} × {Item.UnitPrice:N0} = {Item.LineTotal:N0}" +
            (Item.RefundedQuantity is > 0 ? $" · qaytarilgan {Item.RefundedQuantity:0.###}" : "");

        public string MaxText => CanRefund ? $"/ {Max:0.###}" : "qaytarilgan";

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value && CanRefund;
                OnChanged(nameof(IsChecked));
                _changed();
            }
        }

        public string QtyText
        {
            get => _qtyText;
            set
            {
                if (_qtyText == value) return;
                _qtyText = value;
                OnChanged(nameof(QtyText));
                if (CanRefund && !_isChecked && TryGetQuantity(out var q) && q > 0)
                {
                    _isChecked = true;
                    OnChanged(nameof(IsChecked));
                }
                _changed();
            }
        }

        public void SelectAll()
        {
            _qtyText = Max.ToString("0.###", CultureInfo.CurrentCulture);
            _isChecked = true;
            OnChanged(nameof(QtyText));
            OnChanged(nameof(IsChecked));
        }

        public bool TryGetQuantity(out decimal qty)
        {
            var s = (_qtyText ?? "").Trim().Replace(" ", "").Replace(",", ".");
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out qty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
