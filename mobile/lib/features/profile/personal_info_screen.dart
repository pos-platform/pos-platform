import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../auth/data/auth_models.dart';
import 'data/profile_repository.dart';
import 'widgets/profile_widgets.dart';

/// Shaxsiy ma'lumotlar (dizayn: PersonalInfo.dc.html).
///
/// [fromPrompt] — kirgandan keyin ism so'rash uchun ochilgan (router redirect): "Keyinroq" tugmasi bor,
/// orqaga / saqlash — bosh sahifaga (foydalanuvchi qamalib qolmaydi).
///
/// Keyinga: avatar rasmini o'zgartirish, telefon raqamni o'zgartirish (backendda endpoint yo'q).
class PersonalInfoScreen extends ConsumerWidget {
  const PersonalInfoScreen({super.key, this.fromPrompt = false});

  final bool fromPrompt;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final profile = ref.watch(profileProvider);
    final scaffold = Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ProfilePageHeader(
              title: t.profilePersonalInfo,
              trailing: fromPrompt
                  ? TextButton(onPressed: () => context.go('/home'), child: Text(t.namePromptSkip))
                  : null,
            ),
            if (fromPrompt)
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
                child: Text(t.namePromptHint, style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
              ),
            Expanded(
              child: switch (profile) {
                AsyncData(value: final p?) => _PersonalInfoForm(profile: p, fromPrompt: fromPrompt),
                AsyncError(:final error) => ProfileErrorView(
                    error: error,
                    onRetry: () => ref.invalidate(profileProvider),
                  ),
                _ => const Center(child: CircularProgressIndicator()),
              },
            ),
          ],
        ),
      ),
    );
    if (!fromPrompt) return scaffold;
    // So'rovdan ochilgan: orqaga — profil tabiga emas, bosh sahifaga.
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) context.go('/home');
      },
      child: scaffold,
    );
  }
}

class _PersonalInfoForm extends ConsumerStatefulWidget {
  const _PersonalInfoForm({required this.profile, this.fromPrompt = false});

  final ShopProfile profile;
  final bool fromPrompt;

  @override
  ConsumerState<_PersonalInfoForm> createState() => _PersonalInfoFormState();
}

class _PersonalInfoFormState extends ConsumerState<_PersonalInfoForm> {
  final _formKey = GlobalKey<FormState>();
  late final _firstName = TextEditingController(text: widget.profile.firstName ?? '');
  late final _lastName = TextEditingController(text: widget.profile.lastName ?? '');
  late final _email = TextEditingController(text: widget.profile.email ?? '');
  late DateTime? _birthDate = widget.profile.birthDate;
  late Gender? _gender = widget.profile.gender;
  bool _busy = false;

