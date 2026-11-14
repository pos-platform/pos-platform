using System.Threading.Channels;
using PosCloud.Application.Features.ShopPush;
using PosCloud.Application.Features.ShopPush.Dtos;

namespace PosCloud.Api.Background;

/// <summary>
/// Buyurtma push'lari navbati (chegaralangan). So'rov ichida faqat navbatga qo'yiladi —
/// FCM'ga yuborish <see cref="OrderPushService"/> fon vazifasida, o'z DI scope'ida.
/// Navbat to'lsa yangi vazifa tashlab yuboriladi (logga yoziladi) — asosiy amal hech qachon kutmaydi.
/// </summary>
public class OrderPushQueue
{
    public const int Capacity = 1000;

    private readonly Channel<OrderStatusPush> _channel = Channel.CreateBounded<OrderStatusPush>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly ILogger<OrderPushQueue> _logger;

    public OrderPushQueue(ILogger<OrderPushQueue> logger)
    {
        _logger = logger;
    }

    public void Enqueue(OrderStatusPush push)
    {
        if (!_channel.Writer.TryWrite(push))
            _logger.LogWarning("Push navbati to'la, xabar tashlab yuborildi: {OrderId} {Status}", push.OrderId, push.Status);
    }

    public ChannelReader<OrderStatusPush> Reader => _channel.Reader;
}

/// <summary>Navbatdagi buyurtma push'larini ketma-ket yuboradi.</summary>
public class OrderPushService : BackgroundService
{
    private readonly OrderPushQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPushService> _logger;

    public OrderPushService(OrderPushQueue queue, IServiceScopeFactory scopeFactory, ILogger<OrderPushService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var push in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IShopPushService>();
                    await service.SendOrderStatusAsync(push, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Buyurtma push'i yuborilmadi: {OrderId}", push.OrderId);
                    BackgroundErrors.Report(_scopeFactory, "OrderPush", ex, level: PosCloud.Domain.Enums.SystemErrorLevel.Warning);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ilova to'xtatilmoqda.
        }
    }
}
