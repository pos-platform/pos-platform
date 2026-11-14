/// Mijoz buyurtmalari modellari (backend: `ShopOrderDtos.cs`). Enumlar serverdan matn ko'rinishida keladi.
library;

enum OrderStatus {
  created('New'),
  accepted('Accepted'),
  picking('Picking'),
  ready('Ready'),
  awaitingTransfer('AwaitingTransfer'),
  onTheWay('OnTheWay'),
  readyForPickup('ReadyForPickup'),
  delivered('Delivered'),
  rejected('Rejected'),
  cancelled('Cancelled');

  const OrderStatus(this.apiName);
  final String apiName;

  static OrderStatus parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, created);

  bool get isTerminal => this == delivered || this == rejected || this == cancelled;
  bool get isFailed => this == rejected || this == cancelled;
}

enum FulfillmentType {
  delivery('Delivery'),
  pickup('Pickup');

  const FulfillmentType(this.apiName);
  final String apiName;

  static FulfillmentType parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, delivery);
}

/// Server enumi: Cash=0, Card=1, Mixed=2, Debt=3, Transfer=4.
enum PaymentMethod {
  cash('Cash'),
  card('Card'),
  mixed('Mixed'),
  debt('Debt'),
  transfer('Transfer');

  const PaymentMethod(this.apiName);
  final String apiName;

  static PaymentMethod parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, cash);
}

enum PaymentStatus {
  unpaid('Unpaid'),
  paid('Paid'),
  refunded('Refunded');

  const PaymentStatus(this.apiName);
  final String apiName;

  static PaymentStatus parse(Object? v) => _parseEnum(values, v, (e) => e.apiName, unpaid);
}

/// Matn (registrsiz) yoki raqam (enum indeksi) ko'rinishidagi qiymatni o'qiydi.
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

