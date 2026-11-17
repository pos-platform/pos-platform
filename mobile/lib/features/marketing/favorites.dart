import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../catalog/data/catalog_models.dart';
import 'data/marketing_repository.dart';

/// Sevimli mahsulotlar ID'lari (`GET /api/me/favorites/ids`) — yurakchalarni tez bo'yash uchun kesh.
/// Kirmagan bo'lsa bo'sh. Bosilganda darhol o'zgaradi (optimistik), xatoda qaytariladi.
class FavoritesController extends Notifier<FavState> {
  int _generation = 0;

  @override
  FavState build() {
    final signedIn = ref.watch(authControllerProvider.select((a) => a.isSignedIn));
    if (signedIn) Future.microtask(reload);
    return const FavState();
  }

  Future<void> reload() async {
    // Ilova ochilishida tarmoq/token hali tayyor bo'lmasligi mumkin — bir necha marta qayta urinamiz.
    // Aks holda yurakchalar faqat mahsulotdagi `isFavorite`ga tayanib, "o'chib ketgandek" ko'rinardi.
    for (var attempt = 0; attempt < 4; attempt++) {
      if (attempt > 0) await Future<void>.delayed(Duration(seconds: 2 * attempt));
      final gen = ++_generation;
      try {
        if (!ref.read(authControllerProvider).isSignedIn) return;
        final ids = await ref.read(marketingRepositoryProvider).favoriteIds();
        if (gen == _generation) state = FavState(ids: ids, removed: state.removed, loaded: true);
        return;
      } catch (_) {
        // Notifier yopilgan yoki tarmoq xatosi — keyingi urinish (yopilgan bo'lsa ref.read yana xato beradi).
        if (gen != _generation) return; // foydalanuvchi bu orada bosdi — uning tanlovi ustun
      }
    }
  }

  bool isFavorite(ShopProduct p) => state.isFavorite(p);

  /// Qaytaradi: yangi qiymat yoki null (kirish so'raldi / xato).
  Future<bool?> toggle(BuildContext context, ShopProduct p) async {
    if (!ref.read(authControllerProvider).isSignedIn) {
      final t = AppLocalizations.of(context);
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: Text(t.favLoginTitle),
          content: Text(t.favLoginText),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.catCancel)),
            TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.login)),
          ],
        ),
      );
      if (ok == true) ref.read(authControllerProvider.notifier).requestSignIn();
      return null;
    }
    final messenger = ScaffoldMessenger.maybeOf(context);
    final value = !isFavorite(p);
    _set(p.id, value);
    try {
      await ref.read(marketingRepositoryProvider).setFavorite(p.id, value);
      ref.invalidate(favoriteProductsProvider);
      return value;
    } catch (e) {
      _set(p.id, !value);
      messenger?.showSnackBar(SnackBar(content: Text(ApiException.from(e).message)));
      return null;
    }
  }

  void _set(String id, bool value) {
    _generation++; // yo'ldagi reload natijasi optimistik qiymatni bosib ketmasin
    final ids = Set<String>.of(state.ids), removed = Set<String>.of(state.removed);
    if (value) {
      ids.add(id);
      removed.remove(id);
    } else {
      ids.remove(id);
      removed.add(id);
    }
    state = FavState(ids: ids, removed: removed, loaded: state.loaded);
  }
}

/// [ids] — sevimlilar; [removed] — kesh yuklanmasdan oldin olib tashlanganlar ([loaded] false bo'lsa
/// mahsulotdagi `isFavorite` ishlatiladi).
class FavState {
  const FavState({this.ids = const {}, this.removed = const {}, this.loaded = false});

  final Set<String> ids;
  final Set<String> removed;
  final bool loaded;

  bool isFavorite(ShopProduct p) {
    if (ids.contains(p.id)) return true;
    if (loaded || removed.contains(p.id)) return false;
    return p.isFavorite ?? false;
  }
}

final favoritesProvider = NotifierProvider<FavoritesController, FavState>(FavoritesController.new);

/// ♥ tugmasi (kartochka va mahsulot sahifasi).
class FavoriteButton extends ConsumerWidget {
  const FavoriteButton({super.key, required this.product, this.size = 20, this.boxed = true});

  final ShopProduct product;
  final double size;

  /// Oq dumaloq fon (rasm ustida).
  final bool boxed;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.watch(favoritesProvider);
    final ctrl = ref.read(favoritesProvider.notifier);
    final fav = ctrl.isFavorite(product);
    final t = AppLocalizations.of(context);
    final icon = Icon(
      fav ? Icons.favorite : Icons.favorite_border,
      size: size,
      color: fav ? AppColors.primary : AppColors.textSecondary,
    );
    return Semantics(
      button: true,
      toggled: fav,
      label: fav ? t.favRemove : t.favAdd,
      child: Material(
        color: boxed ? AppColors.surface.withValues(alpha: 0.92) : Colors.transparent,
        shape: const CircleBorder(),
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: () => ctrl.toggle(context, product),
          child: Padding(padding: EdgeInsets.all(boxed ? 6 : 10), child: icon),
        ),
      ),
    );
  }
}
