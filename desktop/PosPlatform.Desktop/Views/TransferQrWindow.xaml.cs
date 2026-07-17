using System.Windows;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>
/// O'tkazma (Payme/Click) to'lovi oynasi: mijozga skan qilish uchun QR ko'rsatadi.
/// Payme tab — sozlamalardagi shaxsiy havola (payme.uz/@...), skan qilinsa Payme'da
/// qabul qiluvchi avtomatik ochiladi. Karta tab — karta raqami (boshqa ilovalar uchun).
/// Pul tushgani avtomatik aniqlanmaydi — kassir o'z ilovasida ko'rib tasdiqlaydi.
/// </summary>
public partial class TransferQrWindow : Window
{
    /// <summary>Kassir tasdiqlagan paytda faol bo'lgan usul: "Karta" yoki "Payme" — savdoga yozib boriladi.</summary>
    public string SelectedSource { get; private set; } = "Karta";

    public TransferQrWindow(decimal total)
    {
        InitializeComponent();
        AmountText.Text = total.ToString("N0");

        // Default — karta raqami; karta kiritilmagan-u Payme havola bor bo'lsa Payme'dan boshlaymiz
        if (string.IsNullOrWhiteSpace(AppState.TransferCard) && !string.IsNullOrWhiteSpace(AppState.PaymeLink))
            ShowPayme();
        else
            ShowCard();
    }

    private void PaymeTab_Click(object sender, RoutedEventArgs e) => ShowPayme();
    private void CardTab_Click(object sender, RoutedEventArgs e) => ShowCard();

    private void ShowPayme()
    {
        SelectedSource = "Payme";
        SetActiveTab(payme: true);
        CardNumberText.Visibility = Visibility.Collapsed;

        var link = AppState.PaymeLink;
        if (string.IsNullOrWhiteSpace(link))
        {
            QrImageView.Source = null;
            HintText.Text = "Payme havola kiritilmagan.\n" +
                            "Sozlamalar → \"O'tkazma to'lovi\" bo'limida Payme shaxsiy havolangizni kiriting " +
                            "(Payme ilovasi → profil → o'tkazma qabul qilish).";
            return;
        }

        // QR telefon kamerasida ochilishi uchun havola https:// bilan boshlanishi kerak
        var url = link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? link : "https://" + link;
        QrImageView.Source = QrImage.Generate(url, pixelsPerModule: 10);
        HintText.Text = "Mijoz telefon kamerasi yoki Payme ilovasida QR'ni skan qiladi — " +
                        "o'tkazma oynasi sizning kartangiz bilan ochiladi.";
    }

    private void ShowCard()
    {
        SelectedSource = "Karta";
        SetActiveTab(payme: false);

        var card = AppState.TransferCard;
        if (string.IsNullOrWhiteSpace(card))
        {
            QrImageView.Source = null;
            CardNumberText.Visibility = Visibility.Collapsed;
            HintText.Text = "Karta raqami kiritilmagan.\n" +
                            "Sozlamalar → \"O'tkazma to'lovi\" bo'limida karta raqamingizni kiriting.";
            return;
        }

        var digits = new string(card.Where(char.IsDigit).ToArray());
        QrImageView.Source = QrImage.Generate(digits, pixelsPerModule: 10);
        CardNumberText.Text = FormatCard(digits);
        CardNumberText.Visibility = Visibility.Visible;
        HintText.Text = "Mijoz Click yoki istalgan bank ilovasida shu kartaga o'tkazadi " +
                        "(QR skan qilsa raqam chiqadi, yoki raqamni qo'lda teradi).";
    }

    private void SetActiveTab(bool payme)
    {
        var tab = (Style)FindResource("SrcTab");
        var active = (Style)FindResource("SrcTabActive");
        PaymeTab.Style = payme ? active : tab;
        CardTab.Style = payme ? tab : active;
    }

    /// <summary>8600123412341234 → 8600 1234 1234 1234</summary>
    private static string FormatCard(string digits) =>
        string.Join(" ", Enumerable.Range(0, (digits.Length + 3) / 4)
            .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4))));

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
