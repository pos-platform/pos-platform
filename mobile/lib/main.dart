import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'app.dart';
import 'core/locale/locale_controller.dart';
import 'core/monitoring.dart';
import 'core/store_app/store_app.dart';
import 'features/bonus/data/bonus_card_cache.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Saqlangan til birinchi kadrdan oldin o'qiladi (miltillamasligi uchun).
  await LocaleController.preload();
  // Do'kon rejimi: keshdagi do'kon ma'lumoti va brend rangi (offline ochilish, rang miltillamasligi uchun).
  await StoreAppController.preload();
  // Oxirgi bonus QR — "Bonus" tabi darhol ko'rsatishi uchun.
  await BonusCardCache.preload();
  // SENTRY_DSN berilgan bo'lsa xatolar Sentry'ga yuboriladi, aks holda oddiy runApp.
  await Monitoring.run(() => const ProviderScope(child: PosCloudApp()));
}
