import 'dart:ui' show Locale, PlatformDispatcher;

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Ilova tili. `null` — qurilma tili (uz/ru, boshqasi bo'lsa o'zbekcha).
///
/// Tanlov `shared_preferences`da saqlanadi. Ilova ochilishida [preload] chaqiriladi —
/// shunda birinchi kadrdanoq to'g'ri til ko'rinadi (miltillash bo'lmaydi).
class LocaleController extends Notifier<Locale?> {
  static const _key = 'app.locale';
  static const supportedCodes = ['uz', 'ru'];

  static Locale? _initial;
  static Locale? _current;

  /// `main()` da runApp'dan oldin. Xotira ishlamasa (test, eski qurilma) — qurilma tili.
  static Future<void> preload() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final code = prefs.getString(_key);
      if (code != null && supportedCodes.contains(code)) _initial = _current = Locale(code);
    } catch (_) {}
  }

  /// Serverga yuboriladigan til kodi (`Accept-Language`, push matnlari): tanlangan yoki qurilma tili.
  static String get languageCode => _current?.languageCode ?? deviceLanguageCode;

  static String get deviceLanguageCode {
    final device = PlatformDispatcher.instance.locale.languageCode;
    return supportedCodes.contains(device) ? device : 'uz';
  }

  @override
  Locale? build() => _initial;

  Future<void> set(Locale? locale) async {
    _current = locale;
    state = locale;
    try {
      final prefs = await SharedPreferences.getInstance();
      if (locale == null) {
        await prefs.remove(_key);
      } else {
        await prefs.setString(_key, locale.languageCode);
      }
    } catch (_) {}
  }
}

final localeControllerProvider = NotifierProvider<LocaleController, Locale?>(LocaleController.new);
