using System.Windows;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>
/// O'tkazma to'lovi oynasi: mijozga kartaga o'tkazish uchun QR va karta raqamini ko'rsatadi.
/// Karta raqami sozlamalardagi TransferCard'dan olinadi. Mijoz Payme/Click/bank ilovasida
/// shu kartaga o'tkazadi. Pul tushgani avtomatik aniqlanmaydi — kassir o'z ilovasida ko'rib
/// "Pul tushdi" tugmasini bosadi.
/// </summary>
public partial class TransferQrWindow : Window
{
    public TransferQrWindow(decimal total)
    {
        InitializeComponent();
        AmountText.Text = total.ToString("N0");
        ShowCard();
    }

    private void ShowCard()
    {
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
        HintText.Text = "Mijoz Payme, Click yoki istalgan bank ilovasida shu kartaga o'tkazadi " +
                        "(QR skan qilsa raqam chiqadi, yoki raqamni qo'lda teradi).";
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
