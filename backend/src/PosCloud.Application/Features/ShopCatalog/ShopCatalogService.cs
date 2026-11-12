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
    Task<IReadOnlyList<ShopStoreDto>> GetStoresAsync(double? lat, double? lng, string? search, CancellationToken ct = default);
    Task<ShopStoreDto> GetStoreAsync(Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<ShopCategoryDto>> GetCategoriesAsync(Guid branchId, CancellationToken ct = default);
    Task<PagedResult<ShopProductDto>> GetProductsAsync(Guid branchId, ShopProductQuery query, CancellationToken ct = default);
    Task<ShopProductDetailDto> GetProductAsync(Guid branchId, Guid productId, CancellationToken ct = default);
}

/// <summary>
/// Mobil ilova katalogi (tokensiz ochiq). Tenant filtri yo'q — har so'rovda tashkilot va filial
/// QO'LDA filtrlanadi. Ko'rinadigan do'kon: filial va tashkilot faol, turi Store, online sozlama
/// yoqilgan, bloklanmagan, tarif muddati o'tmagan.
/// </summary>
public class ShopCatalogService : IShopCatalogService
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
        CancellationToken ct = default)
    {
        var rows = await LoadVisibleStores(null, ct);

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
        CancellationToken ct = default)
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
            var lower = term.ToLower();
            products = products.Where(p => p.Name.ToLower().Contains(lower) || p.Barcode == term);
        }

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

        return new PagedResult<ShopProductDto>
        {
            Items = pageRows.Select(r => Fill(new ShopProductDto(), r.Product, r.Available, promotions)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ShopProductDetailDto> GetProductAsync(Guid branchId, Guid productId, CancellationToken ct = default)
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
        CancellationToken ct)
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

    private static T Fill<T>(T dto, Product p, decimal available, List<Promotion> promotions) where T : ShopProductDto
    {
        var percent = PromotionCalculator.GetDiscountPercent(promotions, p.Id, p.CategoryId);

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
            AllowCash = s.AllowCash,
            AllowTransfer = s.AllowTransfer,
            AllowCardOnDelivery = s.AllowCardOnDelivery
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

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
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
