using System.ComponentModel;

namespace PosPlatform.Desktop.Models;

/// <summary>Restoran buyurtma qatori (ovqat + miqdor + izoh).</summary>
public class OrderLine : INotifyPropertyChanged
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = "";
    public decimal UnitPrice { get; init; }

    /// <summary>Oshxonaga yuborilgan qator — qayta yuborilmaydi, o'chirib bo'lmaydi.</summary>
    public bool SentToKitchen { get; set; }

    public OrderLine() { }

    private decimal _quantity = 1;
    public decimal Quantity
    {
        get => _quantity;
        set { _quantity = value; Raise(nameof(Quantity)); Raise(nameof(QtyLabel)); Raise(nameof(LineTotal)); Raise(nameof(LineTotalLabel)); }
    }

    private string? _note;
    public string? Note
    {
        get => _note;
        set { _note = value; Raise(nameof(Note)); }
    }

    public decimal LineTotal => UnitPrice * _quantity;
    public string LineTotalLabel => LineTotal.ToString("N0");
    public string QtyLabel => _quantity.ToString("0.###");
    public string SentLabel => SentToKitchen ? "✓ oshxonada" : "yangi";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
