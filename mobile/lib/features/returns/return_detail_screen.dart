import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/config.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../orders/order_updates.dart';
import '../orders/widgets/order_format.dart';
import '../profile/widgets/profile_widgets.dart';
import 'data/return_models.dart';
import 'data/returns_repository.dart';
import 'widgets/return_format.dart';

/// Qaytarish holati (`/profile/returns/:id`): holatlar ketma-ketligi, tovarlar, summa, rasmlar va
/// "So'rovni bekor qilish" (faqat Requested). Push `type=return_status` shu ekranni ochadi.
class ReturnDetailScreen extends ConsumerWidget {
  const ReturnDetailScreen({super.key, required this.returnId});

  final String returnId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    ref.watch(orderUpdatesProvider); // ReturnStatusChanged → qayta yuklanadi
    final async = ref.watch(returnDetailProvider(returnId));
    final r = async.value;

    return Scaffold(
      body: SafeArea(
        child: Column(children: [
          ProfilePageHeader(title: r == null ? t.returnsTitle : t.returnNumber(r.number)),
          Expanded(
            child: switch (async) {
              AsyncValue(value: final v?) => RefreshIndicator(
                  onRefresh: () async {
                    ref.invalidate(returnDetailProvider(returnId));
                    try {
                      await ref.read(returnDetailProvider(returnId).future);
                    } catch (_) {}
                  },
                  child: _Body(item: v),
                ),
              AsyncError(:final error) =>
                ProfileErrorView(error: error, onRetry: () => ref.invalidate(returnDetailProvider(returnId))),
              _ => const Center(child: CircularProgressIndicator()),
            },
          ),
          if (r != null && r.canCancel) _CancelBar(item: r),
        ]),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.item});

  final ShopReturn item;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final r = item;
    final label = const TextStyle(fontSize: 12, color: AppColors.textSecondary);
    final amount = r.refundAmount > 0 ? r.refundAmount : r.itemsTotal;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
      children: [
        _Card(children: [
          Row(children: [
            Expanded(
              child: Text(returnStatusLabel(t, r.status),
                  style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
            ),
            ReturnStatusPill(status: r.status),
          ]),
          const SizedBox(height: 4),
          Text(
            [
              if (r.orderNumber != null) t.ordersOrderNumber(r.orderNumber!),
              ?r.storeName,
            ].join(' · '),
            style: label,
          ),
          if (r.status == ReturnStatus.rejected && r.rejectReason != null) ...[
            const SizedBox(height: 10),
            Text(t.returnRejectReason(r.rejectReason!),
                style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w600)),
          ],
        ]),
        const SizedBox(height: 12),
        _Card(children: [
          Text(t.returnTimeline, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          for (final s in returnTimeline(r)) _TimelineRow(step: s),
        ]),
        const SizedBox(height: 12),
        _Card(children: [
          for (final it in r.items)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(children: [
                Expanded(
                  child: Text('${it.productName} × ${OrderFormat.quantity(it.quantity)}',
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                ),
                if (it.unitPrice > 0) Text(OrderFormat.money(t, it.lineTotal)),
              ]),
            ),
          if (r.items.isNotEmpty) const Divider(height: 20, color: AppColors.borderSoft),
          _kv(t.returnReason, returnReasonLabel(t, r.reason)),
          _kv(t.returnRefundMethod, refundMethodLabel(t, r.refundMethod)),
          if (r.method != null) _kv(t.returnMethod, returnMethodLabel(t, r.method!)),
          if (r.loyaltyWithheldAmount > 0)
            _kv(t.returnWithheld, '−${OrderFormat.money(t, r.loyaltyWithheldAmount)}'),
          if (amount > 0) _kv(t.returnAmount, OrderFormat.money(t, amount), strong: true),
        ]),
        if (r.comment != null) ...[
          const SizedBox(height: 12),
          _Card(children: [
            Text(t.returnComment, style: label),
            const SizedBox(height: 4),
            Text(r.comment!),
          ]),
        ],
        if (r.photoUrls.isNotEmpty) ...[
          const SizedBox(height: 12),
          Wrap(spacing: 10, runSpacing: 10, children: [
            for (final p in r.photoUrls)
              ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Image.network(
                  AppConfig.imageUrl(p) ?? p,
                  width: 96,
                  height: 96,
                  fit: BoxFit.cover,
                  errorBuilder: (_, _, _) => Container(width: 96, height: 96, color: AppColors.chipSurface),
                ),
              ),
          ]),
        ],
      ],
    );
  }

  Widget _kv(String k, String v, {bool strong = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(children: [
          Expanded(child: Text(k, style: const TextStyle(color: AppColors.textSecondary))),
          Text(v, style: TextStyle(fontWeight: strong ? FontWeight.w800 : FontWeight.w600, fontSize: strong ? 16 : 14)),
        ]),
      );
}

