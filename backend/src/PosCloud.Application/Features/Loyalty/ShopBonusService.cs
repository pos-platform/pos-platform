using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Loyalty.Dtos;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty;

/// <summary>Mijoz (mobil ilova) tomoni: dinamik QR bonus karta, do'konlar bo'yicha ballar va tarix.</summary>
public interface IShopBonusService
{
    Task<BonusCardDto> GetCardAsync(Guid shopUserId, Guid branchId, CancellationToken ct = default);
    Task<IReadOnlyList<BonusStoreDto>> GetStoresAsync(Guid shopUserId, CancellationToken ct = default);
    Task<PagedResult<LoyaltyHistoryItemDto>> GetHistoryAsync(Guid shopUserId, Guid organizationId, int page, int pageSize,
        CancellationToken ct = default);
}

/// <summary>
/// Tenant filtri yo'q (mijoz JWT'sida tashkilot yo'q) — har so'rovda ShopUserId va tashkilot QO'LDA filtrlanadi.
/// </summary>
public class ShopBonusService : IShopBonusService
{
    /// <summary>QR kod amal qilish muddati.</summary>
    public static readonly TimeSpan QrLifetime = TimeSpan.FromSeconds(90);

    private readonly IApplicationDbContext _db;
    private readonly ILoyaltyQrSigner _qr;

    public ShopBonusService(IApplicationDbContext db, ILoyaltyQrSigner qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<BonusCardDto> GetCardAsync(Guid shopUserId, Guid branchId, CancellationToken ct = default)
    {
        var user = await RequireUser(shopUserId, ct);

        var branch = await _db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.Id == branchId && b.IsActive && b.Organization!.IsActive)
            .Select(b => new { b.OrganizationId, OrgName = b.Organization!.Name })
            .FirstOrDefaultAsync(ct);
        if (branch?.OrganizationId is not { } orgId)
            throw new NotFoundException("Do'kon topilmadi.");

        // Do'konda mijoz kartochkasi bo'lmasa — yaratiladi (signupBonus bir marta).
        var customer = await ShopCustomerLinker.EnsureAsync(_db, orgId, user, LoyaltySource.Online, ct);
        await _db.SaveChangesAsync(ct);

        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.OrganizationId == orgId).Select(s => s.DisplayName).FirstOrDefaultAsync(ct);

        var expiresAt = DateTime.UtcNow.Add(QrLifetime);
        // Sekund aniqligiga keltiramiz (payload unix sekundda).
        expiresAt = DateTime.SpecifyKind(new DateTime(expiresAt.Ticks - expiresAt.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

        return new BonusCardDto
        {
            StoreName = string.IsNullOrWhiteSpace(settings) ? branch.OrgName : settings!,
            Points = Math.Floor(Math.Max(0, customer.LoyaltyPoints)),
            QrPayload = _qr.Sign(user.Id, orgId, expiresAt),
            ExpiresAt = expiresAt
        };
    }

    public async Task<IReadOnlyList<BonusStoreDto>> GetStoresAsync(Guid shopUserId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var rows = await _db.Customers.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.ShopUserId == shopUserId && c.IsActive && c.OrganizationId != null)
            .Select(c => new { OrgId = c.OrganizationId!.Value, c.LoyaltyPoints })
            .ToListAsync(ct);
        if (rows.Count == 0) return [];

        var orgIds = rows.Select(r => r.OrgId).Distinct().ToList();
        var orgs = await _db.Organizations.AsNoTracking()
            .Where(o => orgIds.Contains(o.Id) && o.IsActive)
            .Select(o => new
            {
                o.Id,
                o.Name,
                Settings = _db.OnlineStoreSettings.IgnoreQueryFilters()
                    .Where(s => s.OrganizationId == o.Id)
                    .Select(s => new { s.DisplayName, s.LogoUrl })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        // Birinchi faol online filial (ilovada ko'rinadigan do'konlar).
        var visible = await ShopCatalogService.LoadVisibleStoresAsync(_db, null, ct);
        var firstBranch = visible
            .Where(v => v.Branch.OrganizationId is { } o && orgIds.Contains(o))
            .GroupBy(v => v.Branch.OrganizationId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Branch.CreatedAt).First().Branch.Id);

        return orgs
            .Select(o => new BonusStoreDto
            {
                OrganizationId = o.Id,
                BranchId = firstBranch.TryGetValue(o.Id, out var b) ? b : null,
                StoreName = string.IsNullOrWhiteSpace(o.Settings?.DisplayName) ? o.Name : o.Settings!.DisplayName!,
                LogoUrl = o.Settings?.LogoUrl,
                Points = Math.Floor(Math.Max(0, rows.Where(r => r.OrgId == o.Id).Sum(r => r.LoyaltyPoints)))
            })
            .OrderByDescending(s => s.Points)
            .ThenBy(s => s.StoreName)
            .ToList();
    }

    public async Task<PagedResult<LoyaltyHistoryItemDto>> GetHistoryAsync(Guid shopUserId, Guid organizationId, int page,
        int pageSize, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        // Faqat o'z kartochkasi (ShopUserId) va ko'rsatilgan tashkilot.
        var customerIds = _db.Customers.IgnoreQueryFilters()
            .Where(c => c.ShopUserId == shopUserId && c.OrganizationId == organizationId)
            .Select(c => c.Id);

        return await LoyaltyHistory.PageAsync(
            _db.LoyaltyTransactions.IgnoreQueryFilters().AsNoTracking()
                .Where(t => t.OrganizationId == organizationId && customerIds.Contains(t.CustomerId)),
            page, pageSize, ct);
    }

    private async Task<ShopUser> RequireUser(Guid shopUserId, CancellationToken ct)
    {
        var user = await _db.ShopUsers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Id == shopUserId, ct);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        return user;
    }
}
