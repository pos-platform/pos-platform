using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Background;

/// <summary>
/// Har daqiqada: do'kon tashkilot sozlamasidagi <c>AutoCancelMinutes</c> (&gt;0) ichida qabul qilmagan "Yangi"
/// buyurtmalarni avtomatik bekor qiladi va mijozga xabar beradi. "Yangi"da ombor rezervi yo'q — faqat holat o'zgaradi.
/// Har buyurtma alohida, shartli UPDATE (Status = New) bilan — xodim shu payt qabul qilsa, uning amali ustun.
/// </summary>
public class OnlineOrderAutoCancelService : BackgroundService
{
    public const string Reason = "Do'kon vaqtida javob bermadi";

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(30);
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OnlineOrderAutoCancelService> _logger;

    public OnlineOrderAutoCancelService(IServiceScopeFactory scopeFactory, ILogger<OnlineOrderAutoCancelService> logger)
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
                _logger.LogWarning(ex, "Online buyurtmalarni avtomatik bekor qilishda xato");
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
        var notifier = scope.ServiceProvider.GetRequiredService<IOnlineOrderNotifier>();
        var now = DateTime.UtcNow;

        // Tashkilotlar soni kam — sozlamalarni xotiraga olamiz, har tashkilot uchun chegarani alohida hisoblaymiz.
        var rules = await db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.AutoCancelMinutes > 0 && s.OrganizationId != null)
            .Select(s => new { OrganizationId = s.OrganizationId!.Value, s.AutoCancelMinutes })
            .ToListAsync(ct);

        var cancelled = 0;
        foreach (var rule in rules)
        {
            var before = now.AddMinutes(-rule.AutoCancelMinutes);
            var ids = await db.OnlineOrders.IgnoreQueryFilters().AsNoTracking()
                .Where(o => o.OrganizationId == rule.OrganizationId
                            && o.Status == OnlineOrderStatus.New
                            && o.CreatedAt < before)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            foreach (var id in ids)
            {
                try
                {
                    var at = DateTime.UtcNow;
                    var affected = await db.OnlineOrders.IgnoreQueryFilters()
                        .Where(o => o.Id == id && o.Status == OnlineOrderStatus.New)
                        .ExecuteUpdateAsync(u => u
                            .SetProperty(o => o.Status, OnlineOrderStatus.Cancelled)
                            .SetProperty(o => o.CancelledAt, at)
                            .SetProperty(o => o.CancelReason, Reason)
                            .SetProperty(o => o.UpdatedAt, at), ct);
                    if (affected == 0) continue; // xodim ulgurdi

                    cancelled++;
                    var order = await db.OnlineOrders.IgnoreQueryFilters().AsNoTracking()
                        .FirstAsync(o => o.Id == id, ct);

                    // Promokod ishlatilishi qaytariladi.
                    if (order.PromoCodeId is { } promoId)
                        await db.PromoCodes.IgnoreQueryFilters()
                            .Where(p => p.Id == promoId && p.UsedCount > 0)
                            .ExecuteUpdateAsync(u => u.SetProperty(p => p.UsedCount, p => p.UsedCount - 1), ct);
                    await notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: true, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Buyurtma avtomatik bekor qilinmadi: {OrderId}", id);
                }
            }
        }

        if (cancelled > 0)
            _logger.LogInformation("Javobsiz qolgan {Count} ta online buyurtma avtomatik bekor qilindi", cancelled);
    }
}
