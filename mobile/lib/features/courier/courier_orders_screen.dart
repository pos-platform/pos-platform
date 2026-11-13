import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import 'courier_navigation.dart';
import 'courier_updates.dart';
import 'data/courier_models.dart';
import 'data/courier_repository.dart';
import 'widgets/courier_widgets.dart';

/// Kuryer bosh ekrani: Faol (yo'ldagi) va Bugun (yakunlangan) buyurtmalar.
class CourierOrdersScreen extends ConsumerStatefulWidget {
  const CourierOrdersScreen({super.key});

  @override
  ConsumerState<CourierOrdersScreen> createState() => _CourierOrdersScreenState();
}

class _CourierOrdersScreenState extends ConsumerState<CourierOrdersScreen> {
  Timer? _poll;

  @override
  void initState() {
    super.initState();
    // SignalR ulanmasa ham ro'yxat yangilanib tursin.
    _poll = Timer.periodic(const Duration(seconds: 30), (_) {
      if (!mounted) return;
      ref.invalidate(courierOrdersProvider(false));
      ref.invalidate(courierOrdersProvider(true));
    });
  }

  @override
  void dispose() {
    _poll?.cancel();
    super.dispose();
  }

  Future<void> _routeAll() async {
    final t = AppLocalizations.of(context);
    final orders = ref.read(courierOrdersProvider(false)).value ?? const [];
    final loc = ref.read(courierLocationProvider).value;
    final ok = await CourierNavigation.routeAll(loc, orders);
    if (!ok && mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t.courierOpenAppError)));
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    ref.watch(courierUpdatesProvider);
    final active = ref.watch(courierOrdersProvider(false)).value ?? const [];

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(t.courierMode),
          actions: const [CourierProfileChip()],
          bottom: TabBar(
            labelColor: AppColors.primary,
            unselectedLabelColor: AppColors.textSecondary,
            indicatorColor: AppColors.primary,
            labelStyle: const TextStyle(fontWeight: FontWeight.w800, fontFamily: 'Manrope'),
            tabs: [Tab(text: t.courierTabActive), Tab(text: t.courierTabToday)],
          ),
        ),
        body: const TabBarView(children: [_OrdersList(history: false), _OrdersList(history: true)]),
        bottomNavigationBar: active.isEmpty
            ? null
            : SafeArea(
                minimum: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton.icon(
                  onPressed: _routeAll,
                  icon: const Icon(Icons.alt_route),
                  label: Text(t.courierRouteAll),
                ),
              ),
      ),
    );
  }
}

class _OrdersList extends ConsumerWidget {
  const _OrdersList({required this.history});

  final bool history;

  Future<void> _refresh(WidgetRef ref) async {
    ref.invalidate(courierLocationProvider);
    ref.invalidate(courierOrdersProvider(history));
    try {
      await ref.read(courierOrdersProvider(history).future);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(courierOrdersProvider(history));
    final loc = ref.watch(courierLocationProvider).value;

    return async.when(
      skipLoadingOnRefresh: true,
      skipLoadingOnReload: true,
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => CourierErrorView(message: e.toString(), onRetry: () => _refresh(ref)),
      data: (orders) {
        // Faollar — eng yaqini birinchi; tarix — server tartibida.
        final list = history ? orders : _byDistance(loc, orders);
        return RefreshIndicator(
          onRefresh: () => _refresh(ref),
          child: list.isEmpty
              ? ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    const SizedBox(height: 120),
                    const Icon(Icons.inventory_2_outlined, size: 56, color: AppColors.iconMuted),
                    const SizedBox(height: 12),
                    Text(history ? t.courierEmptyToday : t.courierEmptyActive,
                        textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
                  ],
                )
              : ListView.separated(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                  itemCount: list.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (_, i) => _OrderCard(
                    order: list[i],
                    distance: CourierNavigation.distance(loc, list[i]),
                    history: history,
                  ),
                ),
        );
      },
    );
  }

  static List<CourierOrder> _byDistance(({double lat, double lng})? loc, List<CourierOrder> orders) {
    if (loc == null) return orders;
    final sorted = [...orders];
    sorted.sort((a, b) {
      final da = CourierNavigation.distance(loc, a);
      final db = CourierNavigation.distance(loc, b);
      if (da == null && db == null) return 0;
      if (da == null) return 1;
      if (db == null) return -1;
      return da.compareTo(db);
    });
    return sorted;
  }
}

class _OrderCard extends StatelessWidget {
  const _OrderCard({required this.order, required this.distance, required this.history});

  final CourierOrder order;
  final double? distance;
  final bool history;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final details = CourierFormat.addressDetails(t, order);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/courier/order/${order.id}'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Text('#${order.number}', style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(width: 8),
              if (order.contactless)
                const Icon(Icons.do_not_touch_outlined, size: 18, color: AppColors.textSecondary),
              const Spacer(),
              if (history)
                _StatusPill(delivered: order.status == 'Delivered')
              else if (distance != null)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(color: AppColors.chipSurface, borderRadius: BorderRadius.circular(10)),
                  child: Row(mainAxisSize: MainAxisSize.min, children: [
                    Icon(Icons.near_me_outlined, size: 16, color: AppColors.primary),
                    const SizedBox(width: 4),
                    Text(CourierFormat.distance(t, distance!), style: const TextStyle(fontWeight: FontWeight.w700)),
                  ]),
                ),
            ]),
            const SizedBox(height: 10),
            Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              const Padding(
                padding: EdgeInsets.only(top: 2),
                child: Icon(Icons.place_outlined, size: 20, color: AppColors.textSecondary),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(order.addressLine ?? '—', style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                  if (details.isNotEmpty)
                    Text(details, style: theme.textTheme.bodySmall?.copyWith(color: AppColors.textSecondary)),
                ]),
              ),
            ]),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(child: Align(alignment: Alignment.centerLeft, child: CourierPaymentBadge(order: order))),
              const SizedBox(width: 8),
              Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                Text(CourierFormat.money(t, order.totalAmount),
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
                if (order.itemCount > 0)
                  Text(t.courierItemsCount(order.itemCount.toString()),
                      style: theme.textTheme.bodySmall?.copyWith(color: AppColors.textSecondary)),
              ]),
            ]),
          ]),
        ),
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.delivered});

  final bool delivered;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: delivered ? AppColors.primarySoft : const Color(0xFFFDE8E7),
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
      ),
      child: Text(
        delivered ? t.courierDelivered : t.courierStatusNotDelivered,
        style: TextStyle(fontWeight: FontWeight.w700, color: delivered ? AppColors.primary : AppColors.danger),
      ),
    );
  }
}
