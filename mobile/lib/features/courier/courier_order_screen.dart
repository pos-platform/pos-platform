import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../returns/data/return_models.dart';
import '../returns/widgets/return_format.dart';
import 'courier_navigation.dart';
import 'courier_updates.dart';
import 'data/courier_models.dart';
import 'data/courier_repository.dart';
import 'widgets/courier_widgets.dart';

/// Bitta buyurtma: mijoz, manzil, navigatsiya, tarkib, "Yetkazildi" / "Yetkazib bo'lmadi".
class CourierOrderScreen extends ConsumerStatefulWidget {
  const CourierOrderScreen({super.key, required this.orderId});

  final String orderId;

  @override
  ConsumerState<CourierOrderScreen> createState() => _CourierOrderScreenState();
}

class _CourierOrderScreenState extends ConsumerState<CourierOrderScreen> {
  Timer? _poll;
  bool _busy = false;

  /// Eshik oldida mijoz olmagan qatorlar (kelishuv 10.5) — "Yetkazildi" bilan birga yuboriladi.
  List<CourierRejectedItem> _rejected = const [];

  Future<void> _editRejected(CourierOrder o) async {
    final result = await showModalBottomSheet<List<CourierRejectedItem>>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: AppColors.surface,
      builder: (_) => CourierRejectSheet(order: o, initial: _rejected),
    );
    if (result != null && mounted) setState(() => _rejected = result);
  }

  Future<void> _pickedUp(CourierOrder o) async {
    final t = AppLocalizations.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).pickedUp(o.id);
      ref.invalidate(courierOrderProvider(o.id));
      ref.invalidate(courierOrdersProvider(false));
      if (mounted) _snack(t.courierDonePickedUp);
    } on ApiException catch (e) {
      if (mounted) _snack(e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void initState() {
    super.initState();
    _poll = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted && !_busy) ref.invalidate(courierOrderProvider(widget.orderId));
    });
  }

  @override
  void dispose() {
    _poll?.cancel();
    super.dispose();
  }

  void _snack(String text) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  Future<void> _openNav(Future<bool> Function() open) async {
    final t = AppLocalizations.of(context);
    if (!await open() && mounted) _snack(t.courierOpenAppError);
  }

  void _finish(String message) {
    ref.invalidate(courierOrdersProvider(false));
    ref.invalidate(courierOrdersProvider(true));
    _snack(message);
    if (context.canPop()) {
      context.pop();
    } else {
      context.go('/courier');
    }
  }

  Future<void> _delivered(CourierOrder o) async {
    final t = AppLocalizations.of(context);
    double? cash;
    if (o.mustCollectCash) {
      // Rad etilgan tovarlar ayirilgan summa (server ham qayta hisoblaydi).
      cash = await showDialog<double>(
          context: context, builder: (_) => _CashDialog(total: courierAmountToCollect(o, _rejected)));
      if (cash == null) return;
    } else {
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          content: Text(t.courierDeliveredConfirm),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.courierCancel)),
            TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.courierConfirm)),
          ],
        ),
      );
      if (ok != true) return;
    }
    if (!mounted) return;
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).delivered(o.id, cashCollected: cash, rejectedItems: _rejected);
      if (mounted) _finish(t.courierDoneDelivered);
    } on ApiException catch (e) {
      if (mounted) _snack(e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _failed(CourierOrder o) async {
    final t = AppLocalizations.of(context);
    final reason = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: AppColors.surface,
      builder: (_) => const _FailReasonSheet(),
    );
    if (reason == null || !mounted) return;
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).failed(o.id, reason);
      if (mounted) _finish(t.courierDoneFailed);
    } on ApiException catch (e) {
      if (mounted) _snack(e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    ref.watch(courierUpdatesProvider);
    final async = ref.watch(courierOrderProvider(widget.orderId));
    final order = async.value;

    return Scaffold(
      appBar: AppBar(
        title: Text(order == null ? '' : '#${order.number}'),
        actions: const [CourierProfileChip()],
      ),
      body: async.when(
        skipLoadingOnRefresh: true,
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => CourierErrorView(
          message: e.toString(),
          onRetry: () => ref.invalidate(courierOrderProvider(widget.orderId)),
        ),
        data: (o) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(courierOrderProvider(widget.orderId));
            try {
              await ref.read(courierOrderProvider(widget.orderId).future);
            } catch (_) {}
          },
          child: _Body(
            order: o,
            onYandex: () => _openNav(() => CourierNavigation.yandex(o)),
            onGoogle: () => _openNav(() => CourierNavigation.google(o)),
            onCall: o.customerPhone == null ? null : () => _openNav(() => CourierNavigation.call(o.customerPhone!)),
            rejected: _rejected,
            onReject: o.isActive && o.items.any((i) => i.id != null) ? () => _editRejected(o) : null,
          ),
        ),
      ),
      bottomNavigationBar: order == null || !(order.isActive || order.isAwaitingPickup)
          ? null
          : order.isAwaitingPickup
              // Taklif qabul qilingan — do'kondan olgach "Do'kondan oldim" → OnTheWay (kelishuv 10.6).
              ? SafeArea(
                  minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                  child: FilledButton.icon(
                    onPressed: _busy ? null : () => _pickedUp(order),
                    icon: const Icon(Icons.shopping_bag_outlined),
                    label: Text(t.courierPickedUpFromStore),
                  ),
                )
              : SafeArea(
              minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
              child: Row(children: [
                Expanded(
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(AppTheme.controlHeight),
                      foregroundColor: AppColors.danger,
                      side: const BorderSide(color: AppColors.border),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTheme.radiusInput)),
                    ),
                    onPressed: _busy ? null : () => _failed(order),
                    child: Text(t.courierFailed, textAlign: TextAlign.center),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: _busy ? null : () => _delivered(order),
                    child: _busy
                        ? const SizedBox(
                            width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                        : Text(t.courierDelivered),
                  ),
                ),
              ]),
            ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({
    required this.order,
    required this.onYandex,
    required this.onGoogle,
    required this.onCall,
    this.rejected = const [],
    this.onReject,
  });

  final CourierOrder order;
  final VoidCallback onYandex;
  final VoidCallback onGoogle;
  final VoidCallback? onCall;
  final List<CourierRejectedItem> rejected;

  /// Null — rad etish mumkin emas (yo'lda emas yoki server qator ID bermagan).
  final VoidCallback? onReject;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final o = order;
    final label = theme.textTheme.bodySmall?.copyWith(color: AppColors.textSecondary);
    final strong = theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w700);

    Widget field(String name, String? value) => value == null
        ? const SizedBox.shrink()
        : Padding(
            padding: const EdgeInsets.only(top: 10),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(name, style: label),
              const SizedBox(height: 2),
              Text(value, style: strong),
            ]),
          );

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        CourierPaymentBadge(order: o, large: true),
        const SizedBox(height: 12),
        // Mijoz
        _Section(children: [
          Row(children: [
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(t.courierCustomer, style: label),
                const SizedBox(height: 2),
                Text(o.customerName ?? '—', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
                if (o.customerPhone != null) Text(o.customerPhone!, style: label),
              ]),
            ),
            if (onCall != null)
              IconButton.filled(
                onPressed: onCall,
                tooltip: t.courierCall,
                style: IconButton.styleFrom(backgroundColor: AppColors.primary, minimumSize: const Size(52, 52)),
                icon: const Icon(Icons.call, color: Colors.white),
              ),
          ]),
          if (o.contactless) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.circular(12)),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                Icon(Icons.do_not_touch_outlined, size: 18, color: AppColors.primary),
                const SizedBox(width: 8),
                Text(t.courierContactless, style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
              ]),
            ),
          ],
        ]),
        const SizedBox(height: 12),
        // Manzil va navigatsiya
        _Section(children: [
          Text(t.courierAddress, style: label),
          const SizedBox(height: 2),
          GestureDetector(
            onLongPress: o.addressLine == null ? null : () => Clipboard.setData(ClipboardData(text: o.addressLine!)),
            child: Text(o.addressLine ?? '—', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          ),
          Wrap(spacing: 20, children: [
            field(t.courierEntrance, o.entrance),
            field(t.courierFloor, o.floor),
            field(t.courierApartment, o.apartment),
            field(t.courierIntercom, o.intercom),
          ]),
          field(t.courierLandmark, o.landmark),
          if (!o.hasCoordinates) ...[
            const SizedBox(height: 10),
            Text(t.courierNoCoordinates, style: label),
          ],
          const SizedBox(height: 14),
          Row(children: [
            Expanded(child: _NavButton(label: t.courierYandexNavi, color: const Color(0xFFFC3F1D), onTap: onYandex)),
            const SizedBox(width: 10),
            Expanded(child: _NavButton(label: t.courierGoogleMaps, color: const Color(0xFF1A73E8), onTap: onGoogle)),
          ]),
        ]),
        if (o.comment != null) ...[
          const SizedBox(height: 12),
          _Section(children: [
            Text(t.courierComment, style: label),
            const SizedBox(height: 4),
            Text(o.comment!, style: theme.textTheme.bodyLarge),
          ]),
        ],
        const SizedBox(height: 12),
        // Tarkibi
        _Section(children: [
          Text(t.courierItems, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          for (final it in o.items)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(it.productName, style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                    Text('${CourierFormat.number(it.quantity)} × ${CourierFormat.money(t, it.unitPrice)}', style: label),
                  ]),
                ),
                const SizedBox(width: 8),
                Text(CourierFormat.money(t, it.lineTotal), style: const TextStyle(fontWeight: FontWeight.w700)),
              ]),
            ),
          // Rad etilgan qatorlar (eshik oldida).
          for (final r in rejected)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(children: [
                const Icon(Icons.remove_circle_outline, size: 18, color: AppColors.danger),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    '${_itemName(o, r.itemId)} × ${CourierFormat.number(r.quantity)} · ${returnReasonLabel(t, ReturnReason.parse(r.reason))}',
                    style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w600),
                  ),
                ),
                Text('−${CourierFormat.money(t, r.quantity * r.unitPrice)}',
                    style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w700)),
              ]),
            ),
          if (onReject != null) ...[
            const SizedBox(height: 4),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton.icon(
                onPressed: onReject,
                style: TextButton.styleFrom(foregroundColor: AppColors.danger),
                icon: const Icon(Icons.block, size: 18),
                label: Text(rejected.isEmpty ? t.courierRejectItems : t.courierRejectedEdit),
              ),
            ),
          ],
          const Divider(height: 24, color: AppColors.borderSoft),
          Row(children: [
            Expanded(child: Text(t.courierTotal, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800))),
            Text(CourierFormat.money(t, o.totalAmount),
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
          ]),
          if (rejected.isNotEmpty) ...[
            const SizedBox(height: 8),
            Row(children: [
              Expanded(
                child: Text(t.courierToCollect,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800, color: AppColors.accent)),
              ),
              Text(CourierFormat.money(t, courierAmountToCollect(o, rejected)),
                  style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800, color: AppColors.accent)),
            ]),
            Text(t.courierRejectApprox, style: label),
          ],
        ]),
      ],
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children),
        ),
      );
}

