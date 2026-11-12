import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../../core/api/api_client.dart';
import '../../core/config.dart';
import '../auth/auth_controller.dart';
import 'data/order_models.dart';
import 'data/orders_repository.dart';

/// Jonli yangilanish: SignalR `/hubs/orders` (`?access_token=` — kutubxona o'zi qo'shadi).
/// Server mijozga `OrderStatusChanged` yuboradi → ro'yxat va buyurtma provayderlari yangilanadi.
///
/// Faqat kirgan foydalanuvchi uchun ulanadi; chiqqanda (auth o'zgarsa) ulanish yopiladi.
/// Ulanmasa ham ilova ishlayveradi — kuzatish ekrani har 30 soniyada so'raydi, ro'yxatda pull-to-refresh bor.
///
/// Ishlatish: shell'da yoki buyurtma ekranlarida `ref.watch(orderUpdatesProvider)`.
final orderUpdatesProvider = Provider<OrderUpdates?>((ref) {
  final signedIn = ref.watch(authControllerProvider.select((s) => s.isSignedIn));
  if (!signedIn) return null;

  final session = ref.read(sessionManagerProvider);
  final updates = OrderUpdates._(
    () async => await session.accessToken() ?? '',
    (e) {
      ref.invalidate(ordersListProvider);
      ref.invalidate(orderDetailProvider(e.id));
    },
  );
  updates._start();
  ref.onDispose(updates._dispose);
  return updates;
});

class OrderUpdates {
  OrderUpdates._(this._accessToken, this._onEvent);

  final Future<String> Function() _accessToken;
  final void Function(OrderStatusEvent) _onEvent;
  final _events = StreamController<OrderStatusEvent>.broadcast();
  HubConnection? _connection;
  Timer? _retry;
  bool _disposed = false;

  /// Kelgan holat o'zgarishlari (masalan, ekranda xabar ko'rsatish uchun).
  Stream<OrderStatusEvent> get events => _events.stream;

  bool get isConnected => _connection?.state == HubConnectionState.Connected;

  void _start() {
    final connection = HubConnectionBuilder()
        .withUrl('${AppConfig.apiBaseUrl}/hubs/orders',
            options: HttpConnectionOptions(accessTokenFactory: _accessToken))
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();
    connection.on('OrderStatusChanged', (args) {
      final e = OrderStatusEvent.tryParse(args?.isNotEmpty == true ? args!.first : null);
      if (e == null) return;
      _events.add(e);
      _onEvent(e);
    });
    // Avtomatik qayta ulanish ham tugasa — 60 soniyadan keyin yana urinamiz.
    connection.onclose(({error}) => _scheduleRetry());
    _connection = connection;
    _connect();
  }

  Future<void> _connect() async {
    if (_disposed) return;
    try {
      await _connection?.start();
    } catch (e) {
      debugPrint('OrderUpdates: ulanib bo‘lmadi: $e');
      _scheduleRetry();
    }
  }

  void _scheduleRetry() {
    if (_disposed) return;
    _retry?.cancel();
    _retry = Timer(const Duration(seconds: 60), _connect);
  }

  void _dispose() {
    _disposed = true;
    _retry?.cancel();
    _events.close();
    _connection?.stop().catchError((_) {});
  }
}
