import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import '../catalog/data/catalog_models.dart';
import '../catalog/widgets/common.dart';
import '../orders/data/order_models.dart';
import '../orders/data/orders_repository.dart';
import '../orders/reorder.dart';
import 'data/marketing_models.dart';
import 'data/marketing_repository.dart';
import '../recipes/recipe_screens.dart';
import 'stories.dart';
import 'widgets/banner_carousel.dart';
import 'widgets/countdown.dart';
import 'widgets/product_rail.dart';

/// Shu filialdagi oxirgi yetkazilgan buyurtma ("Oxirgi buyurtmani takrorlash" kartasi uchun). Kirmagan — null.
final lastDeliveredOrderProvider = FutureProvider.autoDispose.family<OrderSummary?, String>((ref, branchId) async {
  if (!ref.watch(authControllerProvider.select((a) => a.isSignedIn))) return null;
  try {
    final page = await ref.watch(ordersRepositoryProvider).list(active: false, pageSize: 10);
    return page.items.where((o) => o.status == OrderStatus.delivered && o.branchId == branchId).firstOrNull;
  } catch (_) {
    return null; // karta ixtiyoriy
  }
});

/// Bosh sahifaning yuqori bloklari (kelishuv 9.1–9.4): stories, banner karuseli (bo'lmasa — [fallbackBanner]),
/// "Aksiyalar (N)", oxirgi buyurtmani takrorlash, "Yana olasizmi?", Top 10, kolleksiyalar.
/// `home` yuklanmagan yoki xato bo'lsa — faqat [fallbackBanner] (katalog odatdagidek ishlaydi).
class HomeMarketingSections extends ConsumerWidget {
  const HomeMarketingSections({super.key, required this.branchId, this.fallbackBanner, this.middle});

  final String branchId;
  final Widget? fallbackBanner;

  /// "Yana olasizmi?" va retseptdan keyin, Top 10 dan oldin (dizayn v3: bo'lim plitkalari).
  final Widget? middle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final home = ref.watch(shopHomeProvider(branchId)).value ?? const ShopHome();
    final lastOrder = ref.watch(lastDeliveredOrderProvider(branchId)).value;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (home.stories.isNotEmpty) ...[StoriesRow(stories: home.stories), const SizedBox(height: 16)],
        if (home.banners.isNotEmpty) ...[
          BannerCarousel(banners: home.banners),
          const SizedBox(height: 14),
        ] else if (fallbackBanner != null) ...[
          Padding(padding: const EdgeInsets.symmetric(horizontal: 16), child: fallbackBanner),
          const SizedBox(height: 14),
        ],
        if (home.discountedCount > 0 || lastOrder != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
            child: Column(
              children: [
                if (home.discountedCount > 0)
                  _EntryTile(
                    icon: Icons.local_offer_outlined,
                    iconColor: AppColors.accent,
                    iconBackground: const Color(0xFFFDF0E6),
                    title: t.mkDiscountsEntry(home.discountedCount),
                    subtitle: t.mkDiscountsEntryHint,
                    onTap: () => context.push('/home/discounts'),
                  ),
                if (home.discountedCount > 0 && lastOrder != null) const SizedBox(height: 10),
                if (lastOrder != null) _ReorderCard(order: lastOrder),
              ],
            ),
          ),
        if (home.buyAgain.isNotEmpty) ...[
          RailHeader(title: t.mkBuyAgain, hint: t.homeBuyAgainHint, padding: const EdgeInsets.fromLTRB(16, 26, 16, 12)),
          _BuyAgainRail(branchId: branchId, products: home.buyAgain),
        ],
        if (home.recipes.isNotEmpty) RecipeHomeBanner(recipes: home.recipes),
        ?middle,
        if (home.topProducts.isNotEmpty) ...[
          RailHeader(title: t.mkTop10),
          ProductRail(branchId: branchId, products: home.topProducts.take(10).toList(), ranked: true),
        ],
        for (final c in home.collections) ...[
          RailHeader(
            title: c.title,
            trailing: c.endsAt == null
                ? null
                : CountdownChip(endsAt: c.endsAt!, compact: true, label: (t, time) => time),
            onAll: () => context.push('/home/collection/${c.id}'),
          ),
          ProductRail(branchId: branchId, products: c.products),
        ],
      ],
    );
  }
}

class _EntryTile extends StatelessWidget {
  const _EntryTile({
    required this.icon,
    required this.iconColor,
    required this.iconBackground,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final Color iconColor;
  final Color iconBackground;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: AppColors.surface,
    borderRadius: BorderRadius.circular(AppTheme.radiusCard),
    child: InkWell(
      borderRadius: BorderRadius.circular(AppTheme.radiusCard),
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(AppTheme.radiusCard),
          border: Border.all(color: AppColors.borderSoft),
        ),
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(color: iconBackground, borderRadius: BorderRadius.circular(12)),
              child: Icon(icon, color: iconColor),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  ),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, color: AppColors.iconMuted),
          ],
        ),
      ),
    ),
  );
}

