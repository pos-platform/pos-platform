using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Infrastructure.Authentication;
using PosPlatform.Infrastructure.Excel;
using PosPlatform.Infrastructure.Fiscal;
using PosPlatform.Infrastructure.Persistence;

namespace PosPlatform.Infrastructure;

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

        // Fiskalizatsiya seam'i — hozir placeholder. Keyin lokal agent (FDS/Rahmat MK)
        // bilan ishlaydigan implementatsiya bilan almashtiriladi.
        services.AddScoped<IFiscalReceiptService, PendingFiscalReceiptService>();

        services.AddSingleton<IProductExcelService, ProductExcelService>();
        services.AddSingleton<IReportExcelService, ReportExcelService>();

        return services;
    }
}
