import 'package:dio/dio.dart';

/// Server yoki tarmoq xatosi — foydalanuvchiga ko'rsatiladigan matn bilan.
class ApiException implements Exception {
  ApiException(this.message, {this.statusCode, this.retryAfterSeconds});

  final String message;
  final int? statusCode;

  /// 429 da server `Retry-After` qaytaradi (soniya).
  final int? retryAfterSeconds;

  bool get isUnauthorized => statusCode == 401;
  bool get isNetwork => statusCode == null;

  /// Dio xatosini tushunarli xabarga aylantiradi. Server `{ "error": "..." }` qaytaradi.
  factory ApiException.from(Object error) {
    if (error is ApiException) return error;
    if (error is DioException) {
      final response = error.response;
      if (response == null) {
        return ApiException(
          error.type == DioExceptionType.connectionTimeout || error.type == DioExceptionType.receiveTimeout
              ? 'Server javob bermadi. Internetni tekshirib, qayta urinib ko‘ring.'
              : 'Internet bilan aloqa yo‘q.',
        );
      }
      final data = response.data;
      String? message;
      if (data is Map && data['error'] is String) message = data['error'] as String;
      final retry = int.tryParse(response.headers.value('retry-after') ?? '');
      return ApiException(
        message ?? _defaultMessage(response.statusCode),
        statusCode: response.statusCode,
        retryAfterSeconds: retry,
      );
    }
    return ApiException('Kutilmagan xato. Qayta urinib ko‘ring.');
  }

  static String _defaultMessage(int? code) => switch (code) {
        401 => 'Sessiya tugagan. Qayta kiring.',
        403 => 'Ruxsat yo‘q.',
        404 => 'Topilmadi.',
        429 => 'Juda ko‘p urinish. Birozdan keyin qayta urinib ko‘ring.',
        _ => 'Server xatosi. Birozdan keyin qayta urinib ko‘ring.',
      };

  @override
  String toString() => message;
}
