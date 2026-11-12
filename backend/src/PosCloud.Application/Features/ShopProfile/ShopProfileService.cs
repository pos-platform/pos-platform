using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopProfile.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopProfile;

public interface IShopProfileService
{
    Task<ShopProfileDto> GetAsync(Guid shopUserId, CancellationToken ct = default);
    Task<ShopProfileDto> UpdateAsync(Guid shopUserId, UpdateShopProfileRequest request, CancellationToken ct = default);
    Task DeleteAccountAsync(Guid shopUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ShopAddressDto>> GetAddressesAsync(Guid shopUserId, CancellationToken ct = default);
    Task<ShopAddressDto> CreateAddressAsync(Guid shopUserId, SaveShopAddressRequest request, CancellationToken ct = default);
    Task<ShopAddressDto> UpdateAddressAsync(Guid shopUserId, Guid addressId, SaveShopAddressRequest request, CancellationToken ct = default);
    Task DeleteAddressAsync(Guid shopUserId, Guid addressId, CancellationToken ct = default);
}

/// <summary>
/// Mijoz profili va manzillari. Har so'rov faqat shu ShopUserId yozuvlari bilan ishlaydi
/// (tenant filtri mijozga taalluqli emas — IgnoreQueryFilters).
/// </summary>
public class ShopProfileService : IShopProfileService
{
    public const int MaxAddresses = 10;

    private static readonly OnlineOrderStatus[] FinishedStatuses =
        [OnlineOrderStatus.Delivered, OnlineOrderStatus.Rejected, OnlineOrderStatus.Cancelled];

    private readonly IApplicationDbContext _db;

    public ShopProfileService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ShopProfileDto> GetAsync(Guid shopUserId, CancellationToken ct = default)
    {
        var user = await RequireUser(shopUserId, ct);
        return await ToDto(user, ct);
    }

    public async Task<ShopProfileDto> UpdateAsync(Guid shopUserId, UpdateShopProfileRequest request, CancellationToken ct = default)
    {
        var user = await RequireUser(shopUserId, ct);

        var firstName = request.FirstName.Trim();
        if (firstName.Length == 0)
            throw new ValidationException("Ismni kiriting.");

        if (request.BirthDate is { } birth)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (birth > today || birth < today.AddYears(-120))
                throw new ValidationException("Tug'ilgan sana noto'g'ri.");
        }

        if (request.Gender is { } gender && !Enum.IsDefined(gender))
            throw new ValidationException("Jins noto'g'ri.");

        user.FirstName = firstName;
        user.LastName = NullIfEmpty(request.LastName);
        user.BirthDate = request.BirthDate;
        user.Gender = request.Gender;
        user.Email = NullIfEmpty(request.Email)?.ToLowerInvariant();

        await _db.SaveChangesAsync(ct);
        return await ToDto(user, ct);
    }

