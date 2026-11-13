import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/features/bonus/data/bonus_card_repository.dart';
import 'package:poscloud_market/features/catalog/data/catalog_models.dart';
import 'package:poscloud_market/features/marketing/data/marketing_models.dart';
import 'package:poscloud_market/features/orders/data/order_models.dart';

/// Bosqich 9 modellari: maydonlar yo'q yoki noto'g'ri bo'lsa ham ilova yiqilmasligi kerak.
void main() {
  Map<String, dynamic> product(String id, {Object? rank, Object? endsAt}) => {
        'id': id,
        'name': 'Sut $id',
        'price': 12000,
        'available': 5,
        'inStock': true,
        'rank': ?rank,
        'discountEndsAt': ?endsAt,
      };

  test('home: bannerlar, Top 10, kolleksiyalar, stories o‘qiladi; bo‘shlari tashlanadi', () {
    final h = ShopHome.fromJson({
      'banners': [
        {'id': 'b1', 'title': 'Aksiya', 'linkType': 'Collection', 'linkValue': 'c1', 'backgroundColor': '#FF0000'},
        {'id': 'b2', 'title': 'Sayt', 'linkType': 'url', 'linkValue': 'https://x.uz'},
        {'id': 'b3', 'title': 'Noma’lum', 'linkType': 'Whatever'},
      ],
      'topProducts': [product('p1', rank: 1), product('p2', rank: 2), 'yaroqsiz'],
      'collections': [
        {'id': 'c1', 'title': 'Yangi', 'endsAt': '2030-01-01T00:00:00', 'products': [product('p3')]},
        {'id': 'c2', 'title': 'Bo‘sh', 'products': []},
      ],
      'discountedCount': 7,
      'stories': [
        {
          'id': 's1',
          'title': 'Hikoya',
          'slides': [
            {'id': 'sl1', 'imageUrl': '/u/1.jpg', 'durationSec': 99, 'linkType': 'Discounts'},
          ],
        },
        {'id': 's2', 'title': 'Slaydsiz', 'slides': []},
      ],
    });
    expect(h.banners.map((b) => b.link.type),
        [ShopLinkType.collection, ShopLinkType.url, ShopLinkType.none]);
    expect(h.banners[2].link.isActionable, isFalse);
    expect(h.topProducts.map((p) => p.rank), [1, 2]);
    expect(h.collections.single.endsAt, isNotNull);
    expect(h.discountedCount, 7);
    expect(h.buyAgain, isEmpty);
    expect(h.stories.single.slides.single.durationSec, 15); // 3–15 oralig'iga qisiladi
    expect(h.stories.single.slides.single.link.isActionable, isTrue);
  });

  test('home: bo‘sh javob', () {
    final h = ShopHome.fromJson(const {});
    expect(h.banners, isEmpty);
    expect(h.discountedCount, 0);
  });

  test('mahsulot: discountEndsAt UTC sifatida o‘qiladi, isFavorite ixtiyoriy', () {
    final p = ShopProduct.fromJson(product('p1', endsAt: '2030-05-01T10:00:00'));
    expect(p.discountEndsAt!.toUtc(), DateTime.utc(2030, 5, 1, 10));
    expect(p.isFavorite, isNull);
    expect(ShopProduct.fromJson({...product('p2'), 'isFavorite': true}).isFavorite, isTrue);
  });

  test('ortga sanash bo‘laklari', () {
    expect(countdownParts(Duration.zero), isNull);
    expect(countdownParts(const Duration(days: 2, hours: 4, minutes: 10)), (days: 2, hours: 4, minutes: 10));
    expect(countdownParts(const Duration(hours: 3, minutes: 5)), (days: 0, hours: 3, minutes: 5));
    // Soniyalar daqiqaga yuqoriga yaxlitlanadi.
    expect(countdownParts(const Duration(seconds: 30)), (days: 0, hours: 0, minutes: 1));
  });

  test('qidiruv takliflari va referral', () {
    final s = SearchSuggest.fromJson({
      'products': [
        {'id': 'p1', 'name': 'Kola', 'price': 9000},
      ],
      'categories': [
        {'id': 'c1', 'name': 'Ichimliklar'},
      ],
    });
    expect(s.products.single.price, 9000);
    expect(s.categories.single.name, 'Ichimliklar');
    final r = ReferralInfo.fromJson({'code': 'AB12CD', 'enabled': true, 'referrerBonus': 5000, 'invitedCount': 3});
    expect(r.code, 'AB12CD');
    expect(r.rewardedCount, 0);
  });

  test('buyurtma bahosi va bonus darajasi', () {
    final o = OrderDetail.fromJson({
      'id': 'o1',
      'status': 'Delivered',
      'canRate': true,
      'ratingTags': ['fast', 'fresh'],
    });
    expect(o.canRate, isTrue);
    expect(o.rating, isNull);
    expect(o.ratingTags, ['fast', 'fresh']);

    final c = BonusCard.fromJson({
      'points': 100,
      'tier': {'name': 'Kumush', 'cashbackPercent': 3},
      'nextTier': {'name': 'Gold', 'minSpend': 1000000, 'remaining': 350000},
    });
    expect(c.tier!.cashbackPercent, 3);
    expect(c.nextTier!.remaining, 350000);
    expect(BonusCard.fromJson({'tier': null}).tier, isNull);
  });
}
