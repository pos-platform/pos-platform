import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/app.dart';
import 'package:poscloud_market/features/auth/data/auth_models.dart';
import 'package:poscloud_market/features/auth/data/token_storage.dart';

/// Testda qurilma xotirasi o'rniga.
class MemoryTokenStorage implements TokenStorage {
  StoredSession? session;

  @override
  Future<StoredSession?> read() async => session;

  @override
  Future<void> write(AuthTokens tokens) async => session = StoredSession(
        accessToken: tokens.accessToken,
        accessTokenExpiresAt: tokens.accessTokenExpiresAt,
        refreshToken: tokens.refreshToken,
        user: tokens.user,
      );

  @override
  Future<void> updateUser(ShopUser user) async {}

  @override
  Future<void> clear() async => session = null;
}

void main() {
  testWidgets('Kirmagan foydalanuvchi login ekranini ko‘radi, mehmon sifatida ichkariga kiradi', (tester) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [tokenStorageProvider.overrideWithValue(MemoryTokenStorage())],
      child: const PosCloudApp(),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Kodni olish'), findsOneWidget);

    await tester.tap(find.text('Hozircha kirmasdan ko‘rish'));
    // Bosh sahifa tarmoqdan yuklaydi (spinner) — settle kutilmaydi.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('Asosiy'), findsOneWidget);

  });
}
