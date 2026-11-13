import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../marketing/rating.dart';
import '../marketing/support.dart';
import 'data/order_models.dart';
import 'data/orders_repository.dart';
import 'order_updates.dart';
import 'widgets/order_format.dart';
import 'widgets/order_items_sheet.dart';
import 'widgets/transfer_section.dart';

/// Buyurtmani kuzatish (dizayn: Tracking.dc.html). Marshrut: `/orders/:id`.
/// Yangilanish: SignalR (order_updates.dart) + faol buyurtmada har 30 soniyada so'rov + pull-to-refresh
/// + ilovaga qaytilganda (masalan, Payme/Click sahifasidan) darhol yangilanadi.
class OrderTrackingScreen extends ConsumerStatefulWidget {
  const OrderTrackingScreen({super.key, required this.orderId});

  final String orderId;

  @override
  ConsumerState<OrderTrackingScreen> createState() => _OrderTrackingScreenState();
}

class _OrderTrackingScreenState extends ConsumerState<OrderTrackingScreen> with WidgetsBindingObserver {
  Timer? _poll;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _poll = Timer.periodic(const Duration(seconds: 30), (_) {
      final order = ref.read(orderDetailProvider(widget.orderId)).value;
      if (order != null && order.isActive) {
        ref.read(orderDetailProvider(widget.orderId).notifier).refreshSilently();
      }
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _poll?.cancel();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // To'lov sahifasidan qaytildi — holat (To'langan) darhol ko'rinsin.
    if (state == AppLifecycleState.resumed) {
      ref.read(orderDetailProvider(widget.orderId).notifier).refreshSilently();
    }
  }

  void _back() => context.canPop() ? context.pop() : context.go('/orders');

  bool _ratingChecked = false;

  /// `canRate` bo'lsa baho oynasi bir marta taklif qilinadi (kelishuv 9.3).
  void _maybeRate(OrderDetail order) {
    if (_ratingChecked || !order.canRate) return;
    _ratingChecked = true;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) maybePromptRating(context, ref, order);
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    ref.watch(orderUpdatesProvider); // jonli yangilanish (SignalR)
    final async = ref.watch(orderDetailProvider(widget.orderId));
    final order = async.value;
    if (order != null) _maybeRate(order);

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
              child: Row(
                children: [
                  _SquareButton(icon: Icons.arrow_back, tooltip: t.ordersBack, onTap: _back),
                  Expanded(
                    child: Text(
                      order == null ? '' : t.ordersOrderNumber('${order.number}'),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: AppColors.textSecondary),
                    ),
                  ),
                  _SquareButton(
                    icon: Icons.phone_outlined,
                    tooltip: t.ordersCallStore,
                    onTap: order?.storePhone == null ? null : () => _callPhone(order!.storePhone!),
                  ),
                ],
              ),
            ),
            Expanded(
              child: switch (async) {
                AsyncValue(value: final o?) => _Body(order: o),
                AsyncError(:final error) => Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text('$error', textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
                        TextButton(
                          onPressed: () => ref.invalidate(orderDetailProvider(widget.orderId)),
                          child: Text(t.retry),
                        ),
                      ],
                    ),
                  ),
                _ => const Center(child: CircularProgressIndicator()),
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final o = order;
    final failed = o.status.isFailed;

    return Column(
      children: [
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => ref.read(orderDetailProvider(o.id).notifier).refreshSilently(),
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
              children: [
                if (failed) _FailedCard(order: o) else _StatusCard(order: o),
                const SizedBox(height: 16),
                if (o.status == OrderStatus.delivered && OrderRatingCard.visibleFor(o)) ...[
                  OrderRatingCard(order: o),
                  const SizedBox(height: 16),
                ],
                if (!failed) ...[
                  _Timeline(order: o),
                  const SizedBox(height: 16),
                ],
                if (o.canPayOnline && o.onlineProviders.isNotEmpty) ...[
                  _OnlinePaySection(order: o),
                  const SizedBox(height: 16),
                ],
                if (TransferSection.visibleFor(o)) ...[
                  TransferSection(order: o),
                  const SizedBox(height: 16),
                ],
                _SummaryCard(order: o),
                const SizedBox(height: 16),
                SupportCard(branchId: o.branchId),
              ],
            ),
          ),
        ),
        if (o.isActive)
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
            child: _CancelArea(order: o),
          ),
      ],
    );
  }
}

