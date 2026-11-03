using PosCloud.Desktop.Services;
using Velopack;

namespace PosCloud.Desktop;

/// <summary>
/// Kirish nuqtasi (App.xaml avtomatik yaratadigan Main o'rniga — csproj'dagi StartupObject).
/// Velopack hamma narsadan oldin ishlashi shart: o'rnatish/yangilash/o'chirish paytida
/// Update.exe ilovani maxsus argumentlar bilan chaqiradi va Run() ularga ishlov berib chiqib ketadi.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        // Eski (zipdan ochilgan) papkadagi yorliq bosilgan bo'lsa — Velopack o'rnatgan nusxani ochamiz.
        if (UpdateService.TryLaunchInstalledCopy()) return;

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
