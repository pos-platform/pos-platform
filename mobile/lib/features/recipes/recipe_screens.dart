import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/circle_icon_button.dart';
import '../../l10n/app_localizations.dart';
import '../cart/cart_controller.dart';
import '../catalog/store_selection.dart';
import '../catalog/widgets/common.dart';
import 'data/recipes_data.dart';

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §1): retseptlar ro'yxati, retsept sahifasi va bosh sahifa banneri.

/// Bosh sahifadagi to'q banner: "Bugun nima pishiramiz?" + birinchi retsept; bir nechta bo'lsa — "Hammasi".
class RecipeHomeBanner extends StatelessWidget {
  const RecipeHomeBanner({super.key, required this.recipes});

  final List<RecipeBrief> recipes;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final r = recipes.first;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 26, 16, 0),
      child: Material(
        color: AppColors.ink,
        borderRadius: BorderRadius.circular(28),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => context.push(recipes.length > 1 ? '/home/recipes' : '/home/recipe/${r.id}'),
          child: SizedBox(
            height: 176,
            child: Stack(children: [
              if (r.imageUrl != null)
                Positioned(
                  right: -40,
                  top: -20,
                  width: 230,
                  height: 230,
                  child: ShaderMask(
                    shaderCallback: (rect) => const RadialGradient(
                      colors: [Colors.black, Colors.black, Colors.transparent],
                      stops: [0, 0.55, 1],
                    ).createShader(rect),
                    blendMode: BlendMode.dstIn,
                    child: ProductImage(url: r.imageUrl, fit: BoxFit.cover),
                  ),
                ),
              Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(t.recipeToday.toUpperCase(),
                        style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.7,
                            color: Colors.white.withValues(alpha: 0.6))),
                    ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 200),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(r.title,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            style: AppTheme.display(22, color: Colors.white).copyWith(height: 1.15)),
                        const SizedBox(height: 10),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                          decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(14)),
                          child: Text(t.recipeOneTap(r.itemCount),
                              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: Colors.white)),
                        ),
                      ]),
                    ),
                  ],
                ),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}

/// Barcha retseptlar (2 ustunli rangli kartalar).
class RecipesScreen extends ConsumerWidget {
  const RecipesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final branchId = ref.watch(storeSelectionProvider).value;
    final async = branchId == null ? null : ref.watch(recipesProvider(branchId));
    return Scaffold(
      appBar: AppBar(title: Text(t.recipesTitle)),
      body: switch (async) {
        AsyncData(:final value) => GridView.builder(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 10,
              crossAxisSpacing: 10,
              childAspectRatio: 0.82,
            ),
            itemCount: value.length,
            itemBuilder: (_, i) => _RecipeCard(recipe: value[i]),
          ),
        AsyncError(:final error) => ErrorRetry(
            message: ApiException.from(error).message,
            onRetry: () => ref.invalidate(recipesProvider(branchId!)),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _RecipeCard extends StatelessWidget {
  const _RecipeCard({required this.recipe});

  final RecipeBrief recipe;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final meta = [
      if (recipe.cookMinutes != null) t.recipeMinutes(recipe.cookMinutes!),
      t.recipeOneTap(recipe.itemCount).split('·').first.trim(),
    ].join(' · ');
    return Material(
      color: AppColors.tintFor(recipe.id),
      borderRadius: BorderRadius.circular(26),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/home/recipe/${recipe.id}'),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(8, 8, 8, 12),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Expanded(
              child: ClipRRect(
                borderRadius: BorderRadius.circular(20),
                child: ProductImage(url: recipe.imageUrl, fit: BoxFit.cover),
              ),
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 6),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(recipe.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w800, height: 1.25)),
                const SizedBox(height: 2),
                Text(meta,
                    style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.text.withValues(alpha: 0.5))),
              ]),
            ),
          ]),
        ),
      ),
    );
  }
}

/// Retsept sahifasi: rasm, vaqt/porsiya, ingrediyentlar (belgilash), "Hammasini savatga", qadamlar.
class RecipeScreen extends ConsumerWidget {
  const RecipeScreen({super.key, required this.recipeId});

