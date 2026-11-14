import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import 'courier_navigation.dart';
import 'data/courier_models.dart';
import 'data/courier_repository.dart';
import 'widgets/courier_widgets.dart';

/// Qaytarishni olib ketish vazifasi (kelishuv 10.5, `kind: "return_pickup"`): manzil, tovarlar,
/// navigatsiya/qo'ng'iroq va "Olib ketildi". Kuryer pul bermaydi — do'kon `received` dan keyin qaytaradi.
class CourierReturnCard extends ConsumerStatefulWidget {
  const CourierReturnCard({super.key, required this.task, this.distance});

  final CourierOrder task;
  final double? distance;

  @override
  ConsumerState<CourierReturnCard> createState() => _CourierReturnCardState();
}

class _CourierReturnCardState extends ConsumerState<CourierReturnCard> {
  bool _busy = false;

  CourierOrder get task => widget.task;

  void _snack(String text) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  Future<void> _open(Future<bool> Function() open) async {
    final t = AppLocalizations.of(context);
    if (!await open() && mounted) _snack(t.courierOpenAppError);
  }

  Future<void> _pickedUp() async {
    final t = AppLocalizations.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        content: Text(t.courierReturnPickedUpConfirm),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.courierCancel)),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.courierConfirm)),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).returnPickedUp(task.id);
      ref.invalidate(courierOrdersProvider(false));
      ref.invalidate(courierOrdersProvider(true));
      if (mounted) _snack(t.courierReturnDone);
    } on ApiException catch (e) {
      if (mounted) _snack(e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final details = CourierFormat.addressDetails(t, task);
    final pending = task.isReturnPending;
    final label = theme.textTheme.bodySmall?.copyWith(color: AppColors.textSecondary);

    return Card(
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppColors.accent, width: 1.5),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(color: const Color(0xFFFFEDD5), borderRadius: BorderRadius.circular(10)),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                const Icon(Icons.assignment_return_outlined, size: 16, color: AppColors.accent),
                const SizedBox(width: 4),
                Text(t.courierReturnTask, style: const TextStyle(fontWeight: FontWeight.w800, color: AppColors.accent)),
              ]),
            ),
            const Spacer(),
            if (widget.distance != null && pending)
              Text(CourierFormat.distance(t, widget.distance!), style: const TextStyle(fontWeight: FontWeight.w700)),
          ]),
          const SizedBox(height: 10),
          Text(
            [
              t.courierReturnNumber(task.number),
              if (task.orderNumber != null) '#${task.orderNumber}',
            ].join(' · '),
            style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          if (task.customerName != null) Text(task.customerName!, style: label),
          const SizedBox(height: 8),
          Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Padding(
              padding: EdgeInsets.only(top: 2),
              child: Icon(Icons.place_outlined, size: 20, color: AppColors.textSecondary),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(task.addressLine ?? '—', style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                if (details.isNotEmpty) Text(details, style: label),
              ]),
            ),
          ]),
          if (task.items.isNotEmpty) ...[
            const SizedBox(height: 10),
            for (final it in task.items)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Text('• ${it.productName} × ${CourierFormat.number(it.quantity)}',
                    style: theme.textTheme.bodyMedium),
              ),
          ],
          const SizedBox(height: 6),
          Text(t.courierReturnNoCash, style: label),
          if (pending) ...[
            const SizedBox(height: 12),
            Row(children: [
              IconButton.outlined(
                tooltip: t.courierYandexNavi,
                onPressed: () => _open(() => CourierNavigation.yandex(task)),
                icon: const Icon(Icons.navigation_outlined),
              ),
              if (task.customerPhone != null) ...[
                const SizedBox(width: 8),
                IconButton.outlined(
                  tooltip: t.courierCall,
                  onPressed: () => _open(() => CourierNavigation.call(task.customerPhone!)),
                  icon: const Icon(Icons.call_outlined),
                ),
              ],
              const SizedBox(width: 12),
              Expanded(
                child: FilledButton(
                  onPressed: _busy ? null : _pickedUp,
                  child: _busy
                      ? const SizedBox(
                          width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                      : Text(t.courierReturnPickedUp),
                ),
              ),
            ]),
          ] else ...[
            const SizedBox(height: 8),
            Row(children: [
              Icon(Icons.check_circle_outline, size: 18, color: AppColors.primary),
              const SizedBox(width: 6),
              Text(t.courierReturnPickedUp, style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
            ]),
          ],
        ]),
      ),
    );
  }
}
