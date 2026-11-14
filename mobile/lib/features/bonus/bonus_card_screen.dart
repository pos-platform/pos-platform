import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:screen_brightness/screen_brightness.dart';

import '../../core/api/api_exception.dart';
import '../../core/config.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../catalog/store_selection.dart';
import '../catalog/widgets/common.dart';
import '../orders/widgets/order_format.dart';
import '../profile/widgets/profile_widgets.dart';
import 'data/bonus_card_cache.dart';
import 'data/bonus_card_repository.dart';
import '../../core/store_app/store_branding.dart';

final _points = NumberFormat('#,##0', 'en_US');

/// "12 450 ball" — ball butun so'mga pastga yaxlitlanadi (kelishuv 7).
String formatBonusPoints(AppLocalizations t, double points) =>
    t.bonusPoints(_points.format(points.truncate()).replaceAll(',', ' '));

/// Bonus karta (dizayn: BonusCard.dc.html; kelishuv 7, §3): do'kon tanlash, dinamik QR (~90 s, muddatidan
/// ~10 s oldin avtomatik yangilanadi), balans va ball tarixi.
///
/// Daraja ("Kumush · 3%") va keyingi darajagacha progress — bosqich 9.4 (`tier`/`nextTier`).
class BonusCardScreen extends ConsumerWidget {
  const BonusCardScreen({super.key, this.organizationId});

  /// Ochiladigan do'kon (tashkilot). Null — tanlangan do'kon yoki birinchisi.
  final String? organizationId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final signedIn = ref.watch(authControllerProvider.select((a) => a.isSignedIn));
    final t = AppLocalizations.of(context);
    return Scaffold(
      body: SafeArea(
        child: Column(children: [
          // Pastki menyudagi "Bonus" tabida ildiz ekran — orqaga tugmasi yo'q.
          ProfilePageHeader(title: t.bonusTitle, showBack: Navigator.of(context).canPop()),
          Expanded(child: signedIn ? _BonusBody(organizationId: organizationId) : const _GuestPrompt()),
        ]),
      ),
    );
  }
}

class _BonusBody extends ConsumerStatefulWidget {
  const _BonusBody({this.organizationId});

  final String? organizationId;

  @override
  ConsumerState<_BonusBody> createState() => _BonusBodyState();
}

class _BonusBodyState extends ConsumerState<_BonusBody> with WidgetsBindingObserver {
  /// Kod muddatidan shuncha oldin yangilanadi.
  static const _refreshBefore = Duration(seconds: 10);

  final _scroll = ScrollController();

  bool _initialized = false;
  String? _orgId;
  String? _branchId;
  bool _storesReloaded = false;

  BonusCard? _card;
  BonusCard? _preview; // keshdagi (muddati o'tgan bo'lishi mumkin) — nom va balans uchun
  DateTime? _fetchedAt;
  DateTime? _validUntil; // qurilma soati bo'yicha (server soati farqidan himoya)
  bool _loadingCard = false;
  String? _cardError;
  int _generation = 0;

