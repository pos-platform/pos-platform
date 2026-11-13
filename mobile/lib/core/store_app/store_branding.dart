import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../l10n/app_localizations.dart';
import '../config.dart';
import '../theme/app_colors.dart';
import 'store_app.dart';

/// Ilova nomi: do'kon rejimida do'kon ilovasi nomi, aks holda "PosCloud Market".
String brandAppName(WidgetRef ref, AppLocalizations t) => ref.watch(storeBrandingProvider).name ?? t.appTitle;

/// Ilova belgisi: do'kon rejimida do'kon logosi (yuklanmasa — standart belgi), marketplace'da standart belgi.
class StoreLogo extends ConsumerWidget {
  const StoreLogo({super.key, this.size = 64});

  final double size;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final radius = BorderRadius.circular(size * 0.31);
    final fallback = Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: AppColors.primary, borderRadius: radius),
      child: Icon(Icons.shopping_bag_outlined, color: Colors.white, size: size * 0.53),
    );
    final url = AppConfig.imageUrl(ref.watch(storeBrandingProvider).logoUrl);
    if (url == null) return fallback;
    return ClipRRect(
      borderRadius: radius,
      child: Image.network(
        url,
        width: size,
        height: size,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => fallback,
      ),
    );
  }
}
