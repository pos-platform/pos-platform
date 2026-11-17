import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../catalog/data/catalog_models.dart';
import '../../catalog/widgets/product_card.dart';

/// Bo'lim sarlavhasi: nom + ixtiyoriy "Hammasi".
class RailHeader extends StatelessWidget {
  const RailHeader({
    super.key,
    required this.title,
    this.onAll,
    this.trailing,
    this.hint,
    this.padding = const EdgeInsets.fromLTRB(16, 26, 4, 8),
  });

  final String title;
  final VoidCallback? onAll;
  final Widget? trailing;

  /// Sarlavha yonidagi och izoh (masalan "oldingi xaridlar").
  final String? hint;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) => Padding(
    padding: padding,
    child: ConstrainedBox(
      constraints: const BoxConstraints(minHeight: 36),
      child: Row(
        children: [
          Expanded(child: Text(title, style: AppTheme.display(18))),
          if (hint != null)
            Padding(
              padding: const EdgeInsets.only(right: 12),
              child: Text(hint!, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.faint)),
            ),
          ?trailing,
          if (onAll != null)
            TextButton(
              onPressed: onAll,
              style: TextButton.styleFrom(foregroundColor: AppColors.textSecondary),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(AppLocalizations.of(context).mkAll, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                  const SizedBox(width: 2),
                  const Icon(Icons.arrow_forward, size: 18),
                ],
              ),
            ),
        ],
      ),
    ),
  );
}

/// Gorizontal mahsulotlar lentasi. [ranked] — Top 10: kartochka chetida katta raqam.
class ProductRail extends StatelessWidget {
  const ProductRail({super.key, required this.branchId, required this.products, this.ranked = false});

  static const cardWidth = 156.0;
  static const height = 268.0;

  final String branchId;
  final List<ShopProduct> products;
  final bool ranked;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: height,
    child: ListView.separated(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: products.length,
      separatorBuilder: (_, _) => const SizedBox(width: 12),
      itemBuilder: (_, i) {
        final p = products[i];
        final card = SizedBox(
          width: cardWidth,
          child: ProductCard(branchId: branchId, product: p),
        );
        if (!ranked) return card;
        return _Ranked(rank: p.rank ?? i + 1, child: card);
      },
    ),
  );
}

/// Top 10: chapda kartochka ostidan chiqib turadigan katta konturli raqam.
class _Ranked extends StatelessWidget {
  const _Ranked({required this.rank, required this.child});

  final int rank;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final wide = rank >= 10;
    return SizedBox(
      width: ProductRail.cardWidth + (wide ? 50 : 30),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: 0,
            bottom: 52,
            child: Semantics(
              label: '#$rank',
              child: Stack(
                children: [
                  Text(
                    '$rank',
                    style: TextStyle(
                      fontSize: 88,
                      height: 1,
                      fontWeight: FontWeight.w800,
                      letterSpacing: -6,
                      foreground: Paint()
                        ..style = PaintingStyle.stroke
                        ..strokeWidth = 3
                        ..color = AppColors.primary,
                    ),
                  ),
                  Text(
                    '$rank',
                    style: TextStyle(
                      fontSize: 88,
                      height: 1,
                      fontWeight: FontWeight.w800,
                      letterSpacing: -6,
                      color: AppColors.primarySoft,
                    ),
                  ),
                ],
              ),
            ),
          ),
          Positioned(right: 0, top: 0, bottom: 0, child: child),
        ],
      ),
    );
  }
}
