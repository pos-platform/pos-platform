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
        // Standart kategoriya.
        if (!await _db.Categories.AnyAsync())
        {
            _db.Categories.Add(new Category { Name = "Umumiy" });
            await _db.SaveChangesAsync();
            _logger.LogInformation("Standart 'Umumiy' kategoriyasi yaratildi.");
        }

        // Standart tashkilot (mijoz) — toza bazada ham, eski bazada ham bo'lishini ta'minlaymiz.
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Name == "Asosiy tashkilot");
        if (org is null)
        {
            org = new Organization { Name = "Asosiy tashkilot", IsActive = true };
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Standart tashkilot yaratildi.");
        }

        // Standart do'kon + kassa (FENIXdagi Магазин/Касса kabi).
        if (!await _db.Branches.AnyAsync())
        {
            var store = new Branch { Name = "Asosiy do'kon", IsActive = true, OrganizationId = org.Id };
            store.Cashboxes.Add(new Cashbox { Name = "Kassa 1", IsActive = true });
            _db.Branches.Add(store);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Standart do'kon va kassa yaratildi.");
        }

        // Tashkilotsiz qolgan eski do'konlarni standart tashkilotga biriktiramiz.
        var orphanBranches = await _db.Branches.Where(b => b.OrganizationId == null).ToListAsync();
        if (orphanBranches.Count > 0)
        {
            foreach (var b in orphanBranches) b.OrganizationId = org.Id;
            await _db.SaveChangesAsync();
        }

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
}
