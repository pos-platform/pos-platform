import 'dart:async';
import 'dart:math' as math;
import 'dart:ui' as ui;

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
import '../../core/widgets/circle_icon_button.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import '../profile/profile_screen.dart' show showLanguageSheet;
import '../cart/delivery_time.dart';
import '../marketing/data/marketing_repository.dart';
import '../marketing/home_sections.dart';
import '../marketing/search.dart';
import '../marketing/shop_link.dart';
import '../marketing/voice_search.dart';
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
  final _searchFocus = FocusNode();
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
    _searchFocus.addListener(() => setState(() {}));
    // Banner / story / push havolasi "kategoriyani och" desa — filtr qo'llanadi.
    ref.listenManual(homeCategoryRequestProvider, (_, id) {
      if (id == null) return;
      Future.microtask(() {
        ref.read(homeCategoryRequestProvider.notifier).clear();
        if (!mounted) return;
        _search.clear();
        _categoryId = id;
        _reset();
        if (_scroll.hasClients) _scroll.jumpTo(0);
      });
    }, fireImmediately: true);
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _scroll.dispose();
    _search.dispose();
    _searchFocus.dispose();
    super.dispose();
  }

  /// Qidiruvni bajaradi va tarixga yozadi.
  void _runSearch(String q) {
    _debounce?.cancel();
    if (_search.text != q) _search.text = q;
    _searchFocus.unfocus();
    ref.read(searchHistoryProvider.notifier).add(q);
    _reset();
  }

  /// Shtrix-kod: topilgan kod mavjud `search` ga beriladi; bitta natija bo'lsa — mahsulot ochiladi.
  Future<void> _scan() async {
    final code = await Navigator.of(context, rootNavigator: true)
        .push<String>(MaterialPageRoute(builder: (_) => const BarcodeScannerScreen()));
    if (code == null || !mounted) return;
    try {
      final r = await ref.read(catalogRepositoryProvider).products(_branchId, search: code, pageSize: 2);
      if (!mounted) return;
      if (r.items.length == 1) {
        context.push('/home/product/${r.items.first.id}');
        return;
      }
    } catch (_) {}
    if (mounted) _runSearch(code);
  }

  /// Ovozli qidiruv (CONTRACT-11 §3): eshitilgan matn oddiy qidiruvga beriladi.
  Future<void> _voice() async {
    final text = await showVoiceSearch(context);
    if (text != null && mounted) _runSearch(text);
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
    // Ikki darajali daraxt: asosiy bo'limlar va tanlangan asosiy bo'limning ichki bo'limlari.
    final roots = categories.where((c) => c.isRoot).toList();
    final selected = categories.where((c) => c.id == _categoryId).firstOrNull;
    final rootId = selected == null ? null : (selected.parentId ?? selected.id);
    final children = rootId == null ? const <ShopCategory>[] : categories.where((c) => c.parentId == rootId).toList();
    final cart = ref.watch(cartControllerProvider);
    final showCartBar = !cart.isEmpty && cart.branchId == _branchId;
    // Qidiruv yoki kategoriya tanlanmagan bo'lsa — marketing bloklari (bannerlar, Top 10, kolleksiyalar...).
    final showSections = _search.text.trim().isEmpty && _categoryId == null;

    return Stack(children: [
      RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(categoriesProvider(_branchId));
          ref.invalidate(storeProvider(_branchId));
          ref.invalidate(shopHomeProvider(_branchId));
          ref.invalidate(lastDeliveredOrderProvider(_branchId));
          _reset();
        },
        child: CustomScrollView(
          controller: _scroll,
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
              sliver: SliverList.list(children: [
                _StoreHeader(store: store),
                const SizedBox(height: 16),
                _Greeting(store: store),
                const SizedBox(height: 16),
                TextField(
                  controller: _search,
                  focusNode: _searchFocus,
                  textInputAction: TextInputAction.search,
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w500),
                  decoration: InputDecoration(
                    hintText: t.catSearchHint,
                    filled: true,
                    fillColor: AppColors.surface,
                    contentPadding: const EdgeInsets.symmetric(vertical: 16),
                    border: const OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(26)), borderSide: BorderSide.none),
                    enabledBorder: const OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(26)), borderSide: BorderSide.none),
                    focusedBorder: const OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(26)), borderSide: BorderSide.none),
                    prefixIcon: const Padding(
                      padding: EdgeInsets.only(left: 8),
                      child: Icon(Icons.search, size: 21, color: AppColors.textSecondary),
                    ),
                    suffixIcon: Padding(
                      padding: const EdgeInsets.only(right: 6),
                      child: _search.text.isEmpty
                          ? Row(mainAxisSize: MainAxisSize.min, children: [
                              IconButton(
                                tooltip: t.scanTitle,
                                icon: const Icon(Icons.qr_code_scanner, size: 21, color: AppColors.textSecondary),
                                onPressed: _scan,
                              ),
                              CircleIconButton(
                                icon: Icons.mic_rounded,
                                tooltip: t.voiceSearch,
                                size: 40,
                                iconSize: 20,
                                background: AppColors.ink,
                                foreground: Colors.white,
                                onPressed: _voice,
                              ),
                            ])
                          : CircleIconButton(
                              icon: Icons.close,
                              tooltip: t.catClear,
                              size: 40,
                              iconSize: 20,
                              background: AppColors.chipSurface,
                              onPressed: () {
                                _search.clear();
                                _reset();
                              },
                            ),
                    ),
                    suffixIconConstraints: const BoxConstraints(minWidth: 46, minHeight: 40),
                  ),
                  onChanged: (_) {
                    setState(() {});
                    _debounce?.cancel();
                    _debounce = Timer(const Duration(milliseconds: 400), _reset);
                  },
                  onSubmitted: _runSearch,
                ),
                if (_searchFocus.hasFocus)
                  SearchSuggestionsPanel(
                    branchId: _branchId,
                    query: _search.text,
                    onQuery: _runSearch,
                    onProduct: (id) {
                      ref.read(searchHistoryProvider.notifier).add(_search.text);
                      _searchFocus.unfocus();
                      context.push('/home/product/$id');
                    },
                    onCategory: (id, _) {
                      _searchFocus.unfocus();
                      _search.clear();
                      _categoryId = id;
                      _reset();
                    },
                  ),
                const SizedBox(height: 22),
              ]),
            ),
            if (showSections)
              SliverToBoxAdapter(
                child: HomeMarketingSections(
                  branchId: _branchId,
                  fallbackBanner:
                      store.deliveryEnabled && store.freeDeliveryFrom != null ? _PromoBanner(store: store) : null,
                  middle: roots.isEmpty
                      ? null
                      : _CategoryTiles(
                          categories: roots,
                          onTap: (id) {
                            _categoryId = id;
                            _reset();
                            if (_scroll.hasClients) _scroll.jumpTo(0);
                          },
                        ),
                ),
              ),
            if (showSections) const SliverToBoxAdapter(child: SizedBox(height: 16)),
            SliverToBoxAdapter(
              child: SizedBox(
                height: 38,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 16),
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
                    for (final c in roots)
                      _CategoryChip(
                        label: c.name,
                        selected: rootId == c.id,
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
            // Ichki bo'limlar: asosiy bo'lim tanlanganda uning ostida ikkinchi qator.
            if (children.isNotEmpty)
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: SizedBox(
                    height: 38,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      children: [
                        _CategoryChip(
                          label: t.catAll,
                          selected: _categoryId == rootId,
                          onTap: () {
                            if (_categoryId == rootId) return;
                            _categoryId = rootId;
                            _reset();
                          },
                        ),
                        for (final c in children)
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
              ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 16, 4, 8),
              sliver: SliverToBoxAdapter(
                child: Row(children: [
                  Expanded(child: Text(_titleFor(t, categories), style: AppTheme.display(18))),
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
              padding: const EdgeInsets.symmetric(horizontal: 16),
              sliver: SliverGrid.builder(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  mainAxisSpacing: 10,
                  crossAxisSpacing: 10,
                  mainAxisExtent: 262,
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

/// Yuqori qator (dizayn v3): logo, do'kon (bosilsa — almashtirish), til, sevimlilar.
class _StoreHeader extends ConsumerWidget {
  const _StoreHeader({required this.store});

  final ShopStore store;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    // Do'kon rejimida filial bitta bo'lsa — almashtirish yo'q (marketplace do'konlari ko'rsatilmaydi).
    final canSwitch = !(ref.watch(storeAppProvider).value?.singleBranch ?? false);
    // Yopiq va keyingi ochilish ma'lum bo'lsa: "Yopiq · 09:00 da ochiladi" (ish vaqti o'rniga).
    final opensAt = store.isOpenNow ? null : store.nextOpenTime;
    final status = [
      if (!store.isOpenNow) t.catClosedNow,
      if (opensAt != null) t.catOpensAt(opensAt) else ?store.hoursText,
    ].join(' · ');
    return Row(children: [
      ClipRRect(
        borderRadius: BorderRadius.circular(14),
        // Do'kon rejimida (yoki logo yuklanmagan bo'lsa) — ilova ichidagi brend logosi.
        child: SizedBox(
          width: 42,
          height: 42,
          child: AppConfig.isStoreMode || (store.logoUrl ?? '').isEmpty
              ? const ColoredBox(color: AppColors.chipSurface, child: Padding(padding: EdgeInsets.all(4), child: StoreLogo(size: 34)))
              : ProductImage(url: store.logoUrl, iconSize: 20, fit: BoxFit.cover),
        ),
      ),
      const SizedBox(width: 10),
      Expanded(
        child: Semantics(
          button: canSwitch,
          label: canSwitch ? t.catChangeStore : null,
          child: InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: canSwitch ? () => context.push('/home/stores') : null,
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: 44),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (status.isNotEmpty)
                    Text(status,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: store.isOpenNow ? AppColors.textSecondary : AppColors.primary)),
                  Row(children: [
                    Flexible(
                      // Do'kon rejimida filial nomi o'rniga brend yozuvi (Xo‘ja market).
                      child: AppConfig.isStoreMode
                          ? const BrandWordmark(fontSize: 16)
                          : Text(store.name,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                    ),
                    if (canSwitch) const Icon(Icons.expand_more, size: 20, color: AppColors.textSecondary),
                  ]),
                ],
              ),
            ),
          ),
        ),
      ),
      const _LanguageButton(),
      const SizedBox(width: 8),
      CircleIconButton(
        icon: Icons.favorite_border,
        tooltip: t.favTitle,
        iconSize: 21,
        onPressed: () => context.push('/home/favorites'),
      ),
    ]);
  }
}

/// Til almashtirish (UZ / RU) — kirmasdan ham bosh sahifadan. Oq doira, ichida til kodi.
class _LanguageButton extends ConsumerWidget {
  const _LanguageButton();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.watch(localeControllerProvider);
    final code = Localizations.localeOf(context).languageCode.toUpperCase();
    return Tooltip(
      message: AppLocalizations.of(context).profileLanguage,
      child: Material(
        color: AppColors.surface,
        shape: const CircleBorder(),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => showLanguageSheet(context, ref),
          child: SizedBox(
            width: 44,
            height: 44,
            child: Center(child: Text(code, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800))),
          ),
        ),
      ),
    );
  }
}

