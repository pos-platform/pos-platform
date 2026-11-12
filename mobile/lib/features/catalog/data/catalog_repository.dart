import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import 'catalog_models.dart';

/// Katalog API — tokensiz (mehmon ham ko'radi).
class CatalogRepository {
  CatalogRepository(this._dio);

  final Dio _dio;

  Future<List<ShopStore>> stores({String? search, double? lat, double? lng}) => apiCall(() async {
        final r = await _dio.get<List<dynamic>>('/api/shop/stores', queryParameters: {
          if (search != null && search.trim().isNotEmpty) 'search': search.trim(),
          'lat': ?lat,
          'lng': ?lng,
        });
        return (r.data ?? const []).map((e) => ShopStore.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<ShopStore> store(String branchId) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/shop/stores/$branchId');
        return ShopStore.fromJson(r.data!);
      });

  Future<List<ShopCategory>> categories(String branchId) => apiCall(() async {
        final r = await _dio.get<List<dynamic>>('/api/shop/stores/$branchId/categories');
        return (r.data ?? const []).map((e) => ShopCategory.fromJson(e as Map<String, dynamic>)).toList();
      });

  Future<PagedResult<ShopProduct>> products(
    String branchId, {
    String? search,
    String? categoryId,
    ProductSort sort = ProductSort.popular,
    int page = 1,
    int pageSize = 30,
    CancelToken? cancelToken,
  }) =>
      apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>(
          '/api/shop/stores/$branchId/products',
          queryParameters: {
            if (search != null && search.trim().isNotEmpty) 'search': search.trim(),
            'categoryId': ?categoryId,
            'sort': sort.api,
            'page': page,
            'pageSize': pageSize,
          },
          cancelToken: cancelToken,
        );
        return PagedResult.fromJson(r.data!, ShopProduct.fromJson);
      });

  Future<ShopProductDetail> product(String branchId, String productId) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/shop/stores/$branchId/products/$productId');
        return ShopProductDetail.fromJson(r.data!);
      });
}

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) => CatalogRepository(ref.watch(dioProvider)));

/// Qayta urinmaslik — ekranda "Qayta urinish" tugmasi bor.
Duration? noRetry(int retryCount, Object error) => null;

/// Bitta do'kon (sozlamalari bilan: ish vaqti, yetkazish, to'lov usullari).
final storeProvider = FutureProvider.autoDispose.family<ShopStore, String>(
  (ref, branchId) => ref.watch(catalogRepositoryProvider).store(branchId),
  retry: noRetry,
);

final categoriesProvider = FutureProvider.autoDispose.family<List<ShopCategory>, String>(
  (ref, branchId) => ref.watch(catalogRepositoryProvider).categories(branchId),
  retry: noRetry,
);

final productDetailProvider =
    FutureProvider.autoDispose.family<ShopProductDetail, ({String branchId, String productId})>(
  (ref, a) => ref.watch(catalogRepositoryProvider).product(a.branchId, a.productId),
  retry: noRetry,
);
