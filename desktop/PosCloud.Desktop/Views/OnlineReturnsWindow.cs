using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>
/// "Qaytarishlar" (CONTRACT-10 §10.5) — mijozlarning yetkazilgandan keyingi qaytarish so'rovlari.
/// Ro'yxat GET /api/online-returns; tafsilot (rasmlar bilan) va holatga qarab amallar:
/// tasdiq (kuryer olib keladi / mijoz olib keladi), rad, kuryer biriktirish, "Do'konga keldi", "Pul berildi".
/// SignalR ReturnCreated/ReturnUpdated'da ro'yxat yangilanadi (yangi so'rovda ovozni hub chaladi).
/// </summary>
public class OnlineReturnsWindow : Window
{
    private static readonly string[] OpenStatuses = ["Requested", "Approved", "PickupAssigned", "PickedUp", "Received"];

    private readonly OnlineOrdersHub? _hub;
    private readonly ListBox _list = new() { FontSize = 13, HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox _filter = new() { Width = 160, Margin = new Thickness(0, 0, 8, 0) };
    private readonly StackPanel _detail = new();
    private readonly TextBlock _status = new() { FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private OnlineReturnDto? _selected;
    private bool _busy;

    public OnlineReturnsWindow(OnlineOrdersHub? hub)
    {
        _hub = hub;
        Title = "Qaytarishlar";
        Width = 1100;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xED, 0xE7));

        _filter.ItemsSource = new[] { "Faol", "Hammasi" };
        _filter.SelectedIndex = 0;
        _filter.SelectionChanged += async (_, _) => await LoadAsync();

        var refresh = new Button { Content = "Yangilash", Padding = new Thickness(12, 4, 12, 4) };
        refresh.Click += async (_, _) => await LoadAsync();

        var tools = new StackPanel { Orientation = Orientation.Horizontal };
        tools.Children.Add(_filter);
        tools.Children.Add(refresh);

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(tools, Dock.Right);
        header.Children.Add(tools);
        header.Children.Add(new TextBlock { Text = "Qaytarish so'rovlari", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });

        _list.ItemTemplate = BuildItemTemplate();
        _list.SelectionChanged += async (_, _) =>
        {
            if (_list.SelectedItem is OnlineReturnDto r) await SelectAsync(r.Id);
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(_list, 0);
        grid.Children.Add(_list);

        var detailBorder = new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(12), Padding = new Thickness(14), Margin = new Thickness(12, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xDE, 0xDB, 0xD2)), BorderThickness = new Thickness(1),
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _detail }
        };
        Grid.SetColumn(detailBorder, 1);
        grid.Children.Add(detailBorder);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(_status);
        root.Children.Add(grid);
        Content = root;

        ShowEmpty();

        Loaded += async (_, _) =>
        {
            if (_hub is not null)
            {
                _hub.ReturnCreated += OnHubEvent;
                _hub.ReturnUpdated += OnHubEvent;
            }
            await LoadAsync();
        };
        Closed += (_, _) =>
        {
            if (_hub is not null)
            {
                _hub.ReturnCreated -= OnHubEvent;
                _hub.ReturnUpdated -= OnHubEvent;
            }
        };
    }

    private void OnHubEvent(OnlineReturnEvent e) => Dispatcher.InvokeAsync(async () =>
    {
        await LoadAsync();
        if (_selected?.Id == e.Id) await SelectAsync(e.Id);
    });

    private static DataTemplate BuildItemTemplate()
    {
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(MarginProperty, new Thickness(4, 6, 4, 6));
        var title = new FrameworkElementFactory(typeof(TextBlock));
        title.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(OnlineReturnDto.Title)));
        title.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        title.SetValue(TextBlock.FontSizeProperty, 14.0);
        var line = new FrameworkElementFactory(typeof(TextBlock));
        line.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(OnlineReturnDto.Line)));
        line.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x5B, 0x66, 0x60)));
        line.SetValue(TextBlock.FontSizeProperty, 12.0);
        panel.AppendChild(title);
        panel.AppendChild(line);
        return new DataTemplate { VisualTree = panel };
    }

    private async Task LoadAsync()
    {
        try
        {
            var all = await AppState.Backend.GetOnlineReturnsAsync();
            var showAll = _filter.SelectedIndex == 1;
            var list = all
                .Where(r => showAll || OpenStatuses.Contains(r.Status))
                .OrderBy(r => r.Status == "Requested" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
            var keep = _selected?.Id;
            _list.ItemsSource = list;
            if (keep is { } k && list.FirstOrDefault(r => r.Id == k) is { } again)
                _list.SelectedItem = again;
            if (list.Count == 0) ShowStatus(showAll ? "Qaytarishlar yo'q." : "Faol qaytarish so'rovlari yo'q.", error: false);
        }
        catch (Exception ex)
        {
            ShowStatus("Ro'yxat olinmadi: " + ex.Message, error: true);
        }
    }

    private async Task SelectAsync(Guid id)
    {
        try
        {
            ShowReturn(await AppState.Backend.GetOnlineReturnAsync(id));
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, error: true);
        }
    }

    private void ShowEmpty()
    {
        _selected = null;
        _detail.Children.Clear();
        _detail.Children.Add(new TextBlock { Text = "Qaytarish so'rovini tanlang", Foreground = Brushes.Gray, FontSize = 14, Margin = new Thickness(0, 20, 0, 0) });
    }

    private void ShowReturn(OnlineReturnDto r)
    {
        _selected = r;
        _detail.Children.Clear();

        void Text(string text, double size = 13, bool bold = false, Brush? color = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _detail.Children.Add(new TextBlock
            {
                Text = text, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap,
                Foreground = color ?? new SolidColorBrush(Color.FromRgb(0x3A, 0x45, 0x40)), Margin = new Thickness(0, 2, 0, 2)
            });
        }

        _detail.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xFD, 0xEB, 0xDD)), CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock { Text = r.StatusText, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x34, 0x12)) }
        });
        Text(r.Title, 20, bold: true);
        Text(string.Join(" · ", new[] { r.CustomerName, r.CustomerPhone }.Where(x => !string.IsNullOrWhiteSpace(x))));
        Text($"Sabab: {r.ReasonText}" + (string.IsNullOrWhiteSpace(r.Comment) ? "" : $" — «{r.Comment!.Trim()}»"));
        Text($"Pul qaytarish usuli: {r.RefundMethodText}" + (r.MethodText.Length > 0 ? $" · {r.MethodText}" : ""));
        if (!string.IsNullOrWhiteSpace(r.CourierName)) Text("Kuryer: " + r.CourierName, color: new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45)));
        if (!string.IsNullOrWhiteSpace(r.RejectReason)) Text("Rad etish sababi: " + r.RejectReason, color: Brushes.Firebrick);
        Text($"So'rov: {r.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}", 12);

        // Tovarlar
        Text("Tovarlar", 14, bold: true);
        foreach (var i in r.Items ?? new())
            Text($"• {i.ProductName} × {i.Quantity:0.###} — {i.Quantity * i.UnitPrice:N0} so'm");
        Text($"Tovarlar summasi: {r.ItemsTotal:N0} so'm", bold: true);
        if (r.RefundAmount > 0) Text($"Qaytarilgan summa: {r.RefundAmount:N0} so'm", bold: true);
        if (r.LoyaltyWithheldAmount > 0) Text($"Keshbek ushlab qolindi: {r.LoyaltyWithheldAmount:N0} so'm", color: new SolidColorBrush(Color.FromRgb(0x9A, 0x34, 0x12)));

        // Rasmlar
        var photos = (r.PhotoUrls ?? new()).Where(u => !string.IsNullOrWhiteSpace(u)).Select(FullUrl).ToList();
        if (photos.Count > 0)
        {
            Text("Rasmlar (kattalashtirish uchun bosing)", 14, bold: true);
            var wrap = new WrapPanel();
            foreach (var url in photos)
                wrap.Children.Add(BuildPhoto(url));
            _detail.Children.Add(wrap);
        }

        // Amallar
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        void Action(string text, bool primary, Func<Task> run)
        {
            var b = new Button
            {
                Content = text, Height = 40, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 6, 6), FontWeight = FontWeights.SemiBold,
                Background = primary ? new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45)) : new SolidColorBrush(Color.FromRgb(0xF1, 0xEF, 0xEA)),
                Foreground = primary ? Brushes.White : Brushes.Black, Cursor = Cursors.Hand
            };
            b.Click += async (_, _) =>
            {
                if (_busy) return;
                _busy = true;
                actions.IsEnabled = false;
                try { await run(); }
                catch (Exception ex) { ShowStatus(ex.Message, error: true); }
                finally { _busy = false; actions.IsEnabled = true; }
            };
            actions.Children.Add(b);
        }

        switch (r.Status)
        {
            case "Requested":
                Action("Tasdiqlash · kuryer olib keladi", true, () => Do(r, "approve", new { method = "CourierPickup" }, "tasdiqlandi (kuryer olib keladi)"));
                Action("Tasdiqlash · mijoz olib keladi", false, () => Do(r, "approve", new { method = "BringToStore" }, "tasdiqlandi (mijoz do'konga olib keladi)"));
                Action("Rad etish", false, () => Reject(r));
                break;
            case "Approved":
                if (r.Method == "CourierPickup")
                    Action("Kuryer biriktirish", true, () => AssignCourier(r));
                Action("Do'konga keldi", r.Method != "CourierPickup", () => Received(r));
                break;
            case "PickupAssigned":
                Action("Kuryerni almashtirish", false, () => AssignCourier(r));
                Action("Do'konga keldi", true, () => Received(r));
                break;
            case "PickedUp":
                Action("Do'konga keldi", true, () => Received(r));
                break;
            case "Received":
                Action(r.RefundMethod == "Bonus" ? "Ball berildi" : "Pul berildi", true, () => Refunded(r));
                break;
        }
        if (r.RefundSaleId is { } saleId)
            Action("Qaytarish cheki", false, () => ShowRefundReceipt(saleId));
        _detail.Children.Add(actions);
    }

    private static string FullUrl(string url) =>
        url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : AppState.BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');

    private static UIElement BuildPhoto(string url)
    {
        var holder = new Border
        {
            Width = 130, Height = 130, Margin = new Thickness(0, 4, 8, 4), CornerRadius = new CornerRadius(8), Cursor = Cursors.Hand,
            Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xEF, 0xEA)), ToolTip = url
        };
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(url);
            img.DecodePixelWidth = 260;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            holder.Child = new Image { Source = img, Stretch = Stretch.UniformToFill };
        }
        catch
        {
            holder.Child = new TextBlock { Text = "Rasm ochilmadi", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }
        holder.MouseLeftButtonUp += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* brauzer yo'q */ }
        };
        return holder;
    }

    // ───────────── amallar ─────────────

    private async Task Do(OnlineReturnDto r, string action, object? body, string done)
    {
        var updated = await AppState.Backend.OnlineReturnActionAsync(r.Id, action, body);
        ShowStatus($"✓ Qaytarish #{r.Number} {done}.", error: false);
        await LoadAsync();
        if (updated is not null && updated.Id != Guid.Empty) ShowReturn(updated);
        else await SelectAsync(r.Id);
    }

    private async Task Reject(OnlineReturnDto r)
    {
        var reason = PromptWindow.Ask(this, "Rad etish", $"Qaytarish #{r.Number} — rad etish sababi (mijozga ko'rinadi):");
        if (string.IsNullOrWhiteSpace(reason)) return;
        await Do(r, "reject", new { reason }, "rad etildi");
    }

    private async Task AssignCourier(OnlineReturnDto r)
    {
        List<OnlineCourierDto> couriers = new();
        string? loadError = null;
        try { couriers = await AppState.Backend.GetOnlineCouriersAsync(r.BranchId); }
        catch (Exception ex) { loadError = ex.Message; }

        var pick = CourierPickWindow.Ask(this, r.Number, couriers, loadError);
        if (pick is null) return;
        if (pick.Manual || pick.Selected is null)
        {
            ShowStatus("Qaytarishga faqat tizimdagi kuryer akkaunti biriktiriladi.", error: true);
            return;
        }
        await Do(r, "assign-courier", new { courierId = pick.Selected.Id }, $"kuryer biriktirildi: {pick.Selected.Name}");
    }

    private async Task Received(OnlineReturnDto r)
    {
        if (MessageBox.Show(
                $"Qaytarish #{r.Number} tovarlari do'konga keldimi?\n\nQisman qaytarish bajariladi: qoldiq qaytadi, berilgan keshbek ayiriladi" +
                (r.RefundMethod == "Bonus" ? ", summa mijozga ball sifatida qaytadi." : "."),
                "Do'konga keldi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        await Do(r, "received", null, "qabul qilindi");
    }

    private async Task Refunded(OnlineReturnDto r)
    {
        var amount = r.RefundAmount > 0 ? r.RefundAmount : r.ItemsTotal;
        if (MessageBox.Show($"Qaytarish #{r.Number}: mijozga {amount:N0} so'm ({r.RefundMethodText}) berildimi?",
                "Pul berildi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        await Do(r, "refunded", null, "yakunlandi (pul berildi)");
    }

    private async Task ShowRefundReceipt(Guid saleId)
    {
        var sale = await AppState.Backend.GetSaleAsync(saleId);
        if (sale is null)
        {
            ShowStatus("Qaytarish cheki topilmadi.", error: true);
            return;
        }
        new ReceiptPreviewWindow(SalesHistoryWindow.ToReceiptData(sale), ReceiptViewMode.History) { Owner = this }.ShowDialog();
    }

    private void ShowStatus(string text, bool error)
    {
        _status.Text = text;
        _status.Foreground = error ? Brushes.Firebrick : new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45));
    }
}
