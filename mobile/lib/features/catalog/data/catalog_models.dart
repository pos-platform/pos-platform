// Katalog API (`/api/shop/stores`) modellari. Enumlar serverdan string (camelCase emas — `Piece`, `Kilogram`).

double _d(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
double? _dn(Object? v) => v == null ? null : _d(v);
String? _text(Object? v) => v is String && v.trim().isNotEmpty ? v.trim() : null;

/// Server UTC vaqti (zonasiz bo'lsa ham UTC deb olinadi) → mahalliy vaqt.
DateTime? parseUtc(Object? v) {
  if (v is! String || v.isEmpty) return null;
  final hasZone = v.endsWith('Z') || RegExp(r'[+-]\d\d:?\d\d$').hasMatch(v);
  return DateTime.tryParse(hasZone ? v : '${v}Z')?.toLocal();
}

/// Mahalliy vaqt → "HH:mm".
String formatHhMm(DateTime d) => '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

/// "HH:mm:ss" → daqiqa (kun boshidan). Null — ish vaqti berilmagan.
int? _minutes(Object? v) {
  if (v is! String || v.isEmpty) return null;
  final p = v.split(':');
  if (p.length < 2) return null;
  final h = int.tryParse(p[0]), m = int.tryParse(p[1]);
  return h == null || m == null ? null : h * 60 + m;
}

/// Ilovadagi do'kon = tashkilotning bitta filiali (`id` — filial ID).
class ShopStore {
  const ShopStore({
    required this.id,
    required this.name,
    required this.branchName,
    this.description,
    this.logoUrl,
    this.phone,
    this.address,
    this.distanceKm,
    required this.isFeatured,
    required this.isOpenNow,
    required this.isAcceptingOrders,
    this.opensAtMinutes,
    this.closesAtMinutes,
    required this.deliveryEnabled,
    required this.pickupEnabled,
    required this.minOrderAmount,
    required this.deliveryFee,
    this.freeDeliveryFrom,
    required this.allowCash,
    required this.allowTransfer,
    required this.allowCardOnDelivery,
    this.deliveryMinMinutes,
    this.deliveryMaxMinutes,
    this.allowPayme = false,
    this.allowClick = false,
    this.bonusPayEnabled = false,
    this.bonusMaxPercent,
    this.deliveryRadiusKm,
    this.supportPhone,
    this.supportTelegram,
    this.organizationId,
    this.acceptWhenClosed = false,
    this.nextOpenAt,
  });

  final String id;
  final String name;
  final String branchName;
  final String? description;
  final String? logoUrl;
  final String? phone;
  final String? address;
  final double? distanceKm;
  final bool isFeatured;
  final bool isOpenNow;
  final bool isAcceptingOrders;

  /// Ish vaqti mahalliy (UTC+5), kun boshidan daqiqada. Null — doim ochiq.
  final int? opensAtMinutes;
  final int? closesAtMinutes;
  final bool deliveryEnabled;
  final bool pickupEnabled;
  final double minOrderAmount;
  final double deliveryFee;
  final double? freeDeliveryFrom;
  final bool allowCash;
  final bool allowTransfer;
  final bool allowCardOnDelivery;

  /// Taxminiy yetkazish vaqti (daqiqa), masalan 60–90. Null — ko'rsatilmaydi.
  final int? deliveryMinMinutes;
  final int? deliveryMaxMinutes;

  /// Online to'lov (yig'ilgandan keyin ilovada): Payme / Click.
  final bool allowPayme;
  final bool allowClick;

  /// Bonus bilan to'lash yoqilganmi va mahsulotlar summasining necha foizigacha (0..100).
  final bool bonusPayEnabled;
  final double? bonusMaxPercent;

  /// Yetkazish radiusi (km). Null — cheklovsiz.
  final double? deliveryRadiusKm;

  /// "Yordam": qo'ng'iroq raqami va Telegram username (`@` siz) — kelishuv 9.3.
  final String? supportPhone;
  final String? supportTelegram;

  /// Kelishuvda yo'q — server bersa ishlatiladi ("Do'stni taklif qilish" uchun tashkilot).
  final String? organizationId;

  /// Kelishuv 10.1: yopiq do'kon ham buyurtma qabul qiladimi (oldindan buyurtma). Eski server bermasa — false.
  final bool acceptWhenClosed;

  /// Yopiq bo'lsa keyingi ochilish vaqti (serverda UTC, bu yerda mahalliy vaqtga o'tkazilgan).
  final DateTime? nextOpenAt;

  /// Hozir yopiq, lekin oldindan buyurtma qabul qilinadi.
  bool get acceptsPreorder => !isOpenNow && acceptWhenClosed && isAcceptingOrders;

  /// Keyingi ochilish "HH:mm" (mahalliy) yoki null.
  String? get nextOpenTime => nextOpenAt == null ? null : formatHhMm(nextOpenAt!);

  bool get hasHours => opensAtMinutes != null && closesAtMinutes != null && opensAtMinutes != closesAtMinutes;

  /// Serverdagi `IsWithinHours` bilan bir xil: tungi smena (22:00–06:00) ham hisobga olinadi.
  bool isWithinHours(int minuteOfDay) {
    if (!hasHours) return true;
    final o = opensAtMinutes!, c = closesAtMinutes!;
    return o < c ? minuteOfDay >= o && minuteOfDay < c : minuteOfDay >= o || minuteOfDay < c;
  }

  static String _hhmm(int m) => '${(m ~/ 60).toString().padLeft(2, '0')}:${(m % 60).toString().padLeft(2, '0')}';

  /// "09:00–22:00" yoki null.
  String? get hoursText => hasHours ? '${_hhmm(opensAtMinutes!)}–${_hhmm(closesAtMinutes!)}' : null;

  factory ShopStore.fromJson(Map<String, dynamic> j) => ShopStore(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        branchName: j['branchName'] as String? ?? '',
        description: j['description'] as String?,
        logoUrl: j['logoUrl'] as String?,
        phone: j['phone'] as String?,
        address: j['address'] as String?,
        distanceKm: _dn(j['distanceKm']),
        isFeatured: j['isFeatured'] == true,
        isOpenNow: j['isOpenNow'] == true,
        isAcceptingOrders: j['isAcceptingOrders'] == true,
        opensAtMinutes: _minutes(j['opensAt']),
        closesAtMinutes: _minutes(j['closesAt']),
        deliveryEnabled: j['deliveryEnabled'] == true,
        pickupEnabled: j['pickupEnabled'] == true,
        minOrderAmount: _d(j['minOrderAmount']),
        deliveryFee: _d(j['deliveryFee']),
        freeDeliveryFrom: _dn(j['freeDeliveryFrom']),
        allowCash: j['allowCash'] == true,
        allowTransfer: j['allowTransfer'] == true,
        allowCardOnDelivery: j['allowCardOnDelivery'] == true,
        deliveryMinMinutes: (j['deliveryMinMinutes'] as num?)?.toInt(),
        deliveryMaxMinutes: (j['deliveryMaxMinutes'] as num?)?.toInt(),
        allowPayme: j['allowPayme'] == true,
        allowClick: j['allowClick'] == true,
        bonusPayEnabled: j['bonusPayEnabled'] == true,
        bonusMaxPercent: _dn(j['bonusMaxPercent']),
        deliveryRadiusKm: _dn(j['deliveryRadiusKm']),
        supportPhone: _text(j['supportPhone']),
        supportTelegram: _text(j['supportTelegram'])?.replaceFirst('@', ''),
        organizationId: _text(j['organizationId']),
        acceptWhenClosed: j['acceptWhenClosed'] == true,
        nextOpenAt: parseUtc(j['nextOpenAt']),
      );
}

class ShopCategory {
  const ShopCategory({
    required this.id,
    required this.name,
    required this.productCount,
    this.parentId,
    this.imageUrl,
  });

  final String id;
  final String name;

  /// Asosiy bo'limda — ichki bo'limlari bilan birga.
  final int productCount;

  /// Ota bo'lim (null — asosiy bo'lim).
  final String? parentId;
  final String? imageUrl;

  bool get isRoot => parentId == null;

  factory ShopCategory.fromJson(Map<String, dynamic> j) => ShopCategory(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        productCount: (j['productCount'] as num?)?.toInt() ?? 0,
        parentId: j['parentId'] as String?,
        imageUrl: j['imageUrl'] as String?,
      );
}

class ShopProduct {
  const ShopProduct({
    required this.id,
    required this.name,
    required this.categoryId,
    required this.unit,
    required this.isWeighed,
    this.imageUrl,
    required this.price,
    this.oldPrice,
    this.discountPercent,
    required this.available,
    required this.inStock,
    this.rank,
    this.discountEndsAt,
    this.isFavorite,
  });

  final String id;
  final String name;
  final String categoryId;

  /// `Piece` yoki `Kilogram`.
  final String unit;
  final bool isWeighed;
  final String? imageUrl;

  /// Aksiya qo'llangan yakuniy narx.
  final double price;

  /// Aksiya bo'lsa — chegirmasiz narx (chizilgan).
  final double? oldPrice;
  final double? discountPercent;
  final double available;
  final bool inStock;

  /// Top 10 javobida o'rni (1..10).
  final int? rank;

  /// Aksiya tugash vaqti (ortga sanash uchun), mahalliy vaqtda.
  final DateTime? discountEndsAt;

  /// Token bo'lsa server beradi; null — noma'lum (sevimlilar keshidan olinadi).
  final bool? isFavorite;

  bool get isKg => isWeighed || unit == 'Kilogram';

  factory ShopProduct.fromJson(Map<String, dynamic> j) => ShopProduct(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        categoryId: j['categoryId'] as String? ?? '',
        unit: j['unit'] as String? ?? 'Piece',
        isWeighed: j['isWeighed'] == true,
        imageUrl: j['imageUrl'] as String?,
        price: _d(j['price']),
        oldPrice: _dn(j['oldPrice']),
        discountPercent: _dn(j['discountPercent']),
        available: _d(j['available']),
        inStock: j['inStock'] == true,
        rank: (j['rank'] as num?)?.toInt(),
        discountEndsAt: parseUtc(j['discountEndsAt']),
        isFavorite: j['isFavorite'] is bool ? j['isFavorite'] as bool : null,
      );

  /// Ro'yxat JSON'i (noto'g'ri elementlar tashlab yuboriladi).
  static List<ShopProduct> listFrom(Object? raw) => [
        for (final e in raw is List ? raw : const [])
          if (e is Map && e['id'] is String) ShopProduct.fromJson(Map<String, dynamic>.from(e)),
      ];
}

class ShopProductDetail {
  const ShopProductDetail({
    required this.product,
    this.description,
    this.barcode,
    this.categoryName,
    this.isSubscribed,
  });

  final ShopProduct product;
  final String? description;
  final String? barcode;
  final String? categoryName;

  /// "Kelganda xabar berish" obunasi (token bo'lsa).
  final bool? isSubscribed;

  factory ShopProductDetail.fromJson(Map<String, dynamic> j) => ShopProductDetail(
        product: ShopProduct.fromJson(j),
        description: j['description'] as String?,
        barcode: j['barcode'] as String?,
        categoryName: j['categoryName'] as String?,
        isSubscribed: j['isSubscribed'] is bool ? j['isSubscribed'] as bool : null,
      );
}

class PagedResult<T> {
  const PagedResult({required this.items, required this.totalCount, required this.page, required this.totalPages});

  final List<T> items;
  final int totalCount;
  final int page;
  final int totalPages;

  bool get hasMore => page < totalPages;

  factory PagedResult.fromJson(Map<String, dynamic> j, T Function(Map<String, dynamic>) item) => PagedResult(
        items: (j['items'] as List? ?? const []).map((e) => item(e as Map<String, dynamic>)).toList(),
        totalCount: (j['totalCount'] as num?)?.toInt() ?? 0,
        page: (j['page'] as num?)?.toInt() ?? 1,
        totalPages: (j['totalPages'] as num?)?.toInt() ?? 1,
      );
}

/// Mahsulot tartibi (server `sort` qiymatlari).
enum ProductSort {
  popular('popular'),
  name('name'),
  priceAsc('price_asc'),
  priceDesc('price_desc');

  const ProductSort(this.api);
  final String api;
}
