using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Audit.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Audit;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string? detail = null,
        bool success = true,
        Guid? organizationId = null,
        Guid? actorUserId = null,
        string? actorUsername = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLog
        {
            Action = action,
            Detail = detail,
            Success = success,
            OrganizationId = organizationId,
            ActorUserId = actorUserId ?? _currentUser.UserId,
            ActorUsername = actorUsername ?? _currentUser.Username,
            IpAddress = _currentUser.IpAddress
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take = 200, CancellationToken ct = default)
    {
        if (take is < 1 or > 1000) take = 200;
        var logs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return logs.Select(AuditLogDto.FromEntity).ToList();
    }

    public async Task<IReadOnlyList<FiscalFailureDto>> GetFiscalFailuresAsync(int take = 100, CancellationToken ct = default)
    {
        if (take is < 1 or > 500) take = 100;

        // SuperAdmin (OrganizationId == null) — tenant filtri o'chadi, barcha tashkilotlar ko'rinadi.
        // Butunlay to'xtaganlar (Failed) va hali avto-qayta urinilayotganlar (Queued + xato matni bor).
        // Tashkilot nomi Receipt.OrganizationId bo'yicha alohida so'rov bilan biriktiriladi.
        var failures = await _db.Receipts
            .AsNoTracking()
            .Where(r => r.Sale != null &&
                (r.FiscalStatus == FiscalStatus.Failed ||
                 (r.FiscalStatus == FiscalStatus.Queued && r.ErrorMessage != null)))
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Take(take)
            .Select(r => new
            {
                r.SaleId,
                r.Sale!.ReceiptNumber,
                r.Sale.TerminalId,
                r.Sale.TotalAmount,
                r.OrganizationId,
                r.ErrorMessage,
                r.FiscalStatus,
                r.FiscalRetryCount,
                SoldAt = r.Sale.CreatedAt,
                FailedAt = r.UpdatedAt ?? r.CreatedAt
            })
            .ToListAsync(ct);

        var orgIds = failures.Where(f => f.OrganizationId != null).Select(f => f.OrganizationId!.Value).Distinct().ToList();
        var orgNames = await _db.Organizations
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Name, ct);

        return failures.Select(f => new FiscalFailureDto
        {
            SaleId = f.SaleId,
            ReceiptNumber = f.ReceiptNumber,
            TerminalId = f.TerminalId,
            TotalAmount = f.TotalAmount,
            OrganizationName = f.OrganizationId != null && orgNames.TryGetValue(f.OrganizationId.Value, out var name) ? name : null,
            ErrorMessage = f.ErrorMessage,
            Status = f.FiscalStatus == FiscalStatus.Queued ? "Retrying" : "Failed",
            RetryCount = f.FiscalRetryCount,
            SoldAt = f.SoldAt,
            FailedAt = f.FailedAt
        }).ToList();
    }
}
