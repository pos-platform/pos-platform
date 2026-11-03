using PosCloud.Application.Features.Audit.Dtos;

namespace PosCloud.Application.Features.Audit;

/// <summary>Xavfsizlik/audit hodisalarini yozadi va o'qiydi.</summary>
public interface IAuditService
{
    /// <summary>Hodisani jurnalga yozadi. Aktor/IP joriy foydalanuvchidan olinadi (yoki override qilinadi).</summary>
    Task LogAsync(
        string action,
        string? detail = null,
        bool success = true,
        Guid? organizationId = null,
        Guid? actorUserId = null,
        string? actorUsername = null,
        CancellationToken ct = default);

    /// <summary>Audit yozuvlari — vaqt oraligi, hodisa turi, foydalanuvchi va matn bo'yicha filtrlanadi.</summary>
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(AuditLogQuery query, CancellationToken ct = default);

    /// <summary>Fiskalizatsiya muvaffaqiyatsiz tugagan cheklar (FiscalStatus = Failed), eng yangisi birinchi.</summary>
    Task<IReadOnlyList<FiscalFailureDto>> GetFiscalFailuresAsync(FiscalFailureQuery query, CancellationToken ct = default);

    /// <summary>Fiskal audit filtri uchun tashkilot/kassa ro'yxatlari.</summary>
    Task<FiscalFilterOptionsDto> GetFiscalFilterOptionsAsync(CancellationToken ct = default);
}
