import 'dart:async';
import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';
import 'package:package_info_plus/package_info_plus.dart';

import 'api/api_client.dart';

/// Kutilmagan xatolarni serverga yuborish (kelishuv 10.4): `POST /api/client-errors`, SuperAdmin "Xatoliklar" sahifasida ko'rinadi.
/// Sentry (agar yoqilgan bo'lsa) bilan birga ishlaydi — oldingi handler'lar saqlanadi va chaqiriladi.
///
/// Qoidalar: hech qachon istisno tashlamaydi; bir xil xabar daqiqada bir martadan ko'p yuborilmaydi;
/// sessiyada ko'pi bilan [_maxPerSession] ta; token, telefon, karta raqami kabi ma'lumotlar yuborilmaydi (tokensiz so'rov).
class ErrorReporter {
  ErrorReporter._();

  static const _maxPerSession = 50;
  static const _maxMessage = 1000;
  static const _maxDetails = 16000;
  static const _dedupeWindow = Duration(minutes: 1);

  static final _lastSent = <String, DateTime>{};
  static int _sentCount = 0;
  static String? _appVersion;
  static bool _installed = false;

  /// Testlarda yuborish o'rniga chaqiriladi.
  @visibleForTesting
  static Future<void> Function(Map<String, dynamic> payload)? sendOverride;

  /// `main()` da, `runApp` dan oldin bir marta.
  static void install() {
    if (_installed) return;
    _installed = true;
    final prevFlutter = FlutterError.onError;
    FlutterError.onError = (details) {
      report(details.exception, details.stack, context: details.context?.toString());
      if (prevFlutter != null) {
        prevFlutter(details);
      } else {
        FlutterError.presentError(details);
      }
    };
    final prevPlatform = PlatformDispatcher.instance.onError;
    PlatformDispatcher.instance.onError = (error, stack) {
      report(error, stack);
      return prevPlatform?.call(error, stack) ?? false;
    };
    unawaited(_loadVersion());
  }

  static Future<void> _loadVersion() async {
    try {
      final i = await PackageInfo.fromPlatform();
      _appVersion = '${i.version}+${i.buildNumber}';
    } catch (_) {}
  }

  /// Xatoni yuboradi (fon rejimida, natijani kutmasdan). Hech qachon istisno tashlamaydi.
  static void report(Object error, StackTrace? stack, {String? context}) {
    try {
      final payload = buildPayload(error, stack, context: context);
      if (payload == null) return;
      unawaited(_send(payload));
    } catch (_) {}
  }

  /// Yuboriladigan ma'lumot; dublikat yoki limit oshgan bo'lsa — null.
  @visibleForTesting
  static Map<String, dynamic>? buildPayload(Object error, StackTrace? stack, {String? context, DateTime? now}) {
    final message = truncate(sanitize('${error.runtimeType}: $error'), _maxMessage);
    final at = now ?? DateTime.now();
    final last = _lastSent[message];
    if (last != null && at.difference(last) < _dedupeWindow) return null;
    if (_sentCount >= _maxPerSession) return null;
    _lastSent[message] = at;
    _sentCount++;
    final details = [
      if (context != null && context.isNotEmpty) 'context: $context',
      if (stack != null) stack.toString(),
    ].join('\n');
    return {
      'source': 'Mobile',
      'level': 'Error',
      'message': message,
      if (details.isNotEmpty) 'details': truncate(sanitize(details), _maxDetails),
      'appVersion': ?_appVersion,
      'device': _device(),
    };
  }

  /// Dublikat holatini tozalash (testlar uchun).
  @visibleForTesting
  static void resetForTest() {
    _lastSent.clear();
    _sentCount = 0;
  }

  /// Token va shaxsiy raqamlarni yashiradi: `Bearer ...`, JWT, 9+ xonali raqamlar (telefon, karta).
  @visibleForTesting
  static String sanitize(String s) => s
      .replaceAll(RegExp(r'Bearer\s+[A-Za-z0-9\-._~+/]+=*', caseSensitive: false), 'Bearer ***')
      .replaceAll(RegExp(r'eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+'), '***')
      .replaceAll(RegExp(r'(access_token|refresh_token|token|password)=[^&\s]+', caseSensitive: false), r'$1=***')
      .replaceAll(RegExp(r'\+?\d[\d \-]{7,}\d'), '***');

  @visibleForTesting
  static String truncate(String s, int max) => s.length <= max ? s : '${s.substring(0, max - 1)}…';

  static String _device() {
    try {
      if (kIsWeb) return 'web';
      return '${Platform.operatingSystem} ${Platform.operatingSystemVersion}';
    } catch (_) {
      return 'unknown';
    }
  }

  static Future<void> _send(Map<String, dynamic> payload) async {
    try {
      final override = sendOverride;
      if (override != null) return await override(payload);
      // Tokensiz bazaviy klient: xato yuborishda refresh/qayta urinish zanjiri boshlanmaydi.
      await createBaseDio().post<void>('/api/client-errors', data: payload).timeout(const Duration(seconds: 10));
    } catch (_) {
      // Server/tarmoq xatosi — jim (xato haqidagi xato yuborilmaydi).
    }
  }
}
