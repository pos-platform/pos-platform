import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../l10n/app_localizations.dart';
import '../cart/cart_controller.dart';
import '../cart/data/order_api.dart' as cart_api;
import '../catalog/store_selection.dart';
import 'data/orders_repository.dart';

/// "Qayta buyurtma": buyurtma tarkibini (olib tashlanmagan qatorlar) o'sha do'kon savatchasiga qo'yadi va `/cart` ga o'tadi.
/// Narx/qoldiq savatchada server hisobidan (`/quote`) olinadi — mavjud bo'lmagan mahsulotlar u yerda belgilanadi.
/// Qaytaradi: savatchaga qo'shildimi.
Future<bool> reorderToCart(BuildContext context, WidgetRef ref, String orderId) async {
  final t = AppLocalizations.of(context);
  final messenger = ScaffoldMessenger.of(context);
  try {
    final order = await ref.read(ordersRepositoryProvider).get(orderId);

    // Bir mahsulot bir necha qatorda bo'lishi mumkin (almashtirish) — miqdorlar qo'shiladi.
    final qty = <String, double>{};
    final source = <String, ({String name, String? image, double price})>{};
    for (final i in order.items) {
      if (i.isRemoved || i.productId.isEmpty) continue;
      final q = i.pickedQuantity ?? i.quantity;
      if (q <= 0) continue;
      qty[i.productId] = CartState.round((qty[i.productId] ?? 0) + q);
      source[i.productId] = (name: i.productName, image: i.imageUrl, price: i.unitPrice);
    }
    if (qty.isEmpty || order.branchId.isEmpty) {
      messenger.showSnackBar(SnackBar(content: Text(t.ordersReorderEmpty)));
      return false;
    }

    // Buyurtma qatorida birlik yo'q — kasr miqdor bo'lsa tarozi tovari deb taxmin qilinadi.
    var lines = [
      for (final e in qty.entries)
        CartLine(
          productId: e.key,
          name: source[e.key]!.name,
          imageUrl: source[e.key]!.image,
          unit: e.value == e.value.roundToDouble() ? 'Piece' : 'Kilogram',
          isWeighed: e.value != e.value.roundToDouble(),
          price: source[e.key]!.price,
          quantity: e.value,
        ),
    ];

    // Joriy nom, birlik, narx va qoldiqni serverdan olishga urinamiz (xatoda — buyurtmadagi ma'lumot qoladi).
    try {
      final quote = await ref.read(cart_api.orderApiProvider).quote(order.branchId, cart_api.FulfillmentType.pickup, lines);
      lines = [
        for (final l in lines)
          switch (quote.line(l.productId)) {
            final q? when q.problem != 'not_found' => CartLine(
                productId: l.productId,
                name: q.name.isEmpty ? l.name : q.name,
                imageUrl: q.imageUrl ?? l.imageUrl,
                unit: q.unit,
                isWeighed: q.isWeighed,
                price: q.unitPrice,
                quantity: l.quantity,
                available: q.available,
              ),
            _ => l,
          },
      ];
    } catch (_) {}

    if (!context.mounted) return false;
    final cart = ref.read(cartControllerProvider.notifier);
    if (cart.conflictsWith(order.branchId)) {
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: Text(t.catSwitchStoreTitle),
          content: Text(t.catSwitchStoreBody),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.catCancel)),
            TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.catSwitchStoreConfirm)),
          ],
        ),
      );
      if (ok != true || !context.mounted) return false;
    }
    cart.addLines(order.branchId, lines);
    await ref.read(storeSelectionProvider.notifier).select(order.branchId);
    if (context.mounted) context.go('/cart');
    return true;
  } catch (e) {
    messenger.showSnackBar(SnackBar(content: Text(ApiException.from(e).message)));
    return false;
  }
}
