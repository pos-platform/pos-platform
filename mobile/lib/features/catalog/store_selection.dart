import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'data/catalog_models.dart';
import 'data/catalog_repository.dart';

/// Tanlangan do'kon (filial ID). Qurilmada saqlanadi — ilova qayta ochilganda shu do'kon ko'rinadi.
class StoreSelection extends AsyncNotifier<String?> {
  static const _key = 'shop.branchId';
  static const _storage = FlutterSecureStorage();

  @override
  Future<String?> build() async {
    try {
      return await _storage.read(key: _key);
    } catch (_) {
      return null; // xotira ishlamasa (test, eski qurilma) — do'kon qayta tanlanadi
    }
  }

  Future<void> select(String branchId) async {
    state = AsyncData(branchId);
    try {
      await _storage.write(key: _key, value: branchId);
    } catch (_) {}
  }

  Future<void> clear() async {
    state = const AsyncData(null);
    try {
      await _storage.delete(key: _key);
    } catch (_) {}
  }
}

final storeSelectionProvider = AsyncNotifierProvider<StoreSelection, String?>(StoreSelection.new, retry: noRetry);

/// Tanlangan do'kon ma'lumoti. Null — do'kon tanlanmagan.
final selectedStoreProvider = FutureProvider<ShopStore?>((ref) async {
  final id = await ref.watch(storeSelectionProvider.future);
  if (id == null) return null;
  return ref.watch(storeProvider(id).future);
}, retry: noRetry);
