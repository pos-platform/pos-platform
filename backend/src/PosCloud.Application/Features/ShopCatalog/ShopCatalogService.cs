using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.ShopCatalog.Dtos;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopCatalog;

public interface IShopCatalogService
{
    Task<IReadOnlyList<ShopStoreDto>> GetStoresAsync(double? lat, double? lng, string? search, Guid? organizationId = null,
        CancellationToken ct = default);
    Task<ShopStoreAppDto> GetStoreAppAsync(string slug, CancellationToken ct = default);

    /// <summary>/api/app/config uchun: slug bo'yicha do'kon ilovasining platforma qiymatlari. Topilmasa null.</summary>
    Task<StoreAppPlatformInfo?> GetStoreAppPlatformAsync(string slug, bool isIos, CancellationToken ct = default);
    Task<ShopStoreDto> GetStoreAsync(Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<ShopCategoryDto>> GetCategoriesAsync(Guid branchId, CancellationToken ct = default);
    /// <param name="shopUserId">Token bo'lsa — isFavorite to'ldiriladi.</param>
    Task<PagedResult<ShopProductDto>> GetProductsAsync(Guid branchId, ShopProductQuery query, Guid? shopUserId = null,
        CancellationToken ct = default);

    /// <param name="shopUserId">Token bo'lsa — isFavorite va isSubscribed to'ldiriladi.</param>
    Task<ShopProductDetailDto> GetProductAsync(Guid branchId, Guid productId, Guid? shopUserId = null,
        CancellationToken ct = default);

    // ---- Bosqich 9: marketing ----
    Task<ShopHomeDto> GetHomeAsync(Guid branchId, Guid? shopUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ShopProductDto>> GetTopAsync(Guid branchId, int days, int limit, Guid? shopUserId, CancellationToken ct = default);
    Task<ShopCollectionDto> GetCollectionAsync(Guid branchId, Guid collectionId, Guid? shopUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ShopProductDto>> GetRelatedAsync(Guid branchId, Guid productId, int limit, Guid? shopUserId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ShopProductDto>> GetCartSuggestionsAsync(Guid branchId, IReadOnlyCollection<Guid> productIds,
        Guid? shopUserId, CancellationToken ct = default);
    Task<ShopSearchSuggestDto> SearchSuggestAsync(Guid branchId, string? q, CancellationToken ct = default);

    /// <summary>Mijozning sevimlilari — shu filial narxi/qoldig'i bilan (boshqa tashkilot mahsuloti chiqmaydi).</summary>
    Task<IReadOnlyList<ShopProductDto>> GetFavoritesAsync(Guid shopUserId, Guid branchId, CancellationToken ct = default);
}

/// <summary>
/// Mobil ilova katalogi (tokensiz ochiq). Tenant filtri yo'q — har so'rovda tashkilot va filial
/// QO'LDA filtrlanadi. Ko'rinadigan do'kon: filial va tashkilot faol, turi Store, online sozlama
/// yoqilgan, bloklanmagan, tarif muddati o'tmagan.
/// </summary>
public partial class ShopCatalogService : IShopCatalogService
{
    /// <summary>O'zbekiston vaqti (UTC+5, yozgi vaqt yo'q).</summary>
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(5);
    private const int PopularDays = 30;
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;

    public ShopCatalogService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ShopStoreDto>> GetStoresAsync(double? lat, double? lng, string? search,
        Guid? organizationId = null, CancellationToken ct = default)
    {
        var rows = await LoadVisibleStoresAsync(_db, null, ct, organizationId);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            rows = rows.Where(r =>
                    (r.Settings.DisplayName ?? r.OrganizationName).Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.Branch.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return rows
            .Select(r => (Dto: ToStoreDto(r, lat, lng), r.Settings.SortOrder))
            .OrderByDescending(x => x.Dto.IsFeatured)
            .ThenBy(x => x.Dto.DistanceKm ?? double.MaxValue)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Dto.Name)
            .Select(x => x.Dto)
            .ToList();
    }

    public async Task<ShopStoreAppDto> GetStoreAppAsync(string slug, CancellationToken ct = default)
    {
        const string unavailable = "Do'kon vaqtincha ishlamayapti";
        var key = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key)) throw new NotFoundException(unavailable);

        // Slug kichik harfda saqlanadi — katta-kichik harf farqsiz qidiruv.
        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
                           .FirstOrDefaultAsync(s => s.Slug == key, ct)
                       ?? throw new NotFoundException(unavailable);

        // Katalog bilan bir xil ko'rinish qoidalari; birorta filial ko'rinmasa — do'kon ishlamayapti.
        var rows = await LoadVisibleStoresAsync(_db, null, ct, settings.OrganizationId);
        if (rows.Count == 0) throw new NotFoundException(unavailable);

        var s = rows[0].Settings;
        var name = !string.IsNullOrWhiteSpace(s.AppName) ? s.AppName : StoreName(rows[0]);

        return new ShopStoreAppDto
        {
            OrganizationId = settings.OrganizationId!.Value,
            Slug = s.Slug!,
            AppName = name,
            BrandColor = s.BrandColor,
            LogoUrl = s.LogoUrl,
            Description = s.Description,
            Phone = s.Phone,
            Branches = rows.Select(r => ToStoreDto(r, null, null)).OrderBy(d => d.BranchName).ToList()
        };
    }

    public async Task<StoreAppPlatformInfo?> GetStoreAppPlatformAsync(string slug, bool isIos, CancellationToken ct = default)
    {
        var key = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key)) return null;

        var query = _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking().Where(s => s.Slug == key);
        return isIos
            ? await query.Select(s => new StoreAppPlatformInfo(s.IosMinBuild, s.IosLatestBuild, s.IosLatestVersion, s.IosStoreUrl))
                .FirstOrDefaultAsync(ct)
            : await query.Select(s => new StoreAppPlatformInfo(s.AndroidMinBuild, s.AndroidLatestBuild, s.AndroidLatestVersion, s.AndroidStoreUrl))
                .FirstOrDefaultAsync(ct);
    }

