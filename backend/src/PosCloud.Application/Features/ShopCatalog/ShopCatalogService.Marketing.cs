using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.ShopCatalog.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopCatalog;

/// <summary>
/// Bosqich 9: bosh sahifa (banner, Top 10, kolleksiyalar, stories, "Yana olasizmi?"), birga olinadiganlar,
/// qidiruv takliflari va sevimlilar. Tenant filtri yo'q — tashkilot va filial QO'LDA filtrlanadi.
/// Sotuv statistikasi: kassa savdolari (online buyurtma savdolari bundan chiqariladi) + yetkazilgan online buyurtmalar.
/// </summary>
public partial class ShopCatalogService
{
    public const int TopLimit = 10;
    private const int TopFallbackDays = 30;
    private const int RelatedDays = 90;
    private const int BuyAgainDays = 180;
    private const int BuyAgainLimit = 12;
    private const int HomeCollectionLimit = 10;
    private const int HomeBannerLimit = 10;
    private const int HomeStoryLimit = 20;
    private const int SuggestProducts = 8;
    private const int SuggestCategories = 3;

    public async Task<ShopHomeDto> GetHomeAsync(Guid branchId, Guid? shopUserId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var now = DateTime.UtcNow;
        var promotions = await LoadActivePromotions(orgId, ct);

        var banners = await _db.ShopBanners.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.OrganizationId == orgId && b.IsActive
                        && (b.BranchId == null || b.BranchId == branchId)
                        && (b.StartsAt == null || b.StartsAt <= now)
                        && (b.EndsAt == null || b.EndsAt > now))
            .OrderBy(b => b.SortOrder).ThenBy(b => b.CreatedAt)
            .Take(HomeBannerLimit)
            .Select(b => new ShopBannerDto
            {
                Id = b.Id,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ImageUrl = b.ImageUrl,
                BackgroundColor = b.BackgroundColor,
                LinkType = b.LinkType,
                LinkValue = b.LinkValue,
                EndsAt = b.EndsAt
            })
            .ToListAsync(ct);

        var top = await TopInternalAsync(orgId, branchId, 7, TopLimit, promotions, ct);

