import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import 'catalog_repository.dart' show noRetry;

/// Taxminiy joylashuv (do'konlarni masofa bo'yicha tartiblash uchun).
class UserLocation {
  const UserLocation(this.lat, this.lng);

  final double lat;
  final double lng;
}

/// Qurilma joylashuvi. Ruxsat berilmasa, GPS o'chiq yoki xato bo'lsa — null (ro'yxat odatiy tartibda).
/// Natija sessiya davomida keshlanadi; yangilash — `ref.invalidate(userLocationProvider)`.
final userLocationProvider = FutureProvider<UserLocation?>((ref) async {
  try {
    if (!await Geolocator.isLocationServiceEnabled()) return null;
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) return null;

    Position? pos;
    try {
      pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.medium,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } catch (_) {
      // Signal yo'q / vaqt tugadi — oxirgi ma'lum joylashuv ham yetarli.
      pos = await Geolocator.getLastKnownPosition();
    }
    return pos == null ? null : UserLocation(pos.latitude, pos.longitude);
  } catch (_) {
    return null; // plagin yo'q (test) yoki platforma xatosi
  }
}, retry: noRetry);
