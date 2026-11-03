using System.Threading;
using System.Windows;
using PosCloud.Desktop.Models;
using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Views;

/// <summary>Chek oynasi qaysi oqimdan ochilgani — kutish va ko'rinish shu bo'yicha farq qiladi.</summary>
public enum ReceiptViewMode
{
    /// <summary>To'lovdan keyin: fiskalizatsiya kelguncha kutiladi, kodlar ko'rsatiladi.</summary>
    Fiscal,

    /// <summary>Kunlik savdolar tarixi: kutilmaydi, savdo qanday saqlangan bo'lsa shundayligicha.</summary>
    History
}

/// <summary>Chekni chop etishdan oldin ko'rsatadigan oyna.</summary>
public partial class ReceiptPreviewWindow : Window
{
    private readonly ReceiptData _receipt;
    private readonly ReceiptPrinter _printer = new();
    private CancellationTokenSource? _pollCts;
    private readonly ReceiptViewMode _mode;

    /// <param name="mode">Oqim turi: to'lovdan keyin (fiskal kutiladi) yoki tarix.</param>
    public ReceiptPreviewWindow(ReceiptData receipt, ReceiptViewMode mode = ReceiptViewMode.Fiscal)
    {
        InitializeComponent();
        _mode = mode;

        // Oyna balandligi qat'iy (XAML'da Height=640) — QR kod kelganda ham o'zgarmaydi,
        // uzun chek ichkarida scroll qilinadi. Faqat juda past ekranlarda ish sohasiga sig'diramiz.
        if (Height > SystemParameters.WorkArea.Height - 24)
            Height = SystemParameters.WorkArea.Height - 24;

        _receipt = receipt;
        Render();

        // Darhol-nusxa va tarix rejimlarida fiskalizatsiya kutilmaydi: poll so'rovi yuborilmaydi,
        // tugmalar darhol ko'rinadi, "kutilmoqda" matni chiqmaydi. Tarix — o'zgarmas ko'rinish:
        // savdo qanday saqlangan bo'lsa shundayligicha qayta chop etiladi, yangi so'rov qilinmaydi.
        if (_mode != ReceiptViewMode.Fiscal)
        {
            ActionButtons.Visibility = Visibility.Visible;
            return;
        }

        // Tepadagi "To'lash" tugmasidan: QR hali yo'q bo'lsa agent fiskalizatsiyasini fonda
        // kutamiz va QR kelganda chekni yangilaymiz. QR kelguncha tugmalar yashirin turadi.
        // Tashqi fiskal qurilma QR'ni o'zi chiqarsa, POS fiskalizatsiyani kutmaydi.
        var expectingFiscal = !_receipt.ExternalFiscalDevice
                              && _receipt.SaleId is Guid && string.IsNullOrEmpty(_receipt.QrCodeUrl) && AppState.IsOnline;
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
        // "Fiskalizatsiya kutilmoqda..." matni faqat fiskal oqimda ma'noli: tarixda hech narsa
        // kutilmaydi, Shtrix/MXIK kodlari esa asl chekdagidek qoladi.
        DocViewer.Document = ReceiptPrinter.BuildDocument(_receipt,
            showFiscalPending: _mode == ReceiptViewMode.Fiscal, showCodes: true);
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
            _printer.Print(_receipt, AppState.PrinterName, showCodes: true);
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
