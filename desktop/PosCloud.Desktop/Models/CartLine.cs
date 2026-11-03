using System.ComponentModel;

namespace PosCloud.Desktop.Models;

public class CartLine : INotifyPropertyChanged
{
    public ProductDto Product { get; }

    /// <summary>
    /// Markirovka (Asl Belgisi) kodi — GS1 DataMatrix ning to'liq satri, faqat markirovkali
    /// tovarda to'ldiriladi. Har bir dona uchun noyob, shu sabab bunday qatorda miqdor doim 1.
    /// </summary>
    public string? MarkingCode { get; }

    public CartLine(ProductDto product, decimal quantity, string? markingCode = null)
    {
        Product = product;
        _quantity = quantity;
        MarkingCode = markingCode;
    }

    /// <summary>Qatorда markirovka kodi bormi — mavjud bo'lsa miqdor va o'zgartirish qulflanadi.</summary>
    public bool IsMarked => Product.IsMarked;

    private decimal _quantity;
    public decimal Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            Raise(nameof(Quantity));
            Raise(nameof(QtyLabel));
            Raise(nameof(LineTotal));
            Raise(nameof(LineTotalLabel));
        }
    }

    public decimal LineTotal => Product.Price * _quantity;
    public string LineTotalLabel => LineTotal.ToString("N0");
    public string QtyLabel => _quantity.ToString("0.###");

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
