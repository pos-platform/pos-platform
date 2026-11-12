using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Loyalty;

namespace PosCloud.Api.Background;

/// <summary>
/// Kuniga bir marta: pointsExpireDays sozlangan tashkilotlarda oxirgi ball harakati N kundan eski mijozlar ballini yondiradi
/// (Expire tranzaksiya, balans 0). Idempotent — qayta ishga tushsa, yondirilgan mijozlar qayta tanlanmaydi.
/// </summary>
public class LoyaltyExpiryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoyaltyExpiryService> _logger;

    public LoyaltyExpiryService(IServiceScopeFactory scopeFactory, ILogger<LoyaltyExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

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
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bonus ball muddatini tekshirishda xato");
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

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var expired = await LoyaltyExpiry.RunAsync(db, DateTime.UtcNow, ct);
        if (expired > 0)
            _logger.LogInformation("Bonus ball muddati tugadi: {Count} ta mijoz", expired);
    }
}
