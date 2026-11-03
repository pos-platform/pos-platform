namespace PosCloud.Application.Features.Audit.Dtos;

/// <summary>
/// Fiskalizatsiya muvaffaqiyatsiz tugagan chek (FiscalStatus = Failed) — audit sahifasining
/// pastki bo'limi uchun. SuperAdmin barcha tashkilotlar bo'yicha ko'radi.
/// </summary>
public class FiscalFailureDto
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? TerminalId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? OrganizationName { get; set; }

    /// <summary>Fiskal moduldan qaytgan aniq xato matni (receipt time / SEND_ALL / applet / MXIK ...).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>"Retrying" — hali navbatda avto-qayta urinilmoqda; "Failed" — butunlay to'xtagan.</summary>
    public string Status { get; set; } = "Failed";

    /// <summary>Nechanchi marta qayta urinilgani.</summary>
    public int RetryCount { get; set; }

    /// <summary>Savdo yaratilgan vaqt.</summary>
    public DateTime SoldAt { get; set; }

    /// <summary>Xato oxirgi marta yozilgan vaqt (chek yangilangan vaqti).</summary>
    public DateTime FailedAt { get; set; }
}
