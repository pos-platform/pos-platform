using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using PosPlatform.Desktop.Services;
using PosPlatform.Desktop.Views;

namespace PosPlatform.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // CP866 (termal printer ESC/POS) kodlashini yoqamiz — .NET Core'da standart mavjud emas.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

        AppState.BaseUrl = config["Backend:BaseUrl"] ?? "https://poscloud.uz";
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

    public static BitmapSource CreateLogoIcon(int size = 64)
    {
        // Qidirish joylari: 1) embedded resource  2) output/Resources  3) source/Resources (dev mode)
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png"),
            Path.Combine(AppContext.BaseDirectory, "Resources", "logo.jpg"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "logo.png"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "logo.jpg"),
        };
        foreach (var path in candidates)
        {
            try
            {
                var full = Path.GetFullPath(path);
                if (!File.Exists(full)) continue;
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource    = new Uri(full);
                img.CacheOption  = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch { }
        }
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/logo.png");
            return new BitmapImage(uri);
        }
        catch { }

        // Oxirgi chora: DrawingImage dan render
        var drawing = (DrawingImage)Current.Resources["AppLogo"];
        var visual  = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
            ctx.DrawImage(drawing, new Rect(0, 0, size, size));
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        return bmp;
    }
}
