import 'package:flutter/rendering.dart' show OverflowBoxFit;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../catalog/data/catalog_repository.dart';
import '../catalog/widgets/common.dart';
import '../checkout/checkout_options.dart';
import '../marketing/data/marketing_repository.dart';
import '../marketing/widgets/product_rail.dart';
import 'cart_controller.dart';
import 'data/order_api.dart';

/// Savatcha (dizayn: Cart.dc.html). Summalar serverdan (`/quote`, debounce bilan) — ilova narxiga ishonilmaydi.
/// Promokod shu yerda kiritiladi (umumiy [checkoutOptionsProvider]) — chegirmani server hisoblaydi.
class CartScreen extends ConsumerWidget {
  const CartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final cart = ref.watch(cartControllerProvider);
    final branchId = cart.branchId;

    if (cart.isEmpty || branchId == null) {
      return Scaffold(
        body: SafeArea(
          child: Column(children: [
            _CartHeader(title: t.navCart, action: const _ListButton()),
            Expanded(
              child: Center(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(32, 0, 32, 60),
                  child: Column(mainAxisSize: MainAxisSize.min, children: [
                    Container(
                      width: 96,
                      height: 96,
                      decoration: BoxDecoration(color: AppColors.tintPeach, borderRadius: BorderRadius.circular(32)),
                      child: const Icon(Icons.shopping_basket_outlined, size: 42, color: AppColors.ink),
                    ),
                    const SizedBox(height: 18),
                    Text(t.cartEmpty, style: AppTheme.display(18)),
                    const SizedBox(height: 8),
                    Text(t.cartEmptyHint,
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 14, height: 1.5, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
                    const SizedBox(height: 22),
                    FilledButton(
                      style: FilledButton.styleFrom(minimumSize: const Size(0, 52), padding: const EdgeInsets.symmetric(horizontal: 28)),
                      onPressed: () => context.go('/home'),
                      child: Text(t.cartGoShopping),
                    ),
                  ]),
                ),
              ),
            ),
          ]),
        ),
      );
    }

    final store = ref.watch(storeProvider(branchId)).value;
    final type = store == null || store.deliveryEnabled ? FulfillmentType.delivery : FulfillmentType.pickup;
    final quoteAsync = ref.watch(cartQuoteProvider(type));
    final quote = quoteAsync.value;
    final loading = quoteAsync.isLoading;

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(children: [
          _CartHeader(
            title: t.navCart,
            action: Row(mainAxisSize: MainAxisSize.min, children: [
              const _ListButton(),
              TextButton(
            onPressed: () async {
              final ok = await showDialog<bool>(
                context: context,
                builder: (ctx) => AlertDialog(
                  title: Text(t.cartClearConfirm),
                  actions: [
                    TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.catCancel)),
                    TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.cartClear)),
                  ],
                ),
              );
              if (ok == true) ref.read(cartControllerProvider.notifier).clear();
            },
            child: Text(t.cartClear, style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w700)),
            ),
            ]),
          ),
          Expanded(
            child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
        children: [
          if (quote?.freeDeliveryRemaining != null && quote?.freeDeliveryFrom != null) ...[
            _FreeDeliveryProgress(remaining: quote!.freeDeliveryRemaining!, from: quote.freeDeliveryFrom!),
            const SizedBox(height: 12),
          ],
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
            decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(AppTheme.radiusCard)),
            child: Column(children: [
              for (final (i, line) in cart.lines.values.indexed) ...[
                if (i > 0) const Divider(height: 1, color: AppColors.line),
                _CartLineTile(line: line, quote: quote?.line(line.productId)),
              ],
            ]),
          ),
          _CartSuggestions(branchId: branchId, productIds: cart.lines.keys),
          const SizedBox(height: 12),
          _PromoSection(quote: quote, loading: loading),
          const SizedBox(height: 14),
          if (quoteAsync.hasError && quote == null)
            ErrorRetry(
              message: ApiException.from(quoteAsync.error!).message,
              onRetry: () => ref.invalidate(cartQuoteProvider(type)),
            )
          else
            _Totals(
              itemKinds: cart.count,
              itemsTotal: quote?.itemsTotal ?? cart.estimatedTotal,
              deliveryFee: type == FulfillmentType.delivery ? quote?.deliveryFee : null,
              promoDiscount: quote?.promoDiscount ?? 0,
              bonusUsed: quote?.bonusUsed ?? 0,
            ),
          if (quote != null && quote.minOrderShortfall > 0) ...[
            const SizedBox(height: 12),
            _Notice(t.cartMinOrderShortfall(
                formatMoney(context, quote.minOrderAmount), formatMoney(context, quote.minOrderShortfall))),
          ],
          if (quote?.message != null) ...[
            const SizedBox(height: 12),
            _Notice(quote!.message!),
          ],
        ],
            ),
          ),
          TotalBar(
            total: quote?.totalAmount,
            button: FilledButton(
            onPressed: quote == null || loading || !quote.canOrder
                ? null
                : () {
                    final auth = ref.read(authControllerProvider);
                    if (!auth.isSignedIn) {
                      ref.read(authControllerProvider.notifier).requestSignIn();
                      return;
                    }
                    context.push('/cart/checkout');
                  },
            child: loading && quote == null
                ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                : Text(t.cartCheckout),
            ),
          ),
        ]),
      ),
    );
  }
}

