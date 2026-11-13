/// Ilova sozlamalari. Build paytida beriladi:
/// `flutter run --dart-define=API_BASE_URL=https://poscloud.uz`
class AppConfig {
  AppConfig._();

  /// Backend manzili (oxirida `/` siz). Standart — Android emulyatoridan
  /// kompyuterdagi lokal API (10.0.2.2 = host mashina).
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5099',
  );

  /// Do'kon (white-label) rejimi: `--dart-define=STORE_SLUG=<slug>` (tool/store.ps1 beradi).
  /// Standart — Xo‘ja Market (ilova faqat shu do'kon uchun). Bo'sh qiymat (`STORE_SLUG=`) — marketplace rejimi.
  static const storeSlug = String.fromEnvironment('STORE_SLUG', defaultValue: 'xoja-market');

  static bool get isStoreMode => storeSlug.isNotEmpty;

  /// Build paytidagi brend rangi (`#RRGGBB`, store.json dan) — server javobi kelguncha (birinchi ochilishda) ishlatiladi.
  static const brandColor = String.fromEnvironment('BRAND_COLOR', defaultValue: '#D81F26');

  /// Build paytidagi ilova nomi (store.json dan) — server javobi kelguncha.
  static const appName = String.fromEnvironment('APP_NAME', defaultValue: 'Xo‘ja Market');

  /// Nisbiy rasm manzilini ("/uploads/..") to'liq URL'ga aylantiradi.
  static String? imageUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return '$apiBaseUrl${path.startsWith('/') ? '' : '/'}$path';
  }
}
