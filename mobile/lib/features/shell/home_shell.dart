import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_colors.dart';
import '../auth/auth_controller.dart';
import '../bonus/data/bonus_card_cache.dart';
import '../orders/order_updates.dart';

import '../../l10n/app_localizations.dart';

/// Tartib router'dagi StatefulShellBranch'lar tartibi bilan bir xil.
enum AppTab { home, cart, bonus, orders, profile }

/// Pastki menyu: Asosiy, Savatcha, Bonus (o'rtada, QR), Buyurtmalar, Profil.
class HomeShell extends ConsumerWidget {
  const HomeShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Kirgan mijoz uchun buyurtma holatlari (SignalR) butun ilova davomida ulanib turadi.
    ref.watch(orderUpdatesProvider);
    // Bonus QR oldindan yuklanadi — tab ochilganda kod darhol ko'rinadi.
    ref.watch(bonusPrefetchProvider);
    final t = AppLocalizations.of(context);
    return Scaffold(
      body: shell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: shell.currentIndex,
        onDestinationSelected: (i) {
          // Mehmon "Bonus"ni bossa — kirish taklifi (karta telefon raqamiga bog'langan).
          if (i == AppTab.bonus.index && !ref.read(authControllerProvider).isSignedIn) {
            ref.read(authControllerProvider.notifier).requestSignIn();
            return;
          }
          shell.goBranch(i, initialLocation: i == shell.currentIndex);
        },
        destinations: [
          NavigationDestination(icon: const Icon(Icons.home_outlined), selectedIcon: const Icon(Icons.home), label: t.navHome),
          NavigationDestination(
              icon: const Icon(Icons.shopping_cart_outlined), selectedIcon: const Icon(Icons.shopping_cart), label: t.navCart),
          NavigationDestination(
            icon: const _BonusTabIcon(selected: false),
            selectedIcon: const _BonusTabIcon(selected: true),
            label: t.navBonus,
          ),
          NavigationDestination(
              icon: const Icon(Icons.receipt_long_outlined), selectedIcon: const Icon(Icons.receipt_long), label: t.navOrders),
          NavigationDestination(icon: const Icon(Icons.person_outline), selectedIcon: const Icon(Icons.person), label: t.navProfile),
        ],
      ),
    );
  }
}

/// "Bonus" tabi belgisi — boshqalardan ajralib turadi: brend rangidagi doira ichida QR.
class _BonusTabIcon extends StatelessWidget {
  const _BonusTabIcon({required this.selected});

  final bool selected;

  @override
  Widget build(BuildContext context) => Container(
        width: 40,
        height: 40,
        decoration: BoxDecoration(
          color: selected ? AppColors.primaryPressed : AppColors.primary,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(color: AppColors.primary.withValues(alpha: 0.3), blurRadius: 8, offset: const Offset(0, 2)),
          ],
        ),
        child: const Icon(Icons.qr_code_2, color: Colors.white, size: 22),
      );
}
