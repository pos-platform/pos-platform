using System.Windows;
using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Views;

/// <summary>
/// O'tkazma to'lovi oynasi: mijozga kartaga o'tkazish uchun plastik karta ko'rinishida
/// karta raqamini ko'rsatadi. Karta raqami sozlamalardagi TransferCard'dan olinadi.
/// Mijoz shu kartaga o'tkazadi. Pul tushgani avtomatik aniqlanmaydi — kassir o'z
/// ilovasida ko'rib "Pul tushdi" tugmasini bosadi.
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
        ShowExpiry();
        ShowBranding();
        HolderText.Text = (AppState.TransferCardHolder ?? "").ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(card))
        {
            CardNumberText.Text = "0000 0000 0000 0000";
            HintText.Text = "Karta raqami kiritilmagan.\n" +
                            "Sozlamalar → \"O'tkazma to'lovi\" bo'limida karta raqamingizni kiriting.";
            return;
        }

        var digits = new string(card.Where(char.IsDigit).ToArray());
        CardNumberText.Text = FormatCard(digits);
        HintText.Text = "Mijoz yuqoridagi kartaga to'lov summasini o'tkazadi.";
    }

    private void ShowBranding()
    {
        var shortCode = (AppState.TransferCardBankShort ?? "").Trim();
        var bankName = (AppState.TransferCardBank ?? "").Trim();
        var cardType = (AppState.TransferCardType ?? "").Trim();

        if (string.IsNullOrEmpty(shortCode))
        {
            BankShortBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            BankShortText.Text = shortCode.ToUpperInvariant();
            BankShortBox.Visibility = Visibility.Visible;
        }

        // Bank nomi bo'sh bo'lsa va belgi ham yo'q bo'lsa — umumiy sarlavha
        BankNameText.Text = !string.IsNullOrEmpty(bankName)
            ? bankName.ToUpperInvariant()
            : (string.IsNullOrEmpty(shortCode) ? "BANK KARTASI" : "");

        CardTypeText.Text = cardType.ToUpperInvariant();
    }

    private void ShowExpiry()
    {
        var expiry = AppState.TransferCardExpiry;
        if (string.IsNullOrWhiteSpace(expiry))
        {
            ExpiryPanel.Visibility = Visibility.Collapsed;
            return;
        }
        ExpiryText.Text = expiry.Trim();
        ExpiryPanel.Visibility = Visibility.Visible;
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