/// "Oxirgi buyurtmani takrorlash": rasmlar, summa va tugma (mavjud `reorderToCart`).
class _ReorderCard extends ConsumerStatefulWidget {
  const _ReorderCard({required this.order});

  final OrderSummary order;

  @override
  ConsumerState<_ReorderCard> createState() => _ReorderCardState();
}

class _ReorderCardState extends ConsumerState<_ReorderCard> {
  bool _busy = false;

  Future<void> _reorder() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      await reorderToCart(context, ref, widget.order.id);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final o = widget.order;
    final images = o.previewImages.take(3).toList();
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.circular(AppTheme.radiusCard)),
      child: Row(
        children: [
          SizedBox(
            width: 44.0 + (images.length > 1 ? (images.length - 1) * 18 : 0),
            height: 44,
            child: Stack(
              children: [
                for (var i = 0; i < images.length; i++)
                  Positioned(
                    left: i * 18.0,
                    child: Container(
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: AppColors.surface, width: 2),
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(10),
                        child: ProductImage(url: images[i], iconSize: 16, fit: BoxFit.cover),
                      ),
                    ),
                  ),
                if (images.isEmpty) Icon(Icons.replay, size: 32, color: AppColors.primary),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(t.mkReorderTitle, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
                Text(
                  t.mkReorderSubtitle('${o.number}', formatMoney(context, o.totalAmount)),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 13, color: AppColors.textMuted),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          FilledButton(
            style: FilledButton.styleFrom(
              minimumSize: const Size(0, 40),
              padding: const EdgeInsets.symmetric(horizontal: 14),
            ),
            onPressed: _busy ? null : _reorder,
            child: _busy
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                  )
                : Text(t.mkReorderButton),
          ),
        ],
      ),
    );
  }
}

/// "Yana olasizmi?" (dizayn v3): kichik kvadrat plitkalar — rangli fon, rasm, burchakda "+" yoki miqdor;
/// ostida narx (Unbounded) va nom.
class _BuyAgainRail extends StatelessWidget {
  const _BuyAgainRail({required this.branchId, required this.products});

  final String branchId;
  final List<ShopProduct> products;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 160,
    child: ListView.separated(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: products.length,
      separatorBuilder: (_, _) => const SizedBox(width: 10),
      itemBuilder: (_, i) => _BuyAgainTile(
        branchId: branchId,
        product: products[i],
        tint: AppColors.tints[i % AppColors.tints.length],
      ),
    ),
  );
}

class _BuyAgainTile extends ConsumerWidget {
  const _BuyAgainTile({required this.branchId, required this.product, required this.tint});

  final String branchId;
  final ShopProduct product;
  final Color tint;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final p = product;
    final qty = ref.watch(cartControllerProvider.select((c) => c.branchId == branchId ? c.quantityOf(p.id) : 0.0));
    final canAdd = p.inStock && CartState.firstQuantity(p) <= p.available + 1e-9;
    return SizedBox(
      width: 108,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 108,
            height: 108,
            child: Stack(
              children: [
                Positioned.fill(
                  child: Material(
                    color: tint,
                    borderRadius: BorderRadius.circular(AppTheme.radiusCard),
                    clipBehavior: Clip.antiAlias,
                    child: InkWell(
                      onTap: () => context.push('/home/product/${p.id}'),
                      child: ProductImage(url: p.imageUrl, fit: BoxFit.cover),
                    ),
                  ),
                ),
                Positioned(
                  right: 6,
                  bottom: 6,
                  child: qty > 0
                      ? Container(
                          constraints: const BoxConstraints(minWidth: 34),
                          height: 34,
                          padding: const EdgeInsets.symmetric(horizontal: 10),
                          alignment: Alignment.center,
                          decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(17)),
                          child: Text(
                            formatQty(context, qty, false),
                            style: const TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w800),
                          ),
                        )
                      : Semantics(
                          button: true,
                          label: t.catAddToCart,
                          child: Material(
                            color: AppColors.surface,
                            shape: const CircleBorder(),
                            elevation: 2,
                            shadowColor: AppColors.ink.withValues(alpha: 0.3),
                            child: InkWell(
                              customBorder: const CircleBorder(),
                              onTap: canAdd
                                  ? () => ref.read(cartControllerProvider.notifier).increment(branchId, p)
                                  : null,
                              child: SizedBox(
                                width: 34,
                                height: 34,
                                child: Icon(Icons.add, size: 20, color: canAdd ? AppColors.text : AppColors.faint),
                              ),
                            ),
                          ),
                        ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 7),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 2),
            child: Text(
              formatMoney(context, p.price),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: AppTheme.display(13),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 2),
            child: Text(
              p.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.textSecondary),
            ),
          ),
        ],
      ),
    );
  }
}
