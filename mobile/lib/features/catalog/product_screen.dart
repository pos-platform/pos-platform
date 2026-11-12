import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../cart/cart_controller.dart';
import 'data/catalog_models.dart';
import 'data/catalog_repository.dart';
import 'store_selection.dart';
import 'widgets/common.dart';
import 'widgets/product_card.dart';

/// Mahsulot sahifasi (dizayn: Product.dc.html): rasm, qoldiq, nom, narx (eski narx chizilgan, −%), tavsif,
/// pastda miqdor va "Savatchaga · summa".
/// Keyinga (modelda yo'q): rasmlar karuseli, brend, "saqlash / yaroqlilik / ishlab chiqaruvchi" jadvali, sevimlilar.
class ProductScreen extends ConsumerWidget {
  const ProductScreen({super.key, required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final branchId = ref.watch(storeSelectionProvider).value;
    if (branchId == null) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    final args = (branchId: branchId, productId: productId);
    final detail = ref.watch(productDetailProvider(args));
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: switch (detail) {
        AsyncData(:final value) => _ProductBody(branchId: branchId, detail: value),
        AsyncError(:final error) => SafeArea(
            child: Column(children: [
              const Align(alignment: Alignment.centerLeft, child: BackButton()),
              Expanded(
                child: ErrorRetry(
                  message: ApiException.from(error).message,
                  onRetry: () => ref.invalidate(productDetailProvider(args)),
                ),
              ),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _ProductBody extends ConsumerStatefulWidget {
  const _ProductBody({required this.branchId, required this.detail});

  final String branchId;
  final ShopProductDetail detail;

  @override
  ConsumerState<_ProductBody> createState() => _ProductBodyState();
}

class _ProductBodyState extends ConsumerState<_ProductBody> {
  late double _qty;

  ShopProduct get _p => widget.detail.product;
  double get _step => CartState.stepFor(_p.isKg);

  @override
  void initState() {
    super.initState();
    final cart = ref.read(cartControllerProvider);
    final inCart = cart.branchId == widget.branchId ? cart.quantityOf(_p.id) : 0.0;
    _qty = inCart > 0 ? inCart : _step;
  }

  void _change(double delta) {
    final q = CartState.round(_qty + delta);
    if (q < _step || q > _p.available + 1e-9) return;
    setState(() => _qty = q);
  }

  void _addToCart() {
    final t = AppLocalizations.of(context);
    ref.read(cartControllerProvider.notifier).setQuantity(widget.branchId, _p, _qty);
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t.catAddedToCart)));
    if (context.canPop()) context.pop();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final p = _p;
    final d = widget.detail;
    final subtitle = [
      ?d.categoryName,
      if (d.barcode != null) t.catBarcode(d.barcode!),
    ].join(' · ');

    return Column(children: [
      Expanded(
        child: CustomScrollView(slivers: [
          SliverToBoxAdapter(
            child: Stack(children: [
              SizedBox(height: 360, width: double.infinity, child: ProductImage(url: p.imageUrl, iconSize: 72)),
              Positioned(
                top: MediaQuery.paddingOf(context).top + 12,
                left: 20,
                child: Material(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  child: InkWell(
                    borderRadius: BorderRadius.circular(14),
                    onTap: () => context.canPop() ? context.pop() : context.go('/home'),
                    child: SizedBox(
                      width: 44,
                      height: 44,
                      child: Icon(Icons.chevron_left, semanticLabel: t.catBack),
                    ),
                  ),
                ),
              ),
            ]),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(24, 24, 24, 24),
            sliver: SliverList.list(children: [
              Align(
                alignment: Alignment.centerLeft,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: p.inStock ? AppColors.primarySoft : AppColors.chipSurface,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    p.inStock ? t.catInStock(formatQty(context, p.available, p.isKg)) : t.catOutOfStock,
                    style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: p.inStock ? AppColors.primary : AppColors.textSecondary),
                  ),
                ),
              ),
              const SizedBox(height: 6),
              Text(p.name, style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w800, height: 1.2)),
              if (subtitle.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text(subtitle, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
              ],
              const SizedBox(height: 18),
              Wrap(
                crossAxisAlignment: WrapCrossAlignment.center,
                spacing: 10,
                runSpacing: 4,
                children: [
                  Text('${formatMoney(context, p.price)} / ${p.isKg ? t.catUnitKg : t.catUnitPiece}',
                      style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800)),
                  if (p.oldPrice != null)
                    Text(formatMoney(context, p.oldPrice!),
                        style: const TextStyle(
                            fontSize: 16, color: AppColors.textSecondary, decoration: TextDecoration.lineThrough)),
                  if (p.discountPercent != null && p.discountPercent! > 0)
                    DiscountBadge(percent: p.discountPercent!, fontSize: 13),
                ],
              ),
              if (d.description != null && d.description!.trim().isNotEmpty) ...[
                const SizedBox(height: 18),
                Text(d.description!, style: const TextStyle(fontSize: 15, height: 1.55, color: AppColors.textMuted)),
              ],
            ]),
          ),
        ]),
      ),
      Container(
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.borderSoft)),
        ),
        padding: EdgeInsets.fromLTRB(20, 16, 20, 16 + MediaQuery.paddingOf(context).bottom),
        child: Row(children: [
          QtyStepper(
            label: formatQty(context, _qty, p.isKg),
            height: AppTheme.controlHeight,
            buttonWidth: 48,
            fontSize: 18,
            background: AppColors.background,
            foreground: AppColors.text,
            onMinus: p.inStock && _qty - _step >= _step - 1e-9 ? () => _change(-_step) : null,
            onPlus: p.inStock && _qty + _step <= p.available + 1e-9 ? () => _change(_step) : null,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: FilledButton(
              onPressed: p.inStock ? _addToCart : null,
              child: FittedBox(
                fit: BoxFit.scaleDown,
                child: Text(p.inStock ? t.catAddToCartSum(formatMoney(context, p.price * _qty)) : t.catOutOfStock),
              ),
            ),
          ),
        ]),
      ),
    ]);
  }
}
