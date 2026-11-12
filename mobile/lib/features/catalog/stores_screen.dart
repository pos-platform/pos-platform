import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../cart/cart_controller.dart';
import 'data/catalog_models.dart';
import 'data/catalog_repository.dart';
import 'data/user_location.dart';
import 'store_selection.dart';
import 'widgets/common.dart';

/// Do'kon tanlash. Savatchada boshqa do'kon mahsuloti bo'lsa — tozalashni so'raydi.
/// Qaytaradi: tanlandimi.
Future<bool> selectStore(BuildContext context, WidgetRef ref, ShopStore store) async {
  final cart = ref.read(cartControllerProvider.notifier);
  if (cart.conflictsWith(store.id)) {
    final t = AppLocalizations.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(t.catSwitchStoreTitle),
        content: Text(t.catSwitchStoreBody),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.catCancel)),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.catSwitchStoreConfirm)),
        ],
      ),
    );
    if (ok != true) return false;
    cart.clear();
  }
  await ref.read(storeSelectionProvider.notifier).select(store.id);
  return true;
}

/// Do'konlar ro'yxati sahifasi (`/home/stores`).
class StoresScreen extends StatelessWidget {
  const StoresScreen({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: Text(AppLocalizations.of(context).catChooseStore)),
        body: StoreList(onSelected: () => context.canPop() ? context.pop() : context.go('/home')),
      );
}

/// Qidiruv + do'konlar ro'yxati (bosh sahifada do'kon tanlanmaganda ham shu ko'rsatiladi).
/// Joylashuvga ruxsat berilsa — server eng yaqin do'konlarni birinchi qaytaradi va masofa (`distanceKm`) ko'rsatiladi;
/// ruxsat berilmasa — odatiy tartib.
class StoreList extends ConsumerStatefulWidget {
  const StoreList({super.key, this.onSelected, this.header});

  final VoidCallback? onSelected;
  final Widget? header;

  @override
  ConsumerState<StoreList> createState() => _StoreListState();
}

class _StoreListState extends ConsumerState<StoreList> {
  final _search = TextEditingController();
  Timer? _debounce;
  late Future<List<ShopStore>> _future = _load();

  Future<List<ShopStore>> _load() async {
    final loc = await ref.read(userLocationProvider.future);
    return ref.read(catalogRepositoryProvider).stores(search: _search.text, lat: loc?.lat, lng: loc?.lng);
  }

  void _reload() => setState(() => _future = _load());

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final selectedId = ref.watch(storeSelectionProvider).value;
    return Column(children: [
      ?widget.header,
      Padding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
        child: TextField(
          controller: _search,
          textInputAction: TextInputAction.search,
          decoration: InputDecoration(
            hintText: t.catSearchStores,
            prefixIcon: const Icon(Icons.search, color: AppColors.textSecondary),
          ),
          onChanged: (_) {
            _debounce?.cancel();
            _debounce = Timer(const Duration(milliseconds: 400), _reload);
          },
        ),
      ),
      Expanded(
        child: FutureBuilder<List<ShopStore>>(
          future: _future,
          builder: (context, snap) {
            if (snap.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snap.hasError) {
              return ErrorRetry(message: ApiException.from(snap.error!).message, onRetry: _reload);
            }
            final stores = snap.data!;
            if (stores.isEmpty) {
              return Center(child: Text(t.catNoStores, style: const TextStyle(color: AppColors.textSecondary)));
            }
            return RefreshIndicator(
              onRefresh: () async {
                // Joylashuv ham yangilanadi (foydalanuvchi ko'chgan yoki ruxsatni endi bergan bo'lishi mumkin).
                ref.invalidate(userLocationProvider);
                _reload();
                await _future.catchError((_) => <ShopStore>[]);
              },
              child: ListView.separated(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
                itemCount: stores.length,
                separatorBuilder: (_, _) => const SizedBox(height: 10),
                itemBuilder: (_, i) => _StoreTile(
                  store: stores[i],
                  selected: stores[i].id == selectedId,
                  onTap: () async {
                    if (await selectStore(context, ref, stores[i])) widget.onSelected?.call();
                  },
                ),
              ),
            );
          },
        ),
      ),
    ]);
  }
}

class _StoreTile extends StatelessWidget {
  const _StoreTile({required this.store, required this.selected, required this.onTap});

  final ShopStore store;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final info = <String>[
      if (store.branchName.isNotEmpty && store.branchName != store.name) store.branchName,
      if (store.distanceKm != null) t.catDistanceKm(store.distanceKm!.toStringAsFixed(1)),
      if (store.hoursText != null) store.hoursText!,
    ];
    return Material(
      color: AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        side: BorderSide(color: selected ? AppColors.primary : AppColors.borderSoft, width: selected ? 2 : 1),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(14),
              child: SizedBox(width: 56, height: 56, child: ProductImage(url: store.logoUrl, iconSize: 24, fit: BoxFit.cover)),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(store.name, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                if (info.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(info.join(' · '), style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                ],
                if (store.address != null) ...[
                  const SizedBox(height: 2),
                  Text(store.address!,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                ],
                const SizedBox(height: 6),
                _OpenBadge(open: store.isOpenNow),
              ]),
            ),
            if (selected) const Icon(Icons.check_circle, color: AppColors.primary),
          ]),
        ),
      ),
    );
  }
}

class _OpenBadge extends StatelessWidget {
  const _OpenBadge({required this.open});

  final bool open;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
      decoration: BoxDecoration(
        color: open ? AppColors.primarySoft : AppColors.chipSurface,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(open ? t.catOpenNow : t.catClosedNow,
          style: TextStyle(
              fontSize: 12, fontWeight: FontWeight.w700, color: open ? AppColors.primary : AppColors.textSecondary)),
    );
  }
}
