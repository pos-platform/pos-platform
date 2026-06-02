using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Audit.Dtos;
using PosPlatform.Domain.Entities;

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
}
