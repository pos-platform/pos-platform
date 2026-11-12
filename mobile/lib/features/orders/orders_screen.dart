import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/config.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import 'data/order_models.dart';
import 'data/orders_repository.dart';
import 'order_updates.dart';
import 'reorder.dart';
import 'widgets/order_format.dart';

/// "Buyurtmalarim" (dizayn: Orders.dc.html) — Faol / Tarix tablari.
class OrdersScreen extends ConsumerStatefulWidget {
  const OrdersScreen({super.key});

  @override
  ConsumerState<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends ConsumerState<OrdersScreen> {
  bool _active = true;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final signedIn = ref.watch(authControllerProvider.select((s) => s.isSignedIn));

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(t.ordersTitle, style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800)),
              const SizedBox(height: 16),
              if (!signedIn)
                const Expanded(child: _SignInPrompt())
              else ...[
                _Tabs(active: _active, onChanged: (v) => setState(() => _active = v)),
                const SizedBox(height: 16),
                Expanded(child: _OrdersList(key: ValueKey(_active), active: _active)),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _SignInPrompt extends ConsumerWidget {
  const _SignInPrompt();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.receipt_long_outlined, size: 56, color: AppColors.iconMuted),
          const SizedBox(height: 12),
          Text(t.ordersSignInPrompt,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: () => ref.read(authControllerProvider.notifier).requestSignIn(),
            child: Text(t.login),
          ),
        ],
      ),
    );
  }
}

/// Segment tugmalari: "Faol (n)" / "Tarix".
class _Tabs extends ConsumerWidget {
  const _Tabs({required this.active, required this.onChanged});

  final bool active;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final activeCount = ref.watch(ordersListProvider(true)).value?.totalCount;
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(color: const Color(0xFFE9E7E0), borderRadius: BorderRadius.circular(14)),
      child: Row(
        children: [
          _tab(activeCount == null || activeCount == 0 ? t.ordersTabActive : t.ordersTabActiveCount('$activeCount'),
              active, () => onChanged(true)),
          _tab(t.ordersTabHistory, !active, () => onChanged(false)),
        ],
      ),
    );
  }

  Widget _tab(String label, bool selected, VoidCallback onTap) => Expanded(
        child: Semantics(
          selected: selected,
          button: true,
          child: GestureDetector(
            behavior: HitTestBehavior.opaque,
            onTap: onTap,
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              height: 44,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? AppColors.surface : Colors.transparent,
                borderRadius: BorderRadius.circular(11),
                boxShadow: selected
                    ? const [BoxShadow(color: Color(0x14000000), blurRadius: 3, offset: Offset(0, 1))]
                    : null,
              ),
              child: Text(label,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: selected ? FontWeight.w800 : FontWeight.w700,
                    color: selected ? AppColors.text : AppColors.textMuted,
                  )),
            ),
          ),
        ),
      );
}

class _OrdersList extends ConsumerWidget {
  const _OrdersList({super.key, required this.active});

  final bool active;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    ref.watch(orderUpdatesProvider); // jonli yangilanish (SignalR)
    final async = ref.watch(ordersListProvider(active));

    Future<void> refresh() => ref.refresh(ordersListProvider(active).future);

    return async.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => _ErrorView(message: '$e', onRetry: () => ref.invalidate(ordersListProvider(active))),
      data: (page) {
        if (page.items.isEmpty) {
          return RefreshIndicator(
            onRefresh: refresh,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                const SizedBox(height: 80),
                const Icon(Icons.receipt_long_outlined, size: 56, color: AppColors.iconMuted),
                const SizedBox(height: 12),
                Text(active ? t.ordersEmptyActive : t.ordersEmptyHistory,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
              ],
            ),
          );
        }
        return RefreshIndicator(
          onRefresh: refresh,
          child: NotificationListener<ScrollNotification>(
            onNotification: (n) {
              if (n.metrics.extentAfter < 300) ref.read(ordersListProvider(active).notifier).loadMore();
              return false;
            },
            child: ListView.separated(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.only(bottom: 24),
              itemCount: page.items.length + (page.hasMore ? 1 : 0),
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (context, i) {
                if (i >= page.items.length) {
                  return const Padding(
                    padding: EdgeInsets.all(16),
                    child: Center(child: CircularProgressIndicator()),
                  );
                }
                return _OrderCard(order: page.items[i]);
              },
            ),
          ),
        );
      },
    );
  }
}

class _OrderCard extends ConsumerStatefulWidget {
  const _OrderCard({required this.order});

  final OrderSummary order;

  @override
  ConsumerState<_OrderCard> createState() => _OrderCardState();
}

class _OrderCardState extends ConsumerState<_OrderCard> {
  bool _reordering = false;

  OrderSummary get order => widget.order;

  Future<void> _reorder() async {
    if (_reordering) return;
    setState(() => _reordering = true);
    try {
      await reorderToCart(context, ref, order.id);
    } finally {
      if (mounted) setState(() => _reordering = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final images = order.previewImages.take(3).toList();
    final extra = order.itemCount - images.length;
    void open() => context.push('/orders/${order.id}');

    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: open,
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: AppColors.borderSoft),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('#${order.number}', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                        const SizedBox(height: 2),
                        Text(
                          '${OrderFormat.dateTime(context, t, order.createdAt)} · '
                          '${OrderFormat.fulfillment(t, order.fulfillmentType)}',
                          style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 8),
                  OrderStatusPill(status: order.status, fulfillment: order.fulfillmentType),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  for (var i = 0; i < images.length; i++) ...[
                    OrderThumb(url: AppConfig.imageUrl(images[i]), index: i),
                    const SizedBox(width: 6),
                  ],
                  Container(
                    height: 40,
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                    alignment: Alignment.center,
                    decoration: BoxDecoration(color: AppColors.background, borderRadius: BorderRadius.circular(10)),
                    child: Text(
                      images.isEmpty || extra <= 0 ? t.ordersItemKinds('${order.itemCount}') : '+$extra',
                      style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.textMuted),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: Text(OrderFormat.money(t, order.totalAmount),
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                  ),
                  if (order.isActive)
                    _SmallButton(label: t.ordersTrack, primary: true, onTap: open)
                  else if (!order.status.isFailed)
                    _SmallButton(
                      label: t.ordersReorder,
                      primary: false,
                      busy: _reordering,
                      onTap: _reorder,
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SmallButton extends StatelessWidget {
  const _SmallButton({required this.label, required this.primary, required this.onTap, this.busy = false});

  final String label;
  final bool primary;
  final bool busy;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: primary ? AppColors.primary : AppColors.primarySoft,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Container(
          height: 44,
          padding: const EdgeInsets.symmetric(horizontal: 16),
          alignment: Alignment.center,
          child: busy
              ? SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2, color: primary ? Colors.white : AppColors.primary),
                )
              : Text(label,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                    color: primary ? Colors.white : AppColors.primary,
                  )),
        ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
          const SizedBox(height: 12),
          TextButton(onPressed: onRetry, child: Text(t.retry)),
        ],
      ),
    );
  }
}
