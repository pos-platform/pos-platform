import 'dart:async';
import 'dart:convert';
import 'dart:io' show Platform;

import 'package:dio/dio.dart' show Options;
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart' show Locale, WidgetsBinding;

import '../../features/auth/auth_controller.dart';
import '../../features/catalog/store_selection.dart';
import '../../features/courier/courier_offers.dart';
import '../../features/courier/data/courier_session.dart';
import '../../features/marketing/data/marketing_models.dart';
import '../../features/marketing/shop_link.dart';
import '../../features/returns/data/returns_repository.dart';
import '../../l10n/app_localizations.dart';
import '../api/api_client.dart';
import '../locale/locale_controller.dart';
import '../router.dart';

/// Push bildirishnomalar (FCM).
///
/// Firebase sozlamasi (`google-services.json` / `GoogleService-Info.plist`) bo'lmasa
/// `Firebase.initializeApp` xato beradi — push jimgina o'chadi, ilova odatdagidek ishlaydi.
///
/// Oqim: ruxsat so'rash → token → kirgan bo'lsa `POST /api/me/devices`; token yoki til o'zgarsa qayta
/// ro'yxatdan o'tadi; chiqishda token logout so'rovida yuboriladi (AuthController.signOut).
/// Xabar ma'lumoti `{type: 'order', orderId}` — bosilganda `/orders/<id>` ochiladi; bosqich 9 turlari
/// (`back_in_stock`, `cart_reminder`, `campaign`, `referral_reward`) — [_openFromData].
class PushService {
  PushService(this._ref);

  final Ref _ref;

  /// Android kanali — backend `Push:Fcm:AndroidChannelId` bilan bir xil bo'lishi shart.
  static const channelId = 'orders';

  final _local = FlutterLocalNotificationsPlugin();
  final _orderEvents = StreamController<String>.broadcast();

  bool _started = false;
  bool _enabled = false;
  String? _token;
  String? _registered; // "token|til" — bir xil ma'lumot qayta yuborilmaydi
  String? _courierRegistered; // kuryer uchun "token|sessiya"
  Future<void> Function()? _pending; // kirgandan keyin bajariladigan navigatsiya

  /// Joriy FCM token (push o'chiq bo'lsa null).
  String? get token => _token;

  bool get enabled => _enabled;

  /// Buyurtma haqida push kelganda (ilova ochiq) — buyurtma ID. Buyurtmalar ekrani tinglab yangilanishi mumkin.
  Stream<String> get orderEvents => _orderEvents.stream;

  /// Bir marta, ilova ishga tushganda (PosCloudApp).
  Future<void> start() async {
    if (_started) return;
    _started = true;
    if (kIsWeb || !(Platform.isAndroid || Platform.isIOS)) return;
    try {
      await Firebase.initializeApp();
    } catch (e) {
      debugPrint('Push o‘chiq: Firebase sozlanmagan ($e)');
      return;
    }
    try {
      await _setup();
      _enabled = true;
    } catch (e) {
      debugPrint('Push ishga tushmadi: $e');
    }
  }

  Future<void> _setup() async {
    await _initLocalNotifications();
    final messaging = FirebaseMessaging.instance;

    // Android 13+ POST_NOTIFICATIONS va iOS ruxsati.
    await messaging.requestPermission();
    // iOS: ilova ochiq bo'lganda tizim o'zi ko'rsatadi; Android'da — mahalliy bildirishnoma.
    await messaging.setForegroundNotificationPresentationOptions(alert: true, badge: true, sound: true);

    FirebaseMessaging.onMessage.listen(_onForegroundMessage);
    FirebaseMessaging.onMessageOpenedApp.listen((m) => _openFromData(m.data));
    final initial = await messaging.getInitialMessage();
    if (initial != null) _openFromData(initial.data);

    // firebase_messaging 17: getToken/onTokenRefresh deprecated (yangi register()/onRegistered FID beradi).
    // Backend FCM HTTP v1 `message.token` ga registration token yuboradi — shuning uchun hozircha eski API.
    // ignore: deprecated_member_use
    messaging.onTokenRefresh.listen((t) {
      _token = t;
      _register();
      _registerCourier();
    });

    // Kuryer kirganda — token yuboriladi va kutilayotgan push (taklif) ochiladi.
    _ref.listen(courierAuthProvider, (prev, next) {
      if (next.isSignedIn && prev?.session?.token != next.session?.token) {
        _courierRegistered = null;
        _registerCourier();
        _flushCourierPending();
      }
      if (!next.isSignedIn) _courierRegistered = null;
    });

    _ref.listen(authControllerProvider, (prev, next) {
      if (next.isSignedIn && prev?.isSignedIn != true) {
        _registered = null;
        _register();
        _flushPending();
      }
      if (!next.isSignedIn) _registered = null;
    });
    _ref.listen(localeControllerProvider, (_, _) => _register());

    try {
      // ignore: deprecated_member_use
      _token = await messaging.getToken();
    } catch (e) {
      // iOS: APNs tokeni hali yo'q (simulyator / Push capability yoqilmagan) — onTokenRefresh kutiladi.
      debugPrint('FCM token olinmadi: $e');
    }
    await _register();
    await _registerCourier();
  }

