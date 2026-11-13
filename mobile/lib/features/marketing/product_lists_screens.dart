import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../catalog/data/catalog_models.dart';
import '../catalog/data/catalog_repository.dart';
import '../catalog/store_selection.dart';
import '../catalog/widgets/common.dart';
import '../catalog/widgets/product_card.dart';
import 'data/marketing_repository.dart';
import 'widgets/countdown.dart';

const _gridDelegate = SliverGridDelegateWithFixedCrossAxisCount(
  crossAxisCount: 2,
  mainAxisSpacing: 12,
  crossAxisSpacing: 12,
  mainAxisExtent: 268,
);

Widget _grid(String branchId, List<ShopProduct> items) => SliverPadding(
  padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
  sliver: SliverGrid.builder(
    gridDelegate: _gridDelegate,
    itemCount: items.length,
    itemBuilder: (_, i) => ProductCard(branchId: branchId, product: items[i]),
  ),
);

Widget _empty(String text, {IconData icon = Icons.inventory_2_outlined}) => SliverFillRemaining(
  hasScrollBody: false,
  child: Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 52, color: AppColors.iconMuted),
          const SizedBox(height: 12),
          Text(
            text,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 15, color: AppColors.textSecondary),
          ),
        ],
      ),
    ),
  ),
);

/// Kolleksiya sahifasi (`/home/collection/:id`): rasm/tavsif, `endsAt` bo'lsa ortga sanash, mahsulotlar setkasi.
class CollectionScreen extends ConsumerWidget {
  const CollectionScreen({super.key, required this.collectionId});

  final String collectionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final branchId = ref.watch(storeSelectionProvider).value;
    if (branchId == null) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    final args = (branchId: branchId, id: collectionId);
    final async = ref.watch(collectionProvider(args));
    final c = async.value;
    return Scaffold(
      appBar: AppBar(title: Text(c?.title ?? '')),
      body: switch (async) {
        AsyncValue(value: final c?) => RefreshIndicator(
          onRefresh: () => ref.refresh(collectionProvider(args).future),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(20, 4, 20, 4),
                sliver: SliverList.list(
                  children: [
                    if (c.imageUrl != null) ...[
                      ClipRRect(
                        borderRadius: BorderRadius.circular(20),
                        child: AspectRatio(
                          aspectRatio: 2.2,
                          child: ProductImage(url: c.imageUrl, fit: BoxFit.cover),
                        ),
                      ),
                      const SizedBox(height: 12),
                    ],
                    if (c.endsAt != null) ...[
                      Align(
                        alignment: Alignment.centerLeft,
                        child: CountdownChip(endsAt: c.endsAt!, label: (t, time) => t.mkEndsIn(time)),
                      ),
                      const SizedBox(height: 10),
                    ],
                    if (c.description != null)
                      Text(
                        c.description!,
                        style: const TextStyle(fontSize: 15, height: 1.5, color: AppColors.textMuted),
                      ),
                  ],
                ),
              ),
              if (c.products.isEmpty) _empty(t.catNoProducts) else _grid(branchId, c.products),
            ],
          ),
        ),
        AsyncError(:final error) => ErrorRetry(
          message: ApiException.from(error).message,
          onRetry: () => ref.invalidate(collectionProvider(args)),
        ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

/// Aksiya sahifasi (`/home/discounts`): `products?discounted=true`, cheksiz aylantirish.
class DiscountsScreen extends ConsumerStatefulWidget {
  const DiscountsScreen({super.key});

  @override
  ConsumerState<DiscountsScreen> createState() => _DiscountsScreenState();
}

class _DiscountsScreenState extends ConsumerState<DiscountsScreen> {
  final _scroll = ScrollController();
  final _items = <ShopProduct>[];
  int _page = 0;
  bool _hasMore = true;
  bool _loading = false;
  String? _error;
  String? _branchId;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.extentAfter < 600) _load();
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final branchId = _branchId;
    if (branchId == null || _loading || !_hasMore) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref
          .read(catalogRepositoryProvider)
          .products(branchId, discounted: true, page: _page + 1, pageSize: 30);
      if (!mounted || branchId != _branchId) return;
      setState(() {
        _items.addAll(r.items);
        _page = r.page;
        _hasMore = r.hasMore && r.items.isNotEmpty;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.message;
      });
    }
  }

  Future<void> _refresh() async {
    setState(() {
      _items.clear();
      _page = 0;
      _hasMore = true;
      _loading = false;
    });
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final branchId = ref.watch(storeSelectionProvider).value;
    if (branchId != null && branchId != _branchId) {
      _branchId = branchId;
      _items.clear();
      _page = 0;
      _hasMore = true;
      Future.microtask(_load);
    }
    return Scaffold(
      appBar: AppBar(title: Text(t.mkDiscountsTitle)),
      body: branchId == null
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _refresh,
              child: CustomScrollView(
                controller: _scroll,
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  if (_items.isEmpty && !_loading && _error == null && !_hasMore)
                    _empty(t.mkDiscountsEmpty, icon: Icons.local_offer_outlined)
                  else
                    _grid(branchId, _items),
                  SliverToBoxAdapter(
                    child: _error != null
                        ? ErrorRetry(message: _error!, onRetry: _load)
                        : _loading
                        ? const Padding(
                            padding: EdgeInsets.all(24),
                            child: Center(child: CircularProgressIndicator()),
                          )
                        : const SizedBox(height: 16),
                  ),
                ],
              ),
            ),
    );
  }
}

/// Profil → "Sevimlilar" (`/profile/favorites`): tanlangan filial narxi/qoldig'i bilan.
class FavoritesScreen extends ConsumerWidget {
  const FavoritesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final signedIn = ref.watch(authControllerProvider.select((a) => a.isSignedIn));
    final branchId = ref.watch(storeSelectionProvider).value;
    final Widget body;
    if (!signedIn || branchId == null) {
      body = CustomScrollView(slivers: [_empty(t.favEmpty, icon: Icons.favorite_border)]);
    } else {
      final async = ref.watch(favoriteProductsProvider(branchId));
      body = switch (async) {
        AsyncValue(value: final items?) => RefreshIndicator(
          onRefresh: () => ref.refresh(favoriteProductsProvider(branchId).future),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              if (items.isEmpty)
                SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.favorite_border, size: 52, color: AppColors.iconMuted),
                          const SizedBox(height: 12),
                          Text(t.favEmpty, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                          const SizedBox(height: 6),
                          Text(
                            t.favEmptyHint,
                            textAlign: TextAlign.center,
                            style: const TextStyle(color: AppColors.textSecondary),
                          ),
                          const SizedBox(height: 20),
                          FilledButton(onPressed: () => context.go('/home'), child: Text(t.cartGoShopping)),
                        ],
                      ),
                    ),
                  ),
                )
              else
                _grid(branchId, items),
            ],
          ),
        ),
        AsyncError(:final error) => ErrorRetry(
          message: ApiException.from(error).message,
          onRetry: () => ref.invalidate(favoriteProductsProvider(branchId)),
        ),
        _ => const Center(child: CircularProgressIndicator()),
      };
    }
    return Scaffold(
      appBar: AppBar(title: Text(t.favTitle)),
      body: body,
    );
  }
}
