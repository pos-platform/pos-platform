using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace PosPlatform.Api.Controllers;

/// <summary>
/// Desktop POS ilovasi uchun avtomatik yangilanish endpointlari.
/// Serverda ContentRoot/updates/desktop papkasiga "PosPlatform.Desktop-1.2.3.zip"
/// ko'rinishida zip qo'yiladi — desktop eng yuqori versiyani tekshirib, o'zi yuklab oladi.
/// Autentifikatsiya talab qilinmaydi (login oynasidan oldin ham tekshiriladi).
/// </summary>
[ApiController]
[Route("api/desktop-update")]
public class DesktopUpdateController : ControllerBase
{
    private static readonly Regex ZipName =
        new(@"^PosPlatform\.Desktop-(\d+(?:\.\d+){1,3})\.zip$", RegexOptions.IgnoreCase);

    private readonly IWebHostEnvironment _env;

    public DesktopUpdateController(IWebHostEnvironment env) => _env = env;

    private (Version Version, FileInfo File)? FindLatest()
    {
        var dir = Path.Combine(_env.ContentRootPath, "updates", "desktop");
        if (!Directory.Exists(dir)) return null;

        (Version Version, FileInfo File)? best = null;
        foreach (var path in Directory.EnumerateFiles(dir, "*.zip"))
        {
            var match = ZipName.Match(Path.GetFileName(path));
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
        return PhysicalFile(latest.Value.File.FullName, "application/zip", latest.Value.File.Name);
    }
}
