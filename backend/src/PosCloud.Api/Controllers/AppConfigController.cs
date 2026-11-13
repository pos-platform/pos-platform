using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.ShopCatalog;

namespace PosCloud.Api.Controllers;

/// <summary>
/// Mobil ilova ishga tushganda so'raydigan sozlama: majburiy yangilash, texnik ishlar rejimi,
/// huquqiy sahifalar havolalari. Tokensiz ochiq (login'dan oldin ham chaqiriladi).
/// Qiymatlar appsettings "MobileApp" bo'limidan olinadi — server qayta ishga tushmasdan ham
/// (reloadOnChange) yangilanadi.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/app/config")]
public class AppConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IShopCatalogService _catalog;

    public AppConfigController(IConfiguration configuration, IShopCatalogService catalog)
    {
        _configuration = configuration;
        _catalog = catalog;
    }

    /// <summary>
    /// GET /api/app/config?platform=android|ios&amp;version=1.0.0&amp;build=1&amp;store=slug
    /// forceUpdate = build &lt; minSupportedBuild. store berilsa — do'kon ilovasining qiymatlari
    /// (to'ldirilganlari) global qiymatlar o'rniga ishlatiladi.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AppConfigResponse>> Get(
        [FromQuery] string? platform,
        [FromQuery] string? version,
        [FromQuery] int? build,
        [FromQuery] string? store,
        CancellationToken ct)
    {
        var options = _configuration.GetSection(MobileAppOptions.SectionName).Get<MobileAppOptions>()
            ?? new MobileAppOptions();

        // Noma'lum platforma — Android qiymatlari (asosiy platforma).
        var isIos = string.Equals(platform, "ios", StringComparison.OrdinalIgnoreCase);
        var global = (isIos ? options.Ios : options.Android) ?? new MobilePlatformOptions();
        var p = new MobilePlatformOptions
        {
            MinSupportedBuild = global.MinSupportedBuild,
            LatestBuild = global.LatestBuild,
            LatestVersion = global.LatestVersion,
            StoreUrl = global.StoreUrl
        };

        if (!string.IsNullOrWhiteSpace(store)
            && await _catalog.GetStoreAppPlatformAsync(store, isIos, ct) is { } s)
        {
            if (s.MinBuild is { } min) p.MinSupportedBuild = min;
            if (s.LatestBuild is { } latest) p.LatestBuild = latest;
            if (!string.IsNullOrWhiteSpace(s.LatestVersion)) p.LatestVersion = s.LatestVersion;
            if (!string.IsNullOrWhiteSpace(s.StoreUrl)) p.StoreUrl = s.StoreUrl;
        }

        // Havolalar sozlanmagan bo'lsa — shu serverdagi statik sahifalar (wwwroot).
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var privacyUrl = string.IsNullOrWhiteSpace(options.PrivacyUrl) ? $"{baseUrl}/privacy.html" : options.PrivacyUrl;
        var termsUrl = string.IsNullOrWhiteSpace(options.TermsUrl) ? $"{baseUrl}/terms.html" : options.TermsUrl;

        return Ok(new AppConfigResponse(
            MinSupportedBuild: p.MinSupportedBuild,
            LatestBuild: p.LatestBuild,
            LatestVersion: p.LatestVersion ?? "",
            StoreUrl: p.StoreUrl ?? "",
            ForceUpdate: build is int b && b < p.MinSupportedBuild,
            Maintenance: options.Maintenance,
            MaintenanceMessage: options.MaintenanceMessage ?? "",
            PrivacyUrl: privacyUrl,
            TermsUrl: termsUrl));
    }
}

/// <summary>/api/app/config javobi (camelCase JSON).</summary>
public sealed record AppConfigResponse(
    int MinSupportedBuild,
    int LatestBuild,
    string LatestVersion,
    string StoreUrl,
    bool ForceUpdate,
    bool Maintenance,
    string MaintenanceMessage,
    string PrivacyUrl,
    string TermsUrl);

/// <summary>appsettings "MobileApp" bo'limi.</summary>
public sealed class MobileAppOptions
{
    public const string SectionName = "MobileApp";

    public MobilePlatformOptions? Android { get; set; }
    public MobilePlatformOptions? Ios { get; set; }

    /// <summary>true — ilova texnik ishlar ekranini ko'rsatadi.</summary>
    public bool Maintenance { get; set; }
    public string? MaintenanceMessage { get; set; }

    /// <summary>Bo'sh bo'lsa — {server}/privacy.html.</summary>
    public string? PrivacyUrl { get; set; }

    /// <summary>Bo'sh bo'lsa — {server}/terms.html.</summary>
    public string? TermsUrl { get; set; }
}

/// <summary>Bitta platforma (Android/iOS) versiya sozlamalari.</summary>
public sealed class MobilePlatformOptions
{
    /// <summary>Bundan kichik build majburiy yangilanadi.</summary>
    public int MinSupportedBuild { get; set; } = 1;
    public int LatestBuild { get; set; } = 1;
    public string? LatestVersion { get; set; }
    public string? StoreUrl { get; set; }
}
