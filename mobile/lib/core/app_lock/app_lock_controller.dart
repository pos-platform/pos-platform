import 'dart:convert';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:local_auth/local_auth.dart';

/// Ilova qulfi (Telegram "Kod-parol" kabi): 4 xonali PIN + ixtiyoriy telefon qulfi
/// (barmoq izi / Face ID / ekran paroli).
///
/// - Yoqilgan bo'lsa ilova har ochilganda qulflanadi.
/// - Fonda [AppLockState.timeout] dan ko'p tursa — qaytganda qulflanadi.
/// - 5 marta xato PIN — 30 soniya kutish, keyingi har 5 xatoda ikki baravar.
/// - PIN qurilmada tuz (salt) + SHA-256 ko'rinishida, Keystore/Keychain ichida saqlanadi.
class AppLockState {
  const AppLockState({
    this.ready = false,
    this.enabled = false,
    this.locked = false,
    this.useDeviceAuth = false,
    this.timeout = const Duration(minutes: 1),
    this.deviceAuthAvailable = false,
    this.failedAttempts = 0,
    this.blockedUntil,
  });

  /// Sozlamalar o'qildi (o'qilguncha ilova ko'rsatilmaydi — qulfsiz "miltillash" bo'lmasin).
  final bool ready;
  final bool enabled;
  final bool locked;

  /// Telefon qulfi bilan ochish (barmoq izi, Face ID yoki telefonning o'z paroli).
  final bool useDeviceAuth;
  final Duration timeout;
  final bool deviceAuthAvailable;
  final int failedAttempts;
  final DateTime? blockedUntil;

  bool get canUseDeviceAuth => useDeviceAuth && deviceAuthAvailable;

  AppLockState copyWith({
    bool? ready,
    bool? enabled,
    bool? locked,
    bool? useDeviceAuth,
    Duration? timeout,
    bool? deviceAuthAvailable,
    int? failedAttempts,
    DateTime? blockedUntil,
    bool clearBlock = false,
  }) =>
      AppLockState(
        ready: ready ?? this.ready,
        enabled: enabled ?? this.enabled,
        locked: locked ?? this.locked,
        useDeviceAuth: useDeviceAuth ?? this.useDeviceAuth,
        timeout: timeout ?? this.timeout,
        deviceAuthAvailable: deviceAuthAvailable ?? this.deviceAuthAvailable,
        failedAttempts: failedAttempts ?? this.failedAttempts,
        blockedUntil: clearBlock ? null : (blockedUntil ?? this.blockedUntil),
      );
}

/// Avtomatik qulflash variantlari (sozlamalar ekrani).
const appLockTimeouts = [
  Duration.zero,
  Duration(minutes: 1),
  Duration(minutes: 5),
  Duration(hours: 1),
];

const appLockPinLength = 4;

/// Saqlash va telefon qulfi — testlarda almashtiriladi.
abstract class AppLockPlatform {
  Future<String?> read();
  Future<void> write(String value);
  Future<void> delete();
  Future<bool> deviceAuthAvailable();
  Future<bool> authenticate(String reason);
}

class DefaultAppLockPlatform implements AppLockPlatform {
  static const _key = 'app_lock_v1';
  final _storage = const FlutterSecureStorage();
  final _auth = LocalAuthentication();

  @override
  Future<String?> read() => _storage.read(key: _key);

  @override
  Future<void> write(String value) => _storage.write(key: _key, value: value);

  @override
  Future<void> delete() => _storage.delete(key: _key);

  @override
  Future<bool> deviceAuthAvailable() async {
    try {
      return await _auth.isDeviceSupported();
    } catch (_) {
      return false;
    }
  }

  @override
  Future<bool> authenticate(String reason) async {
    try {
      // biometricOnly: false — barmoq izi bo'lmasa telefonning PIN/grafik kaliti ham ishlaydi.
      return await _auth.authenticate(localizedReason: reason, persistAcrossBackgrounding: true);
    } catch (_) {
      // Bekor qilindi, sozlanmagan, vaqtincha bloklangan — o'zimizning PIN qoladi.
      return false;
    }
  }
}

final appLockPlatformProvider = Provider<AppLockPlatform>((ref) => DefaultAppLockPlatform());

class AppLockController extends Notifier<AppLockState> {
  String? _hash;
  String? _salt;
  DateTime? _hiddenAt;

  /// Telefon qulfi oynasi ochiq — Android'da ekran paroli alohida oyna bo'lib, ilovani "fon"ga o'tkazadi.
  bool _deviceAuthInProgress = false;

  AppLockPlatform get _platform => ref.read(appLockPlatformProvider);

  @override
  AppLockState build() {
    final listener = AppLifecycleListener(onHide: _onHide, onShow: _onShow);
    ref.onDispose(listener.dispose);
    _load();
    return const AppLockState();
  }

