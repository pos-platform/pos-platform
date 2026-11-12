import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import 'home_shell.dart';

export 'home_shell.dart' show AppTab;

/// Vaqtinchalik ekran — tegishli qadam (4.3–4.6) bajarilganda almashtiriladi.
class PlaceholderScreen extends StatelessWidget {
  const PlaceholderScreen({super.key, required this.tab});

  final AppTab tab;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final title = switch (tab) {
      AppTab.home => t.appTitle,
      AppTab.cart => t.navCart,
      AppTab.orders => t.navOrders,
      AppTab.profile => t.navProfile,
    };
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Center(
        child: Text(t.comingSoon, style: const TextStyle(color: AppColors.textSecondary, fontSize: 16)),
      ),
    );
  }
}
