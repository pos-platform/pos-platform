import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/courier/data/courier_session.dart';
import '../../l10n/app_localizations.dart';
import '../theme/app_colors.dart';
import 'app_lock_controller.dart';
import 'pin_pad.dart';

/// Butun ilovani o'raydi: qulflangan bo'lsa ilova ustida qulf ekrani.
/// Ilova ostida qoladi (holati yo'qolmaydi), faqat ko'rinmaydi va bosilmaydi.
class AppLockGate extends ConsumerWidget {
  const AppLockGate({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lock = ref.watch(appLockProvider);
    // Sozlama o'qilguncha ilovani ko'rsatmaymiz — qulf oldidan ichki ekran "miltillab" qolmasin.
    if (!lock.ready) return const ColoredBox(color: AppColors.background);
    if (!lock.locked) return child;
    return Stack(
      children: [
        Offstage(child: child),
        const Positioned.fill(child: _LockScreen()),
      ],
    );
  }
}

class _LockScreen extends ConsumerStatefulWidget {
  const _LockScreen();

  @override
  ConsumerState<_LockScreen> createState() => _LockScreenState();
}

class _LockScreenState extends ConsumerState<_LockScreen> {
  final _padKey = GlobalKey<PinPadState>();
  String? _error;
  bool _forgot = false;
  Timer? _ticker;

  @override
  void initState() {
    super.initState();
    // Telefon qulfi yoqilgan bo'lsa — darhol barmoq izi / Face ID oynasi.
    WidgetsBinding.instance.addPostFrameCallback((_) => _deviceAuth());
    _startTickerIfBlocked();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  Future<void> _deviceAuth() async {
    if (!mounted || !ref.read(appLockProvider).canUseDeviceAuth) return;
    await ref.read(appLockProvider.notifier).unlockWithDevice(AppLocalizations.of(context).lockDeviceReason);
  }

  Future<void> _onPin(String pin) async {
    final ok = await ref.read(appLockProvider.notifier).unlockWithPin(pin);
    if (ok || !mounted) return;
    _padKey.currentState?.clear();
    setState(() => _error = AppLocalizations.of(context).lockWrong);
    _startTickerIfBlocked();
  }

  /// Bloklangan paytda soniyalarni sanab turish.
  void _startTickerIfBlocked() {
    _ticker?.cancel();
    if (!ref.read(appLockProvider.notifier).isBlocked) return;
    _ticker = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) return t.cancel();
      if (!ref.read(appLockProvider.notifier).isBlocked) {
        t.cancel();
        _error = null;
      }
      setState(() {});
    });
  }

  Future<void> _signOutAndReset() async {
    await ref.read(appLockProvider.notifier).disable();
    if (ref.read(courierAuthProvider).isSignedIn) await ref.read(courierAuthProvider.notifier).logout();
    if (ref.read(authControllerProvider).isSignedIn) await ref.read(authControllerProvider.notifier).signOut();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final lock = ref.watch(appLockProvider);
    final notifier = ref.read(appLockProvider.notifier);
    final blocked = notifier.isBlocked;
    final left = blocked ? lock.blockedUntil!.difference(DateTime.now()).inSeconds + 1 : 0;

    return Material(
      color: AppColors.background,
      child: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
            child: _forgot
                ? _ForgotView(onCancel: () => setState(() => _forgot = false), onConfirm: _signOutAndReset)
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.lock_outline, size: 40, color: Theme.of(context).colorScheme.primary),
                      const SizedBox(height: 16),
                      PinPad(
                        key: _padKey,
                        title: t.lockTitle,
                        enabled: !blocked,
                        error: blocked ? t.lockBlocked(left) : _error,
                        onCompleted: _onPin,
                        leftAction: lock.canUseDeviceAuth
                            ? IconButton(
                                iconSize: 32,
                                tooltip: t.lockUseDevice,
                                onPressed: _deviceAuth,
                                icon: const Icon(Icons.fingerprint, color: AppColors.text),
                              )
                            : null,
                        footer: TextButton(
                          onPressed: () => setState(() => _forgot = true),
                          child: Text(t.lockForgot),
                        ),
                      ),
                    ],
                  ),
          ),
        ),
      ),
    );
  }
}

class _ForgotView extends StatelessWidget {
  const _ForgotView({required this.onCancel, required this.onConfirm});

  final VoidCallback onCancel;
  final VoidCallback onConfirm;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const Icon(Icons.help_outline, size: 40, color: AppColors.textSecondary),
        const SizedBox(height: 16),
        Text(t.lockForgot,
            textAlign: TextAlign.center, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
        const SizedBox(height: 12),
        Text(t.lockForgotText,
            textAlign: TextAlign.center, style: const TextStyle(fontSize: 15, color: AppColors.textSecondary)),
        const SizedBox(height: 24),
        SizedBox(
          width: double.infinity,
          child: FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: onConfirm,
            child: Text(t.lockForgotConfirm),
          ),
        ),
        const SizedBox(height: 8),
        SizedBox(width: double.infinity, child: TextButton(onPressed: onCancel, child: Text(t.lockBack))),
      ],
    );
  }
}
