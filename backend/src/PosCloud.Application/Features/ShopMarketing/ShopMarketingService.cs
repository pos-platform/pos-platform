using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopMarketing.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopMarketing;

/// <summary>Mijoz tomoni (Bosqich 9): sevimlilar, "kelganda xabar berish", savatcha nusxasi, do'stni taklif qilish.</summary>
public interface IShopMarketingService
{
    Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid shopUserId, CancellationToken ct = default);
    Task AddFavoriteAsync(Guid shopUserId, Guid productId, CancellationToken ct = default);
    Task RemoveFavoriteAsync(Guid shopUserId, Guid productId, CancellationToken ct = default);

    Task SubscribeStockAsync(Guid shopUserId, StockSubscriptionRequest request, CancellationToken ct = default);
    Task UnsubscribeStockAsync(Guid shopUserId, Guid productId, Guid branchId, CancellationToken ct = default);

    Task SaveCartAsync(Guid shopUserId, SaveShopCartRequest request, CancellationToken ct = default);

    Task<ShopReferralDto> GetReferralAsync(Guid shopUserId, Guid organizationId, CancellationToken ct = default);
    Task ApplyReferralAsync(Guid shopUserId, ApplyReferralRequest request, CancellationToken ct = default);
}

/// <summary>
/// Global yozuvlar (ShopFavorite, ShopStockSubscription, ShopCartSnapshot) — IgnoreQueryFilters, ShopUserId bo'yicha QO'LDA.
/// ShopReferral — tenant yozuv, OrganizationId qo'lda beriladi.
/// </summary>
public class ShopMarketingService : IShopMarketingService
{
    public const int MaxFavorites = 500;
    public const int MaxStockSubscriptions = 200;
    public const int MaxCartItems = 100;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IApplicationDbContext _db;

    public ShopMarketingService(IApplicationDbContext db)
    {
        _db = db;
    }

    // ---------------- sevimlilar ----------------

