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
        appBar: AppBar(title: Text(t.navCart)),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.shopping_cart_outlined, size: 56, color: AppColors.iconMuted),
              const SizedBox(height: 12),
              Text(t.cartEmpty, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 6),
              Text(t.cartEmptyHint,
                  textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
              const SizedBox(height: 20),
              FilledButton(onPressed: () => context.go('/home'), child: Text(t.cartGoShopping)),
            ]),
          ),
        ),
      );
    }

    final store = ref.watch(storeProvider(branchId)).value;
    final type = store == null || store.deliveryEnabled ? FulfillmentType.delivery : FulfillmentType.pickup;
    final quoteAsync = ref.watch(cartQuoteProvider(type));
    final quote = quoteAsync.value;
    final loading = quoteAsync.isLoading;

    return Scaffold(
      appBar: AppBar(
        title: Text(t.navCart),
        actions: [
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
            child: Text(t.cartClear, style: const TextStyle(color: AppColors.danger)),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
        children: [
          if (quote?.freeDeliveryRemaining != null && quote?.freeDeliveryFrom != null) ...[
            _FreeDeliveryProgress(remaining: quote!.freeDeliveryRemaining!, from: quote.freeDeliveryFrom!),
            const SizedBox(height: 16),
          ],
          Container(
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(AppTheme.radiusCard),
              border: Border.all(color: AppColors.borderSoft),
            ),
            child: Column(children: [
              for (final (i, line) in cart.lines.values.indexed) ...[
                if (i > 0) const Divider(height: 1, color: AppColors.borderSoft),
                _CartLineTile(line: line, quote: quote?.line(line.productId)),
              ],
            ]),
          ),
          const SizedBox(height: 16),
          _PromoSection(quote: quote, loading: loading),
          const SizedBox(height: 16),
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
              total: quote?.totalAmount,
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
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
          child: FilledButton(
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
      ),
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
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.circular(16)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        if (remaining <= 0)
          Text(t.cartFreeDeliveryReached,
              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: AppColors.primary))
        else
          Text.rich(
            idx < 0
                ? TextSpan(text: text)
                : TextSpan(children: [
                    TextSpan(text: text.substring(0, idx)),
                    TextSpan(text: sum, style: const TextStyle(fontWeight: FontWeight.w800)),
                    TextSpan(text: text.substring(idx + sum.length)),
                  ]),
            style: const TextStyle(fontSize: 14, color: AppColors.text),
          ),
        const SizedBox(height: 10),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(
            value: progress,
            minHeight: 6,
            color: AppColors.primary,
            backgroundColor: AppColors.surface,
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

    return Padding(
      padding: const EdgeInsets.all(12),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: SizedBox(width: 64, height: 64, child: ProductImage(url: line.imageUrl, iconSize: 24)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(line.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
              Text('${formatMoney(context, unitPrice)} / ${line.isKg ? t.catUnitKg : t.catUnitPiece}',
                  style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
              const SizedBox(height: 2),
              Text(formatMoney(context, total), style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
            ]),
          ),
          QtyStepper(
            label: formatQty(context, line.quantity, line.isKg),
            buttonWidth: 34,
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
    required this.total,
  });

  final int itemKinds;
  final double itemsTotal;

  /// Null — olib ketish (qator ko'rsatilmaydi).
  final double? deliveryFee;
  final double promoDiscount;
  final double bonusUsed;

  /// Null — server hisobi hali kelmagan.
  final double? total;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Widget row(String a, String b, {bool bold = false}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 5),
          child: Row(children: [
            Expanded(
              child: Text(a,
                  style: TextStyle(
                      fontSize: bold ? 17 : 15,
                      fontWeight: bold ? FontWeight.w800 : FontWeight.w500,
                      color: bold ? AppColors.text : AppColors.textSecondary)),
            ),
            Text(b, style: TextStyle(fontSize: bold ? 17 : 15, fontWeight: bold ? FontWeight.w800 : FontWeight.w700)),
          ]),
        );
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(children: [
        row(t.cartItemsKinds(itemKinds), formatMoney(context, itemsTotal)),
        if (promoDiscount > 0) row(t.cartPromoDiscount, '−${formatMoney(context, promoDiscount)}'),
        if (bonusUsed > 0) row(t.cartBonusUsed, '−${formatMoney(context, bonusUsed)}'),
        if (deliveryFee != null)
          row(t.cartDelivery, deliveryFee == 0 ? t.cartFree : formatMoney(context, deliveryFee!)),
        const Divider(color: AppColors.borderSoft),
        row(t.cartTotal, total == null ? '…' : formatMoney(context, total!), bold: true),
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
            decoration: InputDecoration(
              hintText: t.cartPromoHint,
              counterText: '',
              prefixIcon: const Icon(Icons.local_offer_outlined, size: 20),
            ),
          ),
        ),
        const SizedBox(width: 10),
        SizedBox(
          height: 56,
          child: FilledButton.tonal(onPressed: _apply, child: Text(t.cartPromoApply)),
        ),
      ]);
    } else {
      // Hisob yangilanayotganda natija hali eski bo'lishi mumkin — kutish belgisi.
      final pending = widget.loading || quote == null;
      final error = pending ? null : quote.promoMessage;
      final ok = !pending && error == null && quote.promoDiscount > 0;
      body = Row(children: [
        Icon(
          error != null ? Icons.error_outline : Icons.local_offer,
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
                  style: const TextStyle(fontSize: 13, color: AppColors.primary, fontWeight: FontWeight.w600)),
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

    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(t.cartPromoTitle, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      const SizedBox(height: 10),
      if (applied == null)
        body
      else
        Container(
          padding: const EdgeInsets.fromLTRB(14, 10, 6, 10),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(AppTheme.radiusCard),
            border: Border.all(color: AppColors.borderSoft),
          ),
          child: body,
        ),
    ]);
  }
}
