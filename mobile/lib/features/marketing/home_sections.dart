import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../catalog/widgets/common.dart';
import '../orders/data/order_models.dart';
import '../orders/data/orders_repository.dart';
import '../orders/reorder.dart';
import 'data/marketing_models.dart';
import 'data/marketing_repository.dart';
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
  const HomeMarketingSections({super.key, required this.branchId, this.fallbackBanner});

  final String branchId;
  final Widget? fallbackBanner;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final home = ref.watch(shopHomeProvider(branchId)).value ?? const ShopHome();
    final lastOrder = ref.watch(lastDeliveredOrderProvider(branchId)).value;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (home.stories.isNotEmpty) ...[StoriesRow(stories: home.stories), const SizedBox(height: 10)],
        if (home.banners.isNotEmpty) ...[
          BannerCarousel(banners: home.banners),
          const SizedBox(height: 14),
        ] else if (fallbackBanner != null) ...[
          Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: fallbackBanner),
          const SizedBox(height: 14),
        ],
        if (home.discountedCount > 0 || lastOrder != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 4),
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
          RailHeader(title: t.mkBuyAgain),
          ProductRail(branchId: branchId, products: home.buyAgain),
        ],
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
