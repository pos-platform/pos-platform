import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/legal_links.dart';
import '../../core/locale/locale_controller.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../auth/data/auth_models.dart';
import '../bonus/bonus_card_screen.dart';
import '../catalog/store_selection.dart';
import '../marketing/support.dart';
import 'data/addresses_repository.dart';
import 'data/bonus_repository.dart';
import 'data/profile_repository.dart';
import 'widgets/profile_widgets.dart';

/// Profil (dizayn: Profile.dc.html).
///
/// Keyinga (dizaynda bor, 4.6 ga kirmagan): to'lov kartalari,
/// bildirishnomalar sozlamasi. Sevimlilar, do'stni taklif qilish va "Yordam" — bosqich 9.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);
    final user = auth.user;
    if (!auth.isSignedIn || user == null) return const _GuestProfile();

    final t = AppLocalizations.of(context);
    final addressCount = ref.watch(addressesProvider).value?.length;
    // Tanlangan do'kondagi bonus balansi (do'kon tanlanmagan bo'lsa — ko'rsatilmaydi).
    final branchId = ref.watch(storeSelectionProvider).value;
    final bonus = branchId == null ? null : ref.watch(bonusBalanceProvider(branchId)).value;

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(profileProvider);
            ref.invalidate(addressesProvider);
            ref.invalidate(bonusBalanceProvider);
            await ref.read(addressesProvider.future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              _UserHeader(user: user),
              if (!user.profileCompleted) ...[
                const SizedBox(height: 16),
                const _NameBanner(),
              ],
              const SizedBox(height: 16),
              _MenuGroup(children: [
                _MenuRow(
                  icon: Icons.qr_code_2,
                  label: t.bonusTitle,
                  value: bonus == null ? null : formatBonusPoints(t, bonus),
                  onTap: () => context.go('/bonus'),
                ),
                _MenuRow(
                  icon: Icons.person_outline,
                  label: t.profilePersonalInfo,
                  onTap: () => context.push('/profile/personal'),
                ),
                _MenuRow(
                  icon: Icons.location_on_outlined,
                  label: t.profileAddresses,
                  value: addressCount == null || addressCount == 0 ? null : t.profileAddressCount(addressCount),
                  onTap: () => context.push('/profile/addresses'),
                ),
                _MenuRow(
                  icon: Icons.favorite_border,
                  label: t.favTitle,
                  onTap: () => context.push('/profile/favorites'),
                ),
                _MenuRow(
                  icon: Icons.card_giftcard,
                  label: t.refTitle,
                  onTap: () => context.push('/profile/referral'),
                ),
              ]),
              const SizedBox(height: 16),
              _MenuGroup(children: [
                _MenuRow(
                  icon: Icons.support_agent,
                  label: t.supportTitle,
                  onTap: () => showSupportSheet(context, ref.read(selectedStoreProvider).value),
                ),
                _MenuRow(
                  icon: Icons.language,
                  label: t.profileLanguage,
                  value: ref.watch(localeControllerProvider) == null ? t.langDevice : t.profileLanguageName,
                  onTap: () => showLanguageSheet(context, ref),
                ),
              ]),
              const SizedBox(height: 16),
              const _LegalGroup(),
              const SizedBox(height: 8),
              _DangerButton(
                icon: Icons.logout,
                label: t.signOut,
                onTap: () => _confirmSignOut(context, ref),
              ),
              _DangerButton(
                label: t.profileDeleteAccount,
                small: true,
                onTap: () => _confirmDelete(context, ref),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _confirmSignOut(BuildContext context, WidgetRef ref) async {
    final t = AppLocalizations.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(t.profileSignOutTitle),
        content: Text(t.profileSignOutMessage),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.profileCancel)),
          TextButton(
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(t.signOut),
          ),
        ],
      ),
    );
    if (ok == true) await ref.read(authControllerProvider.notifier).signOut();
  }

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref) async {
    final ok = await showDialog<bool>(context: context, builder: (_) => const _DeleteAccountDialog());
    if (ok != true || !context.mounted) return;
    try {
      await ref.read(profileRepositoryProvider).deleteAccount();
      // Server sessiyalarni allaqachon yopgan — lokal tozalash (logout xatosi e'tiborsiz).
      await ref.read(authControllerProvider.notifier).signOut();
    } on ApiException catch (e) {
      if (context.mounted) showProfileSnack(context, e.message);
    }
  }
}

class _UserHeader extends StatelessWidget {
  const _UserHeader({required this.user});

  final ShopUser user;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: () => context.push('/profile/personal'),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: 60),
        child: Row(
          children: [
            CircleAvatar(
              radius: 28,
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              child: Text(
                initialsOf(user.firstName, user.lastName, '?'),
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user.displayName == user.phone ? formatPhone(user.phone) : user.displayName,
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
                  ),
                  const SizedBox(height: 2),
                  Text(formatPhone(user.phone), style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, color: AppColors.textSecondary),
          ],
        ),
      ),
    );
  }
}

/// Ism kiritilmagan (profileCompleted == false) — to'ldirishga taklif.
class _NameBanner extends StatelessWidget {
  const _NameBanner();

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Material(
      color: AppColors.primarySoft,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: () => context.push('/profile/personal'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Icon(Icons.badge_outlined, color: AppColors.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(t.profileNameBannerTitle,
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: AppColors.primary)),
                    const SizedBox(height: 2),
                    Text(t.profileNameBannerText, style: const TextStyle(fontSize: 13, color: AppColors.textMuted)),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, color: AppColors.primary),
            ],
          ),
        ),
      ),
    );
  }
}

