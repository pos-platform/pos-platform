import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/shell/home_shell.dart';
import '../features/shell/placeholder_screen.dart';

/// Ilova marshrutlari. Pastki menyu (Asosiy, Savatcha, Buyurtmalar, Profil) —
/// StatefulShellRoute: har tab o'z navigatsiya stekini saqlaydi.
/// Kirish (4.2), katalog (4.3) va boshqalar keyingi qadamlarda shu yerga qo'shiladi.
final routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/home',
    routes: [
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => HomeShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/home', builder: (_, _) => const PlaceholderScreen(tab: AppTab.home)),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/cart', builder: (_, _) => const PlaceholderScreen(tab: AppTab.cart)),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/orders', builder: (_, _) => const PlaceholderScreen(tab: AppTab.orders)),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/profile', builder: (_, _) => const PlaceholderScreen(tab: AppTab.profile)),
          ]),
        ],
      ),
    ],
  );
});
