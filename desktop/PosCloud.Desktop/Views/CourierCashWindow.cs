using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>
/// "Kuryer pullari" — kassaga hali topshirilmagan kuryer naqdlari (GET /api/online-orders/courier-cash).
/// "Qabul qildim" → POST {id}/cash-handover: server kassaga Income harakati yozadi.
/// </summary>
public class CourierCashWindow : Window
{
    private readonly StackPanel _rows = new();
    private readonly TextBlock _status = new() { FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private readonly TextBlock _total = new() { FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 0) };
    private bool _busy;

    /// <summary>Oynada kamida bitta naqd qabul qilindi.</summary>
    public bool Changed { get; private set; }

    public CourierCashWindow()
    {
        Title = "Kuryer pullari";
        Width = 560;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xED, 0xE7));

        var refresh = new Button { Content = "Yangilash", Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Right };
        refresh.Click += async (_, _) => await LoadAsync();

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(refresh, Dock.Right);
        header.Children.Add(refresh);
        header.Children.Add(new TextBlock { Text = "Kassaga topshirilmagan kuryer naqdlari", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });

        var bottom = new StackPanel();
        bottom.Children.Add(_total);
        bottom.Children.Add(_status);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(bottom);
        root.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _rows });
        Content = root;

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var list = await AppState.Backend.GetCourierCashAsync();
            _rows.Children.Clear();
            foreach (var c in list.OrderBy(x => x.DeliveredAt ?? DateTime.MaxValue))
                _rows.Children.Add(BuildRow(c));
            _total.Text = list.Count == 0 ? "Topshirilmagan naqd yo'q." : $"Jami: {list.Sum(x => x.Amount):N0} so'm · {list.Count} ta buyurtma";
        }
        catch (Exception ex)
        {
            ShowStatus("Ro'yxat olinmadi: " + ex.Message, error: true);
        }
    }

    private UIElement BuildRow(CourierCashDto c)
    {
        var accept = new Button
        {
            Content = "Qabul qildim", Height = 36, Padding = new Thickness(12, 0, 12, 0), FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x6B, 0x45)), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
        };
        accept.Click += async (_, _) => await AcceptAsync(c);

        var info = new StackPanel();
        info.Children.Add(new TextBlock { Text = $"#{c.Number} · {c.Amount:N0} so'm", FontSize = 15, FontWeight = FontWeights.Bold });
        info.Children.Add(new TextBlock
        {
            Text = $"Kuryer: {(string.IsNullOrWhiteSpace(c.CourierName) ? "—" : c.CourierName)}" +
                   (c.DeliveredAt is { } d ? $" · yetkazildi {d.ToLocalTime():dd.MM HH:mm}" : ""),
            FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x5B, 0x66, 0x60))
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

    private async Task AcceptAsync(CourierCashDto c)
    {
        if (_busy) return;
        if (MessageBox.Show($"#{c.Number} — kuryerdan {c.Amount:N0} so'm naqd qabul qilindimi?\nKassaga kirim yoziladi.",
                "Kuryer pullari", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _busy = true;
        try
        {
            await AppState.Backend.CourierCashHandoverAsync(c.OrderId);
            Changed = true;
            ShowStatus($"✓ #{c.Number}: {c.Amount:N0} so'm kassaga qabul qilindi.", error: false);
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
