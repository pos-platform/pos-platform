using System.ComponentModel;

namespace PosPlatform.Desktop.Models;

public class CartLine : INotifyPropertyChanged
{
    public ProductDto Product { get; }

    public CartLine(ProductDto product, decimal quantity)
    {
        Product = product;
        _quantity = quantity;
    }

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
