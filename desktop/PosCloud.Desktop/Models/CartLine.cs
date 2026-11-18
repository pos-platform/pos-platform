using System.ComponentModel;
using PosCloud.Desktop.Services;

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
            RaisePricing();
        }
    }

    /// <summary>Aksiya chegirmasi — server bilan bir xil formula (<see cref="PromotionCalculator"/>).</summary>
    public decimal Discount => PromotionCalculator.LineDiscount(Product, _quantity);
    public bool HasDiscount => Discount > 0;
    public string DiscountLabel => HasDiscount ? $"Aksiya: −{Discount:N0}" : "";

    public decimal LineTotal => PromotionCalculator.LineTotal(Product, _quantity);
    public string LineTotalLabel => LineTotal.ToString("N0");

    /// <summary>Aksiyalar ro'yxati o'zgarganda narx ko'rinishini yangilaydi.</summary>
    public void RaisePricing()
    {
        Raise(nameof(Discount));
        Raise(nameof(HasDiscount));
        Raise(nameof(DiscountLabel));
        Raise(nameof(LineTotal));
        Raise(nameof(LineTotalLabel));
    }
    public string QtyLabel => _quantity.ToString("0.###");

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
