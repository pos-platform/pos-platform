import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/app_lock/app_lock_controller.dart';
import '../../core/api/api_exception.dart';
import '../../core/legal_links.dart';
import '../../core/locale/locale_controller.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
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

/// Profil (dizayn v3): foydalanuvchi sarlavhasi, 4 ta rangli plitka, sozlamalar ro'yxati,
/// pastda "Maxfiylik · Shartlar · Chiqish".
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
            padding: const EdgeInsets.fromLTRB(16, 6, 16, 24),
            children: [
              _UserHeader(user: user),
              if (!user.profileCompleted) ...[
                const SizedBox(height: 8),
                const _NameBanner(),
              ],
              const SizedBox(height: 12),
              GridView.count(
                crossAxisCount: 2,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                mainAxisSpacing: 10,
                crossAxisSpacing: 10,
                childAspectRatio: 1.45,
                children: [
                  _Tile(
                    icon: Icons.receipt_long_outlined,
                    label: t.navOrders,
                    background: AppColors.tintBlue,
                    onTap: () => context.go('/orders'),
                  ),
                  _Tile(
                    icon: Icons.qr_code_2,
                    label: t.bonusTitle,
                    sub: bonus == null ? null : formatBonusPoints(t, bonus),
                    background: AppColors.ink,
                    foreground: Colors.white,
                    onTap: () => context.go('/bonus'),
                  ),
                  _Tile(
                    icon: Icons.favorite_border,
                    label: t.favTitle,
                    background: AppColors.tintRose,
                    onTap: () => context.push('/profile/favorites'),
                  ),
                  _Tile(
                    icon: Icons.card_giftcard,
                    label: t.refTitle,
                    background: AppColors.tintYellow,
                    onTap: () => context.push('/profile/referral'),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              _MenuGroup(children: [
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
                  icon: Icons.assignment_return_outlined,
                  label: t.returnsTitle,
                  onTap: () => context.push('/profile/returns'),
                ),
                _MenuRow(
                  icon: Icons.support_agent,
                  label: t.supportTitle,
                  onTap: () => showSupportSheet(context, ref.read(selectedStoreProvider).value),
                ),
                _MenuRow(
                  icon: Icons.lock_outline,
                  label: t.passcodeTitle,
                  value: ref.watch(appLockProvider).enabled ? t.passcodeOn : t.passcodeOff,
                  onTap: () => context.push('/profile/passcode'),
                ),
                _MenuRow(
                  icon: Icons.language,
                  label: t.profileLanguage,
                  value: ref.watch(localeControllerProvider) == null ? t.langDevice : t.profileLanguageName,
                  onTap: () => showLanguageSheet(context, ref),
                ),
              ]),
              const SizedBox(height: 10),
              Wrap(
                alignment: WrapAlignment.center,
                children: [
                  _FooterLink(label: t.legalPrivacy, onTap: () => openLegal(context, LegalLinks.privacyUrl)),
                  _FooterLink(label: t.legalTerms, onTap: () => openLegal(context, LegalLinks.termsUrl)),
                  _FooterLink(label: t.signOut, danger: true, onTap: () => _confirmSignOut(context, ref)),
                ],
              ),
              _FooterLink(label: t.profileDeleteAccount, danger: true, onTap: () => _confirmDelete(context, ref)),
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
      builder: (ctx) => Dialog(
        insetPadding: const EdgeInsets.symmetric(horizontal: 32),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(t.profileSignOutTitle, style: AppTheme.display(17)),
            const SizedBox(height: 8),
            Text(t.profileSignOutMessage,
                style: const TextStyle(fontSize: 14, height: 1.5, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
            const SizedBox(height: 22),
            Row(children: [
              Expanded(
                child: FilledButton(
                  style: FilledButton.styleFrom(
                    minimumSize: const Size(0, 50),
                    backgroundColor: AppColors.chipSurface,
                    foregroundColor: AppColors.text,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  ),
                  onPressed: () => Navigator.pop(ctx, false),
                  child: Text(t.profileStay),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: FilledButton(
                  style: FilledButton.styleFrom(
                    minimumSize: const Size(0, 50),
                    backgroundColor: AppColors.ink,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  ),
                  onPressed: () => Navigator.pop(ctx, true),
                  child: Text(t.signOut),
                ),
              ),
            ]),
          ]),
        ),
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
      child: Padding(
        padding: const EdgeInsets.fromLTRB(4, 4, 4, 6),
        child: Row(
          children: [
            Container(
              width: 58,
              height: 58,
              alignment: Alignment.center,
              decoration: BoxDecoration(color: AppColors.ink, borderRadius: BorderRadius.circular(20)),
              child: Text(initialsOf(user.firstName, user.lastName, '?'), style: AppTheme.display(18, color: Colors.white)),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user.displayName == user.phone ? formatPhone(user.phone) : user.displayName,
                    style: AppTheme.display(18),
                  ),
                  const SizedBox(height: 3),
                  Text(formatPhone(user.phone),
                      style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textSecondary)),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, size: 22, color: AppColors.faint),
          ],
        ),
      ),
    );
  }
}