  static final _emailRe = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _email.dispose();
    super.dispose();
  }

  Future<void> _pickBirthDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _birthDate ?? DateTime(now.year - 25),
      firstDate: DateTime(1900),
      lastDate: now,
      initialEntryMode: DatePickerEntryMode.calendarOnly,
    );
    if (picked != null) setState(() => _birthDate = picked);
  }

  String? _trimOrNull(TextEditingController c) {
    final v = c.text.trim();
    return v.isEmpty ? null : v;
  }

  Future<void> _save() async {
    final t = AppLocalizations.of(context);
    if (!_formKey.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      final saved = await ref.read(profileRepositoryProvider).update(UpdateProfileRequest(
            firstName: _firstName.text.trim(),
            lastName: _trimOrNull(_lastName),
            birthDate: _birthDate,
            gender: _gender,
            email: _trimOrNull(_email),
          ));
      // Saqlangan sessiyadagi foydalanuvchi ham yangilanadi (sarlavha, ism so'rovi banneri).
      final current = ref.read(authControllerProvider).user;
      if (current != null) {
        await ref.read(authControllerProvider.notifier).updateUser(ShopUser(
              id: current.id,
              phone: current.phone,
              firstName: saved.firstName,
              lastName: saved.lastName,
              profileCompleted: saved.profileCompleted,
            ));
      }
      ref.invalidate(profileProvider);
      if (!mounted) return;
      showProfileSnack(context, t.profileSaved);
      if (widget.fromPrompt) {
        context.go('/home');
      } else {
        Navigator.of(context).maybePop();
      }
    } on ApiException catch (e) {
      if (mounted) showProfileSnack(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final p = widget.profile;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              children: [
                Row(
                  children: [
                    CircleAvatar(
                      radius: 36,
                      backgroundColor: AppColors.primary,
                      foregroundColor: Colors.white,
                      child: Text(
                        initialsOf(p.firstName, p.lastName, '?'),
                        style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800),
                      ),
                    ),
                    // Keyinga: "Rasmni o'zgartirish" (avatar yuklash backendda yo'q).
                  ],
                ),
                const SizedBox(height: 18),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: LabeledField(
                        label: t.profileFirstName,
                        controller: _firstName,
                        maxLength: 100,
                        textCapitalization: TextCapitalization.words,
                        validator: (v) => (v ?? '').trim().isEmpty ? t.profileFirstNameRequired : null,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: LabeledField(
                        label: t.profileLastName,
                        controller: _lastName,
                        maxLength: 100,
                        textCapitalization: TextCapitalization.words,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                _PhoneField(phone: p.phone, verified: p.phoneVerified),
                const SizedBox(height: 18),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: _FieldShell(
                        label: t.profileBirthDate,
                        child: InkWell(
                          borderRadius: BorderRadius.circular(12),
                          onTap: _pickBirthDate,
                          child: InputDecorator(
                            decoration: profileInputDecoration(),
                            child: Text(
                              _birthDate == null ? '—' : _formatDate(_birthDate!),
                              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                            ),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _FieldShell(
                        label: t.profileGender,
                        child: _GenderToggle(
                          value: _gender,
                          onChanged: (g) => setState(() => _gender = g),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text(t.profileBirthDateHint, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                const SizedBox(height: 18),
                LabeledField(
                  label: t.profileEmail,
                  labelSuffix: t.profileEmailHint,
                  controller: _email,
                  hint: 'ism@pochta.uz',
                  maxLength: 256,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.done,
                  validator: (v) {
                    final s = (v ?? '').trim();
                    return s.isEmpty || _emailRe.hasMatch(s) ? null : t.profileEmailInvalid;
                  },
                ),
              ],
            ),
          ),
        ),
        BottomActionBar(label: t.profileSave, busy: _busy, onPressed: _save),
      ],
    );
  }
}

class _FieldShell extends StatelessWidget {
  const _FieldShell({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(label, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700)),
        const SizedBox(height: 6),
        child,
      ],
    );
  }
}

class _PhoneField extends StatelessWidget {
  const _PhoneField({required this.phone, required this.verified});

  final String phone;
  final bool verified;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return _FieldShell(
      label: t.profilePhone,
      child: Container(
        height: 48,
        padding: const EdgeInsets.symmetric(horizontal: 14),
        decoration: BoxDecoration(
          color: AppColors.chipSurface,
          border: Border.all(color: AppColors.border),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(formatPhone(phone),
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600, color: AppColors.textMuted)),
            ),
            if (verified) ...[
              const Icon(Icons.check, size: 14, color: AppColors.primary),
              const SizedBox(width: 4),
              Text(t.profilePhoneVerified,
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppColors.primary)),
            ],
          ],
        ),
      ),
    );
  }
}

/// Erkak / Ayol segment. Tanlangan tugmani qayta bosish tanlovni bekor qiladi (jins ixtiyoriy).
class _GenderToggle extends StatelessWidget {
  const _GenderToggle({required this.value, required this.onChanged});

  final Gender? value;
  final ValueChanged<Gender?> onChanged;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Widget option(Gender g, String label) {
      final selected = value == g;
      return Expanded(
        child: Semantics(
          selected: selected,
          button: true,
          child: GestureDetector(
            behavior: HitTestBehavior.opaque,
            onTap: () => onChanged(selected ? null : g),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? AppColors.surface : Colors.transparent,
                borderRadius: BorderRadius.circular(9),
                boxShadow: selected
                    ? const [BoxShadow(color: Color(0x14000000), blurRadius: 3, offset: Offset(0, 1))]
                    : null,
              ),
              child: Text(
                label,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: selected ? FontWeight.w800 : FontWeight.w700,
                  color: selected ? AppColors.text : AppColors.textMuted,
                ),
              ),
            ),
          ),
        ),
      );
    }

    return Container(
      height: 48,
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(color: const Color(0xFFE9E7E0), borderRadius: BorderRadius.circular(12)),
      child: Row(children: [
        option(Gender.male, t.profileGenderMale),
        option(Gender.female, t.profileGenderFemale),
      ]),
    );
  }
}

/// "dd.MM.yyyy" (dizayndagi ko'rinish).
String _formatDate(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}.${d.month.toString().padLeft(2, '0')}.${d.year}';
