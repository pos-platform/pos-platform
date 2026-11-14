using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Courier;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Background;

/// <summary>Davriy vazifa asosi (bosqich 10): har intervalda o'z DI scope'ida; xato keyingi aylanishni to'xtatmaydi va SuperAdmin'ga yoziladi.</summary>
public abstract class PeriodicScopedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger Logger;

    protected PeriodicScopedService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        Logger = logger;
    }

    protected abstract TimeSpan Interval { get; }
    protected virtual TimeSpan StartDelay => TimeSpan.FromSeconds(40);
    protected abstract string Name { get; }
    protected abstract Task<int> RunAsync(IServiceProvider sp, DateTime nowUtc, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var count = await RunAsync(scope.ServiceProvider, DateTime.UtcNow, stoppingToken);
                if (count > 0) Logger.LogInformation("{Name}: {Count}", Name, count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "{Name} vazifasida xato", Name);
                BackgroundErrors.Report(_scopeFactory, Name, ex);
            }
        } while (await WaitNext(timer, stoppingToken));
    }

    private static async Task<bool> WaitNext(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// Har daqiqa: vaqti kelgan (ScheduledFor ≤ hozir) va hali "Yangi" oldindan buyurtmalar uchun filial guruhiga
/// SignalR <c>OrderCreated</c> qayta yuboriladi (kassada ovoz), PreorderNotifiedAt qo'yiladi (shartli UPDATE — bir marta).
/// </summary>
public class PreorderNotifyService : PeriodicScopedService
{
    public PreorderNotifyService(IServiceScopeFactory scopeFactory, ILogger<PreorderNotifyService> logger)
        : base(scopeFactory, logger) { }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);
    protected override string Name => "Oldindan buyurtmalar kassaga yuborildi";

    protected override async Task<int> RunAsync(IServiceProvider sp, DateTime nowUtc, CancellationToken ct)
    {
        var db = sp.GetRequiredService<IApplicationDbContext>();
        var notifier = sp.GetRequiredService<IOnlineOrderNotifier>();

        var ids = await db.OnlineOrders.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.IsPreorder && o.Status == OnlineOrderStatus.New && o.PreorderNotifiedAt == null
                        && o.ScheduledFor != null && o.ScheduledFor <= nowUtc)
            .OrderBy(o => o.ScheduledFor)
            .Select(o => o.Id)
            .Take(200)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var id in ids)
        {
            var affected = await db.OnlineOrders.IgnoreQueryFilters()
                .Where(o => o.Id == id && o.PreorderNotifiedAt == null && o.Status == OnlineOrderStatus.New)
                .ExecuteUpdateAsync(u => u.SetProperty(o => o.PreorderNotifiedAt, nowUtc), ct);
            if (affected == 0) continue;

            var order = await db.OnlineOrders.IgnoreQueryFilters().AsNoTracking().FirstAsync(o => o.Id == id, ct);
            await notifier.OrderChangedAsync(order, isNew: true, pushToCustomer: false, ct);
            sent++;
        }
        return sent;
    }
}

/// <summary>Har 15 soniya: muddati o'tgan kuryer takliflari Expired; kutilayotgani qolmagan buyurtma — NoCourier.</summary>
public class CourierOfferExpiryService : PeriodicScopedService
{
    public CourierOfferExpiryService(IServiceScopeFactory scopeFactory, ILogger<CourierOfferExpiryService> logger)
        : base(scopeFactory, logger) { }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(15);
    protected override TimeSpan StartDelay => TimeSpan.FromSeconds(20);
    protected override string Name => "Kuryer takliflari muddati tugadi";

    protected override Task<int> RunAsync(IServiceProvider sp, DateTime nowUtc, CancellationToken ct) =>
        sp.GetRequiredService<ICourierOfferService>().ExpireDueAsync(nowUtc, ct);
}