/// "Oilaviy ro'yxat" tugmasi (CONTRACT-11 §2).
class _ListButton extends StatelessWidget {
  const _ListButton();

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(right: 4),
        child: Material(
          color: AppColors.surface,
          shape: const StadiumBorder(),
          child: InkWell(
            customBorder: const StadiumBorder(),
            onTap: () => context.push('/cart/list'),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(10, 10, 14, 10),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                Icon(Icons.checklist_rounded, size: 18, color: AppColors.primary),
                const SizedBox(width: 6),
                Text(AppLocalizations.of(context).listTitle,
                    style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
              ]),
            ),
          ),
        ),
      );
}

/// Katta sarlavha (Unbounded 24) + ixtiyoriy amal.
class _CartHeader extends StatelessWidget {
  const _CartHeader({required this.title, this.action});

  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(20, 6, 8, 12),
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 46),
          child: Row(children: [
            Expanded(child: Text(title, style: AppTheme.display(24))),
            ?action,
          ]),
        ),
      );
}

/// Pastki oq panel: chapda "Jami" va summa (Unbounded), o'ngda asosiy tugma. Savatcha va rasmiylashtirishda.
class TotalBar extends StatelessWidget {
  const TotalBar({super.key, required this.total, required this.button});

  /// Null — server hisobi hali kelmagan.
  final double? total;
  final Widget button;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return DecoratedBox(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(top: BorderSide(color: AppColors.borderSoft)),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 12, 16, 12),
        child: Row(children: [
          Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(t.cartTotal,
                style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textSecondary)),
            Text(total == null ? '…' : formatMoney(context, total!), style: AppTheme.display(19)),
          ]),
          const SizedBox(width: 14),
          Expanded(child: button),
        ]),
      ),
    );
  }
}

/// "Qo'shib qo'ying" — savatdagilar bilan birga olinadiganlar (`cart-suggestions`). Bo'sh bo'lsa ko'rinmaydi.
/// ListView ichida 20 px chetlardan chiqib, butun kenglikda suriladi.
class _CartSuggestions extends ConsumerWidget {
  const _CartSuggestions({required this.branchId, required this.productIds});

  final String branchId;
  final Iterable<String> productIds;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ids = (productIds.toList()..sort()).join(',');
    final items = ref.watch(cartSuggestionsProvider((branchId: branchId, ids: ids))).value;
    if (items == null || items.isEmpty) return const SizedBox.shrink();
    final width = MediaQuery.sizeOf(context).width;
    return OverflowBox(
      fit: OverflowBoxFit.deferToChild,
      minWidth: width,
      maxWidth: width,
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        RailHeader(title: AppLocalizations.of(context).mkCartSuggestions, padding: const EdgeInsets.fromLTRB(20, 16, 8, 8)),
        ProductRail(branchId: branchId, products: items),
      ]),
    );
  }
}

class _FreeDeliveryProgress extends StatelessWidget {
  const _FreeDeliveryProgress({required this.remaining, required this.from});

