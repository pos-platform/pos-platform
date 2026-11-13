import 'dart:async';

import 'package:flutter/gestures.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/legal_links.dart';
import '../../core/store_app/store_branding.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import 'auth_controller.dart';

/// Kirish (dizayn: Login.dc.html) — telefon (+998), kod (server aytgan uzunlikda, odatda 5),
/// qayta yuborish taymeri, "Hozircha kirmasdan ko'rish".
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _phone = TextEditingController();
  final _code = TextEditingController();
  final _codeFocus = FocusNode();

  bool _busy = false;
  String? _error;
  String? _sentChannel; // null — kod hali yuborilmagan
  int _codeLength = 5;
  int _resendLeft = 0;
  Timer? _timer;

  String get _digits => _phone.text.replaceAll(RegExp(r'\D'), '');
  bool get _phoneComplete => _digits.length == 9;
  bool get _codeSent => _sentChannel != null;

  @override
  void dispose() {
    _timer?.cancel();
    _phone.dispose();
    _code.dispose();
    _codeFocus.dispose();
    super.dispose();
  }

  void _startTimer(int seconds) {
    _timer?.cancel();
    setState(() => _resendLeft = seconds);
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) return t.cancel();
      setState(() => _resendLeft = _resendLeft > 0 ? _resendLeft - 1 : 0);
      if (_resendLeft == 0) t.cancel();
    });
  }

  Future<void> _send({bool preferSms = false}) async {
    final t = AppLocalizations.of(context);
    if (!_phoneComplete) {
      setState(() => _error = t.invalidPhone);
      return;
    }
    await _run(() async {
      final sent = await ref.read(authControllerProvider.notifier).sendCode(_digits, preferSms: preferSms);
      setState(() {
        _sentChannel = sent.channel;
        _codeLength = sent.codeLength;
        _code.clear();
      });
      _startTimer(sent.resendAfterSeconds);
      _codeFocus.requestFocus();
    });
  }

  Future<void> _verify() async {
    if (_code.text.length != _codeLength) return;
    // Muvaffaqiyatda router o'zi asosiy ekranga o'tkazadi.
    await _run(() => ref.read(authControllerProvider.notifier).verify(_digits, _code.text));
    if (_error != null) _code.clear();
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await action();
    } catch (e) {
      final ex = ApiException.from(e);
      if (ex.retryAfterSeconds != null) _startTimer(ex.retryAfterSeconds!);
      if (mounted) setState(() => _error = ex.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _changeNumber() {
    _timer?.cancel();
    setState(() {
      _sentChannel = null;
      _resendLeft = 0;
      _code.clear();
      _error = null;
    });
  }

  String get _maskedPhone {
    final d = _digits;
    return d.length == 9 ? '+998 ${d.substring(0, 2)} *** ** ${d.substring(7)}' : '+998 $d';
  }

  String _fmt(int s) => '${s ~/ 60}:${(s % 60).toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(24, 32, 24, 24),
            child: ConstrainedBox(
              constraints: BoxConstraints(minHeight: constraints.maxHeight - 56),
              child: IntrinsicHeight(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Do'kon rejimida — do'kon logosi va nomi.
                    const Align(alignment: Alignment.centerLeft, child: StoreLogo()),
                    const SizedBox(height: 20),
                    Text(brandAppName(ref, t), style: textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800)),
                    const SizedBox(height: 8),
                    Text(t.loginSubtitle,
                        style: textTheme.bodyLarge?.copyWith(color: AppColors.textSecondary, height: 1.5)),
                    const SizedBox(height: 32),
                    Text(t.phoneNumber, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 10),
                    _PhoneField(controller: _phone, enabled: !_codeSent && !_busy, onSubmitted: () => _send()),
                    if (_codeSent) ...[
                      const SizedBox(height: 22),
                      Text(t.smsCode, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 10),
                      _CodeBoxes(
                        controller: _code,
                        focusNode: _codeFocus,
                        length: _codeLength,
                        enabled: !_busy,
                        onCompleted: _verify,
                      ),
                      const SizedBox(height: 10),
                      Text(
                        (_sentChannel == 'telegram' ? t.codeSentTelegram(_maskedPhone) : t.codeSentSms(_maskedPhone)) +
                            (_resendLeft > 0 ? ' · ${t.resendIn(_fmt(_resendLeft))}' : ''),
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                      ),
                      Wrap(
                        spacing: 4,
                        children: [
                          if (_resendLeft == 0)
                            TextButton(onPressed: _busy ? null : () => _send(), child: Text(t.resendCode)),
                          if (_resendLeft == 0 && _sentChannel == 'telegram')
                            TextButton(onPressed: _busy ? null : () => _send(preferSms: true), child: Text(t.sendViaSms)),
                          TextButton(onPressed: _busy ? null : _changeNumber, child: Text(t.changeNumber)),
                        ],
                      ),
                    ],
                    if (_error != null) ...[
                      const SizedBox(height: 12),
                      Text(_error!, style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w600)),
                    ],
                    const Spacer(),
                    const SizedBox(height: 24),
                    FilledButton(
                      onPressed: _busy
                          ? null
                          : _codeSent
                              ? (_code.text.length == _codeLength ? _verify : null)
                              : () => _send(),
                      child: _busy
                          ? const SizedBox(
                              width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                          : Text(_codeSent ? t.login : t.getCode),
                    ),
                    const SizedBox(height: 8),
                    TextButton(
                      onPressed: _busy ? null : () => ref.read(authControllerProvider.notifier).continueAsGuest(),
                      child: Text(t.browseWithoutLogin),
                    ),
                    const SizedBox(height: 6),
                    const _LegalAgreement(),
                    const SizedBox(height: 4),
                    TextButton(
                      onPressed: _busy ? null : () => context.go('/courier/login'),
                      child: Text(t.courierEntry, style: const TextStyle(color: AppColors.textSecondary)),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// "Davom etish orqali foydalanish shartlari va maxfiylik siyosatiga rozilik bildirasiz." —
/// havolalar bosilganda tashqi brauzerda ochiladi.
class _LegalAgreement extends StatefulWidget {
  const _LegalAgreement();

  @override
  State<_LegalAgreement> createState() => _LegalAgreementState();
}

class _LegalAgreementState extends State<_LegalAgreement> {
  late final _terms = TapGestureRecognizer()..onTap = () => openLegal(context, LegalLinks.termsUrl);
  late final _privacy = TapGestureRecognizer()..onTap = () => openLegal(context, LegalLinks.privacyUrl);

  @override
  void dispose() {
    _terms.dispose();
    _privacy.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final link = TextStyle(
      color: AppColors.primary,
      fontWeight: FontWeight.w700,
      decoration: TextDecoration.underline,
      decorationColor: AppColors.primary,
    );
    return Text.rich(
      TextSpan(children: [
        TextSpan(text: t.legalAgreePrefix),
        TextSpan(text: t.legalTermsLink, style: link, recognizer: _terms),
        TextSpan(text: t.legalAgreeMiddle),
        TextSpan(text: t.legalPrivacyLink, style: link, recognizer: _privacy),
        TextSpan(text: t.legalAgreeSuffix),
      ]),
      textAlign: TextAlign.center,
      style: const TextStyle(fontSize: 12, color: AppColors.textSecondary, height: 1.5),
    );
  }
}

/// "+998 | 90 123 45 67" — 9 raqam, avtomatik bo'shliqlar bilan.
class _PhoneField extends StatelessWidget {
  const _PhoneField({required this.controller, required this.enabled, required this.onSubmitted});

  final TextEditingController controller;
  final bool enabled;
  final VoidCallback onSubmitted;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      enabled: enabled,
      autofocus: true,
      keyboardType: TextInputType.phone,
      textInputAction: TextInputAction.done,
      autofillHints: const [AutofillHints.telephoneNumberNational],
      onSubmitted: (_) => onSubmitted(),
      inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(9), _UzPhoneFormatter()],
      style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600, letterSpacing: 0.5),
      decoration: const InputDecoration(
        hintText: '90 123 45 67',
        prefixIcon: Padding(
          padding: EdgeInsets.only(left: 16, right: 10),
          child: Text('+998', style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700, color: AppColors.text)),
        ),
        prefixIconConstraints: BoxConstraints(minWidth: 0, minHeight: AppTheme.controlHeight),
      ),
    );
  }
}