        // Bosh sahifadagi kolleksiyalar: har birida ko'pi bilan 10 ta, faqat qoldig'i bor mahsulotlar.
        var collections = await ActiveCollections(orgId, branchId, now)
            .Where(c => c.ShowOnHome)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt)
            .Take(HomeCollectionLimit)
            .ToListAsync(ct);
        var collectionDtos = new List<ShopCollectionDto>();
        if (collections.Count > 0)
        {
            var colIds = collections.Select(c => c.Id).ToList();
            var items = await _db.ShopCollectionItems.IgnoreQueryFilters().AsNoTracking()
                .Where(i => colIds.Contains(i.CollectionId))
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.CollectionId, i.ProductId })
                .ToListAsync(ct);
            var products = await LoadByIdsAsync(orgId, branchId, items.Select(i => i.ProductId).Distinct().ToList(),
                true, promotions, ct);

            foreach (var c in collections)
            {
                var list = items.Where(i => i.CollectionId == c.Id)
                    .Select(i => products.GetValueOrDefault(i.ProductId))
                    .Where(p => p is not null)
                    .Take(TopLimit)
                    .Select(p => p!)
                    .ToList();
                if (list.Count == 0) continue;
                collectionDtos.Add(ToCollectionDto(c, list));
            }
        }

        var discountedCount = await OnlyDiscounted(VisibleProducts(orgId, branchId), orgId)
            .Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0))
            .CountAsync(ct);

        var stories = await _db.ShopStories.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.OrganizationId == orgId && s.IsActive
                        && (s.BranchId == null || s.BranchId == branchId)
                        && (s.StartsAt == null || s.StartsAt <= now)
                        && (s.EndsAt == null || s.EndsAt > now)
                        && s.Slides.Any())
            .OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt)
            .Take(HomeStoryLimit)
            .Select(s => new ShopStoryDto
            {
                Id = s.Id,
                Title = s.Title,
                CoverImageUrl = s.CoverImageUrl,
                Slides = s.Slides.OrderBy(x => x.SortOrder).Select(x => new ShopStorySlideDto
                {
                    Id = x.Id,
                    ImageUrl = x.ImageUrl,
                    Caption = x.Caption,
                    LinkType = x.LinkType,
                    LinkValue = x.LinkValue,
                    DurationSec = x.DurationSec
                }).ToList()
            })
            .ToListAsync(ct);

        var buyAgain = shopUserId is { } userId
            ? await BuyAgainAsync(orgId, branchId, userId, promotions, ct)
            : [];

        var recipes = await HomeRecipesAsync(orgId, branchId, ct);

        var home = new ShopHomeDto
        {
            Recipes = recipes,
            Banners = banners,
            TopProducts = top,
            Collections = collectionDtos,
            DiscountedCount = discountedCount,
            BuyAgain = buyAgain,
            Stories = stories
        };
        await MarkFavoritesAsync(top.Concat(buyAgain).Concat(collectionDtos.SelectMany(c => c.Products)), shopUserId, ct);
        return home;
    }

    public async Task<IReadOnlyList<ShopProductDto>> GetTopAsync(Guid branchId, int days, int limit, Guid? shopUserId,
        CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var promotions = await LoadActivePromotions(orgId, ct);
        var list = await TopInternalAsync(orgId, branchId, Math.Clamp(days, 1, 90), Math.Clamp(limit, 1, TopLimit),
            promotions, ct);
        await MarkFavoritesAsync(list, shopUserId, ct);
        return list;
    }

    public async Task<ShopCollectionDto> GetCollectionAsync(Guid branchId, Guid collectionId, Guid? shopUserId,
        CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var collection = await ActiveCollections(orgId, branchId, DateTime.UtcNow)
                             .FirstOrDefaultAsync(c => c.Id == collectionId, ct)
                         ?? throw new NotFoundException("Kolleksiya topilmadi.");

        var ids = await _db.ShopCollectionItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.CollectionId == collectionId)
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ProductId)
            .ToListAsync(ct);
        var promotions = await LoadActivePromotions(orgId, ct);
        var products = await LoadByIdsAsync(orgId, branchId, ids, false, promotions, ct);

        var list = ids.Select(id => products.GetValueOrDefault(id)).Where(p => p is not null).Select(p => p!).ToList();
        await MarkFavoritesAsync(list, shopUserId, ct);
        return ToCollectionDto(collection, list);
    }

    public async Task<IReadOnlyList<ShopProductDto>> GetRelatedAsync(Guid branchId, Guid productId, int limit,
        Guid? shopUserId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        limit = Math.Clamp(limit, 1, 20);

        var categoryId = await VisibleProducts(orgId, branchId).Where(p => p.Id == productId)
            .Select(p => (Guid?)p.CategoryId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Mahsulot topilmadi.");

        var list = await SuggestAsync(orgId, branchId, [productId], [categoryId], limit, ct);
        await MarkFavoritesAsync(list, shopUserId, ct);
        return list;
    }

    public async Task<IReadOnlyList<ShopProductDto>> GetCartSuggestionsAsync(Guid branchId,
        IReadOnlyCollection<Guid> productIds, Guid? shopUserId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var ids = (productIds ?? []).Distinct().Take(100).ToList();
        if (ids.Count == 0) return [];

        var categoryIds = await VisibleProducts(orgId, branchId).Where(p => ids.Contains(p.Id))
            .Select(p => p.CategoryId).Distinct().ToListAsync(ct);

        var list = await SuggestAsync(orgId, branchId, ids, categoryIds, TopLimit, ct);
        await MarkFavoritesAsync(list, shopUserId, ct);
        return list;
    }

    public async Task<ShopSearchSuggestDto> SearchSuggestAsync(Guid branchId, string? q, CancellationToken ct = default)
    {
        var result = new ShopSearchSuggestDto();
        var term = q?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length > 100) return result;

        var orgId = await RequireStoreOrganization(branchId, ct);
        var terms = ShopSearch.Terms(term);
        if (terms.Count == 0) return result;

        var rows = await VisibleProducts(orgId, branchId)
            .Where(ShopSearch.Match<Product>(p => p.Name, terms, p => p.Barcode == term))
            .Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0))
            .OrderBy(p => p.Name.Length).ThenBy(p => p.Name)
            .Take(SuggestProducts)
            .Select(p => new { p.Id, p.Name, p.ImageUrl, p.Price, p.CategoryId })
            .ToListAsync(ct);

        var promotions = await LoadActivePromotions(orgId, ct);
        result.Products = rows.Select(r => new ShopProductBriefDto
        {
            Id = r.Id,
            Name = r.Name,
            ImageUrl = r.ImageUrl,
            Price = DiscountedPrice(r.Price,
                Promotions.PromotionCalculator.GetDiscountPercent(promotions, r.Id, r.CategoryId))
        }).ToList();

        // Kategoriyalar: nomi mos va sotuvda mahsuloti bor.
        var visible = VisibleProducts(orgId, branchId)
            .Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0));
        result.Categories = await _db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.OrganizationId == orgId && c.ShowOnline)
            .Where(ShopSearch.Match<Category>(c => c.Name, terms))
            .Where(c => visible.Any(p => p.CategoryId == c.Id || p.Category!.ParentId == c.Id))
            .OrderBy(c => c.Name.Length).ThenBy(c => c.Name)
            .Take(SuggestCategories)
            .Select(c => new ShopCategoryBriefDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        return result;
    }

    public async Task<IReadOnlyList<ShopProductDto>> GetFavoritesAsync(Guid shopUserId, Guid branchId,
        CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var ids = await _db.ShopFavorites.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.ShopUserId == shopUserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ProductId)
            .ToListAsync(ct);
        if (ids.Count == 0) return [];

        var promotions = await LoadActivePromotions(orgId, ct);
        var products = await LoadByIdsAsync(orgId, branchId, ids, false, promotions, ct);
        var list = ids.Select(id => products.GetValueOrDefault(id)).Where(p => p is not null).Select(p => p!).ToList();
        foreach (var p in list) p.IsFavorite = true;
        return list;
    }

    // ---------------- yordamchilar ----------------

    /// <summary>Faqat amaldagi aksiyaga tushadigan mahsulotlar (PromotionCalculator bilan bir xil moslik).</summary>
    private IQueryable<Product> OnlyDiscounted(IQueryable<Product> products, Guid orgId)
    {
        var now = DateTime.UtcNow;
        var promos = _db.Promotions.IgnoreQueryFilters()
            .Where(pr => pr.OrganizationId == orgId && pr.IsActive && pr.StartDate <= now && pr.EndDate >= now
                         && pr.DiscountPercent > 0);
        return products.Where(p => promos.Any(pr => pr.ProductId == p.Id || pr.CategoryId == p.CategoryId
                                                    || (pr.ProductId == null && pr.CategoryId == null)));
    }

    private IQueryable<ShopCollection> ActiveCollections(Guid orgId, Guid branchId, DateTime now) =>
        _db.ShopCollections.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.OrganizationId == orgId && c.IsActive
                        && (c.BranchId == null || c.BranchId == branchId)
                        && (c.StartsAt == null || c.StartsAt <= now)
                        && (c.EndsAt == null || c.EndsAt > now));

    private static ShopCollectionDto ToCollectionDto(ShopCollection c, List<ShopProductDto> products) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        EndsAt = c.EndsAt,
        Products = products
    };

    /// <summary>Token bo'lsa — isFavorite (true/false); aks holda null qoladi.</summary>
    private async Task MarkFavoritesAsync(IEnumerable<ShopProductDto> items, Guid? shopUserId, CancellationToken ct)
    {
        if (shopUserId is not { } userId) return;
        var list = items.ToList();
        if (list.Count == 0) return;

        var ids = list.Select(p => p.Id).Distinct().ToList();
        var favorites = (await _db.ShopFavorites.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.ShopUserId == userId && ids.Contains(f.ProductId))
            .Select(f => f.ProductId)
            .ToListAsync(ct)).ToHashSet();
        foreach (var p in list) p.IsFavorite = favorites.Contains(p.Id);
    }

    /// <summary>ID bo'yicha ko'rinadigan mahsulotlar (shu filial narxi/qoldig'i bilan).</summary>
    private async Task<Dictionary<Guid, ShopProductDto>> LoadByIdsAsync(Guid orgId, Guid branchId, List<Guid> ids,
        bool inStockOnly, List<Promotion> promotions, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, ShopProductDto>();

        var query = VisibleProducts(orgId, branchId).Where(p => ids.Contains(p.Id));
        if (inStockOnly)
            query = query.Where(p => p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0));

        var rows = await query
            .Select(p => new
            {
                Product = p,
                Available = p.Stocks.Where(s => s.BranchId == branchId).Select(s => s.Quantity - s.Reserved).FirstOrDefault()
            })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Product.Id, r => Fill(new ShopProductDto(), r.Product, r.Available, promotions));
    }

    /// <summary>Filialda davr ichida sotilgan miqdor (kassa savdolari + yetkazilgan online buyurtmalar).</summary>
    private async Task<Dictionary<Guid, decimal>> SoldQuantitiesAsync(Guid branchId, DateTime since, CancellationToken ct)
    {
        var prefix = LoyaltyRules.OnlineSalePrefix;
        var sales = await _db.SaleItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.Sale!.BranchId == branchId && i.Sale.Status == SaleStatus.Completed
                        && i.Sale.Type == SaleType.Sale && i.Sale.CreatedAt >= since
                        && (i.Sale.ClientSaleId == null || !i.Sale.ClientSaleId.StartsWith(prefix)))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(i => i.Quantity) })
            .ToListAsync(ct);

        var online = await _db.OnlineOrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => !i.IsRemoved && i.OnlineOrder!.BranchId == branchId
                        && i.OnlineOrder.Status == OnlineOrderStatus.Delivered && i.OnlineOrder.DeliveredAt >= since)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(i => i.PickedQuantity ?? i.Quantity) })
            .ToListAsync(ct);

        var result = sales.ToDictionary(x => x.ProductId, x => x.Qty);
        foreach (var o in online)
            result[o.ProductId] = result.GetValueOrDefault(o.ProductId) + o.Qty;
        return result;
    }

    /// <summary>Top: oxirgi N kun miqdori bo'yicha; yetmasa 30 kunlik bilan to'ldiriladi. Faqat qoldig'i borlar.</summary>
    private async Task<List<ShopProductDto>> TopInternalAsync(Guid orgId, Guid branchId, int days, int limit,
        List<Promotion> promotions, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var result = new List<ShopProductDto>();

        foreach (var period in days >= TopFallbackDays ? new[] { days } : new[] { days, TopFallbackDays })
        {
            var sold = await SoldQuantitiesAsync(branchId, now.AddDays(-period), ct);
            var candidates = sold.Where(x => x.Value > 0 && result.All(r => r.Id != x.Key))
                .OrderByDescending(x => x.Value)
                .Take(limit * 5)
                .Select(x => x.Key)
                .ToList();
            var products = await LoadByIdsAsync(orgId, branchId, candidates, true, promotions, ct);
            foreach (var id in candidates)
            {
                if (result.Count >= limit) break;
                if (products.TryGetValue(id, out var p)) result.Add(p);
            }
            if (result.Count >= limit) break;
        }

        for (var i = 0; i < result.Count; i++) result[i].Rank = i + 1;
        return result;
    }

    /// <summary>
    /// Berilgan mahsulotlar bilan bir chekda/buyurtmada eng ko'p olinganlar (oxirgi 90 kun); yetmasa — shu kategoriyalardagi
    /// ommaboplar (30 kun). Berilgan mahsulotlar chiqmaydi, faqat qoldig'i borlar.
    /// </summary>
    private async Task<List<ShopProductDto>> SuggestAsync(Guid orgId, Guid branchId, List<Guid> sourceIds,
        List<Guid> categoryIds, int limit, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-RelatedDays);
        var prefix = LoyaltyRules.OnlineSalePrefix;

        var saleIds = _db.SaleItems.IgnoreQueryFilters()
            .Where(i => sourceIds.Contains(i.ProductId) && i.Sale!.BranchId == branchId
                        && i.Sale.Status == SaleStatus.Completed && i.Sale.Type == SaleType.Sale && i.Sale.CreatedAt >= since
                        && (i.Sale.ClientSaleId == null || !i.Sale.ClientSaleId.StartsWith(prefix)))
            .Select(i => i.SaleId);
        var fromSales = await _db.SaleItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => saleIds.Contains(i.SaleId) && !sourceIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(100)
            .ToListAsync(ct);

        var orderIds = _db.OnlineOrderItems.IgnoreQueryFilters()
            .Where(i => sourceIds.Contains(i.ProductId) && !i.IsRemoved && i.OnlineOrder!.BranchId == branchId
                        && i.OnlineOrder.Status == OnlineOrderStatus.Delivered && i.OnlineOrder.DeliveredAt >= since)
            .Select(i => i.OnlineOrderId);
        var fromOrders = await _db.OnlineOrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => orderIds.Contains(i.OnlineOrderId) && !i.IsRemoved && !sourceIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(100)
            .ToListAsync(ct);

        var counts = fromSales.ToDictionary(x => x.ProductId, x => x.Count);
        foreach (var o in fromOrders) counts[o.ProductId] = counts.GetValueOrDefault(o.ProductId) + o.Count;

        var promotions = await LoadActivePromotions(orgId, ct);
        var candidates = counts.OrderByDescending(x => x.Value).Select(x => x.Key).Take(limit * 5).ToList();
        var products = await LoadByIdsAsync(orgId, branchId, candidates, true, promotions, ct);
        var result = candidates.Where(products.ContainsKey).Select(id => products[id]).Take(limit).ToList();

        if (result.Count < limit && categoryIds.Count > 0)
        {
            // To'ldirish: shu kategoriyalardagi ommaboplar (30 kun).
            var exclude = sourceIds.Concat(result.Select(r => r.Id)).ToList();
            var sold = await SoldQuantitiesAsync(branchId, DateTime.UtcNow.AddDays(-PopularDays), ct);
            var pool = await VisibleProducts(orgId, branchId)
                .Where(p => categoryIds.Contains(p.CategoryId) && !exclude.Contains(p.Id)
                            && p.Stocks.Any(s => s.BranchId == branchId && s.Quantity - s.Reserved > 0))
                .Select(p => p.Id)
                .Take(500)
                .ToListAsync(ct);
            var fill = pool.OrderByDescending(id => sold.GetValueOrDefault(id)).Take(limit - result.Count).ToList();
            var fillProducts = await LoadByIdsAsync(orgId, branchId, fill, true, promotions, ct);
            result.AddRange(fill.Where(fillProducts.ContainsKey).Select(id => fillProducts[id]));
        }

        return result;
    }

    /// <summary>
    /// "Yana olasizmi?": mijozning shu tashkilotdagi oxirgi 180 kunlik yetkazilgan online buyurtmalari va kassa savdolaridan
    /// (Customer.ShopUserId orqali) eng ko'p olingan, hozir qoldig'i bor mahsulotlar.
    /// </summary>
    private async Task<List<ShopProductDto>> BuyAgainAsync(Guid orgId, Guid branchId, Guid shopUserId,
        List<Promotion> promotions, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-BuyAgainDays);
        var prefix = LoyaltyRules.OnlineSalePrefix;

        var fromOrders = await _db.OnlineOrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => !i.IsRemoved && i.OnlineOrder!.ShopUserId == shopUserId && i.OnlineOrder.OrganizationId == orgId
                        && i.OnlineOrder.Status == OnlineOrderStatus.Delivered && i.OnlineOrder.DeliveredAt >= since)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var customerIds = _db.Customers.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == orgId && c.ShopUserId == shopUserId)
            .Select(c => c.Id);
        var fromSales = await _db.SaleItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.Sale!.CustomerId != null && customerIds.Contains(i.Sale.CustomerId.Value)
                        && i.Sale.Status == SaleStatus.Completed && i.Sale.Type == SaleType.Sale && i.Sale.CreatedAt >= since
                        && (i.Sale.ClientSaleId == null || !i.Sale.ClientSaleId.StartsWith(prefix)))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var counts = fromOrders.ToDictionary(x => x.ProductId, x => x.Count);
        foreach (var s in fromSales) counts[s.ProductId] = counts.GetValueOrDefault(s.ProductId) + s.Count;
        if (counts.Count == 0) return [];

        var candidates = counts.OrderByDescending(x => x.Value).Select(x => x.Key).Take(BuyAgainLimit * 5).ToList();
        var products = await LoadByIdsAsync(orgId, branchId, candidates, true, promotions, ct);
        return candidates.Where(products.ContainsKey).Select(id => products[id]).Take(BuyAgainLimit).ToList();
    }
}
