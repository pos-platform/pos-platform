using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Audit.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Audit;

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

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var take = query.Take is < 1 or > 2000 ? 200 : query.Take;

        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.From is { } from) { var f = AsUtc(from); q = q.Where(a => a.CreatedAt >= f); }
        if (query.To is { } to) { var t = AsUtc(to); q = q.Where(a => a.CreatedAt <= t); }
        if (query.Success is { } ok) q = q.Where(a => a.Success == ok);
        if (query.OrganizationId is { } orgId) q = q.Where(a => a.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            q = q.Where(a => a.Action.StartsWith(action));
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var u = query.Username.Trim().ToLower();
            q = q.Where(a => a.ActorUsername != null && a.ActorUsername.ToLower().Contains(u));
        }

        if (!string.IsNullOrWhiteSpace(query.Ip))
        {
            var ip = query.Ip.Trim();
            q = q.Where(a => a.IpAddress != null && a.IpAddress.Contains(ip));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(a =>
                a.Action.ToLower().Contains(s) ||
                (a.Detail != null && a.Detail.ToLower().Contains(s)) ||
                (a.ActorUsername != null && a.ActorUsername.ToLower().Contains(s)));
        }

        var logs = await q
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return logs.Select(AuditLogDto.FromEntity).ToList();
    }

    public async Task<IReadOnlyList<FiscalFailureDto>> GetFiscalFailuresAsync(FiscalFailureQuery query, CancellationToken ct = default)
    {
        var take = query.Take is < 1 or > 1000 ? 100 : query.Take;

        // SuperAdmin (OrganizationId == null) — tenant filtri o'chadi, barcha tashkilotlar ko'rinadi.
        // Butunlay to'xtaganlar (Failed) va hali avto-qayta urinilayotganlar (Queued + xato matni bor).
        var q = _db.Receipts
            .AsNoTracking()
            .Where(r => r.Sale != null &&
                (r.FiscalStatus == FiscalStatus.Failed ||
                 (r.FiscalStatus == FiscalStatus.Queued && r.ErrorMessage != null)));

        if (query.From is { } from) { var f = AsUtc(from); q = q.Where(r => (r.UpdatedAt ?? r.CreatedAt) >= f); }
        if (query.To is { } to) { var t = AsUtc(to); q = q.Where(r => (r.UpdatedAt ?? r.CreatedAt) <= t); }

        if (string.Equals(query.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            q = q.Where(r => r.FiscalStatus == FiscalStatus.Failed);
        else if (string.Equals(query.Status, "Retrying", StringComparison.OrdinalIgnoreCase))
            q = q.Where(r => r.FiscalStatus == FiscalStatus.Queued);

        if (query.OrganizationId is { } orgId) q = q.Where(r => r.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(query.TerminalId))
        {
            var term = query.TerminalId.Trim();
            q = q.Where(r => r.Sale!.TerminalId == term);
        }

        if (query.MinAmount is { } min) q = q.Where(r => r.Sale!.TotalAmount >= min);
        if (query.MaxAmount is { } max) q = q.Where(r => r.Sale!.TotalAmount <= max);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(r =>
                r.Sale!.ReceiptNumber.ToLower().Contains(s) ||
                (r.ErrorMessage != null && r.ErrorMessage.ToLower().Contains(s)));
        }

        var failures = await q
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

    public async Task<FiscalFilterOptionsDto> GetFiscalFilterOptionsAsync(CancellationToken ct = default)
    {
        // Filtr ro'yxatlari filtrdan qat'i nazar to'liq bo'lishi uchun alohida (filtrsiz) so'rov.
        var rows = await _db.Receipts
            .AsNoTracking()
            .Where(r => r.Sale != null &&
                (r.FiscalStatus == FiscalStatus.Failed ||
                 (r.FiscalStatus == FiscalStatus.Queued && r.ErrorMessage != null)))
            .Select(r => new { r.OrganizationId, r.Sale!.TerminalId })
            .Distinct()
            .ToListAsync(ct);

        var orgIds = rows.Where(r => r.OrganizationId != null).Select(r => r.OrganizationId!.Value).Distinct().ToList();
        var orgs = await _db.Organizations
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationOptionDto { Id = o.Id, Name = o.Name })
            .ToListAsync(ct);

        var terminals = rows
            .Select(r => r.TerminalId)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return new FiscalFilterOptionsDto { Organizations = orgs, Terminals = terminals };
    }

    private static DateTime AsUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
