import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/router.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import 'data/courier_models.dart';
import 'data/courier_repository.dart';
import 'widgets/courier_widgets.dart';

/// Kuryerga taklif (kelishuv 10.6): push `type=courier_offer` yoki SignalR `CourierOffer` kelganda
/// takliflar qayta yuklanadi va yangi taklif to'liq ekranda ochiladi (bir taklif — bir marta).
class CourierOfferAlert {
  CourierOfferAlert(this._ref);

  final Ref _ref;
  final _shown = <String>{};
  bool _busy = false;

  /// Yangi taklif xabari. [orderId] / [offerId] — qaysi biri ma'lum bo'lsa.
  Future<void> incoming({String? orderId, String? offerId}) async {
    _ref.invalidate(courierOffersProvider);
    if (_busy) return;
    _busy = true;
    try {
      final offers = await _ref.read(courierOffersProvider.future);
      CourierOffer? offer;
      for (final o in offers) {
        if ((offerId != null && o.offerId == offerId) || (orderId != null && o.orderId == orderId)) offer = o;
      }
      // Identifikator berilmagan bo'lsa — eng yangi ko'rilmagan taklif.
      if (offer == null && orderId == null && offerId == null) {
        for (final o in offers) {
          if (!_shown.contains(o.offerId)) offer = o;
        }
      }
      if (offer == null || _shown.contains(offer.offerId) || offer.isExpired()) return;
      _shown.add(offer.offerId);
      final router = _ref.read(routerProvider);
      // Boshqa taklif ekrani ochiq bo'lsa ham — yangisi ustidan.
      unawaited(router.push('/courier/offer?offerId=${Uri.encodeComponent(offer.offerId)}'));
    } catch (_) {
      // Tarmoq xatosi — kuryer "Takliflar" tabidan ko'radi.
    } finally {
      _busy = false;
    }
  }

  /// Push bosilganda: taklifni ko'rsatish (oldin ko'rilgan bo'lsa ham).
  void open({String? orderId, String? offerId}) {
    final q = [
      if (offerId != null) 'offerId=${Uri.encodeComponent(offerId)}',
      if (orderId != null) 'orderId=${Uri.encodeComponent(orderId)}',
    ].join('&');
    if (offerId != null) _shown.add(offerId);
    _ref.invalidate(courierOffersProvider);
    _ref.read(routerProvider).push('/courier/offer${q.isEmpty ? '' : '?$q'}');
  }

  /// Taklif yopildi (boshqa kuryer oldi / muddati tugadi) — ro'yxat yangilanadi, ochiq ekran o'zi yopiladi.
  void closed() => _ref.invalidate(courierOffersProvider);
}

final courierOfferAlertProvider = Provider<CourierOfferAlert>((ref) => CourierOfferAlert(ref));

/// "02:05" ko'rinishida qolgan vaqt.
String formatCountdown(Duration d) {
  final m = d.inMinutes, s = d.inSeconds % 60;
  return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
}

/// To'liq ekranli taklif kartasi: raqam, filial, manzil, masofa, summa, to'lov turi, ortga sanash.
class CourierOfferScreen extends ConsumerStatefulWidget {
  const CourierOfferScreen({super.key, this.offerId, this.orderId});

  final String? offerId;
  final String? orderId;

  @override
  ConsumerState<CourierOfferScreen> createState() => _CourierOfferScreenState();
}

class _CourierOfferScreenState extends ConsumerState<CourierOfferScreen> {
  Timer? _tick;
  bool _busy = false;

  /// Server 409 qaytardi yoki taklif ro'yxatdan yo'qoldi.
  String? _closedText;

  @override
  void initState() {
    super.initState();
    _tick = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _tick?.cancel();
    super.dispose();
  }

  CourierOffer? _find(List<CourierOffer> offers) {
    for (final o in offers) {
      if (widget.offerId != null && o.offerId == widget.offerId) return o;
      if (widget.offerId == null && widget.orderId != null && o.orderId == widget.orderId) return o;
    }
    return null;
  }