  Future<void> _load() async {
    final available = await _platform.deviceAuthAvailable();
    Map<String, dynamic>? json;
    try {
      final raw = await _platform.read();
      if (raw != null) json = jsonDecode(raw) as Map<String, dynamic>;
    } catch (_) {
      json = null;
    }
    if (!ref.mounted) return;
    if (json == null || json['hash'] == null) {
      state = AppLockState(ready: true, deviceAuthAvailable: available);
      return;
    }
    _hash = json['hash'] as String;
    _salt = json['salt'] as String;
    final blocked = json['blockedUntil'] as String?;
    state = AppLockState(
      ready: true,
      enabled: true,
      locked: true,
      useDeviceAuth: json['deviceAuth'] as bool? ?? false,
      timeout: Duration(seconds: json['timeoutSec'] as int? ?? 60),
      deviceAuthAvailable: available,
      failedAttempts: json['failed'] as int? ?? 0,
      blockedUntil: blocked == null ? null : DateTime.parse(blocked),
    );
  }

  void _onHide() {
    if (_deviceAuthInProgress || !state.enabled || state.locked) return;
    _hiddenAt = DateTime.now();
  }

  void _onShow() {
    final hiddenAt = _hiddenAt;
    _hiddenAt = null;
    if (hiddenAt == null || !state.enabled || state.locked) return;
    if (DateTime.now().difference(hiddenAt) >= state.timeout) state = state.copyWith(locked: true);
  }

  /// Sozlamalar ekranidan: tasdiqlangan yangi PIN bilan yoqish yoki PINni almashtirish.
  Future<void> setPin(String pin) async {
    _salt = _newSalt();
    _hash = _hashPin(pin, _salt!);
    state = state.copyWith(enabled: true, locked: false, failedAttempts: 0, clearBlock: true);
    await _persist();
  }

  Future<void> disable() async {
    _hash = null;
    _salt = null;
    state = AppLockState(ready: true, deviceAuthAvailable: state.deviceAuthAvailable);
    await _platform.delete();
  }

  Future<void> setUseDeviceAuth(bool value) async {
    state = state.copyWith(useDeviceAuth: value);
    await _persist();
  }

  Future<void> setTimeout(Duration value) async {
    state = state.copyWith(timeout: value);
    await _persist();
  }

  /// Joriy PINni tekshirish (bloklash hisobga olinmaydi) — sozlamalarda o'chirish/almashtirishdan oldin.
  bool verifyPin(String pin) => _hash != null && _hashPin(pin, _salt!) == _hash;

  /// Qulf ekranida PIN kiritildi. `true` — ochildi.
  Future<bool> unlockWithPin(String pin) async {
    if (isBlocked) return false;
    if (verifyPin(pin)) {
      state = state.copyWith(locked: false, failedAttempts: 0, clearBlock: true);
      await _persist();
      return true;
    }
    final failed = state.failedAttempts + 1;
    DateTime? blockedUntil;
    if (failed % 5 == 0) {
      // 5 → 30 s, 10 → 60 s, 15 → 120 s ... (eng ko'pi 1 soat).
      final seconds = min(30 * pow(2, failed ~/ 5 - 1).toInt(), 3600);
      blockedUntil = DateTime.now().add(Duration(seconds: seconds));
    }
    state = state.copyWith(failedAttempts: failed, blockedUntil: blockedUntil);
    await _persist();
    return false;
  }

  /// Telefon qulfi (barmoq izi / Face ID / ekran paroli) bilan ochish.
  Future<bool> unlockWithDevice(String reason) async {
    if (!state.canUseDeviceAuth || _deviceAuthInProgress) return false;
    _deviceAuthInProgress = true;
    try {
      final ok = await _platform.authenticate(reason);
      if (ok && ref.mounted) {
        state = state.copyWith(locked: false, failedAttempts: 0, clearBlock: true);
        await _persist();
      }
      return ok;
    } finally {
      _deviceAuthInProgress = false;
    }
  }

  /// Sozlamada telefon qulfini yoqishdan oldin tekshirish (haqiqatan ishlashiga ishonch).
  Future<bool> confirmDevice(String reason) async {
    _deviceAuthInProgress = true;
    try {
      return await _platform.authenticate(reason);
    } finally {
      _deviceAuthInProgress = false;
    }
  }

  bool get isBlocked {
    final until = state.blockedUntil;
    return until != null && DateTime.now().isBefore(until);
  }

  Future<void> _persist() async {
    if (_hash == null) return;
    await _platform.write(jsonEncode({
      'hash': _hash,
      'salt': _salt,
      'deviceAuth': state.useDeviceAuth,
      'timeoutSec': state.timeout.inSeconds,
      'failed': state.failedAttempts,
      'blockedUntil': state.blockedUntil?.toIso8601String(),
    }));
  }

  static String _newSalt() {
    final r = Random.secure();
    return base64Url.encode(List<int>.generate(16, (_) => r.nextInt(256)));
  }

  static String _hashPin(String pin, String salt) => sha256.convert(utf8.encode('$salt:$pin')).toString();
}

final appLockProvider = NotifierProvider<AppLockController, AppLockState>(AppLockController.new);
