using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Infrastructure.Persistence;

namespace PosCloud.Infrastructure.Diagnostics;

/// <summary>
/// SystemError yozuvchi (singleton). Har yozish — alohida scope va DbContext (asosiy so'rov tranzaksiyasiga aralashmaydi),
/// har qanday xato yutiladi va faqat logga tushadi. Bir xil fingerprint bitta qatorga: shartli UPDATE (Count + 1,
/// LastSeenAt, hal qilingan bo'lsa qayta ochiladi); qator yo'q bo'lsa INSERT (unikal indeks poygasida — qayta UPDATE).
/// </summary>
public sealed class SystemErrorRecorder : ISystemErrorRecorder
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemErrorRecorder> _logger;

    public SystemErrorRecorder(IServiceScopeFactory scopeFactory, ILogger<SystemErrorRecorder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Record(SystemErrorEntry entry) =>
        _ = Task.Run(() => RecordAsync(entry, CancellationToken.None));

    public async Task RecordAsync(SystemErrorEntry entry, CancellationToken ct = default)
    {
        try
        {
            var message = SystemErrorText.Scrub(entry.Message, SystemErrorText.MaxMessage);
            if (string.IsNullOrWhiteSpace(message)) message = "(xabarsiz xato)";
            var details = SystemErrorText.Scrub(entry.Details, SystemErrorText.MaxDetails);
            var location = entry.Location ?? SystemErrorText.LocationFromDetails(details);
            var fingerprint = SystemErrorText.Fingerprint(entry.Source, entry.ErrorType, message, location);
            var path = SystemErrorText.CleanPath(entry.Path);
            var userName = Cut(entry.UserName, 100);
            var appVersion = Cut(SystemErrorText.Scrub(entry.AppVersion, 50), 50);
            var device = Cut(SystemErrorText.Scrub(entry.Device, 200), 200);
            var requestId = Cut(entry.RequestId, 100);
            var method = Cut(entry.Method, 10);
            var now = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            string? orgName = null;
            if (entry.OrganizationId is { } orgId)
                orgName = Cut(await db.Organizations.AsNoTracking().Where(o => o.Id == orgId)
                    .Select(o => o.Name).FirstOrDefaultAsync(ct), 200);

            Task<int> Update() => db.SystemErrors
                .Where(e => e.Fingerprint == fingerprint)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Count, e => e.Count + 1)
                    .SetProperty(e => e.LastSeenAt, now)
                    .SetProperty(e => e.ResolvedAt, (DateTime?)null)
                    .SetProperty(e => e.ResolvedBy, (string?)null)
                    .SetProperty(e => e.Level, e => e.Level > entry.Level ? e.Level : entry.Level)
                    .SetProperty(e => e.Message, message)
                    .SetProperty(e => e.Details, details)
                    .SetProperty(e => e.Path, path)
                    .SetProperty(e => e.Method, method)
                    .SetProperty(e => e.StatusCode, entry.StatusCode)
                    .SetProperty(e => e.OrganizationId, entry.OrganizationId)
                    .SetProperty(e => e.OrganizationName, orgName)
                    .SetProperty(e => e.BranchId, entry.BranchId)
                    .SetProperty(e => e.UserId, entry.UserId)
                    .SetProperty(e => e.UserName, userName)
                    .SetProperty(e => e.AppVersion, appVersion)
                    .SetProperty(e => e.Device, device)
                    .SetProperty(e => e.RequestId, requestId), ct);

            if (await Update() > 0) return;

            db.SystemErrors.Add(new SystemError
            {
                Fingerprint = fingerprint,
                Source = entry.Source,
                Level = entry.Level,
                Message = message,
                Details = details,
                Path = path,
                Method = method,
                StatusCode = entry.StatusCode,
                OrganizationId = entry.OrganizationId,
                OrganizationName = orgName,
                BranchId = entry.BranchId,
                UserId = entry.UserId,
                UserName = userName,
                AppVersion = appVersion,
                Device = device,
                RequestId = requestId,
                FirstSeenAt = now,
                LastSeenAt = now,
                Count = 1
            });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Parallel so'rov shu fingerprint'ni birinchi yozdi — hisoblagichni oshiramiz.
                db.ChangeTracker.Clear();
                await Update();
            }
        }
        catch (Exception ex)
        {
            // Xatolikni yozish xatosi hech qachon asosiy jarayonni buzmaydi.
            _logger.LogWarning(ex, "SystemError yozilmadi");
        }
    }

    private static string? Cut(string? s, int max) => s is not null && s.Length > max ? s[..max] : s;
}
