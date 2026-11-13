using System.Threading.Channels;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopPush;

namespace PosCloud.Api.Background;

/// <summary>
/// Mijozga umumiy push navbati (do'stni taklif mukofoti va h.k.). So'rov ichida faqat navbatga qo'yiladi,
/// FCM'ga <see cref="ShopPushDispatchService"/> fon vazifasida yuboriladi.
/// </summary>
public class ShopPushQueue : IShopPushQueue
{
    public const int Capacity = 1000;

    private readonly Channel<ShopUserPush> _channel = Channel.CreateBounded<ShopUserPush>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly ILogger<ShopPushQueue> _logger;

    public ShopPushQueue(ILogger<ShopPushQueue> logger)
    {
        _logger = logger;
    }

    public void Enqueue(ShopUserPush push)
    {
        if (!_channel.Writer.TryWrite(push))
            _logger.LogWarning("Mijoz push navbati to'la, xabar tashlab yuborildi: {ShopUserId}", push.ShopUserId);
    }

    public ChannelReader<ShopUserPush> Reader => _channel.Reader;
}

/// <summary>Navbatdagi mijoz push'larini ketma-ket yuboradi.</summary>
public class ShopPushDispatchService : BackgroundService
{
    private readonly ShopPushQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShopPushDispatchService> _logger;

    public ShopPushDispatchService(ShopPushQueue queue, IServiceScopeFactory scopeFactory,
        ILogger<ShopPushDispatchService> logger)
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
                    await service.SendToUserAsync(push, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Mijoz push'i yuborilmadi: {ShopUserId}", push.ShopUserId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ilova to'xtatilmoqda.
        }
    }
}
