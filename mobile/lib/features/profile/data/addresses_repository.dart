import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../auth/auth_controller.dart';

/// Mijozning saqlangan manzili (backend `ShopAddressDto`).
class ShopAddress {
  const ShopAddress({
    required this.id,
    required this.label,
    required this.addressLine,
    this.entrance,
    this.floor,
    this.apartment,
    this.intercom,
    this.landmark,
    this.comment,
    this.latitude,
    this.longitude,
    this.isDefault = false,
  });

  final String id;

  /// "Uy", "Ish" yoki mijoz yozgan nom.
  final String label;
  final String addressLine;
  final String? entrance;
  final String? floor;
  final String? apartment;
  final String? intercom;
  final String? landmark;
  final String? comment;
  final double? latitude;
  final double? longitude;
  final bool isDefault;

  factory ShopAddress.fromJson(Map<String, dynamic> json) => ShopAddress(
        id: json['id'] as String,
        label: json['label'] as String? ?? '',
        addressLine: json['addressLine'] as String? ?? '',
        entrance: json['entrance'] as String?,
        floor: json['floor'] as String?,
        apartment: json['apartment'] as String?,
        intercom: json['intercom'] as String?,
        landmark: json['landmark'] as String?,
        comment: json['comment'] as String?,
        latitude: (json['latitude'] as num?)?.toDouble(),
        longitude: (json['longitude'] as num?)?.toDouble(),
        isDefault: json['isDefault'] as bool? ?? false,
      );
}

/// `POST/PUT /api/me/addresses` so'rovi (`SaveShopAddressRequest`).
class SaveAddressRequest {
  const SaveAddressRequest({
    required this.label,
    required this.addressLine,
    this.entrance,
    this.floor,
    this.apartment,
    this.intercom,
    this.landmark,
    this.comment,
    this.latitude,
    this.longitude,
    this.isDefault = false,
  });

  final String label;
  final String addressLine;
  final String? entrance;
  final String? floor;
  final String? apartment;
  final String? intercom;
  final String? landmark;
  final String? comment;
  final double? latitude;
  final double? longitude;
  final bool isDefault;

  Map<String, dynamic> toJson() => {
        'label': label,
        'addressLine': addressLine,
        'entrance': entrance,
        'floor': floor,
        'apartment': apartment,
        'intercom': intercom,
        'landmark': landmark,
        'comment': comment,
        'latitude': latitude,
        'longitude': longitude,
        'isDefault': isDefault,
      };
}

class AddressesRepository {
  AddressesRepository(this._dio, this._ref);

  final Dio _dio;
  final Ref _ref;

  Future<List<ShopAddress>> list() => apiCall(() async {
        final res = await _dio.get<List<dynamic>>('/api/me/addresses');
        return (res.data ?? const [])
            .map((e) => ShopAddress.fromJson(e as Map<String, dynamic>))
            .toList();
      });

  Future<ShopAddress> create(SaveAddressRequest request) async {
    final saved = await apiCall(() async {
      final res = await _dio.post<Map<String, dynamic>>('/api/me/addresses', data: request.toJson());
      return ShopAddress.fromJson(res.data!);
    });
    _refresh();
    return saved;
  }

  Future<ShopAddress> update(String id, SaveAddressRequest request) async {
    final saved = await apiCall(() async {
      final res = await _dio.put<Map<String, dynamic>>('/api/me/addresses/$id', data: request.toJson());
      return ShopAddress.fromJson(res.data!);
    });
    _refresh();
    return saved;
  }

  Future<void> delete(String id) async {
    await apiCall(() => _dio.delete<void>('/api/me/addresses/$id'));
    _refresh();
  }

  // Asosiy manzil boshqa yozuvlarda ham o'zgarishi mumkin — ro'yxat butunlay qayta o'qiladi.
  void _refresh() => _ref.invalidate(addressesProvider);
}

final addressesRepositoryProvider =
    Provider<AddressesRepository>((ref) => AddressesRepository(ref.watch(dioProvider), ref));

/// Saqlangan manzillar (asosiysi birinchi). Mehmon uchun bo'sh ro'yxat.
final addressesProvider = FutureProvider<List<ShopAddress>>((ref) async {
  final userId = ref.watch(authControllerProvider.select((s) => s.isSignedIn ? s.user?.id : null));
  if (userId == null) return const [];
  return ref.watch(addressesRepositoryProvider).list();
});