  Timer? _tick;
  Timer? _refreshTimer;
  bool _active = true; // tab ko'rinib turibdi va ilova old fonda
  bool _boosted = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _scroll.addListener(() {
      final org = _orgId;
      if (org != null && _scroll.position.extentAfter < 400) {
        ref.read(bonusHistoryProvider(org).notifier).loadMore();
      }
    });
    _restoreFromCache();
    ref.listenManual(bonusStoresProvider, (_, next) {
      final stores = next.value;
      // Provider yangilanishi build paytida kelishi mumkin — setState keyingi mikrovazifada.
      if (stores != null) Future.microtask(() => mounted ? _onStores(stores) : null);
    }, fireImmediately: true);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Pastki menyuda boshqa tab ochilsa — go_router bu tabning tickerlarini o'chiradi.
    _setActive(TickerMode.valuesOf(context).enabled && _appResumed);
  }

  bool get _appResumed {
    final s = WidgetsBinding.instance.lifecycleState;
    return s == null || s == AppLifecycleState.resumed;
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _setActive(state == AppLifecycleState.resumed && TickerMode.valuesOf(context).enabled);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _tick?.cancel();
    _refreshTimer?.cancel();
    _scroll.dispose();
    _brightness(false);
    super.dispose();
  }

  void _setActive(bool active) {
    if (active == _active && _tick != null) return;
    _active = active;
    _brightness(active);
    _tick?.cancel();
    _refreshTimer?.cancel();
    if (!active) return;
    _tick = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
    // Qaytganda kod eskirgan bo'lishi mumkin — darhol yoki rejaga ko'ra yangilanadi.
    if (_card != null) _scheduleRefresh();
  }

  /// Skaner o'qishi uchun ekran yorqinligi oshiriladi (faqat shu ekran ochiq paytda).
  Future<void> _brightness(bool on) async {
    if (on == _boosted) return;
    _boosted = on;
    try {
      if (on) {
        await ScreenBrightness.instance.setApplicationScreenBrightness(1.0);
      } else {
        await ScreenBrightness.instance.resetApplicationScreenBrightness();
      }
    } catch (_) {
      // Plagin yo'q (test, ba'zi platformalar) — yorqinliksiz ishlaydi.
    }
  }

  /// Keshdagi karta (oldindan yuklangan yoki oxirgi) — tanlangan do'konniki bo'lsa darhol ko'rsatiladi:
  /// muddati o'tmagan bo'lsa QR bilan, aks holda faqat do'kon nomi/balans (QR yangilanguncha).
  void _restoreFromCache() {
    final cached = ref.read(bonusCardCacheProvider);
    if (cached == null || widget.organizationId != null) return;
    final selected = ref.read(storeSelectionProvider).value;
    if (selected != null && selected != cached.branchId) return;
    _branchId = cached.branchId;
    _preview = cached.card;
    if (cached.isFresh) {
      _applyCard(cached);
    } else {
      // Do'konlar ro'yxatini kutmasdan yangi QR so'raladi (initState ichida setState chaqirilmaydi).
      Future.microtask(() => mounted ? _loadCard() : null);
    }
  }

  void _applyCard(CachedBonusCard c) {
    _card = c.card;
    _preview = c.card;
    _fetchedAt = c.fetchedAt;
    _validUntil = c.validUntil;
  }

  /// Do'konlar ro'yxati keldi: birinchi marta — boshlang'ich do'kon tanlanadi.
  Future<void> _onStores(List<BonusStore> stores) async {
    if (_initialized) {
      // Karta yangi mijoz yozuvini yaratgan bo'lishi mumkin — tashkilot ID filial bo'yicha topiladi.
      if (_orgId == null && _branchId != null) {
        final m = stores.where((s) => s.branchId == _branchId).firstOrNull;
        if (m != null && mounted) setState(() => _orgId = m.organizationId);
      }
      return;
    }
    _initialized = true;
    String? selectedBranch;
    try {
      selectedBranch = await ref.read(storeSelectionProvider.future);
    } catch (_) {}
    if (!mounted) return;

    BonusStore? pick;
    if (widget.organizationId != null) {
      pick = stores.where((s) => s.organizationId == widget.organizationId).firstOrNull;
    }
    pick ??= selectedBranch == null ? null : stores.where((s) => s.branchId == selectedBranch).firstOrNull;
    // Keshdan ko'rsatilgan karta do'koni.
    pick ??= _branchId == null ? null : stores.where((s) => s.branchId == _branchId).firstOrNull;

    if (pick != null) {
      _select(pick.organizationId, pick.branchId);
    } else if (selectedBranch != null) {
      // Tanlangan do'konda hali mijoz yozuvi yo'q — karta so'rovi uni yaratadi.
      _select(null, selectedBranch);
    } else if (stores.isNotEmpty) {
      _select(stores.first.organizationId, stores.first.branchId);
    } else {
      setState(() {});
    }
  }

  void _select(String? orgId, String? branchId) {
    if (branchId != null && branchId == _branchId && _card != null) {
      // Keshdan ko'rsatilgan karta shu do'konniki — faqat tashkilot aniqlanadi, QR rejaga ko'ra yangilanadi.
      setState(() => _orgId = orgId);
      _scheduleRefresh();
      return;
    }
    _generation++;
    _refreshTimer?.cancel();
    setState(() {
      _orgId = orgId;
      _branchId = branchId;
      _card = null;
      _preview = null;
      _cardError = null;
      _loadingCard = false;
      final cached = ref.read(bonusCardCacheProvider.notifier).forBranch(branchId);
      if (cached != null) {
        _preview = cached.card;
        if (cached.isFresh) _applyCard(cached);
      }
    });
    if (branchId == null) return;
    if (_card != null) {
      _scheduleRefresh();
    } else {
      _loadCard();
    }
  }

  Future<void> _loadCard() async {
    final branchId = _branchId;
    if (branchId == null || _loadingCard) return;
    final gen = _generation;
    setState(() {
      _loadingCard = true;
      _cardError = null;
    });
    try {
      final card = await ref.read(bonusCardRepositoryProvider).card(branchId);
      if (!mounted || gen != _generation) return;
      final cached = CachedBonusCard.fromServer(branchId, card);
      ref.read(bonusCardCacheProvider.notifier).put(cached);
      setState(() {
        _applyCard(cached);
        _loadingCard = false;
      });
      _scheduleRefresh();
      if (_orgId == null && !_storesReloaded) {
        _storesReloaded = true;
        ref.invalidate(bonusStoresProvider);
      }
    } on ApiException catch (e) {
      if (!mounted || gen != _generation) return;
      setState(() {
        _cardError = e.message;
        _loadingCard = false;
      });
      // Tarmoq qaytishi bilan avtomatik qayta urinish.
      _refreshTimer?.cancel();
      if (_active) _refreshTimer = Timer(const Duration(seconds: 10), _loadCard);
    }
  }

  void _scheduleRefresh() {
    _refreshTimer?.cancel();
    final until = _validUntil;
    if (until == null || !_active) return;
    final wait = until.subtract(_refreshBefore).difference(DateTime.now());
    if (wait <= Duration.zero) {
      _loadCard();
    } else {
      _refreshTimer = Timer(wait, _loadCard);
    }
  }

  Future<void> _onRefresh() async {
    ref.invalidate(bonusStoresProvider);
    final org = _orgId;
    if (org != null) ref.invalidate(bonusHistoryProvider(org));
    await _loadCard();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final storesAsync = ref.watch(bonusStoresProvider);
    final stores = storesAsync.value ?? const <BonusStore>[];

    if (!_initialized && _preview == null) {
      if (storesAsync case AsyncError(:final error)) {
        return Center(
          child: ErrorRetry(
            message: ApiException.from(error).message,
            onRetry: () => ref.invalidate(bonusStoresProvider),
          ),
        );
      }
      return const Center(child: CircularProgressIndicator());
    }
    if (_orgId == null && _branchId == null) return const _NoStores();

    final current = stores.where((s) => s.organizationId == _orgId).firstOrNull;
    final org = _orgId;

    return RefreshIndicator(
      onRefresh: _onRefresh,
      child: ListView(
        controller: _scroll,
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          if (stores.length > 1 || (stores.isNotEmpty && current == null)) ...[
            _StoreChips(
              stores: stores,
              selectedOrgId: _orgId,
              onSelected: (s) {
                if (s.organizationId != _orgId) _select(s.organizationId, s.branchId);
              },
            ),
            const SizedBox(height: 14),
          ],
          _CardPanel(
            storeName: _card?.storeName ?? _preview?.storeName ?? current?.storeName,
            points: _card?.points ?? _preview?.points ?? current?.points,
            card: _card,
            hasBranch: _branchId != null,
            loading: _loadingCard,
            error: _cardError,
            remaining: _remaining(),
            progress: _progress(),
            onRetry: _loadCard,
          ),
          if (_card?.tier != null || _card?.nextTier != null) ...[
            const SizedBox(height: 12),
            _TierProgress(card: _card!),
          ],
          const SizedBox(height: 20),
          Text(t.bonusHistory, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          if (org == null)
            const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()))
          else
            _History(organizationId: org),
        ],
      ),
    );
  }

  Duration? _remaining() {
    final until = _validUntil;
    if (until == null) return null;
    final r = until.difference(DateTime.now());
    return r.isNegative ? Duration.zero : r;
  }

  /// Halqa uchun 1 → 0 (kod qancha muddat qolgani).
  double? _progress() {
    final until = _validUntil, from = _fetchedAt;
    if (until == null || from == null) return null;
    final total = until.difference(from).inMilliseconds;
    if (total <= 0) return 0;
    return (until.difference(DateTime.now()).inMilliseconds / total).clamp(0.0, 1.0);
  }
}

