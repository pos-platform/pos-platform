using System.Collections.ObjectModel;

namespace PosPlatform.Desktop.Models;

/// <summary>Bitta ochiq savdo (savat + to'lov holati). Bir nechta parallel savdo uchun.</summary>
public class SaleSession
{
    public ObservableCollection<CartLine> Cart { get; } = new();
    public string Paid { get; set; } = "0";
    /// <summary>Standart to'lov turi — karta.</summary>
    public string PaymentMethod { get; set; } = "Card";
    public Guid? DebtorId { get; set; }
}
