import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../api/api_client.dart';
import '../config.dart';

/// `GET /api/app/config` javobi: majburiy/yumshoq yangilash va texnik ishlar holati.
class RemoteAppConfig {
  const RemoteAppConfig({
    required this.minSupportedBuild,
    required this.latestBuild,
    this.latestVersion,
    this.storeUrl,
    required this.forceUpdate,
    required this.maintenance,
    this.maintenanceMessage,
    this.privacyUrl,
    this.termsUrl,
  });

  final int minSupportedBuild;
  final int latestBuild;
  final String? latestVersion;
  final String? storeUrl;
  final bool forceUpdate;
  final bool maintenance;
  final String? maintenanceMessage;
  final String? privacyUrl;
  final String? termsUrl;

  factory RemoteAppConfig.fromJson(Map<String, dynamic> json) {
    int asInt(Object? v) => v is num ? v.toInt() : int.tryParse('$v') ?? 0;
    String? asText(Object? v) => v is String && v.trim().isNotEmpty ? v : null;
    return RemoteAppConfig(
      minSupportedBuild: asInt(json['minSupportedBuild']),
      latestBuild: asInt(json['latestBuild']),
      latestVersion: asText(json['latestVersion']),
      storeUrl: asText(json['storeUrl']),
      forceUpdate: json['forceUpdate'] == true,
      maintenance: json['maintenance'] == true,
      maintenanceMessage: asText(json['maintenanceMessage']),
      privacyUrl: asText(json['privacyUrl']),
      termsUrl: asText(json['termsUrl']),
    );
  }
}

/// Ilovaning joriy versiyasi va konfiguratsiyasi birga.
class AppConfigState {
  const AppConfigState({required this.config, required this.currentBuild});

  final RemoteAppConfig config;
  final int currentBuild;

  /// Server majburiy desa yoki joriy build minimal qo'llab-quvvatlanadigandan past bo'lsa.
  bool get mustUpdate => config.forceUpdate || (currentBuild > 0 && currentBuild < config.minSupportedBuild);

  /// Yangi versiya bor, lekin majburiy emas.
  bool get canUpdate => !mustUpdate && currentBuild > 0 && config.latestBuild > currentBuild;
}

class AppConfigRepository {
  AppConfigRepository(this._dio);

  final Dio _dio;

  Future<RemoteAppConfig> fetch({required String platform, required String version, required int build}) async {
    final res = await _dio.get<Map<String, dynamic>>(
      '/api/app/config',
      // Do'kon ilovasi: do'konning o'z versiya qiymatlari (bo'lsa) global qiymatlar o'rniga (kelishuv 8).
      queryParameters: {
        'platform': platform,
        'version': version,
        'build': build,
        if (AppConfig.isStoreMode) 'store': AppConfig.storeSlug,
      },
    );
    return RemoteAppConfig.fromJson(res.data ?? const {});
  }
}

final appConfigRepositoryProvider = Provider<AppConfigRepository>((ref) => AppConfigRepository(createBaseDio()));

/// Ilova konfiguratsiyasi. Har qanday xato (tarmoq, plagin, format) — `null`:
/// ilova HECH QACHON shu so'rov tufayli bloklanmaydi.
final appConfigProvider = FutureProvider<AppConfigState?>((ref) async {
  try {
    final info = await PackageInfo.fromPlatform();
    final build = int.tryParse(info.buildNumber) ?? 0;
    final platform = defaultTargetPlatform == TargetPlatform.iOS ? 'ios' : 'android';
    final config = await ref
        .read(appConfigRepositoryProvider)
        .fetch(platform: platform, version: info.version, build: build);
    return AppConfigState(config: config, currentBuild: build);
  } catch (e) {
    debugPrint('App config olinmadi: $e');
    return null;
  }
});
