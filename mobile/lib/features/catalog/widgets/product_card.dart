import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../cart/cart_controller.dart';
import '../../marketing/favorites.dart';
import '../data/catalog_models.dart';
import 'common.dart';

/// Mahsulot kartochkasi (dizayn v3): rangli karta, kvadrat rasm, chapda chegirma, o'ngda ♥,
/// pastki o'ng burchakda qizil "+" (savatda bo'lsa — oq "− N +" kapsula); ostida narx (Unbounded), nom, birlik.
/// [compact] — 3 ustunli grid uchun kichik variant.
class ProductCard extends ConsumerWidget {
  const ProductCard({super.key, required this.branchId, required this.product, this.compact = false});

  final String branchId;
  final ShopProduct product;
  final bool compact;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final p = product;
    final c = compact;
    final btn = c ? 32.0 : 42.0;
    final qty = ref.watch(cartControllerProvider.select((c) => c.branchId == branchId ? c.quantityOf(p.id) : 0.0));
    final cart = ref.read(cartControllerProvider.notifier);
    final canAdd = p.inStock &&
        (qty > 0 ? qty + CartState.stepFor(p.isKg) : CartState.firstQuantity(p)) <= p.available + 1e-9;
    final discounted = (p.discountPercent ?? 0) > 0;
    void open() => context.push('/home/product/${p.id}');

    return Container(
      decoration: BoxDecoration(color: AppColors.tintFor(p.id), borderRadius: BorderRadius.circular(c ? 18 : 26)),
      padding: c ? const EdgeInsets.fromLTRB(5, 5, 5, 8) : const EdgeInsets.fromLTRB(8, 8, 8, 12),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Expanded(
          child: Stack(children: [
            Positioned.fill(
              child: GestureDetector(
                onTap: open,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(c ? 14 : 20),
                  child: Opacity(
                    opacity: p.inStock ? 1 : 0.5,
                    child: ProductImage(url: p.imageUrl, iconSize: c ? 30 : 44, fit: BoxFit.cover, onTint: true),
                  ),
                ),
              ),
            ),
            if (discounted)
              Positioned(left: 4, top: 4, child: DiscountBadge(percent: p.discountPercent!, fontSize: c ? 9 : 11)),
            Positioned(right: 0, top: 0, child: FavoriteButton(product: p, size: c ? 13 : 17)),
            if (qty > 0)
              Positioned(
                left: 2,
                right: 2,
                bottom: 2,
                child: Container(
                  height: btn,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(btn / 2),
                    boxShadow: [BoxShadow(color: AppColors.ink.withValues(alpha: 0.12), blurRadius: 16, offset: const Offset(0, 6))],
                  ),
                  child: Row(children: [
                    _RoundIcon(size: btn, icon: Icons.remove, label: t.catDecrease, onTap: () => cart.decrement(branchId, p)),
                    Expanded(
                      child: FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Text(formatQty(context, qty, p.isKg), style: AppTheme.display(c ? 12 : 14)),
                      ),
                    ),
                    _RoundIcon(
                        size: btn, icon: Icons.add, label: t.catIncrease, onTap: canAdd ? () => cart.increment(branchId, p) : null),
                  ]),
                ),
              )
            else if (p.inStock)
              Positioned(
                right: 2,
                bottom: 2,
                child: Semantics(
                  button: true,
                  label: t.catAddToCart,
                  child: Material(
                    color: canAdd ? AppColors.primary : AppColors.segment,
                    shape: const CircleBorder(),
                    elevation: canAdd ? 4 : 0,
                    shadowColor: AppColors.primary.withValues(alpha: 0.5),
                    child: InkWell(
                      customBorder: const CircleBorder(),
                      onTap: canAdd ? () => cart.increment(branchId, p) : null,
                      child: SizedBox(
                        width: btn,
                        height: btn,
                        child: Icon(Icons.add, size: c ? 18 : 23, color: canAdd ? Colors.white : AppColors.faint),
                      ),
                    ),
                  ),
                ),
              ),
          ]),
        ),
        SizedBox(height: c ? 6 : 8),
        GestureDetector(
          onTap: open,
          behavior: HitTestBehavior.opaque,
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: c ? 3 : 6),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(crossAxisAlignment: CrossAxisAlignment.baseline, textBaseline: TextBaseline.alphabetic, children: [
                Flexible(
                  child: FittedBox(
                    fit: BoxFit.scaleDown,
                    alignment: Alignment.centerLeft,
                    child: Text(formatMoney(context, p.price),
                        style: AppTheme.display(c ? 12 : 15, color: discounted ? AppColors.primary : AppColors.text)),
                  ),
                ),
                if (p.oldPrice != null) ...[
                  SizedBox(width: c ? 4 : 6),
                  Flexible(
                    child: Text(formatMoney(context, p.oldPrice!),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                            fontSize: c ? 10 : 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.text.withValues(alpha: 0.4),
                            decoration: TextDecoration.lineThrough)),
                  ),
                ],
              ]),
              SizedBox(height: c ? 2 : 3),
              Text(p.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: c ? 12 : 14, fontWeight: FontWeight.w700, height: c ? 1.25 : 1.3)),
              Text(p.inStock ? unitLabel(context, p.isKg) : t.catOutOfStock,
                  style: TextStyle(
                      fontSize: c ? 10.5 : 12, fontWeight: FontWeight.w600, color: AppColors.text.withValues(alpha: 0.45))),
            ]),
          ),
        ),
      ]),
    );
  }
}

class _RoundIcon extends StatelessWidget {
  const _RoundIcon({required this.icon, required this.label, required this.onTap, this.size = 42});

  final IconData icon;
  final String label;
  final VoidCallback? onTap;
  final double size;

  @override
  Widget build(BuildContext context) => Semantics(
        button: true,
        label: label,
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: onTap,
          child: SizedBox(
            width: size,
            height: size,
            child: Icon(icon, size: size * 0.48, color: onTap == null ? AppColors.faint : AppColors.primary),
          ),
        ),
      );
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
