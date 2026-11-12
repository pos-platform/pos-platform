using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty;

/// <summary>
/// Mobil ilova akkauntini (global ShopUser) do'kondagi mijoz kartochkasiga (Customer) bog'laydi:
/// ShopUserId bo'yicha, bo'lmasa telefon bo'yicha bog'lanadi, bo'lmasa yaratiladi.
/// Yangi bog'lanishda signupBonus bir marta beriladi (Signup tranzaksiyasi bo'lmasa).
/// Tenant filtri chetlab o'tiladi — tashkilot aniq beriladi. SaveChanges chaqiruvchida.
/// </summary>
public static class ShopCustomerLinker
{
    public static async Task<Customer> EnsureAsync(IApplicationDbContext db, Guid orgId, ShopUser user,
        LoyaltySource source, CancellationToken ct)
    {
        var customers = db.Customers.IgnoreQueryFilters().Where(c => c.OrganizationId == orgId);

        var linked = await customers.FirstOrDefaultAsync(c => c.ShopUserId == user.Id, ct);
        if (linked is not null) return linked;

        var customer = await customers.FirstOrDefaultAsync(c => c.ShopUserId == null && c.Phone == user.Phone, ct);
        var created = customer is null;
        if (customer is not null)
        {
            customer.ShopUserId = user.Id;
        }
        else
        {
            customer = new Customer
            {
                OrganizationId = orgId,
                Name = FullName(user),
                Phone = user.Phone,
                Email = user.Email,
                ShopUserId = user.Id,
                Comment = "Mobil ilova"
            };
            db.Customers.Add(customer);
        }

        var settings = await LoyaltyRules.LoadAsync(db, orgId, ct);
        if (settings.Enabled && settings.SignupBonus > 0)
        {
            var alreadyGranted = !created && await db.LoyaltyTransactions.IgnoreQueryFilters()
                .AnyAsync(t => t.CustomerId == customer.Id && t.Type == LoyaltyTransactionType.Signup, ct);
            if (!alreadyGranted)
                LoyaltyRules.Apply(db, customer, Math.Floor(settings.SignupBonus), LoyaltyTransactionType.Signup,
                    source, 0, "Ilova orqali ro'yxatdan o'tish bonusi");
        }

        return customer;
    }

    public static string FullName(ShopUser u)
    {
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return name.Length > 0 ? name : "Mijoz";
    }
}
