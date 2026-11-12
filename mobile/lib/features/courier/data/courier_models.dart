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
  const CourierOrderItem({required this.productName, required this.quantity, required this.unitPrice, required this.lineTotal});

  final String productName;
  final double quantity;
  final double unitPrice;
  final double lineTotal;

  factory CourierOrderItem.fromJson(Map<String, dynamic> j) => CourierOrderItem(
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
  });

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

  factory CourierOrder.fromJson(Map<String, dynamic> j) => CourierOrder(
        id: j['id']?.toString() ?? '',
        number: j['number']?.toString() ?? '',
        status: j['status']?.toString() ?? '',
        customerName: _s(j['customerName']),
        customerPhone: _s(j['customerPhone']),
        addressLine: _s(j['addressLine']),
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
