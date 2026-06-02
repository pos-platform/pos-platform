using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Infrastructure.Persistence;

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
        // Standart tashkilot (mijoz) — toza bazada ham, eski bazada ham bo'lishini ta'minlaymiz.
        // BIRINCHI yaratiladi, chunki qolgan standart yozuvlar shunga biriktiriladi.
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

        // Eski (tashkilotsiz) ma'lumotlarni standart tashkilotga biriktiramiz — bir martalik backfill.
        // SuperAdmin foydalanuvchidan tashqari (u tashkilotsiz qoladi).
        await BackfillOrganizationAsync(org.Id);

        // Far-future = obuna amalda. Boshlang'ich admin uchun ishlatamiz (dev login uzilmasligi uchun).
        var farFuture = DateTime.UtcNow.Date.AddYears(50);

        // Boshlang'ich admin (login qilish uchun).
        var adminUsername = _configuration["Seed:AdminUsername"] ?? "admin";
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin12345!";

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Username == adminUsername);
        if (admin is null)
        {
            _db.Users.Add(new User
            {
                Username = adminUsername,
                PasswordHash = _passwordHasher.Hash(adminPassword),
                Role = UserRole.Admin,
                OrganizationId = org.Id,
                LicensedUntil = farFuture
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Boshlang'ich admin yaratildi: '{Username}'. DIQQAT: parolni almashtiring.",
                adminUsername);
        }
        else if (admin.Role == UserRole.Admin)
        {
            // Eski admin — tashkilot/obunani backfill qilamiz (aks holda yangi qoidalar uni bloklab qo'yadi).
            var changed = false;
            if (admin.OrganizationId is null) { admin.OrganizationId = org.Id; changed = true; }
            if (admin.LicensedUntil is null) { admin.LicensedUntil = farFuture; changed = true; }
            if (changed) await _db.SaveChangesAsync();
        }

        // Platforma egasi (SuperAdmin) — barcha tashkilotlarni boshqaradi.
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
