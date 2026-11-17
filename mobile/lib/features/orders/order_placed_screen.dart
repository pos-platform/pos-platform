import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../catalog/widgets/common.dart';
import 'data/order_models.dart';
import 'data/orders_repository.dart';
import 'widgets/order_format.dart';

/// Buyurtma berilgandan keyin (dizayn v3): qizil ✓, "Buyurtma qabul qilindi", chek ko'rinishidagi karta
/// (do'kon, raqam, sana, qatorlar, chegirmalar, jami, to'lov) va "Bosh sahifa" / "Kuzatish".
class OrderPlacedScreen extends ConsumerWidget {
  const OrderPlacedScreen({super.key, required this.orderId});

  final String orderId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(orderDetailProvider(orderId));
    return Scaffold(
      body: SafeArea(
        child: Column(children: [
          Expanded(
            child: switch (async) {
              AsyncData(:final value) => _Body(order: value),
              AsyncError(:final error) => ErrorRetry(
                  message: ApiException.from(error).message,
                  onRetry: () => ref.invalidate(orderDetailProvider(orderId)),
                ),
              _ => const Center(child: CircularProgressIndicator()),
            },
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            child: Row(children: [
              Expanded(
                flex: 10,
                child: FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: AppColors.surface, foregroundColor: AppColors.text),
                  onPressed: () => context.go('/home'),
                  child: Text(t.orderPlacedHome),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                flex: 13,
                child: FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: AppColors.ink),
                  onPressed: () => context.go('/orders/$orderId'),
                  child: Text(t.orderPlacedTrack),
                ),
              ),
            ]),
          ),
        ]),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final o = order;
    final from = o.estimatedDeliveryFrom, to = o.estimatedDeliveryTo;
    final eta = from != null && to != null
        ? t.ordersEta('${OrderFormat.time(from.toLocal())}–${OrderFormat.time(to.toLocal())}')
        : null;
    final items = o.items.where((i) => !i.isRemoved).toList();

    Widget row(String a, String b, {Color? color}) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 3.5),
          child: DefaultTextStyle.merge(
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: color ?? AppColors.textSecondary),
            child: Row(children: [Expanded(child: Text(a)), Text(b)]),
          ),
        );

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 18, 20, 20),
      children: [
        Center(
          child: TweenAnimationBuilder<double>(
            tween: Tween(begin: 0.6, end: 1),
            duration: const Duration(milliseconds: 450),
            curve: Curves.elasticOut,
            builder: (_, v, child) => Transform.scale(scale: v, child: child),
            child: Container(
              width: 68,
              height: 68,
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(24),
                boxShadow: [
                  BoxShadow(color: AppColors.primary.withValues(alpha: 0.3), blurRadius: 24, offset: const Offset(0, 10)),
                ],
              ),
              child: const Icon(Icons.check_rounded, size: 36, color: Colors.white),
            ),
          ),
        ),
        const SizedBox(height: 12),
        Text(t.orderPlacedTitle, textAlign: TextAlign.center, style: AppTheme.display(20)),
        if (eta != null) ...[
          const SizedBox(height: 6),
          Text(eta,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
        ],
        const SizedBox(height: 20),
        DecoratedBox(
          decoration: BoxDecoration(
            boxShadow: [BoxShadow(color: AppColors.ink.withValues(alpha: 0.08), blurRadius: 20, offset: const Offset(0, 8))],
          ),
          child: Column(children: [
            Container(
              padding: const EdgeInsets.fromLTRB(20, 22, 20, 18),
              decoration: const BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.vertical(top: Radius.circular(22)),
              ),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                Text(o.storeName.toUpperCase(), textAlign: TextAlign.center, style: AppTheme.display(14)),
                if ((o.branchName ?? o.branchAddress) != null) ...[
                  const SizedBox(height: 3),
                  Text([?o.branchName, ?o.branchAddress].join(' · '),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
                ],
                const SizedBox(height: 14),
                DefaultTextStyle.merge(
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textSecondary),
                  child: Row(children: [
                    Expanded(child: Text(t.orderPlacedNumber('${o.number}'))),
                    Text(OrderFormat.dateTime(context, t, o.createdAt.toLocal())),
                  ]),
                ),
                const _Dashed(),
                for (final i in items)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text(i.productName, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                          Text('${OrderFormat.quantity(i.quantity)} × ${formatMoney(context, i.unitPrice)}',
                              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.faint)),
                        ]),
                      ),
                      const SizedBox(width: 12),
                      Text(formatMoney(context, i.lineTotal),
                          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w800)),
                    ]),
                  ),
                const _Dashed(),
                row(t.cartItemsKinds(items.length), formatMoney(context, o.itemsTotal)),
                if (o.promoDiscount > 0)
                  row(t.cartPromoDiscount, '−${formatMoney(context, o.promoDiscount)}', color: AppColors.primary),
                if (o.bonusUsed > 0)
                  row(t.cartBonusUsed, '−${formatMoney(context, o.bonusUsed)}', color: AppColors.primary),
                if (o.fulfillmentType == FulfillmentType.delivery)
                  row(t.cartDelivery, o.deliveryFee == 0 ? t.cartFree : formatMoney(context, o.deliveryFee)),
                const SizedBox(height: 8),
                Row(crossAxisAlignment: CrossAxisAlignment.baseline, textBaseline: TextBaseline.alphabetic, children: [
                  Expanded(child: Text(t.cartTotal, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800))),
                  Text(formatMoney(context, o.totalAmount), style: AppTheme.display(23)),
                ]),
                row(t.checkoutPayment, OrderFormat.paymentMethod(t, o.paymentMethod)),
              ]),
            ),
            // Chekning tishli pastki cheti.
            const SizedBox(height: 10, width: double.infinity, child: CustomPaint(painter: _ZigZag())),
          ]),
        ),
      ],
    );
  }
}

class _Dashed extends StatelessWidget {
  const _Dashed();

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: LayoutBuilder(
          builder: (_, c) {
            final n = (c.maxWidth / 8).floor();
            return Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: List.generate(n, (_) => const SizedBox(width: 4, height: 1.5, child: ColoredBox(color: AppColors.border))),
            );
          },
        ),
      );
}

/// Oq yarim doiralar qatori (chek qog'ozi cheti).
class _ZigZag extends CustomPainter {
  const _ZigZag();

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = AppColors.surface;
    const step = 14.0, r = 7.0;
    // Pastdan yarim doira kesiladi: oq to'rtburchak − doiralar.
    final cut = Path();
    for (var x = r; x < size.width + r; x += step) {
      cut.addOval(Rect.fromCircle(center: Offset(x, size.height), radius: r - 0.5));
    }
    canvas.drawPath(
      Path.combine(PathOperation.difference, Path()..addRect(Offset.zero & size), cut),
      paint,
    );
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