/// Yashil karta: holat, 5 bo'lakli progress, izoh.
class _StatusCard extends StatelessWidget {
  const _StatusCard({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final done = _doneSteps(order.status);
    final scheduled = order.scheduledFor;
    final eta = _etaText(order);

    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(22)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            scheduled != null && order.isActive
                ? t.ordersScheduled(OrderFormat.dateTime(context, t, scheduled))
                : t.ordersCardLabel,
            style: TextStyle(fontSize: 13, color: AppColors.onPrimarySoft, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 4),
          Text(
            OrderFormat.status(t, order.status, order.fulfillmentType),
            style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w800, color: Colors.white, letterSpacing: -0.5),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              for (var i = 0; i < 5; i++) ...[
                if (i > 0) const SizedBox(width: 6),
                Expanded(
                  child: Container(
                    height: 6,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(3),
                      color: i < done
                          ? Colors.white
                          : i == done
                              ? const Color(0xFF9FD3B5)
                              : const Color(0xFF2E8A63),
                    ),
                  ),
                ),
              ],
            ],
          ),
          const SizedBox(height: 14),
          Text(OrderFormat.headline(t, order.status),
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: Colors.white)),
          if (eta != null) ...[
            const SizedBox(height: 6),
            Row(children: [
              Icon(Icons.schedule, size: 18, color: AppColors.onPrimarySoft),
              const SizedBox(width: 6),
              Expanded(
                child: Text(t.ordersEta(eta),
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.onPrimarySoft)),
              ),
            ]),
          ],
        ],
      ),
    );
  }
}

/// Rad etilgan / bekor qilingan buyurtma.
class _FailedCard extends StatelessWidget {
  const _FailedCard({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final when = order.cancelledAt;
    // To'langan buyurtma bekor qilinsa — pul qaytariladi (do'kon "qaytarildi" deb belgilaguncha).
    final (IconData, String)? refund = switch (order.paymentStatus) {
      PaymentStatus.refunded => (Icons.check_circle, t.ordersRefundDone),
      PaymentStatus.paid => (Icons.schedule, t.ordersRefundPending),
      PaymentStatus.unpaid => null,
    };
    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.cancel_outlined, color: AppColors.danger),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  order.status == OrderStatus.rejected ? t.ordersRejectedTitle : t.ordersCancelledTitle,
                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                ),
              ),
            ],
          ),
          if (order.cancelReason != null) ...[
            const SizedBox(height: 8),
            Text(t.ordersReason(order.cancelReason!), style: const TextStyle(fontSize: 14, color: AppColors.textMuted)),
          ],
          if (when != null) ...[
            const SizedBox(height: 4),
            Text(OrderFormat.dateTime(context, t, when),
                style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
          ],
          if (refund != null) ...[
            const SizedBox(height: 12),
            Row(children: [
              Icon(refund.$1, size: 20, color: AppColors.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Text(refund.$2,
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: AppColors.primary)),
              ),
            ]),
          ],
        ],
      ),
    );
  }
}

/// "14:30 – 15:00" (mahalliy vaqt) — faol yetkazishda server taxmin bergan bo'lsa.
String? _etaText(OrderDetail o) {
  final from = o.estimatedDeliveryFrom, to = o.estimatedDeliveryTo;
  if (!o.isActive || o.fulfillmentType != FulfillmentType.delivery || (from == null && to == null)) return null;
  if (from != null && to != null) return '${OrderFormat.time(from)} – ${OrderFormat.time(to)}';
  return OrderFormat.time((from ?? to)!);
}