  void _close() => context.canPop() ? context.pop() : context.go('/courier');

  Future<void> _accept(CourierOffer o) async {
    final t = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).acceptOffer(o.offerId);
      ref.invalidate(courierOffersProvider);
      ref.invalidate(courierOrdersProvider(false));
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(t.courierOfferAccepted)));
      context.go('/courier/order/${o.orderId}');
    } on ApiException catch (e) {
      ref.invalidate(courierOffersProvider);
      if (!mounted) return;
      if (e.statusCode == 409) {
        setState(() => _closedText = t.courierOfferTaken);
      } else {
        messenger.showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _decline(CourierOffer o) async {
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(courierRepositoryProvider).declineOffer(o.offerId);
      ref.invalidate(courierOffersProvider);
      if (mounted) _close();
    } on ApiException catch (e) {
      ref.invalidate(courierOffersProvider);
      if (!mounted) return;
      // Taklif allaqachon yopilgan bo'lsa ham ekran yopiladi.
      if (e.statusCode == 409 || e.statusCode == 404) {
        _close();
      } else {
        messenger.showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(courierOffersProvider);
    final offers = async.value;
    final offer = offers == null ? null : _find(offers);
    // Ro'yxat yangilandi va taklif yo'q — boshqa kuryer oldi yoki yopildi.
    final closed = _closedText ?? (offers != null && offer == null && !async.isLoading ? t.courierOfferClosed : null);

    final Widget body;
    if (closed != null) {
      body = _Closed(text: closed, onBack: _close);
    } else if (offer == null) {
      body = async.hasError
          ? CourierErrorView(message: async.error.toString(), onRetry: () => ref.invalidate(courierOffersProvider))
          : const Center(child: CircularProgressIndicator());
    } else {
      body = _OfferCard(
        offer: offer,
        busy: _busy,
        onAccept: () => _accept(offer),
        onDecline: () => _decline(offer),
      );
    }

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(t.courierOfferTitle),
        leading: IconButton(icon: const Icon(Icons.close), tooltip: t.courierCancel, onPressed: _close),
      ),
      body: SafeArea(child: body),
    );
  }
}

class _Closed extends StatelessWidget {
  const _Closed({required this.text, required this.onBack});

  final String text;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            const Icon(Icons.do_not_disturb_on_outlined, size: 56, color: AppColors.iconMuted),
            const SizedBox(height: 12),
            Text(text,
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            const SizedBox(height: 16),
            OutlinedButton(onPressed: onBack, child: Text(AppLocalizations.of(context).courierBack)),
          ]),
        ),
      );
}

class _OfferCard extends StatelessWidget {
  const _OfferCard({required this.offer, required this.busy, required this.onAccept, required this.onDecline});

