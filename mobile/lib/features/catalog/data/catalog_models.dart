// Katalog API (`/api/shop/stores`) modellari. Enumlar serverdan string (camelCase emas — `Piece`, `Kilogram`).

double _d(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
double? _dn(Object? v) => v == null ? null : _d(v);

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
      );
}

class ShopCategory {
  const ShopCategory({required this.id, required this.name, required this.productCount});

  final String id;
  final String name;
  final int productCount;

  factory ShopCategory.fromJson(Map<String, dynamic> j) => ShopCategory(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        productCount: (j['productCount'] as num?)?.toInt() ?? 0,
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
      );
}

class ShopProductDetail {
  const ShopProductDetail({required this.product, this.description, this.barcode, this.categoryName});

  final ShopProduct product;
  final String? description;
  final String? barcode;
  final String? categoryName;

  factory ShopProductDetail.fromJson(Map<String, dynamic> j) => ShopProductDetail(
        product: ShopProduct.fromJson(j),
        description: j['description'] as String?,
        barcode: j['barcode'] as String?,
        categoryName: j['categoryName'] as String?,
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
