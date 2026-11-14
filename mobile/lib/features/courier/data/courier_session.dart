import 'dart:convert';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/api_exception.dart';
import '../../../core/push/push_service.dart';

/// Kuryer sessiyasi — xodim JWT (`POST /api/auth/login`). Mijoz sessiyasidan butunlay alohida:
/// boshqa kalitda saqlanadi, boshqa Dio bilan yuboriladi. Refresh token yo'q — muddati tugasa qayta kirish so'raladi.
class CourierSession {
  const CourierSession({
    required this.token,
    required this.expiresAt,
    required this.userId,
    required this.username,
    this.fullName,
    this.organizationName,
    this.branchName,
  });

  final String token;
  final DateTime expiresAt;
  final String userId;
  final String username;
  final String? fullName;
  final String? organizationName;
  final String? branchName;

  String get displayName => (fullName?.trim().isNotEmpty ?? false) ? fullName!.trim() : username;

  /// 30 soniya zaxira bilan.
  bool get isExpired => !DateTime.now().toUtc().isBefore(expiresAt.subtract(const Duration(seconds: 30)));

  factory CourierSession.fromJson(Map<String, dynamic> json) => CourierSession(
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String).toUtc(),
        userId: json['userId']?.toString() ?? '',
        username: json['username'] as String? ?? '',
        fullName: json['fullName'] as String?,
        organizationName: json['organizationName'] as String?,
        branchName: json['branchName'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'token': token,
        'expiresAt': expiresAt.toIso8601String(),
        'userId': userId,
        'username': username,
        'fullName': fullName,
        'organizationName': organizationName,
        'branchName': branchName,
      };
}

/// Kuryer tokeni va qurilma ID'sini saqlash. Testlarda xotiradagi variant bilan almashtiriladi.
abstract class CourierSessionStorage {
  Future<CourierSession?> read();
  Future<void> write(CourierSession session);
  Future<void> clear();

  /// Bir marta yaratiladigan va saqlanadigan qurilma ID'si (UUID v4).
  Future<String> deviceId();
}

class SecureCourierSessionStorage implements CourierSessionStorage {
  SecureCourierSessionStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'courier_session_v1';
  static const _deviceKey = 'courier_device_id_v1';
  final FlutterSecureStorage _storage;

  @override
  Future<CourierSession?> read() async {
    try {
      // Plagin javob bermasa (test muhiti) ilova splash'da qotib qolmasin.
      final raw = await _storage.read(key: _key).timeout(const Duration(seconds: 3));
      if (raw == null) return null;
      return CourierSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      // Plagin yo'q (test) yoki buzilgan yozuv — sessiya yo'q deb hisoblaymiz.
      return null;
    }
  }

  @override
  Future<void> write(CourierSession session) => _storage.write(key: _key, value: jsonEncode(session.toJson()));

  @override
  Future<void> clear() async {
    try {
      await _storage.delete(key: _key);
    } catch (_) {}
  }

  @override
  Future<String> deviceId() async {
    try {
      final existing = await _storage.read(key: _deviceKey);
      if (existing != null && existing.isNotEmpty) return existing;
      final id = newUuidV4();
      await _storage.write(key: _deviceKey, value: id);
      return id;
    } catch (_) {
      return newUuidV4();
    }
  }
}

/// Tasodifiy UUID v4 (qo'shimcha paketsiz).
String newUuidV4() {
  final r = Random.secure();
  final b = List<int>.generate(16, (_) => r.nextInt(256));
  b[6] = (b[6] & 0x0f) | 0x40;
  b[8] = (b[8] & 0x3f) | 0x80;
  final h = b.map((v) => v.toRadixString(16).padLeft(2, '0')).join();
  return '${h.substring(0, 8)}-${h.substring(8, 12)}-${h.substring(12, 16)}-${h.substring(16, 20)}-${h.substring(20)}';
}

final courierSessionStorageProvider = Provider<CourierSessionStorage>((ref) => SecureCourierSessionStorage());

enum CourierAuthStatus { unknown, none, signedIn }