  Future<void> _initLocalNotifications() async {
    await _local.initialize(
      settings: const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        // Ruxsatni FirebaseMessaging.requestPermission so'raydi.
        iOS: DarwinInitializationSettings(
          requestAlertPermission: false,
          requestBadgePermission: false,
          requestSoundPermission: false,
        ),
      ),
      onDidReceiveNotificationResponse: (r) {
        final p = r.payload;
        if (p == null || p.isEmpty) return;
        if (!p.startsWith('{')) return _openOrder(p); // eski format: faqat buyurtma ID
        try {
          final data = jsonDecode(p);
          if (data is Map) _openFromData(Map<String, dynamic>.from(data));
        } catch (_) {}
      },
    );
    final t = lookupAppLocalizations(Locale(LocaleController.languageCode));
    await _local
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(AndroidNotificationChannel(
          channelId,
          t.pushChannelName,
          description: t.pushChannelDescription,
          importance: Importance.high,
        ));
  }

  Future<void> _register() async {
    final token = _token;
    if (token == null || !_ref.read(authControllerProvider).isSignedIn) return;
    final locale = LocaleController.languageCode;
    final key = '$token|$locale';
    if (_registered == key) return;
    try {
      await _ref.read(dioProvider).post<void>('/api/me/devices', data: {
        'token': token,
        'platform': Platform.isIOS ? 'ios' : 'android',
        'locale': locale,
      });
      _registered = key;
    } catch (e) {
      // Keyingi kirish / token yangilanishida qayta urinadi.
      debugPrint('Push token ro‘yxatdan o‘tmadi: $e');
    }
  }

  void _onForegroundMessage(RemoteMessage m) {
    final orderId = _orderIdOf(m.data);
    if (orderId != null) _orderEvents.add(orderId);
    // Ilova ochiq va kuryer rejimida — bildirishnoma o'rniga darhol to'liq ekranli taklif.
    if (m.data['type'] == 'courier_offer' && _ref.read(courierAuthProvider).isSignedIn) {
      _ref.read(courierOfferAlertProvider).incoming(orderId: _str(m.data['orderId']), offerId: _str(m.data['offerId']));
      return;
    }
    if (m.data['type'] == 'return_status') _ref.invalidate(returnsListProvider);

    final n = m.notification;
    if (n == null || !Platform.isAndroid) return; // iOS'da tizim o'zi ko'rsatadi
    final t = lookupAppLocalizations(Locale(LocaleController.languageCode));
    _local.show(
      id: m.messageId?.hashCode ?? DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title: n.title,
      body: n.body,
      // Bosilganda ochiladigan joy uchun to'liq data (JSON).
      payload: jsonEncode(m.data),
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          channelId,
          t.pushChannelName,
          channelDescription: t.pushChannelDescription,
          importance: Importance.high,
          priority: Priority.high,
        ),
      ),
    );
  }

  static String? _orderIdOf(Map<String, dynamic> data) {
    if (data['type'] != 'order') return null;
    final id = data['orderId']?.toString();
    return id == null || id.isEmpty ? null : id;
  }

  static String? _str(Object? v) {
    final s = v?.toString().trim();
    return s == null || s.isEmpty ? null : s;
  }

  /// Bosilgan push → ekran (kelishuv 9.2/9.4): `order` → buyurtma, `back_in_stock` → mahsulot,
  /// `cart_reminder` → savatcha, `campaign` → havola, `referral_reward` → bonus karta.
  void _openFromData(Map<String, dynamic> data) {
    final id = _orderIdOf(data);
    if (id != null) return _openOrder(id);
    final router = _ref.read(routerProvider);
    final branchId = _str(data['branchId']);

    // Push boshqa filial haqida bo'lsa — avval o'sha filial tanlanadi.
    Future<void> selectBranch() async {
      if (branchId != null && _ref.read(storeSelectionProvider).value != branchId) {
        await _ref.read(storeSelectionProvider.notifier).select(branchId);
      }
    }

    switch (data['type']) {
      case 'courier_offer':
        // Kelishuv 10.6: kuryer rejimida taklif kartasi ochiladi.
        _scheduleCourier(() async => _ref
            .read(courierOfferAlertProvider)
            .open(orderId: _str(data['orderId']), offerId: _str(data['offerId'])));
      case 'return_status':
        // Kelishuv 10.5: qaytarish holati ekrani.
        final returnId = _str(data['returnId']);
        _schedule(() async => router.go(returnId == null ? '/profile/returns' : '/profile/returns/$returnId'));
      case 'back_in_stock':
        final productId = _str(data['productId']);
        if (productId == null) return;
        _schedule(() async {
          await selectBranch();
          router.go('/home');
          router.push('/home/product/$productId');
        });
      case 'cart_reminder':
        _schedule(() async => router.go('/cart'));
      case 'referral_reward':
        _schedule(() async => router.go('/bonus'));
      case 'campaign':
        final link = ShopLink(ShopLinkType.parse(data['linkType']), _str(data['linkValue']));
        _schedule(() async {
          await selectBranch();
          if (!link.isActionable) return router.go('/home');
          router.go('/home');
          await openShopLink(router, _ref.read(homeCategoryRequestProvider.notifier), link);
        });
    }
  }

  /// Sessiya hali o'qilmagan bo'lsa (ilova push bosilib ochilgan) — kirgandan keyin ochiladi.
  void _openOrder(String orderId) => _schedule(() async => _ref.read(routerProvider).go('/orders/$orderId'));

  void _schedule(Future<void> Function() action) {
    _pending = action;
    _flushPending();
  }

  Future<void> Function()? _courierPending;

  /// Kuryer sessiyasi tiklangach bajariladi (ilova push bosilib ochilgan bo'lsa).
  void _scheduleCourier(Future<void> Function() action) {
    _courierPending = action;
    _flushCourierPending();
  }

  void _flushCourierPending() {
    final action = _courierPending;
    if (action == null || !_ref.read(courierAuthProvider).isSignedIn) return;
    _courierPending = null;
    WidgetsBinding.instance.addPostFrameCallback((_) => action());
    WidgetsBinding.instance.scheduleFrame();
  }

  /// Kuryer qurilma tokeni (kelishuv 10.6): `PUT /api/courier/device-token`. Bir xil token qayta yuborilmaydi.
  Future<void> _registerCourier() async {
    final token = _token;
    final session = _ref.read(courierAuthProvider).session;
    if (token == null || session == null || session.isExpired) return;
    final key = '$token|${session.token.hashCode}';
    if (_courierRegistered == key) return;
    try {
      await createBaseDio().put<void>('/api/courier/device-token',
          data: {'token': token, 'platform': Platform.isIOS ? 'ios' : 'android'},
          options: Options(headers: {'Authorization': 'Bearer ${session.token}'}));
      _courierRegistered = key;
    } catch (e) {
      debugPrint('Kuryer push tokeni yuborilmadi: $e');
    }
  }

  /// Kuryer chiqishida token serverdan o'chiriladi (xato e'tiborsiz, chiqishni to'xtatmaydi).
  Future<void> unregisterCourier(String courierToken) async {
    final token = _token;
    _courierRegistered = null;
    if (token == null) return;
    try {
      await createBaseDio()
          .delete<void>('/api/courier/device-token',
              data: {'token': token}, options: Options(headers: {'Authorization': 'Bearer $courierToken'}))
          .timeout(const Duration(seconds: 5));
    } catch (e) {
      debugPrint('Kuryer push tokeni o‘chirilmadi: $e');
    }
  }

  void _flushPending() {
    final action = _pending;
    if (action == null || !_ref.read(authControllerProvider).isSignedIn) return;
    _pending = null;
    // Router redirect'i (splash → home) tugagach navigatsiya qilamiz.
    WidgetsBinding.instance.addPostFrameCallback((_) => action());
    WidgetsBinding.instance.scheduleFrame();
  }
}

final pushServiceProvider = Provider<PushService>((ref) => PushService(ref));