  final CourierOffer offer;
  final bool busy;
  final VoidCallback onAccept;
  final VoidCallback onDecline;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final left = offer.remaining();
    final expired = offer.isExpired();
    final label = theme.textTheme.bodySmall?.copyWith(color: AppColors.textSecondary);

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Expanded(
          child: ListView(children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    Expanded(
                      child: Text(t.courierOfferNumber(offer.number),
                          style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
                    ),
                    if (offer.expiresAt != null)
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          color: expired ? AppColors.chipSurface : AppColors.primarySoft,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Row(mainAxisSize: MainAxisSize.min, children: [
                          Icon(Icons.timer_outlined, size: 18, color: expired ? AppColors.textSecondary : AppColors.primary),
                          const SizedBox(width: 6),
                          Text(formatCountdown(left),
                              style: TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 16,
                                  color: expired ? AppColors.textSecondary : AppColors.primary)),
                        ]),
                      ),
                  ]),
                  if (offer.branchName != null) ...[
                    const SizedBox(height: 4),
                    Text(offer.branchName!, style: label),
                  ],
                  const SizedBox(height: 16),
                  Text(t.courierAddress, style: label),
                  const SizedBox(height: 2),
                  Text(offer.address ?? '—', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                  if (offer.distanceKm != null) ...[
                    const SizedBox(height: 8),
                    Row(children: [
                      Icon(Icons.near_me_outlined, size: 18, color: AppColors.primary),
                      const SizedBox(width: 6),
                      Text(CourierFormat.distance(t, offer.distanceKm! * 1000),
                          style: const TextStyle(fontWeight: FontWeight.w700)),
                    ]),
                  ],
                  const Divider(height: 28, color: AppColors.borderSoft),
                  Row(children: [
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(offer.isCash ? t.courierOfferCash : t.courierOfferPaidOrCard, style: label),
                        if (offer.itemCount > 0) Text(t.courierItemsCount(offer.itemCount.toString()), style: label),
                      ]),
                    ),
                    Text(CourierFormat.money(t, offer.totalAmount),
                        style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                  ]),
                ]),
              ),
            ),
            if (expired) ...[
              const SizedBox(height: 12),
              Text(t.courierOfferExpired, textAlign: TextAlign.center, style: label),
            ],
          ]),
        ),
        const SizedBox(height: 12),
        FilledButton(
          onPressed: busy || expired ? null : onAccept,
          child: busy
              ? const SizedBox(
                  width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
              : Text(t.courierOfferAccept),
        ),
        const SizedBox(height: 10),
        OutlinedButton(
          onPressed: busy ? null : onDecline,
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(52),
            foregroundColor: AppColors.danger,
            side: const BorderSide(color: AppColors.border),
          ),
          child: Text(t.courierOfferDecline),
        ),
      ]),
    );
  }
}

/// "Takliflar" tabi: kutilayotgan takliflar ro'yxati.
class CourierOffersList extends ConsumerWidget {
  const CourierOffersList({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(courierOffersProvider);

    Future<void> refresh() async {
      ref.invalidate(courierOffersProvider);
      try {
        await ref.read(courierOffersProvider.future);
      } catch (_) {}
    }

    return async.when(
      skipLoadingOnRefresh: true,
      skipLoadingOnReload: true,
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => CourierErrorView(message: e.toString(), onRetry: refresh),
      data: (offers) {
        final list = offers.where((o) => !o.isExpired()).toList();
        return RefreshIndicator(
          onRefresh: refresh,
          child: list.isEmpty
              ? ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    const SizedBox(height: 120),
                    const Icon(Icons.notifications_none, size: 56, color: AppColors.iconMuted),
                    const SizedBox(height: 12),
                    Text(t.courierOffersEmpty,
                        textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
                  ],
                )
              : ListView.separated(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                  itemCount: list.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (_, i) => _OfferTile(offer: list[i]),
                ),
        );
      },
    );
  }
}

class _OfferTile extends ConsumerWidget {
  const _OfferTile({required this.offer});

  final CourierOffer offer;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => ref.read(courierOfferAlertProvider).open(offerId: offer.offerId),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Text(t.courierOfferNumber(offer.number),
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
              const Spacer(),
              if (offer.expiresAt != null)
                _Countdown(offer: offer),
            ]),
            const SizedBox(height: 8),
            Text(offer.address ?? '—', style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Row(children: [
              if (offer.distanceKm != null)
                Text(CourierFormat.distance(t, offer.distanceKm! * 1000),
                    style: const TextStyle(color: AppColors.textSecondary, fontWeight: FontWeight.w600)),
              const Spacer(),
              Text(CourierFormat.money(t, offer.totalAmount), style: const TextStyle(fontWeight: FontWeight.w800)),
            ]),
          ]),
        ),
      ),
    );
  }
}

/// Har soniyada yangilanadigan kichik ortga sanash.
class _Countdown extends StatefulWidget {
  const _Countdown({required this.offer});

  final CourierOffer offer;

  @override
  State<_Countdown> createState() => _CountdownState();
}

class _CountdownState extends State<_Countdown> {
  Timer? _t;

  @override
  void initState() {
    super.initState();
    _t = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _t?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Text(formatCountdown(widget.offer.remaining()),
      style: TextStyle(fontWeight: FontWeight.w800, color: AppColors.primary));
}
