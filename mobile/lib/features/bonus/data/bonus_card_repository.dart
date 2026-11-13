import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/config.dart';
import '../../../core/store_app/store_app.dart';
import '../../auth/auth_controller.dart';

double _num(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;

/// Dinamik QR bonus karta: `GET /api/me/bonus-card?branchId=` (kelishuv 7, §3).
class BonusCard {
  const BonusCard({required this.storeName, required this.points, required this.qrPayload, required this.expiresAt});

  final String storeName;
  final double points;

  /// `PCB1.<payload>.<imzo>` — kassada skanerlanadi; ~90 s amal qiladi.
  final String qrPayload;
  final DateTime expiresAt;

  factory BonusCard.fromJson(Map<String, dynamic> j) => BonusCard(
        storeName: j['storeName'] as String? ?? '',
        points: _num(j['points']),
        qrPayload: j['qrPayload'] as String? ?? '',
        // Server UTC beradi; mahalliy vaqtga o'tkaziladi (faqat farq hisoblanadi).
        expiresAt: DateTime.tryParse(j['expiresAt'] as String? ?? '')?.toLocal() ??
            DateTime.now().add(const Duration(seconds: 90)),
      );
}

/// Mijoz bog'langan do'kon va undagi balans: `GET /api/me/bonus/stores`.
class BonusStore {
  const BonusStore({required this.organizationId, this.branchId, required this.storeName, this.logoUrl, required this.points});

  final String organizationId;

  /// Birinchi faol online filial; null — QR olish uchun filial yo'q.
  final String? branchId;
  final String storeName;
  final String? logoUrl;
  final double points;

  factory BonusStore.fromJson(Map<String, dynamic> j) => BonusStore(
        organizationId: j['organizationId'] as String,
        branchId: j['branchId'] as String?,
        storeName: j['storeName'] as String? ?? '',
        logoUrl: j['logoUrl'] as String?,
        points: _num(j['points']),
      );
}

enum BonusTxType { earn, redeem, expire, adjust, signup, refund, unknown }

enum BonusTxSource { store, online, unknown }

/// Ball tarixi elementi: `GET /api/me/bonus/history`.
class BonusTransaction {
  const BonusTransaction({
    required this.id,
    required this.createdAt,
    required this.type,
    required this.source,
    required this.points,
    this.amount,
    this.comment,
    this.receiptNumber,
  });

  final String id;
  final DateTime createdAt;
  final BonusTxType type;
  final BonusTxSource source;

  /// Musbat — yozildi, manfiy — sarflandi/olib qo'yildi.
  final double points;
  final double? amount;
  final String? comment;
  final String? receiptNumber;

  static BonusTxType _type(String? v) => switch (v?.toLowerCase()) {
        'earn' => BonusTxType.earn,
        'redeem' => BonusTxType.redeem,
        'expire' => BonusTxType.expire,
        'adjust' => BonusTxType.adjust,
        'signup' => BonusTxType.signup,
        'refund' => BonusTxType.refund,
        _ => BonusTxType.unknown,
      };

  static BonusTxSource _source(String? v) => switch (v?.toLowerCase()) {
        'store' => BonusTxSource.store,
        'online' => BonusTxSource.online,
        _ => BonusTxSource.unknown,
      };

  factory BonusTransaction.fromJson(Map<String, dynamic> j) => BonusTransaction(
        id: '${j['id']}',
        createdAt: DateTime.tryParse(j['createdAt'] as String? ?? '')?.toLocal() ?? DateTime.now(),
        type: _type(j['type'] as String?),
        source: _source(j['source'] as String?),
        points: _num(j['points']),
        amount: j['amount'] == null ? null : _num(j['amount']),
        comment: j['comment'] as String?,
        receiptNumber: j['receiptNumber']?.toString(),
      );
}

class BonusHistoryPage {
  const BonusHistoryPage({required this.items, required this.totalCount, required this.page});

  final List<BonusTransaction> items;
  final int totalCount;
  final int page;

  bool get hasMore => items.length < totalCount;

  factory BonusHistoryPage.fromJson(Map<String, dynamic> j) => BonusHistoryPage(
        items: [
          for (final e in (j['items'] as List? ?? const [])) BonusTransaction.fromJson(e as Map<String, dynamic>),
        ],
        totalCount: (j['totalCount'] as num?)?.toInt() ?? 0,
        page: (j['page'] as num?)?.toInt() ?? 1,
      );
}

class BonusCardRepository {
  BonusCardRepository(this._dio);

  final Dio _dio;

  /// QR kod va balans. Do'konda mijoz yozuvi bo'lmasa server uni shu so'rovda yaratadi.
  Future<BonusCard> card(String branchId) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/me/bonus-card', queryParameters: {'branchId': branchId});
        return BonusCard.fromJson(r.data!);
      });

  Future<List<BonusStore>> stores() => apiCall(() async {
        final r = await _dio.get<List<dynamic>>('/api/me/bonus/stores');
        return [for (final e in r.data ?? const []) BonusStore.fromJson(e as Map<String, dynamic>)];
      });

  Future<BonusHistoryPage> history(String organizationId, {int page = 1, int pageSize = 20}) => apiCall(() async {
        final r = await _dio.get<Map<String, dynamic>>('/api/me/bonus/history',
            queryParameters: {'organizationId': organizationId, 'page': page, 'pageSize': pageSize});
        return BonusHistoryPage.fromJson(r.data ?? const {});
      });
}