    public async Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid shopUserId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        return await _db.ShopFavorites.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.ShopUserId == shopUserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ProductId)
            .ToListAsync(ct);
    }

    public async Task AddFavoriteAsync(Guid shopUserId, Guid productId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var favorites = _db.ShopFavorites.IgnoreQueryFilters().Where(f => f.ShopUserId == shopUserId);
        if (await favorites.AnyAsync(f => f.ProductId == productId, ct)) return;

        await RequireOnlineProduct(productId, ct);
        if (await favorites.CountAsync(ct) >= MaxFavorites)
            throw new ValidationException($"Sevimlilarga ko'pi bilan {MaxFavorites} ta mahsulot qo'shish mumkin.");

        _db.ShopFavorites.Add(new ShopFavorite { ShopUserId = shopUserId, ProductId = productId, OrganizationId = null });
        await SaveIgnoringDuplicate(ct);
    }

    public async Task RemoveFavoriteAsync(Guid shopUserId, Guid productId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var rows = await _db.ShopFavorites.IgnoreQueryFilters()
            .Where(f => f.ShopUserId == shopUserId && f.ProductId == productId)
            .ToListAsync(ct);
        if (rows.Count == 0) return;
        _db.ShopFavorites.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- "kelganda xabar berish" ----------------

    public async Task SubscribeStockAsync(Guid shopUserId, StockSubscriptionRequest request, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var store = (await ShopCatalogService.LoadVisibleStoresAsync(_db, request.BranchId, ct)).FirstOrDefault()
                    ?? throw new NotFoundException("Do'kon topilmadi yoki vaqtincha ishlamayapti.");
        var orgId = store.Branch.OrganizationId!.Value;

        if (!await ShopCatalogService.VisibleProductsQuery(_db, orgId, request.BranchId).AnyAsync(p => p.Id == request.ProductId, ct))
            throw new NotFoundException("Mahsulot topilmadi.");

        var existing = await _db.ShopStockSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ShopUserId == shopUserId && s.ProductId == request.ProductId
                                      && s.BranchId == request.BranchId, ct);
        if (existing is not null)
        {
            // Avval xabar berilgan bo'lsa — qayta obuna.
            if (existing.NotifiedAt is null) return;
            existing.NotifiedAt = null;
            existing.CreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var count = await _db.ShopStockSubscriptions.IgnoreQueryFilters()
            .CountAsync(s => s.ShopUserId == shopUserId && s.NotifiedAt == null, ct);
        if (count >= MaxStockSubscriptions)
            throw new ValidationException($"Ko'pi bilan {MaxStockSubscriptions} ta mahsulotga obuna bo'lish mumkin.");

        _db.ShopStockSubscriptions.Add(new ShopStockSubscription
        {
            ShopUserId = shopUserId,
            ProductId = request.ProductId,
            BranchId = request.BranchId,
            OrganizationId = null
        });
        await SaveIgnoringDuplicate(ct);
    }

    public async Task UnsubscribeStockAsync(Guid shopUserId, Guid productId, Guid branchId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var rows = await _db.ShopStockSubscriptions.IgnoreQueryFilters()
            .Where(s => s.ShopUserId == shopUserId && s.ProductId == productId && s.BranchId == branchId)
            .ToListAsync(ct);
        if (rows.Count == 0) return;
        _db.ShopStockSubscriptions.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- savatcha nusxasi ----------------

    public async Task SaveCartAsync(Guid shopUserId, SaveShopCartRequest request, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var items = (request.Items ?? [])
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new { productId = g.Key, quantity = g.Sum(i => i.Quantity) })
            .ToList();
        if (items.Count > MaxCartItems)
            throw new ValidationException($"Savatchada ko'pi bilan {MaxCartItems} xil mahsulot bo'lishi mumkin.");

        var snapshot = await _db.ShopCartSnapshots.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ShopUserId == shopUserId && s.BranchId == request.BranchId, ct);

        if (items.Count == 0)
        {
            if (snapshot is null) return;
            _db.ShopCartSnapshots.Remove(snapshot);
            await _db.SaveChangesAsync(ct);
            return;
        }

        if (snapshot is null)
        {
            if (!await _db.Branches.IgnoreQueryFilters().AnyAsync(b => b.Id == request.BranchId, ct))
                throw new NotFoundException("Do'kon topilmadi.");
            snapshot = new ShopCartSnapshot { ShopUserId = shopUserId, BranchId = request.BranchId, OrganizationId = null };
            _db.ShopCartSnapshots.Add(snapshot);
        }

        snapshot.ItemsJson = JsonSerializer.Serialize(items);
        snapshot.ItemCount = items.Count;
        snapshot.ChangedAt = DateTime.UtcNow;
        await SaveIgnoringDuplicate(ct);
    }

    // ---------------- do'stni taklif qilish ----------------

    public async Task<ShopReferralDto> GetReferralAsync(Guid shopUserId, Guid organizationId, CancellationToken ct = default)
    {
        var user = await _db.ShopUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == shopUserId, ct);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");

        var store = await StoreInfo(organizationId, ct);
        var code = await EnsureCodeAsync(user, ct);

        var referrals = _db.ShopReferrals.IgnoreQueryFilters()
            .Where(r => r.OrganizationId == organizationId && r.ReferrerShopUserId == shopUserId);
        var invited = await referrals.CountAsync(ct);
        var rewarded = await referrals.CountAsync(r => r.RewardedAt != null, ct);

        var enabled = store.Settings?.ReferralEnabled == true;
        var refereeBonus = store.Settings?.RefereeBonus ?? 0;
        return new ShopReferralDto
        {
            Code = code,
            Enabled = enabled,
            ReferrerBonus = store.Settings?.ReferrerBonus ?? 0,
            RefereeBonus = refereeBonus,
            InvitedCount = invited,
            RewardedCount = rewarded,
            ShareText = refereeBonus > 0
                ? $"{store.Name} ilovasida taklif kodim: {code}. Birinchi buyurtmangiz yetkazilganda {refereeBonus:0} bonus ball olasiz!"
                : $"{store.Name} ilovasida taklif kodim: {code}"
        };
    }

    public async Task ApplyReferralAsync(Guid shopUserId, ApplyReferralRequest request, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var orgId = request.OrganizationId;
        var store = await StoreInfo(orgId, ct);
        if (store.Settings is not { ReferralEnabled: true } settings)
            throw new ValidationException("Bu do'konda do'stni taklif qilish dasturi yoqilmagan.");

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length is < 6 or > 8)
            throw new ValidationException("Taklif kodi noto'g'ri.");

        var referrerId = await _db.ShopUsers.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.ReferralCode == code && u.IsActive && u.DeletedAt == null)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationException("Taklif kodi topilmadi.");
        if (referrerId == shopUserId)
            throw new ValidationException("O'zingizni taklif qila olmaysiz.");

        if (await _db.OnlineOrders.IgnoreQueryFilters().AnyAsync(o => o.OrganizationId == orgId
                && o.ShopUserId == shopUserId && o.Status == OnlineOrderStatus.Delivered, ct))
            throw new ValidationException("Taklif kodi faqat yangi mijozlar uchun (birinchi buyurtmagacha).");

        if (await _db.ShopReferrals.IgnoreQueryFilters()
                .AnyAsync(r => r.OrganizationId == orgId && r.RefereeShopUserId == shopUserId, ct))
            throw new ValidationException("Siz allaqachon taklif kodini kiritgansiz.");

        _db.ShopReferrals.Add(new ShopReferral
        {
            OrganizationId = orgId,
            ReferrerShopUserId = referrerId,
            RefereeShopUserId = shopUserId,
            ReferrerBonus = Math.Floor(Math.Max(0, settings.ReferrerBonus)),
            RefereeBonus = Math.Floor(Math.Max(0, settings.RefereeBonus))
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException("Siz allaqachon taklif kodini kiritgansiz.");
        }
    }

    // ---------------- yordamchilar ----------------

    private sealed record StoreRow(string Name, OnlineStoreSettings? Settings);

    private async Task<StoreRow> StoreInfo(Guid organizationId, CancellationToken ct)
    {
        var org = await _db.Organizations.AsNoTracking()
                      .Where(o => o.Id == organizationId && o.IsActive)
                      .Select(o => new { o.Name })
                      .FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("Do'kon topilmadi.");
        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        var name = string.IsNullOrWhiteSpace(settings?.DisplayName) ? org.Name : settings!.DisplayName!;
        return new StoreRow(name, settings);
    }

    /// <summary>Taklif kodi birinchi so'rovda yaratiladi (6 belgi, to'qnashuvda uzunroq).</summary>
    private async Task<string> EnsureCodeAsync(ShopUser user, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(user.ReferralCode)) return user.ReferralCode;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var code = NewCode(attempt < 3 ? 6 : attempt < 5 ? 7 : 8);
            if (await _db.ShopUsers.IgnoreQueryFilters().AnyAsync(u => u.ReferralCode == code, ct)) continue;

            user.ReferralCode = code;
            try
            {
                await _db.SaveChangesAsync(ct);
                return code;
            }
            catch (DbUpdateException)
            {
                // Parallel so'rov xuddi shu kodni oldi — boshqasini urinamiz.
                user.ReferralCode = null;
            }
        }
        throw new ConflictException("Taklif kodini yaratib bo'lmadi. Qayta urinib ko'ring.");
    }

    private static string NewCode(int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return new string(chars);
    }

    private async Task RequireOnlineProduct(Guid productId, CancellationToken ct)
    {
        if (!await _db.Products.IgnoreQueryFilters()
                .AnyAsync(p => p.Id == productId && p.ShowOnline && p.ProductType == ProductType.Good, ct))
            throw new NotFoundException("Mahsulot topilmadi.");
    }

    /// <summary>Bir vaqtda ikki so'rov bir xil yozuvni qo'shsa — unikal indeks; natija baribir to'g'ri.</summary>
    private async Task SaveIgnoringDuplicate(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("So'rov bir vaqtda bajarilmoqda. Qayta urinib ko'ring.");
        }
    }

    private async Task RequireUser(Guid shopUserId, CancellationToken ct)
    {
        var ok = await _db.ShopUsers.IgnoreQueryFilters()
            .AnyAsync(u => u.Id == shopUserId && u.IsActive && u.DeletedAt == null, ct);
        if (!ok) throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
    }
}
