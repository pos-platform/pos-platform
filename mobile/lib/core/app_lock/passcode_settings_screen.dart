import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_colors.dart';
import 'app_lock_controller.dart';
import 'pin_pad.dart';

/// Profil → "Kod-parol": yoqish/o'chirish, kodni almashtirish, telefon qulfi, avtomatik qulflash.
class PasscodeSettingsScreen extends ConsumerWidget {
  const PasscodeSettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final lock = ref.watch(appLockProvider);
    final notifier = ref.read(appLockProvider.notifier);

    return Scaffold(
      appBar: AppBar(title: Text(t.passcodeTitle)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          Text(t.passcodeHint, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
          const SizedBox(height: 16),
          if (!lock.enabled)
            FilledButton.icon(
              icon: const Icon(Icons.lock_outline),
              label: Text(t.passcodeEnable),
              onPressed: () async {
                final pin = await _askNewPin(context);
                if (pin != null) await notifier.setPin(pin);
              },
            )
          else ...[
            Card(
              clipBehavior: Clip.antiAlias,
              child: Column(
                children: [
                  ListTile(
                    leading: const Icon(Icons.password),
                    title: Text(t.passcodeChange),
                    onTap: () async {
                      if (!await _askCurrentPin(context, notifier) || !context.mounted) return;
                      final pin = await _askNewPin(context);
                      if (pin != null) await notifier.setPin(pin);
                    },
                  ),
                  if (lock.deviceAuthAvailable) ...[
                    const Divider(height: 1),
                    SwitchListTile(
                      secondary: const Icon(Icons.fingerprint),
                      title: Text(t.passcodeDeviceAuth),
                      subtitle: Text(t.passcodeDeviceAuthHint),
                      value: lock.useDeviceAuth,
                      onChanged: (v) async {
                        // Yoqishdan oldin bir marta tekshiramiz — telefonda qulf sozlanmagan bo'lishi mumkin.
                        if (v && !await notifier.confirmDevice(t.lockDeviceReason)) return;
                        await notifier.setUseDeviceAuth(v);
                      },
                    ),
                  ],
                  const Divider(height: 1),
                  ListTile(
                    leading: const Icon(Icons.timer_outlined),
                    title: Text(t.passcodeAutoLock),
                    trailing: Text(_timeoutLabel(t, lock.timeout),
                        style: const TextStyle(color: AppColors.textSecondary)),
                    onTap: () => _pickTimeout(context, ref),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            TextButton(
              style: TextButton.styleFrom(foregroundColor: AppColors.danger),
              onPressed: () async {
                if (await _askCurrentPin(context, notifier)) await notifier.disable();
              },
              child: Text(t.passcodeDisable),
            ),
          ],
        ],
      ),
    );
  }

  static String _timeoutLabel(AppLocalizations t, Duration d) {
    if (d == Duration.zero) return t.passcodeAutoLockNow;
    if (d.inMinutes >= 60) return t.passcodeAutoLockHours(d.inHours);
    return t.passcodeAutoLockMinutes(d.inMinutes);
  }

  Future<void> _pickTimeout(BuildContext context, WidgetRef ref) async {
    final t = AppLocalizations.of(context);
    final current = ref.read(appLockProvider).timeout;
    final picked = await showModalBottomSheet<Duration>(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final d in appLockTimeouts)
              ListTile(
                title: Text(_timeoutLabel(t, d)),
                trailing: d == current ? const Icon(Icons.check) : null,
                onTap: () => Navigator.of(context).pop(d),
              ),
          ],
        ),
      ),
    );
    if (picked != null) await ref.read(appLockProvider.notifier).setTimeout(picked);
  }

  Future<bool> _askCurrentPin(BuildContext context, AppLockController notifier) async {
    final ok = await Navigator.of(context).push<bool>(MaterialPageRoute(
      fullscreenDialog: true,
      builder: (_) => _PinEntryPage(mode: _PinMode.current, verify: notifier.verifyPin),
    ));
    return ok == true;
  }

  Future<String?> _askNewPin(BuildContext context) => Navigator.of(context).push<String>(MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) => const _PinEntryPage(mode: _PinMode.create),
      ));
}

enum _PinMode { current, create }

/// To'liq ekran PIN kiritish: joriy kodni tekshirish yoki yangi kod (ikki marta).
/// Natija: `current` — `true`; `create` — yangi PIN.
class _PinEntryPage extends StatefulWidget {
  const _PinEntryPage({required this.mode, this.verify});

  final _PinMode mode;
  final bool Function(String pin)? verify;

  @override
  State<_PinEntryPage> createState() => _PinEntryPageState();
}

class _PinEntryPageState extends State<_PinEntryPage> {
  final _padKey = GlobalKey<PinPadState>();
  String? _first;
  String? _error;

  void _onPin(String pin) {
    final t = AppLocalizations.of(context);
    if (widget.mode == _PinMode.current) {
      if (widget.verify!(pin)) return Navigator.of(context).pop(true);
      _padKey.currentState?.clear();
      return setState(() => _error = t.lockWrong);
    }
    if (_first == null) {
      _padKey.currentState?.clear();
      return setState(() {
        _first = pin;
        _error = null;
      });
    }
    if (pin == _first) return Navigator.of(context).pop(pin);
    _padKey.currentState?.clear();
    setState(() {
      _first = null;
      _error = t.pinMismatch;
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final title = switch (widget.mode) {
      _PinMode.current => t.pinCurrent,
      _PinMode.create => _first == null ? t.pinNew : t.pinRepeat,
    };
    return Scaffold(
      appBar: AppBar(),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: PinPad(key: _padKey, title: title, error: _error, onCompleted: _onPin),
          ),
        ),
      ),
    );
  }
}