/// Loyalty darajasi ("Kumush · 3%") va keyingi darajagacha progress ("Gold'gacha 350 000 so'm").
class _TierProgress extends StatelessWidget {
  const _TierProgress({required this.card});

  final BonusCard card;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tier = card.tier, next = card.nextTier;
    final progress = next == null || next.minSpend <= 0
        ? 1.0
        : ((next.minSpend - next.remaining) / next.minSpend).clamp(0.0, 1.0);
    final pct = NumberFormat('0.#', 'en_US');
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Icon(Icons.workspace_premium_outlined, color: AppColors.primary),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              tier == null ? t.tierNone : t.tierCurrent(tier.name, pct.format(tier.cashbackPercent)),
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
            ),
          ),
        ]),
        if (next != null) ...[
          const SizedBox(height: 10),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 6,
              color: AppColors.primary,
              backgroundColor: AppColors.primarySoft,
            ),
          ),
          const SizedBox(height: 8),
          Text(t.tierNextLeft(next.name, formatMoney(context, next.remaining)),
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
        ] else if (tier != null) ...[
          const SizedBox(height: 6),
          Text(t.tierTop, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
        ],
      ]),
    );
  }
}

class _StoreChips extends StatelessWidget {
  const _StoreChips({required this.stores, required this.selectedOrgId, required this.onSelected});

