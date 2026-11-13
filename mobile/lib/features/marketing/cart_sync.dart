import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import 'data/marketing_repository.dart';

/// Savatcha nusxasini serverga yuboradi (`PUT /api/me/cart`, kelishuv 9.4) — "savatchangizda N ta mahsulot qoldi"
/// eslatmasi uchun. Faqat kirgan foydalanuvchi; o'zgarishdan 2 s keyin (debounce). Savatcha bo'shasa yoki
/// do'kon almashsa — eski filial nusxasi bo'sh `items` bilan o'chiriladi. Xatolar jim (keyingi o'zgarishda qayta).
class CartSync {
  CartSync(this._ref) {
    _ref.listen(cartControllerProvider, (_, _) => _schedule());
    _ref.listen(authControllerProvider.select((a) => a.isSignedIn), (_, signedIn) {
      if (signedIn) {
        _lastSent = null;
        _schedule();
      }
    });
    _ref.onDispose(() => _timer?.cancel());
  }

  final Ref _ref;
  Timer? _timer;
  String? _lastSent; // "branchId|qatorlar" — bir xil holat qayta yuborilmaydi
  String? _lastBranch;

  void _schedule() {
    _timer?.cancel();
    _timer = Timer(const Duration(seconds: 2), _send);
  }

  Future<void> _send() async {
    if (!_ref.read(authControllerProvider).isSignedIn) return;
    final cart = _ref.read(cartControllerProvider);
    final repo = _ref.read(marketingRepositoryProvider);
    final branchId = cart.branchId;
    final items = [for (final l in cart.lines.values) (productId: l.productId, quantity: l.quantity)];
    final key = '$branchId|${items.map((i) => '${i.productId}:${i.quantity}').join(',')}';
    if (key == _lastSent) return;
    try {
      // Boshqa filialga o'tildi yoki savatcha bo'shadi — eski nusxa o'chiriladi.
      final old = _lastBranch;
      if (old != null && old != branchId) await repo.syncCart(old, const []);
      if (branchId != null) await repo.syncCart(branchId, items);
      _lastSent = key;
      _lastBranch = branchId;
    } catch (_) {}
  }
}

/// `PosCloudApp` ishga tushganda o'qiladi (tinglovchilar faollashadi).
final cartSyncProvider = Provider<CartSync>((ref) => CartSync(ref));
