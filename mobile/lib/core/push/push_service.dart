import 'dart:async';
import 'dart:io' show Platform;

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart' show Locale, WidgetsBinding;

import '../../features/auth/auth_controller.dart';
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
/// Xabar ma'lumoti `{type: 'order', orderId}` — bosilganda `/orders/<id>` ochiladi.
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
  String? _pendingOrderId;

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
        final id = r.payload;
        if (id != null && id.isNotEmpty) _openOrder(id);
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

    final n = m.notification;
    if (n == null || !Platform.isAndroid) return; // iOS'da tizim o'zi ko'rsatadi
    final t = lookupAppLocalizations(Locale(LocaleController.languageCode));
    _local.show(
      id: m.messageId?.hashCode ?? DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title: n.title,
      body: n.body,
      payload: orderId,
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

  void _openFromData(Map<String, dynamic> data) {
    final id = _orderIdOf(data);
    if (id != null) _openOrder(id);
  }

  /// Sessiya hali o'qilmagan bo'lsa (ilova push bosilib ochilgan) — kirgandan keyin ochiladi.
  void _openOrder(String orderId) {
    _pendingOrderId = orderId;
    _flushPending();
  }

  void _flushPending() {
    final id = _pendingOrderId;
    if (id == null || !_ref.read(authControllerProvider).isSignedIn) return;
    _pendingOrderId = null;
    // Router redirect'i (splash → home) tugagach navigatsiya qilamiz.
    WidgetsBinding.instance.addPostFrameCallback((_) => _ref.read(routerProvider).go('/orders/$id'));
    WidgetsBinding.instance.scheduleFrame();
  }
}

final pushServiceProvider = Provider<PushService>((ref) => PushService(ref));
