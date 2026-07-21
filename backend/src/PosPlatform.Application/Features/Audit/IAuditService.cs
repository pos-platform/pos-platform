using PosPlatform.Application.Features.Audit.Dtos;

namespace PosPlatform.Application.Features.Audit;

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

    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 200, CancellationToken ct = default);

    /// <summary>Fiskalizatsiya muvaffaqiyatsiz tugagan cheklar (FiscalStatus = Failed), eng yangisi birinchi.</summary>
    Task<IReadOnlyList<FiscalFailureDto>> GetFiscalFailuresAsync(int take = 100, CancellationToken ct = default);
}
