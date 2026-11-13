using Microsoft.Extensions.DependencyInjection;
using PosCloud.Application.Features.Analytics;
using PosCloud.Application.Features.Audit;
using PosCloud.Application.Features.Auth;
using PosCloud.Application.Features.CashMovements;
using PosCloud.Application.Features.Categories;
using PosCloud.Application.Features.Customers;
using PosCloud.Application.Features.Dashboard;
using PosCloud.Application.Features.Dishes;
using PosCloud.Application.Features.KitchenIssues;
using PosCloud.Application.Features.KitchenTickets;
using PosCloud.Application.Features.Tables;
using PosCloud.Application.Features.Debtors;
using PosCloud.Application.Features.Expenses;
using PosCloud.Application.Features.Fiscal;
using PosCloud.Application.Features.Inventories;
using PosCloud.Application.Features.Labels;
using PosCloud.Application.Features.OnlinePayments;
using PosCloud.Application.Features.Organizations;
using PosCloud.Application.Features.Payments;
using PosCloud.Application.Features.Products;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.Reports;
using PosCloud.Application.Features.Sales;
using PosCloud.Application.Features.ShopAuth;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopProfile;
using PosCloud.Application.Features.ShopOrders;
using PosCloud.Application.Features.ShopPush;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.OnlineStores;
using PosCloud.Application.Features.Shifts;
using PosCloud.Application.Features.Stock;
using PosCloud.Application.Features.Stores;
using PosCloud.Application.Features.Suppliers;
using PosCloud.Application.Features.Transfers;
using PosCloud.Application.Features.RoleTemplates;
using PosCloud.Application.Features.Roles;
using PosCloud.Application.Features.Users;
using PosCloud.Application.Features.WriteOffs;

namespace PosCloud.Application;

public static class DependencyInjection
{
    /// <summary>Application qatlami servislarini DI konteynerga ulaydi.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IRoleTemplateService, RoleTemplateService>();
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
        services.AddScoped<IFiscalModuleService, FiscalModuleService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<ICashMovementService, CashMovementService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<PosCloud.Application.Features.Loyalty.ILoyaltyService, PosCloud.Application.Features.Loyalty.LoyaltyService>();
        services.AddScoped<PosCloud.Application.Features.Loyalty.IShopBonusService, PosCloud.Application.Features.Loyalty.ShopBonusService>();
        services.AddScoped<IOnlinePaymentService, OnlinePaymentService>();
        services.AddScoped<ILabelService, LabelService>();

        // Mobil ilova (online do'kon)
        services.AddScoped<IShopAuthService, ShopAuthService>();
        services.AddScoped<IShopCatalogService, ShopCatalogService>();
        services.AddScoped<IShopProfileService, ShopProfileService>();
        services.AddScoped<IShopOrderService, ShopOrderService>();
        services.AddScoped<IShopPushService, ShopPushService>();
        services.AddScoped<PosCloud.Application.Features.ShopMarketing.IShopMarketingService, PosCloud.Application.Features.ShopMarketing.ShopMarketingService>();
        services.AddScoped<PosCloud.Application.Features.OnlineMarketing.IOnlineMarketingService, PosCloud.Application.Features.OnlineMarketing.OnlineMarketingService>();
        services.AddScoped<IOnlineOrderService, OnlineOrderService>();
        services.AddScoped<IOnlineStoreService, OnlineStoreService>();
        services.AddScoped<PosCloud.Application.Features.Courier.ICourierService, PosCloud.Application.Features.Courier.CourierService>();
        services.AddScoped<PosCloud.Application.Features.PromoCodes.IPromoCodeService, PosCloud.Application.Features.PromoCodes.PromoCodeService>();

        // Restoran moduli
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IKitchenIssueService, KitchenIssueService>();
        services.AddScoped<IKitchenTicketService, KitchenTicketService>();

        return services;
    }
}
