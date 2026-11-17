import 'package:material_ui/material_ui.dart';

import 'app_colors.dart';

/// Dizayn v3: matn — Manrope, sarlavha va narxlar — Unbounded; burchaklar 18 (tugma/input) va 24 (karta),
/// tugma/input balandligi 56px, kartalar chegarasiz oq.
class AppTheme {
  AppTheme._();

  static const radiusInput = 18.0;
  static const radiusCard = 24.0;
  static const controlHeight = 56.0;

  /// Sarlavha va narx shrifti.
  static const displayFont = 'Unbounded';

  /// Unbounded uslubi (dizaynda deyarli har doim w600, salgina siqiq harf oralig'i).
  static TextStyle display(double size, {Color? color, FontWeight weight = FontWeight.w600}) => TextStyle(
    fontFamily: displayFont,
    fontSize: size,
    fontWeight: weight,
    color: color ?? AppColors.text,
    letterSpacing: size >= 20 ? -0.02 * size : -0.01 * size,
    height: 1.2,
    fontFeatures: const [FontFeature.tabularFigures()],
  );

  static ThemeData light() {
    final base = ThemeData(
      useMaterial3: true,
      fontFamily: 'Manrope',
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppColors.primary,
        primary: AppColors.primary,
        surface: AppColors.surface,
        error: AppColors.danger,
      ),
      scaffoldBackgroundColor: AppColors.background,
    );

    final baseText = base.textTheme.apply(
      bodyColor: AppColors.text,
      displayColor: AppColors.text,
    );
    TextStyle? unb(TextStyle? s) =>
        s?.copyWith(fontFamily: displayFont, fontWeight: FontWeight.w600, letterSpacing: -0.3);
    final textTheme = baseText.copyWith(
      displayLarge: unb(baseText.displayLarge),
      displayMedium: unb(baseText.displayMedium),
      displaySmall: unb(baseText.displaySmall),
      headlineLarge: unb(baseText.headlineLarge),
      headlineMedium: unb(baseText.headlineMedium),
      headlineSmall: unb(baseText.headlineSmall),
      titleLarge: unb(baseText.titleLarge)?.copyWith(fontSize: 20),
    );

    final roundedInput = OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusInput),
      borderSide: BorderSide.none,
    );

    return base.copyWith(
      textTheme: textTheme,
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.background,
        foregroundColor: AppColors.text,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: true,
        titleTextStyle: display(16),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(controlHeight),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusInput)),
          textStyle: baseText.titleMedium?.copyWith(fontWeight: FontWeight.w800),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.text,
          backgroundColor: AppColors.surface,
          side: BorderSide.none,
          minimumSize: const Size.fromHeight(controlHeight),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusInput)),
          textStyle: baseText.titleMedium?.copyWith(fontWeight: FontWeight.w800),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          minimumSize: const Size(44, 48),
          textStyle: baseText.titleSmall?.copyWith(fontWeight: FontWeight.w700),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 17),
        hintStyle: const TextStyle(color: AppColors.faint, fontWeight: FontWeight.w500),
        border: roundedInput,
        enabledBorder: roundedInput,
        focusedBorder: roundedInput.copyWith(
          borderSide: BorderSide(color: AppColors.primary, width: 2),
        ),
      ),
      cardTheme: CardThemeData(
        color: AppColors.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusCard)),
      ),
      dividerTheme: const DividerThemeData(color: AppColors.line, thickness: 1, space: 1),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: AppColors.surface,
        dragHandleColor: AppColors.border,
        dragHandleSize: Size(36, 4),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(30))),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: AppColors.surface,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
        titleTextStyle: display(17),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: AppColors.ink,
        contentTextStyle: const TextStyle(
          fontFamily: 'Manrope',
          fontSize: 14,
          fontWeight: FontWeight.w700,
          color: Colors.white,
        ),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AppColors.surface,
        indicatorColor: AppColors.primarySoft,
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => textTheme.labelSmall?.copyWith(
            fontSize: 11,
            overflow: TextOverflow.ellipsis,
            fontWeight: states.contains(WidgetState.selected) ? FontWeight.w700 : FontWeight.w600,
            color: states.contains(WidgetState.selected) ? AppColors.primary : AppColors.textSecondary,
          ),
        ),
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            color: states.contains(WidgetState.selected) ? AppColors.primary : AppColors.textSecondary,
          ),
        ),
      ),
    );
  }
}
