import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../cart/cart_controller.dart';
import '../data/catalog_models.dart';
import 'common.dart';

/// Katalogdagi mahsulot kartochkasi (dizayn: Main.dc.html — rasm, nom, birlik, narx, "+" yoki stepper).
class ProductCard extends ConsumerWidget {
  const ProductCard({super.key, required this.branchId, required this.product});

  final String branchId;
  final ShopProduct product;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final qty = ref.watch(cartControllerProvider.select((c) => c.branchId == branchId ? c.quantityOf(product.id) : 0.0));
    final cart = ref.read(cartControllerProvider.notifier);
    final canAdd = product.inStock && qty + CartState.stepFor(product.isKg) <= product.available + 1e-9;
    void open() => context.push('/home/product/${product.id}');

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        border: Border.all(color: AppColors.borderSoft),
      ),
      padding: const EdgeInsets.all(10),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Expanded(
          child: GestureDetector(
            onTap: open,
            child: Stack(children: [
              Positioned.fill(
                child: ClipRRect(borderRadius: BorderRadius.circular(12), child: ProductImage(url: product.imageUrl)),
              ),
              if (product.discountPercent != null && product.discountPercent! > 0)
                Positioned(left: 6, top: 6, child: DiscountBadge(percent: product.discountPercent!)),
            ]),
          ),
        ),
        const SizedBox(height: 8),
        GestureDetector(
          onTap: open,
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(product.name,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700, height: 1.3)),
            Text(unitLabel(context, product.isKg), style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
          ]),
        ),
        const SizedBox(height: 6),
        Row(children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              if (product.oldPrice != null)
                Text(formatMoney(context, product.oldPrice!),
                    style: const TextStyle(
                        fontSize: 11, color: AppColors.textSecondary, decoration: TextDecoration.lineThrough)),
              FittedBox(
                fit: BoxFit.scaleDown,
                alignment: Alignment.centerLeft,
                child: Text(formatMoney(context, product.price),
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
              ),
            ]),
          ),
          if (qty > 0)
            QtyStepper(
              label: formatQty(context, qty, false),
              buttonWidth: 32,
              onMinus: () => cart.decrement(branchId, product),
              onPlus: canAdd ? () => cart.increment(branchId, product) : null,
            )
          else
            Semantics(
              button: true,
              label: t.catAddToCart,
              child: Material(
                color: product.inStock ? AppColors.primary : AppColors.chipSurface,
                borderRadius: BorderRadius.circular(12),
                child: InkWell(
                  borderRadius: BorderRadius.circular(12),
                  onTap: canAdd ? () => cart.increment(branchId, product) : null,
                  child: SizedBox(
                    width: 44,
                    height: 44,
                    child: Icon(Icons.add, color: product.inStock ? Colors.white : AppColors.iconMuted),
                  ),
                ),
              ),
            ),
        ]),
      ]),
    );
  }
}

/// "−10%" belgisi.
class DiscountBadge extends StatelessWidget {
  const DiscountBadge({super.key, required this.percent, this.fontSize = 12});

  final double percent;
  final double fontSize;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(color: AppColors.accent, borderRadius: BorderRadius.circular(8)),
        child: Text('−${percent.round()}%',
            style: TextStyle(fontSize: fontSize, fontWeight: FontWeight.w800, color: Colors.white)),
      );
}
