import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

import '../theme/app_colors.dart';
import 'app_lock_controller.dart';

/// 4 xonali PIN kiritish: sarlavha, nuqtalar, xato matni va raqamli klaviatura.
/// Oxirgi raqam kiritilganda [onCompleted] chaqiriladi; tozalash — [PinPadState.clear].
class PinPad extends StatefulWidget {
  const PinPad({
    super.key,
    required this.title,
    required this.onCompleted,
    this.error,
    this.enabled = true,
    this.leftAction,
    this.footer,
  });

  final String title;
  final String? error;
  final bool enabled;
  final ValueChanged<String> onCompleted;

  /// Klaviaturaning chap pastki tugmasi (masalan, barmoq izi).
  final Widget? leftAction;
  final Widget? footer;

  @override
  State<PinPad> createState() => PinPadState();
}

class PinPadState extends State<PinPad> {
  String _pin = '';

  void clear() => setState(() => _pin = '');

  void _tap(String digit) {
    if (!widget.enabled || _pin.length >= appLockPinLength) return;
    HapticFeedback.selectionClick();
    setState(() => _pin += digit);
    if (_pin.length == appLockPinLength) {
      final pin = _pin;
      // Oxirgi nuqta to'lganini ko'rsatib, keyin tekshiramiz.
      Future.delayed(const Duration(milliseconds: 120), () {
        if (mounted) widget.onCompleted(pin);
      });
    }
  }

  void _backspace() {
    if (_pin.isEmpty) return;
    setState(() => _pin = _pin.substring(0, _pin.length - 1));
  }

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          widget.title,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: AppColors.text),
        ),
        const SizedBox(height: 24),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            for (var i = 0; i < appLockPinLength; i++)
              AnimatedContainer(
                duration: const Duration(milliseconds: 120),
                margin: const EdgeInsets.symmetric(horizontal: 10),
                width: 16,
                height: 16,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: i < _pin.length ? primary : Colors.transparent,
                  border: Border.all(color: i < _pin.length ? primary : AppColors.iconMuted, width: 2),
                ),
              ),
          ],
        ),
        SizedBox(
          height: 44,
          child: Center(
            child: widget.error == null
                ? null
                : Text(
                    widget.error!,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 14, color: AppColors.danger, fontWeight: FontWeight.w600),
                  ),
          ),
        ),
        for (final row in const [
          ['1', '2', '3'],
          ['4', '5', '6'],
          ['7', '8', '9'],
        ])
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [for (final d in row) _Key(label: d, onTap: () => _tap(d))],
          ),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            SizedBox(width: 88, height: 76, child: Center(child: widget.leftAction)),
            _Key(label: '0', onTap: () => _tap('0')),
            SizedBox(
              width: 88,
              height: 76,
              child: _pin.isEmpty
                  ? null
                  : IconButton(
                      onPressed: _backspace,
                      icon: const Icon(Icons.backspace_outlined, color: AppColors.text),
                    ),
            ),
          ],
        ),
        if (widget.footer != null) ...[const SizedBox(height: 8), widget.footer!],
      ],
    );
  }
}

class _Key extends StatelessWidget {
  const _Key({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
      child: Material(
        color: AppColors.chipSurface,
        shape: const CircleBorder(),
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: onTap,
          child: SizedBox(
            width: 68,
            height: 68,
            child: Center(
              child: Text(label, style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w600, color: AppColors.text)),
            ),
          ),
        ),
      ),
    );
  }
}