/// Qadam ostidagi kichik tugma (masalan, "Kuryerga qo'ng'iroq").
class _StepAction extends StatelessWidget {
  const _StepAction({required this.icon, required this.label, required this.onTap});

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Align(
        alignment: Alignment.centerLeft,
        child: OutlinedButton.icon(
          onPressed: onTap,
          style: OutlinedButton.styleFrom(
            minimumSize: const Size(0, 40),
            padding: const EdgeInsets.symmetric(horizontal: 12),
            foregroundColor: AppColors.primary,
            side: const BorderSide(color: AppColors.border),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
          ),
          icon: Icon(icon, size: 18),
          label: Text(label),
        ),
      );
}

/// Yetkazilgan buyurtma: chek raqami va "Fiskal chek" (soliq saytidagi QR havola).
class _FiscalReceipt extends StatelessWidget {
  const _FiscalReceipt({required this.receiptNumber, required this.url});

  final String? receiptNumber;
  final String? url;

  static Widget? maybe(OrderDetail o) => o.receiptNumber == null && o.fiscalQrUrl == null
      ? null
      : _FiscalReceipt(receiptNumber: o.receiptNumber, url: o.fiscalQrUrl);

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final uri = url == null ? null : Uri.tryParse(url!);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (receiptNumber != null)
          Text(t.ordersReceiptNumber(receiptNumber!),
              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted)),
        if (uri != null) ...[
          if (receiptNumber != null) const SizedBox(height: 6),
          _StepAction(
            icon: Icons.receipt_long_outlined,
            label: t.ordersFiscalReceipt,
            onTap: () => launchUrl(uri, mode: LaunchMode.externalApplication),
          ),
        ],
      ],
    );
  }
}

Future<void> _callPhone(String phone) async {
  await launchUrl(Uri(scheme: 'tel', path: phone.replaceAll(RegExp(r'[^\d+]'), '')));
}

/// Bajarilgan qadamlar soni (0..5). Joriy qadam — shu indeks.
int _doneSteps(OrderStatus s) => switch (s) {
      OrderStatus.created => 1,
      OrderStatus.accepted || OrderStatus.picking => 2,
      OrderStatus.ready || OrderStatus.awaitingTransfer => 3,
      OrderStatus.onTheWay || OrderStatus.readyForPickup => 4,
      OrderStatus.delivered => 5,
      OrderStatus.rejected || OrderStatus.cancelled => 0,
    };

class _Step {
  const _Step(this.title, this.sub, this.time, {this.extra});

  final String title;
  final String? sub;
  final DateTime? time;

  /// Qadam ostidagi qo'shimcha (kuryerga qo'ng'iroq, fiskal chek).
  final Widget? extra;
}

class _Timeline extends StatelessWidget {
  const _Timeline({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final o = order;
    final pickup = o.fulfillmentType == FulfillmentType.pickup;
    final done = _doneSteps(o.status);

    final steps = [
      _Step(t.ordersStepPlaced,
          '${OrderFormat.paymentMethod(t, o.paymentMethod)} · ${OrderFormat.paymentStatus(t, o.paymentStatus)}',
          o.createdAt),
      _Step(t.ordersStepAccepted, o.branchName ?? o.storeName, o.acceptedAt),
      _Step(t.ordersStepPicking, o.status == OrderStatus.picking ? t.ordersStepPickingSub : null,
          o.pickingStartedAt ?? o.readyAt),
      pickup
          ? _Step(t.ordersStepReadyForPickup, o.branchAddress, done >= 4 ? o.readyAt : null)
          : _Step(
              t.ordersStepDispatched,
              o.status == OrderStatus.awaitingTransfer
                  ? t.ordersStepAwaitingTransfer
                  : o.courierName == null
                      ? null
                      : t.ordersCourier(o.courierName!),
              o.dispatchedAt,
              extra: o.courierPhone != null && done >= 3 && o.status != OrderStatus.delivered
                  ? _StepAction(
                      icon: Icons.phone_outlined,
                      label: t.ordersCallCourier,
                      onTap: () => _callPhone(o.courierPhone!),
                    )
                  : null,
            ),
      _Step(
        pickup ? t.ordersStepPickedUp : t.ordersStepDelivered,
        pickup ? null : o.addressLine,
        o.deliveredAt,
        extra: o.status == OrderStatus.delivered ? _FiscalReceipt.maybe(o) : null,
      ),
    ];

    return Container(
      padding: const EdgeInsets.fromLTRB(18, 18, 18, 2),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        children: [
          for (var i = 0; i < steps.length; i++)
            _StepRow(
              step: steps[i],
              state: i < done ? _StepState.done : (i == done ? _StepState.current : _StepState.todo),
              isLast: i == steps.length - 1,
            ),
        ],
      ),
    );
  }
}