double _num(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
double? _numOrNull(Object? v) => v == null ? null : _num(v);
String? _str(Object? v) => v is String && v.isNotEmpty ? v : null;

/// Server UTC vaqt qaytaradi; zona ko'rsatilmagan bo'lsa ham UTC deb olinadi, so'ng mahalliy vaqtga o'tkaziladi.
DateTime? _date(Object? v) {
  if (v is! String || v.isEmpty) return null;
  final hasZone = v.endsWith('Z') || RegExp(r'[+-]\d\d:?\d\d$').hasMatch(v);
  return DateTime.tryParse(hasZone ? v : '${v}Z')?.toLocal();
}

class OrderSummary {
  OrderSummary({
    required this.id,
    required this.number,
    required this.branchId,
    required this.storeName,
    required this.status,
    required this.fulfillmentType,
    required this.paymentMethod,
    required this.paymentStatus,
    required this.totalAmount,
    required this.itemCount,
    required this.previewImages,
    required this.createdAt,
    required this.isActive,
  });

  final String id;
  final int number;
  final String branchId;
  final String storeName;
  final OrderStatus status;
  final FulfillmentType fulfillmentType;
  final PaymentMethod paymentMethod;
  final PaymentStatus paymentStatus;
  final double totalAmount;
  final int itemCount;
  final List<String?> previewImages;
  final DateTime createdAt;
  final bool isActive;

  factory OrderSummary.fromJson(Map<String, dynamic> j) => OrderSummary(
        id: j['id'] as String,
        number: (j['number'] as num?)?.toInt() ?? 0,
        branchId: j['branchId'] as String? ?? '',
        storeName: j['storeName'] as String? ?? '',
        status: OrderStatus.parse(j['status']),
        fulfillmentType: FulfillmentType.parse(j['fulfillmentType']),
        paymentMethod: PaymentMethod.parse(j['paymentMethod']),
        paymentStatus: PaymentStatus.parse(j['paymentStatus']),
        totalAmount: _num(j['totalAmount']),
        itemCount: (j['itemCount'] as num?)?.toInt() ?? 0,
        previewImages: (j['previewImages'] as List? ?? const []).map((e) => _str(e)).toList(),
        createdAt: _date(j['createdAt']) ?? DateTime.now(),
        isActive: j['isActive'] as bool? ?? false,
      );
}

class OrderItem {
  OrderItem({
    required this.id,
    required this.productId,
    required this.productName,
    required this.imageUrl,
    required this.unitPrice,
    required this.quantity,
    required this.pickedQuantity,
    required this.lineTotal,
    required this.isRemoved,
    required this.replacesItemId,
    this.returnedQuantity = 0,
    this.returnableQuantityRaw,
  });

  final String id;
  final String productId;
  final String productName;
  final String? imageUrl;
  final double unitPrice;
  final double quantity;
  final double? pickedQuantity;
  final double lineTotal;
  final bool isRemoved;

  /// Almashtirish qatori bo'lsa — qaysi qator o'rniga.
  final String? replacesItemId;

  /// Kelishuv 10.5 (ixtiyoriy): allaqachon qaytarilgan (yoki rad etilgan) miqdor.
  final double returnedQuantity;

  /// Server qaytariladigan miqdorni o'zi bersa (`returnableQuantity`).
  final double? returnableQuantityRaw;

  bool get isReplacement => replacesItemId != null;

  /// Qaytarish mumkin bo'lgan miqdor: olingan − qaytarilgan (server bergan bo'lsa — o'shasi).
  double get returnableQuantity {
    if (isRemoved) return 0;
    final raw = returnableQuantityRaw;
    if (raw != null) return raw < 0 ? 0 : raw;
    final left = (pickedQuantity ?? quantity) - returnedQuantity;
    return left < 0 ? 0 : left;
  }

  factory OrderItem.fromJson(Map<String, dynamic> j) => OrderItem(
        id: j['id'] as String? ?? '',
        productId: j['productId'] as String? ?? '',
        productName: j['productName'] as String? ?? '',
        imageUrl: _str(j['imageUrl']),
        unitPrice: _num(j['unitPrice']),
        quantity: _num(j['quantity']),
        pickedQuantity: _numOrNull(j['pickedQuantity']),
        lineTotal: _num(j['lineTotal']),
        isRemoved: j['isRemoved'] as bool? ?? false,
        replacesItemId: _str(j['replacesItemId']),
        returnedQuantity: _num(j['returnedQuantity'] ?? j['refundedQuantity'] ?? j['rejectedQuantity']),
        returnableQuantityRaw: _numOrNull(j['returnableQuantity']),
      );
}

class OrderDetail extends OrderSummary {
  OrderDetail({
    required super.id,
    required super.number,
    required super.branchId,
    required super.storeName,
    required super.status,
    required super.fulfillmentType,
    required super.paymentMethod,
    required super.paymentStatus,
    required super.totalAmount,
    required super.itemCount,
    required super.previewImages,
    required super.createdAt,
    required super.isActive,
    required this.branchName,
    required this.storePhone,
    required this.branchAddress,
    required this.addressLine,
    required this.scheduledFor,
    required this.comment,
    required this.itemsTotal,
    required this.deliveryFee,
    required this.items,
    required this.acceptedAt,
    required this.readyAt,
    required this.dispatchedAt,
    required this.deliveredAt,
    required this.cancelledAt,
    required this.cancelReason,
    required this.paymentConfirmedAt,
    required this.canCancel,
    required this.canUploadTransferProof,
    required this.transferProofUrl,
    required this.transferCardNumber,
    required this.transferCardHolder,
    this.courierName,
    this.courierPhone,
    this.pickingStartedAt,
    this.receiptNumber,
    this.fiscalQrUrl,
    this.estimatedDeliveryFrom,
    this.estimatedDeliveryTo,
    this.promoCode,
    this.promoDiscount = 0,
    this.bonusUsed = 0,
    this.canPayOnline = false,
    this.onlineProviders = const [],
    this.onlinePaymentProvider,
    this.canRate = false,
    this.rating,
    this.ratingComment,
    this.ratingTags = const [],
    this.ratedAt,
    this.isPreorder = false,
    this.canReturn = false,
  });

  final String? branchName;
  final String? storePhone;
  final String? branchAddress;
  final String? addressLine;
  final DateTime? scheduledFor;
  final String? comment;
  final double itemsTotal;
  final double deliveryFee;
  final List<OrderItem> items;
  final DateTime? acceptedAt;
  final DateTime? readyAt;
  final DateTime? dispatchedAt;
  final DateTime? deliveredAt;
  final DateTime? cancelledAt;
  final String? cancelReason;
  final DateTime? paymentConfirmedAt;
  final bool canCancel;
  final bool canUploadTransferProof;
  final String? transferProofUrl;
  final String? transferCardNumber;
  final String? transferCardHolder;
  final String? courierName;
  final String? courierPhone;
  final DateTime? pickingStartedAt;

  /// Fiskal chek raqami va QR havolasi (soliq saytidagi chek).
  final String? receiptNumber;
  final String? fiscalQrUrl;

  /// Taxminiy yetkazish oralig'i (mahalliy vaqtga o'tkazilgan).
  final DateTime? estimatedDeliveryFrom;
  final DateTime? estimatedDeliveryTo;

  /// Promokod va uning chegirmasi, bonus bilan to'langan summa (1 ball = 1 so'm).
  final String? promoCode;
  final double promoDiscount;
  final double bonusUsed;

  /// Yakuniy summa ma'lum va to'lanmagan bo'lsa — ilovada online to'lash mumkin ([onlineProviders]: "Payme", "Click").
  final bool canPayOnline;
  final List<String> onlineProviders;

  /// Checkout'da tanlangan online provayder (Payme/Click) — bunda karta raqami ko'rsatilmaydi, faqat to'lov tugmasi.
  final String? onlinePaymentProvider;

  /// Baho (kelishuv 9.3): Delivered, baholanmagan va 14 kun ichida bo'lsa [canRate].
  final bool canRate;
  final int? rating;
  final String? ratingComment;

  /// `fast|fresh|polite|accurate|late|damaged|missing|wrong_item`.
  final List<String> ratingTags;
  final DateTime? ratedAt;

  /// Kelishuv 10.1: yopiq do'konga berilgan buyurtma — do'kon ochilganda ([scheduledFor]) ko'rib chiqiladi.
  final bool isPreorder;

  /// Kelishuv 10.5: Delivered va qaytarish muddati ichida — "Qaytarish" tugmasi.
  final bool canReturn;

  bool get isOnlinePayment => onlinePaymentProvider != null;

  factory OrderDetail.fromJson(Map<String, dynamic> j) {
    final s = OrderSummary.fromJson(j);
    return OrderDetail(
      id: s.id,
      number: s.number,
      branchId: s.branchId,
      storeName: s.storeName,
      status: s.status,
      fulfillmentType: s.fulfillmentType,
      paymentMethod: s.paymentMethod,
      paymentStatus: s.paymentStatus,
      totalAmount: s.totalAmount,
      itemCount: s.itemCount,
      previewImages: s.previewImages,
      createdAt: s.createdAt,
      isActive: s.isActive,
      branchName: _str(j['branchName']),
      storePhone: _str(j['storePhone']),
      branchAddress: _str(j['branchAddress']),
      addressLine: _str(j['addressLine']),
      scheduledFor: _date(j['scheduledFor']),
      comment: _str(j['comment']),
      itemsTotal: _num(j['itemsTotal']),
      deliveryFee: _num(j['deliveryFee']),
      items: (j['items'] as List? ?? const [])
          .map((e) => OrderItem.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(),
      acceptedAt: _date(j['acceptedAt']),
      readyAt: _date(j['readyAt']),
      dispatchedAt: _date(j['dispatchedAt']),
      deliveredAt: _date(j['deliveredAt']),
      cancelledAt: _date(j['cancelledAt']),
      cancelReason: _str(j['cancelReason']),
      paymentConfirmedAt: _date(j['paymentConfirmedAt']),
      canCancel: j['canCancel'] as bool? ?? false,
      canUploadTransferProof: j['canUploadTransferProof'] as bool? ?? false,
      transferProofUrl: _str(j['transferProofUrl']),
      transferCardNumber: _str(j['transferCardNumber']),
      transferCardHolder: _str(j['transferCardHolder']),
      courierName: _str(j['courierName']),
      courierPhone: _str(j['courierPhone']),
      pickingStartedAt: _date(j['pickingStartedAt']),
      receiptNumber: j['receiptNumber'] == null ? null : _str('${j['receiptNumber']}'),
      fiscalQrUrl: _str(j['fiscalQrUrl']),
      estimatedDeliveryFrom: _date(j['estimatedDeliveryFrom']),
      estimatedDeliveryTo: _date(j['estimatedDeliveryTo']),
      promoCode: _str(j['promoCode']),
      promoDiscount: _num(j['promoDiscount']),
      bonusUsed: _num(j['bonusUsed']),
      canPayOnline: j['canPayOnline'] as bool? ?? false,
      onlineProviders: [for (final p in (j['onlineProviders'] as List? ?? const [])) ?_str(p)],
      onlinePaymentProvider: _str(j['onlinePaymentProvider']),
      canRate: j['canRate'] == true,
      rating: (j['rating'] as num?)?.toInt(),
      ratingComment: _str(j['ratingComment']),
      ratingTags: switch (j['ratingTags']) {
        final List l => [for (final e in l) ?_str(e)],
        // Ehtiyot: vergulli matn ko'rinishida kelsa ham.
        final String s => [for (final e in s.split(',')) ?_str(e.trim())],
        _ => const [],
      },
      ratedAt: _date(j['ratedAt']),
      isPreorder: j['isPreorder'] == true,
      canReturn: j['canReturn'] == true,
    );
  }
}

class OrdersPage {
  const OrdersPage({required this.items, required this.totalCount, required this.page, required this.pageSize});

  final List<OrderSummary> items;
  final int totalCount;
  final int page;
  final int pageSize;

  bool get hasMore => items.length < totalCount;

  factory OrdersPage.fromJson(Map<String, dynamic> j) => OrdersPage(
        items: (j['items'] as List? ?? const [])
            .map((e) => OrderSummary.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList(),
        totalCount: (j['totalCount'] as num?)?.toInt() ?? 0,
        page: (j['page'] as num?)?.toInt() ?? 1,
        pageSize: (j['pageSize'] as num?)?.toInt() ?? 20,
      );
}

/// SignalR `OrderStatusChanged` xabari (shaxsiy ma'lumotsiz).
class OrderStatusEvent {
  const OrderStatusEvent({required this.id, required this.number, required this.status});

  final String id;
  final int number;
  final OrderStatus status;

  static OrderStatusEvent? tryParse(Object? raw) {
    if (raw is! Map) return null;
    final id = raw['id'] ?? raw['Id'];
    if (id is! String) return null;
    return OrderStatusEvent(
      id: id,
      number: ((raw['number'] ?? raw['Number']) as num?)?.toInt() ?? 0,
      status: OrderStatus.parse(raw['status'] ?? raw['Status']),
    );
  }
}
