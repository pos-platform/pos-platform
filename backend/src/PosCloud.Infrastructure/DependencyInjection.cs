using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Infrastructure.Authentication;
using PosCloud.Infrastructure.Excel;
using PosCloud.Infrastructure.Fiscal;
using PosCloud.Infrastructure.Otp;
using PosCloud.Infrastructure.Persistence;
using PosCloud.Infrastructure.Push;

namespace PosCloud.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure qatlami: PostgreSQL DbContext, JWT generator va parol hasher.
    /// Bu yerga keyinchalik fiskal/printer/tarozi servislari qo'shiladi.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitializer>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Bonus karta: dinamik QR imzosi (Loyalty:QrSigningKey yoki JWT kalitidan hosila).
        services.AddSingleton<ILoyaltyQrSigner, LoyaltyQrSigner>();

        // Mobil ilova: mijoz tokeni va kirish kodi (Telegram Gateway → Eskiz SMS).
        services.AddSingleton<IShopTokenGenerator, ShopTokenGenerator>();
        services.Configure<OtpSettings>(configuration.GetSection(OtpSettings.SectionName));
        services.AddHttpClient<TelegramGatewayClient>();
        services.AddHttpClient<EskizSmsClient>();
        services.AddScoped<IOtpSender, OtpSender>();

        // Mobil ilova: push bildirishnomalar (FCM HTTP v1). Sozlanmagan bo'lsa — faqat log.
        services.Configure<PushSettings>(configuration.GetSection(PushSettings.SectionName));
        services.AddHttpClient(FcmPushSender.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(15));
        var push = configuration.GetSection(PushSettings.SectionName).Get<PushSettings>() ?? new PushSettings();
        if (push.Fcm.IsConfigured)
            services.AddSingleton<IPushSender, FcmPushSender>();
        else
            services.AddSingleton<IPushSender, LogPushSender>();

        // Fiskalizatsiya seam'i — hozir placeholder. Keyin lokal agent (FDS/Rahmat MK)
        // bilan ishlaydigan implementatsiya bilan almashtiriladi.
        services.AddScoped<IFiscalReceiptService, PendingFiscalReceiptService>();

        services.AddSingleton<IProductExcelService, ProductExcelService>();
        services.AddSingleton<IReportExcelService, ReportExcelService>();

        return services;
    }
}
