using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PosCloud.Desktop.Views;

/// <summary>Bitta qiymat so'raydigan oddiy dialog (sabab, miqdor, shtrix-kod).</summary>
public class PromptWindow : Window
{
    private readonly TextBox _input;

    public string Value => _input.Text.Trim();

    private PromptWindow(string title, string label, string initial)
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _input = new TextBox { Text = initial, Height = 34, FontSize = 14, VerticalContentAlignment = VerticalAlignment.Center };
        _input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { DialogResult = true; }
        };

        var ok = new Button { Content = "OK", Width = 90, Height = 32, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => DialogResult = true;
        var cancel = new Button { Content = "Bekor", Width = 90, Height = 32, IsCancel = true };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8), FontSize = 13 });
        root.Children.Add(_input);
        root.Children.Add(buttons);
        Content = root;

        Loaded += (_, _) => { _input.Focus(); _input.SelectAll(); };
    }

    /// <summary>Null — bekor qilindi.</summary>
    public static string? Ask(Window owner, string title, string label, string initial = "")
    {
        var w = new PromptWindow(title, label, initial) { Owner = owner };
        return w.ShowDialog() == true ? w.Value : null;
    }
}
