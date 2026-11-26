import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../l10n/app_localizations.dart';
import 'app_config_repository.dart';

/// Butun ilovani o'raydi (`MaterialApp.router(builder: ...)`):
/// - majburiy yangilash — to'liq ekran, faqat "Yangilash" tugmasi;
/// - texnik ishlar — to'liq ekran, "Qayta urinish" tugmasi;
/// - yangi versiya (majburiy emas) — sessiyada bir marta yopiladigan banner.
///
/// Konfiguratsiya yuklanayotganda yoki xato bo'lsa ilova odatdagidek ko'rsatiladi.
/// Navigator'dan YUQORIDA turadi — shuning uchun dialog emas, banner ishlatiladi.
class AppUpdateGate extends ConsumerStatefulWidget {
  const AppUpdateGate({super.key, required this.child});

  final Widget child;

  @override
  ConsumerState<AppUpdateGate> createState() => _AppUpdateGateState();
}

class _AppUpdateGateState extends ConsumerState<AppUpdateGate> {
  /// Yumshoq banner sessiyada bir marta: yopilgach qayta chiqmaydi.
  bool _softDismissed = false;

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(appConfigProvider).value;
    if (state == null) return widget.child;

    if (state.mustUpdate) {
      return _ForceUpdateScreen(storeUrl: state.config.storeUrl);
    }
    if (state.config.maintenance) {
      return _MaintenanceScreen(
        message: state.config.maintenanceMessage,
        onRetry: () => ref.invalidate(appConfigProvider),
      );
    }
    if (state.canUpdate && !_softDismissed) {
      return Stack(
        children: [
          widget.child,
          Positioned(
            left: 0,
            right: 0,
            top: 0,
            child: _SoftUpdateBanner(
              version: state.config.latestVersion,
              storeUrl: state.config.storeUrl,
              onDismiss: () => setState(() => _softDismissed = true),
            ),
          ),
        ],
      );
    }
    return widget.child;
  }
}

/// Do'kon sahifasini ochadi. URL berilmasa Android'da Play sahifasiga.
Future<void> _openStore(String? storeUrl) async {
  final url = storeUrl ??
      (defaultTargetPlatform == TargetPlatform.android
          ? 'https://play.google.com/store/apps/details?id=uz.xojamarket.app'
          : null);
  if (url == null) return;
  final uri = Uri.tryParse(url);
  if (uri == null) return;
  try {
    await launchUrl(uri, mode: LaunchMode.externalApplication);
  } catch (e) {
    debugPrint('Do\'kon ochilmadi: $e');
  }
}

class _FullScreenMessage extends StatelessWidget {
  const _FullScreenMessage({
    required this.icon,
    required this.title,
    required this.message,
    required this.buttonText,
    required this.onPressed,
  });

  final IconData icon;
  final String title;
  final String message;
  final String buttonText;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, size: 72, color: theme.colorScheme.primary),
                const SizedBox(height: 24),
                Text(title, style: theme.textTheme.headlineSmall, textAlign: TextAlign.center),
                const SizedBox(height: 12),
                Text(message, style: theme.textTheme.bodyLarge, textAlign: TextAlign.center),
                const SizedBox(height: 32),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(onPressed: onPressed, child: Text(buttonText)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ForceUpdateScreen extends StatelessWidget {
  const _ForceUpdateScreen({required this.storeUrl});

  final String? storeUrl;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return PopScope(
      canPop: false,
      child: _FullScreenMessage(
        icon: Icons.system_update,
        title: t.updateRequiredTitle,
        message: t.updateRequiredMessage,
        buttonText: t.updateButton,
        onPressed: () => _openStore(storeUrl),
      ),
    );
  }
}

class _MaintenanceScreen extends StatelessWidget {
  const _MaintenanceScreen({required this.message, required this.onRetry});

  final String? message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return _FullScreenMessage(
      icon: Icons.build_circle_outlined,
      title: t.updateMaintenanceTitle,
      message: message ?? t.updateMaintenanceMessage,
      buttonText: t.updateRetry,
      onPressed: onRetry,
    );
  }
}

class _SoftUpdateBanner extends StatelessWidget {
  const _SoftUpdateBanner({required this.version, required this.storeUrl, required this.onDismiss});

  final String? version;
  final String? storeUrl;
  final VoidCallback onDismiss;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final theme = Theme.of(context);
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
        child: Material(
          elevation: 4,
          borderRadius: BorderRadius.circular(16),
          color: theme.colorScheme.primaryContainer,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 8, 4),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(t.updateAvailableTitle, style: theme.textTheme.titleMedium),
                const SizedBox(height: 4),
                Text(
                  version == null ? t.updateAvailableMessage : t.updateAvailableVersion(version!),
                  style: theme.textTheme.bodyMedium,
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(onPressed: onDismiss, child: Text(t.updateLater)),
                    TextButton(
                      onPressed: () {
                        onDismiss();
                        _openStore(storeUrl);
                      },
                      child: Text(t.updateButton),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
