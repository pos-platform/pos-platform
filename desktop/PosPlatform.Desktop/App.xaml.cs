using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using PosPlatform.Desktop.Services;
using PosPlatform.Desktop.Views;

namespace PosPlatform.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // UI oqimidagi ushlanmagan xatoliklar ilovani yiqitmasligi uchun.
        DispatcherUnhandledException += (_, args) =>
        {
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), args.Exception.ToString()); }
            catch { /* ignore */ }
            MessageBox.Show(args.Exception.Message, "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // appsettings.json — standart qiymatlar.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        AppState.BaseUrl = config["Backend:BaseUrl"] ?? "http://localhost:5000";
        AppState.PrinterEnabled = bool.TryParse(config["Printer:Enabled"], out var pe) && pe;
        AppState.PrinterName = string.IsNullOrWhiteSpace(config["Printer:Name"]) ? null : config["Printer:Name"];

        // Foydalanuvchi sozlamalari (%APPDATA%) — agar bor bo'lsa, ustun.
        var saved = SettingsService.Load();
        if (saved is not null)
        {
            if (!string.IsNullOrWhiteSpace(saved.BaseUrl)) AppState.BaseUrl = saved.BaseUrl;
            AppState.PrinterEnabled = saved.PrinterEnabled;
            AppState.PrinterName = string.IsNullOrWhiteSpace(saved.PrinterName) ? null : saved.PrinterName;
            AppState.ScannerComPort = string.IsNullOrWhiteSpace(saved.ScannerComPort) ? null : saved.ScannerComPort;
            AppState.ScannerBaudRate = saved.ScannerBaudRate > 0 ? saved.ScannerBaudRate : 9600;
        }

        AppState.Backend = new BackendClient(AppState.BaseUrl);

        new LoginWindow().Show();
    }
}
