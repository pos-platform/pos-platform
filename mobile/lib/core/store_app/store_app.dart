import 'dart:async';
import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../features/catalog/data/catalog_models.dart';
import '../../features/catalog/store_selection.dart';
import '../api/api_client.dart';
import '../api/api_exception.dart';
import '../config.dart';
import '../theme/app_colors.dart';

/// Do'kon ilovasi (white-label) ma'lumoti: `GET /api/shop/store-app/{slug}` (kelishuv 8).
class StoreApp {
  const StoreApp({
    required this.organizationId,
    required this.slug,
    required this.appName,
    this.brandColor,
    this.logoUrl,
    this.description,
    this.phone,
    required this.branches,
    required this.raw,
  });

  final String organizationId;
  final String slug;
  final String appName;

  /// `#RRGGBB` yoki null (standart yashil).
  final String? brandColor;
  final String? logoUrl;
  final String? description;
  final String? phone;

  /// Ilovada ko'rinadigan filiallar.
  final List<ShopStore> branches;

  /// Keshga yoziladigan asl javob.
  final Map<String, dynamic> raw;

  bool get singleBranch => branches.length == 1;

  factory StoreApp.fromJson(Map<String, dynamic> j) {
    String? text(Object? v) => v is String && v.trim().isNotEmpty ? v : null;
    return StoreApp(
      organizationId: '${j['organizationId']}',
      slug: j['slug'] as String? ?? '',
      appName: text(j['appName']) ?? '',
      brandColor: text(j['brandColor']),
      logoUrl: text(j['logoUrl']),
      description: text(j['description']),
      phone: text(j['phone']),
      branches: [
        for (final b in (j['branches'] as List? ?? const [])) ShopStore.fromJson(b as Map<String, dynamic>),
      ],
      raw: j,
    );
  }
}

/// Do'kon o'chirilgan/bloklangan (server 404) yoki ilovada ko'rinadigan filiali yo'q.
class StoreUnavailableException implements Exception {
  const StoreUnavailableException();
}

class StoreAppRepository {
  StoreAppRepository(this._dio);

  final Dio _dio;

  Future<StoreApp> fetch(String slug) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/shop/store-app/${Uri.encodeComponent(slug)}');
        return StoreApp.fromJson(r.data ?? const {});
      });
}

final storeAppRepositoryProvider = Provider<StoreAppRepository>((ref) => StoreAppRepository(ref.watch(dioProvider)));

/// Do'kon rejimi holati. Marketplace rejimida (STORE_SLUG bo'sh) — doim `null`.
///
/// Oxirgi muvaffaqiyatli javob `shared_preferences`da saqlanadi: internet bo'lmasa ham ilova keshdagi
/// ma'lumot bilan ochiladi, server javobi esa fonda yangilanadi. 404 — kesh o'chiriladi va
/// "Do'kon vaqtincha ishlamayapti" ekrani ko'rsatiladi.
class StoreAppController extends AsyncNotifier<StoreApp?> {
  static const _key = 'store.app';
  static StoreApp? _cached;

  /// `main()` da runApp'dan oldin: keshdagi do'kon va brend rangi birinchi kadrdanoq qo'llanadi.
  static Future<void> preload() async {
    if (!AppConfig.isStoreMode) return;
    var color = AppColors.parseHex(AppConfig.brandColor);
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_key);
      if (raw != null) {
        final app = StoreApp.fromJson(jsonDecode(raw) as Map<String, dynamic>);
        if (app.slug == AppConfig.storeSlug && app.branches.isNotEmpty) {
          _cached = app;
          color = AppColors.parseHex(app.brandColor) ?? color;
        }
      }
    } catch (_) {
      // Kesh buzilgan yoki xotira ishlamaydi — server javobi kutiladi.
    }
    AppColors.applyBrand(color);
  }

  @override
  Future<StoreApp?> build() async {
    if (!AppConfig.isStoreMode) return null;
    final cached = _cached;
    if (cached != null) {
      await _syncSelection(cached);
      unawaited(_refreshInBackground());
      return cached;
    }
    final app = await _fetch();
    await _syncSelection(app);
    return app;
  }

  Future<StoreApp> _fetch() async {
    try {
      final app = await ref.read(storeAppRepositoryProvider).fetch(AppConfig.storeSlug);
      if (app.branches.isEmpty) throw const StoreUnavailableException();
      _cached = app;
      AppColors.applyBrand(AppColors.parseHex(app.brandColor) ?? AppColors.parseHex(AppConfig.brandColor));
      unawaited(_save(app));
      return app;
    } on StoreUnavailableException {
      await _forget();
      rethrow;
    } on ApiException catch (e) {
      if (e.statusCode == 404) {
        await _forget();
        throw const StoreUnavailableException();
      }
      rethrow;
    }
  }

  Future<void> _refreshInBackground() async {
    try {
      final app = await _fetch();
      state = AsyncData(app);
      await _syncSelection(app);
    } on StoreUnavailableException catch (e, st) {
      state = AsyncError(e, st);
    } catch (e) {
      // Internet yo'q — keshdagi ma'lumot bilan davom etiladi.
      debugPrint('Do\'kon ma\'lumoti yangilanmadi: $e');
    }
  }

  /// Filial bitta bo'lsa — avtomatik tanlanadi; tanlangan filial shu do'konniki bo'lmasa — tozalanadi.
  Future<void> _syncSelection(StoreApp app) async {
    final selection = ref.read(storeSelectionProvider.notifier);
    String? current;
    try {
      current = await ref.read(storeSelectionProvider.future);
    } catch (_) {}
    if (app.singleBranch) {
      if (current != app.branches.first.id) await selection.select(app.branches.first.id);
    } else if (current != null && !app.branches.any((b) => b.id == current)) {
      await selection.clear();
    }
  }

  static Future<void> _save(StoreApp app) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_key, jsonEncode(app.raw));
    } catch (_) {}
  }

  static Future<void> _forget() async {
    _cached = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_key);
    } catch (_) {}
  }
}

final storeAppProvider = AsyncNotifierProvider<StoreAppController, StoreApp?>(
  StoreAppController.new,
  // Qayta urinish foydalanuvchi qo'lida ("Qayta urinish" tugmasi).
  retry: (_, _) => null,
);

/// Sarlavhalar uchun ilova nomi va logosi: do'kon rejimida do'konniki, marketplace'da — null
/// (u holda "PosCloud Market" va standart belgi ko'rsatiladi).
final storeBrandingProvider = Provider<({String? name, String? logoUrl})>((ref) {
  if (!AppConfig.isStoreMode) return (name: null, logoUrl: null);
  final app = ref.watch(storeAppProvider).value;
  final name = app?.appName.isNotEmpty == true ? app!.appName : (AppConfig.appName.isEmpty ? null : AppConfig.appName);
  return (name: name, logoUrl: app?.logoUrl);
});
