using System.Windows;
using System.Windows.Input;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await LoginAsync();

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await LoginAsync();
    }

    private async Task LoginAsync()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;
        LoginButton.Content = "Kirilmoqda...";
        try
        {
            var resp = await AppState.Backend.LoginAsync(UsernameBox.Text.Trim(), PasswordBox.Password);
            AppState.Backend.SetToken(resp.Token);
            AppState.Username = resp.Username;
            AppState.Role = resp.Role;

            var pos = new PosWindow();
            pos.Show();
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "Kirish";
        }
    }
}
