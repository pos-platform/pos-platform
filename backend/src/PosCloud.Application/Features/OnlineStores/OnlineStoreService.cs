using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineStores.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineStores;

public interface IOnlineStoreService
{
    // SuperAdmin
    Task<IReadOnlyList<OnlineStoreListItemDto>> GetAllAsync(string? filter, string? search, CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> UpdateAdminAsync(Guid organizationId, UpdateOnlineStoreAdminRequest request, CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> UpdateAppAsync(Guid organizationId, UpdateStoreAppRequest request, CancellationToken ct = default);

    // Do'kon admini / kassa (joriy tashkilot)
    Task<OnlineStoreSettingsDto> GetMineAsync(CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> UpdateMineAsync(UpdateOnlineStoreSettingsRequest request, CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> RequestListingAsync(CancellationToken ct = default);
    Task<OnlineStoreSettingsDto> SetAcceptingOrdersAsync(bool isAccepting, CancellationToken ct = default);
}

/// <summary>
/// Online do'kon sozlamalari. Yuqori qism (yoqish, bloklash, to'lov usullari, tavsiya, tarif) — SuperAdmin;
/// pastki qism (ish vaqti, yetkazish, karta) — do'kon admini. Har tashkilotga bitta yozuv, kerak bo'lganda yaratiladi.
/// </summary>
public class OnlineStoreService : IOnlineStoreService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public OnlineStoreService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OnlineStoreListItemDto>> GetAllAsync(string? filter, string? search, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var rows = await _db.Organizations.AsNoTracking()
            .Select(o => new
            {
                Org = o,
                Settings = _db.OnlineStoreSettings.IgnoreQueryFilters().FirstOrDefault(s => s.OrganizationId == o.Id),
                StoreBranches = _db.Branches.IgnoreQueryFilters()
                    .Count(b => b.OrganizationId == o.Id && b.IsActive && b.BusinessType == BusinessType.Store),
                Orders = _db.OnlineOrders.IgnoreQueryFilters().Count(x => x.OrganizationId == o.Id && x.CreatedAt >= since)
            })
            .ToListAsync(ct);

        var list = rows.Select(r => new OnlineStoreListItemDto
        {
            OrganizationId = r.Org.Id,
            OrganizationName = r.Org.Name,
            OrganizationActive = r.Org.IsActive,
            StoreBranchCount = r.StoreBranches,
            HasSettings = r.Settings is not null,
            DisplayName = r.Settings?.DisplayName,
            IsRequested = r.Settings?.IsRequested ?? false,
            IsEnabled = r.Settings?.IsEnabled ?? false,
            IsBlocked = r.Settings?.IsBlocked ?? false,
            BlockReason = r.Settings?.BlockReason,
            IsFeatured = r.Settings?.IsFeatured ?? false,
            SortOrder = r.Settings?.SortOrder ?? 0,
            PaidUntil = r.Settings?.PaidUntil,
            IsAcceptingOrders = r.Settings?.IsAcceptingOrders ?? false,
            Slug = r.Settings?.Slug,
            AppName = r.Settings?.AppName,
            OrdersLast30Days = r.Orders
        });

        list = (filter ?? "all").ToLowerInvariant() switch
        {
            // So'rov yuborgan, hali yoqilmagan
            "requested" => list.Where(x => x.IsRequested && !x.IsEnabled),
            "enabled" => list.Where(x => x.IsEnabled && !x.IsBlocked),
            "blocked" => list.Where(x => x.IsBlocked),
            _ => list
        };

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            list = list.Where(x => x.OrganizationName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || (x.DisplayName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                   || (x.Slug?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

        return list
            .OrderByDescending(x => x.IsRequested && !x.IsEnabled)
            .ThenByDescending(x => x.IsEnabled)
            .ThenBy(x => x.OrganizationName)
            .ToList();
    }

    public async Task<OnlineStoreSettingsDto> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await RequireOrganization(organizationId, ct);
        var settings = await Find(organizationId, ct) ?? NewSettings(organizationId);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> UpdateAdminAsync(Guid organizationId, UpdateOnlineStoreAdminRequest r,
        CancellationToken ct = default)
    {
        var org = await RequireOrganization(organizationId, ct);
        var settings = await GetOrCreate(organizationId, ct);

        if (r.IsBlocked && string.IsNullOrWhiteSpace(r.BlockReason))
            throw new ValidationException("Bloklash sababini kiriting.");
        if (r.IsEnabled && !r.AllowCash && !r.AllowTransfer && !r.AllowCardOnDelivery && !r.AllowPayme && !r.AllowClick)
            throw new ValidationException("Kamida bitta to'lov usuli yoqilgan bo'lishi kerak.");

        settings.IsEnabled = r.IsEnabled;
        settings.IsBlocked = r.IsBlocked;
        settings.BlockReason = r.IsBlocked ? r.BlockReason!.Trim() : null;
        settings.IsFeatured = r.IsFeatured;
        settings.SortOrder = r.SortOrder;
        settings.PaidUntil = r.PaidUntil is { } p ? DateTime.SpecifyKind(p, DateTimeKind.Utc) : null;
        settings.AllowCash = r.AllowCash;
        settings.AllowTransfer = r.AllowTransfer;
        settings.AllowCardOnDelivery = r.AllowCardOnDelivery;
        settings.AllowPayme = r.AllowPayme;
        settings.AllowClick = r.AllowClick;
        if (r.IsEnabled) settings.IsRequested = false; // so'rov ko'rib chiqildi

        await _db.SaveChangesAsync(ct);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> UpdateAppAsync(Guid organizationId, UpdateStoreAppRequest r,
        CancellationToken ct = default)
    {
        var org = await RequireOrganization(organizationId, ct);

        var slug = NullIfEmpty(r.Slug)?.ToLowerInvariant();
        if (slug is not null && !SlugRegex.IsMatch(slug))
            throw new ValidationException("Slug 3–40 ta kichik lotin harfi, raqam yoki '-' dan iborat bo'lishi kerak.");

        var appName = NullIfEmpty(r.AppName);
        if (appName is { Length: > 30 })
            throw new ValidationException("Ilova nomi 30 belgidan oshmasligi kerak.");

        var packageId = NullIfEmpty(r.AppPackageId);
        if (packageId is not null && (packageId.Length > 150 || !PackageIdRegex.IsMatch(packageId)))
            throw new ValidationException("Paket ID noto'g'ri (masalan: uz.poscloud.mystore).");

        var color = NullIfEmpty(r.BrandColor)?.ToUpperInvariant();
        if (color is not null && !ColorRegex.IsMatch(color))
            throw new ValidationException("Brend rangi #RRGGBB ko'rinishida bo'lishi kerak.");

        ValidateBuilds(r.AndroidMinBuild, r.AndroidLatestBuild, "Android");
        ValidateBuilds(r.IosMinBuild, r.IosLatestBuild, "iOS");

        var androidVersion = NullIfEmpty(r.AndroidLatestVersion);
        var iosVersion = NullIfEmpty(r.IosLatestVersion);
        if (androidVersion is { Length: > 30 } || iosVersion is { Length: > 30 })
            throw new ValidationException("Versiya 30 belgidan oshmasligi kerak.");

        var androidUrl = NormalizeHttpsUrl(r.AndroidStoreUrl, "Android");
        var iosUrl = NormalizeHttpsUrl(r.IosStoreUrl, "iOS");

        // Unikallik: boshqa tashkilotda band bo'lmasligi kerak.
        if (slug is not null && await _db.OnlineStoreSettings.IgnoreQueryFilters()
                .AnyAsync(s => s.Slug == slug && s.OrganizationId != organizationId, ct))
            throw new ConflictException($"\"{slug}\" slug'i boshqa do'konda band.");
        if (packageId is not null && await _db.OnlineStoreSettings.IgnoreQueryFilters()
                .AnyAsync(s => s.AppPackageId == packageId && s.OrganizationId != organizationId, ct))
            throw new ConflictException($"\"{packageId}\" paket ID'si boshqa do'konda band.");

        var settings = await GetOrCreate(organizationId, ct);
        settings.Slug = slug;
        settings.AppName = appName;
        settings.AppPackageId = packageId;
        settings.BrandColor = color;
        settings.AndroidMinBuild = r.AndroidMinBuild;
        settings.AndroidLatestBuild = r.AndroidLatestBuild;
        settings.AndroidLatestVersion = androidVersion;
        settings.AndroidStoreUrl = androidUrl;
        settings.IosMinBuild = r.IosMinBuild;
        settings.IosLatestBuild = r.IosLatestBuild;
        settings.IosLatestVersion = iosVersion;
        settings.IosStoreUrl = iosUrl;

        await _db.SaveChangesAsync(ct);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> GetMineAsync(CancellationToken ct = default)
    {
        var orgId = CurrentOrganization();
        var org = await RequireOrganization(orgId, ct);
        var settings = await Find(orgId, ct) ?? NewSettings(orgId);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> UpdateMineAsync(UpdateOnlineStoreSettingsRequest r, CancellationToken ct = default)
    {
        var orgId = CurrentOrganization();
        var org = await RequireOrganization(orgId, ct);
        var settings = await GetOrCreate(orgId, ct);

        if (!r.DeliveryEnabled && !r.PickupEnabled)
            throw new ValidationException("Yetkazish yoki olib ketishdan kamida bittasi yoqilgan bo'lishi kerak.");
        if ((r.OpensAt is null) != (r.ClosesAt is null))
            throw new ValidationException("Ish vaqtining boshlanishi va tugashini birga kiriting.");
        if (r.FreeDeliveryFrom is { } free && free <= 0)
            throw new ValidationException("Bepul yetkazish summasi 0 dan katta bo'lishi kerak.");

        if ((r.DeliveryMinMinutes is null) != (r.DeliveryMaxMinutes is null))
            throw new ValidationException("Yetkazish vaqtining eng kam va eng ko'p qiymatini birga kiriting.");
        if (r.DeliveryMinMinutes is { } dMin && r.DeliveryMaxMinutes is { } dMax)
        {
            if (dMin < 5 || dMax > 1440)
                throw new ValidationException("Yetkazish vaqti 5 daqiqadan 1440 daqiqagacha bo'lishi kerak.");
            if (dMin > dMax)
                throw new ValidationException("Yetkazish vaqtining eng kam qiymati eng ko'pidan katta bo'lmasligi kerak.");
        }
        if (r.AutoCancelMinutes is < 0 or > 1440)
            throw new ValidationException("Avtomatik bekor qilish vaqti 0 dan 1440 daqiqagacha bo'lishi kerak.");

        if (r.DeliveryRadiusKm is { } radius && (radius <= 0 || radius > 500))
            throw new ValidationException("Yetkazish radiusi 0 dan 500 km gacha bo'lishi kerak.");
        if (r.BonusMaxPercent is < 0 or > 100)
            throw new ValidationException("Bonus foizi 0 dan 100 gacha bo'lishi kerak.");
        var mxik = NullIfEmpty(r.DeliveryServiceMxikCode);
        if (mxik is not null && !Mxik.IsValid(mxik))
            throw new ValidationException($"Yetkazish xizmati MXIK kodi {Mxik.Length} xonali bo'lishi kerak.");

        var telegram = NullIfEmpty(r.SupportTelegram)?.TrimStart('@');
        if (telegram is not null && !TelegramRegex.IsMatch(telegram))
            throw new ValidationException("Telegram username 5–32 ta lotin harfi, raqam yoki '_' bo'lishi kerak.");
        if (r.ReferrerBonus < 0 || r.RefereeBonus < 0)
            throw new ValidationException("Taklif bonuslari manfiy bo'lmasligi kerak.");
        if (r.ReferralEnabled && r.ReferrerBonus <= 0 && r.RefereeBonus <= 0)
            throw new ValidationException("Do'stni taklif qilish uchun kamida bitta bonusni kiriting.");

        var card = NormalizeCard(r.TransferCardNumber);
        if (card is not null && (card.Length < 16 || card.Length > 20))
            throw new ValidationException("Karta raqami 16 xonali bo'lishi kerak.");

        settings.IsAcceptingOrders = r.IsAcceptingOrders;
        settings.DisplayName = NullIfEmpty(r.DisplayName);
        settings.Description = NullIfEmpty(r.Description);
        settings.LogoUrl = NullIfEmpty(r.LogoUrl);
        settings.Phone = NullIfEmpty(r.Phone);
        settings.DeliveryEnabled = r.DeliveryEnabled;
        settings.PickupEnabled = r.PickupEnabled;
        settings.MinOrderAmount = r.MinOrderAmount;
        settings.DeliveryFee = r.DeliveryFee;
        settings.FreeDeliveryFrom = r.FreeDeliveryFrom;
        settings.OpensAt = r.OpensAt;
        settings.ClosesAt = r.ClosesAt;
        settings.TransferCardNumber = card;
        settings.TransferCardHolder = NullIfEmpty(r.TransferCardHolder);
        settings.RequireTransferBeforeDispatch = r.RequireTransferBeforeDispatch;
        settings.DeliveryMinMinutes = r.DeliveryMinMinutes;
        settings.DeliveryMaxMinutes = r.DeliveryMaxMinutes;
        settings.AutoCancelMinutes = r.AutoCancelMinutes;
        settings.DeliveryRadiusKm = r.DeliveryRadiusKm;
        settings.DeliveryServiceMxikCode = mxik;
        settings.BonusPayEnabled = r.BonusPayEnabled;
        settings.BonusMaxPercent = r.BonusMaxPercent;
        settings.SupportPhone = NullIfEmpty(r.SupportPhone);
        settings.SupportTelegram = telegram;
        settings.ReferralEnabled = r.ReferralEnabled;
        settings.ReferrerBonus = Math.Floor(r.ReferrerBonus);
        settings.RefereeBonus = Math.Floor(r.RefereeBonus);

        // Bosqich 10 (ixtiyoriy maydonlar).
        if (r.AcceptWhenClosed is { } acceptWhenClosed) settings.AcceptWhenClosed = acceptWhenClosed;
        if (r.ReturnsEnabled is { } returnsEnabled) settings.ReturnsEnabled = returnsEnabled;
        if (r.ReturnDays is { } returnDays) settings.ReturnDays = Math.Clamp(returnDays, 1, 30);
        if (r.ReturnPickupByCourier is { } pickup) settings.ReturnPickupByCourier = pickup;
        if (r.CourierOfferMode is { } mode && Enum.IsDefined(mode)) settings.CourierOfferMode = mode;
        if (r.CourierOfferTimeoutSec is { } timeout) settings.CourierOfferTimeoutSec = Math.Clamp(timeout, 15, 3600);

        await _db.SaveChangesAsync(ct);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> RequestListingAsync(CancellationToken ct = default)
    {
        var orgId = CurrentOrganization();
        var org = await RequireOrganization(orgId, ct);
        var settings = await GetOrCreate(orgId, ct);

        if (settings.IsEnabled)
            throw new ValidationException("Do'kon allaqachon ilovada.");
        if (settings.IsBlocked)
            throw new ValidationException($"Do'kon bloklangan: {settings.BlockReason}");

        settings.IsRequested = true;
        await _db.SaveChangesAsync(ct);
        return ToDto(org, settings);
    }

    public async Task<OnlineStoreSettingsDto> SetAcceptingOrdersAsync(bool isAccepting, CancellationToken ct = default)
    {
        var orgId = CurrentOrganization();
        var org = await RequireOrganization(orgId, ct);
        var settings = await GetOrCreate(orgId, ct);

        settings.IsAcceptingOrders = isAccepting;
        await _db.SaveChangesAsync(ct);
        return ToDto(org, settings);
    }

    // ---------------- yordamchilar ----------------

    private static readonly Regex SlugRegex = new("^[a-z0-9-]{3,40}$", RegexOptions.Compiled);
    private static readonly Regex PackageIdRegex = new(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$", RegexOptions.Compiled);
    private static readonly Regex ColorRegex = new("^#[0-9A-F]{6}$", RegexOptions.Compiled);
    private static readonly Regex TelegramRegex = new("^[A-Za-z0-9_]{5,32}$", RegexOptions.Compiled);

    private static void ValidateBuilds(int? min, int? latest, string platform)
    {
        if (min is < 0 || latest is < 0)
            throw new ValidationException($"{platform} build raqamlari manfiy bo'lmasligi kerak.");
        if (min is { } m && latest is { } l && l < m)
            throw new ValidationException($"{platform}: oxirgi build minimal build'dan kichik bo'lmasligi kerak.");
    }

    /// <summary>Bo'sh — null; aks holda to'liq https havola bo'lishi shart.</summary>
    private static string? NormalizeHttpsUrl(string? value, string platform)
    {
        var url = NullIfEmpty(value);
        if (url is null) return null;
        if (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException($"{platform} store havolasi https:// bilan boshlanadigan to'liq manzil bo'lishi kerak.");
        return url;
    }

    private Guid CurrentOrganization() =>
        _currentUser.OrganizationId ?? throw new ValidationException("Bu amal tashkilot foydalanuvchisi uchun.");

    private async Task<Organization> RequireOrganization(Guid id, CancellationToken ct) =>
        await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new NotFoundException("Tashkilot topilmadi.");

    private Task<OnlineStoreSettings?> Find(Guid orgId, CancellationToken ct) =>
        _db.OnlineStoreSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

    private async Task<OnlineStoreSettings> GetOrCreate(Guid orgId, CancellationToken ct)
    {
        var settings = await Find(orgId, ct);
        if (settings is not null) return settings;

        settings = NewSettings(orgId);
        _db.OnlineStoreSettings.Add(settings);
        return settings;
    }

    /// <summary>Yangi yozuv: ilovada ko'rinmaydi (SuperAdmin yoqmaguncha).</summary>
    private static OnlineStoreSettings NewSettings(Guid orgId) => new() { OrganizationId = orgId };

    private static OnlineStoreSettingsDto ToDto(Organization org, OnlineStoreSettings s) => new()
    {
        OrganizationId = org.Id,
        OrganizationName = org.Name,
        IsRequested = s.IsRequested,
        IsEnabled = s.IsEnabled,
        IsBlocked = s.IsBlocked,
        BlockReason = s.BlockReason,
        IsFeatured = s.IsFeatured,
        SortOrder = s.SortOrder,
        PaidUntil = s.PaidUntil,
        AllowCash = s.AllowCash,
        AllowTransfer = s.AllowTransfer,
        AllowCardOnDelivery = s.AllowCardOnDelivery,
        AllowPayme = s.AllowPayme,
        AllowClick = s.AllowClick,
        IsAcceptingOrders = s.IsAcceptingOrders,
        DisplayName = s.DisplayName,
        Description = s.Description,
        LogoUrl = s.LogoUrl,
        Phone = s.Phone,
        DeliveryEnabled = s.DeliveryEnabled,
        PickupEnabled = s.PickupEnabled,
        MinOrderAmount = s.MinOrderAmount,
        DeliveryFee = s.DeliveryFee,
        FreeDeliveryFrom = s.FreeDeliveryFrom,
        OpensAt = s.OpensAt,
        ClosesAt = s.ClosesAt,
        TransferCardNumber = s.TransferCardNumber,
        TransferCardHolder = s.TransferCardHolder,
        RequireTransferBeforeDispatch = s.RequireTransferBeforeDispatch,
        DeliveryMinMinutes = s.DeliveryMinMinutes,
        DeliveryMaxMinutes = s.DeliveryMaxMinutes,
        AutoCancelMinutes = s.AutoCancelMinutes,
        DeliveryRadiusKm = s.DeliveryRadiusKm,
        DeliveryServiceMxikCode = s.DeliveryServiceMxikCode,
        BonusPayEnabled = s.BonusPayEnabled,
        BonusMaxPercent = s.BonusMaxPercent,
        Slug = s.Slug,
        AppName = s.AppName,
        AppPackageId = s.AppPackageId,
        BrandColor = s.BrandColor,
        AndroidMinBuild = s.AndroidMinBuild,
        AndroidLatestBuild = s.AndroidLatestBuild,
        AndroidLatestVersion = s.AndroidLatestVersion,
        AndroidStoreUrl = s.AndroidStoreUrl,
        IosMinBuild = s.IosMinBuild,
        IosLatestBuild = s.IosLatestBuild,
        IosLatestVersion = s.IosLatestVersion,
        IosStoreUrl = s.IosStoreUrl,
        IsVisible = org.IsActive && s.IsEnabled && !s.IsBlocked && (s.PaidUntil == null || s.PaidUntil >= DateTime.UtcNow),
        SupportPhone = s.SupportPhone,
        SupportTelegram = s.SupportTelegram,
        ReferralEnabled = s.ReferralEnabled,
        ReferrerBonus = s.ReferrerBonus,
        RefereeBonus = s.RefereeBonus,
        AcceptWhenClosed = s.AcceptWhenClosed,
        ReturnsEnabled = s.ReturnsEnabled,
        ReturnDays = s.ReturnDays,
        ReturnPickupByCourier = s.ReturnPickupByCourier,
        CourierOfferMode = s.CourierOfferMode,
        CourierOfferTimeoutSec = s.CourierOfferTimeoutSec
    };

    private static string? NormalizeCard(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var digits = new string(s.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
