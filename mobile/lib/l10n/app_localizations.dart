import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ru.dart';
import 'app_localizations_uz.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ru'),
    Locale('uz'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In uz, this message translates to:
  /// **'Xo‘ja Market'**
  String get appTitle;

  /// No description provided for @navHome.
  ///
  /// In uz, this message translates to:
  /// **'Asosiy'**
  String get navHome;

  /// No description provided for @navCart.
  ///
  /// In uz, this message translates to:
  /// **'Savatcha'**
  String get navCart;

  /// No description provided for @navOrders.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalar'**
  String get navOrders;

  /// No description provided for @navProfile.
  ///
  /// In uz, this message translates to:
  /// **'Profil'**
  String get navProfile;

  /// No description provided for @comingSoon.
  ///
  /// In uz, this message translates to:
  /// **'Tez orada'**
  String get comingSoon;

  /// No description provided for @loginSubtitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kondagi mahsulotlar — uyingizgacha. Kirish uchun telefon raqamingizni kiriting.'**
  String get loginSubtitle;

  /// No description provided for @phoneNumber.
  ///
  /// In uz, this message translates to:
  /// **'Telefon raqam'**
  String get phoneNumber;

  /// No description provided for @smsCode.
  ///
  /// In uz, this message translates to:
  /// **'SMS kod'**
  String get smsCode;

  /// No description provided for @login.
  ///
  /// In uz, this message translates to:
  /// **'Kirish'**
  String get login;

  /// No description provided for @browseWithoutLogin.
  ///
  /// In uz, this message translates to:
  /// **'Hozircha kirmasdan ko‘rish'**
  String get browseWithoutLogin;

  /// No description provided for @getCode.
  ///
  /// In uz, this message translates to:
  /// **'Kodni olish'**
  String get getCode;

  /// No description provided for @codeSentTelegram.
  ///
  /// In uz, this message translates to:
  /// **'Kod Telegram orqali {phone} raqamiga yuborildi'**
  String codeSentTelegram(String phone);

  /// No description provided for @codeSentSms.
  ///
  /// In uz, this message translates to:
  /// **'Kod {phone} raqamiga yuborildi'**
  String codeSentSms(String phone);

  /// No description provided for @resendIn.
  ///
  /// In uz, this message translates to:
  /// **'qayta yuborish {time}'**
  String resendIn(String time);

  /// No description provided for @resendCode.
  ///
  /// In uz, this message translates to:
  /// **'Kodni qayta yuborish'**
  String get resendCode;

  /// No description provided for @sendViaSms.
  ///
  /// In uz, this message translates to:
  /// **'SMS orqali yuborish'**
  String get sendViaSms;

  /// No description provided for @changeNumber.
  ///
  /// In uz, this message translates to:
  /// **'Raqamni o‘zgartirish'**
  String get changeNumber;

  /// No description provided for @invalidPhone.
  ///
  /// In uz, this message translates to:
  /// **'Telefon raqamni to‘liq kiriting'**
  String get invalidPhone;

  /// No description provided for @retry.
  ///
  /// In uz, this message translates to:
  /// **'Qayta urinish'**
  String get retry;

  /// No description provided for @signInRequired.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma berish uchun kiring'**
  String get signInRequired;

  /// No description provided for @signOut.
  ///
  /// In uz, this message translates to:
  /// **'Chiqish'**
  String get signOut;

  /// No description provided for @profileAddressCount.
  ///
  /// In uz, this message translates to:
  /// **'{count} ta'**
  String profileAddressCount(int count);

  /// No description provided for @profilePersonalInfo.
  ///
  /// In uz, this message translates to:
  /// **'Shaxsiy ma’lumotlar'**
  String get profilePersonalInfo;

  /// No description provided for @profileAddresses.
  ///
  /// In uz, this message translates to:
  /// **'Manzillarim'**
  String get profileAddresses;

  /// No description provided for @profileLanguage.
  ///
  /// In uz, this message translates to:
  /// **'Til'**
  String get profileLanguage;

  /// No description provided for @profileLanguageName.
  ///
  /// In uz, this message translates to:
  /// **'O‘zbekcha'**
  String get profileLanguageName;

  /// No description provided for @profileLanguageLater.
  ///
  /// In uz, this message translates to:
  /// **'Tilni o‘zgartirish keyingi versiyada. Hozir qurilma tili ishlatiladi.'**
  String get profileLanguageLater;

  /// No description provided for @profileCancel.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilish'**
  String get profileCancel;

  /// No description provided for @profileSignOutTitle.
  ///
  /// In uz, this message translates to:
  /// **'Akkauntdan chiqasizmi?'**
  String get profileSignOutTitle;

  /// No description provided for @profileSignOutMessage.
  ///
  /// In uz, this message translates to:
  /// **'Qayta kirish uchun telefon raqamingizga kod yuboriladi.'**
  String get profileSignOutMessage;

  /// No description provided for @profileDeleteAccount.
  ///
  /// In uz, this message translates to:
  /// **'Akkauntni o‘chirish'**
  String get profileDeleteAccount;

  /// No description provided for @profileDeleteTitle.
  ///
  /// In uz, this message translates to:
  /// **'Akkaunt o‘chirilsinmi?'**
  String get profileDeleteTitle;

  /// No description provided for @profileDeleteMessage.
  ///
  /// In uz, this message translates to:
  /// **'Ism, manzillar va boshqa shaxsiy ma’lumotlar o‘chiriladi, barcha qurilmalardan chiqasiz. Buni qaytarib bo‘lmaydi.'**
  String get profileDeleteMessage;

  /// No description provided for @profileDeleteConfirmWord.
  ///
  /// In uz, this message translates to:
  /// **'O‘CHIRISH'**
  String get profileDeleteConfirmWord;

  /// No description provided for @profileDeleteTypeHint.
  ///
  /// In uz, this message translates to:
  /// **'Tasdiqlash uchun {word} deb yozing'**
  String profileDeleteTypeHint(String word);

  /// No description provided for @profileDeleteButton.
  ///
  /// In uz, this message translates to:
  /// **'O‘chirish'**
  String get profileDeleteButton;

  /// No description provided for @profileGuestTitle.
  ///
  /// In uz, this message translates to:
  /// **'Profilga kiring'**
  String get profileGuestTitle;

  /// No description provided for @profileGuestText.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalar, manzillar va bonuslar uchun telefon raqamingiz bilan kiring.'**
  String get profileGuestText;

  /// No description provided for @profileNameBannerTitle.
  ///
  /// In uz, this message translates to:
  /// **'Ismingizni kiriting'**
  String get profileNameBannerTitle;

  /// No description provided for @profileNameBannerText.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon va kuryer sizga ism bilan murojaat qilishi uchun.'**
  String get profileNameBannerText;

  /// No description provided for @profileFirstName.
  ///
  /// In uz, this message translates to:
  /// **'Ism'**
  String get profileFirstName;

  /// No description provided for @profileLastName.
  ///
  /// In uz, this message translates to:
  /// **'Familiya'**
  String get profileLastName;

  /// No description provided for @profilePhone.
  ///
  /// In uz, this message translates to:
  /// **'Telefon raqam'**
  String get profilePhone;

  /// No description provided for @profilePhoneVerified.
  ///
  /// In uz, this message translates to:
  /// **'Tasdiqlangan'**
  String get profilePhoneVerified;

  /// No description provided for @profileBirthDate.
  ///
  /// In uz, this message translates to:
  /// **'Tug‘ilgan sana'**
  String get profileBirthDate;

  /// No description provided for @profileBirthDateHint.
  ///
  /// In uz, this message translates to:
  /// **'Tug‘ilgan kuningizda bonus sovg‘a qilamiz'**
  String get profileBirthDateHint;

  /// No description provided for @profileGender.
  ///
  /// In uz, this message translates to:
  /// **'Jins'**
  String get profileGender;

  /// No description provided for @profileGenderMale.
  ///
  /// In uz, this message translates to:
  /// **'Erkak'**
  String get profileGenderMale;

  /// No description provided for @profileGenderFemale.
  ///
  /// In uz, this message translates to:
  /// **'Ayol'**
  String get profileGenderFemale;

  /// No description provided for @profileEmail.
  ///
  /// In uz, this message translates to:
  /// **'Email'**
  String get profileEmail;

  /// No description provided for @profileEmailHint.
  ///
  /// In uz, this message translates to:
  /// **'elektron chek va xabarlar uchun, ixtiyoriy'**
  String get profileEmailHint;

  /// No description provided for @profileEmailInvalid.
  ///
  /// In uz, this message translates to:
  /// **'Email noto‘g‘ri'**
  String get profileEmailInvalid;

  /// No description provided for @profileFirstNameRequired.
  ///
  /// In uz, this message translates to:
  /// **'Ismni kiriting'**
  String get profileFirstNameRequired;

  /// No description provided for @profileSave.
  ///
  /// In uz, this message translates to:
  /// **'Saqlash'**
  String get profileSave;

  /// No description provided for @profileSaved.
  ///
  /// In uz, this message translates to:
  /// **'Saqlandi'**
  String get profileSaved;

  /// No description provided for @addressesTitle.
  ///
  /// In uz, this message translates to:
  /// **'Manzillarim'**
  String get addressesTitle;

  /// No description provided for @addressAdd.
  ///
  /// In uz, this message translates to:
  /// **'Yangi manzil qo‘shish'**
  String get addressAdd;

  /// No description provided for @addressDefaultBadge.
  ///
  /// In uz, this message translates to:
  /// **'Asosiy'**
  String get addressDefaultBadge;

  /// No description provided for @addressEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Hali manzil qo‘shilmagan'**
  String get addressEmpty;

  /// No description provided for @addressOutOfZoneNote.
  ///
  /// In uz, this message translates to:
  /// **'Manzil yetkazish hududidan tashqarida bo‘lsa, buyurtmani do‘kondan olib ketishingiz mumkin.'**
  String get addressOutOfZoneNote;

  /// No description provided for @addressNewTitle.
  ///
  /// In uz, this message translates to:
  /// **'Manzil qo‘shish'**
  String get addressNewTitle;

  /// No description provided for @addressEditTitle.
  ///
  /// In uz, this message translates to:
  /// **'Manzilni tahrirlash'**
  String get addressEditTitle;

  /// No description provided for @addressLine.
  ///
  /// In uz, this message translates to:
  /// **'Manzil'**
  String get addressLine;

  /// No description provided for @addressLineHint.
  ///
  /// In uz, this message translates to:
  /// **'Ko‘cha, uy raqami'**
  String get addressLineHint;

  /// No description provided for @addressLineRequired.
  ///
  /// In uz, this message translates to:
  /// **'Manzilni kiriting'**
  String get addressLineRequired;

  /// No description provided for @addressEntrance.
  ///
  /// In uz, this message translates to:
  /// **'Podyezd'**
  String get addressEntrance;

  /// No description provided for @addressFloor.
  ///
  /// In uz, this message translates to:
  /// **'Qavat'**
  String get addressFloor;

  /// No description provided for @addressApartment.
  ///
  /// In uz, this message translates to:
  /// **'Xonadon'**
  String get addressApartment;

  /// No description provided for @addressIntercom.
  ///
  /// In uz, this message translates to:
  /// **'Domofon kodi'**
  String get addressIntercom;

  /// No description provided for @addressLandmark.
  ///
  /// In uz, this message translates to:
  /// **'Mo‘ljal'**
  String get addressLandmark;

  /// No description provided for @addressLandmarkHint.
  ///
  /// In uz, this message translates to:
  /// **'Masalan: maktab yonida'**
  String get addressLandmarkHint;

  /// No description provided for @addressComment.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer uchun izoh'**
  String get addressComment;

  /// No description provided for @addressCommentHint.
  ///
  /// In uz, this message translates to:
  /// **'Masalan: darvoza ko‘k rangda'**
  String get addressCommentHint;

  /// No description provided for @addressLabel.
  ///
  /// In uz, this message translates to:
  /// **'Nomi'**
  String get addressLabel;

  /// No description provided for @addressLabelHome.
  ///
  /// In uz, this message translates to:
  /// **'Uy'**
  String get addressLabelHome;

  /// No description provided for @addressLabelWork.
  ///
  /// In uz, this message translates to:
  /// **'Ish'**
  String get addressLabelWork;

  /// No description provided for @addressLabelOther.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa'**
  String get addressLabelOther;

  /// No description provided for @addressLabelCustomHint.
  ///
  /// In uz, this message translates to:
  /// **'Masalan: Onamnikiga'**
  String get addressLabelCustomHint;

  /// No description provided for @addressLabelRequired.
  ///
  /// In uz, this message translates to:
  /// **'Manzil nomini kiriting'**
  String get addressLabelRequired;

  /// No description provided for @addressMakeDefault.
  ///
  /// In uz, this message translates to:
  /// **'Asosiy manzil'**
  String get addressMakeDefault;

  /// No description provided for @addressSave.
  ///
  /// In uz, this message translates to:
  /// **'Manzilni saqlash'**
  String get addressSave;

  /// No description provided for @addressDelete.
  ///
  /// In uz, this message translates to:
  /// **'Manzilni o‘chirish'**
  String get addressDelete;

  /// No description provided for @addressDeleteConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Bu manzil o‘chirilsinmi?'**
  String get addressDeleteConfirm;

  /// No description provided for @addressNotFound.
  ///
  /// In uz, this message translates to:
  /// **'Manzil topilmadi'**
  String get addressNotFound;

  /// No description provided for @addressDetailEntrance.
  ///
  /// In uz, this message translates to:
  /// **'{value}-podyezd'**
  String addressDetailEntrance(String value);

  /// No description provided for @addressDetailFloor.
  ///
  /// In uz, this message translates to:
  /// **'{value}-qavat'**
  String addressDetailFloor(String value);

  /// No description provided for @addressDetailApartment.
  ///
  /// In uz, this message translates to:
  /// **'{value}-xonadon'**
  String addressDetailApartment(String value);

  /// No description provided for @addressDetailIntercom.
  ///
  /// In uz, this message translates to:
  /// **'domofon {value}'**
  String addressDetailIntercom(String value);

  /// No description provided for @ordersTitle.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalarim'**
  String get ordersTitle;

  /// No description provided for @ordersTabActive.
  ///
  /// In uz, this message translates to:
  /// **'Faol'**
  String get ordersTabActive;

  /// No description provided for @ordersTabActiveCount.
  ///
  /// In uz, this message translates to:
  /// **'Faol ({count})'**
  String ordersTabActiveCount(String count);

  /// No description provided for @ordersTabHistory.
  ///
  /// In uz, this message translates to:
  /// **'Tarix'**
  String get ordersTabHistory;

  /// No description provided for @ordersEmptyActive.
  ///
  /// In uz, this message translates to:
  /// **'Hozir faol buyurtmalar yo‘q'**
  String get ordersEmptyActive;

  /// No description provided for @ordersEmptyHistory.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalar tarixi bo‘sh'**
  String get ordersEmptyHistory;

  /// No description provided for @ordersSignInPrompt.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalaringizni ko‘rish uchun kiring'**
  String get ordersSignInPrompt;

  /// No description provided for @ordersTrack.
  ///
  /// In uz, this message translates to:
  /// **'Kuzatish'**
  String get ordersTrack;

  /// No description provided for @ordersReorder.
  ///
  /// In uz, this message translates to:
  /// **'Qayta buyurtma'**
  String get ordersReorder;

  /// No description provided for @ordersDelivery.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish'**
  String get ordersDelivery;

  /// No description provided for @ordersPickup.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketish'**
  String get ordersPickup;

  /// No description provided for @ordersItemKinds.
  ///
  /// In uz, this message translates to:
  /// **'{count} xil'**
  String ordersItemKinds(String count);

  /// No description provided for @ordersToday.
  ///
  /// In uz, this message translates to:
  /// **'Bugun'**
  String get ordersToday;

  /// No description provided for @ordersYesterday.
  ///
  /// In uz, this message translates to:
  /// **'Kecha'**
  String get ordersYesterday;

  /// No description provided for @ordersOrderNumber.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma #{number}'**
  String ordersOrderNumber(String number);

  /// No description provided for @ordersCallStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konga qo‘ng‘iroq qilish'**
  String get ordersCallStore;

  /// No description provided for @ordersBack.
  ///
  /// In uz, this message translates to:
  /// **'Ortga'**
  String get ordersBack;

  /// No description provided for @ordersMoney.
  ///
  /// In uz, this message translates to:
  /// **'{amount} so‘m'**
  String ordersMoney(String amount);

  /// No description provided for @ordersStatusNew.
  ///
  /// In uz, this message translates to:
  /// **'Yangi'**
  String get ordersStatusNew;

  /// No description provided for @ordersStatusAccepted.
  ///
  /// In uz, this message translates to:
  /// **'Tasdiqlandi'**
  String get ordersStatusAccepted;

  /// No description provided for @ordersStatusPicking.
  ///
  /// In uz, this message translates to:
  /// **'Yig‘ilmoqda'**
  String get ordersStatusPicking;

  /// No description provided for @ordersStatusReady.
  ///
  /// In uz, this message translates to:
  /// **'Tayyor'**
  String get ordersStatusReady;

  /// No description provided for @ordersStatusAwaitingTransfer.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov kutilmoqda'**
  String get ordersStatusAwaitingTransfer;

  /// No description provided for @ordersStatusOnTheWay.
  ///
  /// In uz, this message translates to:
  /// **'Yo‘lda'**
  String get ordersStatusOnTheWay;

  /// No description provided for @ordersStatusReadyForPickup.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketishga tayyor'**
  String get ordersStatusReadyForPickup;

  /// No description provided for @ordersStatusDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazildi'**
  String get ordersStatusDelivered;

  /// No description provided for @ordersStatusPickedUp.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketildi'**
  String get ordersStatusPickedUp;

  /// No description provided for @ordersStatusRejected.
  ///
  /// In uz, this message translates to:
  /// **'Rad etildi'**
  String get ordersStatusRejected;

  /// No description provided for @ordersStatusCancelled.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilindi'**
  String get ordersStatusCancelled;

  /// No description provided for @ordersCardLabel.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma holati'**
  String get ordersCardLabel;

  /// No description provided for @ordersScheduled.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish vaqti: {time}'**
  String ordersScheduled(String time);

  /// No description provided for @ordersHeadlineNew.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon buyurtmangizni tasdiqlashi kutilmoqda'**
  String get ordersHeadlineNew;

  /// No description provided for @ordersHeadlineAccepted.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon buyurtmangizni tasdiqladi'**
  String get ordersHeadlineAccepted;

  /// No description provided for @ordersHeadlinePicking.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon buyurtmangizni yig‘moqda'**
  String get ordersHeadlinePicking;

  /// No description provided for @ordersHeadlineReady.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmangiz yig‘ildi'**
  String get ordersHeadlineReady;

  /// No description provided for @ordersHeadlineAwaitingTransfer.
  ///
  /// In uz, this message translates to:
  /// **'O‘tkazmangiz kutilmoqda'**
  String get ordersHeadlineAwaitingTransfer;

  /// No description provided for @ordersHeadlineOnTheWay.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer yo‘lda'**
  String get ordersHeadlineOnTheWay;

  /// No description provided for @ordersHeadlineReadyForPickup.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmani do‘kondan olib ketishingiz mumkin'**
  String get ordersHeadlineReadyForPickup;

  /// No description provided for @ordersHeadlineDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma yetkazildi. Xaridingiz uchun rahmat!'**
  String get ordersHeadlineDelivered;

  /// No description provided for @ordersStepPlaced.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma qabul qilindi'**
  String get ordersStepPlaced;

  /// No description provided for @ordersStepAccepted.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon tasdiqladi'**
  String get ordersStepAccepted;

  /// No description provided for @ordersStepPicking.
  ///
  /// In uz, this message translates to:
  /// **'Yig‘ilmoqda'**
  String get ordersStepPicking;

  /// No description provided for @ordersStepPickingSub.
  ///
  /// In uz, this message translates to:
  /// **'Kassir mahsulotlarni yig‘moqda'**
  String get ordersStepPickingSub;

  /// No description provided for @ordersStepDispatched.
  ///
  /// In uz, this message translates to:
  /// **'Kuryerga topshirildi'**
  String get ordersStepDispatched;

  /// No description provided for @ordersStepReadyForPickup.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketishga tayyor'**
  String get ordersStepReadyForPickup;

  /// No description provided for @ordersStepDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazildi'**
  String get ordersStepDelivered;

  /// No description provided for @ordersStepPickedUp.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketildi'**
  String get ordersStepPickedUp;

  /// No description provided for @ordersStepAwaitingTransfer.
  ///
  /// In uz, this message translates to:
  /// **'O‘tkazma tasdiqlanishi kutilmoqda'**
  String get ordersStepAwaitingTransfer;

  /// No description provided for @ordersPaymentUnpaid.
  ///
  /// In uz, this message translates to:
  /// **'To‘lanmagan'**
  String get ordersPaymentUnpaid;

  /// No description provided for @ordersPaymentPaid.
  ///
  /// In uz, this message translates to:
  /// **'To‘langan'**
  String get ordersPaymentPaid;

  /// No description provided for @ordersPaymentRefunded.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarilgan'**
  String get ordersPaymentRefunded;

  /// No description provided for @ordersMethodCash.
  ///
  /// In uz, this message translates to:
  /// **'Naqd'**
  String get ordersMethodCash;

  /// No description provided for @ordersMethodCard.
  ///
  /// In uz, this message translates to:
  /// **'Karta (kuryer terminali)'**
  String get ordersMethodCard;

  /// No description provided for @ordersMethodTransfer.
  ///
  /// In uz, this message translates to:
  /// **'Kartaga o‘tkazma'**
  String get ordersMethodTransfer;

  /// No description provided for @ordersItemsSummary.
  ///
  /// In uz, this message translates to:
  /// **'{count} xil mahsulot · {total}'**
  String ordersItemsSummary(String count, String total);

  /// No description provided for @ordersContents.
  ///
  /// In uz, this message translates to:
  /// **'Tarkibi'**
  String get ordersContents;

  /// No description provided for @ordersItemRemoved.
  ///
  /// In uz, this message translates to:
  /// **'Olib tashlandi'**
  String get ordersItemRemoved;

  /// No description provided for @ordersItemReplacement.
  ///
  /// In uz, this message translates to:
  /// **'Almashtirish'**
  String get ordersItemReplacement;

  /// No description provided for @ordersItemsTotal.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotlar'**
  String get ordersItemsTotal;

  /// No description provided for @ordersDeliveryFee.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish'**
  String get ordersDeliveryFee;

  /// No description provided for @ordersTotal.
  ///
  /// In uz, this message translates to:
  /// **'Jami'**
  String get ordersTotal;

  /// No description provided for @ordersCancel.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilish'**
  String get ordersCancel;

  /// No description provided for @ordersCancelDisabled.
  ///
  /// In uz, this message translates to:
  /// **'Yig‘ish boshlangani uchun bekor qilib bo‘lmaydi'**
  String get ordersCancelDisabled;

  /// No description provided for @ordersCancelTitle.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmani bekor qilasizmi?'**
  String get ordersCancelTitle;

  /// No description provided for @ordersCancelReasonHint.
  ///
  /// In uz, this message translates to:
  /// **'Sabab (ixtiyoriy)'**
  String get ordersCancelReasonHint;

  /// No description provided for @ordersCancelDone.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma bekor qilindi'**
  String get ordersCancelDone;

  /// No description provided for @ordersRejectedTitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon buyurtmani rad etdi'**
  String get ordersRejectedTitle;

  /// No description provided for @ordersCancelledTitle.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma bekor qilindi'**
  String get ordersCancelledTitle;

  /// No description provided for @ordersReason.
  ///
  /// In uz, this message translates to:
  /// **'Sabab: {reason}'**
  String ordersReason(String reason);

  /// No description provided for @ordersTransferTitle.
  ///
  /// In uz, this message translates to:
  /// **'Kartaga o‘tkazma'**
  String get ordersTransferTitle;

  /// No description provided for @ordersTransferHint.
  ///
  /// In uz, this message translates to:
  /// **'Quyidagi kartaga {amount} o‘tkazing va chek rasmini yuklang'**
  String ordersTransferHint(String amount);

  /// No description provided for @ordersTransferCard.
  ///
  /// In uz, this message translates to:
  /// **'Karta raqami'**
  String get ordersTransferCard;

  /// No description provided for @ordersTransferHolder.
  ///
  /// In uz, this message translates to:
  /// **'Karta egasi'**
  String get ordersTransferHolder;

  /// No description provided for @ordersTransferAmount.
  ///
  /// In uz, this message translates to:
  /// **'Summa'**
  String get ordersTransferAmount;

  /// No description provided for @ordersCopy.
  ///
  /// In uz, this message translates to:
  /// **'Nusxa olish'**
  String get ordersCopy;

  /// No description provided for @ordersCopied.
  ///
  /// In uz, this message translates to:
  /// **'Nusxa olindi'**
  String get ordersCopied;

  /// No description provided for @ordersUploadProof.
  ///
  /// In uz, this message translates to:
  /// **'Chek rasmini yuklash'**
  String get ordersUploadProof;

  /// No description provided for @ordersReuploadProof.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa rasm yuklash'**
  String get ordersReuploadProof;

  /// No description provided for @ordersProofUploaded.
  ///
  /// In uz, this message translates to:
  /// **'Chek yuklandi — do‘kon to‘lovni tasdiqlashini kuting'**
  String get ordersProofUploaded;

  /// No description provided for @ordersTransferWaitReady.
  ///
  /// In uz, this message translates to:
  /// **'Karta raqami buyurtma yig‘ilgach ko‘rsatiladi'**
  String get ordersTransferWaitReady;

  /// No description provided for @ordersPaymentConfirmed.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov tasdiqlandi'**
  String get ordersPaymentConfirmed;

  /// No description provided for @ordersFileTooBig.
  ///
  /// In uz, this message translates to:
  /// **'Rasm 5 MB dan katta bo‘lmasin'**
  String get ordersFileTooBig;

  /// No description provided for @catSum.
  ///
  /// In uz, this message translates to:
  /// **'{sum} so‘m'**
  String catSum(String sum);

  /// No description provided for @catUnitKg.
  ///
  /// In uz, this message translates to:
  /// **'kg'**
  String get catUnitKg;

  /// No description provided for @catUnitPiece.
  ///
  /// In uz, this message translates to:
  /// **'dona'**
  String get catUnitPiece;

  /// No description provided for @catDecrease.
  ///
  /// In uz, this message translates to:
  /// **'Kamaytirish'**
  String get catDecrease;

  /// No description provided for @catIncrease.
  ///
  /// In uz, this message translates to:
  /// **'Ko‘paytirish'**
  String get catIncrease;

  /// No description provided for @catRetry.
  ///
  /// In uz, this message translates to:
  /// **'Qayta urinish'**
  String get catRetry;

  /// No description provided for @catCancel.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilish'**
  String get catCancel;

  /// No description provided for @catSwitchStoreTitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konni almashtirasizmi?'**
  String get catSwitchStoreTitle;

  /// No description provided for @catSwitchStoreBody.
  ///
  /// In uz, this message translates to:
  /// **'Savatchada boshqa do‘kon mahsulotlari bor. Do‘kon almashtirilsa, savatcha tozalanadi.'**
  String get catSwitchStoreBody;

  /// No description provided for @catSwitchStoreConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Tozalash va almashtirish'**
  String get catSwitchStoreConfirm;

  /// No description provided for @catChooseStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konni tanlang'**
  String get catChooseStore;

  /// No description provided for @catChooseAnotherStore.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa do‘kon tanlash'**
  String get catChooseAnotherStore;

  /// No description provided for @catChangeStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konni almashtirish'**
  String get catChangeStore;

  /// No description provided for @catSearchStores.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon nomi'**
  String get catSearchStores;

  /// No description provided for @catNoStores.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon topilmadi'**
  String get catNoStores;

  /// No description provided for @catDistanceKm.
  ///
  /// In uz, this message translates to:
  /// **'{km} km'**
  String catDistanceKm(String km);

  /// No description provided for @catOpenNow.
  ///
  /// In uz, this message translates to:
  /// **'Ochiq'**
  String get catOpenNow;

  /// No description provided for @catClosedNow.
  ///
  /// In uz, this message translates to:
  /// **'Yopiq'**
  String get catClosedNow;

  /// No description provided for @catSearchHint.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulot yoki shtrix-kod qidirish'**
  String get catSearchHint;

  /// No description provided for @catClear.
  ///
  /// In uz, this message translates to:
  /// **'Tozalash'**
  String get catClear;

  /// No description provided for @catAll.
  ///
  /// In uz, this message translates to:
  /// **'Hammasi'**
  String get catAll;

  /// No description provided for @catPopular.
  ///
  /// In uz, this message translates to:
  /// **'Ommabop'**
  String get catPopular;

  /// No description provided for @catSearchResults.
  ///
  /// In uz, this message translates to:
  /// **'Qidiruv natijalari'**
  String get catSearchResults;

  /// No description provided for @catSort.
  ///
  /// In uz, this message translates to:
  /// **'Tartiblash'**
  String get catSort;

  /// No description provided for @catSortPopular.
  ///
  /// In uz, this message translates to:
  /// **'Ommabop'**
  String get catSortPopular;

  /// No description provided for @catSortName.
  ///
  /// In uz, this message translates to:
  /// **'Nomi bo‘yicha'**
  String get catSortName;

  /// No description provided for @catSortPriceAsc.
  ///
  /// In uz, this message translates to:
  /// **'Avval arzon'**
  String get catSortPriceAsc;

  /// No description provided for @catSortPriceDesc.
  ///
  /// In uz, this message translates to:
  /// **'Avval qimmat'**
  String get catSortPriceDesc;

  /// No description provided for @catNoProducts.
  ///
  /// In uz, this message translates to:
  /// **'Hozircha mahsulot yo‘q'**
  String get catNoProducts;

  /// No description provided for @catNothingFound.
  ///
  /// In uz, this message translates to:
  /// **'Hech narsa topilmadi'**
  String get catNothingFound;

  /// No description provided for @catDeliveryBannerTitle.
  ///
  /// In uz, this message translates to:
  /// **'Eshigingizgacha yetkazamiz'**
  String get catDeliveryBannerTitle;

  /// No description provided for @catFreeDeliveryFrom.
  ///
  /// In uz, this message translates to:
  /// **'{sum}dan yuqori buyurtmaga yetkazish bepul'**
  String catFreeDeliveryFrom(String sum);

  /// No description provided for @catAddToCart.
  ///
  /// In uz, this message translates to:
  /// **'Savatchaga qo‘shish'**
  String get catAddToCart;

  /// No description provided for @catAddToCartSum.
  ///
  /// In uz, this message translates to:
  /// **'Savatchaga · {sum}'**
  String catAddToCartSum(String sum);

  /// No description provided for @catAddedToCart.
  ///
  /// In uz, this message translates to:
  /// **'Savatchaga qo‘shildi'**
  String get catAddedToCart;

  /// No description provided for @catBack.
  ///
  /// In uz, this message translates to:
  /// **'Orqaga'**
  String get catBack;

  /// No description provided for @catBarcode.
  ///
  /// In uz, this message translates to:
  /// **'Shtrix-kod {code}'**
  String catBarcode(String code);

  /// No description provided for @catInStock.
  ///
  /// In uz, this message translates to:
  /// **'Omborda bor · {qty}'**
  String catInStock(String qty);

  /// No description provided for @catOutOfStock.
  ///
  /// In uz, this message translates to:
  /// **'Tugagan'**
  String get catOutOfStock;

  /// No description provided for @cartEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Savatcha bo‘sh'**
  String get cartEmpty;

  /// No description provided for @cartEmptyHint.
  ///
  /// In uz, this message translates to:
  /// **'Katalogdan mahsulot tanlang'**
  String get cartEmptyHint;

  /// No description provided for @cartGoShopping.
  ///
  /// In uz, this message translates to:
  /// **'Xaridga o‘tish'**
  String get cartGoShopping;

  /// No description provided for @cartClear.
  ///
  /// In uz, this message translates to:
  /// **'Tozalash'**
  String get cartClear;

  /// No description provided for @cartClearConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Savatcha tozalansinmi?'**
  String get cartClearConfirm;

  /// No description provided for @cartCheckout.
  ///
  /// In uz, this message translates to:
  /// **'Rasmiylashtirish'**
  String get cartCheckout;

  /// No description provided for @cartFreeDeliveryLeft.
  ///
  /// In uz, this message translates to:
  /// **'Bepul yetkazishgacha {sum} qoldi'**
  String cartFreeDeliveryLeft(String sum);

  /// No description provided for @cartFreeDeliveryReached.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish bepul!'**
  String get cartFreeDeliveryReached;

  /// No description provided for @cartMinOrderShortfall.
  ///
  /// In uz, this message translates to:
  /// **'Minimal buyurtma {min}. Yana {left} lik mahsulot qo‘shing.'**
  String cartMinOrderShortfall(String min, String left);

  /// No description provided for @cartProblemNotFound.
  ///
  /// In uz, this message translates to:
  /// **'Endi sotuvda yo‘q'**
  String get cartProblemNotFound;

  /// No description provided for @cartProblemOutOfStock.
  ///
  /// In uz, this message translates to:
  /// **'Tugagan'**
  String get cartProblemOutOfStock;

  /// No description provided for @cartProblemNotEnough.
  ///
  /// In uz, this message translates to:
  /// **'Faqat {qty} mavjud'**
  String cartProblemNotEnough(String qty);

  /// No description provided for @cartProblemInvalidQuantity.
  ///
  /// In uz, this message translates to:
  /// **'Miqdor noto‘g‘ri'**
  String get cartProblemInvalidQuantity;

  /// No description provided for @cartFixQuantity.
  ///
  /// In uz, this message translates to:
  /// **'Kamaytirish'**
  String get cartFixQuantity;

  /// No description provided for @cartRemove.
  ///
  /// In uz, this message translates to:
  /// **'O‘chirish'**
  String get cartRemove;

  /// No description provided for @cartItemsKinds.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotlar ({count} xil)'**
  String cartItemsKinds(int count);

  /// No description provided for @cartDelivery.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazib berish'**
  String get cartDelivery;

  /// No description provided for @cartFree.
  ///
  /// In uz, this message translates to:
  /// **'Bepul'**
  String get cartFree;

  /// No description provided for @cartTotal.
  ///
  /// In uz, this message translates to:
  /// **'Jami'**
  String get cartTotal;

  /// No description provided for @checkoutTitle.
  ///
  /// In uz, this message translates to:
  /// **'Rasmiylashtirish'**
  String get checkoutTitle;

  /// No description provided for @checkoutDelivery.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazib berish'**
  String get checkoutDelivery;

  /// No description provided for @checkoutPickup.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kondan olib ketish'**
  String get checkoutPickup;

  /// No description provided for @checkoutPickupFrom.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketish manzili'**
  String get checkoutPickupFrom;

  /// No description provided for @checkoutAddress.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish manzili'**
  String get checkoutAddress;

  /// No description provided for @checkoutAddAddress.
  ///
  /// In uz, this message translates to:
  /// **'Yangi manzil qo‘shish'**
  String get checkoutAddAddress;

  /// No description provided for @checkoutNoAddress.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish uchun manzil kiriting'**
  String get checkoutNoAddress;

  /// No description provided for @checkoutEntrance.
  ///
  /// In uz, this message translates to:
  /// **'{v}-podyezd'**
  String checkoutEntrance(String v);

  /// No description provided for @checkoutFloor.
  ///
  /// In uz, this message translates to:
  /// **'{v}-qavat'**
  String checkoutFloor(String v);

  /// No description provided for @checkoutApartment.
  ///
  /// In uz, this message translates to:
  /// **'{v}-xonadon'**
  String checkoutApartment(String v);

  /// No description provided for @checkoutTime.
  ///
  /// In uz, this message translates to:
  /// **'Vaqt'**
  String get checkoutTime;

  /// No description provided for @checkoutAsap.
  ///
  /// In uz, this message translates to:
  /// **'Imkon qadar tez'**
  String get checkoutAsap;

  /// No description provided for @checkoutStoreClosed.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon hozir yopiq'**
  String get checkoutStoreClosed;

  /// No description provided for @checkoutPickTime.
  ///
  /// In uz, this message translates to:
  /// **'Vaqtni tanlash'**
  String get checkoutPickTime;

  /// No description provided for @checkoutPickTimeHint.
  ///
  /// In uz, this message translates to:
  /// **'Bugun / ertaga, 1 soatlik oraliq'**
  String get checkoutPickTimeHint;

  /// No description provided for @checkoutToday.
  ///
  /// In uz, this message translates to:
  /// **'Bugun {range}'**
  String checkoutToday(String range);

  /// No description provided for @checkoutTomorrow.
  ///
  /// In uz, this message translates to:
  /// **'Ertaga {range}'**
  String checkoutTomorrow(String range);

  /// No description provided for @checkoutNoSlots.
  ///
  /// In uz, this message translates to:
  /// **'Bugun va ertaga bo‘sh vaqt yo‘q'**
  String get checkoutNoSlots;

  /// No description provided for @checkoutPayment.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov usuli'**
  String get checkoutPayment;

  /// No description provided for @checkoutNoPayment.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konda to‘lov usuli sozlanmagan'**
  String get checkoutNoPayment;

  /// No description provided for @checkoutPayCash.
  ///
  /// In uz, this message translates to:
  /// **'Naqd pul'**
  String get checkoutPayCash;

  /// No description provided for @checkoutPayCashHint.
  ///
  /// In uz, this message translates to:
  /// **'Qabul qilganda to‘lanadi'**
  String get checkoutPayCashHint;

  /// No description provided for @checkoutPayCard.
  ///
  /// In uz, this message translates to:
  /// **'Karta (terminal)'**
  String get checkoutPayCard;

  /// No description provided for @checkoutPayCardHint.
  ///
  /// In uz, this message translates to:
  /// **'Qabul qilganda terminal orqali'**
  String get checkoutPayCardHint;

  /// No description provided for @checkoutPayTransfer.
  ///
  /// In uz, this message translates to:
  /// **'Kartaga o‘tkazma'**
  String get checkoutPayTransfer;

  /// No description provided for @checkoutPayTransferHint.
  ///
  /// In uz, this message translates to:
  /// **'Yig‘ilgandan keyin karta raqami ko‘rsatiladi'**
  String get checkoutPayTransferHint;

  /// No description provided for @checkoutOutOfStock.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulot tugab qolsa'**
  String get checkoutOutOfStock;

  /// No description provided for @checkoutPolicyCallMe.
  ///
  /// In uz, this message translates to:
  /// **'Menga qo‘ng‘iroq qiling'**
  String get checkoutPolicyCallMe;

  /// No description provided for @checkoutPolicyReplace.
  ///
  /// In uz, this message translates to:
  /// **'O‘xshashiga almashtiring'**
  String get checkoutPolicyReplace;

  /// No description provided for @checkoutPolicyRemove.
  ///
  /// In uz, this message translates to:
  /// **'Olib tashlang'**
  String get checkoutPolicyRemove;

  /// No description provided for @checkoutCommentCourier.
  ///
  /// In uz, this message translates to:
  /// **'Kuryerga izoh'**
  String get checkoutCommentCourier;

  /// No description provided for @checkoutComment.
  ///
  /// In uz, this message translates to:
  /// **'Izoh'**
  String get checkoutComment;

  /// No description provided for @checkoutCommentHint.
  ///
  /// In uz, this message translates to:
  /// **'Masalan: domofon ishlamaydi, qo‘ng‘iroq qiling'**
  String get checkoutCommentHint;

  /// No description provided for @checkoutContactless.
  ///
  /// In uz, this message translates to:
  /// **'Eshik oldiga qoldirish (kontaktsiz)'**
  String get checkoutContactless;

  /// No description provided for @checkoutFixCart.
  ///
  /// In uz, this message translates to:
  /// **'Savatchadagi muammolarni tuzating'**
  String get checkoutFixCart;

  /// No description provided for @checkoutItemsPlusDelivery.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotlar {items} + yetkazish {delivery}'**
  String checkoutItemsPlusDelivery(String items, String delivery);

  /// No description provided for @checkoutItemsOnly.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotlar {items}'**
  String checkoutItemsOnly(String items);

  /// No description provided for @checkoutPlaceOrder.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma berish'**
  String get checkoutPlaceOrder;

  /// No description provided for @checkoutAddressRequired.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish manzilini tanlang'**
  String get checkoutAddressRequired;

  /// No description provided for @checkoutPaymentRequired.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov usulini tanlang'**
  String get checkoutPaymentRequired;

  /// No description provided for @checkoutTimeRequired.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazish vaqtini tanlang'**
  String get checkoutTimeRequired;

  /// No description provided for @legalTerms.
  ///
  /// In uz, this message translates to:
  /// **'Foydalanish shartlari'**
  String get legalTerms;

  /// No description provided for @legalPrivacy.
  ///
  /// In uz, this message translates to:
  /// **'Maxfiylik siyosati'**
  String get legalPrivacy;

  /// No description provided for @legalAgreePrefix.
  ///
  /// In uz, this message translates to:
  /// **'Davom etish orqali '**
  String get legalAgreePrefix;

  /// No description provided for @legalAgreeMiddle.
  ///
  /// In uz, this message translates to:
  /// **' va '**
  String get legalAgreeMiddle;

  /// No description provided for @legalAgreeSuffix.
  ///
  /// In uz, this message translates to:
  /// **'ga rozilik bildirasiz.'**
  String get legalAgreeSuffix;

  /// No description provided for @legalTermsLink.
  ///
  /// In uz, this message translates to:
  /// **'foydalanish shartlari'**
  String get legalTermsLink;

  /// No description provided for @legalPrivacyLink.
  ///
  /// In uz, this message translates to:
  /// **'maxfiylik siyosati'**
  String get legalPrivacyLink;

  /// No description provided for @updateRequiredTitle.
  ///
  /// In uz, this message translates to:
  /// **'Yangilash kerak'**
  String get updateRequiredTitle;

  /// No description provided for @updateRequiredMessage.
  ///
  /// In uz, this message translates to:
  /// **'Ilovaning bu versiyasi endi qo‘llab-quvvatlanmaydi. Davom etish uchun yangi versiyani o‘rnating.'**
  String get updateRequiredMessage;

  /// No description provided for @updateButton.
  ///
  /// In uz, this message translates to:
  /// **'Yangilash'**
  String get updateButton;

  /// No description provided for @updateAvailableTitle.
  ///
  /// In uz, this message translates to:
  /// **'Yangi versiya mavjud'**
  String get updateAvailableTitle;

  /// No description provided for @updateAvailableMessage.
  ///
  /// In uz, this message translates to:
  /// **'Ilovaning yangi versiyasini o‘rnatishni tavsiya qilamiz.'**
  String get updateAvailableMessage;

  /// No description provided for @updateAvailableVersion.
  ///
  /// In uz, this message translates to:
  /// **'{version} versiyasi chiqdi. Yangilashni tavsiya qilamiz.'**
  String updateAvailableVersion(String version);

  /// No description provided for @updateLater.
  ///
  /// In uz, this message translates to:
  /// **'Keyinroq'**
  String get updateLater;

  /// No description provided for @updateMaintenanceTitle.
  ///
  /// In uz, this message translates to:
  /// **'Texnik ishlar'**
  String get updateMaintenanceTitle;

  /// No description provided for @updateMaintenanceMessage.
  ///
  /// In uz, this message translates to:
  /// **'Hozir texnik ishlar olib borilmoqda. Birozdan keyin qayta urinib ko‘ring.'**
  String get updateMaintenanceMessage;

  /// No description provided for @updateRetry.
  ///
  /// In uz, this message translates to:
  /// **'Qayta urinish'**
  String get updateRetry;

  /// No description provided for @langSheetTitle.
  ///
  /// In uz, this message translates to:
  /// **'Ilova tili'**
  String get langSheetTitle;

  /// No description provided for @langUzbek.
  ///
  /// In uz, this message translates to:
  /// **'O‘zbekcha'**
  String get langUzbek;

  /// No description provided for @langRussian.
  ///
  /// In uz, this message translates to:
  /// **'Русский'**
  String get langRussian;

  /// No description provided for @langDevice.
  ///
  /// In uz, this message translates to:
  /// **'Qurilma tili'**
  String get langDevice;

  /// No description provided for @pushChannelName.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmalar'**
  String get pushChannelName;

  /// No description provided for @pushChannelDescription.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma holati haqida xabarlar'**
  String get pushChannelDescription;

  /// No description provided for @namePromptHint.
  ///
  /// In uz, this message translates to:
  /// **'Ismingizni kiriting — do‘kon va kuryer sizga shunday murojaat qiladi.'**
  String get namePromptHint;

  /// No description provided for @namePromptSkip.
  ///
  /// In uz, this message translates to:
  /// **'Keyinroq'**
  String get namePromptSkip;

  /// No description provided for @deliveryMinutesRange.
  ///
  /// In uz, this message translates to:
  /// **'{min}–{max} daqiqa'**
  String deliveryMinutesRange(String min, String max);

  /// No description provided for @deliveryMinutesUpTo.
  ///
  /// In uz, this message translates to:
  /// **'{minutes} daqiqagacha'**
  String deliveryMinutesUpTo(String minutes);

  /// No description provided for @mapPickerTitle.
  ///
  /// In uz, this message translates to:
  /// **'Xaritada belgilash'**
  String get mapPickerTitle;

  /// No description provided for @mapPickerConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Shu yerni tanlash'**
  String get mapPickerConfirm;

  /// No description provided for @addressMapPick.
  ///
  /// In uz, this message translates to:
  /// **'Xaritada belgilash'**
  String get addressMapPick;

  /// No description provided for @addressMapChange.
  ///
  /// In uz, this message translates to:
  /// **'Xaritada o‘zgartirish'**
  String get addressMapChange;

  /// No description provided for @addressMapCoords.
  ///
  /// In uz, this message translates to:
  /// **'Koordinata: {coords}'**
  String addressMapCoords(String coords);

  /// No description provided for @ordersProofCamera.
  ///
  /// In uz, this message translates to:
  /// **'Kamerada suratga olish'**
  String get ordersProofCamera;

  /// No description provided for @ordersProofGallery.
  ///
  /// In uz, this message translates to:
  /// **'Galereyadan tanlash'**
  String get ordersProofGallery;

  /// No description provided for @ordersProofPickFailed.
  ///
  /// In uz, this message translates to:
  /// **'Rasmni olib bo‘lmadi. Kamera yoki galereyaga ruxsat bering.'**
  String get ordersProofPickFailed;

  /// No description provided for @ordersReorderEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Bu buyurtmadan savatchaga qo‘shiladigan mahsulot yo‘q'**
  String get ordersReorderEmpty;

  /// No description provided for @ordersCourier.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer: {name}'**
  String ordersCourier(String name);

  /// No description provided for @ordersCallCourier.
  ///
  /// In uz, this message translates to:
  /// **'Kuryerga qo‘ng‘iroq'**
  String get ordersCallCourier;

  /// No description provided for @ordersEta.
  ///
  /// In uz, this message translates to:
  /// **'Taxminiy yetkazish: {range}'**
  String ordersEta(String range);

  /// No description provided for @ordersFiscalReceipt.
  ///
  /// In uz, this message translates to:
  /// **'Fiskal chek'**
  String get ordersFiscalReceipt;

  /// No description provided for @ordersReceiptNumber.
  ///
  /// In uz, this message translates to:
  /// **'Chek № {number}'**
  String ordersReceiptNumber(String number);

  /// No description provided for @ordersRefundPending.
  ///
  /// In uz, this message translates to:
  /// **'Pul qaytariladi'**
  String get ordersRefundPending;

  /// No description provided for @ordersRefundDone.
  ///
  /// In uz, this message translates to:
  /// **'Pul qaytarildi'**
  String get ordersRefundDone;

  /// No description provided for @courierEntry.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer sifatida kirish'**
  String get courierEntry;

  /// No description provided for @courierLoginTitle.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer kirishi'**
  String get courierLoginTitle;

  /// No description provided for @courierLoginSubtitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon bergan login va parol bilan kiring'**
  String get courierLoginSubtitle;

  /// No description provided for @courierUsername.
  ///
  /// In uz, this message translates to:
  /// **'Login'**
  String get courierUsername;

  /// No description provided for @courierPassword.
  ///
  /// In uz, this message translates to:
  /// **'Parol'**
  String get courierPassword;

  /// No description provided for @courierSignIn.
  ///
  /// In uz, this message translates to:
  /// **'Kirish'**
  String get courierSignIn;

  /// No description provided for @courierBackToCustomer.
  ///
  /// In uz, this message translates to:
  /// **'Mijoz sifatida kirish'**
  String get courierBackToCustomer;

  /// No description provided for @courierNotCourier.
  ///
  /// In uz, this message translates to:
  /// **'Bu akkaunt kuryer emas'**
  String get courierNotCourier;

  /// No description provided for @courierSessionExpired.
  ///
  /// In uz, this message translates to:
  /// **'Sessiya tugadi. Qayta kiring.'**
  String get courierSessionExpired;

  /// No description provided for @courierTabActive.
  ///
  /// In uz, this message translates to:
  /// **'Faol'**
  String get courierTabActive;

  /// No description provided for @courierTabToday.
  ///
  /// In uz, this message translates to:
  /// **'Bugun'**
  String get courierTabToday;

  /// No description provided for @courierRouteAll.
  ///
  /// In uz, this message translates to:
  /// **'Marshrut (hammasi)'**
  String get courierRouteAll;

  /// No description provided for @courierEmptyActive.
  ///
  /// In uz, this message translates to:
  /// **'Hozircha biriktirilgan buyurtma yo‘q'**
  String get courierEmptyActive;

  /// No description provided for @courierEmptyToday.
  ///
  /// In uz, this message translates to:
  /// **'Bugun yakunlangan buyurtma yo‘q'**
  String get courierEmptyToday;

  /// No description provided for @courierMoney.
  ///
  /// In uz, this message translates to:
  /// **'{amount} so‘m'**
  String courierMoney(String amount);

  /// No description provided for @courierCollectCash.
  ///
  /// In uz, this message translates to:
  /// **'{amount} olish kerak'**
  String courierCollectCash(String amount);

  /// No description provided for @courierPaid.
  ///
  /// In uz, this message translates to:
  /// **'To‘langan'**
  String get courierPaid;

  /// No description provided for @courierPaymentPending.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov kutilmoqda'**
  String get courierPaymentPending;

  /// No description provided for @courierDistanceKm.
  ///
  /// In uz, this message translates to:
  /// **'{value} km'**
  String courierDistanceKm(String value);

  /// No description provided for @courierDistanceM.
  ///
  /// In uz, this message translates to:
  /// **'{value} m'**
  String courierDistanceM(String value);

  /// No description provided for @courierItemsCount.
  ///
  /// In uz, this message translates to:
  /// **'{count} ta mahsulot'**
  String courierItemsCount(String count);

  /// No description provided for @courierLogout.
  ///
  /// In uz, this message translates to:
  /// **'Chiqish'**
  String get courierLogout;

  /// No description provided for @courierLogoutConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer rejimidan chiqasizmi?'**
  String get courierLogoutConfirm;

  /// No description provided for @courierCancel.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilish'**
  String get courierCancel;

  /// No description provided for @courierRetry.
  ///
  /// In uz, this message translates to:
  /// **'Qayta urinish'**
  String get courierRetry;

  /// No description provided for @courierCall.
  ///
  /// In uz, this message translates to:
  /// **'Qo‘ng‘iroq'**
  String get courierCall;

  /// No description provided for @courierCustomer.
  ///
  /// In uz, this message translates to:
  /// **'Mijoz'**
  String get courierCustomer;

  /// No description provided for @courierAddress.
  ///
  /// In uz, this message translates to:
  /// **'Manzil'**
  String get courierAddress;

  /// No description provided for @courierEntrance.
  ///
  /// In uz, this message translates to:
  /// **'Podyezd'**
  String get courierEntrance;

  /// No description provided for @courierFloor.
  ///
  /// In uz, this message translates to:
  /// **'Qavat'**
  String get courierFloor;

  /// No description provided for @courierApartment.
  ///
  /// In uz, this message translates to:
  /// **'Xonadon'**
  String get courierApartment;

  /// No description provided for @courierIntercom.
  ///
  /// In uz, this message translates to:
  /// **'Domofon'**
  String get courierIntercom;

  /// No description provided for @courierLandmark.
  ///
  /// In uz, this message translates to:
  /// **'Mo‘ljal'**
  String get courierLandmark;

  /// No description provided for @courierComment.
  ///
  /// In uz, this message translates to:
  /// **'Izoh'**
  String get courierComment;

  /// No description provided for @courierContactless.
  ///
  /// In uz, this message translates to:
  /// **'Kontaktsiz yetkazish'**
  String get courierContactless;

  /// No description provided for @courierItems.
  ///
  /// In uz, this message translates to:
  /// **'Tarkibi'**
  String get courierItems;

  /// No description provided for @courierTotal.
  ///
  /// In uz, this message translates to:
  /// **'Jami'**
  String get courierTotal;

  /// No description provided for @courierYandexNavi.
  ///
  /// In uz, this message translates to:
  /// **'Yandex Navigator'**
  String get courierYandexNavi;

  /// No description provided for @courierGoogleMaps.
  ///
  /// In uz, this message translates to:
  /// **'Google Maps'**
  String get courierGoogleMaps;

  /// No description provided for @courierDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazildi'**
  String get courierDelivered;

  /// No description provided for @courierFailed.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazib bo‘lmadi'**
  String get courierFailed;

  /// No description provided for @courierCashTitle.
  ///
  /// In uz, this message translates to:
  /// **'Naqd pul olindi'**
  String get courierCashTitle;

  /// No description provided for @courierCashHint.
  ///
  /// In uz, this message translates to:
  /// **'Olingan summa'**
  String get courierCashHint;

  /// No description provided for @courierConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Tasdiqlash'**
  String get courierConfirm;

  /// No description provided for @courierDeliveredConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma yetkazilganini tasdiqlaysizmi?'**
  String get courierDeliveredConfirm;

  /// No description provided for @courierFailedTitle.
  ///
  /// In uz, this message translates to:
  /// **'Nima uchun yetkazib bo‘lmadi?'**
  String get courierFailedTitle;

  /// No description provided for @courierReasonNoAnswer.
  ///
  /// In uz, this message translates to:
  /// **'Mijoz javob bermadi'**
  String get courierReasonNoAnswer;

  /// No description provided for @courierReasonNotFound.
  ///
  /// In uz, this message translates to:
  /// **'Manzil topilmadi'**
  String get courierReasonNotFound;

  /// No description provided for @courierReasonRefused.
  ///
  /// In uz, this message translates to:
  /// **'Mijoz rad etdi'**
  String get courierReasonRefused;

  /// No description provided for @courierReasonOther.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa'**
  String get courierReasonOther;

  /// No description provided for @courierReasonOtherHint.
  ///
  /// In uz, this message translates to:
  /// **'Sababni yozing'**
  String get courierReasonOtherHint;

  /// No description provided for @courierStatusNotDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Yetkazilmadi'**
  String get courierStatusNotDelivered;

  /// No description provided for @courierOpenAppError.
  ///
  /// In uz, this message translates to:
  /// **'Ilovani ochib bo‘lmadi'**
  String get courierOpenAppError;

  /// No description provided for @courierDoneDelivered.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma yetkazildi'**
  String get courierDoneDelivered;

  /// No description provided for @courierDoneFailed.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma do‘konga qaytarildi'**
  String get courierDoneFailed;

  /// No description provided for @courierMode.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer'**
  String get courierMode;

  /// No description provided for @courierNoCoordinates.
  ///
  /// In uz, this message translates to:
  /// **'Koordinata yo‘q — manzil matni bo‘yicha qidiriladi'**
  String get courierNoCoordinates;

  /// No description provided for @cartPromoTitle.
  ///
  /// In uz, this message translates to:
  /// **'Promokod'**
  String get cartPromoTitle;

  /// No description provided for @cartPromoHint.
  ///
  /// In uz, this message translates to:
  /// **'Promokodni kiriting'**
  String get cartPromoHint;

  /// No description provided for @cartPromoApply.
  ///
  /// In uz, this message translates to:
  /// **'Qo‘llash'**
  String get cartPromoApply;

  /// No description provided for @cartPromoRemove.
  ///
  /// In uz, this message translates to:
  /// **'Olib tashlash'**
  String get cartPromoRemove;

  /// No description provided for @cartPromoApplied.
  ///
  /// In uz, this message translates to:
  /// **'Chegirma: −{amount}'**
  String cartPromoApplied(String amount);

  /// No description provided for @cartPromoDiscount.
  ///
  /// In uz, this message translates to:
  /// **'Promokod chegirmasi'**
  String get cartPromoDiscount;

  /// No description provided for @cartBonusUsed.
  ///
  /// In uz, this message translates to:
  /// **'Bonusdan'**
  String get cartBonusUsed;

  /// No description provided for @checkoutBonusTitle.
  ///
  /// In uz, this message translates to:
  /// **'Bonus bilan to‘lash'**
  String get checkoutBonusTitle;

  /// No description provided for @checkoutBonusBalance.
  ///
  /// In uz, this message translates to:
  /// **'Balans: {amount}'**
  String checkoutBonusBalance(String amount);

  /// No description provided for @checkoutBonusWillUse.
  ///
  /// In uz, this message translates to:
  /// **'{amount} ishlatiladi'**
  String checkoutBonusWillUse(String amount);

  /// No description provided for @checkoutBonusMaxPercent.
  ///
  /// In uz, this message translates to:
  /// **'summaning {percent}% gacha'**
  String checkoutBonusMaxPercent(String percent);

  /// No description provided for @checkoutDeliveryUnavailable.
  ///
  /// In uz, this message translates to:
  /// **'Bu manzilga yetkazib berilmaydi'**
  String get checkoutDeliveryUnavailable;

  /// No description provided for @checkoutSwitchToPickup.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketishni tanlash'**
  String get checkoutSwitchToPickup;

  /// No description provided for @checkoutDistance.
  ///
  /// In uz, this message translates to:
  /// **'Masofa: {km} km'**
  String checkoutDistance(String km);

  /// No description provided for @checkoutPayPayme.
  ///
  /// In uz, this message translates to:
  /// **'Payme'**
  String get checkoutPayPayme;

  /// No description provided for @checkoutPayClick.
  ///
  /// In uz, this message translates to:
  /// **'Click'**
  String get checkoutPayClick;

  /// No description provided for @checkoutPayOnlineHint.
  ///
  /// In uz, this message translates to:
  /// **'Yig‘ilgandan keyin ilovada to‘lash'**
  String get checkoutPayOnlineHint;

  /// No description provided for @ordersPayOnlineTitle.
  ///
  /// In uz, this message translates to:
  /// **'Online to‘lov'**
  String get ordersPayOnlineTitle;

  /// No description provided for @ordersPayOnlineHint.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma yig‘ildi. Yakuniy summani ilovada to‘lang: {amount}'**
  String ordersPayOnlineHint(String amount);

  /// No description provided for @ordersPayWith.
  ///
  /// In uz, this message translates to:
  /// **'{provider} orqali to‘lash'**
  String ordersPayWith(String provider);

  /// No description provided for @ordersPayOpenFailed.
  ///
  /// In uz, this message translates to:
  /// **'To‘lov sahifasini ochib bo‘lmadi'**
  String get ordersPayOpenFailed;

  /// No description provided for @mapMyLocation.
  ///
  /// In uz, this message translates to:
  /// **'Joylashuvimni aniqlash'**
  String get mapMyLocation;

  /// No description provided for @mapLocationOff.
  ///
  /// In uz, this message translates to:
  /// **'Qurilmada joylashuv (GPS) o‘chiq'**
  String get mapLocationOff;

  /// No description provided for @mapLocationDenied.
  ///
  /// In uz, this message translates to:
  /// **'Joylashuvga ruxsat berilmadi'**
  String get mapLocationDenied;

  /// No description provided for @mapLocationDeniedForever.
  ///
  /// In uz, this message translates to:
  /// **'Joylashuvga ruxsat sozlamalarda o‘chirilgan'**
  String get mapLocationDeniedForever;

  /// No description provided for @mapLocationFailed.
  ///
  /// In uz, this message translates to:
  /// **'Joylashuvni aniqlab bo‘lmadi'**
  String get mapLocationFailed;

  /// No description provided for @mapOpenSettings.
  ///
  /// In uz, this message translates to:
  /// **'Sozlamalar'**
  String get mapOpenSettings;

  /// No description provided for @mapSearchHint.
  ///
  /// In uz, this message translates to:
  /// **'Manzilni qidirish'**
  String get mapSearchHint;

  /// No description provided for @mapSearchEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Hech narsa topilmadi'**
  String get mapSearchEmpty;

  /// No description provided for @mapAddressLoading.
  ///
  /// In uz, this message translates to:
  /// **'Manzil aniqlanmoqda…'**
  String get mapAddressLoading;

  /// No description provided for @mapAddressUnknown.
  ///
  /// In uz, this message translates to:
  /// **'Manzil aniqlanmadi — matnni o‘zingiz yozing'**
  String get mapAddressUnknown;

  /// No description provided for @mapMoveHint.
  ///
  /// In uz, this message translates to:
  /// **'Xaritani suring — pin ostidagi manzil aniqlanadi'**
  String get mapMoveHint;

  /// No description provided for @addressReplaceTitle.
  ///
  /// In uz, this message translates to:
  /// **'Manzil matni almashtirilsinmi?'**
  String get addressReplaceTitle;

  /// No description provided for @addressReplace.
  ///
  /// In uz, this message translates to:
  /// **'Almashtirish'**
  String get addressReplace;

  /// No description provided for @addressKeep.
  ///
  /// In uz, this message translates to:
  /// **'Qoldirish'**
  String get addressKeep;

  /// No description provided for @bonusTitle.
  ///
  /// In uz, this message translates to:
  /// **'Bonus karta'**
  String get bonusTitle;

  /// No description provided for @bonusBalance.
  ///
  /// In uz, this message translates to:
  /// **'Balans'**
  String get bonusBalance;

  /// No description provided for @bonusPoints.
  ///
  /// In uz, this message translates to:
  /// **'{value} ball'**
  String bonusPoints(String value);

  /// No description provided for @bonusHint.
  ///
  /// In uz, this message translates to:
  /// **'Kassada shu QR’ni ko‘rsating — cashback avtomatik yoziladi va ball bilan to‘lash mumkin'**
  String get bonusHint;

  /// No description provided for @bonusRefreshIn.
  ///
  /// In uz, this message translates to:
  /// **'Kod {time} dan keyin yangilanadi'**
  String bonusRefreshIn(String time);

  /// No description provided for @bonusRefreshing.
  ///
  /// In uz, this message translates to:
  /// **'Kod yangilanmoqda…'**
  String get bonusRefreshing;

  /// No description provided for @bonusRefreshFailed.
  ///
  /// In uz, this message translates to:
  /// **'Kodni yangilab bo‘lmadi'**
  String get bonusRefreshFailed;

  /// No description provided for @bonusExpired.
  ///
  /// In uz, this message translates to:
  /// **'Kod eskirdi — yangilang'**
  String get bonusExpired;

  /// No description provided for @bonusRetry.
  ///
  /// In uz, this message translates to:
  /// **'Qayta urinish'**
  String get bonusRetry;

  /// No description provided for @bonusQrSemantics.
  ///
  /// In uz, this message translates to:
  /// **'Bonus karta QR kodi'**
  String get bonusQrSemantics;

  /// No description provided for @bonusNoQr.
  ///
  /// In uz, this message translates to:
  /// **'Bu do‘kon uchun QR kod hozircha mavjud emas'**
  String get bonusNoQr;

  /// No description provided for @bonusHistory.
  ///
  /// In uz, this message translates to:
  /// **'Tarix'**
  String get bonusHistory;

  /// No description provided for @bonusHistoryEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Hali ball harakatlari yo‘q'**
  String get bonusHistoryEmpty;

  /// No description provided for @bonusTypeEarn.
  ///
  /// In uz, this message translates to:
  /// **'Cashback'**
  String get bonusTypeEarn;

  /// No description provided for @bonusTypeRedeem.
  ///
  /// In uz, this message translates to:
  /// **'Sarflandi'**
  String get bonusTypeRedeem;

  /// No description provided for @bonusTypeExpire.
  ///
  /// In uz, this message translates to:
  /// **'Muddati o‘tdi'**
  String get bonusTypeExpire;

  /// No description provided for @bonusTypeAdjust.
  ///
  /// In uz, this message translates to:
  /// **'Tuzatish'**
  String get bonusTypeAdjust;

  /// No description provided for @bonusTypeSignup.
  ///
  /// In uz, this message translates to:
  /// **'Ro‘yxatdan o‘tish bonusi'**
  String get bonusTypeSignup;

  /// No description provided for @bonusTypeRefund.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish'**
  String get bonusTypeRefund;

  /// No description provided for @bonusTypeOther.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa'**
  String get bonusTypeOther;

  /// No description provided for @bonusSourceStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon'**
  String get bonusSourceStore;

  /// No description provided for @bonusSourceOnline.
  ///
  /// In uz, this message translates to:
  /// **'Online'**
  String get bonusSourceOnline;

  /// No description provided for @bonusReceipt.
  ///
  /// In uz, this message translates to:
  /// **'Chek №{number}'**
  String bonusReceipt(String number);

  /// No description provided for @bonusGuestTitle.
  ///
  /// In uz, this message translates to:
  /// **'Bonus karta'**
  String get bonusGuestTitle;

  /// No description provided for @bonusGuestText.
  ///
  /// In uz, this message translates to:
  /// **'Telefon raqamingiz bilan kiring — har xariddan cashback yig‘ing va ball bilan to‘lang'**
  String get bonusGuestText;

  /// No description provided for @bonusNoStoresTitle.
  ///
  /// In uz, this message translates to:
  /// **'Hali bonus kartangiz yo‘q'**
  String get bonusNoStoresTitle;

  /// No description provided for @bonusNoStoresText.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon tanlang — karta avtomatik ochiladi'**
  String get bonusNoStoresText;

  /// No description provided for @bonusChooseStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon tanlash'**
  String get bonusChooseStore;

  /// No description provided for @storeChooseBranch.
  ///
  /// In uz, this message translates to:
  /// **'Filialni tanlang'**
  String get storeChooseBranch;

  /// No description provided for @storeUnavailableTitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon vaqtincha ishlamayapti'**
  String get storeUnavailableTitle;

  /// No description provided for @storeUnavailableText.
  ///
  /// In uz, this message translates to:
  /// **'Keyinroq qayta urinib ko‘ring.'**
  String get storeUnavailableText;

  /// No description provided for @navBonus.
  ///
  /// In uz, this message translates to:
  /// **'Bonus'**
  String get navBonus;

  /// No description provided for @favTitle.
  ///
  /// In uz, this message translates to:
  /// **'Sevimlilar'**
  String get favTitle;

  /// No description provided for @favAdd.
  ///
  /// In uz, this message translates to:
  /// **'Sevimlilarga qo‘shish'**
  String get favAdd;

  /// No description provided for @favRemove.
  ///
  /// In uz, this message translates to:
  /// **'Sevimlilardan olib tashlash'**
  String get favRemove;

  /// No description provided for @favLoginTitle.
  ///
  /// In uz, this message translates to:
  /// **'Sevimlilarga qo‘shish'**
  String get favLoginTitle;

  /// No description provided for @favLoginText.
  ///
  /// In uz, this message translates to:
  /// **'Sevimli mahsulotlarni saqlash uchun telefon raqamingiz bilan kiring.'**
  String get favLoginText;

  /// No description provided for @favEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Sevimlilar hozircha bo‘sh'**
  String get favEmpty;

  /// No description provided for @favEmptyHint.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotdagi ♥ belgisini bosing — u shu yerda saqlanadi'**
  String get favEmptyHint;

  /// No description provided for @mkAll.
  ///
  /// In uz, this message translates to:
  /// **'Hammasi'**
  String get mkAll;

  /// No description provided for @mkClose.
  ///
  /// In uz, this message translates to:
  /// **'Yopish'**
  String get mkClose;

  /// No description provided for @mkStoryOpen.
  ///
  /// In uz, this message translates to:
  /// **'Batafsil'**
  String get mkStoryOpen;

  /// No description provided for @mkEndsIn.
  ///
  /// In uz, this message translates to:
  /// **'Tugashiga {time}'**
  String mkEndsIn(String time);

  /// No description provided for @mkDiscountEndsIn.
  ///
  /// In uz, this message translates to:
  /// **'Aksiya tugashiga {time}'**
  String mkDiscountEndsIn(String time);

  /// No description provided for @mkDiscountsTitle.
  ///
  /// In uz, this message translates to:
  /// **'Aksiyalar'**
  String get mkDiscountsTitle;

  /// No description provided for @mkDiscountsEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Hozircha aksiyadagi mahsulotlar yo‘q'**
  String get mkDiscountsEmpty;

  /// No description provided for @mkDiscountsEntry.
  ///
  /// In uz, this message translates to:
  /// **'Aksiyalar ({count})'**
  String mkDiscountsEntry(int count);

  /// No description provided for @mkDiscountsEntryHint.
  ///
  /// In uz, this message translates to:
  /// **'Chegirmadagi barcha mahsulotlar'**
  String get mkDiscountsEntryHint;

  /// No description provided for @mkBuyAgain.
  ///
  /// In uz, this message translates to:
  /// **'Yana olasizmi?'**
  String get mkBuyAgain;

  /// No description provided for @mkTop10.
  ///
  /// In uz, this message translates to:
  /// **'Top 10'**
  String get mkTop10;

  /// No description provided for @mkRelated.
  ///
  /// In uz, this message translates to:
  /// **'Bular bilan birga olishadi'**
  String get mkRelated;

  /// No description provided for @mkCartSuggestions.
  ///
  /// In uz, this message translates to:
  /// **'Qo‘shib qo‘ying'**
  String get mkCartSuggestions;

  /// No description provided for @mkReorderTitle.
  ///
  /// In uz, this message translates to:
  /// **'Oxirgi buyurtmani takrorlash'**
  String get mkReorderTitle;

  /// No description provided for @mkReorderSubtitle.
  ///
  /// In uz, this message translates to:
  /// **'№{number} · {sum}'**
  String mkReorderSubtitle(String number, String sum);

  /// No description provided for @mkReorderButton.
  ///
  /// In uz, this message translates to:
  /// **'Takrorlash'**
  String get mkReorderButton;

  /// No description provided for @mkNotifyMe.
  ///
  /// In uz, this message translates to:
  /// **'Kelganda xabar berish'**
  String get mkNotifyMe;

  /// No description provided for @mkNotifySubscribed.
  ///
  /// In uz, this message translates to:
  /// **'Kelganda xabar beramiz'**
  String get mkNotifySubscribed;

  /// No description provided for @mkNotifyOn.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulot sotuvga chiqqanda xabar beramiz'**
  String get mkNotifyOn;

  /// No description provided for @mkNotifyOff.
  ///
  /// In uz, this message translates to:
  /// **'Xabarnoma bekor qilindi'**
  String get mkNotifyOff;

  /// No description provided for @cdDaysHours.
  ///
  /// In uz, this message translates to:
  /// **'{days} kun {hours} soat'**
  String cdDaysHours(int days, int hours);

  /// No description provided for @cdHoursMinutes.
  ///
  /// In uz, this message translates to:
  /// **'{hours} soat {minutes} daq'**
  String cdHoursMinutes(int hours, int minutes);

  /// No description provided for @cdMinutes.
  ///
  /// In uz, this message translates to:
  /// **'{minutes} daq'**
  String cdMinutes(int minutes);

  /// No description provided for @searchHistory.
  ///
  /// In uz, this message translates to:
  /// **'Qidiruv tarixi'**
  String get searchHistory;

  /// No description provided for @searchHistoryClear.
  ///
  /// In uz, this message translates to:
  /// **'Tozalash'**
  String get searchHistoryClear;

  /// No description provided for @scanTitle.
  ///
  /// In uz, this message translates to:
  /// **'Shtrix-kodni skanerlash'**
  String get scanTitle;

  /// No description provided for @scanTorch.
  ///
  /// In uz, this message translates to:
  /// **'Chiroq'**
  String get scanTorch;

  /// No description provided for @scanHint.
  ///
  /// In uz, this message translates to:
  /// **'Shtrix-kodni ramka ichiga joylashtiring'**
  String get scanHint;

  /// No description provided for @scanError.
  ///
  /// In uz, this message translates to:
  /// **'Kamerani ochib bo‘lmadi. Sozlamalarda kameraga ruxsat bering.'**
  String get scanError;

  /// No description provided for @rateTitle.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtmani baholang'**
  String get rateTitle;

  /// No description provided for @rateSubtitle.
  ///
  /// In uz, this message translates to:
  /// **'Fikringiz xizmatni yaxshilashga yordam beradi'**
  String get rateSubtitle;

  /// No description provided for @rateYourRating.
  ///
  /// In uz, this message translates to:
  /// **'Sizning bahoingiz'**
  String get rateYourRating;

  /// No description provided for @rateCommentHint.
  ///
  /// In uz, this message translates to:
  /// **'Izoh (ixtiyoriy)'**
  String get rateCommentHint;

  /// No description provided for @rateSubmit.
  ///
  /// In uz, this message translates to:
  /// **'Yuborish'**
  String get rateSubmit;

  /// No description provided for @rateThanks.
  ///
  /// In uz, this message translates to:
  /// **'Rahmat! Bahoingiz qabul qilindi'**
  String get rateThanks;

  /// No description provided for @rateStars.
  ///
  /// In uz, this message translates to:
  /// **'{count} yulduz'**
  String rateStars(int count);

  /// No description provided for @rateTagFast.
  ///
  /// In uz, this message translates to:
  /// **'Tez yetkazildi'**
  String get rateTagFast;

  /// No description provided for @rateTagFresh.
  ///
  /// In uz, this message translates to:
  /// **'Mahsulotlar yangi'**
  String get rateTagFresh;

  /// No description provided for @rateTagPolite.
  ///
  /// In uz, this message translates to:
  /// **'Xushmuomala kuryer'**
  String get rateTagPolite;

  /// No description provided for @rateTagAccurate.
  ///
  /// In uz, this message translates to:
  /// **'Hammasi to‘g‘ri'**
  String get rateTagAccurate;

  /// No description provided for @rateTagLate.
  ///
  /// In uz, this message translates to:
  /// **'Kechikdi'**
  String get rateTagLate;

  /// No description provided for @rateTagDamaged.
  ///
  /// In uz, this message translates to:
  /// **'Shikastlangan'**
  String get rateTagDamaged;

  /// No description provided for @rateTagMissing.
  ///
  /// In uz, this message translates to:
  /// **'Nimadir yetishmadi'**
  String get rateTagMissing;

  /// No description provided for @rateTagWrongItem.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa mahsulot keldi'**
  String get rateTagWrongItem;

  /// No description provided for @supportTitle.
  ///
  /// In uz, this message translates to:
  /// **'Yordam'**
  String get supportTitle;

  /// No description provided for @supportText.
  ///
  /// In uz, this message translates to:
  /// **'Savolingiz bormi? Biz bilan bog‘laning'**
  String get supportText;

  /// No description provided for @supportCall.
  ///
  /// In uz, this message translates to:
  /// **'Qo‘ng‘iroq qilish'**
  String get supportCall;

  /// No description provided for @supportTelegram.
  ///
  /// In uz, this message translates to:
  /// **'Telegram’da yozish'**
  String get supportTelegram;

  /// No description provided for @supportNone.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon aloqa ma’lumotlarini hali kiritmagan'**
  String get supportNone;

  /// No description provided for @refTitle.
  ///
  /// In uz, this message translates to:
  /// **'Do‘stni taklif qilish'**
  String get refTitle;

  /// No description provided for @refUnavailable.
  ///
  /// In uz, this message translates to:
  /// **'Bu do‘konda do‘stni taklif qilish hozircha mavjud emas'**
  String get refUnavailable;

  /// No description provided for @refHeadline.
  ///
  /// In uz, this message translates to:
  /// **'Do‘stingizni taklif qiling — {bonus} oling'**
  String refHeadline(String bonus);

  /// No description provided for @refExplain.
  ///
  /// In uz, this message translates to:
  /// **'Do‘stingiz ham {bonus} oladi — birinchi buyurtmasi yetkazilganda'**
  String refExplain(String bonus);

  /// No description provided for @refShareFallback.
  ///
  /// In uz, this message translates to:
  /// **'Mening kodim: {code}. Ilovada kiriting va {bonus} oling!'**
  String refShareFallback(String code, String bonus);

  /// No description provided for @refYourCode.
  ///
  /// In uz, this message translates to:
  /// **'Sizning kodingiz'**
  String get refYourCode;

  /// No description provided for @refCopy.
  ///
  /// In uz, this message translates to:
  /// **'Nusxa olish'**
  String get refCopy;

  /// No description provided for @refCopied.
  ///
  /// In uz, this message translates to:
  /// **'Kod nusxalandi'**
  String get refCopied;

  /// No description provided for @refShare.
  ///
  /// In uz, this message translates to:
  /// **'Ulashish'**
  String get refShare;

  /// No description provided for @refInvited.
  ///
  /// In uz, this message translates to:
  /// **'Taklif qilinganlar'**
  String get refInvited;

  /// No description provided for @refRewarded.
  ///
  /// In uz, this message translates to:
  /// **'Bonus olinganlar'**
  String get refRewarded;

  /// No description provided for @refHaveCode.
  ///
  /// In uz, this message translates to:
  /// **'Do‘stingiz kodi bormi?'**
  String get refHaveCode;

  /// No description provided for @refHaveCodeHint.
  ///
  /// In uz, this message translates to:
  /// **'Birinchi buyurtmangizgacha kiriting'**
  String get refHaveCodeHint;

  /// No description provided for @refCodeHint.
  ///
  /// In uz, this message translates to:
  /// **'Kod'**
  String get refCodeHint;

  /// No description provided for @refApplied.
  ///
  /// In uz, this message translates to:
  /// **'Kod qabul qilindi'**
  String get refApplied;

  /// No description provided for @tierNone.
  ///
  /// In uz, this message translates to:
  /// **'Daraja hali yo‘q'**
  String get tierNone;

  /// No description provided for @tierCurrent.
  ///
  /// In uz, this message translates to:
  /// **'{name} · {percent}% cashback'**
  String tierCurrent(String name, String percent);

  /// No description provided for @tierNextLeft.
  ///
  /// In uz, this message translates to:
  /// **'{name} darajasigacha {sum}'**
  String tierNextLeft(String name, String sum);

  /// No description provided for @tierTop.
  ///
  /// In uz, this message translates to:
  /// **'Siz eng yuqori darajadasiz'**
  String get tierTop;

  /// No description provided for @catOpensAt.
  ///
  /// In uz, this message translates to:
  /// **'{time} da ochiladi'**
  String catOpensAt(String time);

  /// No description provided for @checkoutWhenOpens.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon ochilganda'**
  String get checkoutWhenOpens;

  /// No description provided for @checkoutWhenOpensAt.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon ochilganda ({time})'**
  String checkoutWhenOpensAt(String time);

  /// No description provided for @checkoutPreorderHint.
  ///
  /// In uz, this message translates to:
  /// **'Oldindan buyurtma'**
  String get checkoutPreorderHint;

  /// No description provided for @ordersPreorderTitle.
  ///
  /// In uz, this message translates to:
  /// **'Oldindan buyurtma'**
  String get ordersPreorderTitle;

  /// No description provided for @ordersPreorderNote.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon hozir yopiq. Buyurtmangiz {time} da ko‘rib chiqiladi.'**
  String ordersPreorderNote(String time);

  /// No description provided for @ordersPreorderNoteNoTime.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kon hozir yopiq. Buyurtmangiz do‘kon ochilganda ko‘rib chiqiladi.'**
  String get ordersPreorderNoteNoTime;

  /// No description provided for @courierTabOffers.
  ///
  /// In uz, this message translates to:
  /// **'Takliflar'**
  String get courierTabOffers;

  /// No description provided for @courierOffersEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Hozircha taklif yo‘q'**
  String get courierOffersEmpty;

  /// No description provided for @courierOfferTitle.
  ///
  /// In uz, this message translates to:
  /// **'Yangi buyurtma'**
  String get courierOfferTitle;

  /// No description provided for @courierOfferNumber.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma #{number}'**
  String courierOfferNumber(String number);

  /// No description provided for @courierOfferExpired.
  ///
  /// In uz, this message translates to:
  /// **'Taklif vaqti tugadi'**
  String get courierOfferExpired;

  /// No description provided for @courierOfferAccept.
  ///
  /// In uz, this message translates to:
  /// **'Qabul qilaman'**
  String get courierOfferAccept;

  /// No description provided for @courierOfferDecline.
  ///
  /// In uz, this message translates to:
  /// **'Rad etaman'**
  String get courierOfferDecline;

  /// No description provided for @courierOfferTaken.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa kuryer oldi'**
  String get courierOfferTaken;

  /// No description provided for @courierOfferClosed.
  ///
  /// In uz, this message translates to:
  /// **'Taklif yopildi'**
  String get courierOfferClosed;

  /// No description provided for @courierOfferAccepted.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma sizga biriktirildi'**
  String get courierOfferAccepted;

  /// No description provided for @courierOfferCash.
  ///
  /// In uz, this message translates to:
  /// **'Naqd to‘lov'**
  String get courierOfferCash;

  /// No description provided for @courierOfferPaidOrCard.
  ///
  /// In uz, this message translates to:
  /// **'Karta / to‘langan'**
  String get courierOfferPaidOrCard;

  /// No description provided for @courierBack.
  ///
  /// In uz, this message translates to:
  /// **'Orqaga'**
  String get courierBack;

  /// No description provided for @courierPickedUpFromStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kondan oldim'**
  String get courierPickedUpFromStore;

  /// No description provided for @courierAwaitingPickup.
  ///
  /// In uz, this message translates to:
  /// **'Do‘kondan olish'**
  String get courierAwaitingPickup;

  /// No description provided for @courierDonePickedUp.
  ///
  /// In uz, this message translates to:
  /// **'Buyurtma yo‘lda'**
  String get courierDonePickedUp;

  /// No description provided for @courierReturnTask.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish'**
  String get courierReturnTask;

  /// No description provided for @courierReturnNumber.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish #{number}'**
  String courierReturnNumber(String number);

  /// No description provided for @courierReturnPickedUp.
  ///
  /// In uz, this message translates to:
  /// **'Olib ketildi'**
  String get courierReturnPickedUp;

  /// No description provided for @courierReturnPickedUpConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Tovarlarni mijozdan olganingizni tasdiqlaysizmi?'**
  String get courierReturnPickedUpConfirm;

  /// No description provided for @courierReturnDone.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish olib ketildi'**
  String get courierReturnDone;

  /// No description provided for @courierReturnNoCash.
  ///
  /// In uz, this message translates to:
  /// **'Pul berilmaydi — do‘kon o‘zi qaytaradi'**
  String get courierReturnNoCash;

  /// No description provided for @courierRejectItems.
  ///
  /// In uz, this message translates to:
  /// **'Ba’zi tovarlarni rad etdi'**
  String get courierRejectItems;

  /// No description provided for @courierRejectTitle.
  ///
  /// In uz, this message translates to:
  /// **'Mijoz qaysi tovarlarni olmadi?'**
  String get courierRejectTitle;

  /// No description provided for @courierRejectedEdit.
  ///
  /// In uz, this message translates to:
  /// **'Rad etilganlarni o‘zgartirish'**
  String get courierRejectedEdit;

  /// No description provided for @courierToCollect.
  ///
  /// In uz, this message translates to:
  /// **'Olinadigan summa'**
  String get courierToCollect;

  /// No description provided for @courierRejectApprox.
  ///
  /// In uz, this message translates to:
  /// **'Taxminiy — yakuniy summani server hisoblaydi'**
  String get courierRejectApprox;

  /// No description provided for @courierSave.
  ///
  /// In uz, this message translates to:
  /// **'Saqlash'**
  String get courierSave;

  /// No description provided for @returnReasonDamaged.
  ///
  /// In uz, this message translates to:
  /// **'Shikastlangan'**
  String get returnReasonDamaged;

  /// No description provided for @returnReasonWrongItem.
  ///
  /// In uz, this message translates to:
  /// **'Noto‘g‘ri tovar'**
  String get returnReasonWrongItem;

  /// No description provided for @returnReasonQuality.
  ///
  /// In uz, this message translates to:
  /// **'Sifatsiz'**
  String get returnReasonQuality;

  /// No description provided for @returnReasonExpired.
  ///
  /// In uz, this message translates to:
  /// **'Muddati o‘tgan'**
  String get returnReasonExpired;

  /// No description provided for @returnReasonMissing.
  ///
  /// In uz, this message translates to:
  /// **'Yetishmaydi'**
  String get returnReasonMissing;

  /// No description provided for @returnReasonOther.
  ///
  /// In uz, this message translates to:
  /// **'Boshqa'**
  String get returnReasonOther;

  /// No description provided for @refundMethodCash.
  ///
  /// In uz, this message translates to:
  /// **'Naqd (do‘konda)'**
  String get refundMethodCash;

  /// No description provided for @refundMethodTransfer.
  ///
  /// In uz, this message translates to:
  /// **'Kartaga o‘tkazma'**
  String get refundMethodTransfer;

  /// No description provided for @refundMethodBonus.
  ///
  /// In uz, this message translates to:
  /// **'Bonus ball'**
  String get refundMethodBonus;

  /// No description provided for @returnMethodCourier.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer olib ketadi'**
  String get returnMethodCourier;

  /// No description provided for @returnMethodStore.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konga o‘zingiz olib borasiz'**
  String get returnMethodStore;

  /// No description provided for @returnStatusRequested.
  ///
  /// In uz, this message translates to:
  /// **'Ko‘rib chiqilmoqda'**
  String get returnStatusRequested;

  /// No description provided for @returnStatusApproved.
  ///
  /// In uz, this message translates to:
  /// **'Tasdiqlandi'**
  String get returnStatusApproved;

  /// No description provided for @returnStatusPickupAssigned.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer biriktirildi'**
  String get returnStatusPickupAssigned;

  /// No description provided for @returnStatusPickedUp.
  ///
  /// In uz, this message translates to:
  /// **'Kuryer olib ketdi'**
  String get returnStatusPickedUp;

  /// No description provided for @returnStatusReceived.
  ///
  /// In uz, this message translates to:
  /// **'Do‘konga yetib keldi'**
  String get returnStatusReceived;

  /// No description provided for @returnStatusRefunded.
  ///
  /// In uz, this message translates to:
  /// **'Pul qaytarildi'**
  String get returnStatusRefunded;

  /// No description provided for @returnStatusRejected.
  ///
  /// In uz, this message translates to:
  /// **'Rad etildi'**
  String get returnStatusRejected;

  /// No description provided for @returnStatusCancelled.
  ///
  /// In uz, this message translates to:
  /// **'Bekor qilindi'**
  String get returnStatusCancelled;

  /// No description provided for @returnsTitle.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarishlarim'**
  String get returnsTitle;

  /// No description provided for @returnsEmpty.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarishlar yo‘q'**
  String get returnsEmpty;

  /// No description provided for @returnAction.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish'**
  String get returnAction;

  /// No description provided for @returnRequestTitle.
  ///
  /// In uz, this message translates to:
  /// **'Tovarni qaytarish'**
  String get returnRequestTitle;

  /// No description provided for @returnChooseItems.
  ///
  /// In uz, this message translates to:
  /// **'Qaytariladigan tovarlar'**
  String get returnChooseItems;

  /// No description provided for @returnNothingToReturn.
  ///
  /// In uz, this message translates to:
  /// **'Qaytariladigan tovar qolmagan'**
  String get returnNothingToReturn;

  /// No description provided for @returnReason.
  ///
  /// In uz, this message translates to:
  /// **'Sabab'**
  String get returnReason;

  /// No description provided for @returnSelectReason.
  ///
  /// In uz, this message translates to:
  /// **'Sababni tanlang'**
  String get returnSelectReason;

  /// No description provided for @returnComment.
  ///
  /// In uz, this message translates to:
  /// **'Izoh'**
  String get returnComment;

  /// No description provided for @returnCommentHint.
  ///
  /// In uz, this message translates to:
  /// **'Muammoni qisqacha yozing'**
  String get returnCommentHint;

  /// No description provided for @returnPhotos.
  ///
  /// In uz, this message translates to:
  /// **'Rasmlar (3 tagacha)'**
  String get returnPhotos;

  /// No description provided for @returnAddPhoto.
  ///
  /// In uz, this message translates to:
  /// **'Rasm'**
  String get returnAddPhoto;

  /// No description provided for @returnRemovePhoto.
  ///
  /// In uz, this message translates to:
  /// **'Rasmni olib tashlash'**
  String get returnRemovePhoto;

  /// No description provided for @returnRefundMethod.
  ///
  /// In uz, this message translates to:
  /// **'Pulni qaytarish usuli'**
  String get returnRefundMethod;

  /// No description provided for @returnCashbackNote.
  ///
  /// In uz, this message translates to:
  /// **'Shu tovarlar uchun berilgan keshbek ayiriladi.'**
  String get returnCashbackNote;

  /// No description provided for @returnAmount.
  ///
  /// In uz, this message translates to:
  /// **'Qaytariladigan summa'**
  String get returnAmount;

  /// No description provided for @returnSubmit.
  ///
  /// In uz, this message translates to:
  /// **'So‘rov yuborish'**
  String get returnSubmit;

  /// No description provided for @returnSelectItems.
  ///
  /// In uz, this message translates to:
  /// **'Kamida bitta tovar tanlang'**
  String get returnSelectItems;

  /// No description provided for @returnSent.
  ///
  /// In uz, this message translates to:
  /// **'So‘rov yuborildi'**
  String get returnSent;

  /// No description provided for @returnNumber.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish #{number}'**
  String returnNumber(String number);

  /// No description provided for @returnWithheld.
  ///
  /// In uz, this message translates to:
  /// **'Keshbek ushlab qolindi'**
  String get returnWithheld;

  /// No description provided for @returnCancel.
  ///
  /// In uz, this message translates to:
  /// **'So‘rovni bekor qilish'**
  String get returnCancel;

  /// No description provided for @returnCancelConfirm.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish so‘rovini bekor qilasizmi?'**
  String get returnCancelConfirm;

  /// No description provided for @returnCancelled.
  ///
  /// In uz, this message translates to:
  /// **'So‘rov bekor qilindi'**
  String get returnCancelled;

  /// No description provided for @returnRejectReason.
  ///
  /// In uz, this message translates to:
  /// **'Rad etish sababi: {reason}'**
  String returnRejectReason(String reason);

  /// No description provided for @returnTimeline.
  ///
  /// In uz, this message translates to:
  /// **'Holat'**
  String get returnTimeline;

  /// No description provided for @returnMethod.
  ///
  /// In uz, this message translates to:
  /// **'Qaytarish usuli'**
  String get returnMethod;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ru', 'uz'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ru':
      return AppLocalizationsRu();
    case 'uz':
      return AppLocalizationsUz();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
