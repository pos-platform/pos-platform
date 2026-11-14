import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../../core/api/api_client.dart';
import 'courier_models.dart';
import 'courier_session.dart';

/// Riverpod 3 avtomatik qayta urinishini o'chiradi (xato darhol ko'rsatiladi).
Duration? courierNoRetry(int retryCount, Object error) => null;

/// `/api/courier/*` endpointlari.
class CourierRepository {
  CourierRepository(this._dio);

  final Dio _dio;

  Future<CourierMe> me() => apiCall(() async {
        final res = await _dio.get<Map<String, dynamic>>('/api/courier/me');
        return CourierMe.fromJson(res.data!);
      });

  Future<List<CourierOrder>> orders({required bool history}) => apiCall(() async {
        final res = await _dio.get<List<dynamic>>('/api/courier/orders', queryParameters: {'history': history});
        return (res.data ?? const []).whereType<Map<String, dynamic>>().map(CourierOrder.fromJson).toList();
      });

  Future<CourierOrder> order(String id) => apiCall(() async {
        final res = await _dio.get<Map<String, dynamic>>('/api/courier/orders/$id');
        return CourierOrder.fromJson(res.data!);
      });

  /// [rejectedItems] — eshik oldida mijoz olmagan qatorlar (kelishuv 10.5); bo'sh bo'lsa yuborilmaydi.
  Future<void> delivered(String id, {double? cashCollected, List<CourierRejectedItem> rejectedItems = const []}) =>
      apiCall(() async {
        await _dio.post<dynamic>('/api/courier/orders/$id/delivered', data: {
          'cashCollected': ?cashCollected,
          if (rejectedItems.isNotEmpty) 'rejectedItems': [for (final r in rejectedItems) r.toJson()],
        });
      });

  Future<void> failed(String id, String reason) => apiCall(() async {
        await _dio.post<dynamic>('/api/courier/orders/$id/failed', data: {'reason': reason});
      });

  /// Qabul qilingan buyurtmani do'kondan oldi → `OnTheWay` (kelishuv 10.6).
  Future<void> pickedUp(String orderId) => apiCall(() async {
        await _dio.post<dynamic>('/api/courier/orders/$orderId/picked-up');
      });

  /// Qaytarish tovarini mijozdan olib ketdi (kelishuv 10.5).
  Future<void> returnPickedUp(String returnId) => apiCall(() async {
        await _dio.post<dynamic>('/api/courier/returns/$returnId/picked-up');
      });

  /// Takliflar ro'yxati. Massiv yoki `{ items }` ko'rinishida kelsa ham o'qiladi.
  Future<List<CourierOffer>> offers() => apiCall(() async {
        final res = await _dio.get<dynamic>('/api/courier/offers');
        final data = res.data;
        final list = data is List ? data : (data is Map ? data['items'] as List? : null) ?? const [];
        return [
          for (final e in list)
            if (e is Map) CourierOffer.fromJson(Map<String, dynamic>.from(e)),
        ];
      });

  /// 409 — boshqa kuryer oldi ([ApiException.statusCode] tekshiriladi).
  Future<void> acceptOffer(String offerId) => apiCall(() async {
        await _dio.post<dynamic>('/api/courier/offers/$offerId/accept');
      });

  Future<void> declineOffer(String offerId, {String? reason}) => apiCall(() async {
        await _dio.post<dynamic>('/api/courier/offers/$offerId/decline', data: {'reason': ?reason});
      });
}

final courierRepositoryProvider = Provider<CourierRepository>((ref) => CourierRepository(ref.watch(courierDioProvider)));

final courierMeProvider = FutureProvider<CourierMe>(
  (ref) => ref.watch(courierRepositoryProvider).me(),
  retry: courierNoRetry,
);

/// `history=false` — faol (yo'lda), `true` — bugungi yakunlanganlar.
final courierOrdersProvider = FutureProvider.family<List<CourierOrder>, bool>(
  (ref, history) => ref.watch(courierRepositoryProvider).orders(history: history),
  retry: courierNoRetry,
);

/// Kutilayotgan takliflar (kelishuv 10.6). Push / SignalR kelganda qayta yuklanadi.
final courierOffersProvider = FutureProvider<List<CourierOffer>>(
  (ref) => ref.watch(courierRepositoryProvider).offers(),
  retry: courierNoRetry,
);

final courierOrderProvider = FutureProvider.autoDispose.family<CourierOrder, String>(
  (ref, id) => ref.watch(courierRepositoryProvider).order(id),
  retry: courierNoRetry,
);

/// Kuryerning joriy joylashuvi (yangilash — `ref.invalidate`). Ruxsat yo'q / xato — null.
final courierLocationProvider = FutureProvider<({double lat, double lng})?>((ref) async {
  try {
    if (!await Geolocator.isLocationServiceEnabled()) return null;
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) return null;
    Position? pos;
    try {
      pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, timeLimit: Duration(seconds: 8)),
      );
    } catch (_) {
      pos = await Geolocator.getLastKnownPosition();
    }
    return pos == null ? null : (lat: pos.latitude, lng: pos.longitude);
  } catch (_) {
    return null;
  }
}, retry: courierNoRetry);
