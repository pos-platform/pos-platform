using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Velopack;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Dastur yangilanishlari (Velopack). Serverda {BaseUrl}/updates/velopack/ papkasida
/// releases.win.json + *.nupkg + PosCloud-win-Setup.exe turadi (publish-desktop-update.ps1 yuklaydi).
///
/// Yangilanish majburiy emas: CheckAsync faqat ma'lumot beradi (⚙ belgisi, Sozlamalar oynasi),
/// o'rnatish kassir "Yangilash" ni bosgandagina boshlanadi.
///
/// Ikki holat:
///  • Velopack o'rnatgan (%LocalAppData%\PosCloud) — delta yuklab, Update.exe orqali almashtiradi.
///  • Eski usulda zipdan ochilgan papka (1.0.8 va undan oldingi o'rnatishlar) — bir martalik
///    o'tish: Setup.exe ni yuklab ishga tushiradi, u dasturni Velopack bilan qayta o'rnatadi.
/// </summary>
public static class UpdateService
{
    /// <summary>vpk pack --packId bilan bir xil bo'lishi shart (o'rnatish papkasi nomi ham shu).</summary>
    private const string PackId = "PosCloud";

    private const string SetupFileName = PackId + "-win-Setup.exe";

    /// <param name="IsMigration">true — eski papkadan Velopack o'rnatishiga bir martalik o'tish.</param>
    public sealed record UpdateInfo(Version Version, long SizeBytes, bool IsMigration);

    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    /// <summary>Oxirgi tekshiruvda topilgan, joriy versiyadan yangi bo'lgan versiya (null = yangilanish yo'q).</summary>
    public static UpdateInfo? Available { get; private set; }

    // Velopack o'rnatgan holatda: tekshiruv natijasi yuklab olish uchun kerak bo'ladi.
    private static UpdateManager? _manager;
    private static Velopack.UpdateInfo? _pending;

    private static string FeedUrl => AppState.BaseUrl.TrimEnd('/') + "/updates/velopack";

    /// <summary>Velopack o'rnatgan nusxaning exe yo'li (o'rnatilmagan bo'lsa fayl yo'q).</summary>
    private static string InstalledExePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        PackId, "current", Path.GetFileName(Environment.ProcessPath ?? "PosPlatform.Desktop.exe"));

    /// <summary>
    /// Eski papkadan ishga tushirilgan, lekin kompyuterda Velopack nusxasi allaqachon bor bo'lsa —
    /// o'shani ochadi va true qaytaradi (chaqiruvchi darhol chiqishi kerak). Eski ish stoli
    /// yorliqlari shu tufayli o'z-o'zidan yangi o'rnatishga yo'naladi.
    /// </summary>
    public static bool TryLaunchInstalledCopy()
    {
#if DEBUG
        return false;
#else
        try
        {
            if (new UpdateManager(FeedUrl).IsInstalled || !File.Exists(InstalledExePath)) return false;
            Process.Start(new ProcessStartInfo(InstalledExePath) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
#endif
    }

    /// <summary>Serverda yangi versiya bor-yo'qligini tekshiradi. Xatoda jim null qaytaradi (oflayn rejim).</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
#if DEBUG
        // Visual Studio'dan ishga tushirilganda o'rnatish/yangilash yo'q.
        await Task.CompletedTask;
        return Available = null;
#else
        try
        {
            var manager = new UpdateManager(FeedUrl);
            if (!manager.IsInstalled) return Available = await CheckMigrationAsync();

            var info = await manager.CheckForUpdatesAsync();
            _manager = manager;
            _pending = info;
            if (info is null) return Available = null;

            var target = info.TargetFullRelease;
            // Delta bo'lsa faqat o'zgarishlar yuklanadi — kassirga haqiqiy hajmni ko'rsatamiz.
            var size = info.DeltasToTarget.Length > 0 ? info.DeltasToTarget.Sum(d => d.Size) : target.Size;
            return Available = new UpdateInfo(
                new Version(target.Version.Major, target.Version.Minor, target.Version.Patch), size, IsMigration: false);
        }
        catch
        {
            return Available = null;
        }
#endif
    }

    /// <summary>Eski papkadagi nusxa uchun: releases.win.json dan eng so'nggi to'liq versiyani o'qiydi.</summary>
    private static async Task<UpdateInfo?> CheckMigrationAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        await using var stream = await http.GetStreamAsync(FeedUrl + "/releases.win.json");
        using var doc = await JsonDocument.ParseAsync(stream);

        UpdateInfo? latest = null;
        foreach (var asset in doc.RootElement.GetProperty("Assets").EnumerateArray())
        {
            if (asset.GetProperty("Type").GetString() != "Full") continue;
            if (!Version.TryParse(asset.GetProperty("Version").GetString(), out var version)) continue;
            if (latest is null || version > latest.Version)
                latest = new UpdateInfo(version, asset.GetProperty("Size").GetInt64(), IsMigration: true);
        }
        return latest is not null && latest.Version > CurrentVersion ? latest : null;
    }

    /// <summary>
    /// Yangilanishni yuklab o'rnatadi va ilovani qayta ochadi. Muvaffaqiyatli bo'lsa bu metoddan
    /// qaytilmaydi — jarayon yopiladi. Sozlamalar (%APPDATA%\PosPlatform) va oflayn baza
    /// (%LOCALAPPDATA%\PosPlatform) dastur papkasidan tashqarida — ularga tegilmaydi.
    /// </summary>
    public static async Task DownloadAndInstallAsync(IProgress<double>? progress = null)
    {
        var info = Available ?? throw new InvalidOperationException("Yangilanish topilmagan — avval CheckAsync chaqiring");

        if (info.IsMigration)
        {
            await RunSetupAsync(progress);
            return;
        }

        if (_manager is null || _pending is null)
            throw new InvalidOperationException("Yangilanish ma'lumoti eskirgan — oynani qayta oching");

        await _manager.DownloadUpdatesAsync(_pending, p => progress?.Report(p));
        _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
    }

    /// <summary>Bir martalik o'tish: Setup.exe ni yuklab ishga tushiradi va eski nusxani yopadi.</summary>
    private static async Task RunSetupAsync(IProgress<double>? progress)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "PosCloudUpdate");
        if (Directory.Exists(workDir)) Directory.Delete(workDir, recursive: true);
        Directory.CreateDirectory(workDir);
        var setupPath = Path.Combine(workDir, SetupFileName);

        using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) })
        using (var resp = await http.GetAsync(FeedUrl + "/" + SetupFileName, HttpCompletionOption.ResponseHeadersRead))
        {
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? 0;
            await using var src = await resp.Content.ReadAsStreamAsync();
            await using var dst = File.Create(setupPath);
            var buffer = new byte[81920];
            long done = 0;
            int n;
            while ((n = await src.ReadAsync(buffer)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, n));
                done += n;
                if (total > 0) progress?.Report(Math.Min(99, done * 100.0 / total));
            }
        }

        progress?.Report(100);
        // Setup dasturni %LocalAppData%\PosCloud ga o'rnatib, ish stoliga yorliq qo'yadi va o'zi ochadi.
        Process.Start(new ProcessStartInfo(setupPath) { UseShellExecute = true });
        Application.Current.Shutdown();
    }
}
