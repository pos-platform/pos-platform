import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
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
      cash = await showDialog<double>(context: context, builder: (_) => _CashDialog(total: o.totalAmount));
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
      await ref.read(courierRepositoryProvider).delivered(o.id, cashCollected: cash);
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
          ),
        ),
      ),
      bottomNavigationBar: order == null || !order.isActive
          ? null
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
  const _Body({required this.order, required this.onYandex, required this.onGoogle, required this.onCall});

  final CourierOrder order;
  final VoidCallback onYandex;
  final VoidCallback onGoogle;
  final VoidCallback? onCall;

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
                const Icon(Icons.do_not_touch_outlined, size: 18, color: AppColors.primary),
                const SizedBox(width: 8),
                Text(t.courierContactless, style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
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
          const Divider(height: 24, color: AppColors.borderSoft),
          Row(children: [
            Expanded(child: Text(t.courierTotal, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800))),
            Text(CourierFormat.money(t, o.totalAmount),
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
          ]),
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
