using Microsoft.EntityFrameworkCore;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// DbContext'ning Application qatlamiga ochiq abstraksiyasi.
/// Infrastructure (EF Core) bunga bog'liq, Application esa konkret
/// DbContext'ni bilmaydi — shu sabab qatlamlar bog'liqligi toza qoladi.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Cashbox> Cashboxes { get; }
    DbSet<User> Users { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Receipt> Receipts { get; }

    /// <summary>Fiskal modul (fleshka) kartochkalari — modul ID ga nom biriktiradi.</summary>
    DbSet<FiscalModule> FiscalModules { get; }
    DbSet<CashShift> CashShifts { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<StockReceipt> StockReceipts { get; }
    DbSet<StockReceiptItem> StockReceiptItems { get; }
    DbSet<Debtor> Debtors { get; }
    DbSet<DebtorTransaction> DebtorTransactions { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<WriteOff> WriteOffs { get; }
    DbSet<WriteOffItem> WriteOffItems { get; }
    DbSet<SupplierPayment> SupplierPayments { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<InventoryItem> InventoryItems { get; }

    DbSet<BranchStock> BranchStocks { get; }

    // Task 4: Branch transfers
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferItem> StockTransferItems { get; }

    // Task 5: Cash movements
    DbSet<CashMovement> CashMovements { get; }

    // Task 8: Customer loyalty
    DbSet<Customer> Customers { get; }
    DbSet<LoyaltyTransaction> LoyaltyTransactions { get; }
    DbSet<LoyaltySettings> LoyaltySettings { get; }
    DbSet<LoyaltyCategoryRate> LoyaltyCategoryRates { get; }

    // Task 9: Online payments
    DbSet<OnlinePayment> OnlinePayments { get; }
    DbSet<ShopUser> ShopUsers { get; }
    DbSet<ShopUserAddress> ShopUserAddresses { get; }
    DbSet<ShopRefreshToken> ShopRefreshTokens { get; }
    DbSet<ShopOtpCode> ShopOtpCodes { get; }
    DbSet<ShopDeviceToken> ShopDeviceTokens { get; }
    DbSet<OnlineOrder> OnlineOrders { get; }
    DbSet<OnlineOrderItem> OnlineOrderItems { get; }
    DbSet<OnlineStoreSettings> OnlineStoreSettings { get; }
    DbSet<PromoCode> PromoCodes { get; }

    // Bosqich 9: marketing
    DbSet<ShopBanner> ShopBanners { get; }
    DbSet<ShopCollection> ShopCollections { get; }
    DbSet<ShopCollectionItem> ShopCollectionItems { get; }
    DbSet<ShopStory> ShopStories { get; }
    DbSet<ShopStorySlide> ShopStorySlides { get; }
    DbSet<ShopFavorite> ShopFavorites { get; }
    DbSet<ShopStockSubscription> ShopStockSubscriptions { get; }
    DbSet<ShopCartSnapshot> ShopCartSnapshots { get; }
    DbSet<ShopReferral> ShopReferrals { get; }
    DbSet<ShopPushCampaign> ShopPushCampaigns { get; }
    DbSet<LoyaltyTier> LoyaltyTiers { get; }

    // Task 6: Label printing
    DbSet<LabelPrintJob> LabelPrintJobs { get; }
    DbSet<LabelPrintJobItem> LabelPrintJobItems { get; }

    DbSet<RoleTemplate> RoleTemplates { get; }

    // Restoran moduli
    DbSet<DishIngredient> DishIngredients { get; }
    DbSet<RestaurantTable> RestaurantTables { get; }
    DbSet<KitchenIssue> KitchenIssues { get; }
    DbSet<KitchenIssueItem> KitchenIssueItems { get; }
    DbSet<KitchenTicket> KitchenTickets { get; }
    DbSet<KitchenTicketItem> KitchenTicketItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
