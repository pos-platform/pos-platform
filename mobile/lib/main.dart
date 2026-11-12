import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'app.dart';
import 'core/locale/locale_controller.dart';
import 'core/monitoring.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Saqlangan til birinchi kadrdan oldin o'qiladi (miltillamasligi uchun).
  await LocaleController.preload();
  // SENTRY_DSN berilgan bo'lsa xatolar Sentry'ga yuboriladi, aks holda oddiy runApp.
  await Monitoring.run(() => const ProviderScope(child: PosCloudApp()));
}
