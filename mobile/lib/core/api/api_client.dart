import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/auth/data/auth_repository.dart';
import '../../features/auth/data/token_storage.dart';
import '../config.dart';
import '../locale/locale_controller.dart';
import 'api_exception.dart';

/// Interceptorsiz bazaviy Dio (auth so'rovlari uchun ham shu).
Dio createBaseDio() => Dio(BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 30),
      contentType: 'application/json',
    ))
      // Server xabarlari tanlangan tilda: til ilova ichida o'zgarishi mumkin — har so'rovda joriy qiymat.
      ..interceptors.add(InterceptorsWrapper(onRequest: (options, handler) {
        options.headers['Accept-Language'] = LocaleController.languageCode;
        handler.next(options);
      }));

/// Asosiy API klient: mijoz tokenini qo'shadi va muddati tugaganda yangilaydi.
///
/// Refresh token har safar almashadi (rotation) — shuning uchun yangilash BITTA vaqtda bitta bo'ladi:
/// parallel so'rovlar bitta `Future`ni kutadi. Aks holda ikkinchi so'rov eski refresh tokenni yuborib,
/// server uni "qayta ishlatish" deb barcha sessiyalarni yopadi.
class SessionManager {
  SessionManager(this._storage, this._repo, this._onSignedOut);

  final TokenStorage _storage;
  final AuthRepository _repo;
  final void Function() _onSignedOut;
  Future<String?>? _refreshing;

  /// Amaldagi access token (kerak bo'lsa oldindan yangilanadi). Null — sessiya yo'q.
  Future<String?> accessToken() async {
    final s = await _storage.read();
    if (s == null) return null;
    if (s.accessTokenFresh) return s.accessToken;
    return refresh();
  }

  /// Saqlangan token (yangilamasdan).
  Future<String?> currentAccessToken() async => (await _storage.read())?.accessToken;

  /// Bitta vaqtda bitta yangilash.
  Future<String?> refresh() => _refreshing ??= _doRefresh().whenComplete(() => _refreshing = null);

  Future<String?> _doRefresh() async {
    final s = await _storage.read();
    if (s == null) return null;
    try {
      final tokens = await _repo.refresh(s.refreshToken);
      await _storage.write(tokens);
      return tokens.accessToken;
    } on ApiException catch (e) {
      // Refresh rad etildi — sessiya tugagan. Tarmoq xatosida sessiya saqlanadi.
      if (e.statusCode == 401 || e.statusCode == 400) {
        await _storage.clear();
        _onSignedOut();
      }
      return null;
    }
  }
}

final sessionManagerProvider = Provider<SessionManager>((ref) => SessionManager(
      ref.watch(tokenStorageProvider),
      ref.watch(authRepositoryProvider),
      () => ref.read(authControllerProvider.notifier).onSessionExpired(),
    ));

final dioProvider = Provider<Dio>((ref) {
  final dio = createBaseDio();
  final session = ref.watch(sessionManagerProvider);

  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await session.accessToken();
      if (token != null) options.headers['Authorization'] = 'Bearer $token';
      handler.next(options);
    },
    onError: (error, handler) async {
      final req = error.requestOptions;
      final sentAuth = req.headers['Authorization'] as String?;
      if (error.response?.statusCode != 401 || sentAuth == null || req.extra['retried'] == true) {
        return handler.next(error);
      }
      // Boshqa so'rov allaqachon yangilagan bo'lsa — yangi token bilan qaytaramiz, qayta refresh qilmaymiz.
      final current = await session.currentAccessToken();
      final fresh = current != null && 'Bearer $current' != sentAuth ? current : await session.refresh();
      if (fresh == null) return handler.next(error);
      try {
        req.headers['Authorization'] = 'Bearer $fresh';
        req.extra['retried'] = true;
        handler.resolve(await dio.fetch<dynamic>(req));
      } on DioException catch (e) {
        handler.next(e);
      }
    },
  ));
  return dio;
});

/// Barcha API chaqiruvlari uchun qisqa yordamchi: xatoni [ApiException]ga aylantiradi.
Future<T> apiCall<T>(Future<T> Function() call) async {
  try {
    return await call();
  } catch (e) {
    throw ApiException.from(e);
  }
}
