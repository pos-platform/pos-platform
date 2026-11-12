import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import 'config.dart';

/// Huquqiy sahifalar (backend wwwroot dan beriladi, manba: frontend/public/).
class LegalLinks {
  LegalLinks._();

  static const privacyUrl = '${AppConfig.apiBaseUrl}/privacy.html';
  static const termsUrl = '${AppConfig.apiBaseUrl}/terms.html';
}

/// Sahifani tashqi brauzerda ochadi; ochilmasa — qisqa xabar.
Future<void> openLegal(BuildContext context, String url) async {
  final messenger = ScaffoldMessenger.maybeOf(context);
  var ok = false;
  try {
    ok = await launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
  } catch (_) {
    ok = false;
  }
  if (!ok) messenger?.showSnackBar(SnackBar(content: Text(url)));
}