class CourierAuthState {
  const CourierAuthState(this.status, {this.session, this.expired = false});

  final CourierAuthStatus status;
  final CourierSession? session;

  /// Sessiya server tomonidan rad etildi (401 / muddati tugadi) — kuryer login ekraniga qaytariladi.
  /// `false` bo'lsa (o'zi chiqdi) — mijoz login ekraniga.
  final bool expired;

  bool get isSignedIn => status == CourierAuthStatus.signedIn;
}

/// Kirgan akkaunt kuryer emas.
class NotCourierException implements Exception {
  const NotCourierException();
}

class CourierAuthController extends Notifier<CourierAuthState> {
  @override
  CourierAuthState build() {
    _restore();
    return const CourierAuthState(CourierAuthStatus.unknown);
  }

  CourierSessionStorage get _storage => ref.read(courierSessionStorageProvider);

  Future<void> _restore() async {
    final s = await _storage.read();
    if (!ref.mounted) return;
    if (s == null) {
      state = const CourierAuthState(CourierAuthStatus.none);
    } else if (s.isExpired) {
      await _storage.clear();
      if (ref.mounted) state = const CourierAuthState(CourierAuthStatus.none, expired: true);
    } else {
      state = CourierAuthState(CourierAuthStatus.signedIn, session: s);
    }
  }

  Future<void> login(String username, String password) async {
    final deviceId = await _storage.deviceId();
    final Map<String, dynamic> data;
    try {
      final res = await createBaseDio().post<Map<String, dynamic>>('/api/auth/login', data: {
        'username': username.trim(),
        'password': password,
        'deviceId': deviceId,
        'deviceName': _deviceName(),
      });
      data = res.data!;
    } catch (e) {
      throw ApiException.from(e);
    }
    final role = data['role'];
    if (!(role == 'Courier' || role == 5)) throw const NotCourierException();
    final session = CourierSession.fromJson(data);
    await _storage.write(session);
    state = CourierAuthState(CourierAuthStatus.signedIn, session: session);
  }

  /// Kuryer o'zi chiqdi — mijoz login ekraniga qaytadi.
  Future<void> logout() async {
    // Kelishuv 10.6: push tokeni serverdan o'chiriladi (xato chiqishni to'xtatmaydi).
    final token = state.session?.token;
    if (token != null) await ref.read(pushServiceProvider).unregisterCourier(token);
    await _storage.clear();
    state = const CourierAuthState(CourierAuthStatus.none);
  }

  /// Token rad etildi yoki muddati tugadi — kuryer login ekraniga.
  Future<void> onSessionExpired() async {
    if (!state.isSignedIn) return;
    await _storage.clear();
    if (ref.mounted) state = const CourierAuthState(CourierAuthStatus.none, expired: true);
  }

  static String _deviceName() => switch (defaultTargetPlatform) {
        TargetPlatform.android => 'Android',
        TargetPlatform.iOS => 'iOS',
        _ => 'Mobile',
      };
}

final courierAuthProvider = NotifierProvider<CourierAuthController, CourierAuthState>(CourierAuthController.new);

/// Kuryer API klienti: bazaviy Dio + kuryer Bearer tokeni. 401 da sessiya tozalanadi.
final courierDioProvider = Provider<Dio>((ref) {
  // Boshqa kuryer kirsa — klient va unga bog'liq keshlar qayta quriladi.
  ref.watch(courierAuthProvider.select((s) => s.session?.token));
  final dio = createBaseDio();
  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) {
      final s = ref.read(courierAuthProvider).session;
      if (s == null || s.isExpired) {
        ref.read(courierAuthProvider.notifier).onSessionExpired();
        return handler.reject(DioException(
          requestOptions: options,
          response: Response(requestOptions: options, statusCode: 401),
          type: DioExceptionType.badResponse,
        ));
      }
      options.headers['Authorization'] = 'Bearer ${s.token}';
      handler.next(options);
    },
    onError: (error, handler) {
      if (error.response?.statusCode == 401) ref.read(courierAuthProvider.notifier).onSessionExpired();
      handler.next(error);
    },
  ));
  return dio;
});
