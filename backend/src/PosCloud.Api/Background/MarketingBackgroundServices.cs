using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineMarketing;
using PosCloud.Application.Features.ShopPush;

namespace PosCloud.Api.Background;

/// <summary>
/// Davriy marketing vazifasi uchun asos: kechikish bilan boshlanadi, har intervalda o'z DI scope'ida ishlaydi,
/// xato keyingi aylanishni to'xtatmaydi.
/// </summary>
public abstract class PeriodicMarketingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    protected readonly ILogger Logger;

    protected PeriodicMarketingService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        Logger = logger;
    }

    protected abstract TimeSpan Interval { get; }
    protected virtual TimeSpan StartDelay => TimeSpan.FromMinutes(1);
    protected abstract string Name { get; }

    protected abstract Task<int> RunAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc,
        CancellationToken ct);

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
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var push = scope.ServiceProvider.GetRequiredService<IShopPushService>();
                var count = await RunAsync(db, push, DateTime.UtcNow, stoppingToken);
                if (count > 0) Logger.LogInformation("{Name}: {Count}", Name, count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "{Name} vazifasida xato", Name);
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

/// <summary>Rejalashtirilgan va "Hozir yuborish" push kampaniyalari (har 30 soniya).</summary>
public class PushCampaignService : PeriodicMarketingService
{
    public PushCampaignService(IServiceScopeFactory scopeFactory, ILogger<PushCampaignService> logger)
        : base(scopeFactory, logger) { }

    protected override TimeSpan Interval => TimeSpan.FromSeconds(30);
    protected override TimeSpan StartDelay => TimeSpan.FromSeconds(30);
    protected override string Name => "Push kampaniyalar yuborildi";

    protected override Task<int> RunAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc, CancellationToken ct) =>
        MarketingJobs.SendDueCampaignsAsync(db, push, nowUtc, ct);
}

/// <summary>"Kelganda xabar berish" — qoldiq paydo bo'lsa push (har 10 daqiqa).</summary>
public class BackInStockService : PeriodicMarketingService
{
    public BackInStockService(IServiceScopeFactory scopeFactory, ILogger<BackInStockService> logger)
        : base(scopeFactory, logger) { }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(10);
    protected override string Name => "Yana sotuvda xabarlari";

    protected override Task<int> RunAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc, CancellationToken ct) =>
        MarketingJobs.NotifyBackInStockAsync(db, push, nowUtc, ct);
}

/// <summary>Tashlab ketilgan savatcha eslatmasi (har 15 daqiqa, 09:00–21:00 UTC+5).</summary>
public class CartReminderService : PeriodicMarketingService
{
    public CartReminderService(IServiceScopeFactory scopeFactory, ILogger<CartReminderService> logger)
        : base(scopeFactory, logger) { }

    protected override TimeSpan Interval => TimeSpan.FromMinutes(15);
    protected override TimeSpan StartDelay => TimeSpan.FromMinutes(3);
    protected override string Name => "Savatcha eslatmalari";

    protected override Task<int> RunAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc, CancellationToken ct) =>
        MarketingJobs.RemindCartsAsync(db, push, nowUtc, ct);
}