/// Salomlashish (Unbounded) va yetkazish vaqti belgisi: "⚡ 30–45 daqiqa ichida yetkazamiz".
class _Greeting extends ConsumerWidget {
  const _Greeting({required this.store});

  final ShopStore store;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final name = ref.watch(authControllerProvider.select((a) => a.user?.firstName))?.trim();
    final time = deliveryTimeText(t, store);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(
        name == null || name.isEmpty ? t.homeWelcome : t.homeGreeting(name),
        style: AppTheme.display(25).copyWith(height: 1.15),
      ),
      if (time != null) ...[
        const SizedBox(height: 6),
        Wrap(crossAxisAlignment: WrapCrossAlignment.center, spacing: 8, children: [
          Container(
            padding: const EdgeInsets.fromLTRB(7, 4, 10, 4),
            decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(20)),
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Icon(Icons.bolt, size: 16, color: AppColors.primary),
              const SizedBox(width: 4),
              Text(time, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
            ]),
          ),
          Text(t.homeDeliverIn,
              style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
        ]),
      ],
    ]);
  }
}

/// Bo'lim plitkalari (dizayn v3): rangli kartalar, 3 ustunli to'r — qatorlar navbat bilan 2+1 / 1+2.
class _CategoryTiles extends StatefulWidget {
  const _CategoryTiles({required this.categories, required this.onTap});

