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

  /// Nisbiy rasm manzilini ("/uploads/..") to'liq URL'ga aylantiradi.
  static String? imageUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return '$apiBaseUrl${path.startsWith('/') ? '' : '/'}$path';
  }
}
