import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/config.dart';
import '../../core/locale/locale_controller.dart';
import '../../core/store_app/store_app.dart';
import '../../core/store_app/store_branding.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../bonus/bonus_card_screen.dart';
import '../cart/cart_controller.dart';
import '../profile/data/bonus_repository.dart';
import '../profile/profile_screen.dart' show showLanguageSheet;
import '../cart/delivery_time.dart';
import 'data/catalog_models.dart';
import 'data/catalog_repository.dart';
import 'store_selection.dart';
import 'stores_screen.dart';
import 'widgets/common.dart';
import 'widgets/product_card.dart';

/// Bosh sahifa (dizayn: Main.dc.html): do'kon, qidiruv, kategoriyalar, mahsulotlar (cheksiz aylantirish).
/// Do'kon tanlanmagan bo'lsa — do'konlar ro'yxati.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final store = ref.watch(selectedStoreProvider);
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: switch (store) {
          AsyncData(value: final s?) => _Catalog(key: ValueKey(s.id), store: s),
          AsyncData() => StoreList(
              header: Padding(
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 4),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(AppConfig.isStoreMode ? t.storeChooseBranch : t.catChooseStore, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800)),
                ),
              ),
            ),
          AsyncError(:final error) => Column(mainAxisAlignment: MainAxisAlignment.center, children: [
              ErrorRetry(
                message: ApiException.from(error).message,
                onRetry: () => ref.invalidate(selectedStoreProvider),
              ),
              // Do'kon rejimida filial bitta bo'lsa — boshqa tanlov yo'q.
              if (!(ref.watch(storeAppProvider).value?.singleBranch ?? false))
                TextButton(
                  onPressed: () => ref.read(storeSelectionProvider.notifier).clear(),
                  child: Text(t.catChooseAnotherStore),
                ),
            ]),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }
}

class _Catalog extends ConsumerStatefulWidget {
  const _Catalog({super.key, required this.store});

  final ShopStore store;

  @override
  ConsumerState<_Catalog> createState() => _CatalogState();
}

class _CatalogState extends ConsumerState<_Catalog> {
  static const _pageSize = 30;

  final _scroll = ScrollController();
  final _search = TextEditingController();
  Timer? _debounce;

  String? _categoryId;
  ProductSort _sort = ProductSort.popular;
  final _items = <ShopProduct>[];
  int _page = 0;
  bool _hasMore = true;
  bool _loading = false;
  String? _error;
  int _generation = 0; // eski so'rov javobini tashlab yuborish uchun

