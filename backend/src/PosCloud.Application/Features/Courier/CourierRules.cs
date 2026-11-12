using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Courier;

/// <summary>Kuryer akkaunti qoidalari (foydalanuvchi yaratish/tahrirlashda — admin va SuperAdmin oqimlari uchun bitta joyda).</summary>
public static class CourierRules
{
    /// <summary>Kuryer uchun telefon va filial majburiy; filial shu tashkilotniki bo'lishi kerak.</summary>
    public static async Task ValidateAsync(IApplicationDbContext db, UserRole role, string? phone, Guid? branchId,
        Guid? organizationId, CancellationToken ct)
    {
        if (role != UserRole.Courier) return;

        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("Kuryer uchun telefon raqamini kiriting.");
        if (branchId is not { } b)
            throw new ValidationException("Kuryer uchun filialni tanlang.");

        var exists = await db.Branches.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == b && (organizationId == null || x.OrganizationId == organizationId), ct);
        if (!exists)
            throw new ValidationException("Filial topilmadi.");
    }

    /// <summary>Kuryer ismi (chekda/mijozga ko'rsatish uchun): to'liq ism, bo'lmasa login.</summary>
    public static string DisplayName(Domain.Entities.User u) =>
        string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName.Trim();
}
