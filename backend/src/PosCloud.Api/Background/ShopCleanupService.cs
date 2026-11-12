using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Api.Background;

/// <summary>
/// Mobil mijoz kirishi bo'yicha eskirgan yozuvlarni tozalaydi (har 6 soatda):
/// OTP kodlar — 1 kundan eski (urinish/soatlik limitlar 1 soatlik oynadan foydalanadi);
/// refresh tokenlar — muddati 1 kundan oldin tugagan yoki 30 kundan oldin bekor qilingan
/// (bekor qilingan token bir muddat saqlanadi — qayta ishlatilsa sessiyalarni yopish uchun).
/// Push qurilma tokenlari — 90 kundan beri ilova ro'yxatdan o'tkazmagan bo'lsa.
/// </summary>
public class ShopCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(2);
    private const int DeviceTokenDays = 90;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShopCleanupService> _logger;

    public ShopCleanupService(IServiceScopeFactory scopeFactory, ILogger<ShopCleanupService> logger)
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
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mijoz OTP/refresh tozalashda xato");
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

    public async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var now = DateTime.UtcNow;

        var otpBefore = now.AddDays(-1);
        var otps = await db.ShopOtpCodes.IgnoreQueryFilters()
            .Where(c => c.CreatedAt < otpBefore)
            .ExecuteDeleteAsync(ct);

        var expiredBefore = now.AddDays(-1);
        var revokedBefore = now.AddDays(-30);
        var tokens = await db.ShopRefreshTokens.IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < expiredBefore || (t.RevokedAt != null && t.RevokedAt < revokedBefore))
            .ExecuteDeleteAsync(ct);

        var deviceBefore = now.AddDays(-DeviceTokenDays);
        var devices = await db.ShopDeviceTokens.IgnoreQueryFilters()
            .Where(d => d.LastSeenAt < deviceBefore)
            .ExecuteDeleteAsync(ct);

        if (otps > 0 || tokens > 0 || devices > 0)
            _logger.LogInformation("Tozalandi: {Otps} ta OTP, {Tokens} ta refresh token, {Devices} ta push token",
                otps, tokens, devices);
    }
}