/// Huquqiy hujjatlar: maxfiylik siyosati va foydalanish shartlari (tashqi brauzerda).
class _LegalGroup extends StatelessWidget {
  const _LegalGroup();

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return _MenuGroup(children: [
      _MenuRow(
        icon: Icons.privacy_tip_outlined,
        label: t.legalPrivacy,
        onTap: () => openLegal(context, LegalLinks.privacyUrl),
      ),
      _MenuRow(
        icon: Icons.description_outlined,
        label: t.legalTerms,
        onTap: () => openLegal(context, LegalLinks.termsUrl),
      ),
    ]);
  }
}

class _MenuGroup extends StatelessWidget {
  const _MenuGroup({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0) const Divider(height: 1, thickness: 1, color: Color(0xFFF0EEE8)),
            children[i],
          ],
        ],
      ),
    );
  }
}

class _MenuRow extends StatelessWidget {
  const _MenuRow({required this.icon, required this.label, required this.onTap, this.value});

  final IconData icon;
  final String label;
  final String? value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: 52),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            children: [
              Container(
                width: 34,
                height: 34,
                decoration: BoxDecoration(color: AppColors.chipSurface, borderRadius: BorderRadius.circular(10)),
                child: Icon(icon, size: 18, color: AppColors.text),
              ),
              const SizedBox(width: 14),
              Expanded(child: Text(label, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700))),
              if (value != null)
                Text(value!, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
              const SizedBox(width: 4),
              const Icon(Icons.chevron_right, size: 20, color: AppColors.iconMuted),
            ],
          ),
        ),
      ),
    );
  }
}

class _DangerButton extends StatelessWidget {
  const _DangerButton({required this.label, required this.onTap, this.icon, this.small = false});

  final String label;
  final IconData? icon;
  final bool small;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return TextButton.icon(
      style: TextButton.styleFrom(
        foregroundColor: AppColors.danger,
        minimumSize: const Size.fromHeight(48),
        textStyle: TextStyle(fontSize: small ? 14 : 15, fontWeight: FontWeight.w700),
      ),
      onPressed: onTap,
      icon: icon == null ? null : Icon(icon, size: 18),
      label: Text(label),
    );
  }
}

/// Kuchli tasdiq: foydalanuvchi tasdiq so'zini yozmaguncha "O'chirish" yoqilmaydi.
class _DeleteAccountDialog extends StatefulWidget {
  const _DeleteAccountDialog();

  @override
  State<_DeleteAccountDialog> createState() => _DeleteAccountDialogState();
}

class _DeleteAccountDialogState extends State<_DeleteAccountDialog> {
  final _input = TextEditingController();

  @override
  void dispose() {
    _input.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final word = t.profileDeleteConfirmWord;
    final matches = _input.text.trim().toUpperCase() == word.toUpperCase();
    return AlertDialog(
      title: Text(t.profileDeleteTitle),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(t.profileDeleteMessage),
          const SizedBox(height: 16),
          Text(t.profileDeleteTypeHint(word), style: const TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          TextField(
            controller: _input,
            autofocus: true,
            textCapitalization: TextCapitalization.characters,
            onChanged: (_) => setState(() {}),
            decoration: profileInputDecoration(hint: word),
          ),
        ],
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context, false), child: Text(t.profileCancel)),
        TextButton(
          style: TextButton.styleFrom(foregroundColor: AppColors.danger),
          onPressed: matches ? () => Navigator.pop(context, true) : null,
          child: Text(t.profileDeleteButton),
        ),
      ],
    );
  }
}

/// Mehmon rejimi: kirish taklifi.
class _GuestProfile extends ConsumerWidget {
  const _GuestProfile();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(t.navProfile)),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: AppColors.primarySoft,
                    child: Icon(Icons.person_outline, color: AppColors.primary, size: 28),
                  ),
                  const SizedBox(height: 14),
                  Text(t.profileGuestTitle,
                      textAlign: TextAlign.center, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                  const SizedBox(height: 6),
                  Text(t.profileGuestText,
                      textAlign: TextAlign.center, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
                  const SizedBox(height: 18),
                  FilledButton(
                    onPressed: () => ref.read(authControllerProvider.notifier).requestSignIn(),
                    child: Text(t.login),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          const _LegalGroup(),
        ],
      ),
    );
  }
}

/// Til tanlash: O'zbekcha / Русский / Qurilma tili. Tanlov darhol qo'llanadi va saqlanadi.
Future<void> showLanguageSheet(BuildContext context, WidgetRef ref) async {
  final t = AppLocalizations.of(context);
  final current = ref.read(localeControllerProvider)?.languageCode;
  final picked = await showModalBottomSheet<String>(
    context: context,
    showDragHandle: true,
    builder: (ctx) {
      Widget option(String? code, String label) => ListTile(
            title: Text(label, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
            trailing: current == code ? Icon(Icons.check, color: AppColors.primary) : null,
            onTap: () => Navigator.pop(ctx, code ?? ''),
          );
      return SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
              child: Text(t.langSheetTitle, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            ),
            option('uz', t.langUzbek),
            option('ru', t.langRussian),
            option(null, t.langDevice),
            const SizedBox(height: 8),
          ],
        ),
      );
    },
  );
  if (picked == null) return; // yopildi
  await ref.read(localeControllerProvider.notifier).set(picked.isEmpty ? null : Locale(picked));
}
