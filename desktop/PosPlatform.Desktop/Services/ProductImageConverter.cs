using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PosPlatform.Desktop.Services;

/// <summary>
/// Mahsulot rasm URL'ini kichraytirilgan (cheklangan o'lchamli), asinxron yuklanadigan
/// va URL bo'yicha keshlanadigan rasmга aylantiradi.
///
/// Nega kerak: mahsulot kartochkasi rasmni to'g'ridan-to'g'ri URL string'ga bog'lasa,
/// WPF har bir kartochka uchun rasmni to'liq o'lchamda yuklab dekod qiladi va
/// (virtualizatsiyada aylantirilganda) qayta-qayta yuklab oladi. Bu esa sekinlik keltiradi.
/// Bu konverter dekodni ~120px ga cheklaydi va bir marta yuklangan rasmni keshda saqlaydi.
/// </summary>
public class ProductImageConverter : IValueConverter
{
    // URL -> yuklangan rasm. Kartochkalar qayta yaratilganda takroran yuklab olinmaydi.
    private static readonly Dictionary<string, BitmapImage> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrEmpty(url))
            return null;

        if (Cache.TryGetValue(url, out var cached))
            return cached;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            // Kartochkadagi rasm maydoni kichik — to'liq o'lchamda dekod qilish shart emas.
            bmp.DecodePixelWidth = 120;
            // OnLoad'siz: masofaviy rasm UI'ni bloklamay, asinxron yuklab olinadi.
            bmp.UriSource = new Uri(url, UriKind.Absolute);
            bmp.EndInit();
            Cache[url] = bmp;
            return bmp;
        }
        catch
        {
            // Yaroqsiz URL yoki tarmoq xatosi — rasm ko'rsatilmaydi (kartochka baribir ishlaydi).
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