enum _StepState { done, current, todo }

class _StepRow extends StatelessWidget {
  const _StepRow({required this.step, required this.state, required this.isLast});

  final _Step step;
  final _StepState state;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final todo = state == _StepState.todo;
    final Widget dot = switch (state) {
      _StepState.done => Container(
          width: 24,
          height: 24,
          decoration: BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
          child: const Icon(Icons.check, size: 14, color: Colors.white),
        ),
      _StepState.current => Container(
          width: 24,
          height: 24,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: AppColors.primarySoft,
            shape: BoxShape.circle,
            border: Border.all(color: AppColors.primary, width: 2),
          ),
          child: Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
          ),
        ),
      _StepState.todo => Container(
          width: 24,
          height: 24,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: Border.all(color: const Color(0xFFD2CFC5), width: 2),
          ),
        ),
    };

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 24,
            child: Column(
              children: [
                dot,
                if (!isLast)
                  Expanded(child: Container(width: 2, constraints: const BoxConstraints(minHeight: 22), color: AppColors.border)),
              ],
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(step.title,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: todo ? FontWeight.w600 : FontWeight.w800,
                              color: todo ? AppColors.textSecondary : AppColors.text,
                            )),
                        if (step.sub != null && step.sub!.isNotEmpty) ...[
                          const SizedBox(height: 2),
                          Text(step.sub!, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                        ],
                        if (step.extra != null && state != _StepState.todo) ...[
                          const SizedBox(height: 8),
                          step.extra!,
                        ],
                      ],
                    ),
                  ),
                  if (step.time != null && state != _StepState.todo)
                    Text(OrderFormat.time(step.time!),
                        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.textSecondary)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// "3 xil mahsulot · 70 500 so'm" + to'lov holati + "Tarkibi".
class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(t.ordersItemsSummary('${order.itemCount}', OrderFormat.money(t, order.totalAmount)),
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                const SizedBox(height: 2),
                Text(
                  [
                    OrderFormat.paymentStatus(t, order.paymentStatus),
                    order.onlinePaymentProvider ?? OrderFormat.paymentMethod(t, order.paymentMethod),
                  ].join(' · '),
                  style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                ),
                if (order.promoDiscount > 0 || order.bonusUsed > 0)
                  Text(
                    [
                      if (order.promoDiscount > 0) '${t.cartPromoDiscount}: −${OrderFormat.money(t, order.promoDiscount)}',
                      if (order.bonusUsed > 0) '${t.cartBonusUsed}: −${OrderFormat.money(t, order.bonusUsed)}',
                    ].join(' · '),
                    style: TextStyle(fontSize: 12, color: AppColors.primary, fontWeight: FontWeight.w600),
                  ),
              ],
            ),
          ),
          TextButton(onPressed: () => showOrderItemsSheet(context, order), child: Text(t.ordersContents)),
        ],
      ),
    );
  }
}

/// Online to'lov (Payme/Click): server havolasini olib, tashqi ilova/brauzerda ochadi.
/// Natija ilovaga qaytilganda (lifecycle resumed) yoki SignalR orqali yangilanadi.
class _OnlinePaySection extends ConsumerStatefulWidget {
  const _OnlinePaySection({required this.order});

  final OrderDetail order;

  @override
  ConsumerState<_OnlinePaySection> createState() => _OnlinePaySectionState();
}

class _OnlinePaySectionState extends ConsumerState<_OnlinePaySection> {
  String? _busy;

