import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';
import 'package:sentry_flutter/sentry_flutter.dart';

/// Xatolar monitoringi (Sentry). Faqat build paytida DSN berilsa yoqiladi:
/// `flutter build appbundle --dart-define=SENTRY_DSN=https://...@sentry.io/... --dart-define=APP_ENV=production`
///
/// DSN bo'sh bo'lsa (dev, testlar, CI) — oddiy `runApp`, Sentry umuman ishga tushmaydi.
class Monitoring {
  Monitoring._();

  static const sentryDsn = String.fromEnvironment('SENTRY_DSN');

  /// Muhit nomi: `--dart-define=APP_ENV=...`, berilmasa release'da 'production', aks holda 'development'.
  static const _appEnv = String.fromEnvironment('APP_ENV');
  static String get environment => _appEnv.isNotEmpty ? _appEnv : (kReleaseMode ? 'production' : 'development');

  static bool get enabled => sentryDsn.isNotEmpty;

  /// Ilovani ishga tushiradi; DSN bo'lsa Sentry bilan o'raydi.
  static Future<void> run(Widget Function() appBuilder) async {
    if (!enabled) {
      runApp(appBuilder());
      return;
    }
    await SentryFlutter.init(
      (options) {
        options.dsn = sentryDsn;
        options.environment = environment;
        // Shaxsiy ma'lumotlar (IP, foydalanuvchi, so'rov tanasi) yuborilmaydi.
        options.sendDefaultPii = false;
        options.attachScreenshot = false;
        options.attachViewHierarchy = false;
      },
      appRunner: () => runApp(appBuilder()),
    );
  }
}
