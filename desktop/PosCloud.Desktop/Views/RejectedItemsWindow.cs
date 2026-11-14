using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>
/// "Kuryer qaytargan tovarlar" (CONTRACT-10 §10.5) — eshik oldida mijoz rad etgan tovarlar.
/// Ro'yxat GET /api/online-orders/rejected-pending; "Qabul qilindi" → POST {id}/rejected-received (qoldiq qaytadi).
/// </summary>
public class RejectedItemsWindow : Window
{
    private readonly StackPanel _rows = new();
    private readonly TextBlock _status = new() { FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private bool _busy;

    public RejectedItemsWindow()
    {
        Title = "Kuryer qaytargan tovarlar";
        Width = 620;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xED, 0xE7));

        var refresh = new Button { Content = "Yangilash", Padding = new Thickness(12, 4, 12, 4) };
        refresh.Click += async (_, _) => await LoadAsync();

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(refresh, Dock.Right);
        header.Children.Add(refresh);
        header.Children.Add(new TextBlock
        {
            Text = "Eshik oldida rad etilgan, do'konga qaytariladigan tovarlar", FontSize = 15, FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
        });

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(_status);
        root.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _rows });
        Content = root;

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var list = await AppState.Backend.GetRejectedPendingAsync();
            _rows.Children.Clear();
            foreach (var o in list.OrderBy(x => x.CreatedAt))
                _rows.Children.Add(BuildRow(o));
            if (list.Count == 0) ShowStatus("Qaytariladigan tovar yo'q.", error: false);
        }
        catch (Exception ex)
        {
            ShowStatus("Ro'yxat olinmadi: " + ex.Message, error: true);
        }
    }

    private UIElement BuildRow(OnlineOrderDto o)
    {
        var accept = new Button
        {
            Content = "Qabul qilindi", Height = 36, Padding = new Thickness(12, 0, 12, 0), FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45)), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
        };
        accept.Click += async (_, _) => await AcceptAsync(o);

        var info = new StackPanel();
        info.Children.Add(new TextBlock { Text = $"Buyurtma #{o.Number}", FontSize = 15, FontWeight = FontWeights.Bold });
        info.Children.Add(new TextBlock
        {
            Text = (o.HasCourier ? o.CourierText : "Kuryer: —") + $" · {o.CustomerName}",
            FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x5B, 0x66, 0x60))
        });
        foreach (var i in o.RejectedItems ?? new())
            info.Children.Add(new TextBlock
            {
                Text = $"• {i.ProductName ?? "—"} × {i.Quantity:0.###}" + (string.IsNullOrWhiteSpace(i.Reason) ? "" : $" — {i.Reason}"),
                FontSize = 13, TextWrapping = TextWrapping.Wrap
            });

        var dock = new DockPanel();
        DockPanel.SetDock(accept, Dock.Right);
        dock.Children.Add(accept);
        dock.Children.Add(info);

        return new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush(Color.FromRgb(0xE4, 0xE2, 0xDA)), BorderThickness = new Thickness(1),
            Child = dock
        };
    }

    private async Task AcceptAsync(OnlineOrderDto o)
    {
        if (_busy) return;
        if (MessageBox.Show($"#{o.Number} — kuryer rad etilgan tovarlarni do'konga topshirdimi?\nTovar qoldig'i qaytadi.",
                "Kuryer qaytargan tovarlar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _busy = true;
        try
        {
            await AppState.Backend.RejectedReceivedAsync(o.Id);
            ShowStatus($"✓ #{o.Number}: tovarlar qabul qilindi.", error: false);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, error: true);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ShowStatus(string text, bool error)
    {
        _status.Text = text;
        _status.Foreground = error ? Brushes.Firebrick : new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45));
    }
}
