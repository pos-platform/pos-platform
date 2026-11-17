import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../catalog/data/catalog_models.dart';
import '../../catalog/data/catalog_repository.dart';

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §1): retseptlar.

List<Map<String, dynamic>> _maps(Object? raw) => [
      for (final e in (raw as List?) ?? const [])
        if (e is Map) Map<String, dynamic>.from(e),
    ];

class RecipeBrief {
  const RecipeBrief({
    required this.id,
    required this.title,
    this.imageUrl,
    this.cookMinutes,
    this.servings,
    this.itemCount = 0,
  });

  final String id;
  final String title;
  final String? imageUrl;
  final int? cookMinutes;
  final int? servings;
  final int itemCount;

  factory RecipeBrief.fromJson(Map<String, dynamic> j) => RecipeBrief(
        id: j['id'] as String,
        title: j['title'] as String? ?? '',
        imageUrl: j['imageUrl'] as String?,
        cookMinutes: (j['cookMinutes'] as num?)?.toInt(),
        servings: (j['servings'] as num?)?.toInt(),
        itemCount: (j['itemCount'] as num?)?.toInt() ?? 0,
      );

  static List<RecipeBrief> listFrom(Object? raw) => [for (final m in _maps(raw)) RecipeBrief.fromJson(m)];
}

class RecipeItem {
  const RecipeItem({
    required this.productId,
    required this.name,
    required this.quantity,
    this.note,
    this.available = false,
    this.product,
  });

  final String productId;
  final String name;
  final double quantity;
  final String? note;
  final bool available;
  final ShopProduct? product;

  factory RecipeItem.fromJson(Map<String, dynamic> j) => RecipeItem(
        productId: j['productId'] as String,
        name: j['name'] as String? ?? '',
        quantity: (j['quantity'] as num?)?.toDouble() ?? 1,
        note: j['note'] as String?,
        available: j['available'] as bool? ?? false,
        product: j['product'] is Map ? ShopProduct.fromJson(Map<String, dynamic>.from(j['product'] as Map)) : null,
      );
}

class Recipe {
  const Recipe({
    required this.id,
    required this.title,
    this.description,
    this.imageUrl,
    this.cookMinutes,
    this.servings,
    this.steps = const [],
    this.items = const [],
  });

  final String id;
  final String title;
  final String? description;
  final String? imageUrl;
  final int? cookMinutes;
  final int? servings;
  final List<String> steps;
  final List<RecipeItem> items;

  factory Recipe.fromJson(Map<String, dynamic> j) => Recipe(
        id: j['id'] as String,
        title: j['title'] as String? ?? '',
        description: j['description'] as String?,
        imageUrl: j['imageUrl'] as String?,
        cookMinutes: (j['cookMinutes'] as num?)?.toInt(),
        servings: (j['servings'] as num?)?.toInt(),
        steps: [for (final s in (j['steps'] as List?) ?? const []) if (s is String && s.trim().isNotEmpty) s],
        items: [for (final m in _maps(j['items'])) RecipeItem.fromJson(m)],
      );
}

class RecipesRepository {
  RecipesRepository(this._dio);

  final Dio _dio;

  Future<List<RecipeBrief>> list(String branchId) => apiCall(() async {
        final r = await _dio.get<List<dynamic>>('/api/shop/stores/$branchId/recipes');
        return RecipeBrief.listFrom(r.data);
      });

  Future<Recipe> get(String branchId, String id) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/shop/stores/$branchId/recipes/$id');
        return Recipe.fromJson(r.data ?? const {});
      });
}

final recipesRepositoryProvider = Provider<RecipesRepository>((ref) => RecipesRepository(ref.watch(dioProvider)));

final recipesProvider = FutureProvider.autoDispose.family<List<RecipeBrief>, String>(
  (ref, branchId) => ref.watch(recipesRepositoryProvider).list(branchId),
  retry: noRetry,
);

final recipeProvider = FutureProvider.autoDispose.family<Recipe, ({String branchId, String id})>(
  (ref, a) => ref.watch(recipesRepositoryProvider).get(a.branchId, a.id),
  retry: noRetry,
);
