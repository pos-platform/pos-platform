using System.IO;
using System.Windows;
using System.Windows.Input;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

public partial class LoginWindow : Window
{
    private bool _showPassword;

    public LoginWindow()
    {
        InitializeComponent();
        var logo = App.CreateLogoIcon();
        Icon = logo;
        LogoImage.Source = logo;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private string CurrentPassword => _showPassword ? PasswordVisible.Text : PasswordBox.Password;

    private void ToggleShowPassword(object sender, RoutedEventArgs e)
    {
        _showPassword = !_showPassword;
        if (_showPassword)
        {
            PasswordVisible.Text = PasswordBox.Password;
            PasswordVisible.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            EyeButton.Content = "🙈";
        }
        else
        {
            PasswordBox.Password = PasswordVisible.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordVisible.Visibility = Visibility.Collapsed;
            EyeButton.Content = "👁";
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await LoginAsync();

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await LoginAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
        => new SettingsWindow { Owner = this }.ShowDialog();

    private async Task LoginAsync()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;
        LoginButton.Content = "Kirilmoqda...";
        try
        {
            var resp = await AppState.Backend.LoginAsync(UsernameBox.Text.Trim(), CurrentPassword);
            if (resp.Role == "Admin")
                throw new Exception("Admin foydalanuvchilar desktop appga kira olmaydi.");
            AppState.Backend.SetToken(resp.Token);
            AppState.Token = resp.Token;
            AppState.Username = resp.Username;
            AppState.CashierName = resp.FullName ?? "";
            // Chek sarlavhasida tashkilot nomi chiqadi (filial nomi emas). Tashkilot nomi
            // bo'lmasa filial nomiga qaytamiz.
            AppState.StoreName = !string.IsNullOrWhiteSpace(resp.OrganizationName) ? resp.OrganizationName!
                : (resp.BranchName ?? "");
            AppState.OrgStir = resp.OrganizationStir;
            AppState.BranchAddress = resp.BranchAddress;
            AppState.Role = resp.Role;
            AppState.BusinessType = string.IsNullOrWhiteSpace(resp.BusinessType) ? "Store" : resp.BusinessType;
            AppState.LicensedUntil = resp.LicensedUntil;
            AppState.IsLifetime = resp.IsLifetime;

            if (AppState.IsRestaurant)
            {
                new TablesWindow().Show();
            }
            else
            {
                new PosWindow().Show();
            }
            Close();
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "login-error.log"), ex.ToString());
            }
            catch { /* ignore */ }
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
