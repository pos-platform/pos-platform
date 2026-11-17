import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import 'app_lock/passcode_settings_screen.dart';
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
import '../features/courier/courier_offers.dart';
import '../features/courier/courier_order_screen.dart';
import '../features/courier/courier_orders_screen.dart';
import '../features/courier/data/courier_session.dart';
import '../features/marketing/product_lists_screens.dart';
import '../features/marketing/referral_screen.dart';
import '../features/orders/order_placed_screen.dart';
import '../features/lists/family_list_screen.dart';
import '../features/recipes/recipe_screens.dart';
import '../features/orders/order_tracking_screen.dart';
import '../features/orders/orders_screen.dart';
import '../features/profile/address_edit_screen.dart';
import '../features/profile/addresses_screen.dart';
import '../features/profile/personal_info_screen.dart';
import '../features/profile/profile_screen.dart';
import '../features/returns/return_detail_screen.dart';
import '../features/returns/return_request_screen.dart';
import '../features/returns/returns_screen.dart';
import '../features/shell/home_shell.dart';
import 'config.dart';
import 'store_app/store_app.dart';
import 'store_app/store_unavailable_screen.dart';

final _rootKey = GlobalKey<NavigatorState>();

/// Ilova marshrutlari. Pastki menyu (Asosiy, Savatcha, Buyurtmalar, Profil) —
/// StatefulShellRoute: har tab o'z navigatsiya stekini saqlaydi.
///
/// Kirish holati bo'yicha yo'naltirish: sessiya o'qilguncha `/splash`; kirmagan — `/login`;
/// kirgan yoki mehmon ("Hozircha kirmasdan ko'rish") — ilova ichida.
///
/// Kuryer rejimi (`/courier*`) mijoz yo'naltirishidan mustaqil: kuryer sessiyasi bo'lsa ilova to'g'ridan-to'g'ri
/// `/courier` da ochiladi; kuryer chiqsa — mijoz `/login`, sessiya rad etilsa — `/courier/login`.
///
/// Do'kon rejimi (STORE_SLUG): do'kon ma'lumoti kelguncha `/splash`; do'kon ishlamasa — `/store-unavailable`;
/// marketplace do'konlar ro'yxati yo'q — `/home/stores` faqat shu do'kon filiallari (filial bitta bo'lsa — `/home`).
final routerProvider = Provider<GoRouter>((ref) {
  final authNotifier = ValueNotifier<AuthStatus>(ref.read(authControllerProvider).status);
  ref.listen(authControllerProvider, (_, next) => authNotifier.value = next.status);
  ref.onDispose(authNotifier.dispose);
  final courierNotifier = ValueNotifier<CourierAuthState>(ref.read(courierAuthProvider));
  ref.listen(courierAuthProvider, (_, next) => courierNotifier.value = next);
  ref.onDispose(courierNotifier.dispose);
  final storeNotifier = ValueNotifier<AsyncValue<StoreApp?>>(ref.read(storeAppProvider));
  ref.listen(storeAppProvider, (_, next) => storeNotifier.value = next);
  ref.onDispose(storeNotifier.dispose);

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/home',
    refreshListenable: Listenable.merge([authNotifier, courierNotifier, storeNotifier]),
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

      // Do'kon rejimi.
      const unavailable = '/store-unavailable';
      if (AppConfig.isStoreMode) {
        final store = storeNotifier.value;
        final app = store.value;
        if (app == null) {
          if (store.hasError && !store.isLoading) return loc == unavailable ? null : unavailable;
          // Yuklanmoqda (yoki "Qayta urinish" bosilgan) — joriy ekran yoki splash.
          return loc == unavailable || loc == '/splash' ? null : '/splash';
        }
        if (loc == '/home/stores' && app.singleBranch) return '/home';
      }
      if (loc == unavailable) return '/home';

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
      GoRoute(path: '/store-unavailable', builder: (_, _) => const StoreUnavailableScreen()),
      // Eski havolalar (push, profil) — "Bonus" tabiga.
      GoRoute(
        path: '/profile/bonus',
        redirect: (_, _) => '/bonus',
        routes: [
          GoRoute(path: ':organizationId', redirect: (_, s) => '/bonus/${s.pathParameters['organizationId']}'),
        ],
      ),
      GoRoute(path: '/courier/login', builder: (_, _) => const CourierLoginScreen()),
      GoRoute(
        path: '/courier',
        builder: (_, _) => const CourierOrdersScreen(),
        routes: [
          GoRoute(path: 'order/:id', builder: (_, s) => CourierOrderScreen(orderId: s.pathParameters['id']!)),
          // Kelishuv 10.6: taklif kartasi (push / SignalR dan ochiladi).
          GoRoute(
            path: 'offer',
            builder: (_, s) => CourierOfferScreen(
              offerId: s.uri.queryParameters['offerId'],
              orderId: s.uri.queryParameters['orderId'],
            ),
          ),
        ],
      ),
      // Checkout'dan yangi manzil — tab almashtirmasdan, butun ekran ustida ochiladi va natija bilan qaytadi.
      GoRoute(
        path: '/address-new',
        parentNavigatorKey: _rootKey,
        builder: (_, _) => const AddressEditScreen(),
      ),
      // Checkout'dan mavjud manzilni tahrirlash.
      GoRoute(
        path: '/address-edit/:id',
        parentNavigatorKey: _rootKey,
        builder: (_, s) => AddressEditScreen(addressId: s.pathParameters['id']),
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
                GoRoute(path: 'collection/:id', builder: (_, s) => CollectionScreen(collectionId: s.pathParameters['id']!)),
                GoRoute(path: 'discounts', builder: (_, _) => const DiscountsScreen()),
                GoRoute(path: 'favorites', builder: (_, _) => const FavoritesScreen()),
                GoRoute(path: 'recipes', builder: (_, _) => const RecipesScreen()),
                GoRoute(path: 'recipe/:id', builder: (_, s) => RecipeScreen(recipeId: s.pathParameters['id']!)),
              ],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/cart',
              builder: (_, _) => const CartScreen(),
              routes: [
                GoRoute(path: 'checkout', builder: (_, _) => const CheckoutScreen()),
                GoRoute(path: 'list', builder: (_, _) => const FamilyListScreen()),
              ],
            ),
          ]),
          // "Bonus" tabi — QR karta darhol (AppTab.bonus).
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/bonus',
              builder: (_, _) => const BonusCardScreen(),
              routes: [
                GoRoute(
                  path: ':organizationId',
                  builder: (_, s) => BonusCardScreen(organizationId: s.pathParameters['organizationId']),
                ),
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
                  routes: [
                    GoRoute(path: 'return', builder: (_, s) => ReturnRequestScreen(orderId: s.pathParameters['id']!)),
                    GoRoute(path: 'placed', builder: (_, s) => OrderPlacedScreen(orderId: s.pathParameters['id']!)),
                  ],
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
                GoRoute(path: 'favorites', builder: (_, _) => const FavoritesScreen()),
                GoRoute(path: 'passcode', builder: (_, _) => const PasscodeSettingsScreen()),
                GoRoute(path: 'referral', builder: (_, _) => const ReferralScreen()),
                // Kelishuv 10.5: qaytarishlar ro'yxati va holati.
                GoRoute(
                  path: 'returns',
                  builder: (_, _) => const ReturnsScreen(),
                  routes: [
                    GoRoute(path: ':id', builder: (_, s) => ReturnDetailScreen(returnId: s.pathParameters['id']!)),
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
