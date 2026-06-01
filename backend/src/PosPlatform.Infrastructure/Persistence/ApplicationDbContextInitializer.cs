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

        // Boshlang'ich admin (login qilish uchun).
        var adminUsername = _configuration["Seed:AdminUsername"] ?? "admin";
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin12345!";

        if (!await _db.Users.AnyAsync(u => u.Username == adminUsername))
        {
            _db.Users.Add(new User
            {
                Username = adminUsername,
                PasswordHash = _passwordHasher.Hash(adminPassword),
                Role = UserRole.Admin
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Boshlang'ich admin yaratildi: '{Username}'. DIQQAT: parolni almashtiring.",
                adminUsername);
        }
    }
}