class _NavButton extends StatelessWidget {
  const _NavButton({required this.label, required this.color, required this.onTap});

  final String label;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => OutlinedButton.icon(
        onPressed: onTap,
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(AppTheme.controlHeight),
          foregroundColor: AppColors.text,
          side: const BorderSide(color: AppColors.border),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppTheme.radiusInput)),
          padding: const EdgeInsets.symmetric(horizontal: 10),
        ),
        icon: Icon(Icons.navigation, color: color),
        label: Text(label, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w700)),
      );
}

/// Naqd olingan summa (standart — jami, o'zgartirish mumkin).
class _CashDialog extends StatefulWidget {
  const _CashDialog({required this.total});

  final double total;

  @override
  State<_CashDialog> createState() => _CashDialogState();
}

class _CashDialogState extends State<_CashDialog> {
  late final _ctrl = TextEditingController(
      text: widget.total == widget.total.roundToDouble() ? widget.total.toStringAsFixed(0) : widget.total.toString());

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  double? get _value => double.tryParse(_ctrl.text.replaceAll(' ', '').replaceAll(',', '.'));

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final v = _value;
    return AlertDialog(
      title: Text(t.courierCashTitle),
      content: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(t.courierCollectCash(CourierFormat.money(t, widget.total)),
            style: const TextStyle(color: AppColors.accent, fontWeight: FontWeight.w800)),
        const SizedBox(height: 14),
        TextField(
          controller: _ctrl,
          autofocus: true,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[\d.,]'))],
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(labelText: t.courierCashHint, suffixText: t.courierMoney('').trim()),
        ),
      ]),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: Text(t.courierCancel)),
        TextButton(
          onPressed: v == null || v < 0 ? null : () => Navigator.pop(context, v),
          child: Text(t.courierConfirm),
        ),
      ],
    );
  }
}

