using Microsoft.Extensions.DependencyInjection;
using PosPlatform.Application.Features.Analytics;
using PosPlatform.Application.Features.Audit;
using PosPlatform.Application.Features.Auth;
using PosPlatform.Application.Features.Categories;
using PosPlatform.Application.Features.Dashboard;
using PosPlatform.Application.Features.Debtors;
using PosPlatform.Application.Features.Expenses;
using PosPlatform.Application.Features.Fiscal;
using PosPlatform.Application.Features.Organizations;
using PosPlatform.Application.Features.Payments;
using PosPlatform.Application.Features.Products;
using PosPlatform.Application.Features.Promotions;
using PosPlatform.Application.Features.Sales;
using PosPlatform.Application.Features.Shifts;
using PosPlatform.Application.Features.Stock;
using PosPlatform.Application.Features.Stores;
using PosPlatform.Application.Features.Suppliers;
using PosPlatform.Application.Features.Users;
using PosPlatform.Application.Features.Reports;
using PosPlatform.Application.Features.WriteOffs;

namespace PosPlatform.Application;

public static class DependencyInjection
{
    /// <summary>Application qatlami servislarini DI konteynerga ulaydi.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ICashShiftService, CashShiftService>();
        services.AddScoped<IDebtorService, DebtorService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IWriteOffService, WriteOffService>();
        services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
        services.AddScoped<IFiscalQueueService, FiscalQueueService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
