import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/config.dart';
import '../../core/store_app/store_branding.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';

/// Saqlangan sessiya (va do'kon rejimida do'kon ma'lumoti) o'qilguncha ko'rinadi.
/// Do'kon rejimida — do'kon logosi va nomi.
class SplashScreen extends ConsumerWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!AppConfig.isStoreMode) {
      return Scaffold(body: Center(child: CircularProgressIndicator(color: AppColors.primary)));
    }
    return Scaffold(
      body: Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const StoreLogo(size: 88),
          const SizedBox(height: 16),
          Text(brandAppName(ref, AppLocalizations.of(context)),
              textAlign: TextAlign.center, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
          const SizedBox(height: 24),
          CircularProgressIndicator(color: AppColors.primary),
        ]),
      ),
    );
  }
}
