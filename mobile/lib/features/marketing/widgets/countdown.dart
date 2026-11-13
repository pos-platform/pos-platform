import 'dart:async';

import 'package:material_ui/material_ui.dart';

import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/marketing_models.dart';

/// "2 kun 4 soat" / "4 soat 12 daq" / "12 daq". Tugagan bo'lsa — null.
String? formatCountdown(AppLocalizations t, Duration left) {
  final p = countdownParts(left);
  if (p == null) return null;
  if (p.days > 0) return t.cdDaysHours(p.days, p.hours);
  if (p.hours > 0) return t.cdHoursMinutes(p.hours, p.minutes);
  return t.cdMinutes(p.minutes);
}

/// Ortga sanash belgisi ("Aksiya tugashiga 2 kun 4 soat"). Har 30 s yangilanadi; tugagach yo'qoladi.
class CountdownChip extends StatefulWidget {
  const CountdownChip({super.key, required this.endsAt, required this.label, this.compact = false});

  final DateTime endsAt;

  /// Matn qolipi: vaqt matnini oladi ("Aksiya tugashiga {time}").
  final String Function(AppLocalizations t, String time) label;
  final bool compact;

  @override
  State<CountdownChip> createState() => _CountdownChipState();
}

class _CountdownChipState extends State<CountdownChip> {
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final text = formatCountdown(t, widget.endsAt.difference(DateTime.now()));
    if (text == null) {
      _timer?.cancel();
      return const SizedBox.shrink();
    }
    return Container(
      padding: EdgeInsets.symmetric(horizontal: widget.compact ? 8 : 12, vertical: widget.compact ? 4 : 7),
      decoration: BoxDecoration(color: const Color(0xFFFDF0E6), borderRadius: BorderRadius.circular(999)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.timer_outlined, size: widget.compact ? 14 : 16, color: AppColors.accent),
          const SizedBox(width: 6),
          Flexible(
            child: Text(
              widget.label(t, text),
              style: TextStyle(
                fontSize: widget.compact ? 12 : 13,
                fontWeight: FontWeight.w700,
                color: AppColors.accent,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
