import 'package:cupertino_ui/cupertino_ui.dart' show GlobalCupertinoLocalizations;
import 'package:flutter_localizations/flutter_localizations.dart' show GlobalWidgetsLocalizations;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'core/app_update/app_update_gate.dart';
import 'core/locale/locale_controller.dart';
import 'core/push/push_service.dart';
import 'core/router.dart';
import 'core/store_app/store_app.dart';
import 'core/theme/app_theme.dart';
import 'l10n/app_localizations.dart';

class PosCloudApp extends ConsumerStatefulWidget {
  const PosCloudApp({super.key});

  @override
  ConsumerState<PosCloudApp> createState() => _PosCloudAppState();
}

class _PosCloudAppState extends ConsumerState<PosCloudApp> {
  @override
  void initState() {
    super.initState();
    // Push (FCM): Firebase sozlanmagan bo'lsa o'zi jimgina o'chadi.
    ref.read(pushServiceProvider).start();
  }

  @override
  Widget build(BuildContext context) {
    // Do'kon rejimi: brend rangi (AppColors.applyBrand allaqachon qo'llangan) o'zgarsa tema qayta quriladi.
    ref.watch(storeAppProvider.select((s) => s.value?.brandColor));
    final storeName = ref.watch(storeBrandingProvider).name;
    return MaterialApp.router(
      onGenerateTitle: (context) => storeName ?? AppLocalizations.of(context).appTitle,
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: ref.watch(routerProvider),
      // Majburiy yangilash / texnik ishlar / yangi versiya banneri — butun ilova ustida.
      builder: (context, child) => AppUpdateGate(child: child ?? const SizedBox.shrink()),
      // Profil → "Til" da tanlangan til; null — qurilma tili.
      locale: ref.watch(localeControllerProvider),
      // Standart til — o'zbekcha; qurilma tili ruscha bo'lsa ruscha.
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      localeResolutionCallback: (device, supported) =>
          supported.firstWhere((l) => l.languageCode == device?.languageCode, orElse: () => const Locale('uz')),
    );
  }
}
