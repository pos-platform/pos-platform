// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Russian (`ru`).
class AppLocalizationsRu extends AppLocalizations {
  AppLocalizationsRu([String locale = 'ru']) : super(locale);

  @override
  String get appTitle => 'PosCloud Market';

  @override
  String get navHome => 'Главная';

  @override
  String get navCart => 'Корзина';

  @override
  String get navOrders => 'Заказы';

  @override
  String get navProfile => 'Профиль';

  @override
  String get comingSoon => 'Скоро';

  @override
  String get loginSubtitle =>
      'Товары из магазина — до вашей двери. Введите номер телефона, чтобы войти.';

  @override
  String get phoneNumber => 'Номер телефона';

  @override
  String get smsCode => 'Код из SMS';

  @override
  String get login => 'Войти';

  @override
  String get browseWithoutLogin => 'Посмотреть без входа';
}
