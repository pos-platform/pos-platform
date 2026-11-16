import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/core/app_lock/app_lock_controller.dart';

class FakeLockPlatform implements AppLockPlatform {
  String? stored;
  bool deviceOk = true;

  @override
  Future<String?> read() async => stored;
  @override
  Future<void> write(String value) async => stored = value;
  @override
  Future<void> delete() async => stored = null;
  @override
  Future<bool> deviceAuthAvailable() async => true;
  @override
  Future<bool> authenticate(String reason) async => deviceOk;
}

Future<ProviderContainer> _container(FakeLockPlatform p) async {
  final c = ProviderContainer(overrides: [appLockPlatformProvider.overrideWithValue(p)]);
  c.listen(appLockProvider, (_, _) {});
  while (!c.read(appLockProvider).ready) {
    await Future<void>.delayed(Duration.zero);
  }
  return c;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('PIN o‘rnatilgach ilova qayta ochilganda qulflanadi va to‘g‘ri kod bilan ochiladi', () async {
    final p = FakeLockPlatform();
    var c = await _container(p);
    expect(c.read(appLockProvider).enabled, isFalse);
    await c.read(appLockProvider.notifier).setPin('1234');
    expect(p.stored, isNot(contains('1234')));
    c.dispose();

    c = await _container(p);
    expect(c.read(appLockProvider).locked, isTrue);
    expect(await c.read(appLockProvider.notifier).unlockWithPin('0000'), isFalse);
    expect(await c.read(appLockProvider.notifier).unlockWithPin('1234'), isTrue);
    expect(c.read(appLockProvider).locked, isFalse);
  });

  test('5 ta xato — bloklanadi, qayta ochilganda ham blok saqlanadi', () async {
    final p = FakeLockPlatform();
    var c = await _container(p);
    await c.read(appLockProvider.notifier).setPin('1111');
    for (var i = 0; i < 5; i++) {
      await c.read(appLockProvider.notifier).unlockWithPin('9999');
    }
    expect(c.read(appLockProvider.notifier).isBlocked, isTrue);
    expect(await c.read(appLockProvider.notifier).unlockWithPin('1111'), isFalse);
    c.dispose();

    c = await _container(p);
    expect(c.read(appLockProvider.notifier).isBlocked, isTrue);
  });

  test('Telefon qulfi yoqilgan bo‘lsa barmoq izi bilan ochiladi; o‘chirilsa qulf yo‘qoladi', () async {
    final p = FakeLockPlatform();
    final c = await _container(p);
    final n = c.read(appLockProvider.notifier);
    await n.setPin('4321');
    await n.setUseDeviceAuth(true);
    c.read(appLockProvider.notifier).state = c.read(appLockProvider).copyWith(locked: true);
    expect(await n.unlockWithDevice('test'), isTrue);
    expect(c.read(appLockProvider).locked, isFalse);

    await n.disable();
    expect(c.read(appLockProvider).enabled, isFalse);
    expect(p.stored, isNull);
  });
}
