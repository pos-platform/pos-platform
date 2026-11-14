import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import 'return_models.dart';

/// Qaytarish uchun tanlangan rasm (baytlar + fayl nomi).
class ReturnPhoto {
  const ReturnPhoto({required this.bytes, required this.fileName});

  final List<int> bytes;
  final String fileName;
}

/// Mijoz qaytarishlari (kelishuv 10.5): `/api/shop/orders/{id}/returns`, `/api/shop/returns`.
class ReturnsRepository {
  ReturnsRepository(this._dio);

  final Dio _dio;

  /// Multipart: `items` (JSON), `reason`, `comment`, `refundMethod`, `photos[]` (3 tagacha).
  Future<ShopReturn?> create(
    String orderId, {
    required List<ReturnRequestItem> items,
    required ReturnReason reason,
    required RefundMethod refundMethod,
    String? comment,
    List<ReturnPhoto> photos = const [],
  }) =>
      apiCall(() async {
        final form = FormData();
        form.fields
          ..add(MapEntry('items', jsonEncode([for (final i in items) i.toJson()])))
          ..add(MapEntry('reason', reason.apiName))
          ..add(MapEntry('refundMethod', refundMethod.apiName));
        final c = comment?.trim();
        if (c != null && c.isNotEmpty) form.fields.add(MapEntry('comment', c));
        for (final p in photos.take(3)) {
          form.files.add(MapEntry('photos', MultipartFile.fromBytes(p.bytes, filename: p.fileName)));
        }
        final r = await _dio.post<dynamic>('/api/shop/orders/$orderId/returns', data: form);
        final data = r.data;
        return data is Map ? ShopReturn.fromJson(Map<String, dynamic>.from(data)) : null;
      });

  Future<List<ShopReturn>> list() => apiCall(() async {
        final r = await _dio.get<dynamic>('/api/shop/returns');
        return ShopReturn.listFromJson(r.data);
      });

  Future<ShopReturn> get(String id) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/shop/returns/$id');
        return ShopReturn.fromJson(r.data!);
      });

  Future<void> cancel(String id) => apiCall(() async {
        await _dio.post<dynamic>('/api/shop/returns/$id/cancel');
      });
}

final returnsRepositoryProvider = Provider<ReturnsRepository>((ref) => ReturnsRepository(ref.watch(dioProvider)));

/// "Qaytarishlarim" ro'yxati.
final returnsListProvider = FutureProvider.autoDispose<List<ShopReturn>>(
  (ref) => ref.watch(returnsRepositoryProvider).list(),
);

/// Bitta qaytarish (holat ekrani).
final returnDetailProvider = FutureProvider.autoDispose.family<ShopReturn, String>(
  (ref, id) => ref.watch(returnsRepositoryProvider).get(id),
);
