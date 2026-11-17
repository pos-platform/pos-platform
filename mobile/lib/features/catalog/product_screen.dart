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
      backgroundColor: AppColors.background,
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
    final top = MediaQuery.paddingOf(context).top;
    final discounted = (p.discountPercent ?? 0) > 0;
    final subtitle = [unitLabel(context, p.isKg), ?d.categoryName].join(' · ');

    Widget roundButton({required Widget child, required VoidCallback onTap, String? label}) => Semantics(
          button: true,
          label: label,
          child: Material(
            color: Colors.white.withValues(alpha: 0.8),
            shape: const CircleBorder(),
            clipBehavior: Clip.antiAlias,
            child: InkWell(onTap: onTap, child: SizedBox(width: 44, height: 44, child: Center(child: child))),
          ),
        );

    return Column(children: [
      Expanded(
        child: CustomScrollView(slivers: [
          // Dizayn v3: rangli tepa (rasm markazda), ustida orqaga / ♥, pastda belgilar.
          SliverToBoxAdapter(
            child: Container(
              height: 330 + top,
              color: AppColors.tintFor(p.id),
              padding: EdgeInsets.only(top: top),
              child: Stack(children: [
                Center(
                  child: Padding(
                    padding: const EdgeInsets.only(top: 10, bottom: 26),
                    child: AspectRatio(
                      aspectRatio: 1,
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(28),
                        child: ProductImage(url: p.imageUrl, iconSize: 72, fit: BoxFit.cover, onTint: true),
                      ),
                    ),
                  ),
                ),
                Positioned(
                  left: 16,
                  top: 6,
                  child: roundButton(
                    label: t.catBack,
                    onTap: () => context.canPop() ? context.pop() : context.go('/home'),
                    child: const Icon(Icons.arrow_back, size: 22),
                  ),
                ),
                Positioned(right: 16, top: 6, child: FavoriteButton(product: p, size: 21)),
                Positioned(
                  left: 16,
                  bottom: 34,
                  child: Wrap(spacing: 6, children: [
                    if (discounted) DiscountBadge(percent: p.discountPercent!, fontSize: 12),
                    _Tag(
                      label: p.inStock ? t.catInStock(formatQty(context, p.available, p.isKg)) : t.catOutOfStock,
                      background: Colors.white,
                      color: p.inStock ? AppColors.text : AppColors.primary,
                    ),
                  ]),
                ),
              ]),
            ),
          ),
          SliverToBoxAdapter(
            child: Transform.translate(
              offset: const Offset(0, -22),
              child: Container(
                decoration: const BoxDecoration(
                  color: AppColors.background,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(26)),
                ),
                padding: const EdgeInsets.fromLTRB(20, 22, 20, 0),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(p.name, style: AppTheme.display(21).copyWith(height: 1.25)),
                  const SizedBox(height: 6),
                  Text(subtitle,
                      style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
                  const SizedBox(height: 18),
                  Wrap(
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: 10,
                    runSpacing: 4,
                    children: [
                      Text(formatMoney(context, p.price),
                          style: AppTheme.display(28, color: discounted ? AppColors.primary : AppColors.text)),
                      if (p.oldPrice != null)
                        Text(formatMoney(context, p.oldPrice!),
                            style: const TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w600,
                                color: AppColors.faint,
                                decoration: TextDecoration.lineThrough)),
                    ],
                  ),
                  if (p.discountEndsAt != null && discounted) ...[
                    const SizedBox(height: 10),
                    CountdownChip(endsAt: p.discountEndsAt!, label: (t, time) => t.mkDiscountEndsIn(time)),
                  ],
                  if (d.description != null && d.description!.trim().isNotEmpty) ...[
                    const SizedBox(height: 18),
                    Text(d.description!,
                        style: const TextStyle(fontSize: 15, height: 1.6, fontWeight: FontWeight.w500, color: AppColors.textMuted)),
                  ],
                  if (d.barcode != null) ...[
                    const SizedBox(height: 14),
                    Row(children: [
                      const Icon(Icons.qr_code_scanner, size: 18, color: AppColors.textSecondary),
                      const SizedBox(width: 8),
                      Text(t.catBarcode(d.barcode!),
                          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.textSecondary)),
                    ]),
                  ],
                ]),
              ),
            ),
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
        padding: EdgeInsets.fromLTRB(16, 12, 16, 12 + MediaQuery.paddingOf(context).bottom),
        child: !p.inStock
            ? _NotifyButton(branchId: widget.branchId, detail: d)
            : Row(children: [
                QtyStepper(
                  label: formatQty(context, _qty, p.isKg),
                  height: AppTheme.controlHeight,
                  buttonWidth: 46,
                  fontSize: 15,
                  background: AppColors.chipSurface,
                  foreground: AppColors.text,
                  onMinus: _qty - _step >= _step - 1e-9 ? () => _change(-_step) : null,
                  onPlus: _qty + _step <= p.available + 1e-9 ? () => _change(_step) : null,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: FilledButton(
                    onPressed: _addToCart,
                    child: FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(t.catAddToCartSum(formatMoney(context, p.price * _qty))),
                    ),
                  ),
                ),
              ]),
      ),
    ]);
  }
}

class _Tag extends StatelessWidget {
  const _Tag({required this.label, required this.background, required this.color});

  final String label;
  final Color background;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(10)),
        child: Text(label, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: color)),
      );
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
      RailHeader(title: AppLocalizations.of(context).mkRelated, padding: const EdgeInsets.fromLTRB(20, 0, 4, 8)),
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
