/// Bosqich 9 (marketing) modellari: bosh sahifa, bannerlar, kolleksiyalar, stories, qidiruv takliflari,
/// do'stni taklif qilish (kelishuv: docs/mobile/CONTRACT-9.md). Maydonlar yo'q bo'lsa ham xato bermaydi.
library;

import '../../catalog/data/catalog_models.dart';

double _d(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
String? _s(Object? v) => v is String && v.trim().isNotEmpty ? v.trim() : null;
List<Map<String, dynamic>> _maps(Object? raw) => [
  for (final e in raw is List ? raw : const [])
    if (e is Map) Map<String, dynamic>.from(e),
];

/// Banner / story slaydi / push kampaniya havolasi turi.
enum ShopLinkType {
  none,
  product,
  category,
  collection,
  discounts,
  url;

  /// Server qiymati (`Product`, `Url` ...; registr muhim emas). Noma'lum — [none].
  static ShopLinkType parse(Object? v) {
    final s = '$v'.toLowerCase();
    for (final e in values) {
      if (e.name == s) return e;
    }
    return none;
  }
}

/// Havola: tur + qiymat (Guid matni yoki https manzil).
class ShopLink {
  const ShopLink(this.type, this.value);

  final ShopLinkType type;
  final String? value;

  /// Bosilganda biror joy ochiladimi.
  bool get isActionable => switch (type) {
    ShopLinkType.none => false,
    ShopLinkType.discounts => true,
    _ => value != null && value!.isNotEmpty,
  };

  factory ShopLink.fromJson(Map<String, dynamic> j) => ShopLink(ShopLinkType.parse(j['linkType']), _s(j['linkValue']));
}

class ShopBanner {
  const ShopBanner({
    required this.id,
    required this.title,
    this.subtitle,
    this.imageUrl,
    this.backgroundColor,
    required this.link,
    this.endsAt,
  });

  final String id;
  final String title;
  final String? subtitle;
  final String? imageUrl;

  /// `#RRGGBB` yoki null (brend rangi).
  final String? backgroundColor;
  final ShopLink link;
  final DateTime? endsAt;

  factory ShopBanner.fromJson(Map<String, dynamic> j) => ShopBanner(
    id: '${j['id']}',
    title: _s(j['title']) ?? '',
    subtitle: _s(j['subtitle']),
    imageUrl: _s(j['imageUrl']),
    backgroundColor: _s(j['backgroundColor']),
    link: ShopLink.fromJson(j),
    endsAt: parseUtc(j['endsAt']),
  );
}

class ShopCollection {
  const ShopCollection({
    required this.id,
    required this.title,
    this.description,
    this.imageUrl,
    this.endsAt,
    required this.products,
  });

  final String id;
  final String title;
  final String? description;
  final String? imageUrl;
  final DateTime? endsAt;
  final List<ShopProduct> products;

  factory ShopCollection.fromJson(Map<String, dynamic> j) => ShopCollection(
    id: '${j['id']}',
    title: _s(j['title']) ?? '',
    description: _s(j['description']),
    imageUrl: _s(j['imageUrl']),
    endsAt: parseUtc(j['endsAt']),
    products: ShopProduct.listFrom(j['products']),
  );
}

class StorySlide {
  const StorySlide({
    required this.id,
    required this.imageUrl,
    this.caption,
    required this.link,
    required this.durationSec,
  });

  final String id;
  final String? imageUrl;
  final String? caption;
  final ShopLink link;

  /// 3–15 s (standart 6).
  final int durationSec;

  factory StorySlide.fromJson(Map<String, dynamic> j) => StorySlide(
    id: '${j['id']}',
    imageUrl: _s(j['imageUrl']),
    caption: _s(j['caption']),
    link: ShopLink.fromJson(j),
    durationSec: ((j['durationSec'] as num?)?.toInt() ?? 6).clamp(3, 15),
  );
}

class ShopStory {
  const ShopStory({required this.id, required this.title, this.coverImageUrl, required this.slides});

  final String id;
  final String title;
  final String? coverImageUrl;
  final List<StorySlide> slides;

  factory ShopStory.fromJson(Map<String, dynamic> j) => ShopStory(
    id: '${j['id']}',
    title: _s(j['title']) ?? '',
    coverImageUrl: _s(j['coverImageUrl']),
    slides: [for (final s in _maps(j['slides'])) StorySlide.fromJson(s)],
  );
}

/// `GET /api/shop/stores/{branchId}/home`.
class ShopHome {
  const ShopHome({
    this.banners = const [],
    this.topProducts = const [],
    this.collections = const [],
    this.discountedCount = 0,
    this.buyAgain = const [],
    this.stories = const [],
  });

  final List<ShopBanner> banners;
  final List<ShopProduct> topProducts;
  final List<ShopCollection> collections;
  final int discountedCount;
  final List<ShopProduct> buyAgain;
  final List<ShopStory> stories;

  factory ShopHome.fromJson(Map<String, dynamic> j) => ShopHome(
    banners: [for (final b in _maps(j['banners'])) ShopBanner.fromJson(b)],
    topProducts: ShopProduct.listFrom(j['topProducts']),
    collections: [
      for (final c in _maps(j['collections']))
        if (ShopCollection.fromJson(c) case final col when col.products.isNotEmpty) col,
    ],
    discountedCount: (j['discountedCount'] as num?)?.toInt() ?? 0,
    buyAgain: ShopProduct.listFrom(j['buyAgain']),
    stories: [
      for (final s in _maps(j['stories']))
        if (ShopStory.fromJson(s) case final st when st.slides.isNotEmpty) st,
    ],
  );
}

/// `GET .../search-suggest?q=`.
class SearchSuggest {
  const SearchSuggest({this.products = const [], this.categories = const []});

  final List<({String id, String name, String? imageUrl, double price})> products;
  final List<({String id, String name})> categories;

  bool get isEmpty => products.isEmpty && categories.isEmpty;

  factory SearchSuggest.fromJson(Map<String, dynamic> j) => SearchSuggest(
    products: [
      for (final p in _maps(j['products']))
        if (p['id'] != null)
          (id: '${p['id']}', name: _s(p['name']) ?? '', imageUrl: _s(p['imageUrl']), price: _d(p['price'])),
    ],
    categories: [
      for (final c in _maps(j['categories']))
        if (c['id'] != null) (id: '${c['id']}', name: _s(c['name']) ?? ''),
    ],
  );
}

/// `GET /api/me/referral?organizationId=`.
class ReferralInfo {
  const ReferralInfo({
    required this.code,
    required this.enabled,
    required this.referrerBonus,
    required this.refereeBonus,
    required this.invitedCount,
    required this.rewardedCount,
    this.shareText,
  });

  final String code;
  final bool enabled;
  final double referrerBonus;
  final double refereeBonus;
  final int invitedCount;
  final int rewardedCount;
  final String? shareText;

  factory ReferralInfo.fromJson(Map<String, dynamic> j) => ReferralInfo(
    code: _s(j['code']) ?? '',
    enabled: j['enabled'] == true,
    referrerBonus: _d(j['referrerBonus']),
    refereeBonus: _d(j['refereeBonus']),
    invitedCount: (j['invitedCount'] as num?)?.toInt() ?? 0,
    rewardedCount: (j['rewardedCount'] as num?)?.toInt() ?? 0,
    shareText: _s(j['shareText']),
  );
}

/// Ortga sanash bo'laklari: kun/soat yoki soat/daqiqa yoki daqiqa. Tugagan bo'lsa — null.
({int days, int hours, int minutes})? countdownParts(Duration left) {
  if (left.inSeconds <= 0) return null;
  // Daqiqa yuqoriga yaxlitlanadi ("0 daq" chiqmasligi uchun).
  final totalMinutes = (left.inSeconds + 59) ~/ 60;
  return (days: totalMinutes ~/ (24 * 60), hours: (totalMinutes ~/ 60) % 24, minutes: totalMinutes % 60);
}
