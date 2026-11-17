import 'package:material_ui/material_ui.dart';
import 'package:speech_to_text/speech_recognition_result.dart';
import 'package:speech_to_text/speech_to_text.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §3): ovozli qidiruv. Server o'zgarmaydi — matn oddiy qidiruvga beriladi.

/// Pastdan sheet: mikrofon tinglaydi, eshitilgan matn jonli ko'rinadi; tugagach matn qaytadi.
/// Tanib olish mavjud bo'lmasa yoki ruxsat berilmasa — snackbar va null.
Future<String?> showVoiceSearch(BuildContext context) async {
  final t = AppLocalizations.of(context);
  final messenger = ScaffoldMessenger.of(context);
  final speech = SpeechToText();
  var ok = false;
  try {
    ok = await speech.initialize();
  } catch (_) {
    ok = false;
  }
  if (!ok) {
    messenger.showSnackBar(SnackBar(content: Text(t.voiceUnavailable)));
    return null;
  }
  if (!context.mounted) return null;
  final result = await showModalBottomSheet<String>(
    context: context,
    builder: (_) => _VoiceSheet(speech: speech),
  );
  await speech.cancel();
  final text = result?.trim();
  return text == null || text.isEmpty ? null : text;
}

class _VoiceSheet extends StatefulWidget {
  const _VoiceSheet({required this.speech});

  final SpeechToText speech;

  @override
  State<_VoiceSheet> createState() => _VoiceSheetState();
}

class _VoiceSheetState extends State<_VoiceSheet> with SingleTickerProviderStateMixin {
  late final _wave = AnimationController(vsync: this, duration: const Duration(milliseconds: 900))..repeat();
  String _text = '';
  bool _done = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _start());
  }

  Future<void> _start() async {
    final lang = Localizations.localeOf(context).languageCode;
    // Qurilmada ilova tili bo'lsa — o'sha, aks holda standart til.
    String? localeId;
    try {
      final locales = await widget.speech.locales();
      localeId = locales.where((l) => l.localeId.toLowerCase().startsWith(lang)).firstOrNull?.localeId;
    } catch (_) {}
    widget.speech.statusListener = (status) {
      if (status == 'done' || status == 'notListening') _finish();
    };
    await widget.speech.listen(
      onResult: _onResult,
      listenOptions: SpeechListenOptions(
        localeId: localeId,
        listenFor: const Duration(seconds: 12),
        pauseFor: const Duration(seconds: 3),
        partialResults: true,
        cancelOnError: true,
      ),
    );
  }

  void _onResult(SpeechRecognitionResult r) {
    if (!mounted) return;
    setState(() => _text = r.recognizedWords);
    if (r.finalResult) _finish();
  }

  void _finish() {
    if (_done || !mounted) return;
    _done = true;
    Navigator.of(context).pop(_text);
  }

  @override
  void dispose() {
    _wave.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 12, 24, 28),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          SizedBox(
            height: 64,
            child: AnimatedBuilder(
              animation: _wave,
              builder: (_, _) => Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < 5; i++)
                    Container(
                      width: 8,
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      height: 16 + 40 * (0.5 + 0.5 * _phase(_wave.value, i)),
                      decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(4)),
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(_text.isEmpty ? t.voiceListening : _text,
              textAlign: TextAlign.center, style: AppTheme.display(18)),
          const SizedBox(height: 18),
          TextButton(
            onPressed: () {
              widget.speech.stop();
              _finish();
            },
            child: Text(MaterialLocalizations.of(context).okButtonLabel),
          ),
        ]),
      ),
    );
  }

  /// Har ustun uchun siljigan to'lqin (-1..1).
  static double _phase(double v, int i) {
    final x = (v + i * 0.18) % 1.0;
    return (x < 0.5 ? x * 4 - 1 : 3 - x * 4);
  }
}
