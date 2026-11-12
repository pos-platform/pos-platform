import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';

import 'data/courier_models.dart';

/// Tashqi navigator / xarita ilovalarini ochish.
class CourierNavigation {
  CourierNavigation._();

  static bool get _isAndroid => defaultTargetPlatform == TargetPlatform.android;

  static Future<bool> _open(Uri uri) async {
    try {
      return await launchUrl(uri, mode: LaunchMode.externalApplication);
    } catch (_) {
      return false;
    }
  }

  static String _addressQuery(CourierOrder o) => o.addressLine ?? '';

  /// Yandex Navigator; o'rnatilmagan bo'lsa — Yandex Xaritalar (veb).
  static Future<bool> yandex(CourierOrder o) async {
    if (o.hasCoordinates) {
      final app = Uri.parse('yandexnavi://build_route_on_map?lat_to=${o.latitude}&lon_to=${o.longitude}');
      if (await _open(app)) return true;
      return _open(Uri.parse('https://yandex.uz/maps/?rtext=~${o.latitude},${o.longitude}&rtt=auto'));
    }
    final q = Uri.encodeComponent(_addressQuery(o));
    if (await _open(Uri.parse('yandexnavi://map_search?text=$q'))) return true;
    return _open(Uri.parse('https://yandex.uz/maps/?text=$q'));
  }

  /// Google Maps: Android'da to'g'ridan-to'g'ri navigatsiya, boshqa joyda — yo'nalish URL.
  static Future<bool> google(CourierOrder o) async {
    final dest = o.hasCoordinates ? '${o.latitude},${o.longitude}' : _addressQuery(o);
    final enc = Uri.encodeComponent(dest);
    if (_isAndroid && await _open(Uri.parse('google.navigation:q=$enc'))) return true;
    return _open(Uri.parse('https://www.google.com/maps/dir/?api=1&destination=$enc&travelmode=driving'));
  }

  static Future<bool> call(String phone) =>
      _open(Uri(scheme: 'tel', path: phone.replaceAll(RegExp(r'[^\d+]'), '')));

  /// Masofa (metr). Koordinata yoki joylashuv yo'q bo'lsa — null.
  static double? distance(({double lat, double lng})? from, CourierOrder o) {
    if (from == null || !o.hasCoordinates) return null;
    return Geolocator.distanceBetween(from.lat, from.lng, o.latitude!, o.longitude!);
  }

  /// Yaqinlik bo'yicha tartib (ketma-ket eng yaqini — "nearest neighbour"). Koordinatasizlar oxirida.
  static List<CourierOrder> routeOrder(({double lat, double lng})? from, List<CourierOrder> orders) {
    final withCoords = orders.where((o) => o.hasCoordinates).toList();
    final without = orders.where((o) => !o.hasCoordinates).toList();
    if (from == null) return [...withCoords, ...without];
    final result = <CourierOrder>[];
    var cur = from;
    while (withCoords.isNotEmpty) {
      withCoords.sort((a, b) => distance(cur, a)!.compareTo(distance(cur, b)!));
      final next = withCoords.removeAt(0);
      result.add(next);
      cur = (lat: next.latitude!, lng: next.longitude!);
    }
    return [...result, ...without];
  }

  /// Barcha faol buyurtmalar bo'yicha Google Maps marshruti (oxirgisi — manzil, qolgani ≤ 9 oraliq nuqta).
  /// `origin` berilmasa Google joriy joylashuvdan boshlaydi.
  static Future<bool> routeAll(({double lat, double lng})? from, List<CourierOrder> orders) {
    final points = routeOrder(from, orders)
        .map((o) => o.hasCoordinates ? '${o.latitude},${o.longitude}' : _addressQuery(o))
        .where((p) => p.isNotEmpty)
        .take(10)
        .toList();
    if (points.isEmpty) return Future.value(false);
    final params = <String, String>{
      'api': '1',
      if (from != null) 'origin': '${from.lat},${from.lng}',
      'destination': points.last,
      if (points.length > 1) 'waypoints': points.sublist(0, points.length - 1).join('|'),
      'travelmode': 'driving',
    };
    return _open(Uri.https('www.google.com', '/maps/dir/', params));
  }
}
