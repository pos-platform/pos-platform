import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import '../marketing/data/marketing_repository.dart';
import '../marketing/favorites.dart';
import '../marketing/widgets/countdown.dart';
import '../marketing/widgets/product_rail.dart';
import 'data/catalog_models.dart';
import 'data/catalog_repository.dart';
import 'store_selection.dart';
import 'widgets/common.dart';
import 'widgets/product_card.dart';

/// Mahsulot sahifasi (dizayn: Product.dc.html): rasm, qoldiq, nom, narx (eski narx chizilgan, −%), tavsif,
/// pastda miqdor va "Savatchaga · summa".
/// Bosqich 9: ♥, aksiya ortga sanashi, "Kelganda xabar berish", "Bular bilan birga olishadi".
/// Keyinga (modelda yo'q): rasmlar karuseli, brend, "saqlash / yaroqlilik / ishlab chiqaruvchi" jadvali.
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
    _qty = inCart > 0 ? inCart : CartState.firstQuantity(_p);
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
              Positioned(
                top: MediaQuery.paddingOf(context).top + 12,
                right: 20,
                child: FavoriteButton(product: p, size: 24),
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
              if (p.discountEndsAt != null && (p.discountPercent ?? 0) > 0) ...[
                const SizedBox(height: 10),
                Align(
                  alignment: Alignment.centerLeft,
                  child: CountdownChip(endsAt: p.discountEndsAt!, label: (t, time) => t.mkDiscountEndsIn(time)),
                ),
              ],
              if (d.description != null && d.description!.trim().isNotEmpty) ...[
                const SizedBox(height: 18),
                Text(d.description!, style: const TextStyle(fontSize: 15, height: 1.55, color: AppColors.textMuted)),
              ],
            ]),
          ),
          SliverToBoxAdapter(child: _Related(branchId: widget.branchId, productId: p.id)),
          const SliverToBoxAdapter(child: SizedBox(height: 16)),
        ]),
      ),
      Container(
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.borderSoft)),
        ),
        padding: EdgeInsets.fromLTRB(20, 16, 20, 16 + MediaQuery.paddingOf(context).bottom),
        child: !p.inStock
            ? _NotifyButton(branchId: widget.branchId, detail: d)
            : Row(children: [
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

/// "Bular bilan birga olishadi" lentasi (bo'sh yoki xato bo'lsa ko'rinmaydi).
class _Related extends ConsumerWidget {
  const _Related({required this.branchId, required this.productId});

  final String branchId;
  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final items = ref.watch(relatedProductsProvider((branchId: branchId, productId: productId))).value;
    if (items == null || items.isEmpty) return const SizedBox.shrink();
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      RailHeader(title: AppLocalizations.of(context).mkRelated, padding: const EdgeInsets.fromLTRB(24, 0, 8, 8)),
      ProductRail(branchId: branchId, products: items),
    ]);
  }
}

/// Qoldiq yo'q: "Kelganda xabar berish" (obuna / bekor qilish). Kirmagan bo'lsa — kirish so'raladi.
class _NotifyButton extends ConsumerStatefulWidget {
  const _NotifyButton({required this.branchId, required this.detail});

  final String branchId;
  final ShopProductDetail detail;

  @override
  ConsumerState<_NotifyButton> createState() => _NotifyButtonState();
}

class _NotifyButtonState extends ConsumerState<_NotifyButton> {
  late bool _subscribed = widget.detail.isSubscribed ?? false;
  bool _busy = false;

  Future<void> _toggle() async {
    final t = AppLocalizations.of(context);
    if (!ref.read(authControllerProvider).isSignedIn) {
      ref.read(authControllerProvider.notifier).requestSignIn();
      return;
    }
    final messenger = ScaffoldMessenger.of(context);
    final value = !_subscribed;
    setState(() => _busy = true);
    try {
      await ref
          .read(marketingRepositoryProvider)
          .setStockSubscription(widget.branchId, widget.detail.product.id, value);
      if (!mounted) return;
      setState(() => _subscribed = value);
      messenger.showSnackBar(SnackBar(content: Text(value ? t.mkNotifyOn : t.mkNotifyOff)));
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(ApiException.from(e).message)));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final child = _busy
        ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
        : Text(_subscribed ? t.mkNotifySubscribed : t.mkNotifyMe);
    return _subscribed
        ? OutlinedButton.icon(
            onPressed: _busy ? null : _toggle,
            icon: const Icon(Icons.notifications_active_outlined),
            label: child,
          )
        : FilledButton.icon(
            onPressed: _busy ? null : _toggle,
            icon: const Icon(Icons.notifications_none),
            label: child,
          );
  }
}
