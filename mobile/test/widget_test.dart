import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/app.dart';

void main() {
  testWidgets('Ilova ochiladi va pastki menyudan tab almashadi', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: PosCloudApp()));
    await tester.pumpAndSettle();

    expect(find.text('PosCloud Market'), findsWidgets);
    expect(find.text('Asosiy'), findsOneWidget);

    await tester.tap(find.text('Profil'));
    await tester.pumpAndSettle();
    expect(find.text('Tez orada'), findsOneWidget);
  });
}
