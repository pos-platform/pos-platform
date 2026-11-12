// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Uzbek (`uz`).
class AppLocalizationsUz extends AppLocalizations {
  AppLocalizationsUz([String locale = 'uz']) : super(locale);

  @override
  String get appTitle => 'PosCloud Market';

  @override
  String get navHome => 'Asosiy';

  @override
  String get navCart => 'Savatcha';

  @override
  String get navOrders => 'Buyurtmalar';

  @override
  String get navProfile => 'Profil';

  @override
  String get comingSoon => 'Tez orada';

  @override
  String get loginSubtitle =>
      'Do‘kondagi mahsulotlar — uyingizgacha. Kirish uchun telefon raqamingizni kiriting.';

  @override
  String get phoneNumber => 'Telefon raqam';

  @override
  String get smsCode => 'SMS kod';

  @override
  String get login => 'Kirish';

  @override
  String get browseWithoutLogin => 'Hozircha kirmasdan ko‘rish';
}
