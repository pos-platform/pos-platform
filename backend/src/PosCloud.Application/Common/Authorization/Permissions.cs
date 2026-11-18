namespace PosCloud.Application.Common.Authorization;

public static class Permissions
{
    // Foydalanuvchilar
    public const string UsersView    = "users.view";
    public const string UsersCreate  = "users.create";
    public const string UsersEdit    = "users.edit";
    public const string UsersActivate = "users.activate";

    // Rollar
    public const string RolesView    = "roles.view";
    public const string RolesManage  = "roles.manage";

    // Filiallar
    public const string BranchesView   = "branches.view";
    public const string BranchesCreate = "branches.create";
    public const string BranchesEdit   = "branches.edit";
    public const string BranchesDelete = "branches.delete";

    // Mahsulotlar
    public const string ProductsView   = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsEdit   = "products.edit";
    public const string ProductsDelete = "products.delete";

    // Kategoriyalar
    public const string CategoriesView   = "categories.view";
    public const string CategoriesManage = "categories.manage";

    // Sotuvlar
    public const string SalesView   = "sales.view";
    public const string SalesCreate = "sales.create";
    public const string SalesRefund = "sales.refund";
    /// <summary>Kassada narxni qo'lda o'zgartirish.</summary>
    public const string SalesPriceOverride = "sales.price_override";
    /// <summary>Kassada qo'lda chegirma berish.</summary>
    public const string SalesDiscount = "sales.discount";

    // Hisobotlar
    public const string ReportsView = "reports.view";

    // Kassa
    public const string CashboxView   = "cashbox.view";
    public const string CashboxManage = "cashbox.manage";
    /// <summary>Kassa harakatlari: naqd kirim/chiqim, inkassatsiya (yaratish va o'chirish).</summary>
    public const string CashMovements = "cash.movements";

    // Ombor / inventar
    public const string InventoryView   = "inventory.view";
    public const string InventoryManage = "inventory.manage";

    // Yetkazib beruvchilar
    public const string SuppliersView   = "suppliers.view";
    public const string SuppliersManage = "suppliers.manage";

    // Mijozlar
    public const string CustomersView   = "customers.view";
    public const string CustomersManage = "customers.manage";

    // Qarzdorlar
    public const string DebtorsView   = "debtors.view";
    public const string DebtorsManage = "debtors.manage";

    // Xarajatlar
    public const string ExpensesView   = "expenses.view";
    public const string ExpensesManage = "expenses.manage";

    // Tashkilot sozlamalari
    public const string SettingsManage = "settings.manage";

    // ---- Standart rollar uchun to'plamlar ----

    public static readonly string[] AdminPermissions = All();

    public static readonly string[] ManagerPermissions =
    [
        UsersView,
        RolesView,
        BranchesView,
        ProductsView, ProductsCreate, ProductsEdit,
        CategoriesView, CategoriesManage,
        SalesView, SalesCreate, SalesRefund, SalesPriceOverride, SalesDiscount,
        ReportsView,
        CashboxView, CashMovements,
        InventoryView, InventoryManage,
        SuppliersView, SuppliersManage,
        CustomersView, CustomersManage,
        DebtorsView, DebtorsManage,
        ExpensesView, ExpensesManage,
    ];

    public static readonly string[] CashierPermissions =
    [
        ProductsView,
        CategoriesView,
        SalesView, SalesCreate, SalesRefund,
        CashboxView,
        CustomersView, CustomersManage,
        DebtorsView,
    ];

    public static string[] All() =>
    [
        UsersView, UsersCreate, UsersEdit, UsersActivate,
        RolesView, RolesManage,
        BranchesView, BranchesCreate, BranchesEdit, BranchesDelete,
        ProductsView, ProductsCreate, ProductsEdit, ProductsDelete,
        CategoriesView, CategoriesManage,
        SalesView, SalesCreate, SalesRefund, SalesPriceOverride, SalesDiscount,
        ReportsView,
        CashboxView, CashboxManage, CashMovements,
        InventoryView, InventoryManage,
        SuppliersView, SuppliersManage,
        CustomersView, CustomersManage,
        DebtorsView, DebtorsManage,
        ExpensesView, ExpensesManage,
        SettingsManage,
    ];
}
