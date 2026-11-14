using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Tizim xatoligi (SuperAdmin ko'radi). Global — tenant filtri yo'q (BaseEntity emas).
/// Bir xil Fingerprint bitta qatorga guruhlanadi: Count++, LastSeenAt yangilanadi; hal qilingandan keyin
/// qayta chiqsa — qayta ochiladi (ResolvedAt = null). Sezgir ma'lumot (token, parol, karta) yozilmaydi.
/// </summary>
public class SystemError
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>source + tur + xabar shabloni + joy xeshi (hex).</summary>
    public string Fingerprint { get; set; } = string.Empty;

    public SystemErrorSource Source { get; set; }
    public SystemErrorLevel Level { get; set; } = SystemErrorLevel.Error;

    /// <summary>Xabar (≤1000).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Stack trace / tafsilot (≤16000).</summary>
    public string? Details { get; set; }

    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? AppVersion { get; set; }
    public string? Device { get; set; }
    public string? RequestId { get; set; }

    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public int Count { get; set; } = 1;

    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}
