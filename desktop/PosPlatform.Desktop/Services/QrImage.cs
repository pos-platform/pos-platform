using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace PosPlatform.Desktop.Services;

/// <summary>Matndan (QR havola) WPF rasm (BitmapSource) yasaydi — chekda ko'rsatish uchun.</summary>
public static class QrImage
{
    /// <summary>QR kodni PNG sifatida yaratib, WPF BitmapSource qaytaradi.</summary>
    public static BitmapSource? Generate(string? text, int pixelsPerModule = 6)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(pixelsPerModule);

        var image = new BitmapImage();
        using (var ms = new MemoryStream(bytes))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
        }
        image.Freeze(); // boshqa thread'dan ham ishlatish mumkin bo'lsin
        return image;
    }
}
