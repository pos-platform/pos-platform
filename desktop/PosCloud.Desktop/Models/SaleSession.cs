using System.Collections.ObjectModel;

namespace PosCloud.Desktop.Models;

/// <summary>Bitta ochiq savdo (savat + to'lov holati). Bir nechta parallel savdo uchun.</summary>
public class SaleSession
{
    public ObservableCollection<CartLine> Cart { get; } = new();
    public string Paid { get; set; } = "0";
    /// <summary>Standart to'lov turi — karta.</summary>
    public string PaymentMethod { get; set; } = "Card";
    public Guid? DebtorId { get; set; }

    /// <summary>Bonus karta (QR) orqali biriktirilgan mijoz — shu savdo tabiga tegishli.</summary>
    public LoyaltyCustomerDto? Customer { get; set; }

    /// <summary>Kassir kiritgan "ball bilan to'lash" summasi (cheklanmagan xom qiymat).</summary>
    public decimal RedeemPoints { get; set; }
}