  final double remaining;
  final double from;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final progress = from <= 0 ? 1.0 : ((from - remaining) / from).clamp(0.0, 1.0);
    final sum = formatMoney(context, remaining);
    final text = t.cartFreeDeliveryLeft(sum);
    final idx = text.indexOf(sum);
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      decoration: BoxDecoration(
        color: remaining <= 0 ? AppColors.tintGreen : AppColors.tintPeach,
        borderRadius: BorderRadius.circular(22),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          const Icon(Icons.local_shipping_outlined, size: 19),
          const SizedBox(width: 8),
          Expanded(child: remaining <= 0
          ? Text(t.cartFreeDeliveryReached, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700))
          : Text.rich(
            idx < 0
                ? TextSpan(text: text)
                : TextSpan(children: [
                    TextSpan(text: text.substring(0, idx)),
                    TextSpan(text: sum, style: const TextStyle(fontWeight: FontWeight.w800)),
                    TextSpan(text: text.substring(idx + sum.length)),
                  ]),
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.text),
          )),
        ]),
        const SizedBox(height: 10),
        ClipRRect(
          borderRadius: BorderRadius.circular(3),
          child: LinearProgressIndicator(
            value: progress,
            minHeight: 6,
            color: AppColors.ink,
            backgroundColor: AppColors.ink.withValues(alpha: 0.08),
          ),
        ),
      ]),
    );
  }
}

class _CartLineTile extends ConsumerWidget {
  const _CartLineTile({required this.line, required this.quote});

  final CartLine line;
  final QuoteLine? quote;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final cart = ref.read(cartControllerProvider.notifier);
    final problem = quote?.problem;
    final available = quote?.available ?? line.available;
    final canPlus = available == null || line.quantity + line.step <= available + 1e-9;
    final total = quote?.lineTotal ?? line.lineTotal;
    final unitPrice = quote?.unitPrice ?? line.price;

    final problemText = switch (problem) {
      'not_found' => t.cartProblemNotFound,
      'out_of_stock' => t.cartProblemOutOfStock,
      'not_enough' => t.cartProblemNotEnough(formatQty(context, quote!.available, line.isKg)),
      'invalid_quantity' => t.cartProblemInvalidQuantity,
      _ => null,
    };

    final last = line.quantity - line.step < line.step - 1e-9;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          GestureDetector(
            onTap: () => context.push('/home/product/${line.productId}'),
            child: Container(
              width: 62,
              height: 62,
              decoration: BoxDecoration(color: AppColors.tintFor(line.productId), borderRadius: BorderRadius.circular(18)),
              clipBehavior: Clip.antiAlias,
              child: ProductImage(url: line.imageUrl, iconSize: 24, fit: BoxFit.cover),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(line.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
              const SizedBox(height: 3),
              Text(formatMoney(context, total), style: AppTheme.display(14)),
              const SizedBox(height: 2),
              Text('${formatMoney(context, unitPrice)} / ${line.isKg ? t.catUnitKg : t.catUnitPiece}',
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.faint)),
            ]),
          ),
          const SizedBox(width: 8),
          QtyStepper(
            label: formatQty(context, line.quantity, line.isKg),
            height: 42,
            buttonWidth: 38,
            fontSize: 14,
            background: AppColors.chipSurface,
            foreground: AppColors.text,
            minusIcon: last ? Icons.delete_outline : Icons.remove,
            onMinus: () => cart.changeLine(line.productId, -line.step),
            onPlus: canPlus && problem == null ? () => cart.changeLine(line.productId, line.step) : null,
          ),
        ]),
        if (problemText != null) ...[
          const SizedBox(height: 8),
          Row(children: [
            const Icon(Icons.error_outline, size: 18, color: AppColors.danger),
            const SizedBox(width: 6),
            Expanded(child: Text(problemText, style: const TextStyle(fontSize: 13, color: AppColors.danger))),
            TextButton(
              onPressed: problem == 'not_enough' && quote!.available > 0
                  ? () => cart.setLineQuantity(line.productId, quote!.available)
                  : () => cart.remove(line.productId),
              child: Text(problem == 'not_enough' && quote!.available > 0 ? t.cartFixQuantity : t.cartRemove),
            ),
          ]),
        ],
      ]),
    );
  }
}

class _Totals extends StatelessWidget {
  const _Totals({
    required this.itemKinds,
    required this.itemsTotal,
    required this.deliveryFee,
    this.promoDiscount = 0,
    this.bonusUsed = 0,
  });

  final int itemKinds;
  final double itemsTotal;