  final List<BonusStore> stores;
  final String? selectedOrgId;
  final ValueChanged<BonusStore> onSelected;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 40,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: stores.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (_, i) {
          final s = stores[i];
          return ChoiceChip(
            label: Text(s.storeName),
            selected: s.organizationId == selectedOrgId,
            onSelected: (_) => onSelected(s),
            showCheckmark: false,
            selectedColor: AppColors.primary,
            backgroundColor: AppColors.surface,
            side: const BorderSide(color: AppColors.border),
            labelStyle: TextStyle(
              fontWeight: FontWeight.w700,
              color: s.organizationId == selectedOrgId ? Colors.white : AppColors.text,
            ),
          );
        },
      ),
    );
  }
}

/// Yashil karta: balans, QR va yangilanish halqasi.
class _CardPanel extends StatelessWidget {
  const _CardPanel({
    required this.storeName,
    required this.points,
    required this.card,
    required this.hasBranch,
    required this.loading,
    required this.error,
    required this.remaining,
    required this.progress,
    required this.onRetry,
  });

  final String? storeName;
  final double? points;
  final BonusCard? card;
  final bool hasBranch;
  final bool loading;
  final String? error;
  final Duration? remaining;
  final double? progress;
  final VoidCallback onRetry;

  static Color get _onPrimarySoft => AppColors.onPrimarySoft;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final expired = remaining == Duration.zero;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(24)),
      child: Column(children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(t.bonusBalance,
                  style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _onPrimarySoft)),
              const SizedBox(height: 2),
              Text(
                points == null ? '…' : formatBonusPoints(t, points!),
                style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w800, letterSpacing: -0.5, color: Colors.white),
              ),
            ]),
          ),
          if (AppConfig.isStoreMode)
            // Do'kon rejimida — tashkilot nomi o'rniga brend yozuvi (oq fonda).
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
              decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(999)),
              child: const BrandWordmark(fontSize: 13),
            )
          else if (storeName != null && storeName!.isNotEmpty)
            Flexible(
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.2), borderRadius: BorderRadius.circular(999)),
                child: Text(storeName!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: Colors.white)),
              ),
            ),
        ]),
        const SizedBox(height: 16),
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(18)),
          child: SizedBox(
            width: 220,
            height: 220,
            child: _qr(t, expired),
          ),
        ),
        const SizedBox(height: 12),
        if (hasBranch) _status(t, expired),
        const SizedBox(height: 10),
        Text(t.bonusHint,
            textAlign: TextAlign.center, style: TextStyle(fontSize: 13, height: 1.45, color: _onPrimarySoft)),
      ]),
    );
  }

  Widget _qr(AppLocalizations t, bool expired) {
    if (!hasBranch) {
      return Center(
        child: Text(t.bonusNoQr,
            textAlign: TextAlign.center, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
      );
    }
    final c = card;
    if (c == null) {
      return error == null
          ? const Center(child: CircularProgressIndicator())
          : const Center(child: Icon(Icons.qr_code_2, size: 96, color: AppColors.iconMuted));
    }
    return AnimatedOpacity(
      opacity: expired ? 0.15 : 1,
      duration: const Duration(milliseconds: 250),
      child: QrImageView(
        data: c.qrPayload,
        padding: EdgeInsets.zero,
        errorCorrectionLevel: QrErrorCorrectLevel.M,
        semanticsLabel: t.bonusQrSemantics,
        eyeStyle: const QrEyeStyle(eyeShape: QrEyeShape.square, color: AppColors.text),
        dataModuleStyle: const QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: AppColors.text),
      ),
    );
  }

  Widget _status(AppLocalizations t, bool expired) {
    const style = TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: Colors.white);
    if (error != null && (card == null || expired)) {
      return Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Flexible(child: Text(card == null ? error! : t.bonusExpired, style: style, textAlign: TextAlign.center)),
        TextButton(
          onPressed: loading ? null : onRetry,
          style: TextButton.styleFrom(foregroundColor: Colors.white),
          child: Text(t.bonusRetry),
        ),
      ]);
    }
    final r = remaining;
    final String text;
    if (loading || r == null) {
      text = t.bonusRefreshing;
    } else if (error != null) {
      text = t.bonusRefreshFailed;
    } else {
      final s = r.inSeconds;
      text = t.bonusRefreshIn('${s ~/ 60}:${(s % 60).toString().padLeft(2, '0')}');
    }
    return Row(mainAxisAlignment: MainAxisAlignment.center, children: [
      SizedBox(
        width: 20,
        height: 20,
        child: CircularProgressIndicator(
          value: loading ? null : progress,
          strokeWidth: 3,
          color: Colors.white,
          backgroundColor: Colors.white.withValues(alpha: 0.3),
        ),
      ),
      const SizedBox(width: 10),
      Flexible(child: Text(text, style: style)),
    ]);
  }
}

