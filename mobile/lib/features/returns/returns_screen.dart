import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../orders/order_updates.dart';
import '../orders/widgets/order_format.dart';
import '../profile/widgets/profile_widgets.dart';
import 'data/return_models.dart';
import 'data/returns_repository.dart';
import 'widgets/return_format.dart';

/// "Qaytarishlarim" (Profil → `/profile/returns`).
class ReturnsScreen extends ConsumerWidget {
  const ReturnsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    ref.watch(orderUpdatesProvider); // ReturnStatusChanged → ro'yxat yangilanadi
    final async = ref.watch(returnsListProvider);

    Future<void> refresh() async {
      ref.invalidate(returnsListProvider);
      try {
        await ref.read(returnsListProvider.future);
      } catch (_) {}
    }

    return Scaffold(
      body: SafeArea(
        child: Column(children: [
          ProfilePageHeader(title: t.returnsTitle),
          Expanded(
            child: async.when(
              skipLoadingOnRefresh: true,
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => ProfileErrorView(error: e, onRetry: refresh),
              data: (list) => RefreshIndicator(
                onRefresh: refresh,
                child: list.isEmpty
                    ? ListView(physics: const AlwaysScrollableScrollPhysics(), children: [
                        const SizedBox(height: 120),
                        const Icon(Icons.assignment_return_outlined, size: 56, color: AppColors.iconMuted),
                        const SizedBox(height: 12),
                        Text(t.returnsEmpty,
                            textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
                      ])
                    : ListView.separated(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                        itemCount: list.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 10),
                        itemBuilder: (_, i) => _ReturnTile(item: list[i]),
                      ),
              ),
            ),
          ),
        ]),
      ),
    );
  }
}

class _ReturnTile extends StatelessWidget {
  const _ReturnTile({required this.item});

  final ShopReturn item;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final amount = item.refundAmount > 0 ? item.refundAmount : item.itemsTotal;
    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: () => context.push('/profile/returns/${item.id}'),
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: AppColors.borderSoft),
          ),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Expanded(
                child: Text(t.returnNumber(item.number), style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
              ),
              ReturnStatusPill(status: item.status),
            ]),
            const SizedBox(height: 6),
            Text(
              [
                if (item.orderNumber != null) t.ordersOrderNumber(item.orderNumber!),
                OrderFormat.dateTime(context, t, item.createdAt),
              ].join(' · '),
              style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
            ),
            if (amount > 0) ...[
              const SizedBox(height: 6),
              Text(OrderFormat.money(t, amount), style: const TextStyle(fontWeight: FontWeight.w700)),
            ],
          ]),
        ),
      ),
    );
  }
}
