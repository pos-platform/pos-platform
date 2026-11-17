import 'package:material_ui/material_ui.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';

/// Ichki sahifa sarlavhasi: oq kvadrat "Orqaga" tugmasi + sarlavha (dizayn: PersonalInfo, Addresses).
class ProfilePageHeader extends StatelessWidget {
  const ProfilePageHeader({super.key, required this.title, this.trailing, this.showBack = true});

  final String title;
  final Widget? trailing;

  /// Orqaga tugmasi (tab ildizida — yo'q).
  final bool showBack;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(showBack ? 16 : 20, 6, 16, 8),
      child: Row(
        children: [
          if (showBack) ...[
          Semantics(
            button: true,
            label: MaterialLocalizations.of(context).backButtonTooltip,
            child: Material(
              color: AppColors.surface,
              shape: const CircleBorder(),
              clipBehavior: Clip.antiAlias,
              child: InkWell(
                onTap: () => Navigator.of(context).maybePop(),
                child: const SizedBox(width: 44, height: 44, child: Icon(Icons.arrow_back, size: 22, color: AppColors.text)),
              ),
            ),
          ),
          const SizedBox(width: 12),
          ],
          Expanded(
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: 46),
              child: Align(
                alignment: Alignment.centerLeft,
                child: Text(title, style: AppTheme.display(showBack ? 18 : 24)),
              ),
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}

/// Ustida yorliq bo'lgan maydon (dizayn: 13px qalin yorliq, 48px input, 12px burchak).
class LabeledField extends StatelessWidget {
  const LabeledField({
    super.key,
    required this.label,
    required this.controller,
    this.hint,
    this.labelSuffix,
    this.keyboardType,
    this.validator,
    this.maxLength,
    this.fillColor,
    this.textCapitalization = TextCapitalization.none,
    this.textInputAction = TextInputAction.next,
  });

  final String label;
  final String? labelSuffix;
  final TextEditingController controller;
  final String? hint;
  final TextInputType? keyboardType;
  final FormFieldValidator<String>? validator;
  final int? maxLength;
  final Color? fillColor;
  final TextCapitalization textCapitalization;
  final TextInputAction textInputAction;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text.rich(
          TextSpan(
            text: label,
            children: [
              if (labelSuffix != null)
                TextSpan(
                  text: ' — $labelSuffix',
                  style: const TextStyle(fontWeight: FontWeight.w500, color: AppColors.textSecondary),
                ),
            ],
          ),
          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 6),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          textInputAction: textInputAction,
          textCapitalization: textCapitalization,
          validator: validator,
          maxLength: maxLength,
          style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
          decoration: profileInputDecoration(hint: hint, fillColor: fillColor),
        ),
      ],
    );
  }
}

InputDecoration profileInputDecoration({String? hint, Color? fillColor}) {
  final border = OutlineInputBorder(
    borderRadius: BorderRadius.circular(12),
    borderSide: BorderSide(color: fillColor == null ? AppColors.border : Colors.transparent),
  );
  return InputDecoration(
    hintText: hint,
    counterText: '',
    isDense: true,
    filled: true,
    fillColor: fillColor ?? AppColors.surface,
    contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
    border: border,
    enabledBorder: border,
    focusedBorder: border.copyWith(borderSide: BorderSide(color: AppColors.primary, width: 2)),
  );
}

/// Pastdagi oq panel ichidagi asosiy tugma (dizayn: 56px, "Saqlash").
class BottomActionBar extends StatelessWidget {
  const BottomActionBar({super.key, required this.label, required this.onPressed, this.busy = false});

  final String label;
  final VoidCallback? onPressed;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(top: BorderSide(color: AppColors.borderSoft)),
      ),
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
      child: SafeArea(
        top: false,
        child: FilledButton(
          onPressed: busy ? null : onPressed,
          child: busy
              ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
              : Text(label),
        ),
      ),
    );
  }
}

/// Yuklash xatosi + "Qayta urinish".
class ProfileErrorView extends StatelessWidget {
  const ProfileErrorView({super.key, required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              ApiException.from(error).message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary, fontSize: 15),
            ),
            const SizedBox(height: 12),
            TextButton(onPressed: onRetry, child: Text(t.retry)),
          ],
        ),
      ),
    );
  }
}

void showProfileSnack(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}

/// Ismdan bosh harflar (avatar uchun).
String initialsOf(String? firstName, String? lastName, String fallback) {
  final parts = [firstName, lastName].where((s) => s != null && s.trim().isNotEmpty).map((s) => s!.trim()[0]);
  final initials = parts.join().toUpperCase();
  return initials.isEmpty ? fallback : initials;
}

/// "+998901234567" → "+998 90 123 45 67".
String formatPhone(String phone) {
  final d = phone.replaceAll(RegExp(r'\D'), '');
  if (d.length != 12 || !d.startsWith('998')) return phone;
  return '+998 ${d.substring(3, 5)} ${d.substring(5, 8)} ${d.substring(8, 10)} ${d.substring(10)}';
}