/// Yetkazib bo'lmaganlik sababi.
class _FailReasonSheet extends StatefulWidget {
  const _FailReasonSheet();

  @override
  State<_FailReasonSheet> createState() => _FailReasonSheetState();
}

class _FailReasonSheetState extends State<_FailReasonSheet> {
  int? _selected;
  final _other = TextEditingController();

  @override
  void dispose() {
    _other.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final reasons = [t.courierReasonNoAnswer, t.courierReasonNotFound, t.courierReasonRefused, t.courierReasonOther];
    final isOther = _selected == reasons.length - 1;
    final reason = _selected == null
        ? null
        : isOther
            ? (_other.text.trim().isEmpty ? null : _other.text.trim())
            : reasons[_selected!];

    return Padding(
      padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text(t.courierFailedTitle, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
          const SizedBox(height: 14),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (var i = 0; i < reasons.length; i++)
              ChoiceChip(
                label: Text(reasons[i]),
                selected: _selected == i,
                onSelected: (_) => setState(() => _selected = i),
                selectedColor: AppColors.primarySoft,
                labelStyle: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: _selected == i ? AppColors.primary : AppColors.text,
                ),
              ),
          ]),
          if (isOther) ...[
            const SizedBox(height: 12),
            TextField(
              controller: _other,
              autofocus: true,
              maxLength: 200,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(hintText: t.courierReasonOtherHint),
            ),
          ],
          const SizedBox(height: 16),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: reason == null ? null : () => Navigator.pop(context, reason),
            child: Text(t.courierConfirm),
          ),
        ]),
      ),
    );
  }
}

