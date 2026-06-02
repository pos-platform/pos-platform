using System.Windows;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>Chekni chop etishdan oldin ko'rsatadigan oyna.</summary>
public partial class ReceiptPreviewWindow : Window
{
    private readonly ReceiptData _receipt;
    private readonly ReceiptPrinter _printer = new();

    public ReceiptPreviewWindow(ReceiptData receipt)
    {
        InitializeComponent();
        _receipt = receipt;
        // Ko'rsatish uchun hujjat (chop etishda yangisi quriladi — bitta hujjat ikki joyda bo'lolmaydi).
        DocViewer.Document = ReceiptPrinter.BuildDocument(receipt);
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _printer.PrintDocument(
                ReceiptPrinter.BuildDocument(_receipt),
                AppState.PrinterName,
                "Chek " + _receipt.ReceiptNumber);
            Close();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("Chek chop etilmadi: " + ex.Message, "Xatolik",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