  Future<void> _pay(String provider) async {
    final t = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = provider);
    try {
      final url = await ref.read(ordersRepositoryProvider).payOnline(widget.order.id, provider);
      final uri = Uri.tryParse(url);
      final ok = uri != null && await launchUrl(uri, mode: LaunchMode.externalApplication);
      if (!ok) messenger.showSnackBar(SnackBar(content: Text(t.ordersPayOpenFailed)));
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text('$e')));
    } finally {
      if (mounted) setState(() => _busy = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final o = widget.order;
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(t.ordersPayOnlineTitle, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
          const SizedBox(height: 4),
          Text(t.ordersPayOnlineHint(OrderFormat.money(t, o.totalAmount)),
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
          const SizedBox(height: 12),
          for (final (i, p) in o.onlineProviders.indexed) ...[
            if (i > 0) const SizedBox(height: 8),
            FilledButton(
              onPressed: _busy == null ? () => _pay(p) : null,
              child: _busy == p
                  ? const SizedBox(
                      width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                  : Text(t.ordersPayWith(p)),
            ),
          ],
        ],
      ),
    );
  }
}

/// Bekor qilish tugmasi: faqat `canCancel` bo'lsa faol (Picking'gacha).
class _CancelArea extends ConsumerStatefulWidget {
  const _CancelArea({required this.order});

  final OrderDetail order;

  @override
  ConsumerState<_CancelArea> createState() => _CancelAreaState();
}

class _CancelAreaState extends ConsumerState<_CancelArea> {
  bool _busy = false;

  Future<void> _cancel() async {
    final t = AppLocalizations.of(context);
    final reason = await showDialog<String>(context: context, builder: (_) => const _CancelDialog());
    if (reason == null || !mounted) return;
    setState(() => _busy = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(orderDetailProvider(widget.order.id).notifier).cancel(reason);
      messenger.showSnackBar(SnackBar(content: Text(t.ordersCancelDone)));
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text('$e')));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final can = widget.order.canCancel;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          width: double.infinity,
          height: 52,
          child: OutlinedButton(
            onPressed: can && !_busy ? _cancel : null,
            style: OutlinedButton.styleFrom(
              backgroundColor: AppColors.surface,
              foregroundColor: AppColors.danger,
              disabledForegroundColor: AppColors.iconMuted,
              side: const BorderSide(color: Color(0xFFD2CFC5)),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
            ),
            child: _busy
                ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(t.ordersCancel),
          ),
        ),
        if (!can) ...[
          const SizedBox(height: 6),
          Text(t.ordersCancelDisabled,
              textAlign: TextAlign.center, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
        ],
      ],
    );
  }
}

/// Sabab so'raydigan dialog. Natija: sabab matni ("" — sababsiz), null — voz kechildi.
class _CancelDialog extends StatefulWidget {
  const _CancelDialog();

  @override
  State<_CancelDialog> createState() => _CancelDialogState();
}

class _CancelDialogState extends State<_CancelDialog> {
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(t.ordersCancelTitle),
      content: TextField(
        controller: _reason,
        maxLength: 500,
        maxLines: 3,
        minLines: 1,
        decoration: InputDecoration(hintText: t.ordersCancelReasonHint),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(t.ordersBack)),
        TextButton(
          onPressed: () => Navigator.of(context).pop(_reason.text.trim()),
          style: TextButton.styleFrom(foregroundColor: AppColors.danger),
          child: Text(t.ordersCancel),
        ),
      ],
    );
  }
}

class _SquareButton extends StatelessWidget {
  const _SquareButton({required this.icon, required this.tooltip, required this.onTap});

  final IconData icon;
  final String tooltip;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: Material(
        color: AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: AppColors.border),
        ),
        child: InkWell(
          customBorder: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          onTap: onTap,
          child: SizedBox(
            width: 44,
            height: 44,
            child: Icon(icon, size: 22, color: onTap == null ? AppColors.iconMuted : AppColors.text),
          ),
        ),
      ),
    );
  }
}
