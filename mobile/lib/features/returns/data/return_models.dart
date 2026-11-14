/// Online buyurtmani qaytarish modellari (kelishuv 10.5, backend: `OnlineReturn`).
/// Enumlar serverdan matn (yoki raqam) ko'rinishida keladi — noma'lum qiymatda xavfsiz standart.
library;

T _parseEnum<T extends Enum>(List<T> values, Object? v, String Function(T) name, T fallback) {
  if (v is int) return v >= 0 && v < values.length ? values[v] : fallback;
  if (v is String) {
    final s = v.toLowerCase();
    for (final e in values) {
      if (name(e).toLowerCase() == s) return e;
    }
  }
  return fallback;
}

double _num(Object? v) => v is num ? v.toDouble() : double.tryParse('${v ?? ''}') ?? 0;
String? _str(Object? v) {
  final s = v?.toString().trim();
  return s == null || s.isEmpty ? null : s;
}

/// Server UTC vaqti (zonasiz bo'lsa ham UTC) → mahalliy.
DateTime? _date(Object? v) {
  if (v is! String || v.isEmpty) return null;
  final hasZone = v.endsWith('Z') || RegExp(r'[+-]\d\d:?\d\d$').hasMatch(v);
  return DateTime.tryParse(hasZone ? v : '${v}Z')?.toLocal();
}

enum ReturnStatus {
  requested('Requested'),
  approved('Approved'),
  pickupAssigned('PickupAssigned'),
  pickedUp('PickedUp'),
  received('Received'),
  refunded('Refunded'),
  rejected('Rejected'),
  cancelled('Cancelled');

  const ReturnStatus(this.apiName);
  final String apiName;

  static ReturnStatus parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, requested);

  bool get isFailed => this == rejected || this == cancelled;
  bool get isFinal => isFailed || this == refunded;
}

enum ReturnReason {
  damaged('Damaged'),
  wrongItem('WrongItem'),
  quality('Quality'),
  expired('Expired'),
  missing('Missing'),
  other('Other');

  const ReturnReason(this.apiName);
  final String apiName;

  static ReturnReason parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, other);
}

enum RefundMethod {
  cash('Cash'),
  transfer('Transfer'),
  bonus('Bonus');

  const RefundMethod(this.apiName);
  final String apiName;

  static RefundMethod parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, cash);
}

enum ReturnMethod {
  courierPickup('CourierPickup'),
  bringToStore('BringToStore');

  const ReturnMethod(this.apiName);
  final String apiName;

  /// Hali tanlanmagan bo'lishi mumkin (Requested) — null.
  static ReturnMethod? tryParse(Object? v) => v == null ? null : _parseEnum(values, v, (e) => e.apiName, courierPickup);
}

class ReturnItem {
  const ReturnItem({required this.orderItemId, required this.productName, required this.quantity, required this.unitPrice});

  final String orderItemId;
  final String productName;
  final double quantity;
  final double unitPrice;

  double get lineTotal => quantity * unitPrice;

  factory ReturnItem.fromJson(Map<String, dynamic> j) => ReturnItem(
        orderItemId: _str(j['onlineOrderItemId'] ?? j['orderItemId'] ?? j['itemId']) ?? '',
        productName: _str(j['productName']) ?? '',
        quantity: _num(j['quantity']),
        unitPrice: _num(j['unitPrice']),
      );
}

/// `GET /api/shop/returns` va `/{id}` javobi (ro'yxatda ba'zi maydonlar bo'lmasligi mumkin).
class ShopReturn {
  const ShopReturn({
    required this.id,
    required this.number,
    required this.status,
    required this.reason,
    required this.refundMethod,
    required this.createdAt,
    this.orderId,
    this.orderNumber,
    this.method,
    this.comment,
    this.photoUrls = const [],
    this.rejectReason,
    this.refundAmount = 0,
    this.loyaltyWithheldAmount = 0,
    this.items = const [],
    this.storeName,
    this.approvedAt,
    this.pickedUpAt,
    this.receivedAt,
    this.refundedAt,
    this.rejectedAt,
    this.cancelledAt,
    this.canCancelRaw,
  });

  final String id;
  final String number;
  final ReturnStatus status;
  final ReturnReason reason;
  final RefundMethod refundMethod;
  final DateTime createdAt;
  final String? orderId;
  final String? orderNumber;
  final ReturnMethod? method;
  final String? comment;
  final List<String> photoUrls;
  final String? rejectReason;
  final double refundAmount;
  final double loyaltyWithheldAmount;
  final List<ReturnItem> items;
  final String? storeName;
  final DateTime? approvedAt;
  final DateTime? pickedUpAt;
  final DateTime? receivedAt;
  final DateTime? refundedAt;
  final DateTime? rejectedAt;
  final DateTime? cancelledAt;
  final bool? canCancelRaw;

  /// Bekor qilish faqat `Requested` holatida (server `canCancel` bersa — o'shasi).
  bool get canCancel => canCancelRaw ?? status == ReturnStatus.requested;

  /// Server summa bermagan bo'lsa — qatorlar yig'indisi.
  double get itemsTotal => items.fold(0.0, (s, e) => s + e.lineTotal);

  factory ShopReturn.fromJson(Map<String, dynamic> j) => ShopReturn(
        id: _str(j['id']) ?? '',
        number: _str(j['number']) ?? '',
        status: ReturnStatus.parse(j['status']),
        reason: ReturnReason.parse(j['reason']),
        refundMethod: RefundMethod.parse(j['refundMethod']),
        createdAt: _date(j['createdAt']) ?? DateTime.now(),
        orderId: _str(j['onlineOrderId'] ?? j['orderId']),
        orderNumber: _str(j['orderNumber']),
        method: ReturnMethod.tryParse(j['method']),
        comment: _str(j['comment']),
        photoUrls: [for (final p in (j['photoUrls'] as List? ?? const [])) ?_str(p)],
        rejectReason: _str(j['rejectReason']),
        refundAmount: _num(j['refundAmount']),
        loyaltyWithheldAmount: _num(j['loyaltyWithheldAmount']),
        items: [
          for (final e in (j['items'] as List? ?? const []))
            if (e is Map) ReturnItem.fromJson(Map<String, dynamic>.from(e)),
        ],
        storeName: _str(j['storeName'] ?? j['branchName']),
        approvedAt: _date(j['approvedAt']),
        pickedUpAt: _date(j['pickedUpAt']),
        receivedAt: _date(j['receivedAt']),
        refundedAt: _date(j['refundedAt']),
        rejectedAt: _date(j['rejectedAt']),
        cancelledAt: _date(j['cancelledAt']),
        canCancelRaw: j['canCancel'] is bool ? j['canCancel'] as bool : null,
      );

  /// Ro'yxat javobi: massiv yoki `{ items: [...] }`.
  static List<ShopReturn> listFromJson(Object? data) {
    final list = data is List ? data : (data is Map ? data['items'] as List? : null) ?? const [];
    return [
      for (final e in list)
        if (e is Map) ShopReturn.fromJson(Map<String, dynamic>.from(e)),
    ];
  }
}

/// Qaytarish so'rovidagi bitta qator.
class ReturnRequestItem {
  const ReturnRequestItem({required this.orderItemId, required this.quantity});

  final String orderItemId;
  final double quantity;

  /// Server nomi kelishuvda: `OnlineOrderItemId` — ikkalasi ham yuboriladi (tolerant backend uchun).
  Map<String, dynamic> toJson() => {'onlineOrderItemId': orderItemId, 'orderItemId': orderItemId, 'quantity': quantity};
}