final bonusCardRepositoryProvider = Provider<BonusCardRepository>((ref) => BonusCardRepository(ref.watch(dioProvider)));

/// Bonus do'konlari (faqat kirgan foydalanuvchi; mehmon — bo'sh ro'yxat).
final bonusStoresProvider = FutureProvider.autoDispose<List<BonusStore>>((ref) async {
  if (!ref.watch(authControllerProvider.select((a) => a.isSignedIn))) return const [];
  return loadBonusStores(ref);
});

/// Bonus do'konlari; do'kon rejimida — faqat shu do'kon (boshqa do'konlar chiplari ko'rsatilmaydi).
Future<List<BonusStore>> loadBonusStores(Ref ref) async {
  final stores = await ref.read(bonusCardRepositoryProvider).stores();
  if (!AppConfig.isStoreMode) return stores;
  final org = (await ref.read(storeAppProvider.future))?.organizationId;
  return [for (final s in stores) if (s.organizationId == org) s];
}

/// Tashkilot bo'yicha ball tarixi (sahifalab).
class BonusHistoryNotifier extends AsyncNotifier<BonusHistoryPage> {
  BonusHistoryNotifier(this.organizationId);

  final String organizationId;
  bool _loadingMore = false;

  static const _pageSize = 20;

  @override
  Future<BonusHistoryPage> build() => ref.read(bonusCardRepositoryProvider).history(organizationId, pageSize: _pageSize);

  /// Keyingi sahifa (ro'yxat oxiriga yetganda).
  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || _loadingMore) return;
    _loadingMore = true;
    try {
      final next = await ref
          .read(bonusCardRepositoryProvider)
          .history(organizationId, page: current.page + 1, pageSize: _pageSize);
      final seen = current.items.map((e) => e.id).toSet();
      state = AsyncData(BonusHistoryPage(
        items: [...current.items, ...next.items.where((e) => !seen.contains(e.id))],
        totalCount: next.totalCount,
        page: next.page,
      ));
    } catch (_) {
      // Keyingi sahifa yuklanmadi — mavjud ro'yxat qoladi.
    } finally {
      _loadingMore = false;
    }
  }
}

final bonusHistoryProvider = AsyncNotifierProvider.autoDispose
    .family<BonusHistoryNotifier, BonusHistoryPage, String>(BonusHistoryNotifier.new);
