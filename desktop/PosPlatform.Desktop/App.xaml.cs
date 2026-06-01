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

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var baseUrl = config["Backend:BaseUrl"] ?? "http://localhost:5000";
        AppState.BaseUrl = baseUrl;
        AppState.Backend = new BackendClient(baseUrl);
        AppState.PrinterEnabled = bool.TryParse(config["Printer:Enabled"], out var pe) && pe;
        AppState.PrinterName = string.IsNullOrWhiteSpace(config["Printer:Name"]) ? null : config["Printer:Name"];

        new LoginWindow().Show();
    }
}
