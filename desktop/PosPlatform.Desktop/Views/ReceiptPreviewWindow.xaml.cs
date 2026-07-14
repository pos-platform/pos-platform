using System.Threading;
using System.Windows;
using PosPlatform.Desktop.Models;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>Chekni chop etishdan oldin ko'rsatadigan oyna.</summary>
public partial class ReceiptPreviewWindow : Window
{
    private readonly ReceiptData _receipt;
    private readonly ReceiptPrinter _printer = new();
    private CancellationTokenSource? _pollCts;

    public ReceiptPreviewWindow(ReceiptData receipt)
    {
        InitializeComponent();
        _receipt = receipt;
        Render();

        // Onlayn savdo bo'lsa va QR hali yo'q bo'lsa — agent fiskalizatsiyasini kutamiz.
        // Fiskal chek (QR) kelgunicha "Yopish"/"Chop etish" tugmalarini yashirib turamiz.
        var expectingFiscal = _receipt.SaleId is Guid && string.IsNullOrEmpty(_receipt.QrCodeUrl) && AppState.IsOnline;
        ActionButtons.Visibility = expectingFiscal ? Visibility.Collapsed : Visibility.Visible;

        if (expectingFiscal)
        {
            Loaded += async (_, _) => await PollFiscalAsync();
            Closed += (_, _) => _pollCts?.Cancel();
        }
    }

    private void Render()
    {
        // Ko'rsatish uchun hujjat (chop etishda yangisi quriladi — bitta hujjat ikki joyda bo'lolmaydi).
        DocViewer.Document = ReceiptPrinter.BuildDocument(_receipt);
    }

    /// <summary>Backenddan chekning fiskal holatini so'rab turadi; QR kelganda chekni yangilaydi.</summary>
    private async Task PollFiscalAsync()
    {
        if (_receipt.SaleId is not Guid saleId) return;
        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;

        try
        {
            // ~20 soniya davomida har 2 sekundda tekshiramiz (agent poll oralig'i 5s).
            for (var i = 0; i < 10 && !token.IsCancellationRequested; i++)
            {
                try
                {
                    await Task.Delay(2000, token);
                    var sale = await AppState.Backend.GetSaleAsync(saleId, token);
                    if (sale?.Receipt is { IsRegistered: true } rc)
                    {
                        _receipt.QrCodeUrl = rc.QrCodeUrl;
                        _receipt.FiscalSign = rc.FiscalSign;
                        _receipt.FiscalModuleId = rc.FiscalModuleId;
                        _receipt.FiscalRegistrationNumber = rc.FiscalRegistrationNumber;
                        _receipt.FiscalReceiptNumber = rc.FiscalReceiptNumber;
                        Render();
                        return;
                    }
                }
                catch (OperationCanceledException) { return; }
                catch { /* tarmoq xatosi — keyingi urinishda qayta sinaymiz */ }
            }
        }
        finally
        {
            // QR keldi yoki timeout bo'ldi — ikkalasida ham tugmalarni ko'rsatamiz
            // (oyna yopilgan bo'lsa ham zararsiz).
            if (!token.IsCancellationRequested)
                ActionButtons.Visibility = Visibility.Visible;
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _printer.Print(_receipt, AppState.PrinterName);
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