/// Rangli plitka: tepada belgi, pastda nom va izoh.
class _Tile extends StatelessWidget {
  const _Tile({
    required this.icon,
    required this.label,
    required this.background,
    required this.onTap,
    this.sub,
    this.foreground = AppColors.text,
  });

  final IconData icon;
  final String label;
  final String? sub;
  final Color background;
  final Color foreground;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
        color: background,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Icon(icon, size: 24, color: foreground),
                Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(label,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: foreground)),
                  if (sub != null)
                    Text(sub!,
                        style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: foreground.withValues(alpha: 0.65))),
                ]),
              ],
            ),
          ),
        ),
      );
}

class _FooterLink extends StatelessWidget {
  const _FooterLink({required this.label, required this.onTap, this.danger = false});

  final String label;
  final VoidCallback onTap;
  final bool danger;

  @override
  Widget build(BuildContext context) => TextButton(
        style: TextButton.styleFrom(
          foregroundColor: danger ? AppColors.primaryPressed : AppColors.textSecondary,
          minimumSize: const Size(0, 44),
          textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
        ),
        onPressed: onTap,
        child: Text(label),
      );
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

class _MenuGroup extends StatelessWidget {
  const _MenuGroup({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(AppTheme.radiusCard),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0) const Divider(height: 1, thickness: 1, indent: 16, endIndent: 16, color: AppColors.line),
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
        constraints: const BoxConstraints(minHeight: 56),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            children: [
              Icon(icon, size: 21, color: AppColors.textSecondary),
              const SizedBox(width: 14),
              Expanded(child: Text(label, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700))),
              if (value != null)
                Text(value!, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.faint)),
              const SizedBox(width: 4),
              const Icon(Icons.chevron_right, size: 20, color: Color(0xFFCFCCC5)),
            ],
          ),
        ),
      ),
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

/// Mehmon rejimi (dizayn v3 "darvoza"): belgi, sarlavha, izoh va qizil "Kirish"; pastda huquqiy havolalar.
class _GuestProfile extends ConsumerWidget {
  const _GuestProfile();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      body: SafeArea(
        child: Column(children: [
          Expanded(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 36),
                child: Column(mainAxisSize: MainAxisSize.min, children: [
                  Container(
                    width: 92,
                    height: 92,
                    decoration: BoxDecoration(color: AppColors.tintBlue, borderRadius: BorderRadius.circular(30)),
                    child: const Icon(Icons.person_outline, size: 40, color: AppColors.ink),
                  ),
                  const SizedBox(height: 18),
                  Text(t.profileGuestTitle, textAlign: TextAlign.center, style: AppTheme.display(18)),
                  const SizedBox(height: 8),
                  Text(t.profileGuestText,
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 14, height: 1.5, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
                  const SizedBox(height: 22),
                  FilledButton(
                    style: FilledButton.styleFrom(minimumSize: const Size(0, 52), padding: const EdgeInsets.symmetric(horizontal: 32)),
                    onPressed: () => ref.read(authControllerProvider.notifier).requestSignIn(),
                    child: Text(t.login),
                  ),
                ]),
              ),
            ),
          ),
          Wrap(alignment: WrapAlignment.center, children: [
            _FooterLink(label: t.legalPrivacy, onTap: () => openLegal(context, LegalLinks.privacyUrl)),
            _FooterLink(label: t.legalTerms, onTap: () => openLegal(context, LegalLinks.termsUrl)),
          ]),
          const SizedBox(height: 8),
        ]),
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
