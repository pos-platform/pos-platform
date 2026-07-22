using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PosPlatform.Application.Common.Interfaces;

namespace PosPlatform.Infrastructure.Persistence;

/// <summary>
/// `dotnet ef migrations`/`database update` buyruqlari uchun design-time DbContext.
/// Api loyihasidagi appsettings.json'dan connection string'ni o'qiydi
/// (yoki ConnectionStrings__DefaultConnection muhit o'zgaruvchisidan).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "PosPlatform.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=pos_platform;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Design-time'da foydalanuvchi konteksti yo'q — tashkilotsiz (filtr o'chiq).
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentUser());
    }

    /// <summary>Design-time uchun bo'sh ICurrentUser (hammasi null).</summary>
    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public string? Username => null;
        public Guid? BranchId => null;
        public Guid? OrganizationId => null;
        public string? TerminalId => null;
        public string? IpAddress => null;
        public IReadOnlyList<string> Permissions => [];
    }
}
