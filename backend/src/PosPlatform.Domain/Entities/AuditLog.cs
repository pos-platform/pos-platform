using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Xavfsizlik/audit jurnali — muhim hodisalar (login, obuna o'zgarishlari, bloklash).
/// Ko'p-ijarachilik filtridan ozod (SuperAdmin barcha tashkilotlar bo'yicha ko'radi);
/// OrganizationId faqat ma'lumot uchun to'ldiriladi.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>Hodisa turi, masalan: login.success, login.fail, license.extend, user.deactivate.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Qo'shimcha tafsilot (target user, sabab, va h.k.).</summary>
    public string? Detail { get; set; }

    public bool Success { get; set; } = true;

    /// <summary>Amalni bajargan foydalanuvchi (mavjud bo'lsa).</summary>
    public Guid? ActorUserId { get; set; }

    public string? ActorUsername { get; set; }

    public string? IpAddress { get; set; }
}
