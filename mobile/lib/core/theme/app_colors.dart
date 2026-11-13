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

  static const background = Color(0xFFF5F4EF);
  static const surface = Color(0xFFFFFFFF);
  static const text = Color(0xFF14162B);
  static const textSecondary = Color(0xFF5E6175);
  static const textMuted = Color(0xFF3A3D52);
  static const border = Color(0xFFE4E2DA);
  static const borderSoft = Color(0xFFECEAE3);
  static const chipSurface = Color(0xFFF1EFEA);
  static const danger = Color(0xFFB42318);
  static const accent = Color(0xFFC2410C);
  static const iconMuted = Color(0xFF9C9FAE);

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
