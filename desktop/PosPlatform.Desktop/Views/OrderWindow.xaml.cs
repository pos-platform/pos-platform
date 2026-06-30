using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>Restoran buyurtma oynasi — ovqat qo'shish, oshxonaga yuborish, hisobni yopish.</summary>
public partial class OrderWindow : Window
{
    private readonly TableDto _table;
    private OrderDto? _order;
    private List<DishDto> _allDishes = new();
    private readonly ObservableCollection<DishDto> _displayed = new();
    private readonly ObservableCollection<OrderLine> _cart = new();

    public OrderWindow(TableDto table)
    {
        InitializeComponent();
        _table = table;
        Icon = App.CreateLogoIcon();
        TableTitle.Text = $"Stol {_table.Name}" + (string.IsNullOrEmpty(_table.Hall) ? "" : $" · {_table.Hall}");
        DishesControl.ItemsSource = _displayed;
        OrderItems.ItemsSource = _cart;
        Loaded += async (_, _) =>
        {
            await LoadDishesAsync();
            await OpenOrderAsync();
            SearchBox.Focus();
        };
    }

    private async Task LoadDishesAsync()
    {
        try
        {
            _allDishes = await AppState.Backend.GetDishesAsync();
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); }
        ApplyFilter();
    }

    private async Task OpenOrderAsync()
    {
        try
        {
            _order = await AppState.Backend.OpenOrderAsync(new OpenOrderRequest { TableId = _table.Id });
            LoadCartFromOrder();
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); }
    }

    private void LoadCartFromOrder()
    {
        _cart.Clear();
        if (_order is null) return;
        foreach (var i in _order.Items)
        {
            _cart.Add(new OrderLine
            {
                ProductId = i.ProductId,
                Name = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Note = i.Note,
                SentToKitchen = i.SentToKitchen
            });
        }
        RecalcTotal();
    }

    private void ApplyFilter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        _displayed.Clear();
        foreach (var d in _allDishes.Where(d => string.IsNullOrEmpty(q) || d.Name.ToLowerInvariant().Contains(q)))
            _displayed.Add(d);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void Dish_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not DishDto dish) return;
        // Yuborilmagan, izohsiz qatorni topib miqdorini oshiramiz; aks holda yangi qator.
        var line = _cart.FirstOrDefault(l => l.ProductId == dish.Id && !l.SentToKitchen && string.IsNullOrEmpty(l.Note));
        if (line is null)
            _cart.Add(new OrderLine { ProductId = dish.Id, Name = dish.Name, UnitPrice = dish.Price, Quantity = 1 });
        else
            line.Quantity += 1;
        RecalcTotal();
    }

    private void Inc_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is OrderLine l) { l.Quantity += 1; RecalcTotal(); }
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is OrderLine l)
        {
            if (l.Quantity <= 1) { if (!ConfirmRemove(l)) return; _cart.Remove(l); }
            else l.Quantity -= 1;
            RecalcTotal();
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is OrderLine l && ConfirmRemove(l)) { _cart.Remove(l); RecalcTotal(); }
    }

    private bool ConfirmRemove(OrderLine l)
    {
        if (!l.SentToKitchen) return true;
        return MessageBox.Show($"\"{l.Name}\" oshxonaga yuborilgan. O'chirilsinmi?", "Tasdiqlash",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private decimal CartTotal() => _cart.Sum(l => l.LineTotal);

    private void RecalcTotal()
    {
        if (TotalText is null) return;
        TotalText.Text = CartTotal().ToString("N0");
    }

    /// <summary>Joriy savatchani serverga saqlaydi va buyurtmani qayta yuklaydi.</summary>
    private async Task<bool> SaveAsync()
    {
        if (_order is null) return false;
        try
        {
            var items = _cart.Select(l => new OrderItemRequest
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                Note = string.IsNullOrWhiteSpace(l.Note) ? null : l.Note.Trim()
            }).ToList();
            _order = await AppState.Backend.SaveOrderItemsAsync(_order.Id, items);
            LoadCartFromOrder();
            return true;
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); return false; }
    }

    private async void SendToKitchen_Click(object sender, RoutedEventArgs e)
    {
        if (_order is null) return;
        if (_cart.Count == 0) { ShowMessage("Buyurtma bo'sh", true); return; }
        if (!await SaveAsync()) return;
        try
        {
            await AppState.Backend.SendToKitchenAsync(_order.Id);
            await ReloadOrderAsync();
            ShowMessage("✓ Oshxonaga yuborildi", false);
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); }
    }

    private async Task ReloadOrderAsync()
    {
        if (_order is null) return;
        try
        {
            var fresh = await AppState.Backend.GetOpenOrderByTableAsync(_table.Id);
            if (fresh is not null) { _order = fresh; LoadCartFromOrder(); }
        }
        catch { /* ignore */ }
    }

    private async void Pay_Click(object sender, RoutedEventArgs e)
    {
        if (_order is null) return;
        if (_cart.Count == 0) { ShowMessage("Buyurtma bo'sh", true); return; }
        if (!await SaveAsync()) return;

        var pay = PromptPayment(CartTotal());
        if (pay is null) return;

        PayButton.IsEnabled = false;
        try
        {
            var total = CartTotal();
            var sale = await AppState.Backend.PayOrderAsync(_order.Id, new PayOrderRequest
            {
                PaymentMethod = pay.Value.method,
                PaidAmount = pay.Value.method == "Cash" ? pay.Value.paid : total,
                ServiceChargePercent = 0
            });

            var receipt = new ReceiptData
            {
                ReceiptNumber = sale.ReceiptNumber,
                StoreName = AppState.StoreName,
                Cashier = AppState.CashierDisplay,
                Payment = pay.Value.method,
                Total = sale.TotalAmount,
                Paid = pay.Value.method == "Cash" ? pay.Value.paid : sale.TotalAmount,
                Change = sale.ChangeAmount,
                Lines = _cart.Select(l => new ReceiptLine
                {
                    Name = l.Name, Quantity = l.Quantity, Price = l.UnitPrice, LineTotal = l.LineTotal
                }).ToList()
            };
            new ReceiptPreviewWindow(receipt) { Owner = this }.ShowDialog();

            DialogResult = true;
            Close();
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); }
        finally { PayButton.IsEnabled = true; }
    }

    private async void CancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_order is null) { Close(); return; }
        if (MessageBox.Show("Buyurtma bekor qilinsinmi?", "Tasdiqlash",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await AppState.Backend.CancelOrderAsync(_order.Id);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { ShowMessage(ex.Message, true); }
    }

    private async void Close_Click(object sender, RoutedEventArgs e)
    {
        // Yopishdan oldin joriy holatni saqlaymiz (ochiq buyurtma stolda qoladi).
        if (_order is not null && _cart.Count > 0) await SaveAsync();
        Close();
    }

    private void ShowMessage(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xDC, 0x26, 0x26)
            : Color.FromRgb(0x16, 0xA3, 0x4A));
        MessageText.Visibility = Visibility.Visible;
    }

    /// <summary>To'lov dialogi — usul (Cash/Card) va berilgan summa.</summary>
    private (string method, decimal paid)? PromptPayment(decimal total)
    {
        var dlg = new Window
        {
            Title = "Hisobni yopish", Width = 320, Height = 230,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
            ResizeMode = ResizeMode.NoResize, Background = Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = $"Jami: {total:N0} so'm", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
        panel.Children.Add(new TextBlock { Text = "To'lov turi:" });
        var combo = new ComboBox { Height = 30, Margin = new Thickness(0, 4, 0, 10) };
        combo.Items.Add(new ComboBoxItem { Content = "Naqd", Tag = "Cash", IsSelected = true });
        combo.Items.Add(new ComboBoxItem { Content = "Karta", Tag = "Card" });
        panel.Children.Add(combo);
        panel.Children.Add(new TextBlock { Text = "Berilgan summa:" });
        var paidBox = new TextBox { Text = total.ToString("0"), Height = 30, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 4, 0, 10) };
        panel.Children.Add(paidBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        (string, decimal)? result = null;
        var ok = new Button { Content = "To'lash", Width = 90, Height = 32, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Bekor", Width = 80, Height = 32, IsCancel = true };
        ok.Click += (_, _) =>
        {
            var method = (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
            var paid = ParseDecimal(paidBox.Text);
            if (method == "Cash" && paid < total)
            {
                MessageBox.Show("Berilgan summa yetarli emas", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            result = (method, paid);
            dlg.DialogResult = true;
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        dlg.Content = panel;
        return dlg.ShowDialog() == true && result is not null
            ? (result.Value.Item1, result.Value.Item2)
            : null;
    }

    private static decimal ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim().Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
