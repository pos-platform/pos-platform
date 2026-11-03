using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace PosCloud.Api.Controllers;

/// <summary>
/// Desktop POS ilovasi uchun avtomatik yangilanish endpointlari.
/// Serverda ContentRoot/updates/desktop papkasida DOIM bitta zip turadi:
///   PosPlatform.Desktop.zip  +  version.json  ({"version":"1.2.3"})
/// CI/CD yoki publish-desktop-update.ps1 shu ikki faylni ustidan yozadi — versiya
/// fayl nomida emas, version.json ichida. Eski (versiya nomli) ziplar ham
/// o'qiladi — o'tish davri uchun zaxira yo'l.
/// Autentifikatsiya talab qilinmaydi (login oynasidan oldin ham tekshiriladi).
/// </summary>
[ApiController]
[Route("api/desktop-update")]
public class DesktopUpdateController : ControllerBase
{
    /// <summary>Doimiy zip nomi — har chiqarishda shu fayl almashtiriladi.
    /// PosCloud ga qayta nomlanganda ataylab o'zgartirilmadi (exe nomi bilan bir xil, eski kassalar mosligi).</summary>
    private const string ZipFileName = "PosPlatform.Desktop.zip";

    /// <summary>Zip yonidagi versiya fayli.</summary>
    private const string MetaFileName = "version.json";

    private static readonly Regex LegacyZipName =
        new(@"^PosPlatform\.Desktop-(\d+(?:\.\d+){1,3})\.zip$", RegexOptions.IgnoreCase);

    private sealed record UpdateMeta(string? Version);

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DesktopUpdateController> _logger;

    public DesktopUpdateController(IWebHostEnvironment env, ILogger<DesktopUpdateController> logger)
    {
        _env = env;
        _logger = logger;
    }

    private (Version Version, FileInfo File)? FindLatest()
    {
        var dir = Path.Combine(_env.ContentRootPath, "updates", "desktop");
        if (!Directory.Exists(dir)) return null;

        // 1) Asosiy yo'l: doimiy nomli zip + version.json
        var zip = new FileInfo(Path.Combine(dir, ZipFileName));
        var metaPath = Path.Combine(dir, MetaFileName);
        if (zip.Exists && System.IO.File.Exists(metaPath))
        {
            try
            {
                var meta = JsonSerializer.Deserialize<UpdateMeta>(
                    System.IO.File.ReadAllText(metaPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (meta?.Version is not null && Version.TryParse(meta.Version, out var v))
                    return (v, zip);

                _logger.LogWarning("updates/desktop/{Meta} ichida to'g'ri versiya yo'q", MetaFileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "updates/desktop/{Meta} o'qilmadi", MetaFileName);
            }
        }

        // 2) Zaxira: eski "PosPlatform.Desktop-1.2.3.zip" fayllari
        (Version Version, FileInfo File)? best = null;
        foreach (var path in Directory.EnumerateFiles(dir, "*.zip"))
        {
            var match = LegacyZipName.Match(Path.GetFileName(path));
            if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version)) continue;
            if (best is null || version > best.Value.Version) best = (version, new FileInfo(path));
        }
        return best;
    }

    /// <summary>Serverdagi eng so'nggi desktop versiyasi haqida ma'lumot (yo'q bo'lsa 204).</summary>
    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetVersion()
    {
        var latest = FindLatest();
        if (latest is null) return NoContent();
        return Ok(new
        {
            version = latest.Value.Version.ToString(),
            fileName = latest.Value.File.Name,
            sizeBytes = latest.Value.File.Length
        });
    }

    /// <summary>Eng so'nggi versiya zip faylini yuklab beradi.</summary>
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Download()
    {
        var latest = FindLatest();
        if (latest is null) return NotFound();
        // Fayl serverda doimiy nomda turadi, lekin klientga versiyali nom bilan beramiz
        // (proksi/brauzer keshi eski nusxani ushlab qolmasin).
        return PhysicalFile(
            latest.Value.File.FullName,
            "application/zip",
            $"PosPlatform.Desktop-{latest.Value.Version}.zip");
    }
}
