import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../catalog/data/catalog_models.dart';
import '../catalog/data/catalog_repository.dart';

/// "Yordam" kontaktlari (kelishuv 9.3): `supportPhone` (bo'lmasa do'kon telefoni) va `supportTelegram`.
({String? phone, String? telegram}) supportContacts(ShopStore? s) =>
    (phone: s?.supportPhone ?? s?.phone, telegram: s?.supportTelegram);

Future<void> _launch(BuildContext context, Uri uri) async {
  final messenger = ScaffoldMessenger.maybeOf(context);
  var ok = false;
  try {
    ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
  } catch (_) {}
  if (!ok) messenger?.showSnackBar(SnackBar(content: Text(uri.toString())));
}

Future<void> callSupport(BuildContext context, String phone) =>
    _launch(context, Uri(scheme: 'tel', path: phone.replaceAll(RegExp(r'[^\d+]'), '')));

Future<void> openTelegram(BuildContext context, String username) =>
    _launch(context, Uri.parse('https://t.me/${Uri.encodeComponent(username.replaceFirst('@', ''))}'));

/// Profildagi "Yordam" — tanlov oynasi (qo'ng'iroq / Telegram).
Future<void> showSupportSheet(BuildContext context, ShopStore? store) {
  final t = AppLocalizations.of(context);
  final c = supportContacts(store);
  return showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    builder: (ctx) => SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 4),
            child: Text(t.supportTitle, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
            child: Text(t.supportText, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
          ),
          if (c.phone != null)
            ListTile(
              leading: Icon(Icons.phone_outlined, color: AppColors.primary),
              title: Text(t.supportCall, style: const TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text(c.phone!),
              onTap: () {
                Navigator.pop(ctx);
                callSupport(context, c.phone!);
              },
            ),
          if (c.telegram != null)
            ListTile(
              leading: Icon(Icons.send_outlined, color: AppColors.primary),
              title: Text(t.supportTelegram, style: const TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text('@${c.telegram}'),
              onTap: () {
                Navigator.pop(ctx);
                openTelegram(context, c.telegram!);
              },
            ),
          if (c.phone == null && c.telegram == null)
            Padding(
              padding: const EdgeInsets.all(20),
              child: Text(t.supportNone, style: const TextStyle(color: AppColors.textSecondary)),
            ),
          const SizedBox(height: 8),
        ],
      ),
    ),
  );
}

/// Buyurtma sahifasidagi "Yordam" kartasi — do'kon kontaktlari bo'lmasa ko'rinmaydi.
class SupportCard extends ConsumerWidget {
  const SupportCard({super.key, required this.branchId});

  final String branchId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (branchId.isEmpty) return const SizedBox.shrink();
    final t = AppLocalizations.of(context);
    final c = supportContacts(ref.watch(storeProvider(branchId)).value);
    if (c.phone == null && c.telegram == null) return const SizedBox.shrink();
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.support_agent, color: AppColors.primary),
              const SizedBox(width: 10),
              Expanded(
                child: Text(t.supportTitle, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              if (c.phone != null)
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => callSupport(context, c.phone!),
                    icon: const Icon(Icons.phone_outlined, size: 18),
                    label: Text(t.supportCall),
                  ),
                ),
              if (c.phone != null && c.telegram != null) const SizedBox(width: 10),
              if (c.telegram != null)
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => openTelegram(context, c.telegram!),
                    icon: const Icon(Icons.send_outlined, size: 18),
                    label: const Text('Telegram'),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
