import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';

final _money = NumberFormat('#,##0.##', 'en_US');
final _qty = NumberFormat('0.###', 'en_US');

/// "14 000 so‘m" — minglar bo'sh joy bilan.
String formatMoney(BuildContext context, double amount) =>
    AppLocalizations.of(context).catSum(_money.format(amount).replaceAll(',', ' '));

/// Miqdor: "2" yoki "0.5 kg".
String formatQty(BuildContext context, double quantity, bool isKg) {
  final n = _qty.format(quantity);
  return isKg ? '$n ${AppLocalizations.of(context).catUnitKg}' : n;
}

/// Narx birligi: "1 kg" yoki "1 dona".
String unitLabel(BuildContext context, bool isKg) {
  final t = AppLocalizations.of(context);
  return '1 ${isKg ? t.catUnitKg : t.catUnitPiece}';
}

/// Mahsulot rasmi: nisbiy manzil API manziliga ulanadi; yo'q yoki xato bo'lsa — belgi.
class ProductImage extends StatelessWidget {
  const ProductImage({super.key, required this.url, this.iconSize = 36, this.fit = BoxFit.contain});

  final String? url;
  final double iconSize;
  final BoxFit fit;

  @override
  Widget build(BuildContext context) {
    final full = AppConfig.imageUrl(url);
    final placeholder = ColoredBox(
      color: AppColors.chipSurface,
      child: Center(child: Icon(Icons.image_outlined, size: iconSize, color: AppColors.textSecondary)),
    );
    if (full == null) return placeholder;
    return ColoredBox(
      color: AppColors.chipSurface,
      child: Image.network(
        full,
        fit: fit,
        errorBuilder: (_, _, _) => placeholder,
        loadingBuilder: (_, child, progress) => progress == null ? child : placeholder,
      ),
    );
  }
}

/// Miqdor tugmalari (−  2  +). Dizayn: yashil fon (katalog) yoki kulrang (mahsulot sahifasi).
class QtyStepper extends StatelessWidget {
  const QtyStepper({
    super.key,
    required this.label,
    required this.onMinus,
    required this.onPlus,
    this.height = 44,
    this.buttonWidth = 36,
    this.background,
    this.foreground,
    this.fontSize = 15,
    this.minusIcon = Icons.remove,
  });

  final String label;
  final VoidCallback? onMinus;
  final VoidCallback? onPlus;
  final double height;
  final double buttonWidth;
  /// null — brend rangining och soyasi.
  final Color? background;
  /// null — brend rangi.
  final Color? foreground;
  final double fontSize;

  /// Masalan oxirgi dona uchun — o'chirish belgisi.
  final IconData minusIcon;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Widget btn(IconData icon, VoidCallback? onTap, String tooltip) => Semantics(
          button: true,
          label: tooltip,
          child: InkWell(
            onTap: onTap,
            borderRadius: BorderRadius.circular(12),
            child: SizedBox(
              width: buttonWidth,
              height: height,
              child: Icon(icon, size: 20, color: onTap == null ? AppColors.iconMuted : (foreground ?? AppColors.primary)),
            ),
          ),
        );
    return Container(
      decoration: BoxDecoration(color: background ?? AppColors.primarySoft, borderRadius: BorderRadius.circular(height >= 56 ? 16 : 12)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        btn(minusIcon, onMinus, t.catDecrease),
        ConstrainedBox(
          constraints: const BoxConstraints(minWidth: 20),
          child: Text(label,
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: fontSize, fontWeight: FontWeight.w800, color: foreground ?? AppColors.primary)),
        ),
        btn(Icons.add, onPlus, t.catIncrease),
      ]),
    );
  }
}

/// Xato + "Qayta urinish".
class ErrorRetry extends StatelessWidget {
  const ErrorRetry({super.key, required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            const Icon(Icons.wifi_off_rounded, size: 40, color: AppColors.textSecondary),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary, fontSize: 15)),
            const SizedBox(height: 12),
            TextButton(onPressed: onRetry, child: Text(AppLocalizations.of(context).catRetry)),
          ]),
        ),
      );
}