    /// <summary>
    /// Akkauntni o'chirish (App Store / Play talabi). Shaxsiy ma'lumotlar tozalanadi, telefon bo'shatiladi
    /// (shu raqam bilan keyin yangi akkaunt ochish mumkin). Buyurtmalar do'kon hisobi uchun qoladi.
    /// </summary>
    public async Task DeleteAccountAsync(Guid shopUserId, CancellationToken ct = default)
    {
        var user = await RequireUser(shopUserId, ct);

        var hasActiveOrders = await _db.OnlineOrders.IgnoreQueryFilters()
            .AnyAsync(o => o.ShopUserId == shopUserId && !FinishedStatuses.Contains(o.Status), ct);
        if (hasActiveOrders)
            throw new ValidationException("Yakunlanmagan buyurtmangiz bor. U yetkazilgach yoki bekor qilingach akkauntni o'chirish mumkin.");

        var now = DateTime.UtcNow;

        // Telefon unikal va 20 belgigacha: "~" + ID'ning 19 belgisi.
        user.Phone = "~" + user.Id.ToString("N")[..19];
        user.FirstName = null;
        user.LastName = null;
        user.BirthDate = null;
        user.Gender = null;
        user.Email = null;
        user.IsActive = false;
        user.DeletedAt = now;

        var addresses = await _db.ShopUserAddresses.IgnoreQueryFilters()
            .Where(a => a.ShopUserId == shopUserId).ToListAsync(ct);
        _db.ShopUserAddresses.RemoveRange(addresses);

        var tokens = await _db.ShopRefreshTokens.IgnoreQueryFilters()
            .Where(t => t.ShopUserId == shopUserId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in tokens) t.RevokedAt = now;

        // Push tokenlari — o'chirilgan akkauntga bildirishnoma bormasin.
        var devices = await _db.ShopDeviceTokens.IgnoreQueryFilters()
            .Where(d => d.ShopUserId == shopUserId).ToListAsync(ct);
        _db.ShopDeviceTokens.RemoveRange(devices);

        // Do'konlardagi mijoz kartochkalari do'konniki — faqat bog'lanishni uzamiz.
        var customers = await _db.Customers.IgnoreQueryFilters()
            .Where(c => c.ShopUserId == shopUserId).ToListAsync(ct);
        foreach (var c in customers) c.ShopUserId = null;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ShopAddressDto>> GetAddressesAsync(Guid shopUserId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var list = await Addresses(shopUserId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<ShopAddressDto> CreateAddressAsync(Guid shopUserId, SaveShopAddressRequest request, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var existing = await Addresses(shopUserId).ToListAsync(ct);
        if (existing.Count >= MaxAddresses)
            throw new ValidationException($"Ko'pi bilan {MaxAddresses} ta manzil saqlash mumkin.");

        var address = new ShopUserAddress { ShopUserId = shopUserId };
        Apply(address, request);

        // Birinchi manzil har doim asosiy.
        address.IsDefault = request.IsDefault || existing.Count == 0;
        if (address.IsDefault)
            foreach (var a in existing) a.IsDefault = false;

        _db.ShopUserAddresses.Add(address);
        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<ShopAddressDto> UpdateAddressAsync(Guid shopUserId, Guid addressId, SaveShopAddressRequest request,
        CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var all = await Addresses(shopUserId).ToListAsync(ct);
        var address = all.FirstOrDefault(a => a.Id == addressId)
                      ?? throw new NotFoundException("Manzil topilmadi.");

        Apply(address, request);

        if (request.IsDefault)
        {
            foreach (var a in all) a.IsDefault = a.Id == addressId;
        }
        else if (address.IsDefault && all.Count > 1)
        {
            // Asosiy belgisini olib tashlash — boshqa manzilni asosiy qilib tanlash orqali bo'ladi.
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task DeleteAddressAsync(Guid shopUserId, Guid addressId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var all = await Addresses(shopUserId).ToListAsync(ct);
        var address = all.FirstOrDefault(a => a.Id == addressId)
                      ?? throw new NotFoundException("Manzil topilmadi.");

        _db.ShopUserAddresses.Remove(address);

        if (address.IsDefault)
        {
            var next = all.Where(a => a.Id != addressId).OrderByDescending(a => a.CreatedAt).FirstOrDefault();
            if (next is not null) next.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ---------------- yordamchilar ----------------

    private IQueryable<ShopUserAddress> Addresses(Guid shopUserId) =>
        _db.ShopUserAddresses.IgnoreQueryFilters().Where(a => a.ShopUserId == shopUserId);

    /// <summary>Token hali amal qilsa ham bloklangan/o'chirilgan akkaunt 401 oladi.</summary>
    private async Task<ShopUser> RequireUser(Guid shopUserId, CancellationToken ct)
    {
        var user = await _db.ShopUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == shopUserId, ct);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        return user;
    }

    private async Task<ShopProfileDto> ToDto(ShopUser u, CancellationToken ct) => new()
    {
        Id = u.Id,
        Phone = u.Phone,
        PhoneVerified = u.PhoneVerifiedAt is not null,
        FirstName = u.FirstName,
        LastName = u.LastName,
        BirthDate = u.BirthDate,
        Gender = u.Gender,
        Email = u.Email,
        AddressCount = await Addresses(u.Id).CountAsync(ct),
        ProfileCompleted = !string.IsNullOrWhiteSpace(u.FirstName)
    };

    private static void Apply(ShopUserAddress a, SaveShopAddressRequest r)
    {
        var label = r.Label.Trim();
        var line = r.AddressLine.Trim();
        if (label.Length == 0) throw new ValidationException("Manzil nomini kiriting.");
        if (line.Length == 0) throw new ValidationException("Manzilni kiriting.");
        if ((r.Latitude is null) != (r.Longitude is null))
            throw new ValidationException("Koordinata to'liq emas.");

        a.Label = label;
        a.AddressLine = line;
        a.Entrance = NullIfEmpty(r.Entrance);
        a.Floor = NullIfEmpty(r.Floor);
        a.Apartment = NullIfEmpty(r.Apartment);
        a.Intercom = NullIfEmpty(r.Intercom);
        a.Landmark = NullIfEmpty(r.Landmark);
        a.Comment = NullIfEmpty(r.Comment);
        a.Latitude = r.Latitude;
        a.Longitude = r.Longitude;
    }

    private static ShopAddressDto ToDto(ShopUserAddress a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        AddressLine = a.AddressLine,
        Entrance = a.Entrance,
        Floor = a.Floor,
        Apartment = a.Apartment,
        Intercom = a.Intercom,
        Landmark = a.Landmark,
        Comment = a.Comment,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        IsDefault = a.IsDefault
    };

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
