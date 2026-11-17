import 'package:material_ui/material_ui.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_colors.dart';
import '../auth/auth_controller.dart';
import '../bonus/data/bonus_card_cache.dart';
import '../cart/cart_controller.dart';
import '../orders/order_updates.dart';

import '../../l10n/app_localizations.dart';

/// Tartib router'dagi StatefulShellBranch'lar tartibi bilan bir xil.
enum AppTab { home, cart, bonus, orders, profile }

/// Pastki menyu (dizayn v3): to'q, suzuvchi, burchaklari yumaloq panel; o'rtada qizil QR (Bonus),
/// savatchada mahsulotlar soni.
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
    final cartCount = ref.watch(cartControllerProvider.select((c) => c.count));

    void go(int i) {
      // Mehmon "Bonus"ni bossa — kirish taklifi (karta telefon raqamiga bog'langan).
      if (i == AppTab.bonus.index && !ref.read(authControllerProvider).isSignedIn) {
        ref.read(authControllerProvider.notifier).requestSignIn();
        return;
      }
      shell.goBranch(i, initialLocation: i == shell.currentIndex);
    }

    final items = [
      (Icons.home_outlined, Icons.home_rounded, t.navHome, 0),
      (Icons.shopping_basket_outlined, Icons.shopping_basket_rounded, t.navCart, cartCount),
      null, // Bonus
      (Icons.receipt_long_outlined, Icons.receipt_long_rounded, t.navOrders, 0),
      (Icons.person_outline_rounded, Icons.person_rounded, t.navProfile, 0),
    ];

    return Scaffold(
      body: shell,
      // Telefonda katta shrift tanlangan bo'lsa ham yozuvlar ("Buyurtmalar") bir qatorga sig'sin.
      bottomNavigationBar: MediaQuery(
        data: MediaQuery.of(context).copyWith(
          textScaler: MediaQuery.textScalerOf(context).clamp(maxScaleFactor: 1.0),
        ),
        child: SafeArea(
          top: false,
          minimum: const EdgeInsets.only(bottom: 12),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(14, 6, 14, 0),
            child: Container(
              height: 66,
              padding: const EdgeInsets.symmetric(horizontal: 6),
              decoration: BoxDecoration(
                color: AppColors.ink,
                borderRadius: BorderRadius.circular(26),
                boxShadow: [
                  BoxShadow(
                    color: AppColors.ink.withValues(alpha: 0.28),
                    blurRadius: 30,
                    offset: const Offset(0, 12),
                  ),
                ],
              ),
              child: Row(
                children: [
                  for (var i = 0; i < items.length; i++)
                    Expanded(
                      child: items[i] == null
                          ? _BonusTab(label: t.navBonus, onTap: () => go(i))
                          : _Tab(
                              icon: items[i]!.$1,
                              selectedIcon: items[i]!.$2,
                              label: items[i]!.$3,
                              badge: items[i]!.$4,
                              selected: shell.currentIndex == i,
                              onTap: () => go(i),
                            ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    required this.badge,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final int badge;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected ? Colors.white : Colors.white.withValues(alpha: 0.5);
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: InkResponse(
        onTap: onTap,
        radius: 32,
        child: Stack(
          clipBehavior: Clip.none,
          alignment: Alignment.center,
          children: [
            Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                AnimatedSlide(
                  duration: const Duration(milliseconds: 200),
                  offset: Offset(0, selected ? -0.08 : 0),
                  child: Icon(selected ? selectedIcon : icon, size: 24, color: color),
                ),
                const SizedBox(height: 3),
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: color),
                ),
              ],
            ),
            if (badge > 0)
              Positioned(
                top: 9,
                left: 0,
                right: 0,
                child: Align(
                  alignment: const Alignment(0.45, 0),
                  child: Container(
                    constraints: const BoxConstraints(minWidth: 18),
                    height: 18,
                    padding: const EdgeInsets.symmetric(horizontal: 5),
                    decoration: BoxDecoration(
                      color: AppColors.primary,
                      borderRadius: BorderRadius.circular(9),
                      border: Border.all(color: AppColors.ink, width: 2),
                    ),
                    // widthFactor: 1 — belgi matn kengligida qoladi (Container.alignment uni butun kenglikka cho'zardi).
                    child: Center(
                      widthFactor: 1,
                      child: Text(
                      badge > 99 ? '99+' : '$badge',
                      style: const TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w800,
                        color: Colors.white,
                      ),
                    ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// "Bonus" tabi — o'rtada brend rangidagi yumaloq kvadrat ichida QR.
class _BonusTab extends StatelessWidget {
  const _BonusTab({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    label: label,
    child: Center(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          width: 50,
          height: 50,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(18),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary.withValues(alpha: 0.45),
                blurRadius: 16,
                offset: const Offset(0, 6),
              ),
            ],
          ),
          child: const Icon(Icons.qr_code_2_rounded, color: Colors.white, size: 25),
        ),
      ),
    ),
  );
}
