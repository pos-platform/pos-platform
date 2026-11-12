import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_models.dart';

/// Saqlangan sessiya (xotirada ham, qurilmada ham).
class StoredSession {
  const StoredSession({required this.accessToken, required this.accessTokenExpiresAt, required this.refreshToken, this.user});

  final String accessToken;
  final DateTime accessTokenExpiresAt;
  final String refreshToken;
  final ShopUser? user;

  /// 30 soniya zaxira bilan — muddati tugashiga oz qolgan tokenni oldindan yangilaymiz.
  bool get accessTokenFresh => DateTime.now().toUtc().isBefore(accessTokenExpiresAt.subtract(const Duration(seconds: 30)));
}

/// Tokenlarni xavfsiz saqlash (Android Keystore / iOS Keychain).
abstract class TokenStorage {
  Future<StoredSession?> read();
  Future<void> write(AuthTokens tokens);
  Future<void> updateUser(ShopUser user);
  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'auth_session_v1';
  final FlutterSecureStorage _storage;
  StoredSession? _cache;
  bool _loaded = false;

  @override
  Future<StoredSession?> read() async {
    if (_loaded) return _cache;
    try {
      final raw = await _storage.read(key: _key);
      if (raw != null) {
        final json = jsonDecode(raw) as Map<String, dynamic>;
        _cache = StoredSession(
          accessToken: json['accessToken'] as String,
          accessTokenExpiresAt: DateTime.parse(json['accessTokenExpiresAt'] as String).toUtc(),
          refreshToken: json['refreshToken'] as String,
          user: json['user'] == null ? null : ShopUser.fromJson(json['user'] as Map<String, dynamic>),
        );
      }
    } catch (_) {
      // Buzilgan yoki o'qib bo'lmaydigan yozuv — qayta kirish so'raladi.
      _cache = null;
    }
    _loaded = true;
    return _cache;
  }

  @override
  Future<void> write(AuthTokens tokens) async {
    _cache = StoredSession(
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: tokens.accessTokenExpiresAt,
      refreshToken: tokens.refreshToken,
      user: tokens.user,
    );
    _loaded = true;
    await _persist();
  }

  @override
  Future<void> updateUser(ShopUser user) async {
    final c = await read();
    if (c == null) return;
    _cache = StoredSession(
      accessToken: c.accessToken,
      accessTokenExpiresAt: c.accessTokenExpiresAt,
      refreshToken: c.refreshToken,
      user: user,
    );
    await _persist();
  }

  @override
  Future<void> clear() async {
    _cache = null;
    _loaded = true;
    await _storage.delete(key: _key);
  }

  Future<void> _persist() async {
    final c = _cache;
    if (c == null) return;
    await _storage.write(
      key: _key,
      value: jsonEncode({
        'accessToken': c.accessToken,
        'accessTokenExpiresAt': c.accessTokenExpiresAt.toIso8601String(),
        'refreshToken': c.refreshToken,
        'user': c.user?.toJson(),
      }),
    );
  }
}

/// Testlarda xotiradagi saqlash bilan almashtiriladi.
final tokenStorageProvider = Provider<TokenStorage>((ref) => SecureTokenStorage());
