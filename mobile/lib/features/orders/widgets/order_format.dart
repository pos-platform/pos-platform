import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/order_models.dart';

/// Buyurtma ekranlari uchun umumiy matn/rang yordamchilari.
class OrderFormat {
  OrderFormat._();

  static final _group = NumberFormat('#,##0.##', 'en_US');

  /// 70500 → "70 500 so'm".
  static String money(AppLocalizations t, double amount) =>
      t.ordersMoney(_group.format(amount).replaceAll(',', ' '));

  /// Miqdor: dona — butun son, tarozi — 0.001 aniqlik.
  static String quantity(double q) => _group.format(q).replaceAll(',', ' ');

  static String time(DateTime d) => '${_two(d.hour)}:${_two(d.minute)}';

  static String _two(int v) => v.toString().padLeft(2, '0');

  static const _uzMonths = [
    'yanvar', 'fevral', 'mart', 'aprel', 'may', 'iyun', //
    'iyul', 'avgust', 'sentabr', 'oktabr', 'noyabr', 'dekabr',
  ];
  static const _ruMonths = [
    'января', 'февраля', 'марта', 'апреля', 'мая', 'июня', //
    'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря',
  ];

  /// "Bugun, 13:42" / "8-noyabr, 19:10" (ru: "8 ноября, 19:10").
  static String dateTime(BuildContext context, AppLocalizations t, DateTime d) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final day = DateTime(d.year, d.month, d.day);
    final diff = today.difference(day).inDays;
    final String date;
    if (diff == 0) {
      date = t.ordersToday;
    } else if (diff == 1) {
      date = t.ordersYesterday;
    } else {
      final ru = Localizations.localeOf(context).languageCode == 'ru';
      date = ru ? '${d.day} ${_ruMonths[d.month - 1]}' : '${d.day}-${_uzMonths[d.month - 1]}';
      if (d.year != now.year) return '$date ${d.year}, ${time(d)}';
    }
    return '$date, ${time(d)}';
  }

  static String fulfillment(AppLocalizations t, FulfillmentType f) =>
      f == FulfillmentType.pickup ? t.ordersPickup : t.ordersDelivery;

  static String status(AppLocalizations t, OrderStatus s, FulfillmentType f) => switch (s) {
        OrderStatus.created => t.ordersStatusNew,
        OrderStatus.accepted => t.ordersStatusAccepted,
        OrderStatus.picking => t.ordersStatusPicking,
        OrderStatus.ready => t.ordersStatusReady,
        OrderStatus.awaitingTransfer => t.ordersStatusAwaitingTransfer,
        OrderStatus.onTheWay => t.ordersStatusOnTheWay,
        OrderStatus.readyForPickup => t.ordersStatusReadyForPickup,
        OrderStatus.delivered => f == FulfillmentType.pickup ? t.ordersStatusPickedUp : t.ordersStatusDelivered,
        OrderStatus.rejected => t.ordersStatusRejected,
        OrderStatus.cancelled => t.ordersStatusCancelled,
      };

  static String headline(AppLocalizations t, OrderStatus s) => switch (s) {
        OrderStatus.created => t.ordersHeadlineNew,
        OrderStatus.accepted => t.ordersHeadlineAccepted,
        OrderStatus.picking => t.ordersHeadlinePicking,
        OrderStatus.ready => t.ordersHeadlineReady,
        OrderStatus.awaitingTransfer => t.ordersHeadlineAwaitingTransfer,
        OrderStatus.onTheWay => t.ordersHeadlineOnTheWay,
        OrderStatus.readyForPickup => t.ordersHeadlineReadyForPickup,
        OrderStatus.delivered => t.ordersHeadlineDelivered,
        OrderStatus.rejected => t.ordersRejectedTitle,
        OrderStatus.cancelled => t.ordersCancelledTitle,
      };

  static String paymentStatus(AppLocalizations t, PaymentStatus s) => switch (s) {
        PaymentStatus.unpaid => t.ordersPaymentUnpaid,
        PaymentStatus.paid => t.ordersPaymentPaid,
        PaymentStatus.refunded => t.ordersPaymentRefunded,
      };

  static String paymentMethod(AppLocalizations t, PaymentMethod m) => switch (m) {
        PaymentMethod.transfer => t.ordersMethodTransfer,
        PaymentMethod.card => t.ordersMethodCard,
        _ => t.ordersMethodCash,
      };

  /// Status "pill" ranglari (dizayn: faol — to'q sariq, yakunlangan — yashil, bekor — kulrang).
  static (Color bg, Color fg) pillColors(OrderStatus s) {
    if (s.isFailed) return (AppColors.chipSurface, AppColors.textSecondary);
    if (s == OrderStatus.delivered) return (AppColors.primarySoft, AppColors.primary);
    return (const Color(0xFFFDEBDD), const Color(0xFF9A3412));
  }
}

/// Holat "pill"i.
class OrderStatusPill extends StatelessWidget {
  const OrderStatusPill({super.key, required this.status, required this.fulfillment});

  final OrderStatus status;
  final FulfillmentType fulfillment;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final (bg, fg) = OrderFormat.pillColors(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Text(
        OrderFormat.status(t, status, fulfillment),
        style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: fg),
      ),
    );
  }
}

/// Kichik mahsulot rasmi (rasm yo'q yoki yuklanmasa — rangli kvadrat).
class OrderThumb extends StatelessWidget {
  const OrderThumb({super.key, required this.url, this.size = 40, this.index = 0});

  final String? url;
  final double size;
  final int index;

  static const _placeholders = [Color(0xFFE8EEF6), Color(0xFFF7F1DE), Color(0xFFEEEAF5)];

  @override
  Widget build(BuildContext context) {
    final bg = _placeholders[index % _placeholders.length];
    final placeholder = Container(width: size, height: size, color: bg);
    return ClipRRect(
      borderRadius: BorderRadius.circular(size / 4),
      child: url == null
          ? placeholder
          : Image.network(url!, width: size, height: size, fit: BoxFit.cover,
              errorBuilder: (_, _, _) => placeholder),
    );
  }
}
