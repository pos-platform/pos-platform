import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/return_models.dart';

/// Qaytarish sababi matni (mijoz va kuryer ekranlarida bir xil).
String returnReasonLabel(AppLocalizations t, ReturnReason r) => switch (r) {
      ReturnReason.damaged => t.returnReasonDamaged,
      ReturnReason.wrongItem => t.returnReasonWrongItem,
      ReturnReason.quality => t.returnReasonQuality,
      ReturnReason.expired => t.returnReasonExpired,
      ReturnReason.missing => t.returnReasonMissing,
      ReturnReason.other => t.returnReasonOther,
    };

String refundMethodLabel(AppLocalizations t, RefundMethod m) => switch (m) {
      RefundMethod.cash => t.refundMethodCash,
      RefundMethod.transfer => t.refundMethodTransfer,
      RefundMethod.bonus => t.refundMethodBonus,
    };

String returnMethodLabel(AppLocalizations t, ReturnMethod m) => switch (m) {
      ReturnMethod.courierPickup => t.returnMethodCourier,
      ReturnMethod.bringToStore => t.returnMethodStore,
    };

String returnStatusLabel(AppLocalizations t, ReturnStatus s) => switch (s) {
      ReturnStatus.requested => t.returnStatusRequested,
      ReturnStatus.approved => t.returnStatusApproved,
      ReturnStatus.pickupAssigned => t.returnStatusPickupAssigned,
      ReturnStatus.pickedUp => t.returnStatusPickedUp,
      ReturnStatus.received => t.returnStatusReceived,
      ReturnStatus.refunded => t.returnStatusRefunded,
      ReturnStatus.rejected => t.returnStatusRejected,
      ReturnStatus.cancelled => t.returnStatusCancelled,
    };

/// Holat "pill"i: yakunlangan — brend rangi, bekor/rad — kulrang, jarayonda — to'q sariq.
class ReturnStatusPill extends StatelessWidget {
  const ReturnStatusPill({super.key, required this.status});

  final ReturnStatus status;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final (Color bg, Color fg) = status.isFailed
        ? (AppColors.chipSurface, AppColors.textSecondary)
        : status == ReturnStatus.refunded
            ? (AppColors.primarySoft, AppColors.primary)
            : (const Color(0xFFFDEBDD), AppColors.accent);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Text(returnStatusLabel(t, status), style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: fg)),
    );
  }
}