    public async Task<ShopStoreDto> GetStoreAsync(Guid branchId, CancellationToken ct = default)
    {
        var row = (await LoadVisibleStores(branchId, ct)).FirstOrDefault()
                  ?? throw new NotFoundException("Do'kon topilmadi yoki vaqtincha ishlamayapti.");
        return ToStoreDto(row, null, null);
    }

    public async Task<IReadOnlyList<ShopCategoryDto>> GetCategoriesAsync(Guid branchId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);

        return await VisibleProducts(orgId, branchId)
            .Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0))
            .GroupBy(p => new { p.CategoryId, p.Category!.Name })
            .Select(g => new ShopCategoryDto
            {
                Id = g.Key.CategoryId,
                Name = g.Key.Name,
                ProductCount = g.Count()
            })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ShopProductDto>> GetProductsAsync(Guid branchId, ShopProductQuery query,
        Guid? shopUserId = null, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var products = VisibleProducts(orgId, branchId);

        if (query.CategoryId is { } categoryId)
            products = products.Where(p => p.CategoryId == categoryId);

        var term = query.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // Transliteratsiya (lotin↔kirill), apostrof variantlari, har so'z nomda; yoki aniq shtrix-kod.
            var terms = ShopSearch.Terms(term);
            products = products.Where(ShopSearch.Match<Product>(p => p.Name, terms, p => p.Barcode == term));
        }

        if (query.Discounted)
            products = OnlyDiscounted(products, orgId);

        if (query.InStockOnly)
            products = products.Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0));

        var since = DateTime.UtcNow.AddDays(-PopularDays);
        var rows = products.Select(p => new ProductRow
        {
            Product = p,
            Available = p.Stocks.Where(s => s.BranchId == branchId).Select(s => s.Quantity - s.Reserved).FirstOrDefault(),
            // Ommaboplik: oxirgi 30 kunda shu filialdagi savdolarda necha marta sotilgan.
            SoldCount = _db.SaleItems.IgnoreQueryFilters().Count(i => i.ProductId == p.Id
                && i.Sale!.BranchId == branchId
                && i.Sale.Status == SaleStatus.Completed
                && i.Sale.CreatedAt >= since)
        });

        rows = (query.Sort ?? (string.IsNullOrEmpty(term) ? "popular" : "name")).ToLowerInvariant() switch
        {
            "name" => rows.OrderBy(r => r.Product.Name),
            "price_asc" => rows.OrderBy(r => r.Product.Price).ThenBy(r => r.Product.Name),
            "price_desc" => rows.OrderByDescending(r => r.Product.Price).ThenBy(r => r.Product.Name),
            _ => rows.OrderByDescending(r => r.SoldCount).ThenBy(r => r.Product.Name)
        };

        var total = await products.CountAsync(ct);
        var pageRows = await rows.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var promotions = await LoadActivePromotions(orgId, ct);
        var items = pageRows.Select(r => Fill(new ShopProductDto(), r.Product, r.Available, promotions)).ToList();
        await MarkFavoritesAsync(items, shopUserId, ct);

        return new PagedResult<ShopProductDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ShopProductDetailDto> GetProductAsync(Guid branchId, Guid productId, Guid? shopUserId = null,
        CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);

        var row = await VisibleProducts(orgId, branchId)
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                Product = p,
                CategoryName = p.Category!.Name,
                Available = p.Stocks.Where(s => s.BranchId == branchId).Select(s => s.Quantity - s.Reserved).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Mahsulot topilmadi.");

        var promotions = await LoadActivePromotions(orgId, ct);
        var dto = Fill(new ShopProductDetailDto(), row.Product, row.Available, promotions);
        dto.Description = row.Product.Description;
        dto.Barcode = row.Product.Barcode;
        dto.CategoryName = row.CategoryName;

        if (shopUserId is { } userId)
        {
            await MarkFavoritesAsync([dto], userId, ct);
            dto.IsSubscribed = await _db.ShopStockSubscriptions.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(s => s.ShopUserId == userId && s.ProductId == productId && s.BranchId == branchId
                               && s.NotifiedAt == null, ct);
        }
        return dto;
    }

    // ---------------- yordamchilar ----------------

    private sealed class ProductRow
    {
        public Product Product { get; init; } = null!;
        public decimal Available { get; init; }
        public int SoldCount { get; init; }
    }

    internal sealed record StoreRow(Branch Branch, string OrganizationName, OnlineStoreSettings Settings);

    private IQueryable<Product> VisibleProducts(Guid orgId, Guid branchId) =>
        VisibleProductsQuery(_db, orgId, branchId).AsNoTracking();

    private Task<List<StoreRow>> LoadVisibleStores(Guid? branchId, CancellationToken ct) =>
        LoadVisibleStoresAsync(_db, branchId, ct);

    private Task<List<Promotion>> LoadActivePromotions(Guid orgId, CancellationToken ct) =>
        LoadActivePromotionsAsync(_db, orgId, ct);

    /// <summary>Do'konning online'da ko'rinadigan mahsulotlari: o'z tashkiloti, ShowOnline, tovar, shu filial yoki umumiy.</summary>
    internal static IQueryable<Product> VisibleProductsQuery(IApplicationDbContext db, Guid orgId, Guid branchId) =>
        db.Products.IgnoreQueryFilters()
            .Where(p => p.OrganizationId == orgId
                        && p.ShowOnline
                        && p.ProductType == ProductType.Good
                        && (p.BranchId == null || p.BranchId == branchId));

    /// <summary>Ilovada ko'rinadigan do'konlar: filial va tashkilot faol, Store, yoqilgan, bloklanmagan, tarif amal qiladi.</summary>
    internal static async Task<List<StoreRow>> LoadVisibleStoresAsync(IApplicationDbContext db, Guid? branchId,
        CancellationToken ct, Guid? organizationId = null)
    {
        var now = DateTime.UtcNow;

        var query =
            from b in db.Branches.IgnoreQueryFilters().AsNoTracking()
            join s in db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking() on b.OrganizationId equals s.OrganizationId
            where b.IsActive
                  && b.BusinessType == BusinessType.Store
                  && b.Organization!.IsActive
                  && s.IsEnabled && !s.IsBlocked
                  && (s.PaidUntil == null || s.PaidUntil >= now)
            select new { Branch = b, OrganizationName = b.Organization!.Name, Settings = s };

        if (branchId is { } id)
            query = query.Where(x => x.Branch.Id == id);
        if (organizationId is { } orgId)
            query = query.Where(x => x.Settings.OrganizationId == orgId);

        var list = await query.ToListAsync(ct);
        return list.Select(x => new StoreRow(x.Branch, x.OrganizationName, x.Settings)).ToList();
    }

    private async Task<Guid> RequireStoreOrganization(Guid branchId, CancellationToken ct)
    {
        var row = (await LoadVisibleStores(branchId, ct)).FirstOrDefault()
                  ?? throw new NotFoundException("Do'kon topilmadi yoki vaqtincha ishlamayapti.");
        return row.Branch.OrganizationId!.Value;
    }

    internal static async Task<List<Promotion>> LoadActivePromotionsAsync(IApplicationDbContext db, Guid orgId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.Promotions.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync(ct);
    }

    internal static T Fill<T>(T dto, Product p, decimal available, List<Promotion> promotions) where T : ShopProductDto
    {
        var promo = PromotionCalculator.GetBestPromotion(promotions, p.Id, p.CategoryId);
        var percent = promo?.DiscountPercent ?? 0m;
        dto.DiscountEndsAt = percent > 0 ? promo!.EndDate : null;

        dto.Id = p.Id;
        dto.Name = p.Name;
        dto.CategoryId = p.CategoryId;
        dto.Unit = p.Unit;
        dto.IsWeighed = p.IsWeighed;
        dto.ImageUrl = p.ImageUrl;
        dto.Price = DiscountedPrice(p.Price, percent);
        dto.OldPrice = percent > 0 ? p.Price : null;
        dto.DiscountPercent = percent > 0 ? percent : null;
        dto.Available = Math.Max(0, available);
        dto.InStock = available > 0;
        return dto;
    }

    /// <summary>Aksiya qo'llangan narx (katalog va buyurtmada bir xil).</summary>
    internal static decimal DiscountedPrice(decimal price, decimal percent) =>
        percent > 0 ? Math.Round(price * (100m - percent) / 100m, 2, MidpointRounding.AwayFromZero) : price;

    internal static string StoreName(StoreRow r) =>
        string.IsNullOrWhiteSpace(r.Settings.DisplayName) ? r.OrganizationName : r.Settings.DisplayName;

    private static ShopStoreDto ToStoreDto(StoreRow r, double? lat, double? lng)
    {
        var s = r.Settings;
        var b = r.Branch;
        return new ShopStoreDto
        {
            Id = b.Id,
            OrganizationId = b.OrganizationId ?? Guid.Empty,
            Name = string.IsNullOrWhiteSpace(s.DisplayName) ? r.OrganizationName : s.DisplayName,
            BranchName = b.Name,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            Phone = s.Phone,
            Address = b.Address,
            Latitude = b.Latitude,
            Longitude = b.Longitude,
            DistanceKm = lat is { } la && lng is { } lo && b.Latitude is { } bla && b.Longitude is { } blo
                ? Math.Round(HaversineKm(la, lo, bla, blo), 2)
                : null,
            IsFeatured = s.IsFeatured,
            IsAcceptingOrders = s.IsAcceptingOrders,
            IsOpenNow = s.IsAcceptingOrders && IsWithinHours(s.OpensAt, s.ClosesAt),
            OpensAt = s.OpensAt,
            ClosesAt = s.ClosesAt,
            DeliveryEnabled = s.DeliveryEnabled,
            PickupEnabled = s.PickupEnabled,
            MinOrderAmount = s.MinOrderAmount,
            DeliveryFee = s.DeliveryFee,
            FreeDeliveryFrom = s.FreeDeliveryFrom,
            DeliveryMinMinutes = s.DeliveryMinMinutes,
            DeliveryMaxMinutes = s.DeliveryMaxMinutes,
            AllowCash = s.AllowCash,
            AllowTransfer = s.AllowTransfer && !string.IsNullOrWhiteSpace(s.TransferCardNumber),
            AllowCardOnDelivery = s.AllowCardOnDelivery,
            AllowPayme = s.AllowPayme,
            AllowClick = s.AllowClick,
            BonusPayEnabled = s.BonusPayEnabled && s.BonusMaxPercent > 0,
            BonusMaxPercent = s.BonusPayEnabled ? s.BonusMaxPercent : 0,
            DeliveryRadiusKm = s.DeliveryRadiusKm,
            SupportPhone = s.SupportPhone,
            SupportTelegram = s.SupportTelegram
        };
    }

    /// <summary>Ish vaqti berilmagan bo'lsa — doim ochiq. Tungi smena (22:00–06:00) ham qo'llanadi.</summary>
    public static bool IsWithinHours(TimeOnly? opens, TimeOnly? closes) =>
        IsWithinHours(opens, closes, DateTime.UtcNow);

    /// <summary>UTC vaqt do'kon ish vaqtiga (mahalliy, UTC+5) to'g'ri keladimi.</summary>
    public static bool IsWithinHours(TimeOnly? opens, TimeOnly? closes, DateTime utc)
    {
        if (opens is null || closes is null || opens == closes) return true;
        var now = TimeOnly.FromDateTime(utc + LocalOffset);
        return opens < closes
            ? now >= opens && now < closes
            : now >= opens || now < closes;
    }

    internal static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0;
        double ToRad(double d) => d * Math.PI / 180.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * r * Math.Asin(Math.Sqrt(a));
    }
}
