import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import 'order_models.dart';

/// `/api/shop/orders` — mijoz buyurtmalari (faqat kirgan foydalanuvchi uchun).
class OrdersRepository {
  OrdersRepository(this._dio);

  final Dio _dio;

  static const _base = '/api/shop/orders';

  /// [active]: true — faol, false — tarix.
  Future<OrdersPage> list({required bool active, int page = 1, int pageSize = 20}) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>(_base,
            queryParameters: {'active': active, 'page': page, 'pageSize': pageSize});
        return OrdersPage.fromJson(r.data ?? const {});
      });

  Future<OrderDetail> get(String id) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('$_base/$id');
        return OrderDetail.fromJson(r.data!);
      });

  Future<OrderDetail> cancel(String id, {String? reason}) => apiCall(() async {
        final r = await _dio.post<Map<String, dynamic>>('$_base/$id/cancel',
            data: {'reason': (reason?.trim().isEmpty ?? true) ? null : reason!.trim()});
        return OrderDetail.fromJson(r.data!);
      });

  /// Online to'lov havolasi: `POST {id}/pay {provider}` → `{ paymentUrl }` (Payme/Click sahifasi).
  Future<String> payOnline(String id, String provider) => apiCall(() async {
        final r = await _dio.post<Map<String, dynamic>>('$_base/$id/pay', data: {'provider': provider});
        return r.data!['paymentUrl'] as String;
      });

  /// O'tkazma cheki rasmi (multipart "file", server 5 MB gacha qabul qiladi).
  Future<OrderDetail> uploadTransferProof(String id, {required List<int> bytes, required String fileName}) =>
      apiCall(() async {
        final form = FormData.fromMap({'file': MultipartFile.fromBytes(bytes, filename: fileName)});
        final r = await _dio.post<Map<String, dynamic>>('$_base/$id/transfer-proof', data: form);
        return OrderDetail.fromJson(r.data!);
      });
}

final ordersRepositoryProvider = Provider<OrdersRepository>((ref) => OrdersRepository(ref.watch(dioProvider)));

/// Buyurtmalar ro'yxati (sahifalab). Argument: true — faol, false — tarix.
class OrdersListNotifier extends AsyncNotifier<OrdersPage> {
  OrdersListNotifier(this.active);

  final bool active;
  bool _loadingMore = false;

  static const _pageSize = 20;

  @override
  Future<OrdersPage> build() =>
      ref.read(ordersRepositoryProvider).list(active: active, pageSize: _pageSize);

  /// Keyingi sahifani qo'shadi (cheksiz aylantirish).
  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || _loadingMore) return;
    _loadingMore = true;
    try {
      final next = await ref
          .read(ordersRepositoryProvider)
          .list(active: active, page: current.page + 1, pageSize: _pageSize);
      final seen = current.items.map((e) => e.id).toSet();
      state = AsyncData(OrdersPage(
        items: [...current.items, ...next.items.where((e) => !seen.contains(e.id))],
        totalCount: next.totalCount,
        page: next.page,
        pageSize: next.pageSize,
      ));
    } catch (_) {
      // Keyingi sahifa yuklanmadi — mavjud ro'yxat qoladi, foydalanuvchi qayta aylantirib urinadi.
    } finally {
      _loadingMore = false;
    }
  }
}

final ordersListProvider = AsyncNotifierProvider.autoDispose.family<OrdersListNotifier, OrdersPage, bool>(
  OrdersListNotifier.new,
);

/// Bitta buyurtma (kuzatish ekrani). Bekor qilish va chek yuklash natijasi shu holatga yoziladi.
class OrderDetailNotifier extends AsyncNotifier<OrderDetail> {
  OrderDetailNotifier(this.id);

  final String id;

  OrdersRepository get _repo => ref.read(ordersRepositoryProvider);

  @override
  Future<OrderDetail> build() => _repo.get(id);

  /// Jimgina yangilash (polling / SignalR): xatoda eski ma'lumot qoladi.
  Future<void> refreshSilently() async {
    try {
      state = AsyncData(await _repo.get(id));
    } catch (_) {}
  }

  Future<void> cancel(String? reason) async {
    state = AsyncData(await _repo.cancel(id, reason: reason));
    ref.invalidate(ordersListProvider);
  }

  Future<void> uploadTransferProof(List<int> bytes, String fileName) async {
    state = AsyncData(await _repo.uploadTransferProof(id, bytes: bytes, fileName: fileName));
    ref.invalidate(ordersListProvider);
  }
}

final orderDetailProvider = AsyncNotifierProvider.autoDispose.family<OrderDetailNotifier, OrderDetail, String>(
  OrderDetailNotifier.new,
);