String _itemName(CourierOrder o, String itemId) {
  for (final it in o.items) {
    if (it.id == itemId) return it.productName;
  }
  return '—';
}

/// "Ba'zi tovarlarni rad etdi": qatorni belgilash, miqdor va sabab. Natija — rad etilgan qatorlar ro'yxati.
class CourierRejectSheet extends StatefulWidget {
  const CourierRejectSheet({super.key, required this.order, this.initial = const []});

  final CourierOrder order;
  final List<CourierRejectedItem> initial;

  @override
  State<CourierRejectSheet> createState() => _CourierRejectSheetState();
}

class _CourierRejectSheetState extends State<CourierRejectSheet> {
  /// itemId → miqdor.
  final _qty = <String, double>{};
  ReturnReason _reason = ReturnReason.damaged;

  @override
  void initState() {
    super.initState();
    for (final r in widget.initial) {
      _qty[r.itemId] = r.quantity;
      _reason = ReturnReason.parse(r.reason);
    }
  }

  /// Tarozi tovarida qadam 0.1, donada 1.
  static double _step(CourierOrderItem it) => it.quantity == it.quantity.roundToDouble() ? 1 : 0.1;

  void _change(CourierOrderItem it, double delta) {
    final id = it.id!;
    final next = ((_qty[id] ?? 0) + delta).clamp(0, it.quantity).toDouble();
    setState(() => next <= 0.0001 ? _qty.remove(id) : _qty[id] = double.parse(next.toStringAsFixed(3)));
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final items = widget.order.items.where((i) => i.id != null).toList();

    return Padding(
      padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(
        child: ConstrainedBox(
          constraints: BoxConstraints(maxHeight: MediaQuery.sizeOf(context).height * 0.8),
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text(t.courierRejectTitle, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 8),
            Flexible(
              child: ListView(shrinkWrap: true, children: [
                for (final it in items)
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    controlAffinity: ListTileControlAffinity.leading,
                    activeColor: AppColors.danger,
                    value: _qty.containsKey(it.id),
                    onChanged: (v) => setState(() => v == true ? _qty[it.id!] = it.quantity : _qty.remove(it.id)),
                    title: Text(it.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text('${CourierFormat.number(it.quantity)} × ${CourierFormat.money(t, it.unitPrice)}'),
                    secondary: _qty.containsKey(it.id)
                        ? Row(mainAxisSize: MainAxisSize.min, children: [
                            IconButton(
                              tooltip: '−',
                              onPressed: () => _change(it, -_step(it)),
                              icon: const Icon(Icons.remove),
                            ),
                            Text(CourierFormat.number(_qty[it.id]!), style: const TextStyle(fontWeight: FontWeight.w800)),
                            IconButton(
                              tooltip: '+',
                              onPressed: (_qty[it.id] ?? 0) >= it.quantity ? null : () => _change(it, _step(it)),
                              icon: const Icon(Icons.add),
                            ),
                          ])
                        : null,
                  ),
                const SizedBox(height: 8),
                Text(t.returnReason, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w800)),
                const SizedBox(height: 8),
                Wrap(spacing: 8, runSpacing: 8, children: [
                  for (final r in ReturnReason.values)
                    ChoiceChip(
                      label: Text(returnReasonLabel(t, r)),
                      selected: _reason == r,
                      onSelected: (_) => setState(() => _reason = r),
                      selectedColor: AppColors.primarySoft,
                      labelStyle: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: _reason == r ? AppColors.primary : AppColors.text,
                      ),
                    ),
                ]),
              ]),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () => Navigator.pop(context, [
                for (final it in items)
                  if (_qty[it.id] != null)
                    CourierRejectedItem(
                      itemId: it.id!,
                      quantity: _qty[it.id]!,
                      reason: _reason.apiName,
                      unitPrice: it.unitPrice,
                    ),
              ]),
              child: Text(t.courierSave),
            ),
          ]),
        ),
      ),
    );
  }
}
