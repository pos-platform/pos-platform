import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../../core/config.dart';
import 'data/courier_repository.dart';
import 'data/courier_session.dart';

/// Kuryer uchun jonli yangilanish: SignalR `/hubs/orders` kuryer tokeni bilan.
/// Server kuryerni `courier:{userId}` guruhiga qo'shadi va `CourierOrderAssigned` / `OrderUpdated` yuboradi —
/// ro'yxat va ochiq buyurtma qayta yuklanadi. Ulanmasa ekranlardagi 30 s polling ishlayveradi.
final courierUpdatesProvider = Provider<CourierUpdates?>((ref) {
  final token = ref.watch(courierAuthProvider.select((s) => s.session?.token));
  if (token == null) return null;

  final updates = CourierUpdates._(token, () {
    ref.invalidate(courierOrdersProvider);
    ref.invalidate(courierOrderProvider);
  });
  updates._start();
  ref.onDispose(updates._dispose);
  return updates;
});

class CourierUpdates {
  CourierUpdates._(this._token, this._onEvent);

  final String _token;
  final void Function() _onEvent;
  HubConnection? _connection;
  Timer? _retry;
  bool _disposed = false;

  void _start() {
    final connection = HubConnectionBuilder()
        .withUrl('${AppConfig.apiBaseUrl}/hubs/orders',
            options: HttpConnectionOptions(accessTokenFactory: () async => _token))
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();
    connection.on('CourierOrderAssigned', (_) => _onEvent());
    connection.on('OrderUpdated', (_) => _onEvent());
    connection.onclose(({error}) => _scheduleRetry());
    _connection = connection;
    _connect();
  }

  Future<void> _connect() async {
    if (_disposed) return;
    try {
      await _connection?.start();
    } catch (e) {
      debugPrint('CourierUpdates: ulanib bo‘lmadi: $e');
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
    _connection?.stop().catchError((_) {});
  }
}
