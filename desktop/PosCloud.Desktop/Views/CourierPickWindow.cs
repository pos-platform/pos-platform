using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Views;

/// <summary>
/// "Kuryerga berildi" dialogi: filialning faol kuryerlari ro'yxati (ism, telefon, faol buyurtmalar)
/// yoki "Boshqa (qo'lda)" — ism/telefonni qo'lda kiritish.
/// </summary>
public class CourierPickWindow : Window
{
    private readonly ListBox _list;

    /// <summary>Tanlangan kuryer (null va <see cref="Manual"/> = true — qo'lda kiritish tanlandi).</summary>
    public OnlineCourierDto? Selected { get; private set; }
    public bool Manual { get; private set; }

    private CourierPickWindow(int orderNumber, IReadOnlyList<OnlineCourierDto> couriers, string? loadError)
    {
        Title = "Kuryerga berildi";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var label = loadError is not null
            ? $"#{orderNumber} — kuryerlar ro'yxati olinmadi: {loadError}"
            : couriers.Count == 0
                ? $"#{orderNumber} — faol kuryer akkauntlari yo'q. \"Boshqa (qo'lda)\" orqali kiriting."
                : $"#{orderNumber} — kuryerni tanlang:";

        _list = new ListBox
        {
            ItemsSource = couriers,
            DisplayMemberPath = nameof(OnlineCourierDto.DisplayText),
            Height = 220,
            FontSize = 14,
            Visibility = couriers.Count == 0 ? Visibility.Collapsed : Visibility.Visible
        };
        _list.MouseDoubleClick += (_, _) => Pick();
        _list.KeyDown += (_, e) => { if (e.Key == Key.Enter) Pick(); };
        if (couriers.Count > 0) _list.SelectedIndex = 0;

        var ok = new Button { Content = "Tanlash", Width = 100, Height = 32, IsDefault = true, Margin = new Thickness(0, 0, 8, 0),
                              IsEnabled = couriers.Count > 0 };
        ok.Click += (_, _) => Pick();
        var manual = new Button { Content = "Boshqa (qo'lda)", Padding = new Thickness(10, 0, 10, 0), Height = 32, Margin = new Thickness(0, 0, 8, 0) };
        manual.Click += (_, _) => { Manual = true; DialogResult = true; };
        var cancel = new Button { Content = "Bekor", Width = 90, Height = 32, IsCancel = true };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(ok);
        buttons.Children.Add(manual);
        buttons.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8), FontSize = 13 });
        root.Children.Add(_list);
        root.Children.Add(buttons);
        Content = root;

        Loaded += (_, _) => { if (couriers.Count > 0) _list.Focus(); else manual.Focus(); };
    }

    private void Pick()
    {
        if (_list.SelectedItem is not OnlineCourierDto c) return;
        Selected = c;
        DialogResult = true;
    }

    /// <summary>Null — bekor qilindi.</summary>
    public static CourierPickWindow? Ask(Window owner, int orderNumber, IReadOnlyList<OnlineCourierDto> couriers, string? loadError)
    {
        var w = new CourierPickWindow(orderNumber, couriers, loadError) { Owner = owner };
        return w.ShowDialog() == true ? w : null;
    }
}
