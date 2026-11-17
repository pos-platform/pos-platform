import 'package:material_ui/material_ui.dart';

/// Dizayn maketidagi ranglar (https://claude.ai/artifact/H9qqdTExBhCdqrnFEYfGva).
///
/// Asosiy (brend) rang do'kon rejimida o'zgaradi (`applyBrand`) — shuning uchun `primary*` const emas.
/// Ularni `const` ifodalar ichida ishlatmang.
class AppColors {
  AppColors._();

  /// Standart (Xo‘ja Market) qizil.
  static const defaultPrimary = Color(0xFFD81F26);

  static Color _primary = defaultPrimary;
  static Color _primaryPressed = const Color(0xFFB71A20);
  static Color _primarySoft = const Color(0xFFFAE4E5);
  static Color _onPrimarySoft = const Color(0xFFF8D7D8);

  static Color get primary => _primary;
  static Color get primaryPressed => _primaryPressed;
  static Color get primarySoft => _primarySoft;

  /// Asosiy rang fonidagi ikkilamchi (och) matn.
  static Color get onPrimarySoft => _onPrimarySoft;

  // v3 dizayn tokenlari (Market Mobile v3).
  static const background = Color(0xFFF6F4EF);
  static const surface = Color(0xFFFFFFFF);
  static const text = Color(0xFF1A1B26);
  static const textSecondary = Color(0xFF6E707C);
  static const textMuted = Color(0xFF4B4D5A);
  static const border = Color(0xFFE3DFD8);
  static const borderSoft = Color(0xFFEEEBE5);
  static const chipSurface = Color(0xFFF2F0EB);
  static const danger = Color(0xFFB42318);
  static const accent = Color(0xFFC2410C);
  static const iconMuted = Color(0xFFA6A7AF);

  /// To'q "siyoh" — pastki menyu, ikkilamchi to'q tugmalar, bonus karta.
  static const ink = Color(0xFF1A1B26);

  /// Juda och matn (placeholder, ikkilamchi izoh).
  static const faint = Color(0xFFA6A7AF);

  /// Kartalar ichidagi ajratuvchi chiziq.
  static const line = Color(0xFFF2F0EB);

  /// Segmentli tanlov (yetkazish/olib ketish) foni.
  static const segment = Color(0xFFECE9E3);

  /// Bo'lim/kategoriya ranglari (dizayndagi oklch tintlar, sRGB'ga o'tkazilgan).
  static const tintGreen = Color(0xFFE2F0D5);
  static const tintBlue = Color(0xFFE0ECF6);
  static const tintPeach = Color(0xFFF8E8DA);
  static const tintAqua = Color(0xFFDDEFF1);
  static const tintYellow = Color(0xFFF8F0CC);
  static const tintRose = Color(0xFFF8E4E3);
  static const tintSand = Color(0xFFF0E9DA);
  static const tintLilac = Color(0xFFEDE6F4);
  static const tints = [tintGreen, tintBlue, tintPeach, tintAqua, tintYellow, tintRose, tintSand, tintLilac];

  /// Kalit (masalan mahsulot id) bo'yicha doimiy tint — bir mahsulot hamma joyda bir xil rangda.
  static Color tintFor(String key) {
    var h = 0;
    for (final c in key.codeUnits) {
      h = (h * 31 + c) & 0x7fffffff;
    }
    return tints[h % tints.length];
  }

  /// `#RRGGBB` (yoki `RRGGBB`) → Color. Noto'g'ri qiymat — null.
  static Color? parseHex(String? hex) {
    if (hex == null) return null;
    final h = hex.trim().replaceFirst('#', '');
    if (!RegExp(r'^[0-9a-fA-F]{6}$').hasMatch(h)) return null;
    return Color(0xFF000000 | int.parse(h, radix: 16));
  }

  /// Brend rangini qo'llaydi (null — standart qizil). Oq matn o'qilishi uchun rang yetarlicha to'q
  /// bo'lguncha (kontrast ≥ 4.5:1) to'qlashtiriladi; bosilgan va och soyalar shundan hosil qilinadi.
  static void applyBrand(Color? brand) {
    if (brand == null || brand.toARGB32() == defaultPrimary.toARGB32()) {
      _primary = defaultPrimary;
      _primaryPressed = const Color(0xFFB71A20);
      _primarySoft = const Color(0xFFFAE4E5);
      _onPrimarySoft = const Color(0xFFF8D7D8);
      return;
    }
    var hsl = HSLColor.fromColor(brand.withValues(alpha: 1));
    while (_contrastWithWhite(hsl.toColor()) < 4.5 && hsl.lightness > 0.05) {
      hsl = hsl.withLightness((hsl.lightness - 0.03).clamp(0.0, 1.0));
    }
    final primary = hsl.toColor();
    _primary = primary;
    _primaryPressed = hsl.withLightness((hsl.lightness - 0.07).clamp(0.0, 1.0)).toColor();
    _primarySoft = Color.lerp(Colors.white, primary, 0.12)!;
    _onPrimarySoft = Color.lerp(primary, Colors.white, 0.82)!;
  }

  static double _contrastWithWhite(Color c) => 1.05 / (c.computeLuminance() + 0.05);
}