  final String recipeId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final branchId = ref.watch(storeSelectionProvider).value;
    if (branchId == null) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    final args = (branchId: branchId, id: recipeId);
    return Scaffold(
      body: switch (ref.watch(recipeProvider(args))) {
        AsyncData(:final value) => _RecipeBody(branchId: branchId, recipe: value),
        AsyncError(:final error) => SafeArea(
            child: Column(children: [
              const Align(alignment: Alignment.centerLeft, child: BackButton()),
              Expanded(
                child: ErrorRetry(
                  message: ApiException.from(error).message,
                  onRetry: () => ref.invalidate(recipeProvider(args)),
                ),
              ),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _RecipeBody extends ConsumerStatefulWidget {
  const _RecipeBody({required this.branchId, required this.recipe});

  final String branchId;
  final Recipe recipe;

  @override
  ConsumerState<_RecipeBody> createState() => _RecipeBodyState();
}

class _RecipeBodyState extends ConsumerState<_RecipeBody> {
  late final Set<String> _selected = {
    for (final i in widget.recipe.items)
      if (i.available && i.product != null) i.productId,
  };

  List<RecipeItem> get _chosen =>
      widget.recipe.items.where((i) => _selected.contains(i.productId) && i.product != null).toList();

  void _addAll() {
    final t = AppLocalizations.of(context);
    final n = ref
        .read(cartControllerProvider.notifier)
        .addMany(widget.branchId, [for (final i in _chosen) (i.product!, i.quantity)]);
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t.cartAddedCount(n))));
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final r = widget.recipe;
    final top = MediaQuery.paddingOf(context).top;
    final chosen = _chosen;
    final sum = chosen.fold<double>(0, (s, i) => s + i.product!.price * i.quantity);

    return Column(children: [
      Expanded(
        child: CustomScrollView(slivers: [
          SliverToBoxAdapter(
            child: Container(
              height: 300 + top,
              color: AppColors.tintFor(r.id),
              child: Stack(children: [
                Positioned.fill(child: ProductImage(url: r.imageUrl, iconSize: 64, fit: BoxFit.cover)),
                Positioned(
                  left: 16,
                  top: top + 6,
                  child: CircleIconButton(
                    icon: Icons.arrow_back,
                    tooltip: t.catBack,
                    background: Colors.white.withValues(alpha: 0.85),
                    onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
                  ),
                ),
              ]),
            ),
          ),
          SliverToBoxAdapter(
            child: Transform.translate(
              offset: const Offset(0, -22),
              child: Container(
                decoration: const BoxDecoration(
                  color: AppColors.background,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(26)),
                ),
                padding: const EdgeInsets.fromLTRB(20, 22, 20, 0),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(r.title, style: AppTheme.display(22).copyWith(height: 1.2)),
                  if (r.cookMinutes != null || r.servings != null) ...[
                    const SizedBox(height: 10),
                    Wrap(spacing: 8, children: [
                      if (r.cookMinutes != null) _Meta(icon: Icons.schedule, text: t.recipeMinutes(r.cookMinutes!)),
                      if (r.servings != null) _Meta(icon: Icons.group_outlined, text: t.recipeServings(r.servings!)),
                    ]),
                  ],
                  if (r.description != null && r.description!.trim().isNotEmpty) ...[
                    const SizedBox(height: 14),
                    Text(r.description!,
                        style: const TextStyle(fontSize: 15, height: 1.6, fontWeight: FontWeight.w500, color: AppColors.textMuted)),
                  ],
                  const SizedBox(height: 20),
                  Text(t.recipeIngredients, style: AppTheme.display(17)),
                  const SizedBox(height: 10),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                    decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(AppTheme.radiusCard)),
                    child: Column(children: [
                      for (final (i, item) in r.items.indexed) ...[
                        if (i > 0) const Divider(height: 1, indent: 10, endIndent: 10, color: AppColors.line),
                        _IngredientRow(
                          item: item,
                          selected: _selected.contains(item.productId),
                          onChanged: item.available && item.product != null
                              ? (v) => setState(() => v ? _selected.add(item.productId) : _selected.remove(item.productId))
                              : null,
                        ),
                      ],
                    ]),
                  ),
                  if (r.steps.isNotEmpty) ...[
                    const SizedBox(height: 22),
                    Text(t.recipeSteps, style: AppTheme.display(17)),
                    const SizedBox(height: 10),
                    for (final (i, step) in r.steps.indexed)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Container(
                            width: 28,
                            height: 28,
                            alignment: Alignment.center,
                            decoration: BoxDecoration(color: AppColors.ink, borderRadius: BorderRadius.circular(10)),
                            child: Text('${i + 1}', style: AppTheme.display(12, color: Colors.white)),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(step,
                                style: const TextStyle(fontSize: 15, height: 1.5, fontWeight: FontWeight.w500)),
                          ),
                        ]),
                      ),
                  ],
                  const SizedBox(height: 8),
                ]),
              ),
            ),
          ),
        ]),
      ),
      DecoratedBox(
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.borderSoft)),
        ),
        child: SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            child: FilledButton(
              onPressed: chosen.isEmpty ? null : _addAll,
              child: FittedBox(
                fit: BoxFit.scaleDown,
                child: Text(chosen.isEmpty
                    ? t.recipeAddAll(0)
                    : '${t.recipeAddAll(chosen.length)} · ${formatMoney(context, sum)}'),
              ),
            ),
          ),
        ),
      ),
    ]);
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.fromLTRB(8, 5, 12, 5),
        decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(20)),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(icon, size: 16, color: AppColors.primary),
          const SizedBox(width: 5),
          Text(text, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
        ]),
      );
}

class _IngredientRow extends StatelessWidget {
  const _IngredientRow({required this.item, required this.selected, required this.onChanged});

  final RecipeItem item;
  final bool selected;
  final ValueChanged<bool>? onChanged;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final p = item.product;
    final enabled = onChanged != null;
    final qty = formatQty(context, item.quantity, p?.isKg ?? false);
    final sub = [qty, ?item.note].join(' · ');
    return InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: enabled ? () => onChanged!(!selected) : null,
      child: Opacity(
        opacity: enabled ? 1 : 0.5,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
          child: Row(children: [
            Checkbox(
              value: enabled && selected,
              onChanged: enabled ? (v) => onChanged!(v ?? false) : null,
              activeColor: AppColors.primary,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
            ),
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(color: AppColors.tintFor(item.productId), borderRadius: BorderRadius.circular(14)),
              clipBehavior: Clip.antiAlias,
              child: ProductImage(url: p?.imageUrl, iconSize: 18, fit: BoxFit.cover),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(item.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                Text(enabled ? sub : t.recipeUnavailable,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.faint)),
              ]),
            ),
            if (p != null && enabled)
              Text(formatMoney(context, p.price * item.quantity),
                  style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800)),
          ]),
        ),
      ),
    );
  }
}