  String get _branchId => widget.store.id;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.extentAfter < 600) _loadMore();
    });
    Future.microtask(_loadMore); // initState ichida setState chaqirmaslik uchun
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _scroll.dispose();
    _search.dispose();
    super.dispose();
  }

  void _reset() {
    _generation++;
    setState(() {
      _items.clear();
      _page = 0;
      _hasMore = true;
      _loading = false;
      _error = null;
    });
    _loadMore();
  }

  Future<void> _loadMore() async {
    if (!mounted || _loading || !_hasMore) return;
    final gen = _generation;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final r = await ref.read(catalogRepositoryProvider).products(
            _branchId,
            search: _search.text,
            categoryId: _categoryId,
            sort: _sort,
            page: _page + 1,
            pageSize: _pageSize,
          );
      if (!mounted || gen != _generation) return;
      setState(() {
        _items.addAll(r.items);
        _page = r.page;
        _hasMore = r.hasMore && r.items.isNotEmpty;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (!mounted || gen != _generation) return;
      setState(() {
        _loading = false;
        _error = e.message;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final store = widget.store;
    final categories = ref.watch(categoriesProvider(_branchId)).value ?? const <ShopCategory>[];
    final cart = ref.watch(cartControllerProvider);
    final showCartBar = !cart.isEmpty && cart.branchId == _branchId;

    return Stack(children: [
      RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(categoriesProvider(_branchId));
          ref.invalidate(storeProvider(_branchId));
          _reset();
        },
        child: CustomScrollView(
          controller: _scroll,
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
              sliver: SliverList.list(children: [
                _StoreHeader(store: store),
                const SizedBox(height: 14),
                TextField(
                  controller: _search,
                  textInputAction: TextInputAction.search,
                  decoration: InputDecoration(
                    hintText: t.catSearchHint,
                    prefixIcon: const Icon(Icons.search, color: AppColors.textSecondary),
                    suffixIcon: _search.text.isEmpty
                        ? null
                        : IconButton(
                            tooltip: t.catClear,
                            icon: const Icon(Icons.close, color: AppColors.textSecondary),
                            onPressed: () {
                              _search.clear();
                              _reset();
                            },
                          ),
                  ),
                  onChanged: (_) {
                    setState(() {});
                    _debounce?.cancel();
                    _debounce = Timer(const Duration(milliseconds: 400), _reset);
                  },
                  onSubmitted: (_) {
                    _debounce?.cancel();
                    _reset();
                  },
                ),
                const SizedBox(height: 14),
                if (store.deliveryEnabled && store.freeDeliveryFrom != null) ...[
                  _PromoBanner(store: store),
                  const SizedBox(height: 14),
                ],
              ]),
            ),
            SliverToBoxAdapter(
              child: SizedBox(
                height: 40,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  children: [
                    _CategoryChip(
                      label: t.catAll,
                      selected: _categoryId == null,
                      onTap: () {
                        if (_categoryId == null) return;
                        _categoryId = null;
                        _reset();
                      },
                    ),
                    for (final c in categories)
                      _CategoryChip(
                        label: c.name,
                        selected: _categoryId == c.id,
                        onTap: () {
                          if (_categoryId == c.id) return;
                          _categoryId = c.id;
                          _reset();
                        },
                      ),
                  ],
                ),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(20, 16, 8, 8),
              sliver: SliverToBoxAdapter(
                child: Row(children: [
                  Expanded(
                    child: Text(_titleFor(t, categories),
                        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                  ),
                  PopupMenuButton<ProductSort>(
                    tooltip: t.catSort,
                    initialValue: _sort,
                    onSelected: (s) {
                      _sort = s;
                      _reset();
                    },
                    itemBuilder: (_) => [
                      for (final s in ProductSort.values) PopupMenuItem(value: s, child: Text(_sortLabel(t, s))),
                    ],
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      child: Row(mainAxisSize: MainAxisSize.min, children: [
                        Text(_sortLabel(t, _sort),
                            style: TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                        const SizedBox(width: 4),
                        Icon(Icons.swap_vert, size: 20, color: AppColors.primary),
                      ]),
                    ),
                  ),
                ]),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              sliver: SliverGrid.builder(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  mainAxisSpacing: 12,
                  crossAxisSpacing: 12,
                  mainAxisExtent: 268,
                ),
                itemCount: _items.length,
                itemBuilder: (_, i) => ProductCard(branchId: _branchId, product: _items[i]),
              ),
            ),
            SliverToBoxAdapter(child: _footer(t)),
            SliverToBoxAdapter(child: SizedBox(height: showCartBar ? 96 : 24)),
          ],
        ),
      ),
      if (showCartBar)
        Positioned(
          left: 16,
          right: 16,
          bottom: 16,
          child: _CartBar(count: cart.count, total: cart.estimatedTotal),
        ),
    ]);
  }

  Widget _footer(AppLocalizations t) {
    if (_error != null) {
      return Padding(
        padding: const EdgeInsets.only(top: 16),
        child: ErrorRetry(message: _error!, onRetry: _loadMore),
      );
    }
    if (_loading) {
      return const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()));
    }
    if (_items.isEmpty) {
      return Padding(
        padding: const EdgeInsets.all(32),
        child: Text(
          _search.text.isEmpty ? t.catNoProducts : t.catNothingFound,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.textSecondary, fontSize: 15),
        ),
      );
    }
    return const SizedBox.shrink();
  }

  String _titleFor(AppLocalizations t, List<ShopCategory> categories) {
    if (_search.text.trim().isNotEmpty) return t.catSearchResults;
    if (_categoryId != null) {
      for (final c in categories) {
        if (c.id == _categoryId) return c.name;
      }
    }
    return t.catPopular;
  }

  static String _sortLabel(AppLocalizations t, ProductSort s) => switch (s) {
        ProductSort.popular => t.catSortPopular,
        ProductSort.name => t.catSortName,
        ProductSort.priceAsc => t.catSortPriceAsc,
        ProductSort.priceDesc => t.catSortPriceDesc,
      };
}

/// Yuqori qator: do'kon nomi (bosilsa — do'kon almashtirish) va holati.
class _StoreHeader extends ConsumerWidget {
  const _StoreHeader({required this.store});

  final ShopStore store;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    // Do'kon rejimida filial bitta bo'lsa — almashtirish yo'q (marketplace do'konlari ko'rsatilmaydi).
    final canSwitch = !(ref.watch(storeAppProvider).value?.singleBranch ?? false);
    final status = [
      store.isOpenNow ? t.catOpenNow : t.catClosedNow,
      ?store.hoursText,
      ?deliveryTimeText(t, store),
    ].join(' · ');
    return Semantics(
      button: canSwitch,
      label: canSwitch ? t.catChangeStore : null,
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: canSwitch ? () => context.push('/home/stores') : null,
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 44),
          child: Row(children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              // Do'kon rejimida (yoki logo yuklanmagan bo'lsa) — ilova ichidagi brend logosi.
              child: SizedBox(
                width: 44,
                height: 44,
                child: AppConfig.isStoreMode || (store.logoUrl ?? '').isEmpty
                    ? const ColoredBox(color: Colors.white, child: Padding(padding: EdgeInsets.all(4), child: StoreLogo(size: 36)))
                    : ProductImage(url: store.logoUrl, iconSize: 20, fit: BoxFit.cover),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(status,
                    style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                        color: store.isOpenNow ? AppColors.primary : AppColors.textSecondary)),
                Row(children: [
                  Flexible(
                    // Do'kon rejimida filial nomi o'rniga brend yozuvi (Xo‘ja market).
                    child: AppConfig.isStoreMode
                        ? const BrandWordmark(fontSize: 18)
                        : Text(store.name,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                  ),
                  if (canSwitch) const Icon(Icons.keyboard_arrow_down, size: 20),
                ]),
              ]),
            ),
            const _LanguageButton(),
            _BonusChip(branchId: store.id),
          ]),
        ),
      ),
    );
  }
}

