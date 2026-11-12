import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Nominatim (OpenStreetMap) natijasi: koordinata va qisqa manzil matni.
class GeoPlace {
  const GeoPlace({required this.lat, required this.lng, required this.label, this.fullName});

  final double lat;
  final double lng;

  /// Qisqa matn: "Amir Temur ko'chasi 15, Yunusobod, Toshkent".
  final String label;

  /// Nominatim `display_name` (to'liq, uzun).
  final String? fullName;
}

/// Nominatim geokodlash. Foydalanish qoidasi (https://operations.osmfoundation.org/policies/nominatim/):
/// aniq User-Agent, soniyasiga ko'pi bilan 1 so'rov, ommaviy so'rov va avtoto'ldirish (har harfda qidiruv) yo'q.
/// Shuning uchun so'rovlar orasida kamida 1 soniya kutiladi, qidiruv faqat "qidirish" bosilganda.
class GeocodingRepository {
  GeocodingRepository([Dio? dio])
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: 'https://nominatim.openstreetmap.org',
              connectTimeout: const Duration(seconds: 10),
              receiveTimeout: const Duration(seconds: 15),
              headers: {'User-Agent': userAgent},
            ));

  static const userAgent = 'PosCloudMarket/1.0 (uz.poscloud.poscloud_market)';
  static const _minInterval = Duration(milliseconds: 1100);

  final Dio _dio;
  DateTime? _last;

  /// So'rovlar orasida kamida [_minInterval]: navbatdagi vaqt sinxron band qilinadi (parallel chaqiruvlar ham ketma-ket).
  Future<void> _throttle() async {
    final now = DateTime.now();
    final last = _last;
    final slot = last == null || !last.add(_minInterval).isAfter(now) ? now : last.add(_minInterval);
    _last = slot;
    final wait = slot.difference(now);
    if (wait > Duration.zero) await Future<void>.delayed(wait);
  }

  /// Koordinatadan manzil. Topilmasa yoki xato bo'lsa — null.
  Future<GeoPlace?> reverse(double lat, double lng, {String language = 'uz', CancelToken? cancelToken}) async {
    await _throttle();
    if (cancelToken?.isCancelled ?? false) return null;
    try {
      final r = await _dio.get<Map<String, dynamic>>('/reverse',
          queryParameters: {
            'format': 'jsonv2',
            'lat': lat.toStringAsFixed(6),
            'lon': lng.toStringAsFixed(6),
            'accept-language': language,
            'zoom': 18,
            'addressdetails': 1,
          },
          cancelToken: cancelToken);
      final j = r.data;
      if (j == null || j['error'] != null) return null;
      return _place(j, lat, lng);
    } catch (_) {
      return null;
    }
  }

  /// Manzil qidirish (O'zbekiston bo'yicha, 5 tagacha). Xatoda — bo'sh ro'yxat.
  Future<List<GeoPlace>> search(String query, {String language = 'uz'}) async {
    final q = query.trim();
    if (q.length < 3) return const [];
    await _throttle();
    try {
      final r = await _dio.get<List<dynamic>>('/search', queryParameters: {
        'format': 'jsonv2',
        'q': q,
        'countrycodes': 'uz',
        'limit': 5,
        'addressdetails': 1,
        'accept-language': language,
      });
      return [
        for (final e in r.data ?? const [])
          if (e is Map<String, dynamic>) ?_place(e, null, null),
      ];
    } catch (_) {
      return const [];
    }
  }

  static GeoPlace? _place(Map<String, dynamic> j, double? lat, double? lng) {
    final la = lat ?? double.tryParse('${j['lat']}');
    final lo = lng ?? double.tryParse('${j['lon']}');
    if (la == null || lo == null) return null;
    final full = j['display_name'] as String?;
    final label = shortLabel(j['address'] is Map ? Map<String, dynamic>.from(j['address'] as Map) : const {}) ??
        full ??
        '';
    if (label.isEmpty) return null;
    return GeoPlace(lat: la, lng: lo, label: label, fullName: full);
  }

  /// Nominatim `address` obyektidan qisqa matn: ko'cha + uy, mahalla/tuman, shahar.
  static String? shortLabel(Map<String, dynamic> a) {
    String? f(List<String> keys) {
      for (final k in keys) {
        final v = a[k];
        if (v is String && v.trim().isNotEmpty) return v.trim();
      }
      return null;
    }

    final road = f(['road', 'pedestrian', 'footway', 'residential', 'street']);
    final house = f(['house_number']);
    final area = f(['neighbourhood', 'quarter', 'suburb', 'city_district', 'district']);
    final city = f(['city', 'town', 'village', 'hamlet', 'county', 'state']);
    final first = road == null ? f(['amenity', 'building', 'shop']) : (house == null ? road : '$road $house');
    final parts = <String>[?first, ?area, ?city];
    final unique = <String>[];
    for (final p in parts) {
      if (!unique.contains(p)) unique.add(p);
    }
    return unique.isEmpty ? null : unique.join(', ');
  }
}

final geocodingRepositoryProvider = Provider<GeocodingRepository>((ref) => GeocodingRepository());