/// Holat ketma-ketligidagi qadam: nomi, vaqti (bo'lsa), bajarilganmi.
class ReturnStep {
  const ReturnStep(this.status, this.time, {required this.done, this.failed = false});

  final ReturnStatus status;
  final DateTime? time;
  final bool done;
  final bool failed;
}

/// Qaytarish holatlari ketma-ketligi. Rad/bekor qilinganda — so'rov va yakuniy holat;
/// do'konga o'zi olib boradigan bo'lsa kuryer qadamlari ko'rsatilmaydi.
List<ReturnStep> returnTimeline(ShopReturn r) {
  if (r.status.isFailed) {
    return [
      ReturnStep(ReturnStatus.requested, r.createdAt, done: true),
      ReturnStep(r.status, r.status == ReturnStatus.rejected ? r.rejectedAt : r.cancelledAt, done: true, failed: true),
    ];
  }
  final viaCourier = r.method != ReturnMethod.bringToStore;
  final order = [
    ReturnStatus.requested,
    ReturnStatus.approved,
    if (viaCourier) ReturnStatus.pickupAssigned,
    if (viaCourier) ReturnStatus.pickedUp,
    ReturnStatus.received,
    ReturnStatus.refunded,
  ];
  final reached = r.status.index;
  DateTime? timeOf(ReturnStatus s) => switch (s) {
        ReturnStatus.requested => r.createdAt,
        ReturnStatus.approved => r.approvedAt,
        ReturnStatus.pickedUp => r.pickedUpAt,
        ReturnStatus.received => r.receivedAt,
        ReturnStatus.refunded => r.refundedAt,
        _ => null,
      };
  return [for (final s in order) ReturnStep(s, timeOf(s), done: s.index <= reached)];
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({required this.step});

  final ReturnStep step;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final color = step.failed
        ? AppColors.danger
        : step.done
            ? AppColors.primary
            : AppColors.iconMuted;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(children: [
        Icon(
          step.failed
              ? Icons.cancel
              : step.done
                  ? Icons.check_circle
                  : Icons.radio_button_unchecked,
          size: 20,
          color: color,
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(returnStatusLabel(t, step.status),
              style: TextStyle(
                  fontWeight: step.done ? FontWeight.w700 : FontWeight.w500,
                  color: step.done ? AppColors.text : AppColors.textSecondary)),
        ),
        if (step.time != null && step.done)
          Text(OrderFormat.dateTime(context, t, step.time!),
              style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
      ]),
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: AppColors.borderSoft),
        ),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children),
      );
}

class _CancelBar extends ConsumerStatefulWidget {
  const _CancelBar({required this.item});

  final ShopReturn item;

  @override
  ConsumerState<_CancelBar> createState() => _CancelBarState();
}

class _CancelBarState extends ConsumerState<_CancelBar> {
  bool _busy = false;

  Future<void> _cancel() async {
    final t = AppLocalizations.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        content: Text(t.returnCancelConfirm),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.ordersBack)),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: Text(t.returnCancel),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    setState(() => _busy = true);
    try {
      await ref.read(returnsRepositoryProvider).cancel(widget.item.id);
      ref.invalidate(returnDetailProvider(widget.item.id));
      ref.invalidate(returnsListProvider);
      if (mounted) showProfileSnack(context, t.returnCancelled);
    } on ApiException catch (e) {
      if (mounted) showProfileSnack(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
      child: SizedBox(
        width: double.infinity,
        height: 52,
        child: OutlinedButton(
          onPressed: _busy ? null : _cancel,
          style: OutlinedButton.styleFrom(
            backgroundColor: AppColors.surface,
            foregroundColor: AppColors.danger,
            side: const BorderSide(color: AppColors.border),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          ),
          child: _busy
              ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(t.returnCancel),
        ),
      ),
    );
  }
}
