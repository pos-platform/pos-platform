import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_colors.dart';
import 'store_app.dart';

/// Ilova nomi: do'kon rejimida do'kon ilovasi nomi, aks holda "PosCloud Market".
String brandAppName(WidgetRef ref, AppLocalizations t) => ref.watch(storeBrandingProvider).name ?? t.appTitle;

/// Brend yozuvi: "Xo‘ja" (to'q) + " market" (qizil) — logodagi kabi.
class BrandWordmark extends StatelessWidget {
  const BrandWordmark({super.key, this.fontSize = 18});

  final double fontSize;

  @override
  Widget build(BuildContext context) => Text.rich(
    TextSpan(children: [
      const TextSpan(text: 'Xo‘ja', style: TextStyle(color: Color(0xFF15172B))),
      TextSpan(text: ' market', style: TextStyle(color: AppColors.primary)),
    ]),
    maxLines: 1,
    overflow: TextOverflow.ellipsis,
    style: TextStyle(fontSize: fontSize, fontWeight: FontWeight.w800, letterSpacing: -0.2),
  );
}

/// Ilova belgisi: ilova ichidagi Xo‘ja Market logosi (`assets/branding/logo.png`, internetsiz ham ko'rinadi).
class StoreLogo extends StatelessWidget {
  const StoreLogo({super.key, this.size = 64});

  final double size;

  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/branding/logo.png',
    width: size,
    height: size,
    fit: BoxFit.contain,
    errorBuilder: (_, _, _) => Icon(Icons.shopping_bag, color: AppColors.primary, size: size),
  );
}