  final List<ShopCategory> categories;
  final ValueChanged<String> onTap;

  @override
  State<_CategoryTiles> createState() => _CategoryTilesState();
}

/// Ko'p mahsulotli bo'limlar oldin; dastlab [_collapsed] tasi, "Hammasi (N)" bosilsa — qolganlari ham.
class _CategoryTilesState extends State<_CategoryTiles> {
  static const _collapsed = 6;
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final sorted = [...widget.categories]..sort((a, b) => b.productCount.compareTo(a.productCount));
    final categories = _expanded ? sorted : sorted.take(_collapsed).toList();
    final onTap = widget.onTap;
    final rows = <Widget>[];
    for (var i = 0, r = 0; i < categories.length; i += 2, r++) {
      final wideFirst = r.isEven;
      Widget tile(int k, int flex) => Expanded(
            flex: flex,
            child: _CategoryTile(
              category: categories[k],
              tint: AppColors.tints[k % AppColors.tints.length],
              onTap: () => onTap(categories[k].id),
            ),
          );
      final hasSecond = i + 1 < categories.length;
      rows.add(Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Row(children: [
          tile(i, hasSecond ? (wideFirst ? 2 : 1) : 1),
          if (hasSecond) ...[const SizedBox(width: 8), tile(i + 1, wideFirst ? 1 : 2)],
        ]),
      ));
    }
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 26, 16, 0),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          Expanded(child: Text(t.homeSections, style: AppTheme.display(18))),
          if (sorted.length > _collapsed)
            TextButton(
              style: TextButton.styleFrom(foregroundColor: AppColors.textSecondary),
              onPressed: () => setState(() => _expanded = !_expanded),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                Text(_expanded ? t.homeSectionsLess : '${t.mkAll} (${sorted.length})',
                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                const SizedBox(width: 2),
                Icon(_expanded ? Icons.expand_less : Icons.expand_more, size: 18),
              ]),
            ),
        ]),
        const SizedBox(height: 8),
        ...rows,
      ]),
    );
  }
}

