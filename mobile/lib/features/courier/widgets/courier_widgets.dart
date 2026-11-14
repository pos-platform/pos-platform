import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/courier_models.dart';
import '../data/courier_repository.dart';
import '../data/courier_session.dart';
import '../../../core/config.dart';
import '../../../core/store_app/store_branding.dart';

/// Kuryer ekranlari uchun formatlash.
class CourierFormat {
  CourierFormat._();

  static final _group = NumberFormat('#,##0.##', 'en_US');

  static String number(double v) => _group.format(v).replaceAll(',', ' ');

  static String money(AppLocalizations t, double amount) => t.courierMoney(number(amount));

  static String distance(AppLocalizations t, double meters) => meters < 1000
      ? t.courierDistanceM(meters.round().toString())
      : t.courierDistanceKm((meters / 1000).toStringAsFixed(meters < 10000 ? 1 : 0));

  /// Podyezd / qavat / xonadon / domofon bitta qatorda.
  static String addressDetails(AppLocalizations t, CourierOrder o) => [
        if (o.entrance != null) '${t.courierEntrance} ${o.entrance}',
        if (o.floor != null) '${t.courierFloor} ${o.floor}',
        if (o.apartment != null) '${t.courierApartment} ${o.apartment}',
        if (o.intercom != null) '${t.courierIntercom} ${o.intercom}',
      ].join(' · ');
}

/// To'lov holati: naqd to'lanmagan — "X so'm olish kerak" (ajratilgan), to'langan — "To'langan".
class CourierPaymentBadge extends StatelessWidget {
  const CourierPaymentBadge({super.key, required this.order, this.large = false});

  final CourierOrder order;
  final bool large;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final (String text, Color bg, Color fg, IconData icon) = order.mustCollectCash
        ? (t.courierCollectCash(CourierFormat.money(t, order.totalAmount)), const Color(0xFFFFEDD5), AppColors.accent, Icons.payments_outlined)
        : order.isPaid
            ? (t.courierPaid, AppColors.primarySoft, AppColors.primary, Icons.check_circle_outline)
            : (t.courierPaymentPending, AppColors.chipSurface, AppColors.textSecondary, Icons.schedule);
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 12, vertical: large ? 12 : 8),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(12)),
      child: Row(mainAxisSize: large ? MainAxisSize.max : MainAxisSize.min, children: [
        Icon(icon, size: large ? 22 : 18, color: fg),
        const SizedBox(width: 8),
        Flexible(
          child: Text(text,
              style: TextStyle(color: fg, fontWeight: FontWeight.w800, fontSize: large ? 16 : 14)),
        ),
      ]),
    );
  }
}

/// Kuryer ismi + chiqish.
class CourierProfileChip extends ConsumerWidget {
  const CourierProfileChip({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final session = ref.watch(courierAuthProvider.select((s) => s.session));
    final me = ref.watch(courierMeProvider).value;
    final name = me?.name.isNotEmpty == true ? me!.name : session?.displayName ?? t.courierMode;

    return PopupMenuButton<int>(
      tooltip: '',
      position: PopupMenuPosition.under,
      onSelected: (_) => confirmCourierLogout(context, ref),
      itemBuilder: (_) => [
        // Do'kon rejimida tashkilot/filial nomi o'rniga brend yozuvi (Xo‘ja market).
        if (AppConfig.isStoreMode)
          const PopupMenuItem<int>(enabled: false, child: BrandWordmark(fontSize: 16))
        else if (me?.storeName != null || me?.branchName != null)
          PopupMenuItem<int>(
            enabled: false,
            child: Text([me?.storeName, me?.branchName].whereType<String>().join(' · ')),
          ),
        PopupMenuItem<int>(
          value: 1,
          child: Row(children: [
            const Icon(Icons.logout, color: AppColors.danger),
            const SizedBox(width: 10),
            Text(t.courierLogout, style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w700)),
          ]),
        ),
      ],
      child: Container(
        margin: const EdgeInsets.only(right: 12),
        padding: const EdgeInsets.fromLTRB(6, 6, 12, 6),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(24),
          border: Border.all(color: AppColors.borderSoft),
        ),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          CircleAvatar(
            radius: 14,
            backgroundColor: AppColors.primarySoft,
            child: Icon(Icons.delivery_dining, size: 18, color: AppColors.primary),
          ),
          const SizedBox(width: 8),
          ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 140),
            child: Text(name, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w700)),
          ),
        ]),
      ),
    );
  }
}

Future<void> confirmCourierLogout(BuildContext context, WidgetRef ref) async {
  final t = AppLocalizations.of(context);
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      content: Text(t.courierLogoutConfirm),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.courierCancel)),
        TextButton(
          onPressed: () => Navigator.pop(ctx, true),
          child: Text(t.courierLogout, style: const TextStyle(color: AppColors.danger)),
        ),
      ],
    ),
  );
  // Router mijoz login ekraniga o'zi yo'naltiradi.
  if (ok == true) await ref.read(courierAuthProvider.notifier).logout();
}

/// Yuklashda xato — qayta urinish tugmasi bilan.
class CourierErrorView extends StatelessWidget {
  const CourierErrorView({super.key, required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const Icon(Icons.cloud_off, size: 48, color: AppColors.iconMuted),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
          const SizedBox(height: 16),
          OutlinedButton(onPressed: onRetry, child: Text(t.courierRetry)),
        ]),
      ),
    );
  }
}
