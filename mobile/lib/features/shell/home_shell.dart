import 'package:material_ui/material_ui.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';

enum AppTab { home, cart, orders, profile }

/// Pastki menyu (dizayn: Main.dc.html — Asosiy, Savatcha, Buyurtmalar, Profil).
class HomeShell extends StatelessWidget {
  const HomeShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      body: shell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: shell.currentIndex,
        onDestinationSelected: (i) => shell.goBranch(i, initialLocation: i == shell.currentIndex),
        destinations: [
          NavigationDestination(icon: const Icon(Icons.home_outlined), selectedIcon: const Icon(Icons.home), label: t.navHome),
          NavigationDestination(
              icon: const Icon(Icons.shopping_cart_outlined), selectedIcon: const Icon(Icons.shopping_cart), label: t.navCart),
          NavigationDestination(
              icon: const Icon(Icons.receipt_long_outlined), selectedIcon: const Icon(Icons.receipt_long), label: t.navOrders),
          NavigationDestination(icon: const Icon(Icons.person_outline), selectedIcon: const Icon(Icons.person), label: t.navProfile),
        ],
      ),
    );
  }
}
