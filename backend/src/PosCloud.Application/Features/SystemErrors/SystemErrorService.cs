using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.SystemErrors;

// Bosqich 10.4: SuperAdmin "Xatoliklar" sahifasi.

public class SystemErrorQuery
{
    public SystemErrorSource? Source { get; set; }
    public SystemErrorLevel? Level { get; set; }
    public Guid? OrganizationId { get; set; }

    /// <summary>open (standart) | resolved | all.</summary>
    public string? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SystemErrorListItemDto
{
    public Guid Id { get; set; }
    public SystemErrorSource Source { get; set; }
    public SystemErrorLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public int Count { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class SystemErrorListDto
{
    public List<SystemErrorListItemDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int OpenCount { get; set; }
}

public class SystemErrorDto : SystemErrorListItemDto
{
    public string Fingerprint { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? AppVersion { get; set; }
    public string? Device { get; set; }
    public string? RequestId { get; set; }
    public string? ResolvedBy { get; set; }
}

public class SystemErrorSummaryDto
{
    public int OpenCount { get; set; }

    /// <summary>Oxirgi 24 soatda takrorlangan (LastSeenAt) xatolar soni.</summary>
    public int Last24h { get; set; }

    /// <summary>Ochiq xatolar manba bo'yicha: { "Api": n, "Background": n, ... }.</summary>
    public Dictionary<string, int> BySource { get; set; } = new();
}

public class ResolveErrorsRequest
{
    [MaxLength(500)]
    public List<Guid> Ids { get; set; } = [];
}

public class ResolveErrorsResultDto
{
    public int Resolved { get; set; }
}

/// <summary>POST /api/client-errors body.</summary>
public class ClientErrorRequest
{
    public SystemErrorSource Source { get; set; } = SystemErrorSource.Desktop;
    public SystemErrorLevel Level { get; set; } = SystemErrorLevel.Error;

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(32000)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string? AppVersion { get; set; }

    [MaxLength(200)]
    public string? Device { get; set; }

    /// <summary>Qo'shimcha kontekst (ekran, amal...) — Details oxiriga tozalangan holda qo'shiladi.</summary>
    public Dictionary<string, object?>? Context { get; set; }
}

public interface ISystemErrorService
{
    Task<SystemErrorListDto> GetListAsync(SystemErrorQuery query, CancellationToken ct = default);
    Task<SystemErrorDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<SystemErrorDto> ResolveAsync(Guid id, CancellationToken ct = default);
    Task<ResolveErrorsResultDto> ResolveBulkAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<SystemErrorSummaryDto> GetSummaryAsync(CancellationToken ct = default);

}

/// <summary>SystemError global (tenant filtrisiz) — faqat SuperAdmin controller orqali ochiladi.</summary>
public class SystemErrorService : ISystemErrorService
{
    public const int ResolvedKeepDays = 30;
    public const int OpenKeepDays = 90;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SystemErrorService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SystemErrorListDto> GetListAsync(SystemErrorQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize <= 0 ? 50 : q.PageSize, 1, 200);
        var query = _db.SystemErrors.AsNoTracking();

        if (q.Source is { } source) query = query.Where(e => e.Source == source);
        if (q.Level is { } level) query = query.Where(e => e.Level == level);
        if (q.OrganizationId is { } orgId) query = query.Where(e => e.OrganizationId == orgId);

        var status = (q.Status ?? "open").Trim().ToLowerInvariant();
        if (status == "open") query = query.Where(e => e.ResolvedAt == null);
        else if (status == "resolved") query = query.Where(e => e.ResolvedAt != null);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(e => e.Message.ToLower().Contains(s)
                                     || (e.Path != null && e.Path.ToLower().Contains(s))
                                     || (e.OrganizationName != null && e.OrganizationName.ToLower().Contains(s)));
        }
        // Sana — LastSeenAt bo'yicha, UTC kalendar kuni; "to" kuni ham kiradi.
        if (q.From is { } from)
        {
            var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
            query = query.Where(e => e.LastSeenAt >= fromUtc);
        }
        if (q.To is { } to)
        {
            var toUtc = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(e => e.LastSeenAt < toUtc);
        }

        return new SystemErrorListDto
        {
            Total = await query.CountAsync(ct),
            OpenCount = await _db.SystemErrors.CountAsync(e => e.ResolvedAt == null, ct),
            Items = await query
                .OrderByDescending(e => e.LastSeenAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(e => new SystemErrorListItemDto
                {
                    Id = e.Id,
                    Source = e.Source,
                    Level = e.Level,
                    Message = e.Message,
                    OrganizationName = e.OrganizationName,
                    Count = e.Count,
                    FirstSeenAt = e.FirstSeenAt,
                    LastSeenAt = e.LastSeenAt,
                    ResolvedAt = e.ResolvedAt
                })
                .ToListAsync(ct)
        };
    }

    public async Task<SystemErrorDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.SystemErrors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Xatolik topilmadi.");
        return ToDto(e);
    }

    public async Task<SystemErrorDto> ResolveAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.SystemErrors.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Xatolik topilmadi.");
        if (e.ResolvedAt is null)
        {
            e.ResolvedAt = DateTime.UtcNow;
            e.ResolvedBy = Cut(_currentUser.Username, 100);
            await _db.SaveChangesAsync(ct);
        }
        return ToDto(e);
    }

    public async Task<ResolveErrorsResultDto> ResolveBulkAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new ResolveErrorsResultDto();
        if (ids.Count > 500) throw new Common.Exceptions.ValidationException("Bir vaqtda ko'pi bilan 500 ta.");
        var list = ids.Distinct().ToList();
        var now = DateTime.UtcNow;
        var by = Cut(_currentUser.Username, 100);
        var rows = await _db.SystemErrors.Where(e => list.Contains(e.Id) && e.ResolvedAt == null).ToListAsync(ct);
        foreach (var e in rows)
        {
            e.ResolvedAt = now;
            e.ResolvedBy = by;
        }
        await _db.SaveChangesAsync(ct);
        return new ResolveErrorsResultDto { Resolved = rows.Count };
    }

    public async Task<SystemErrorSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var bySource = await _db.SystemErrors.AsNoTracking()
            .Where(e => e.ResolvedAt == null)
            .GroupBy(e => e.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new SystemErrorSummaryDto
        {
            OpenCount = bySource.Sum(x => x.Count),
            Last24h = await _db.SystemErrors.CountAsync(e => e.LastSeenAt >= since, ct),
            BySource = Enum.GetValues<SystemErrorSource>()
                .ToDictionary(s => s.ToString(), s => bySource.FirstOrDefault(x => x.Source == s)?.Count ?? 0)
        };
    }

    private static SystemErrorDto ToDto(SystemError e) => new()
    {
        Id = e.Id,
        Fingerprint = e.Fingerprint,
        Source = e.Source,
        Level = e.Level,
        Message = e.Message,
        Details = e.Details,
        Path = e.Path,
        Method = e.Method,
        StatusCode = e.StatusCode,
        OrganizationId = e.OrganizationId,
        OrganizationName = e.OrganizationName,
        BranchId = e.BranchId,
        UserId = e.UserId,
        UserName = e.UserName,
        AppVersion = e.AppVersion,
        Device = e.Device,
        RequestId = e.RequestId,
        Count = e.Count,
        FirstSeenAt = e.FirstSeenAt,
        LastSeenAt = e.LastSeenAt,
        ResolvedAt = e.ResolvedAt,
        ResolvedBy = e.ResolvedBy
    };

    private static string? Cut(string? s, int max) => s is not null && s.Length > max ? s[..max] : s;
}
