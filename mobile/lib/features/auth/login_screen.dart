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
import '../../core/widgets/circle_icon_button.dart';
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
  void initState() {
    super.initState();
    // "Kod olish" tugmasi raqam to'liq bo'lganda qizilga o'tadi.
    _phone.addListener(() => setState(() {}));
  }

  Widget _errorText() => _error == null
      ? const SizedBox.shrink()
      : Padding(
          padding: const EdgeInsets.only(top: 2),
          child: Text(_error!, style: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w600)),
        );

  @override
  Widget build(BuildContext context) {
    return PopScope(
      // OTP bosqichida "orqaga" — raqamni o'zgartirishga qaytadi.
      canPop: !_codeSent,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && _codeSent && !_busy) _changeNumber();
      },
      child: Scaffold(
        body: AnimatedSwitcher(
          duration: const Duration(milliseconds: 300),
          switchInCurve: const Cubic(.2, .8, .2, 1),
          transitionBuilder: (child, anim) => FadeTransition(
            opacity: anim,
            child: SlideTransition(
              position: Tween(begin: const Offset(0.07, 0), end: Offset.zero).animate(anim),
              child: child,
            ),
          ),
          child: _codeSent ? KeyedSubtree(key: const ValueKey('otp'), child: _otpStep()) : _phoneStep(),
        ),
      ),
    );
  }

  /// 1-bosqich (dizayn v3 LOGIN): tepada rasm, ustiga chiqib turgan varaq — logo, matn, telefon, "Kod olish".
  Widget _phoneStep() {
    final t = AppLocalizations.of(context);
    return LayoutBuilder(
      key: const ValueKey('phone'),
      builder: (context, constraints) => SingleChildScrollView(
        child: ConstrainedBox(
          constraints: BoxConstraints(minHeight: constraints.maxHeight),
          child: IntrinsicHeight(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                SizedBox(
                  height: 250 + MediaQuery.paddingOf(context).top,
                  child: Image.asset(
                    'assets/branding/login_hero.jpg',
                    fit: BoxFit.cover,
                    errorBuilder: (_, _, _) => const ColoredBox(color: AppColors.tintGreen),
                  ),
                ),
                Expanded(
                  child: Transform.translate(
                    offset: const Offset(0, -30),
                    child: Container(
                      decoration: const BoxDecoration(
                        color: AppColors.background,
                        borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
                      ),
                      padding: EdgeInsets.fromLTRB(24, 22, 24, (MediaQuery.paddingOf(context).bottom - 10).clamp(0.0, double.infinity)),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          // Do'kon rejimida — do'kon logosi va nomi.
                          Row(
                            children: [
                              const StoreLogo(size: 56),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  brandAppName(ref, t),
                                  style: AppTheme.display(24),
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Text(
                            t.loginSubtitle,
                            style: const TextStyle(
                              fontSize: 15,
                              height: 1.45,
                              fontWeight: FontWeight.w500,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 18),
                          Text(
                            t.phoneNumber,
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          _PhoneField(controller: _phone, enabled: !_busy, onSubmitted: () => _send()),
                          const SizedBox(height: 14),
                          _errorText(),
                          if (_error != null) const SizedBox(height: 12),
                          FilledButton(
                            onPressed: _busy ? null : () => _send(),
                            style: FilledButton.styleFrom(
                              minimumSize: const Size.fromHeight(58),
                              backgroundColor: _phoneComplete ? AppColors.primary : AppColors.segment,
                              foregroundColor: _phoneComplete ? Colors.white : AppColors.faint,
                            ),
                            child: _busy
                                ? const SizedBox(
                                    width: 22,
                                    height: 22,
                                    child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white),
                                  )
                                : Text(t.getCode),
                          ),
                          const SizedBox(height: 6),
                          TextButton(
                            onPressed: _busy ? null : () => ref.read(authControllerProvider.notifier).continueAsGuest(),
                            style: TextButton.styleFrom(
                              foregroundColor: AppColors.text,
                              minimumSize: const Size.fromHeight(44),
                              textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                            ),
                            child: Text(t.browseWithoutLogin),
                          ),
                          const Spacer(),
                          const SizedBox(height: 12),
                          const _LegalAgreement(),
                          TextButton(
                            onPressed: _busy ? null : () => context.go('/courier/login'),
                            child: Text(
                              t.courierEntry,
                              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  /// 2-bosqich (dizayn v3 OTP): orqaga, "Kodni kiriting", kataklar, qayta yuborish taymeri.
  Widget _otpStep() {
    final t = AppLocalizations.of(context);
    final resendText = t.resendIn(_fmt(_resendLeft));
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            CircleIconButton(
              icon: Icons.arrow_back_rounded,
              tooltip: t.changeNumber,
              onPressed: _busy ? null : _changeNumber,
            ),
            const SizedBox(height: 26),
            Text(t.enterCodeTitle, style: AppTheme.display(24)),
            const SizedBox(height: 10),
            Text(
              _sentChannel == 'telegram' ? t.codeSentTelegram(_maskedPhone) : t.codeSentSms(_maskedPhone),
              style: const TextStyle(
                fontSize: 15,
                height: 1.5,
                fontWeight: FontWeight.w500,
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: 26),
            _CodeBoxes(
              controller: _code,
              focusNode: _codeFocus,
              length: _codeLength,
              enabled: !_busy,
              onCompleted: _verify,
            ),
            const SizedBox(height: 26),
            if (_busy)
              const Padding(
                padding: EdgeInsets.only(bottom: 14),
                child: SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5)),
              ),
            _errorText(),
            if (_error != null) const SizedBox(height: 14),
            if (_resendLeft > 0)
              Text(
                resendText[0].toUpperCase() + resendText.substring(1),
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                  fontFeatures: [FontFeature.tabularFigures()],
                ),
              )
            else
              _LinkButton(
                label: t.resendCode,
                color: AppColors.primaryPressed,
                onTap: _busy ? null : () => _send(),
              ),
            if (_resendLeft == 0 && _sentChannel == 'telegram') ...[
              const SizedBox(height: 14),
              _LinkButton(
                label: t.sendViaSms,
                color: AppColors.textSecondary,
                onTap: _busy ? null : () => _send(preferSms: true),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Matnli havola-tugma (dizayn: 14px, w700, kamida 28px balandlik).
class _LinkButton extends StatelessWidget {
  const _LinkButton({required this.label, required this.color, required this.onTap});

  final String label;
  final Color color;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(8),
    child: ConstrainedBox(
      constraints: const BoxConstraints(minHeight: 32),
      child: Align(
        alignment: Alignment.centerLeft,
        widthFactor: 1,
        child: Text(label, style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: color)),
      ),
    ),
  );
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
      style: const TextStyle(fontSize: 12, color: AppColors.faint, height: 1.55),
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
      style: const TextStyle(
        fontSize: 17,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.4,
        fontFeatures: [FontFeature.tabularFigures()],
      ),
      decoration: const InputDecoration(
        hintText: '90 123 45 67',
        prefixIcon: Padding(
          padding: EdgeInsets.only(left: 18, right: 10),
          // Center — prefiks matni maydon balandligi bo'yicha o'rtada turishi uchun (aks holda tepaga yopishadi).
          child: Center(
            widthFactor: 1,
            child: Text('+998', style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700, color: AppColors.textSecondary)),
          ),
        ),
        prefixIconConstraints: BoxConstraints(minWidth: 0, minHeight: 58),
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
                  height: 62,
                  margin: EdgeInsets.only(right: i == widget.length - 1 ? 0 : 10),
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(18),
                    border: active ? Border.all(color: AppColors.primary, width: 2) : null,
                  ),
                  child: Text(i < text.length ? text[i] : '',
                      style: AppTheme.display(22)),
                ),
              );
            }),
          ),
        ),
      ],
    );
  }
}
