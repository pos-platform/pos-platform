using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Infrastructure.Persistence;

/// <summary>
/// Migratsiyalarni qo'llaydi va boshlang'ich ma'lumotni (admin user, standart
/// kategoriya) yaratadi — toza bazada dastur darhol ishlay olishi uchun.
/// </summary>
public class ApplicationDbContextInitializer
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationDbContextInitializer> _logger;

    public ApplicationDbContextInitializer(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<ApplicationDbContextInitializer> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        await _db.Database.MigrateAsync();
    }

    public async Task SeedAsync()
    {
        // Global permission katalogini seed qilamiz — bir marta, barcha orglar uchun.
        await SeedPermissionsAsync();

        // Demo ma'lumot (standart tashkilot + admin + kategoriya + do'kon) — faqat
        // Seed:DemoData=true bo'lsa. Standart (false) holatda toza SaaS: faqat SuperAdmin
        // qoladi, tashkilotlarni SuperAdmin o'zi yaratadi.
        var seedDemo = _configuration.GetValue<bool>("Seed:DemoData");
        if (seedDemo)
        {
            // Standart tashkilot (mijoz). BIRINCHI yaratiladi, qolgan yozuvlar shunga biriktiriladi.
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Name == "Asosiy tashkilot");
            if (org is null)
            {
                org = new Organization { Name = "Asosiy tashkilot", IsActive = true };
                _db.Organizations.Add(org);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Standart tashkilot yaratildi.");
            }

            // Standart kategoriya.
            if (!await _db.Categories.AnyAsync())
            {
                _db.Categories.Add(new Category { Name = "Umumiy", OrganizationId = org.Id });
                await _db.SaveChangesAsync();
                _logger.LogInformation("Standart 'Umumiy' kategoriyasi yaratildi.");
            }

            // Standart do'kon + kassa (FENIXdagi Магазин/Касса kabi).
            if (!await _db.Branches.AnyAsync())
            {
                var store = new Branch { Name = "Asosiy do'kon", IsActive = true, OrganizationId = org.Id };
                store.Cashboxes.Add(new Cashbox { Name = "Kassa 1", IsActive = true, OrganizationId = org.Id });
                _db.Branches.Add(store);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Standart do'kon va kassa yaratildi.");
            }

            // Standart tashkilot uchun 3 ta tizim rolini seed qilamiz.
            await SeedOrgRolesAsync(org.Id);

            // Eski (tashkilotsiz) ma'lumotlarni standart tashkilotga biriktiramiz — bir martalik backfill.
            await BackfillOrganizationAsync(org.Id);

            // Far-future = obuna amalda. Boshlang'ich admin uchun ishlatamiz (dev login uzilmasligi uchun).
            var farFuture = DateTime.UtcNow.Date.AddYears(50);

            var adminUsername = _configuration["Seed:AdminUsername"] ?? "admin";
            var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin12345!";

            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Username == adminUsername);
            if (admin is null)
            {
                var adminRole = await _db.Roles
                    .FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.Name == "Admin");
                _db.Users.Add(new User
                {
                    Username = adminUsername,
                    PasswordHash = _passwordHasher.Hash(adminPassword),
                    Role = UserRole.Admin,
                    AppRoleId = adminRole?.Id,
                    OrganizationId = org.Id,
                    LicensedUntil = farFuture
                });
                await _db.SaveChangesAsync();
                _logger.LogInformation(
                    "Boshlang'ich admin yaratildi: '{Username}'. DIQQAT: parolni almashtiring.",
                    adminUsername);
            }
        }

        // Platforma egasi (SuperAdmin) — barcha tashkilotlarni boshqaradi. Har doim ta'minlanadi.
        var superUsername = _configuration["Seed:SuperAdminUsername"] ?? "superadmin";
        var superPassword = _configuration["Seed:SuperAdminPassword"] ?? "Super12345!";

        if (!await _db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            _db.Users.Add(new User
            {
                Username = superUsername,
                PasswordHash = _passwordHasher.Hash(superPassword),
                Role = UserRole.SuperAdmin
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Platforma egasi (SuperAdmin) yaratildi: '{Username}'. DIQQAT: parolni almashtiring.",
                superUsername);
        }
    }

    /// <summary>
    /// Tizimga tegishli barcha permission'larni bazaga yozadi (idempotent: mavjudlari qayta yozilmaydi).
    /// </summary>
    private async Task SeedPermissionsAsync()
    {
        var existing = await _db.Permissions.Select(p => p.Code).ToListAsync();
        var allCodes = Permissions.All();
        var toAdd = allCodes.Except(existing).ToList();

        if (toAdd.Count == 0) return;

        var categoryMap = new Dictionary<string, string>
        {
            ["users."] = "Foydalanuvchilar",
            ["roles."] = "Rollar",
            ["branches."] = "Filiallar",
            ["products."] = "Mahsulotlar",
            ["categories."] = "Kategoriyalar",
            ["sales."] = "Sotuvlar",
            ["reports."] = "Hisobotlar",
            ["cashbox."] = "Kassa",
            ["inventory."] = "Inventar",
            ["suppliers."] = "Yetkazib beruvchilar",
            ["customers."] = "Mijozlar",
            ["debtors."] = "Qarzdorlar",
            ["expenses."] = "Xarajatlar",
            ["settings."] = "Sozlamalar",
        };

        var nameMap = new Dictionary<string, string>
        {
            [Permissions.UsersView]       = "Foydalanuvchilarni ko'rish",
            [Permissions.UsersCreate]     = "Foydalanuvchi yaratish",
            [Permissions.UsersEdit]       = "Foydalanuvchini tahrirlash",
            [Permissions.UsersActivate]   = "Faollashtirish/bloklash",
            [Permissions.RolesView]       = "Rollarni ko'rish",
            [Permissions.RolesManage]     = "Rollarni boshqarish",
            [Permissions.BranchesView]    = "Filiallarni ko'rish",
            [Permissions.BranchesCreate]  = "Filial yaratish",
            [Permissions.BranchesEdit]    = "Filialni tahrirlash",
            [Permissions.BranchesDelete]  = "Filialni o'chirish",
            [Permissions.ProductsView]    = "Mahsulotlarni ko'rish",
            [Permissions.ProductsCreate]  = "Mahsulot yaratish",
            [Permissions.ProductsEdit]    = "Mahsulotni tahrirlash",
            [Permissions.ProductsDelete]  = "Mahsulotni o'chirish",
            [Permissions.CategoriesView]  = "Kategoriyalarni ko'rish",
            [Permissions.CategoriesManage]= "Kategoriyalarni boshqarish",
            [Permissions.SalesView]       = "Sotuvlarni ko'rish",
            [Permissions.SalesCreate]     = "Sotuv yaratish",
            [Permissions.SalesRefund]     = "Qaytarish",
            [Permissions.ReportsView]     = "Hisobotlarni ko'rish",
            [Permissions.CashboxView]     = "Kassani ko'rish",
            [Permissions.CashboxManage]   = "Kassani boshqarish",
            [Permissions.InventoryView]   = "Inventarni ko'rish",
            [Permissions.InventoryManage] = "Inventarni boshqarish",
            [Permissions.SuppliersView]   = "Yetkazib beruvchilarni ko'rish",
            [Permissions.SuppliersManage] = "Yetkazib beruvchilarni boshqarish",
            [Permissions.CustomersView]   = "Mijozlarni ko'rish",
            [Permissions.CustomersManage] = "Mijozlarni boshqarish",
            [Permissions.DebtorsView]     = "Qarzdorlarni ko'rish",
            [Permissions.DebtorsManage]   = "Qarzdorlarni boshqarish",
            [Permissions.ExpensesView]    = "Xarajatlarni ko'rish",
            [Permissions.ExpensesManage]  = "Xarajatlarni boshqarish",
            [Permissions.SettingsManage]  = "Sozlamalarni boshqarish",
        };

        foreach (var code in toAdd)
        {
            var category = categoryMap.FirstOrDefault(kv => code.StartsWith(kv.Key)).Value ?? "Boshqa";
            var name = nameMap.GetValueOrDefault(code, code);
            _db.Permissions.Add(new Permission { Id = Guid.NewGuid(), Code = code, Name = name, Category = category });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("{Count} ta yangi permission qo'shildi.", toAdd.Count);
    }

    /// <summary>
    /// Berilgan tashkilot uchun 3 ta tizim rolini (Admin, Manager, Cashier) yaratadi va
    /// ular uchun standart permissionlarni belgilaydi. Idempotent.
    /// </summary>
    private async Task SeedOrgRolesAsync(Guid orgId)
    {
        var permMap = await _db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);

        var roleDefinitions = new[]
        {
            ("Admin",   Permissions.AdminPermissions),
            ("Manager", Permissions.ManagerPermissions),
            ("Cashier", Permissions.CashierPermissions),
        };

        foreach (var (roleName, permCodes) in roleDefinitions)
        {
            var existing = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Name == roleName);

            if (existing is null)
            {
                var role = new Role
                {
                    Name = roleName,
                    IsSystem = true,
                    OrganizationId = orgId,
                    RolePermissions = permCodes
                        .Where(c => permMap.ContainsKey(c))
                        .Select(c => new RolePermission { PermissionId = permMap[c] })
                        .ToList()
                };
                _db.Roles.Add(role);
                _logger.LogInformation("'{Role}' roli yaratildi (org: {OrgId}).", roleName, orgId);
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Tashkilotsiz (OrganizationId = NULL) qolgan eski yozuvlarni standart tashkilotga biriktiradi.
    /// Idempotent: keyingi ishga tushishlarda 0 qator yangilaydi. SuperAdmin foydalanuvchi tegilmaydi.
    /// </summary>
    private async Task BackfillOrganizationAsync(Guid orgId)
    {
        // Barcha ijaraga tegishli jadvallar (Organizations va Users'dan tashqari).
        var tables = new[]
        {
            "Categories", "Products", "Branches", "Cashboxes", "Sales", "SaleItems", "Receipts",
            "CashShifts", "Promotions", "Suppliers", "StockReceipts", "StockReceiptItems",
            "Debtors", "DebtorTransactions", "Expenses", "WriteOffs", "WriteOffItems", "SupplierPayments"
        };

        // Jadval nomlari qattiq kodlangan (injection yo'q), orgId esa parametr sifatida uzatiladi.
        foreach (var t in tables)
        {
            await _db.Database.ExecuteSqlRawAsync(
                $"UPDATE \"{t}\" SET \"OrganizationId\" = {{0}} WHERE \"OrganizationId\" IS NULL;", orgId);
        }

        // Foydalanuvchilar: SuperAdmin (Role=3) tashkilotsiz qoladi, qolganlar standartga.
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Users\" SET \"OrganizationId\" = {0} WHERE \"OrganizationId\" IS NULL AND \"Role\" <> 3;", orgId);
    }
}
