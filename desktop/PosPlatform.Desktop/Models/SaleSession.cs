using System.Collections.ObjectModel;

namespace PosPlatform.Desktop.Models;

/// <summary>Bitta ochiq savdo (savat + to'lov holati). Bir nechta parallel savdo uchun.</summary>
public class SaleSession
{
    public ObservableCollection<CartLine> Cart { get; } = new();
    public string Paid { get; set; } = "0";
    public int PaymentIndex { get; set; }
    public Guid? DebtorId { get; set; }
}
