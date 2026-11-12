using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Domain.Entities;
using ValidationException = PosCloud.Application.Common.Exceptions.ValidationException;
using NotFoundException = PosCloud.Application.Common.Exceptions.NotFoundException;
using ConflictException = PosCloud.Application.Common.Exceptions.ConflictException;

namespace PosCloud.Application.Features.PromoCodes;

public class PromoCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int PerUserLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static PromoCodeDto FromEntity(PromoCode p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        DiscountPercent = p.DiscountPercent,
        DiscountAmount = p.DiscountAmount,
        MinOrderAmount = p.MinOrderAmount,
        ValidFrom = p.ValidFrom,
        ValidTo = p.ValidTo,
        MaxUses = p.MaxUses,
        UsedCount = p.UsedCount,
        PerUserLimit = p.PerUserLimit,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}

public class SavePromoCodeRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, 100)]
    public decimal? DiscountPercent { get; set; }

    [Range(1, 1_000_000_000)]
    public decimal? DiscountAmount { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal MinOrderAmount { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [Range(1, 1_000_000)]
    public int? MaxUses { get; set; }

    /// <summary>0 — cheklovsiz.</summary>
    [Range(0, 1000)]
    public int PerUserLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}

public interface IPromoCodeService
{
    Task<IReadOnlyList<PromoCodeDto>> GetAllAsync(CancellationToken ct = default);
    Task<PromoCodeDto> CreateAsync(SavePromoCodeRequest request, CancellationToken ct = default);
    Task<PromoCodeDto> UpdateAsync(Guid id, SavePromoCodeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Do'kon admini uchun promokodlar (tenant filtri ostida — faqat o'z tashkiloti).</summary>
public class PromoCodeService : IPromoCodeService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PromoCodeService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PromoCodeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db.PromoCodes.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        return list.Select(PromoCodeDto.FromEntity).ToList();
    }

    public async Task<PromoCodeDto> CreateAsync(SavePromoCodeRequest request, CancellationToken ct = default)
    {
        var orgId = _currentUser.OrganizationId ?? throw new ValidationException("Tashkilot aniqlanmadi.");
        var promo = new PromoCode { OrganizationId = orgId };
        await Apply(promo, request, ct);
        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync(ct);
        return PromoCodeDto.FromEntity(promo);
    }

    public async Task<PromoCodeDto> UpdateAsync(Guid id, SavePromoCodeRequest request, CancellationToken ct = default)
    {
        var promo = await Find(id, ct);
        await Apply(promo, request, ct);
        await _db.SaveChangesAsync(ct);
        return PromoCodeDto.FromEntity(promo);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var promo = await Find(id, ct);
        // Buyurtmalardagi bog'lanish SetNull bo'ladi, kod matni va chegirma buyurtmada saqlanib qoladi.
        _db.PromoCodes.Remove(promo);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<PromoCode> Find(Guid id, CancellationToken ct) =>
        await _db.PromoCodes.FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new NotFoundException("Promokod topilmadi.");

    private async Task Apply(PromoCode promo, SavePromoCodeRequest r, CancellationToken ct)
    {
        var code = OnlineOrderPricing.NormalizeCode(r.Code)
                   ?? throw new ValidationException("Promokodni kiriting.");
        if (code.Length < 3 || code.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
            throw new ValidationException("Promokod kamida 3 belgi: harf, raqam, \"-\" yoki \"_\".");

        var hasPercent = r.DiscountPercent is > 0;
        var hasAmount = r.DiscountAmount is > 0;
        if (hasPercent == hasAmount)
            throw new ValidationException("Chegirmani foizda yoki summada kiriting (bittasini).");
        if (r.DiscountPercent is > 100)
            throw new ValidationException("Foiz 100 dan oshmasin.");
        if (r.ValidFrom is { } from && r.ValidTo is { } to && to < from)
            throw new ValidationException("Tugash sanasi boshlanishdan oldin bo'lmasin.");
        if (r.PerUserLimit < 0)
            throw new ValidationException("Mijozga limit manfiy bo'lmasin.");

        if (await _db.PromoCodes.AnyAsync(p => p.Code == code && p.Id != promo.Id, ct))
            throw new ConflictException($"\"{code}\" promokodi allaqachon mavjud.");

        promo.Code = code;
        promo.DiscountPercent = hasPercent ? r.DiscountPercent : null;
        promo.DiscountAmount = hasAmount ? r.DiscountAmount : null;
        promo.MinOrderAmount = Math.Max(0, r.MinOrderAmount);
        promo.ValidFrom = ToUtc(r.ValidFrom);
        promo.ValidTo = ToUtc(r.ValidTo);
        promo.MaxUses = r.MaxUses;
        promo.PerUserLimit = r.PerUserLimit;
        promo.IsActive = r.IsActive;
    }

    private static DateTime? ToUtc(DateTime? d) => d is null ? null
        : d.Value.Kind == DateTimeKind.Local ? d.Value.ToUniversalTime()
        : DateTime.SpecifyKind(d.Value, DateTimeKind.Utc);
}