/// Til almashtirish (UZ / RU) — kirmasdan ham bosh sahifadan.
class _LanguageButton extends ConsumerWidget {
  const _LanguageButton();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.watch(localeControllerProvider);
    final code = Localizations.localeOf(context).languageCode.toUpperCase();
    return Semantics(
      button: true,
      label: AppLocalizations.of(context).profileLanguage,
      child: Padding(
        padding: const EdgeInsets.only(right: 8),
        child: InkWell(
          borderRadius: BorderRadius.circular(20),
          onTap: () => showLanguageSheet(context, ref),
          child: Container(
            height: 36,
            padding: const EdgeInsets.symmetric(horizontal: 10),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.textSecondary.withValues(alpha: 0.25)),
            ),
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.language, size: 18),
              const SizedBox(width: 4),
              Text(code, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
            ]),
          ),
        ),
      ),
    );
  }
}

/// Sarlavhadagi kichik bonus belgisi (faqat kirgan foydalanuvchi) — bonus kartani ochadi.
class _BonusChip extends ConsumerWidget {
  const _BonusChip({required this.branchId});

  final String branchId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!ref.watch(authControllerProvider.select((a) => a.isSignedIn))) return const SizedBox.shrink();
    final t = AppLocalizations.of(context);
    final points = ref.watch(bonusBalanceProvider(branchId)).value;
    return Padding(
      padding: const EdgeInsets.only(left: 8),
      child: ActionChip(
        avatar: Icon(Icons.qr_code_2, size: 18, color: AppColors.primary),
        label: Text(points == null ? t.bonusTitle : formatBonusPoints(t, points)),
        labelStyle: TextStyle(fontSize: 13, fontWeight: FontWeight.w800, color: AppColors.primary),
        backgroundColor: AppColors.primarySoft,
        side: BorderSide.none,
        onPressed: () => context.go('/bonus'),
      ),
    );
  }
}

/// Yashil banner: bepul yetkazish chegarasi. (Yetkazish vaqti "60–90 daqiqa" — do'kon sarlavhasida.)
class _PromoBanner extends StatelessWidget {
  const _PromoBanner({required this.store});

  final ShopStore store;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 18),
      decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(20)),
      child: Row(children: [
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(t.catDeliveryBannerTitle,
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Colors.white, height: 1.25)),
            const SizedBox(height: 6),
            Text(t.catFreeDeliveryFrom(formatMoney(context, store.freeDeliveryFrom!)),
                style: TextStyle(fontSize: 13, color: AppColors.onPrimarySoft)),
          ]),
        ),
        const SizedBox(width: 12),
        const Icon(Icons.local_shipping_outlined, size: 56, color: Color(0xFF9FD3B5)),
      ]),
    );
  }
}

class _CategoryChip extends StatelessWidget {
  const _CategoryChip({required this.label, required this.selected, required this.onTap});

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(right: 8),
        child: Material(
          color: selected ? AppColors.text : AppColors.surface,
          shape: StadiumBorder(side: BorderSide(color: selected ? AppColors.text : AppColors.border)),
          child: InkWell(
            customBorder: const StadiumBorder(),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Center(
                child: Text(label,
                    style: TextStyle(
                        fontSize: 14, fontWeight: FontWeight.w600, color: selected ? Colors.white : AppColors.text)),
              ),
            ),
          ),
        ),
      );
}

/// Pastdagi qora "Savatcha" paneli (taxminiy summa; aniq summa savatchada serverdan).
class _CartBar extends StatelessWidget {
  const _CartBar({required this.count, required this.total});

  final int count;
  final double total;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Material(
      color: AppColors.text,
      elevation: 8,
      shadowColor: const Color(0x4016201B),
      borderRadius: BorderRadius.circular(AppTheme.radiusInput),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppTheme.radiusInput),
        onTap: () => context.go('/cart'),
        child: SizedBox(
          height: AppTheme.controlHeight,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 18),
            child: Row(children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(8)),
                child: Text('$count', style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w700)),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(t.navCart,
                    style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700)),
              ),
              Text(formatMoney(context, total),
                  style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w800)),
            ]),
          ),
        ),
      ),
    );
  }
}
