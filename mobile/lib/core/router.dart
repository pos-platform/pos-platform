import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../features/auth/auth_controller.dart';
import '../features/auth/login_screen.dart';
import '../features/auth/splash_screen.dart';
import '../features/bonus/bonus_card_screen.dart';
import '../features/cart/cart_screen.dart';
import '../features/catalog/home_screen.dart';
import '../features/catalog/product_screen.dart';
import '../features/catalog/stores_screen.dart';
import '../features/checkout/checkout_screen.dart';
import '../features/courier/courier_login_screen.dart';
import '../features/courier/courier_order_screen.dart';
import '../features/courier/courier_orders_screen.dart';
import '../features/courier/data/courier_session.dart';
import '../features/orders/order_tracking_screen.dart';
import '../features/orders/orders_screen.dart';
import '../features/profile/address_edit_screen.dart';
import '../features/profile/addresses_screen.dart';
import '../features/profile/personal_info_screen.dart';
import '../features/profile/profile_screen.dart';
import '../features/shell/home_shell.dart';

final _rootKey = GlobalKey<NavigatorState>();

/// Ilova marshrutlari. Pastki menyu (Asosiy, Savatcha, Buyurtmalar, Profil) —
/// StatefulShellRoute: har tab o'z navigatsiya stekini saqlaydi.
///
/// Kirish holati bo'yicha yo'naltirish: sessiya o'qilguncha `/splash`; kirmagan — `/login`;
/// kirgan yoki mehmon ("Hozircha kirmasdan ko'rish") — ilova ichida.
///
/// Kuryer rejimi (`/courier*`) mijoz yo'naltirishidan mustaqil: kuryer sessiyasi bo'lsa ilova to'g'ridan-to'g'ri
/// `/courier` da ochiladi; kuryer chiqsa — mijoz `/login`, sessiya rad etilsa — `/courier/login`.
final routerProvider = Provider<GoRouter>((ref) {
  final authNotifier = ValueNotifier<AuthStatus>(ref.read(authControllerProvider).status);
  ref.listen(authControllerProvider, (_, next) => authNotifier.value = next.status);
  ref.onDispose(authNotifier.dispose);
  final courierNotifier = ValueNotifier<CourierAuthState>(ref.read(courierAuthProvider));
  ref.listen(courierAuthProvider, (_, next) => courierNotifier.value = next);
  ref.onDispose(courierNotifier.dispose);

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/home',
    refreshListenable: Listenable.merge([authNotifier, courierNotifier]),
    redirect: (context, state) {
      final status = authNotifier.value;
      final loc = state.matchedLocation;

      // Kuryer rejimi.
      final courier = courierNotifier.value;
      final isCourierLogin = loc == '/courier/login';
      final isCourierArea = loc == '/courier' || loc.startsWith('/courier/');
      if (courier.status == CourierAuthStatus.unknown) return loc == '/splash' ? null : '/splash';
      if (courier.isSignedIn) return isCourierArea && !isCourierLogin ? null : '/courier';
      if (isCourierLogin) return null;
      if (isCourierArea) return courier.expired ? '/courier/login' : '/login';

      if (status == AuthStatus.unknown) return loc == '/splash' ? null : '/splash';
      if (status == AuthStatus.signedOut) return loc == '/login' ? null : '/login';
      // Endigina kirgan (login ekranidan) va ismi yo'q — bir marta ism so'raymiz. "Keyinroq" bilan o'tkazib yuboriladi;
      // keyingi ochilishlarda (splash orqali) qayta majburlanmaydi — profildagi banner qoladi.
      if (loc == '/login' && status == AuthStatus.signedIn && ref.read(authControllerProvider).user?.profileCompleted == false) {
        return '/profile/personal?prompt=1';
      }
      if (loc == '/login' || loc == '/splash') return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/courier/login', builder: (_, _) => const CourierLoginScreen()),
      GoRoute(
        path: '/courier',
        builder: (_, _) => const CourierOrdersScreen(),
        routes: [
          GoRoute(path: 'order/:id', builder: (_, s) => CourierOrderScreen(orderId: s.pathParameters['id']!)),
        ],
      ),
      // Checkout'dan yangi manzil — tab almashtirmasdan, butun ekran ustida ochiladi va natija bilan qaytadi.
      GoRoute(
        path: '/address-new',
        parentNavigatorKey: _rootKey,
        builder: (_, _) => const AddressEditScreen(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => HomeShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/home',
              builder: (_, _) => const HomeScreen(),
              routes: [
                GoRoute(path: 'stores', builder: (_, _) => const StoresScreen()),
                GoRoute(path: 'product/:id', builder: (_, s) => ProductScreen(productId: s.pathParameters['id']!)),
              ],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/cart',
              builder: (_, _) => const CartScreen(),
              routes: [
                GoRoute(path: 'checkout', builder: (_, _) => const CheckoutScreen()),
              ],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/orders',
              builder: (_, _) => const OrdersScreen(),
              routes: [
                GoRoute(
                  path: ':id',
                  builder: (_, s) => OrderTrackingScreen(orderId: s.pathParameters['id']!),
                ),
              ],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/profile',
              builder: (_, _) => const ProfileScreen(),
              routes: [
                GoRoute(
                  path: 'personal',
                  builder: (_, s) => PersonalInfoScreen(fromPrompt: s.uri.queryParameters['prompt'] == '1'),
                ),
                GoRoute(
                  path: 'bonus',
                  builder: (_, _) => const BonusCardScreen(),
                  routes: [
                    GoRoute(
                      path: ':organizationId',
                      builder: (_, s) => BonusCardScreen(organizationId: s.pathParameters['organizationId']),
                    ),
                  ],
                ),
                GoRoute(
                  path: 'addresses',
                  builder: (_, _) => const AddressesScreen(),
                  routes: [
                    GoRoute(path: 'new', builder: (_, _) => const AddressEditScreen()),
                    GoRoute(
                      path: ':id',
                      builder: (_, s) => AddressEditScreen(addressId: s.pathParameters['id']),
                    ),
                  ],
                ),
              ],
            ),
          ]),
        ],
      ),
    ],
  );
});
