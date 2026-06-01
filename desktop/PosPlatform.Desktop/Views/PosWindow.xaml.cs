using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class PosWindow : Window
{
    private readonly ObservableCollection<CartLine> _cart = new();
    private List<ProductDto> _allProducts = new();
    private ShiftDto? _shift;
    private readonly ReceiptPrinter _printer = new();
    private ReceiptData? _lastReceipt;

    public PosWindow()
    {
        InitializeComponent();
        CartItems.ItemsSource = _cart;
        UserText.Text = $"{AppState.Username} ({AppState.Role})";
        Loaded += async (_, _) =>
        {
            await LoadShiftAsync();
            await LoadProductsAsync();
            SearchBox.Focus();
        };
    }

    // ---------- Yuklash ----------

    private async Task LoadShiftAsync()
    {
        try
        {
            _shift = await AppState.Backend.GetCurrentShiftAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
        RenderShiftBar();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = await AppState.Backend.GetProductsAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
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
            ShiftInfo.Text = "Smena yopiq — savdo uchun smenani oching";
            OpenShiftPanel.Visibility = Visibility.Visible;
            CloseShiftButton.Visibility = Visibility.Collapsed;
        }
    }

    // ---------- Mahsulot ro'yxati / qidiruv ----------

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        IEnumerable<ProductDto> items = _allProducts;
        if (!string.IsNullOrEmpty(q))
        {
            items = _allProducts.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(q)) ||
                p.MxikCode.ToLowerInvariant().Contains(q));
        }
        ProductsControl.ItemsSource = items.ToList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;

        var product =
            _allProducts.FirstOrDefault(p => p.Barcode != null &&
                                             string.Equals(p.Barcode, q, StringComparison.OrdinalIgnoreCase))
            ?? (ProductsControl.ItemsSource as IEnumerable<ProductDto>)?.FirstOrDefault();

        if (product is not null)
        {
            AddToCart(product);
            SearchBox.Clear();
        }
        else
        {
            ShowMessage($"Topilmadi: {q}", error: true);
        }
    }

    private void ProductCard_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is ProductDto p) AddToCart(p);
    }

    // ---------- Savatcha ----------

    private void AddToCart(ProductDto product)
    {
        var line = _cart.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null)
        {
            _cart.Add(new CartLine(product, 1));
        }
        else
        {
            line.Quantity += 1;
        }
        RecalcTotals();
    }

    private void Inc_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l) { l.Quantity += 1; RecalcTotals(); }
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l)
        {
            if (l.Quantity <= 1) _cart.Remove(l);
            else l.Quantity -= 1;
            RecalcTotals();
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is CartLine l) { _cart.Remove(l); RecalcTotals(); }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        PaidBox.Text = "0";
        RecalcTotals();
    }

    private decimal CartTotal() => _cart.Sum(l => l.LineTotal);

    private void RecalcTotals()
    {
        var total = CartTotal();
        TotalText.Text = total.ToString("N0");
        var paid = ParseDecimal(PaidBox.Text);
        var change = Math.Max(0, paid - total);
        ChangeText.Text = change.ToString("N0");
    }

    private void PaidBox_TextChanged(object sender, TextChangedEventArgs e) => RecalcTotals();

    // ---------- Smena ----------

    private async void OpenShift_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _shift = await AppState.Backend.OpenShiftAsync(ParseDecimal(OpeningCashBox.Text));
            RenderShiftBar();
            ShowMessage("Smena ochildi", error: false);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    private async void CloseShift_Click(object sender, RoutedEventArgs e)
    {
        var input = PromptInput("Sanab chiqilgan naqd summasini kiriting:", "Smenani yopish", "0");
        if (input is null) return;
        try
        {
            await AppState.Backend.CloseShiftAsync(ParseDecimal(input));
            _shift = null;
            RenderShiftBar();
            ShowMessage("Smena yopildi", error: false);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
    }

    // ---------- To'lash ----------

    private async void Checkout_Click(object sender, RoutedEventArgs e)
    {
        if (_shift is null) { ShowMessage("Avval smenani oching", error: true); return; }
        if (_cart.Count == 0) { ShowMessage("Savatcha bo'sh", error: true); return; }

        var total = CartTotal();
        var paid = ParseDecimal(PaidBox.Text);
        var payment = (PaymentCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        if (payment == "Cash" && paid < total)
        {
            ShowMessage("Berilgan summa yetarli emas", error: true);
            return;
        }

        Guid? debtorId = null;
        if (payment == "Debt")
        {
            if (DebtorCombo.SelectedItem is not DebtorDto d)
            {
                ShowMessage("Nasiya uchun qarzdorni tanlang", error: true);
                return;
            }
            debtorId = d.Id;
        }

        CheckoutButton.IsEnabled = false;
        try
        {
            var req = new CreateSaleRequest
            {
                PaymentMethod = payment,
                PaidAmount = payment == "Debt" ? 0 : (paid > 0 ? paid : total),
                DebtorId = debtorId,
                Items = _cart.Select(l => new SaleItemRequest { ProductId = l.Product.Id, Quantity = l.Quantity }).ToList()
            };
            // Chek ma'lumotini sotuvdan oldin yig'amiz.
            var receipt = new ReceiptData
            {
                Cashier = AppState.Username,
                Payment = payment,
                Total = total,
                Paid = req.PaidAmount,
                Lines = _cart.Select(l => new ReceiptLine
                {
                    Name = l.Product.Name, Quantity = l.Quantity, Price = l.Product.Price, LineTotal = l.LineTotal
                }).ToList()
            };

            var sale = await AppState.Backend.CreateSaleAsync(req);
            receipt.ReceiptNumber = sale.ReceiptNumber;
            receipt.Change = sale.ChangeAmount;
            _lastReceipt = receipt;
            ReprintButton.IsEnabled = true;

            ShowMessage($"✓ Chek {sale.ReceiptNumber} · Jami {sale.TotalAmount:N0} · Qaytim {sale.ChangeAmount:N0}", error: false);
            _cart.Clear();
            PaidBox.Text = "0";
            RecalcTotals();
            await LoadProductsAsync(); // qoldiq yangilandi

            if (AppState.PrinterEnabled) PrintReceiptSafe(receipt);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, error: true);
        }
        finally
        {
            CheckoutButton.IsEnabled = true;
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }

    // ---------- Nasiya (qarz) ----------

    private async void PaymentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DebtorPanel is null) return; // init paytida hali yaratilmagan bo'lishi mumkin
        var payment = (PaymentCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (payment == "Debt")
        {
            DebtorPanel.Visibility = Visibility.Visible;
            if (DebtorCombo.ItemsSource is null)
            {
                try { DebtorCombo.ItemsSource = await AppState.Backend.GetDebtorsAsync(); }
                catch (Exception ex) { ShowMessage(ex.Message, error: true); }
            }
        }
        else
        {
            DebtorPanel.Visibility = Visibility.Collapsed;
        }
    }

    // ---------- Klaviatura tezkor tugmalari ----------

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F2:
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                break;
            case Key.F9:
                Clear_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F12:
                Checkout_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    // ---------- Chek chop etish / qaytarish ----------

    private void Reprint_Click(object sender, RoutedEventArgs e)
    {
        if (_lastReceipt is not null) PrintReceiptSafe(_lastReceipt);
    }

    private void PrintReceiptSafe(ReceiptData receipt)
    {
        try
        {
            _printer.Print(receipt, AppState.PrinterName);
        }
        catch (Exception ex)
        {
            ShowMessage("Chek chop etilmadi: " + ex.Message, error: true);
        }
    }

    private async void Refund_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RefundWindow { Owner = this };
        dlg.ShowDialog();
        // Qaytarishdan keyin qoldiq o'zgargan bo'lishi mumkin.
        await LoadProductsAsync();
    }

    // ---------- Yordamchilar ----------

    private void ShowMessage(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x16, 0xA3, 0x4A));
        MessageText.Visibility = Visibility.Visible;
    }

    /// <summary>Oddiy modal kiritish dialogi (matn qaytaradi yoki bekor bo'lsa null).</summary>
    private string? PromptInput(string message, string title, string initial)
    {
        var dlg = new Window
        {
            Title = title, Width = 340, Height = 170, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize, Background = Brushes.White
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
