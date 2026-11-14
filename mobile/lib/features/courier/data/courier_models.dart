double _d(Object? v) => v is num ? v.toDouble() : double.tryParse(v?.toString() ?? '') ?? 0;
double? _dn(Object? v) => v == null ? null : (v is num ? v.toDouble() : double.tryParse(v.toString()));
String? _s(Object? v) {
  final s = v?.toString().trim();
  return s == null || s.isEmpty ? null : s;
}

/// `GET /api/courier/me`.
class CourierMe {
  const CourierMe({required this.id, required this.name, this.phone, this.branchName, this.storeName});

  final String id;
  final String name;
  final String? phone;
  final String? branchName;
  final String? storeName;

  factory CourierMe.fromJson(Map<String, dynamic> j) => CourierMe(
        id: j['id']?.toString() ?? '',
        name: _s(j['name']) ?? '',
        phone: _s(j['phone']),
        branchName: _s(j['branchName']),
        storeName: _s(j['storeName']),
      );
}

class CourierOrderItem {
  const CourierOrderItem({
    this.id,
    required this.productName,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  /// Qator ID (eshik oldida rad etish uchun `itemId`). Eski server bermasa — null (rad etib bo'lmaydi).
  final String? id;
  final String productName;
  final double quantity;
  final double unitPrice;
  final double lineTotal;

  factory CourierOrderItem.fromJson(Map<String, dynamic> j) => CourierOrderItem(
        id: _s(j['id'] ?? j['itemId'] ?? j['onlineOrderItemId']),
        productName: _s(j['productName']) ?? '',
        quantity: _d(j['quantity']),
        unitPrice: _d(j['unitPrice']),
        lineTotal: _d(j['lineTotal']),
      );
}

/// Kuryerga biriktirilgan buyurtma (`/api/courier/orders`).
class CourierOrder {
  const CourierOrder({
    required this.id,
    required this.number,
    required this.status,
    this.customerName,
    this.customerPhone,
    this.addressLine,
    this.entrance,
    this.floor,
    this.apartment,
    this.intercom,
    this.landmark,
    this.comment,
    this.contactless = false,
    this.latitude,
    this.longitude,
    required this.totalAmount,
    required this.paymentMethod,
    required this.paymentStatus,
    this.itemCount = 0,
    this.dispatchedAt,
    this.storeName,
    this.branchAddress,
    this.items = const [],
    this.kind = CourierTaskKind.delivery,
    this.orderNumber,
  });

  /// Vazifa turi (kelishuv 10.5): oddiy yetkazish yoki qaytarishni olib ketish. Qaytarishda [id] — qaytarish ID.
  final CourierTaskKind kind;

  /// Qaytarish vazifasida asl buyurtma raqami (bo'lsa).
  final String? orderNumber;

  final String id;
  final String number;
  final String status;
  final String? customerName;
  final String? customerPhone;
  final String? addressLine;
  final String? entrance;
  final String? floor;
  final String? apartment;
  final String? intercom;
  final String? landmark;
  final String? comment;
  final bool contactless;
  final double? latitude;
  final double? longitude;
  final double totalAmount;

  /// Cash | Card | Transfer ...
  final String paymentMethod;

  /// Unpaid | Paid | Refunded
  final String paymentStatus;
  final int itemCount;
  final DateTime? dispatchedAt;
  final String? storeName;
  final String? branchAddress;
  final List<CourierOrderItem> items;

  bool get hasCoordinates => latitude != null && longitude != null && !(latitude == 0 && longitude == 0);
  bool get isPaid => paymentStatus == 'Paid';
  bool get isCash => paymentMethod == 'Cash';

  /// Naqd va hali to'lanmagan — kuryer puli olishi kerak.
  bool get mustCollectCash => isCash && !isPaid;
  bool get isActive => status == 'OnTheWay';

  /// Taklif qabul qilingan, kuryer hali do'kondan olmagan (kelishuv 10.6) — "Do'kondan oldim".
  bool get isAwaitingPickup => kind == CourierTaskKind.delivery && status == 'Ready';

  bool get isReturnPickup => kind == CourierTaskKind.returnPickup;

  /// Qaytarish vazifasi hali olib ketilmagan.
  bool get isReturnPending =>
      isReturnPickup && status != 'PickedUp' && status != 'Received' && status != 'Refunded' && status != 'Cancelled';

  factory CourierOrder.fromJson(Map<String, dynamic> j) => CourierOrder(
        kind: CourierTaskKind.parse(j['kind']),
        orderNumber: _s(j['orderNumber']),
        id: (j['id'] ?? j['returnId'])?.toString() ?? '',
        number: j['number']?.toString() ?? '',
        status: j['status']?.toString() ?? '',
        customerName: _s(j['customerName']),
        customerPhone: _s(j['customerPhone']),
        addressLine: _s(j['addressLine'] ?? j['address']),
        entrance: _s(j['entrance']),
        floor: _s(j['floor']),
        apartment: _s(j['apartment']),
        intercom: _s(j['intercom']),
        landmark: _s(j['landmark']),
        comment: _s(j['comment']),
        contactless: j['contactless'] == true,
        latitude: _dn(j['latitude']),
        longitude: _dn(j['longitude']),
        totalAmount: _d(j['totalAmount']),
        paymentMethod: j['paymentMethod']?.toString() ?? '',
        paymentStatus: j['paymentStatus']?.toString() ?? '',
        itemCount: (j['itemCount'] as num?)?.toInt() ?? 0,
        dispatchedAt: j['dispatchedAt'] == null ? null : DateTime.tryParse(j['dispatchedAt'].toString())?.toLocal(),
        storeName: _s(j['storeName']),
        branchAddress: _s(j['branchAddress']),
        items: (j['items'] as List?)
                ?.whereType<Map<String, dynamic>>()
                .map(CourierOrderItem.fromJson)
                .toList() ??
            const [],
      );
}

/// `kind`: `"delivery"` | `"return_pickup"` (yo'q bo'lsa — yetkazish).
enum CourierTaskKind {
  delivery,
  returnPickup;

  static CourierTaskKind parse(Object? v) {
    final s = v?.toString().toLowerCase().replaceAll('_', '');
    return s == 'returnpickup' ? returnPickup : delivery;
  }
}

/// Eshik oldida rad etilgan qator (`rejectedItems`). Sabab — qaytarish sabablari bilan bir xil enum nomi.
class CourierRejectedItem {
  const CourierRejectedItem({required this.itemId, required this.quantity, required this.reason, required this.unitPrice});

  final String itemId;
  final double quantity;

  /// `Damaged|WrongItem|Quality|Expired|Missing|Other`.
  final String reason;

  /// Faqat ilovada qayta hisoblash uchun (serverga yuborilmaydi).
  final double unitPrice;

  Map<String, dynamic> toJson() => {'itemId': itemId, 'quantity': quantity, 'reason': reason};
}

/// Kuryerga taklif (kelishuv 10.6): `GET /api/courier/offers`.
class CourierOffer {
  const CourierOffer({
    required this.offerId,
    required this.orderId,
    required this.number,
    this.branchName,
    this.address,
    this.distanceKm,
    required this.totalAmount,
    required this.paymentMethod,
    this.itemCount = 0,
    this.expiresAt,
  });

  final String offerId;
  final String orderId;
  final String number;
  final String? branchName;
  final String? address;
  final double? distanceKm;
  final double totalAmount;
  final String paymentMethod;
  final int itemCount;

  /// Mahalliy vaqt. Null — muddat cheklanmagan.
  final DateTime? expiresAt;

  bool get isCash => paymentMethod == 'Cash';

  /// Qolgan vaqt (manfiy bo'lmaydi).
  Duration remaining([DateTime? now]) {
    final e = expiresAt;
    if (e == null) return const Duration(days: 1);
    final d = e.difference(now ?? DateTime.now());
    return d.isNegative ? Duration.zero : d;
  }

  bool isExpired([DateTime? now]) => expiresAt != null && remaining(now) == Duration.zero;

  factory CourierOffer.fromJson(Map<String, dynamic> j) => CourierOffer(
        offerId: (j['offerId'] ?? j['id'])?.toString() ?? '',
        orderId: (j['orderId'] ?? j['onlineOrderId'])?.toString() ?? '',
        number: j['number']?.toString() ?? '',
        branchName: _s(j['branchName']),
        address: _s(j['address'] ?? j['addressLine']),
        distanceKm: _dn(j['distanceKm']),
        totalAmount: _d(j['totalAmount']),
        paymentMethod: j['paymentMethod']?.toString() ?? '',
        itemCount: (j['itemCount'] as num?)?.toInt() ?? 0,
        expiresAt: _utc(j['expiresAt']),
      );
}

/// Server UTC vaqti (zonasiz bo'lsa ham UTC) → mahalliy.
DateTime? _utc(Object? v) {
  if (v is! String || v.isEmpty) return null;
  final hasZone = v.endsWith('Z') || RegExp(r'[+-]\d\d:?\d\d$').hasMatch(v);
  return DateTime.tryParse(hasZone ? v : '${v}Z')?.toLocal();
}

/// Rad etilgan tovarlardan keyin kuryer oladigan summa (taxminiy; yakuniy summani server hisoblaydi).
double courierAmountToCollect(CourierOrder o, List<CourierRejectedItem> rejected) {
  final minus = rejected.fold(0.0, (s, r) => s + r.quantity * r.unitPrice);
  final left = o.totalAmount - minus;
  return left < 0 ? 0 : left;
}
