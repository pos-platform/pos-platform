import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:share_plus/share_plus.dart';

import '../../core/api/api_exception.dart';
import '../../core/config.dart';
import '../../core/store_app/store_app.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../bonus/bonus_card_screen.dart';
import '../bonus/data/bonus_card_repository.dart';
import '../catalog/store_selection.dart';
import '../catalog/widgets/common.dart';
import 'data/marketing_models.dart';
import 'data/marketing_repository.dart';

/// Joriy do'kon tashkiloti: do'kon rejimida — ilova do'koni; aks holda tanlangan filial (server bersa)
/// yoki bonus do'konlari ro'yxatidan shu filial. Topilmasa — null.
final currentOrganizationIdProvider = FutureProvider.autoDispose<String?>((ref) async {
  if (AppConfig.isStoreMode) {
    final org = (await ref.watch(storeAppProvider.future))?.organizationId;
    if (org != null && org.isNotEmpty) return org;
  }
  final store = await ref.watch(selectedStoreProvider.future);
  if (store?.organizationId != null) return store!.organizationId;
  final stores = await ref.watch(bonusStoresProvider.future);
  return stores.where((s) => s.branchId == store?.id).firstOrNull?.organizationId ?? stores.firstOrNull?.organizationId;
});

final referralProvider = FutureProvider.autoDispose<ReferralInfo?>((ref) async {
  final org = await ref.watch(currentOrganizationIdProvider.future);
  if (org == null) return null;
  return ref.watch(marketingRepositoryProvider).referral(org);
});

/// Profil → "Do'stni taklif qilish" (kelishuv 9.4): kod, nusxa, ulashish, statistika, kod kiritish.
class ReferralScreen extends ConsumerWidget {
  const ReferralScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(referralProvider);
    return Scaffold(
      appBar: AppBar(title: Text(t.refTitle)),
      body: switch (async) {
        AsyncData(value: final info?) => RefreshIndicator(
          onRefresh: () => ref.refresh(referralProvider.future),
          child: _ReferralBody(info: info),
        ),
        AsyncData() => Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Text(
              t.refUnavailable,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
          ),
        ),
        AsyncError(:final error) => ErrorRetry(
          message: ApiException.from(error).message,
          onRetry: () => ref.invalidate(referralProvider),
        ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _ReferralBody extends ConsumerWidget {
  const _ReferralBody({required this.info});

  final ReferralInfo info;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    if (!info.enabled) {
      return ListView(
        padding: const EdgeInsets.all(32),
        children: [
          const Icon(Icons.card_giftcard, size: 52, color: AppColors.iconMuted),
          const SizedBox(height: 12),
          Text(
            t.refUnavailable,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textSecondary),
          ),
        ],
      );
    }
    final share = info.shareText ?? t.refShareFallback(info.code, formatBonusPoints(t, info.refereeBonus));
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(20)),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.card_giftcard, color: Colors.white, size: 32),
              const SizedBox(height: 10),
              Text(
                t.refHeadline(formatBonusPoints(t, info.referrerBonus)),
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: Colors.white, height: 1.25),
              ),
              const SizedBox(height: 6),
              Text(
                t.refExplain(formatBonusPoints(t, info.refereeBonus)),
                style: TextStyle(fontSize: 14, color: AppColors.onPrimarySoft, height: 1.4),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Text(
          t.refYourCode,
          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.fromLTRB(18, 8, 8, 8),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(AppTheme.radiusInput),
            border: Border.all(color: AppColors.border),
          ),
          child: Row(
            children: [
              Expanded(
                child: SelectableText(
                  info.code,
                  style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w800, letterSpacing: 3),
                ),
              ),
              IconButton(
                tooltip: t.refCopy,
                icon: const Icon(Icons.copy_rounded),
                onPressed: () async {
                  await Clipboard.setData(ClipboardData(text: info.code));
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t.refCopied)));
                  }
                },
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: () => SharePlus.instance.share(ShareParams(text: share)),
          icon: const Icon(Icons.ios_share),
          label: Text(t.refShare),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _Stat(value: '${info.invitedCount}', label: t.refInvited),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _Stat(value: '${info.rewardedCount}', label: t.refRewarded),
            ),
          ],
        ),
        const SizedBox(height: 24),
        const _ApplyCode(),
      ],
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(AppTheme.radiusCard),
      border: Border.all(color: AppColors.borderSoft),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(value, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800)),
        Text(label, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
      ],
    ),
  );
}

/// "Do'stingiz kodi bormi?" — yangi mijoz uchun kod kiritish.
class _ApplyCode extends ConsumerStatefulWidget {
  const _ApplyCode();

  @override
  ConsumerState<_ApplyCode> createState() => _ApplyCodeState();
}

class _ApplyCodeState extends ConsumerState<_ApplyCode> {
  final _code = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _apply() async {
    final code = _code.text.trim();
    if (code.isEmpty || _busy) return;
    FocusScope.of(context).unfocus();
    final t = AppLocalizations.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final org = await ref.read(currentOrganizationIdProvider.future);
      if (org == null) throw ApiException(t.refUnavailable);
      await ref.read(marketingRepositoryProvider).applyReferral(org, code);
      if (!mounted) return;
      _code.clear();
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t.refApplied)));
      ref.invalidate(referralProvider);
    } catch (e) {
      if (mounted) setState(() => _error = ApiException.from(e).message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(t.refHaveCode, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
        const SizedBox(height: 4),
        Text(t.refHaveCodeHint, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: _code,
                maxLength: 12,
                textCapitalization: TextCapitalization.characters,
                onSubmitted: (_) => _apply(),
                decoration: InputDecoration(hintText: t.refCodeHint, counterText: '', errorText: _error),
              ),
            ),
            const SizedBox(width: 10),
            SizedBox(
              height: 56,
              child: FilledButton.tonal(
                onPressed: _busy ? null : _apply,
                child: _busy
                    ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(t.cartPromoApply),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
