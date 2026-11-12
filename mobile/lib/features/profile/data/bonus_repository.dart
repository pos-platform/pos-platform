import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../auth/auth_controller.dart';

/// Mijozning do'kondagi bonus balansi: `GET /api/me/bonus?branchId=` → `{ points }` (1 ball = 1 so'm).
/// Kirmagan foydalanuvchi uchun — 0. Xatoda ham 0 (bonus bloki shunchaki ko'rsatilmaydi).
final bonusBalanceProvider = FutureProvider.autoDispose.family<double, String>((ref, branchId) async {
  if (!ref.watch(authControllerProvider.select((a) => a.isSignedIn))) return 0;
  final dio = ref.watch(dioProvider);
  try {
    return await apiCall(() async {
      final r = await dio.get<Map<String, dynamic>>('/api/me/bonus', queryParameters: {'branchId': branchId});
      final v = r.data?['points'];
      return v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
    });
  } catch (_) {
    return 0;
  }
});
