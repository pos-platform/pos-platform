import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import 'data/marketing_models.dart';

/// Bosh sahifaga "shu kategoriyani och" so'rovi (banner / story / push havolasi). Bosh sahifa tinglaydi va
/// filtrni qo'llagach [clear] qiladi.
class HomeCategoryRequest extends Notifier<String?> {
  @override
  String? build() => null;

  void request(String categoryId) => state = categoryId;

  void clear() => state = null;
}

final homeCategoryRequestProvider = NotifierProvider<HomeCategoryRequest, String?>(HomeCategoryRequest.new);

/// Havolani ochadi (kelishuv 9.1): Product → mahsulot, Category → bosh sahifa filtri, Collection → kolleksiya,
/// Discounts → aksiya sahifasi, Url → tashqi brauzer (faqat http/https).
/// [push] — true bo'lsa joriy stek ustiga (ekran ichidan), aks holda `go` (push bildirishnomadan).
Future<void> openShopLink(GoRouter router, HomeCategoryRequest categories, ShopLink link, {bool push = true}) async {
  if (!link.isActionable) return;
  void nav(String loc) => push ? router.push(loc) : router.go(loc);
  final v = link.value ?? '';
  switch (link.type) {
    case ShopLinkType.product:
      nav('/home/product/$v');
    case ShopLinkType.collection:
      nav('/home/collection/$v');
    case ShopLinkType.discounts:
      nav('/home/discounts');
    case ShopLinkType.category:
      categories.request(v);
      router.go('/home');
    case ShopLinkType.url:
      final uri = Uri.tryParse(v);
      if (uri == null || !(uri.scheme == 'https' || uri.scheme == 'http')) return;
      try {
        await launchUrl(uri, mode: LaunchMode.externalApplication);
      } catch (_) {}
    case ShopLinkType.none:
      return;
  }
}

/// Ekran ichidan havola ochish.
Future<void> openShopLinkFrom(GoRouter router, WidgetRef ref, ShopLink link) =>
    openShopLink(router, ref.read(homeCategoryRequestProvider.notifier), link);
