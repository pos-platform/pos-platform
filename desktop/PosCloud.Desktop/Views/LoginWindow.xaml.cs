using System.IO;
using System.Windows;
using System.Windows.Input;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

public partial class LoginWindow : Window
{
    private bool _showPassword;

    /// <summary>Parol saqlangan joydan to'ldirilgan — login xato bo'lsa eskirgan deb tozalaymiz.</summary>
    private bool _prefilledFromStore;

    public LoginWindow()
    {
        InitializeComponent();
        var logo = App.CreateLogoIcon();
        Icon = logo;
        LogoImage.Source = logo;

        var saved = CredentialStore.TryLoad();
        if (saved is not null)
        {
            UsernameBox.Text = saved.Value.Username;
            PasswordBox.Password = saved.Value.Password;
            RememberBox.IsChecked = true;
            _prefilledFromStore = true;
        }

        // Maydonlar to'ldirilgan bo'lsa kassir faqat Enter/Kirish bosadi.
        Loaded += (_, _) => { if (_prefilledFromStore) LoginButton.Focus(); else UsernameBox.Focus(); };
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
            var username = UsernameBox.Text.Trim();
            // Kassa ID ni ham yuboramiz — loginga kassa biriktirilgan bo'lsa server
            // faqat o'sha kassadan kirishga ruxsat beradi (bitta login = bitta kassa).
            var resp = await AppState.Backend.LoginAsync(username, CurrentPassword, AppState.TerminalId);
            if (resp.Role == "Admin")
                throw new Exception("Admin foydalanuvchilar desktop appga kira olmaydi.");
            // Kuryer akkaunti faqat mobil (kuryer) ilovada ishlaydi — kassaga kiritilmaydi.
            if (resp.Role == "Courier")
                throw new Exception("Kuryer akkaunti kassaga kira olmaydi — mobil ilovadan foydalaning.");

            // "Eslab qolish": muvaffaqiyatli kirishda saqlaymiz, belgi olib tashlangan bo'lsa tozalaymiz.
            if (RememberBox.IsChecked == true) CredentialStore.Save(username, CurrentPassword);
            else CredentialStore.Clear();
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
            AppState.LicensedUntil = resp.LicensedUntil;
            AppState.IsLifetime = resp.IsLifetime;
            // Server tasdiqlagan kassa ID — yozilishi bir xil bo'lishi uchun (KASSA-1 / kassa-1)
            // sozlamadagi qiymatni serverdagisi bilan almashtiramiz.
            if (!string.IsNullOrWhiteSpace(resp.TerminalId)) AppState.TerminalId = resp.TerminalId;

            new PosWindow().Show();
            Close();
        }
        catch (Exception ex)
        {
            // Server "parol noto'g'ri" dedi va parol saqlangan joydan kelgan edi —
            // demak parol o'zgargan, eskirganini tozalaymiz (tarmoq xatosida tegmaymiz).
            var networkIssue = ex is System.Net.Http.HttpRequestException or TaskCanceledException;
            if (_prefilledFromStore && !networkIssue)
            {
                CredentialStore.Clear();
                _prefilledFromStore = false;
            }

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
