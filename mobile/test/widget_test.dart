import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart' show Color, Icons, Size;

import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/app.dart';
import 'package:poscloud_market/core/theme/app_colors.dart';
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
    // Telefon o'lchami (login varag'i rasm ostida — 800×600 da tugma ko'rinmaydi).
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
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
    // Pastki menyu: 5 tab, o'rtada "Bonus". Mehmon bossa — kirish ekrani.
    expect(find.byIcon(Icons.qr_code_2_rounded), findsOneWidget);
    await tester.tap(find.byIcon(Icons.qr_code_2_rounded));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('Kodni olish'), findsOneWidget);

  });

  test('Brend rangi: och rang oq matn o‘qiladigan darajada to‘qlashtiriladi', () {
    AppColors.applyBrand(const Color(0xFFFFEB3B)); // sariq
    final c = AppColors.primary;
    expect(1.05 / (c.computeLuminance() + 0.05), greaterThanOrEqualTo(4.5));
    AppColors.applyBrand(null);
    expect(AppColors.primary, AppColors.defaultPrimary);
    expect(AppColors.parseHex('#D81F26'), AppColors.defaultPrimary);
    expect(AppColors.parseHex('zzz'), isNull);
  });
}