class _History extends ConsumerWidget {
  const _History({required this.organizationId});

  final String organizationId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final history = ref.watch(bonusHistoryProvider(organizationId));
    return switch (history) {
      AsyncData(:final value) when value.items.isEmpty => Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Text(t.bonusHistoryEmpty,
                textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
          ),
        ),
      AsyncData(:final value) => Column(children: [
          Card(
            clipBehavior: Clip.antiAlias,
            child: Column(children: [
              for (var i = 0; i < value.items.length; i++) ...[
                if (i > 0) const Divider(height: 1, thickness: 1, color: Color(0xFFF0EEE8)),
                _HistoryRow(tx: value.items[i]),
              ],
            ]),
          ),
          if (value.hasMore)
            const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator())),
        ]),
      AsyncError(:final error) => ErrorRetry(
          message: ApiException.from(error).message,
          onRetry: () => ref.invalidate(bonusHistoryProvider(organizationId)),
        ),
      _ => const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator())),
    };
  }
}

class _HistoryRow extends StatelessWidget {
  const _HistoryRow({required this.tx});

  final BonusTransaction tx;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final (icon, label) = switch (tx.type) {
      BonusTxType.earn => (Icons.add_circle_outline, t.bonusTypeEarn),
      BonusTxType.redeem => (Icons.shopping_bag_outlined, t.bonusTypeRedeem),
      BonusTxType.expire => (Icons.timer_off_outlined, t.bonusTypeExpire),
      BonusTxType.adjust => (Icons.tune, t.bonusTypeAdjust),
      BonusTxType.signup => (Icons.card_giftcard, t.bonusTypeSignup),
      BonusTxType.refund => (Icons.undo, t.bonusTypeRefund),
      BonusTxType.unknown => (Icons.swap_horiz, t.bonusTypeOther),
    };
    final positive = tx.points >= 0;
    final color = positive ? AppColors.primary : AppColors.danger;
    final sub = [
      OrderFormat.dateTime(context, t, tx.createdAt),
      switch (tx.source) {
        BonusTxSource.store => t.bonusSourceStore,
        BonusTxSource.online => t.bonusSourceOnline,
        BonusTxSource.unknown => null,
      },
      if (tx.receiptNumber != null && tx.receiptNumber!.isNotEmpty) t.bonusReceipt(tx.receiptNumber!),
    ].nonNulls.join(' · ');
    final value = _points.format(tx.points.abs().truncate()).replaceAll(',', ' ');
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(children: [
        Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            color: positive ? AppColors.primarySoft : const Color(0xFFFDECEA),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(icon, size: 18, color: color),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(label, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
            const SizedBox(height: 2),
            Text(sub, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
            if (tx.comment != null && tx.comment!.trim().isNotEmpty)
              Text(tx.comment!.trim(), style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
          ]),
        ),
        const SizedBox(width: 8),
        Text('${positive ? '+' : '−'}$value',
            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: color)),
      ]),
    );
  }
}

