import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../auth/auth_controller.dart';
import '../../catalog/data/catalog_models.dart';
import '../../catalog/data/catalog_repository.dart';
import 'marketing_models.dart';

/// Bosqich 9 API'lari: katalog qismi tokensiz, `/api/me/*` — faqat kirgan foydalanuvchi.
class MarketingRepository {
  MarketingRepository(this._dio);

  final Dio _dio;

  String _store(String branchId) => '/api/shop/stores/$branchId';

  // ---- Katalog (tokensiz; token bo'lsa `buyAgain`/`isFavorite` to'ldiriladi) ----

  Future<ShopHome> home(String branchId) => apiCall(() async {
    final r = await _dio.get<Map<String, dynamic>>('${_store(branchId)}/home');
    return ShopHome.fromJson(r.data ?? const {});
  });

  Future<ShopCollection> collection(String branchId, String id) => apiCall(() async {
    final r = await _dio.get<Map<String, dynamic>>('${_store(branchId)}/collections/$id');
    return ShopCollection.fromJson(r.data ?? const {});
  });

  Future<List<ShopProduct>> related(String branchId, String productId, {int limit = 10}) => apiCall(() async {
    final r = await _dio.get<List<dynamic>>(
      '${_store(branchId)}/products/$productId/related',
      queryParameters: {'limit': limit},
    );
    return ShopProduct.listFrom(r.data);
  });

  Future<List<ShopProduct>> cartSuggestions(String branchId, List<String> productIds) => apiCall(() async {
    final r = await _dio.post<List<dynamic>>('${_store(branchId)}/cart-suggestions', data: {'productIds': productIds});
    return ShopProduct.listFrom(r.data);
  });

  Future<SearchSuggest> searchSuggest(String branchId, String q, {CancelToken? cancelToken}) => apiCall(() async {
    final r = await _dio.get<Map<String, dynamic>>(
      '${_store(branchId)}/search-suggest',
      queryParameters: {'q': q},
      cancelToken: cancelToken,
    );
    return SearchSuggest.fromJson(r.data ?? const {});
  });

  // ---- Mijoz (`Shop` policy) ----

  Future<List<ShopProduct>> favorites(String branchId) => apiCall(() async {
    final r = await _dio.get<List<dynamic>>('/api/me/favorites', queryParameters: {'branchId': branchId});
    return ShopProduct.listFrom(r.data);
  });

  Future<Set<String>> favoriteIds() => apiCall(() async {
    final r = await _dio.get<List<dynamic>>('/api/me/favorites/ids');
    return {for (final e in r.data ?? const []) '$e'};
  });

  Future<void> setFavorite(String productId, bool value) => apiCall(() async {
    if (value) {
      await _dio.put<void>('/api/me/favorites/$productId');
    } else {
      await _dio.delete<void>('/api/me/favorites/$productId');
    }
  });

  Future<void> setStockSubscription(String branchId, String productId, bool value) => apiCall(() async {
    if (value) {
      await _dio.put<void>('/api/me/stock-subscriptions', data: {'productId': productId, 'branchId': branchId});
    } else {
      await _dio.delete<void>('/api/me/stock-subscriptions/$productId', queryParameters: {'branchId': branchId});
    }
  });

  Future<ReferralInfo> referral(String organizationId) => apiCall(() async {
    final r = await _dio.get<Map<String, dynamic>>(
      '/api/me/referral',
      queryParameters: {'organizationId': organizationId},
    );
    return ReferralInfo.fromJson(r.data ?? const {});
  });

  Future<void> applyReferral(String organizationId, String code) => apiCall(() async {
    await _dio.post<void>('/api/me/referral/apply', data: {'organizationId': organizationId, 'code': code.trim()});
  });

  /// Savatcha nusxasi (eslatma push uchun). Bo'sh [items] — serverdagi nusxa o'chiriladi.
  Future<void> syncCart(String branchId, List<({String productId, double quantity})> items) => apiCall(() async {
    await _dio.put<void>(
      '/api/me/cart',
      data: {
        'branchId': branchId,
        'items': [
          for (final i in items) {'productId': i.productId, 'quantity': i.quantity},
        ],
      },
    );
  });

  Future<void> rateOrder(String orderId, {required int rating, String? comment, List<String> tags = const []}) =>
      apiCall(() async {
        await _dio.post<void>(
          '/api/shop/orders/$orderId/rate',
          data: {'rating': rating, 'comment': (comment?.trim().isEmpty ?? true) ? null : comment!.trim(), 'tags': tags},
        );
      });
}

final marketingRepositoryProvider = Provider<MarketingRepository>((ref) => MarketingRepository(ref.watch(dioProvider)));

/// Bosh sahifa bloklari. Kirish holati o'zgarsa qayta olinadi (`buyAgain` token bilan to'ladi).
final shopHomeProvider = FutureProvider.autoDispose.family<ShopHome, String>((ref, branchId) {
  ref.watch(authControllerProvider.select((a) => a.isSignedIn));
  return ref.watch(marketingRepositoryProvider).home(branchId);
}, retry: noRetry);

final collectionProvider = FutureProvider.autoDispose.family<ShopCollection, ({String branchId, String id})>(
  (ref, a) => ref.watch(marketingRepositoryProvider).collection(a.branchId, a.id),
  retry: noRetry,
);

final relatedProductsProvider = FutureProvider.autoDispose
    .family<List<ShopProduct>, ({String branchId, String productId})>(
      (ref, a) => ref.watch(marketingRepositoryProvider).related(a.branchId, a.productId),
      retry: noRetry,
    );

/// Savatcha tarkibi kaliti — vergul bilan ajratilgan, tartiblangan ID'lar (miqdor o'zgarsa qayta so'ralmaydi).
final cartSuggestionsProvider = FutureProvider.autoDispose.family<List<ShopProduct>, ({String branchId, String ids})>((
  ref,
  a,
) {
  final ids = a.ids.split(',').where((e) => e.isNotEmpty).toList();
  if (ids.isEmpty) return const <ShopProduct>[];
  return ref.watch(marketingRepositoryProvider).cartSuggestions(a.branchId, ids);
}, retry: noRetry);

/// Sevimlilar ro'yxati (Profil → "Sevimlilar").
final favoriteProductsProvider = FutureProvider.autoDispose.family<List<ShopProduct>, String>(
  (ref, branchId) => ref.watch(marketingRepositoryProvider).favorites(branchId),
  retry: noRetry,
);
