import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../catalog/data/catalog_models.dart';

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §2): oilaviy xarid ro'yxati (`/api/me/lists`).

List<Map<String, dynamic>> _maps(Object? raw) => [
      for (final e in (raw as List?) ?? const [])
        if (e is Map) Map<String, dynamic>.from(e),
    ];

class ListBrief {
  const ListBrief({required this.id, required this.name, required this.isOwner});

  final String id;
  final String name;
  final bool isOwner;

  factory ListBrief.fromJson(Map<String, dynamic> j) => ListBrief(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        isOwner: j['isOwner'] as bool? ?? false,
      );
}

class ListMember {
  const ListMember({required this.id, required this.phone, this.name, this.joined = false});

  final String id;
  final String phone;
  final String? name;
  final bool joined;

  factory ListMember.fromJson(Map<String, dynamic> j) => ListMember(
        id: j['id'] as String,
        phone: j['phone'] as String? ?? '',
        name: j['name'] as String?,
        joined: j['joined'] as bool? ?? false,
      );
}

class ListItem {
  const ListItem({
    required this.id,
    required this.title,
    this.productId,
    this.product,
    this.quantity,
    this.isChecked = false,
    this.checkedByName,
    this.addedByName = '',
  });

  final String id;
  final String title;
  final String? productId;
  final ShopProduct? product;
  final double? quantity;
  final bool isChecked;
  final String? checkedByName;
  final String addedByName;

  ListItem copyWith({bool? isChecked}) => ListItem(
        id: id,
        title: title,
        productId: productId,
        product: product,
        quantity: quantity,
        isChecked: isChecked ?? this.isChecked,
        checkedByName: checkedByName,
        addedByName: addedByName,
      );

  factory ListItem.fromJson(Map<String, dynamic> j) => ListItem(
        id: j['id'] as String,
        title: j['title'] as String? ?? '',
        productId: j['productId'] as String?,
        product: j['product'] is Map ? ShopProduct.fromJson(Map<String, dynamic>.from(j['product'] as Map)) : null,
        quantity: (j['quantity'] as num?)?.toDouble(),
        isChecked: j['isChecked'] as bool? ?? false,
        checkedByName: j['checkedByName'] as String?,
        addedByName: j['addedByName'] as String? ?? '',
      );
}

class ShoppingList {
  const ShoppingList({
    required this.id,
    required this.name,
    required this.isOwner,
    required this.ownerName,
    required this.ownerPhone,
    this.members = const [],
    this.items = const [],
  });

  final String id;
  final String name;
  final bool isOwner;
  final String ownerName;
  final String ownerPhone;
  final List<ListMember> members;
  final List<ListItem> items;

  ShoppingList copyWith({List<ListItem>? items}) => ShoppingList(
        id: id,
        name: name,
        isOwner: isOwner,
        ownerName: ownerName,
        ownerPhone: ownerPhone,
        members: members,
        items: items ?? this.items,
      );

  factory ShoppingList.fromJson(Map<String, dynamic> j) {
    final owner = j['owner'] is Map ? Map<String, dynamic>.from(j['owner'] as Map) : const <String, dynamic>{};
    return ShoppingList(
      id: j['id'] as String,
      name: j['name'] as String? ?? '',
      isOwner: j['isOwner'] as bool? ?? false,
      ownerName: owner['name'] as String? ?? '',
      ownerPhone: owner['phone'] as String? ?? '',
      members: [for (final m in _maps(j['members'])) ListMember.fromJson(m)],
      items: [for (final m in _maps(j['items'])) ListItem.fromJson(m)],
    );
  }
}

class ListsRepository {
  ListsRepository(this._dio);

  final Dio _dio;
  static const _base = '/api/me/lists';

  Future<List<ListBrief>> lists() => apiCall(() async {
        final r = await _dio.get<List<dynamic>>(_base);
        return [for (final m in _maps(r.data)) ListBrief.fromJson(m)];
      });

  Future<ShoppingList> create() => apiCall(() async {
        final r = await _dio.post<Map<String, dynamic>>(_base, data: const <String, dynamic>{});
        return ShoppingList.fromJson(r.data ?? const {});
      });

  Future<ShoppingList> get(String id, {String? branchId}) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('$_base/$id',
            queryParameters: {'branchId': ?branchId});
        return ShoppingList.fromJson(r.data ?? const {});
      });

  Future<void> addMember(String id, String phone) => apiCall(() async {
        await _dio.post<void>('$_base/$id/members', data: {'phone': phone});
      });

  Future<void> removeMember(String id, String memberId) => apiCall(() async {
        await _dio.delete<void>('$_base/$id/members/$memberId');
      });

  Future<void> addItem(String id, {required String title, String? productId, double? quantity}) => apiCall(() async {
        await _dio.post<void>('$_base/$id/items',
            data: {'title': title, 'productId': ?productId, 'quantity': ?quantity});
      });

  Future<void> setChecked(String id, String itemId, bool value) => apiCall(() async {
        await _dio.put<void>('$_base/$id/items/$itemId', data: {'isChecked': value});
      });

  Future<void> deleteItem(String id, String itemId) => apiCall(() async {
        await _dio.delete<void>('$_base/$id/items/$itemId');
      });

  Future<void> clearChecked(String id) => apiCall(() async {
        await _dio.post<void>('$_base/$id/clear-checked');
      });
}

final listsRepositoryProvider = Provider<ListsRepository>((ref) => ListsRepository(ref.watch(dioProvider)));
