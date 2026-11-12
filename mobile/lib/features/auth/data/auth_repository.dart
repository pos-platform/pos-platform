import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/api_exception.dart';
import 'auth_models.dart';

/// `/api/customer-auth/*` — interceptorsiz alohida Dio bilan (refresh o'zini o'zi chaqirmasligi uchun).
class AuthRepository {
  AuthRepository(this._dio);

  final Dio _dio;

  Future<OtpSent> sendOtp(String phone, {bool preferSms = false}) => _post(
        '/api/customer-auth/send-otp',
        {'phone': phone, 'preferSms': preferSms},
        OtpSent.fromJson,
      );

  Future<AuthTokens> verify(String phone, String code) =>
      _post('/api/customer-auth/verify', {'phone': phone, 'code': code}, AuthTokens.fromJson);

  Future<AuthTokens> refresh(String refreshToken) =>
      _post('/api/customer-auth/refresh', {'refreshToken': refreshToken}, AuthTokens.fromJson);

  /// [deviceToken] — shu qurilmaning FCM tokeni: server uni o'chiradi, chiqqan akkauntga push kelmaydi.
  Future<void> logout(String refreshToken, {String? deviceToken}) async {
    try {
      await _dio.post<void>('/api/customer-auth/logout', data: {
        'refreshToken': refreshToken,
        'deviceToken': ?deviceToken,
      });
    } catch (_) {
      // Serverga yetmasa ham lokal sessiya baribir tozalanadi.
    }
  }

  Future<T> _post<T>(String path, Map<String, dynamic> body, T Function(Map<String, dynamic>) parse) async {
    try {
      final res = await _dio.post<Map<String, dynamic>>(path, data: body);
      return parse(res.data!);
    } catch (e) {
      throw ApiException.from(e);
    }
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) => AuthRepository(createBaseDio()));