class _CategoryTile extends StatelessWidget {
  const _CategoryTile({required this.category, required this.tint, required this.onTap});

  final ShopCategory category;
  final Color tint;
  final VoidCallback onTap;

  bool get hasImage => AppConfig.imageUrl(category.imageUrl) != null;

  @override
  Widget build(BuildContext context) => Material(
        color: tint,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: SizedBox(
            height: hasImage ? 124 : 104,
            child: Stack(
              children: [
                // Bo'lim rasmi (admin panel → Kategoriyalar) — butun plitka foni; rasm foni plitka rangini almashtiradi.
                if (hasImage)
                  Positioned.fill(child: _TileImage(url: AppConfig.imageUrl(category.imageUrl)!, fallback: tint)),
                Padding(
                  padding: EdgeInsets.fromLTRB(12, hasImage ? 10 : 14, 12, 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(category.name,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                              fontSize: hasImage ? 13.5 : 14,
                              fontWeight: hasImage ? FontWeight.w700 : FontWeight.w800,
                              height: 1.2)),
                      // Rasmli plitkada son ko'rsatilmaydi — rasm ustida yozuv chalkash bo'ladi.
                      if (!hasImage && category.productCount > 0)
                        Text(AppLocalizations.of(context).homeProductsCount(category.productCount),
                            style: TextStyle(
                                fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.text.withValues(alpha: 0.5))),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      );
}

/// Plitka rasmi: fon — rasm burchagidagi rang (rasm foni bilan uzluksiz), rasm o'zi pastki o'ng burchakda,
/// chap yuqorida nom uchun bo'sh joy qoladi.
class _TileImage extends StatefulWidget {
  const _TileImage({required this.url, required this.fallback});

  final String url;

  /// Rasm foni aniqlanguncha ishlatiladigan rang (plitka rangi).
  final Color fallback;

  /// Bir marta aniqlangan fon ranglari (url → rang).
  static final _bgCache = <String, Color>{};

  @override
  State<_TileImage> createState() => _TileImageState();
}

class _TileImageState extends State<_TileImage> {
  ImageStream? _stream;
  ImageStreamListener? _listener;

  Color? get _bg => _TileImage._bgCache[widget.url];

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_bg == null && _stream == null) {
      _stream = NetworkImage(widget.url).resolve(createLocalImageConfiguration(context));
      _listener = ImageStreamListener((info, _) => _sample(info.image), onError: (_, _) {});
      _stream!.addListener(_listener!);
    }
  }

  // Chap yuqori burchakdagi piksel — rasmning fon rangi.
  Future<void> _sample(ui.Image image) async {
    final data = await image.toByteData(format: ui.ImageByteFormat.rawRgba);
    if (data == null || !mounted) return;
    final i = (4 * image.width + 4) * 4;
    final color = Color.fromARGB(data.getUint8(i + 3), data.getUint8(i), data.getUint8(i + 1), data.getUint8(i + 2));
    setState(() => _TileImage._bgCache[widget.url] = color);
  }

  @override
  void dispose() {
    if (_listener != null) _stream?.removeListener(_listener!);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => LayoutBuilder(builder: (context, c) {
        // Rasm o'ng tomonda to'liq balandlikda (cover); chap cheti va tepasi fon rangiga silliq o'tadi —
        // nom rasm ustida emas, "bo'sh" fonda o'qiladi.
        final bg = _bg ?? widget.fallback;
        final w = math.min(c.maxWidth, c.maxHeight * 1.25);
        final wide = w < c.maxWidth;
        return ColoredBox(
          color: bg,
          child: Stack(clipBehavior: Clip.hardEdge, children: [
            Positioned(
              right: 0,
              top: 0,
              bottom: 0,
              width: w,
              child: Image.network(widget.url,
                  fit: BoxFit.cover,
                  alignment: Alignment.bottomRight,
                  errorBuilder: (_, _, _) => const SizedBox.shrink()),
            ),
            if (wide)
              Positioned(
                right: w * 0.6,
                top: 0,
                bottom: 0,
                width: w * 0.4,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(colors: [bg, bg.withValues(alpha: 0)]),
                  ),
                ),
              ),
            Positioned.fill(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [bg.withValues(alpha: 0.95), bg.withValues(alpha: 0.6), bg.withValues(alpha: 0)],
                    stops: const [0, 0.3, 0.6],
                  ),
                ),
              ),
            ),
          ]),
        );
      });
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
      decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(AppTheme.radiusCard)),
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
        Icon(Icons.local_shipping_outlined, size: 56, color: Colors.white.withValues(alpha: 0.85)),
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
        padding: const EdgeInsets.only(right: 6),
        child: Material(
          color: selected ? AppColors.ink : AppColors.surface,
          shape: const StadiumBorder(),
          child: InkWell(
            customBorder: const StadiumBorder(),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 15),
              child: Center(
                child: Text(label,
                    style: TextStyle(
                        fontSize: 14, fontWeight: FontWeight.w700, color: selected ? Colors.white : AppColors.text)),
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
