using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Windows;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Dastur yangilanishlari: serverdan versiyani tekshiradi, zipni yuklab olib,
/// kichik bat-skript orqali fayllarni almashtirib ilovani qayta ishga tushiradi.
/// Server tomonida DesktopUpdateController (api/desktop-update) ishlaydi.
/// </summary>
public static class UpdateService
{
    public sealed record UpdateInfo(Version Version, string FileName, long SizeBytes);

    private sealed record VersionResponse(string? Version, string? FileName, long SizeBytes);

    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    /// <summary>Oxirgi tekshiruvda topilgan, joriy versiyadan yangi bo'lgan versiya (null = yangilanish yo'q).</summary>
    public static UpdateInfo? Available { get; private set; }

    private static HttpClient NewClient(TimeSpan timeout) => new()
    {
        BaseAddress = new Uri(AppState.BaseUrl.TrimEnd('/') + "/"),
        Timeout = timeout
    };

    /// <summary>Serverda yangi versiya bor-yo'qligini tekshiradi. Xatoda jim null qaytaradi (oflayn rejim).</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var http = NewClient(TimeSpan.FromSeconds(15));
            var resp = await http.GetAsync("api/desktop-update/version");
            if (resp.StatusCode != System.Net.HttpStatusCode.OK) return Available = null;

            var data = await resp.Content.ReadFromJsonAsync<VersionResponse>();
            if (data?.Version is null || !Version.TryParse(data.Version, out var version))
                return Available = null;

            return Available = version > CurrentVersion
                ? new UpdateInfo(version, data.FileName ?? "", data.SizeBytes)
                : null;
        }
        catch
        {
            return Available = null;
        }
    }

    /// <summary>
    /// Zipni yuklab oladi, updater skript yozadi va ilovani yopadi — skript fayllarni
    /// almashtirib ilovani qayta ochadi. appsettings.json ustidan yozilmaydi.
    /// </summary>
    public static async Task DownloadAndInstallAsync(IProgress<double>? progress = null)
    {
        var info = Available ?? throw new InvalidOperationException("Yangilanish topilmagan — avval CheckAsync chaqiring");

        var workDir = Path.Combine(Path.GetTempPath(), "PosCloudUpdate");
        if (Directory.Exists(workDir)) Directory.Delete(workDir, recursive: true);
        Directory.CreateDirectory(workDir);

        // Zipni bosqichma-bosqich yuklab olamiz (progress uchun stream'dan o'qiymiz).
        var zipPath = Path.Combine(workDir, "update.zip");
        using (var http = NewClient(TimeSpan.FromMinutes(15)))
        using (var resp = await http.GetAsync("api/desktop-update/download", HttpCompletionOption.ResponseHeadersRead))
        {
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? info.SizeBytes;
            await using var src = await resp.Content.ReadAsStreamAsync();
            await using var dst = File.Create(zipPath);
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

        var extractDir = Path.Combine(workDir, "files");
        ZipFile.ExtractToDirectory(zipPath, extractDir);
        File.Delete(zipPath);

        // Zip ichida bitta ildiz papka bo'lsa (masalan "publish"), uning ichini olamiz.
        var sourceDir = extractDir;
        var entries = Directory.GetFileSystemEntries(extractDir);
        if (entries.Length == 1 && Directory.Exists(entries[0])) sourceDir = entries[0];

        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
        var appDir = Path.GetDirectoryName(exePath)!;
        var exeName = Path.GetFileName(exePath);

        if (!File.Exists(Path.Combine(sourceDir, exeName)))
            throw new InvalidOperationException(
                $"Yangilanish faylida {exeName} topilmadi — serverdagi zip noto'g'ri tuzilgan.");

        // Updater: ilova yopilishini kutadi, fayllarni ko'chiradi (appsettings.json'dan tashqari)
        // va ilovani qayta ishga tushiradi. chcp 65001 — yo'llardagi milliy harflar uchun.
        var batPath = Path.Combine(workDir, "apply-update.bat");
        var bat = $"""
            @echo off
            chcp 65001 >nul
            :wait
            tasklist /FI "PID eq {Environment.ProcessId}" 2>nul | find "{Environment.ProcessId}" >nul
            if not errorlevel 1 (
                timeout /t 1 /nobreak >nul
                goto wait
            )
            robocopy "{sourceDir}" "{appDir}" /E /XF appsettings.json >nul
            start "" "{Path.Combine(appDir, exeName)}"
            """;
        File.WriteAllText(batPath, bat, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        progress?.Report(100);
        Process.Start(new ProcessStartInfo
        {
            FileName = batPath,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = true
        });
        Application.Current.Shutdown();
    }
}
