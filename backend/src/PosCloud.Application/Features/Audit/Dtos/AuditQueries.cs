namespace PosCloud.Application.Features.Audit.Dtos;

/// <summary>Audit jurnali uchun filtrlar (hammasi ixtiyoriy).</summary>
public class AuditLogQuery
{
    public int Take { get; set; } = 200;

    /// <summary>Boshlanish vaqti (UTC, shu vaqtdan keyingi yozuvlar).</summary>
    public DateTime? From { get; set; }

    /// <summary>Tugash vaqti (UTC, shu vaqtgacha bo'lgan yozuvlar).</summary>
    public DateTime? To { get; set; }

    /// <summary>Hodisa turi prefiksi: login, license, user, org, password yoki to'liq action.</summary>
    public string? Action { get; set; }

    /// <summary>true — faqat muvaffaqiyatli, false — faqat xatolar, null — hammasi.</summary>
    public bool? Success { get; set; }

    /// <summary>Foydalanuvchi nomi (qisman moslik).</summary>
    public string? Username { get; set; }

    /// <summary>IP manzil (qisman moslik).</summary>
    public string? Ip { get; set; }

    /// <summary>Tafsilot/hodisa matni bo'yicha qidiruv.</summary>
    public string? Search { get; set; }

    public Guid? OrganizationId { get; set; }
}

/// <summary>Fiskal audit uchun filtrlar (hammasi ixtiyoriy).</summary>
public class FiscalFailureQuery
{
    public int Take { get; set; } = 100;

    /// <summary>Boshlanish vaqti (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Tugash vaqti (UTC).</summary>
    public DateTime? To { get; set; }

    /// <summary>"Failed" | "Retrying" | null (hammasi).</summary>
    public string? Status { get; set; }

    public Guid? OrganizationId { get; set; }

    /// <summary>Kassa (terminal) identifikatori.</summary>
    public string? TerminalId { get; set; }

    /// <summary>Chek raqami yoki xato matni bo'yicha qidiruv.</summary>
    public string? Search { get; set; }

    /// <summary>Minimal summa.</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Maksimal summa.</summary>
    public decimal? MaxAmount { get; set; }
}
