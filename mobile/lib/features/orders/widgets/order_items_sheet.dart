import 'package:material_ui/material_ui.dart';

import '../../../core/config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/order_models.dart';
import 'order_format.dart';

/// "Tarkibi" — buyurtma qatorlari (olib tashlangan va almashtirilganlar belgilanadi) va summalar.
Future<void> showOrderItemsSheet(BuildContext context, OrderDetail order) => showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(22))),
      builder: (_) => _OrderItemsSheet(order: order),
    );

class _OrderItemsSheet extends StatelessWidget {
  const _OrderItemsSheet({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.7,
      maxChildSize: 0.92,
      builder: (context, controller) => ListView(
        controller: controller,
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
        children: [
          Text(t.ordersContents, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
          const SizedBox(height: 12),
          for (final item in order.items) _ItemRow(item: item),
          const Divider(height: 24, color: AppColors.border),
          _totalRow(t.ordersItemsTotal, OrderFormat.money(t, order.itemsTotal)),
          if (order.promoDiscount > 0)
            _totalRow(order.promoCode == null ? t.cartPromoDiscount : '${t.cartPromoDiscount} (${order.promoCode})',
                '−${OrderFormat.money(t, order.promoDiscount)}'),
          if (order.bonusUsed > 0) _totalRow(t.cartBonusUsed, '−${OrderFormat.money(t, order.bonusUsed)}'),
          if (order.fulfillmentType == FulfillmentType.delivery)
            _totalRow(t.ordersDeliveryFee, OrderFormat.money(t, order.deliveryFee)),
          const SizedBox(height: 4),
          _totalRow(t.ordersTotal, OrderFormat.money(t, order.totalAmount), bold: true),
        ],
      ),
    );
  }

  Widget _totalRow(String label, String value, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          children: [
            Expanded(
              child: Text(label,
                  style: TextStyle(
                    fontSize: bold ? 16 : 14,
                    fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
                    color: bold ? AppColors.text : AppColors.textSecondary,
                  )),
            ),
            Text(value, style: TextStyle(fontSize: bold ? 16 : 14, fontWeight: bold ? FontWeight.w800 : FontWeight.w700)),
          ],
        ),
      );
}

class _ItemRow extends StatelessWidget {
  const _ItemRow({required this.item});

  final OrderItem item;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final qty = item.pickedQuantity ?? item.quantity;
    final removed = item.isRemoved;
    final tag = removed ? t.ordersItemRemoved : (item.isReplacement ? t.ordersItemReplacement : null);

    return Opacity(
      opacity: removed ? 0.55 : 1,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Row(
          children: [
            OrderThumb(url: AppConfig.imageUrl(item.imageUrl), size: 48),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.productName,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                      decoration: removed ? TextDecoration.lineThrough : null,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${OrderFormat.quantity(qty)} × ${OrderFormat.money(t, item.unitPrice)}',
                    style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                  ),
                  if (tag != null) ...[
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: removed ? const Color(0xFFFDECEA) : const Color(0xFFFDEBDD),
                        borderRadius: BorderRadius.circular(999),
                      ),
                      child: Text(tag,
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w800,
                            color: removed ? AppColors.danger : const Color(0xFF9A3412),
                          )),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 8),
            Text(
              OrderFormat.money(t, item.lineTotal),
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w800,
                decoration: removed ? TextDecoration.lineThrough : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