  /// Null — olib ketish (qator ko'rsatilmaydi).
  final double? deliveryFee;
  final double promoDiscount;
  final double bonusUsed;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Widget row(String a, String b, {Color? color}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 5),
          child: DefaultTextStyle.merge(
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: color ?? AppColors.textSecondary),
            child: Row(children: [Expanded(child: Text(a)), Text(b)]),
          ),
        );
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8),
      child: Column(children: [
        row(t.cartItemsKinds(itemKinds), formatMoney(context, itemsTotal)),
        if (promoDiscount > 0) row(t.cartPromoDiscount, '−${formatMoney(context, promoDiscount)}', color: AppColors.primary),
        if (bonusUsed > 0) row(t.cartBonusUsed, '−${formatMoney(context, bonusUsed)}', color: AppColors.primary),
        if (deliveryFee != null)
          row(t.cartDelivery, deliveryFee == 0 ? t.cartFree : formatMoney(context, deliveryFee!)),
      ]),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(color: const Color(0xFFFDF0E6), borderRadius: BorderRadius.circular(14)),
        child: Row(children: [
          const Icon(Icons.info_outline, size: 20, color: AppColors.accent),
          const SizedBox(width: 10),
          Expanded(child: Text(text, style: const TextStyle(fontSize: 14, color: AppColors.text))),
        ]),
      );
}

/// "Promokod" + "Qo'llash". Qo'llangach — kod, chegirma yoki server aytgan xato (`promoMessage`) va olib tashlash.
class _PromoSection extends ConsumerStatefulWidget {
  const _PromoSection({required this.quote, required this.loading});

  final OrderQuote? quote;
  final bool loading;

  @override
  ConsumerState<_PromoSection> createState() => _PromoSectionState();
}

class _PromoSectionState extends ConsumerState<_PromoSection> {
  final _code = TextEditingController();

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  void _apply() {
    if (_code.text.trim().isEmpty) return;
    FocusScope.of(context).unfocus();
    ref.read(checkoutOptionsProvider.notifier).applyPromo(_code.text);
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final applied = ref.watch(checkoutOptionsProvider.select((o) => o.promoCode));
    final quote = widget.quote;

    final Widget body;
    if (applied == null) {
      body = Row(children: [
        Expanded(
          child: TextField(
            controller: _code,
            maxLength: 40,
            textCapitalization: TextCapitalization.characters,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => _apply(),
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700, letterSpacing: 0.6),
            decoration: InputDecoration(
              hintText: t.cartPromoTitle,
              counterText: '',
              filled: true,
              fillColor: AppColors.background,
              prefixIcon: const Icon(Icons.sell_outlined, size: 20, color: AppColors.textSecondary),
            ),
          ),
        ),
        const SizedBox(width: 10),
        SizedBox(
          height: 56,
          // Tema tugmani to'liq kenglikka cho'zadi (Size.fromHeight) — Row ichida cheksiz kenglik xatosi bermasligi uchun.
          child: FilledButton(
            style: FilledButton.styleFrom(
              minimumSize: const Size(0, 56),
              padding: const EdgeInsets.symmetric(horizontal: 18),
              backgroundColor: AppColors.ink,
            ),
            onPressed: _apply,
            child: Text(t.cartPromoApply),
          ),
        ),
      ]);
    } else {
      // Hisob yangilanayotganda natija hali eski bo'lishi mumkin — kutish belgisi.
      final pending = widget.loading || quote == null;
      final error = pending ? null : quote.promoMessage;
      final ok = !pending && error == null && quote.promoDiscount > 0;
      body = Row(children: [
        Icon(
          error != null ? Icons.error_outline : Icons.sell,
          color: error != null ? AppColors.danger : AppColors.primary,
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(applied, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
            if (pending)
              const Padding(
                padding: EdgeInsets.only(top: 4),
                child: SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2)),
              )
            else if (error != null)
              Text(error, style: const TextStyle(fontSize: 13, color: AppColors.danger))
            else if (ok)
              Text(t.cartPromoApplied(formatMoney(context, quote.promoDiscount)),
                  style: TextStyle(fontSize: 13, color: AppColors.primary, fontWeight: FontWeight.w600)),
          ]),
        ),
        TextButton(
          onPressed: () {
            _code.clear();
            ref.read(checkoutOptionsProvider.notifier).removePromo();
          },
          child: Text(t.cartPromoRemove),
        ),
      ]);
    }

    return Container(
      padding: EdgeInsets.fromLTRB(applied == null ? 10 : 16, 10, 6, 10),
      decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(AppTheme.radiusCard)),
      child: body,
    );
  }
}