class _UzPhoneFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final d = newValue.text.replaceAll(RegExp(r'\D'), '');
    final b = StringBuffer();
    for (var i = 0; i < d.length; i++) {
      if (i == 2 || i == 5 || i == 7) b.write(' ');
      b.write(d[i]);
    }
    final text = b.toString();
    return TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length));
  }
}

/// Kod kataklari: bitta yashirin maydon + ko'rinadigan kataklar (SMS avtoto'ldirish ishlaydi).
class _CodeBoxes extends StatefulWidget {
  const _CodeBoxes({
    required this.controller,
    required this.focusNode,
    required this.length,
    required this.enabled,
    required this.onCompleted,
  });

  final TextEditingController controller;
  final FocusNode focusNode;
  final int length;
  final bool enabled;
  final VoidCallback onCompleted;

  @override
  State<_CodeBoxes> createState() => _CodeBoxesState();
}

class _CodeBoxesState extends State<_CodeBoxes> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_changed);
    widget.focusNode.addListener(_changed);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_changed);
    widget.focusNode.removeListener(_changed);
    super.dispose();
  }

  void _changed() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final text = widget.controller.text;
    return Stack(
      children: [
        // Yashirin maydon klaviatura va avtoto'ldirish uchun.
        Opacity(
          opacity: 0,
          child: SizedBox(
            height: AppTheme.controlHeight,
            child: TextField(
              controller: widget.controller,
              focusNode: widget.focusNode,
              enabled: widget.enabled,
              keyboardType: TextInputType.number,
              autofillHints: const [AutofillHints.oneTimeCode],
              inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(widget.length)],
              onChanged: (v) {
                if (v.length == widget.length) widget.onCompleted();
              },
            ),
          ),
        ),
        GestureDetector(
          onTap: () => widget.focusNode.requestFocus(),
          child: Row(
            children: List.generate(widget.length, (i) {
              final active = widget.focusNode.hasFocus && i == text.length.clamp(0, widget.length - 1);
              return Expanded(
                child: Container(
                  height: AppTheme.controlHeight,
                  margin: EdgeInsets.only(right: i == widget.length - 1 ? 0 : 10),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: active ? AppColors.primary : AppColors.border,
                      width: active ? 2 : 1,
                    ),
                  ),
                  child: Text(i < text.length ? text[i] : '',
                      style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
                ),
              );
            }),
          ),
        ),
      ],
    );
  }
}