/// Mehmon: kirish taklifi.
class _GuestPrompt extends ConsumerWidget {
  const _GuestPrompt();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return ListView(padding: const EdgeInsets.all(20), children: [
      Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CircleAvatar(
              radius: 28,
              backgroundColor: AppColors.primarySoft,
              child: Icon(Icons.qr_code_2, color: AppColors.primary, size: 28),
            ),
            const SizedBox(height: 14),
            Text(t.bonusGuestTitle,
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
            const SizedBox(height: 6),
            Text(t.bonusGuestText,
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
            const SizedBox(height: 18),
            FilledButton(
              onPressed: () => ref.read(authControllerProvider.notifier).requestSignIn(),
              child: Text(t.login),
            ),
          ]),
        ),
      ),
    ]);
  }
}

/// Bog'langan do'kon ham, tanlangan do'kon ham yo'q.
class _NoStores extends StatelessWidget {
  const _NoStores();

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return ListView(padding: const EdgeInsets.all(20), children: [
      Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text(t.bonusNoStoresTitle,
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            const SizedBox(height: 6),
            Text(t.bonusNoStoresText,
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
            const SizedBox(height: 18),
            FilledButton(onPressed: () => context.go(AppConfig.isStoreMode ? '/home' : '/home/stores'), child: Text(t.bonusChooseStore)),
          ]),
        ),
      ),
    ]);
  }
}
