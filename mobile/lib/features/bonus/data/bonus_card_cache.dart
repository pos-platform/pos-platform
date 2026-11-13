import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../auth/auth_controller.dart';
import '../../catalog/store_selection.dart';
import 'bonus_card_repository.dart';

/// Oxirgi olingan bonus karta (QR) — "Bonus" tabi ochilganda QR darhol ko'rinishi uchun.
class CachedBonusCard {
  const CachedBonusCard({required this.branchId, required this.card, required this.fetchedAt, required this.validUntil});

  final String branchId;
  final BonusCard card;

  /// Qurilma soati bo'yicha (server soati farqidan himoya).
  final DateTime fetchedAt;
  final DateTime validUntil;

  /// Kod hali kassada o'qiladi (kamida ~15 s qolgan).
  bool get isFresh => validUntil.difference(DateTime.now()) > const Duration(seconds: 15);

  /// Server javobidan: amal qilish muddati qurilma soati bo'yicha hisoblanadi.
  factory CachedBonusCard.fromServer(String branchId, BonusCard card) {
    final now = DateTime.now();
    var lifetime = card.expiresAt.difference(now);
    // Qurilma soati serverdan farq qilsa — kelishuvdagi 90 s.
    if (lifetime < const Duration(seconds: 20) || lifetime > const Duration(minutes: 10)) {
      lifetime = const Duration(seconds: 90);
    }
    return CachedBonusCard(branchId: branchId, card: card, fetchedAt: now, validUntil: now.add(lifetime));
  }

  Map<String, dynamic> toJson() => {
        'branchId': branchId,
        'storeName': card.storeName,
        'points': card.points,
        'qrPayload': card.qrPayload,
        'expiresAt': card.expiresAt.toUtc().toIso8601String(),
        'fetchedAt': fetchedAt.toIso8601String(),
        'validUntil': validUntil.toIso8601String(),
      };

  factory CachedBonusCard.fromJson(Map<String, dynamic> j) => CachedBonusCard(
        branchId: j['branchId'] as String,
        card: BonusCard.fromJson(j),
        fetchedAt: DateTime.parse(j['fetchedAt'] as String),
        validUntil: DateTime.parse(j['validUntil'] as String),
      );
}

/// Bonus karta keshi: xotirada va `shared_preferences`da (oxirgi bitta karta).
/// Chiqishda tozalanadi. Muddati o'tgan karta ham saqlanadi — QR yuklanguncha do'kon nomi va balans ko'rinadi.
class BonusCardCache extends Notifier<CachedBonusCard?> {
  static const _key = 'bonus.lastCard';
  static CachedBonusCard? _initial;

  /// `main()` da runApp'dan oldin.
  static Future<void> preload() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_key);
      if (raw != null) _initial = CachedBonusCard.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {}
  }

  @override
  CachedBonusCard? build() {
    ref.listen(authControllerProvider.select((a) => a.status), (_, status) {
      if (status == AuthStatus.signedOut) clear();
    });
    return _initial;
  }

  /// Shu filial uchun keshdagi karta (bo'lsa).
  CachedBonusCard? forBranch(String? branchId) {
    final c = state;
    return c != null && c.branchId == branchId ? c : null;
  }

  Future<void> put(CachedBonusCard card) async {
    state = card;
    _initial = card;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_key, jsonEncode(card.toJson()));
    } catch (_) {}
  }

  Future<void> clear() async {
    state = null;
    _initial = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_key);
    } catch (_) {}
  }
}

final bonusCardCacheProvider = NotifierProvider<BonusCardCache, CachedBonusCard?>(BonusCardCache.new);

/// QR'ni oldindan yuklash (ilova ochilganda / kirgandan keyin; pastki menyuda kuzatiladi).
/// Filial: tanlangan do'kon → keshdagi filial → bonus do'konlarining birinchisi. Xato — jim (tab o'zi qayta so'raydi).
final bonusPrefetchProvider = FutureProvider<void>((ref) async {
  if (!ref.watch(authControllerProvider.select((a) => a.isSignedIn))) return;
  try {
    String? branchId = await ref.watch(storeSelectionProvider.future);
    final cache = ref.read(bonusCardCacheProvider.notifier);
    branchId ??= ref.read(bonusCardCacheProvider)?.branchId;
    branchId ??= (await loadBonusStores(ref)).where((s) => s.branchId != null).firstOrNull?.branchId;
    if (branchId == null) return;
    if (cache.forBranch(branchId)?.isFresh ?? false) return;
    final card = await ref.read(bonusCardRepositoryProvider).card(branchId);
    await cache.put(CachedBonusCard.fromServer(branchId, card));
  } catch (e) {
    debugPrint('Bonus karta oldindan yuklanmadi: $e');
  }
}, retry: (_, _) => null);
