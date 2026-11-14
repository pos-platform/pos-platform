// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Uzbek (`uz`).
class AppLocalizationsUz extends AppLocalizations {
  AppLocalizationsUz([String locale = 'uz']) : super(locale);

  @override
  String get appTitle => 'Xo‘ja Market';

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

  @override
  String get getCode => 'Kodni olish';

  @override
  String codeSentTelegram(String phone) {
    return 'Kod Telegram orqali $phone raqamiga yuborildi';
  }

  @override
  String codeSentSms(String phone) {
    return 'Kod $phone raqamiga yuborildi';
  }

  @override
  String resendIn(String time) {
    return 'qayta yuborish $time';
  }

  @override
  String get resendCode => 'Kodni qayta yuborish';

  @override
  String get sendViaSms => 'SMS orqali yuborish';

  @override
  String get changeNumber => 'Raqamni o‘zgartirish';

  @override
  String get invalidPhone => 'Telefon raqamni to‘liq kiriting';

  @override
  String get retry => 'Qayta urinish';

  @override
  String get signInRequired => 'Buyurtma berish uchun kiring';

  @override
  String get signOut => 'Chiqish';

  @override
  String profileAddressCount(int count) {
    return '$count ta';
  }

  @override
  String get profilePersonalInfo => 'Shaxsiy ma’lumotlar';

  @override
  String get profileAddresses => 'Manzillarim';

  @override
  String get profileLanguage => 'Til';

  @override
  String get profileLanguageName => 'O‘zbekcha';

  @override
  String get profileLanguageLater =>
      'Tilni o‘zgartirish keyingi versiyada. Hozir qurilma tili ishlatiladi.';

  @override
  String get profileCancel => 'Bekor qilish';

  @override
  String get profileSignOutTitle => 'Akkauntdan chiqasizmi?';

  @override
  String get profileSignOutMessage =>
      'Qayta kirish uchun telefon raqamingizga kod yuboriladi.';

  @override
  String get profileDeleteAccount => 'Akkauntni o‘chirish';

  @override
  String get profileDeleteTitle => 'Akkaunt o‘chirilsinmi?';

  @override
  String get profileDeleteMessage =>
      'Ism, manzillar va boshqa shaxsiy ma’lumotlar o‘chiriladi, barcha qurilmalardan chiqasiz. Buni qaytarib bo‘lmaydi.';

  @override
  String get profileDeleteConfirmWord => 'O‘CHIRISH';

  @override
  String profileDeleteTypeHint(String word) {
    return 'Tasdiqlash uchun $word deb yozing';
  }

  @override
  String get profileDeleteButton => 'O‘chirish';

  @override
  String get profileGuestTitle => 'Profilga kiring';

  @override
  String get profileGuestText =>
      'Buyurtmalar, manzillar va bonuslar uchun telefon raqamingiz bilan kiring.';

  @override
  String get profileNameBannerTitle => 'Ismingizni kiriting';

  @override
  String get profileNameBannerText =>
      'Do‘kon va kuryer sizga ism bilan murojaat qilishi uchun.';

  @override
  String get profileFirstName => 'Ism';

  @override
  String get profileLastName => 'Familiya';

  @override
  String get profilePhone => 'Telefon raqam';

  @override
  String get profilePhoneVerified => 'Tasdiqlangan';

  @override
  String get profileBirthDate => 'Tug‘ilgan sana';

  @override
  String get profileBirthDateHint =>
      'Tug‘ilgan kuningizda bonus sovg‘a qilamiz';

  @override
  String get profileGender => 'Jins';

  @override
  String get profileGenderMale => 'Erkak';

  @override
  String get profileGenderFemale => 'Ayol';

  @override
  String get profileEmail => 'Email';

  @override
  String get profileEmailHint => 'elektron chek va xabarlar uchun, ixtiyoriy';

  @override
  String get profileEmailInvalid => 'Email noto‘g‘ri';

  @override
  String get profileFirstNameRequired => 'Ismni kiriting';

  @override
  String get profileSave => 'Saqlash';

  @override
  String get profileSaved => 'Saqlandi';

  @override
  String get addressesTitle => 'Manzillarim';

  @override
  String get addressAdd => 'Yangi manzil qo‘shish';

  @override
  String get addressDefaultBadge => 'Asosiy';

  @override
  String get addressEmpty => 'Hali manzil qo‘shilmagan';

  @override
  String get addressOutOfZoneNote =>
      'Manzil yetkazish hududidan tashqarida bo‘lsa, buyurtmani do‘kondan olib ketishingiz mumkin.';

  @override
  String get addressNewTitle => 'Manzil qo‘shish';

  @override
  String get addressEditTitle => 'Manzilni tahrirlash';

  @override
  String get addressLine => 'Manzil';

  @override
  String get addressLineHint => 'Ko‘cha, uy raqami';

  @override
  String get addressLineRequired => 'Manzilni kiriting';

  @override
  String get addressEntrance => 'Podyezd';

  @override
  String get addressFloor => 'Qavat';

  @override
  String get addressApartment => 'Xonadon';

  @override
  String get addressIntercom => 'Domofon kodi';

  @override
  String get addressLandmark => 'Mo‘ljal';

  @override
  String get addressLandmarkHint => 'Masalan: maktab yonida';

  @override
  String get addressComment => 'Kuryer uchun izoh';

  @override
  String get addressCommentHint => 'Masalan: darvoza ko‘k rangda';

  @override
  String get addressLabel => 'Nomi';

  @override
  String get addressLabelHome => 'Uy';

  @override
  String get addressLabelWork => 'Ish';

  @override
  String get addressLabelOther => 'Boshqa';

  @override
  String get addressLabelCustomHint => 'Masalan: Onamnikiga';

  @override
  String get addressLabelRequired => 'Manzil nomini kiriting';

  @override
  String get addressMakeDefault => 'Asosiy manzil';

  @override
  String get addressSave => 'Manzilni saqlash';

  @override
  String get addressDelete => 'Manzilni o‘chirish';

  @override
  String get addressDeleteConfirm => 'Bu manzil o‘chirilsinmi?';

  @override
  String get addressNotFound => 'Manzil topilmadi';

  @override
  String addressDetailEntrance(String value) {
    return '$value-podyezd';
  }

  @override
  String addressDetailFloor(String value) {
    return '$value-qavat';
  }

  @override
  String addressDetailApartment(String value) {
    return '$value-xonadon';
  }

  @override
  String addressDetailIntercom(String value) {
    return 'domofon $value';
  }

  @override
  String get ordersTitle => 'Buyurtmalarim';

  @override
  String get ordersTabActive => 'Faol';

  @override
  String ordersTabActiveCount(String count) {
    return 'Faol ($count)';
  }

  @override
  String get ordersTabHistory => 'Tarix';

  @override
  String get ordersEmptyActive => 'Hozir faol buyurtmalar yo‘q';

  @override
  String get ordersEmptyHistory => 'Buyurtmalar tarixi bo‘sh';

  @override
  String get ordersSignInPrompt => 'Buyurtmalaringizni ko‘rish uchun kiring';

  @override
  String get ordersTrack => 'Kuzatish';

  @override
  String get ordersReorder => 'Qayta buyurtma';

  @override
  String get ordersDelivery => 'Yetkazish';

  @override
  String get ordersPickup => 'Olib ketish';

  @override
  String ordersItemKinds(String count) {
    return '$count xil';
  }

  @override
  String get ordersToday => 'Bugun';

  @override
  String get ordersYesterday => 'Kecha';

  @override
  String ordersOrderNumber(String number) {
    return 'Buyurtma #$number';
  }

  @override
  String get ordersCallStore => 'Do‘konga qo‘ng‘iroq qilish';

  @override
  String get ordersBack => 'Ortga';

  @override
  String ordersMoney(String amount) {
    return '$amount so‘m';
  }

  @override
  String get ordersStatusNew => 'Yangi';

  @override
  String get ordersStatusAccepted => 'Tasdiqlandi';

  @override
  String get ordersStatusPicking => 'Yig‘ilmoqda';

  @override
  String get ordersStatusReady => 'Tayyor';

  @override
  String get ordersStatusAwaitingTransfer => 'To‘lov kutilmoqda';

  @override
  String get ordersStatusOnTheWay => 'Yo‘lda';

  @override
  String get ordersStatusReadyForPickup => 'Olib ketishga tayyor';

  @override
  String get ordersStatusDelivered => 'Yetkazildi';

  @override
  String get ordersStatusPickedUp => 'Olib ketildi';

  @override
  String get ordersStatusRejected => 'Rad etildi';

  @override
  String get ordersStatusCancelled => 'Bekor qilindi';

  @override
  String get ordersCardLabel => 'Buyurtma holati';

  @override
  String ordersScheduled(String time) {
    return 'Yetkazish vaqti: $time';
  }

  @override
  String get ordersHeadlineNew =>
      'Do‘kon buyurtmangizni tasdiqlashi kutilmoqda';

  @override
  String get ordersHeadlineAccepted => 'Do‘kon buyurtmangizni tasdiqladi';

  @override
  String get ordersHeadlinePicking => 'Do‘kon buyurtmangizni yig‘moqda';

  @override
  String get ordersHeadlineReady => 'Buyurtmangiz yig‘ildi';

  @override
  String get ordersHeadlineAwaitingTransfer => 'O‘tkazmangiz kutilmoqda';

  @override
  String get ordersHeadlineOnTheWay => 'Kuryer yo‘lda';

  @override
  String get ordersHeadlineReadyForPickup =>
      'Buyurtmani do‘kondan olib ketishingiz mumkin';

  @override
  String get ordersHeadlineDelivered =>
      'Buyurtma yetkazildi. Xaridingiz uchun rahmat!';

  @override
  String get ordersStepPlaced => 'Buyurtma qabul qilindi';

  @override
  String get ordersStepAccepted => 'Do‘kon tasdiqladi';

  @override
  String get ordersStepPicking => 'Yig‘ilmoqda';

  @override
  String get ordersStepPickingSub => 'Kassir mahsulotlarni yig‘moqda';

  @override
  String get ordersStepDispatched => 'Kuryerga topshirildi';

  @override
  String get ordersStepReadyForPickup => 'Olib ketishga tayyor';

  @override
  String get ordersStepDelivered => 'Yetkazildi';

  @override
  String get ordersStepPickedUp => 'Olib ketildi';

  @override
  String get ordersStepAwaitingTransfer => 'O‘tkazma tasdiqlanishi kutilmoqda';

  @override
  String get ordersPaymentUnpaid => 'To‘lanmagan';

  @override
  String get ordersPaymentPaid => 'To‘langan';

  @override
  String get ordersPaymentRefunded => 'Qaytarilgan';

  @override
  String get ordersMethodCash => 'Naqd';

  @override
  String get ordersMethodCard => 'Karta (kuryer terminali)';

  @override
  String get ordersMethodTransfer => 'Kartaga o‘tkazma';

  @override
  String ordersItemsSummary(String count, String total) {
    return '$count xil mahsulot · $total';
  }

  @override
  String get ordersContents => 'Tarkibi';

  @override
  String get ordersItemRemoved => 'Olib tashlandi';

  @override
  String get ordersItemReplacement => 'Almashtirish';

  @override
  String get ordersItemsTotal => 'Mahsulotlar';

  @override
  String get ordersDeliveryFee => 'Yetkazish';

  @override
  String get ordersTotal => 'Jami';

  @override
  String get ordersCancel => 'Bekor qilish';

  @override
  String get ordersCancelDisabled =>
      'Yig‘ish boshlangani uchun bekor qilib bo‘lmaydi';

  @override
  String get ordersCancelTitle => 'Buyurtmani bekor qilasizmi?';

  @override
  String get ordersCancelReasonHint => 'Sabab (ixtiyoriy)';

  @override
  String get ordersCancelDone => 'Buyurtma bekor qilindi';

  @override
  String get ordersRejectedTitle => 'Do‘kon buyurtmani rad etdi';

  @override
  String get ordersCancelledTitle => 'Buyurtma bekor qilindi';

  @override
  String ordersReason(String reason) {
    return 'Sabab: $reason';
  }

  @override
  String get ordersTransferTitle => 'Kartaga o‘tkazma';

  @override
  String ordersTransferHint(String amount) {
    return 'Quyidagi kartaga $amount o‘tkazing va chek rasmini yuklang';
  }

  @override
  String get ordersTransferCard => 'Karta raqami';

  @override
  String get ordersTransferHolder => 'Karta egasi';

  @override
  String get ordersTransferAmount => 'Summa';

  @override
  String get ordersCopy => 'Nusxa olish';

  @override
  String get ordersCopied => 'Nusxa olindi';

  @override
  String get ordersUploadProof => 'Chek rasmini yuklash';

  @override
  String get ordersReuploadProof => 'Boshqa rasm yuklash';

  @override
  String get ordersProofUploaded =>
      'Chek yuklandi — do‘kon to‘lovni tasdiqlashini kuting';

  @override
  String get ordersTransferWaitReady =>
      'Karta raqami buyurtma yig‘ilgach ko‘rsatiladi';

  @override
  String get ordersPaymentConfirmed => 'To‘lov tasdiqlandi';

  @override
  String get ordersFileTooBig => 'Rasm 5 MB dan katta bo‘lmasin';

  @override
  String catSum(String sum) {
    return '$sum so‘m';
  }

  @override
  String get catUnitKg => 'kg';

  @override
  String get catUnitPiece => 'dona';

  @override
  String get catDecrease => 'Kamaytirish';

  @override
  String get catIncrease => 'Ko‘paytirish';

  @override
  String get catRetry => 'Qayta urinish';

  @override
  String get catCancel => 'Bekor qilish';

  @override
  String get catSwitchStoreTitle => 'Do‘konni almashtirasizmi?';

  @override
  String get catSwitchStoreBody =>
      'Savatchada boshqa do‘kon mahsulotlari bor. Do‘kon almashtirilsa, savatcha tozalanadi.';

  @override
  String get catSwitchStoreConfirm => 'Tozalash va almashtirish';

  @override
  String get catChooseStore => 'Do‘konni tanlang';

  @override
  String get catChooseAnotherStore => 'Boshqa do‘kon tanlash';

  @override
  String get catChangeStore => 'Do‘konni almashtirish';

  @override
  String get catSearchStores => 'Do‘kon nomi';

  @override
  String get catNoStores => 'Do‘kon topilmadi';

  @override
  String catDistanceKm(String km) {
    return '$km km';
  }

  @override
  String get catOpenNow => 'Ochiq';

  @override
  String get catClosedNow => 'Yopiq';

  @override
  String get catSearchHint => 'Mahsulot yoki shtrix-kod qidirish';

  @override
  String get catClear => 'Tozalash';

  @override
  String get catAll => 'Hammasi';

  @override
  String get catPopular => 'Ommabop';

  @override
  String get catSearchResults => 'Qidiruv natijalari';

  @override
  String get catSort => 'Tartiblash';

  @override
  String get catSortPopular => 'Ommabop';

  @override
  String get catSortName => 'Nomi bo‘yicha';

  @override
  String get catSortPriceAsc => 'Avval arzon';

  @override
  String get catSortPriceDesc => 'Avval qimmat';

  @override
  String get catNoProducts => 'Hozircha mahsulot yo‘q';

  @override
  String get catNothingFound => 'Hech narsa topilmadi';

  @override
  String get catDeliveryBannerTitle => 'Eshigingizgacha yetkazamiz';

  @override
  String catFreeDeliveryFrom(String sum) {
    return '${sum}dan yuqori buyurtmaga yetkazish bepul';
  }

  @override
  String get catAddToCart => 'Savatchaga qo‘shish';

  @override
  String catAddToCartSum(String sum) {
    return 'Savatchaga · $sum';
  }

  @override
  String get catAddedToCart => 'Savatchaga qo‘shildi';

  @override
  String get catBack => 'Orqaga';

  @override
  String catBarcode(String code) {
    return 'Shtrix-kod $code';
  }

  @override
  String catInStock(String qty) {
    return 'Omborda bor · $qty';
  }

  @override
  String get catOutOfStock => 'Tugagan';

  @override
  String get cartEmpty => 'Savatcha bo‘sh';

  @override
  String get cartEmptyHint => 'Katalogdan mahsulot tanlang';

  @override
  String get cartGoShopping => 'Xaridga o‘tish';

  @override
  String get cartClear => 'Tozalash';

  @override
  String get cartClearConfirm => 'Savatcha tozalansinmi?';

  @override
  String get cartCheckout => 'Rasmiylashtirish';

  @override
  String cartFreeDeliveryLeft(String sum) {
    return 'Bepul yetkazishgacha $sum qoldi';
  }

  @override
  String get cartFreeDeliveryReached => 'Yetkazish bepul!';

  @override
  String cartMinOrderShortfall(String min, String left) {
    return 'Minimal buyurtma $min. Yana $left lik mahsulot qo‘shing.';
  }

  @override
  String get cartProblemNotFound => 'Endi sotuvda yo‘q';

  @override
  String get cartProblemOutOfStock => 'Tugagan';

  @override
  String cartProblemNotEnough(String qty) {
    return 'Faqat $qty mavjud';
  }

  @override
  String get cartProblemInvalidQuantity => 'Miqdor noto‘g‘ri';

  @override
  String get cartFixQuantity => 'Kamaytirish';

  @override
  String get cartRemove => 'O‘chirish';

  @override
  String cartItemsKinds(int count) {
    return 'Mahsulotlar ($count xil)';
  }

  @override
  String get cartDelivery => 'Yetkazib berish';

  @override
  String get cartFree => 'Bepul';

  @override
  String get cartTotal => 'Jami';

  @override
  String get checkoutTitle => 'Rasmiylashtirish';

  @override
  String get checkoutDelivery => 'Yetkazib berish';

  @override
  String get checkoutPickup => 'Do‘kondan olib ketish';

  @override
  String get checkoutPickupFrom => 'Olib ketish manzili';

  @override
  String get checkoutAddress => 'Yetkazish manzili';

  @override
  String get checkoutAddAddress => 'Yangi manzil qo‘shish';

  @override
  String get checkoutNoAddress => 'Yetkazish uchun manzil kiriting';

  @override
  String checkoutEntrance(String v) {
    return '$v-podyezd';
  }

  @override
  String checkoutFloor(String v) {
    return '$v-qavat';
  }

  @override
  String checkoutApartment(String v) {
    return '$v-xonadon';
  }

  @override
  String get checkoutTime => 'Vaqt';

  @override
  String get checkoutAsap => 'Imkon qadar tez';

  @override
  String get checkoutStoreClosed => 'Do‘kon hozir yopiq';

  @override
  String get checkoutPickTime => 'Vaqtni tanlash';

  @override
  String get checkoutPickTimeHint => 'Bugun / ertaga, 1 soatlik oraliq';

  @override
  String checkoutToday(String range) {
    return 'Bugun $range';
  }

  @override
  String checkoutTomorrow(String range) {
    return 'Ertaga $range';
  }

  @override
  String get checkoutNoSlots => 'Bugun va ertaga bo‘sh vaqt yo‘q';

  @override
  String get checkoutPayment => 'To‘lov usuli';

  @override
  String get checkoutNoPayment => 'Do‘konda to‘lov usuli sozlanmagan';

  @override
  String get checkoutPayCash => 'Naqd pul';

  @override
  String get checkoutPayCashHint => 'Qabul qilganda to‘lanadi';

  @override
  String get checkoutPayCard => 'Karta (terminal)';

  @override
  String get checkoutPayCardHint => 'Qabul qilganda terminal orqali';

  @override
  String get checkoutPayTransfer => 'Kartaga o‘tkazma';

  @override
  String get checkoutPayTransferHint =>
      'Yig‘ilgandan keyin karta raqami ko‘rsatiladi';

  @override
  String get checkoutOutOfStock => 'Mahsulot tugab qolsa';

  @override
  String get checkoutPolicyCallMe => 'Menga qo‘ng‘iroq qiling';

  @override
  String get checkoutPolicyReplace => 'O‘xshashiga almashtiring';

  @override
  String get checkoutPolicyRemove => 'Olib tashlang';

  @override
  String get checkoutCommentCourier => 'Kuryerga izoh';

  @override
  String get checkoutComment => 'Izoh';

  @override
  String get checkoutCommentHint =>
      'Masalan: domofon ishlamaydi, qo‘ng‘iroq qiling';

  @override
  String get checkoutContactless => 'Eshik oldiga qoldirish (kontaktsiz)';

  @override
  String get checkoutFixCart => 'Savatchadagi muammolarni tuzating';

  @override
  String checkoutItemsPlusDelivery(String items, String delivery) {
    return 'Mahsulotlar $items + yetkazish $delivery';
  }

  @override
  String checkoutItemsOnly(String items) {
    return 'Mahsulotlar $items';
  }

  @override
  String get checkoutPlaceOrder => 'Buyurtma berish';

  @override
  String get checkoutAddressRequired => 'Yetkazish manzilini tanlang';

  @override
  String get checkoutPaymentRequired => 'To‘lov usulini tanlang';

  @override
  String get checkoutTimeRequired => 'Yetkazish vaqtini tanlang';

  @override
  String get legalTerms => 'Foydalanish shartlari';

  @override
  String get legalPrivacy => 'Maxfiylik siyosati';

  @override
  String get legalAgreePrefix => 'Davom etish orqali ';

  @override
  String get legalAgreeMiddle => ' va ';

  @override
  String get legalAgreeSuffix => 'ga rozilik bildirasiz.';

  @override
  String get legalTermsLink => 'foydalanish shartlari';

  @override
  String get legalPrivacyLink => 'maxfiylik siyosati';

  @override
  String get updateRequiredTitle => 'Yangilash kerak';

  @override
  String get updateRequiredMessage =>
      'Ilovaning bu versiyasi endi qo‘llab-quvvatlanmaydi. Davom etish uchun yangi versiyani o‘rnating.';

  @override
  String get updateButton => 'Yangilash';

  @override
  String get updateAvailableTitle => 'Yangi versiya mavjud';

  @override
  String get updateAvailableMessage =>
      'Ilovaning yangi versiyasini o‘rnatishni tavsiya qilamiz.';

  @override
  String updateAvailableVersion(String version) {
    return '$version versiyasi chiqdi. Yangilashni tavsiya qilamiz.';
  }

  @override
  String get updateLater => 'Keyinroq';

  @override
  String get updateMaintenanceTitle => 'Texnik ishlar';

  @override
  String get updateMaintenanceMessage =>
      'Hozir texnik ishlar olib borilmoqda. Birozdan keyin qayta urinib ko‘ring.';

  @override
  String get updateRetry => 'Qayta urinish';

  @override
  String get langSheetTitle => 'Ilova tili';

  @override
  String get langUzbek => 'O‘zbekcha';

  @override
  String get langRussian => 'Русский';

  @override
  String get langDevice => 'Qurilma tili';

  @override
  String get pushChannelName => 'Buyurtmalar';

  @override
  String get pushChannelDescription => 'Buyurtma holati haqida xabarlar';

  @override
  String get namePromptHint =>
      'Ismingizni kiriting — do‘kon va kuryer sizga shunday murojaat qiladi.';

  @override
  String get namePromptSkip => 'Keyinroq';

  @override
  String deliveryMinutesRange(String min, String max) {
    return '$min–$max daqiqa';
  }

  @override
  String deliveryMinutesUpTo(String minutes) {
    return '$minutes daqiqagacha';
  }

  @override
  String get mapPickerTitle => 'Xaritada belgilash';

  @override
  String get mapPickerConfirm => 'Shu yerni tanlash';

  @override
  String get addressMapPick => 'Xaritada belgilash';

  @override
  String get addressMapChange => 'Xaritada o‘zgartirish';

  @override
  String addressMapCoords(String coords) {
    return 'Koordinata: $coords';
  }

  @override
  String get ordersProofCamera => 'Kamerada suratga olish';

  @override
  String get ordersProofGallery => 'Galereyadan tanlash';

  @override
  String get ordersProofPickFailed =>
      'Rasmni olib bo‘lmadi. Kamera yoki galereyaga ruxsat bering.';

  @override
  String get ordersReorderEmpty =>
      'Bu buyurtmadan savatchaga qo‘shiladigan mahsulot yo‘q';

  @override
  String ordersCourier(String name) {
    return 'Kuryer: $name';
  }

  @override
  String get ordersCallCourier => 'Kuryerga qo‘ng‘iroq';

  @override
  String ordersEta(String range) {
    return 'Taxminiy yetkazish: $range';
  }

  @override
  String get ordersFiscalReceipt => 'Fiskal chek';

  @override
  String ordersReceiptNumber(String number) {
    return 'Chek № $number';
  }

  @override
  String get ordersRefundPending => 'Pul qaytariladi';

  @override
  String get ordersRefundDone => 'Pul qaytarildi';

  @override
  String get courierEntry => 'Kuryer sifatida kirish';

  @override
  String get courierLoginTitle => 'Kuryer kirishi';

  @override
  String get courierLoginSubtitle =>
      'Do‘kon bergan login va parol bilan kiring';

  @override
  String get courierUsername => 'Login';

  @override
  String get courierPassword => 'Parol';

  @override
  String get courierSignIn => 'Kirish';

  @override
  String get courierBackToCustomer => 'Mijoz sifatida kirish';

  @override
  String get courierNotCourier => 'Bu akkaunt kuryer emas';

  @override
  String get courierSessionExpired => 'Sessiya tugadi. Qayta kiring.';

  @override
  String get courierTabActive => 'Faol';

  @override
  String get courierTabToday => 'Bugun';

  @override
  String get courierRouteAll => 'Marshrut (hammasi)';

  @override
  String get courierEmptyActive => 'Hozircha biriktirilgan buyurtma yo‘q';

  @override
  String get courierEmptyToday => 'Bugun yakunlangan buyurtma yo‘q';

  @override
  String courierMoney(String amount) {
    return '$amount so‘m';
  }

  @override
  String courierCollectCash(String amount) {
    return '$amount olish kerak';
  }

  @override
  String get courierPaid => 'To‘langan';

  @override
  String get courierPaymentPending => 'To‘lov kutilmoqda';

  @override
  String courierDistanceKm(String value) {
    return '$value km';
  }

  @override
  String courierDistanceM(String value) {
    return '$value m';
  }

  @override
  String courierItemsCount(String count) {
    return '$count ta mahsulot';
  }

  @override
  String get courierLogout => 'Chiqish';

  @override
  String get courierLogoutConfirm => 'Kuryer rejimidan chiqasizmi?';

  @override
  String get courierCancel => 'Bekor qilish';

  @override
  String get courierRetry => 'Qayta urinish';

  @override
  String get courierCall => 'Qo‘ng‘iroq';

  @override
  String get courierCustomer => 'Mijoz';

  @override
  String get courierAddress => 'Manzil';

  @override
  String get courierEntrance => 'Podyezd';

  @override
  String get courierFloor => 'Qavat';

  @override
  String get courierApartment => 'Xonadon';

  @override
  String get courierIntercom => 'Domofon';

  @override
  String get courierLandmark => 'Mo‘ljal';

  @override
  String get courierComment => 'Izoh';

  @override
  String get courierContactless => 'Kontaktsiz yetkazish';

  @override
  String get courierItems => 'Tarkibi';

  @override
  String get courierTotal => 'Jami';

  @override
  String get courierYandexNavi => 'Yandex Navigator';

  @override
  String get courierGoogleMaps => 'Google Maps';

  @override
  String get courierDelivered => 'Yetkazildi';

  @override
  String get courierFailed => 'Yetkazib bo‘lmadi';

  @override
  String get courierCashTitle => 'Naqd pul olindi';

  @override
  String get courierCashHint => 'Olingan summa';

  @override
  String get courierConfirm => 'Tasdiqlash';

  @override
  String get courierDeliveredConfirm =>
      'Buyurtma yetkazilganini tasdiqlaysizmi?';

  @override
  String get courierFailedTitle => 'Nima uchun yetkazib bo‘lmadi?';

  @override
  String get courierReasonNoAnswer => 'Mijoz javob bermadi';

  @override
  String get courierReasonNotFound => 'Manzil topilmadi';

  @override
  String get courierReasonRefused => 'Mijoz rad etdi';

  @override
  String get courierReasonOther => 'Boshqa';

  @override
  String get courierReasonOtherHint => 'Sababni yozing';

  @override
  String get courierStatusNotDelivered => 'Yetkazilmadi';

  @override
  String get courierOpenAppError => 'Ilovani ochib bo‘lmadi';

  @override
  String get courierDoneDelivered => 'Buyurtma yetkazildi';

  @override
  String get courierDoneFailed => 'Buyurtma do‘konga qaytarildi';

  @override
  String get courierMode => 'Kuryer';

  @override
  String get courierNoCoordinates =>
      'Koordinata yo‘q — manzil matni bo‘yicha qidiriladi';

  @override
  String get cartPromoTitle => 'Promokod';

  @override
  String get cartPromoHint => 'Promokodni kiriting';

  @override
  String get cartPromoApply => 'Qo‘llash';

  @override
  String get cartPromoRemove => 'Olib tashlash';

  @override
  String cartPromoApplied(String amount) {
    return 'Chegirma: −$amount';
  }

  @override
  String get cartPromoDiscount => 'Promokod chegirmasi';

  @override
  String get cartBonusUsed => 'Bonusdan';

  @override
  String get checkoutBonusTitle => 'Bonus bilan to‘lash';

  @override
  String checkoutBonusBalance(String amount) {
    return 'Balans: $amount';
  }

  @override
  String checkoutBonusWillUse(String amount) {
    return '$amount ishlatiladi';
  }

  @override
  String checkoutBonusMaxPercent(String percent) {
    return 'summaning $percent% gacha';
  }

  @override
  String get checkoutDeliveryUnavailable => 'Bu manzilga yetkazib berilmaydi';

  @override
  String get checkoutSwitchToPickup => 'Olib ketishni tanlash';

  @override
  String checkoutDistance(String km) {
    return 'Masofa: $km km';
  }

  @override
  String get checkoutPayPayme => 'Payme';

  @override
  String get checkoutPayClick => 'Click';

  @override
  String get checkoutPayOnlineHint => 'Yig‘ilgandan keyin ilovada to‘lash';

  @override
  String get ordersPayOnlineTitle => 'Online to‘lov';

  @override
  String ordersPayOnlineHint(String amount) {
    return 'Buyurtma yig‘ildi. Yakuniy summani ilovada to‘lang: $amount';
  }

  @override
  String ordersPayWith(String provider) {
    return '$provider orqali to‘lash';
  }

  @override
  String get ordersPayOpenFailed => 'To‘lov sahifasini ochib bo‘lmadi';

  @override
  String get mapMyLocation => 'Joylashuvimni aniqlash';

  @override
  String get mapLocationOff => 'Qurilmada joylashuv (GPS) o‘chiq';

  @override
  String get mapLocationDenied => 'Joylashuvga ruxsat berilmadi';

  @override
  String get mapLocationDeniedForever =>
      'Joylashuvga ruxsat sozlamalarda o‘chirilgan';

  @override
  String get mapLocationFailed => 'Joylashuvni aniqlab bo‘lmadi';

  @override
  String get mapOpenSettings => 'Sozlamalar';

  @override
  String get mapSearchHint => 'Manzilni qidirish';

  @override
  String get mapSearchEmpty => 'Hech narsa topilmadi';

  @override
  String get mapAddressLoading => 'Manzil aniqlanmoqda…';

  @override
  String get mapAddressUnknown => 'Manzil aniqlanmadi — matnni o‘zingiz yozing';

  @override
  String get mapMoveHint => 'Xaritani suring — pin ostidagi manzil aniqlanadi';

  @override
  String get addressReplaceTitle => 'Manzil matni almashtirilsinmi?';

  @override
  String get addressReplace => 'Almashtirish';

  @override
  String get addressKeep => 'Qoldirish';

  @override
  String get bonusTitle => 'Bonus karta';

  @override
  String get bonusBalance => 'Balans';

  @override
  String bonusPoints(String value) {
    return '$value ball';
  }

  @override
  String get bonusHint =>
      'Kassada shu QR’ni ko‘rsating — cashback avtomatik yoziladi va ball bilan to‘lash mumkin';

  @override
  String bonusRefreshIn(String time) {
    return 'Kod $time dan keyin yangilanadi';
  }

  @override
  String get bonusRefreshing => 'Kod yangilanmoqda…';

  @override
  String get bonusRefreshFailed => 'Kodni yangilab bo‘lmadi';

  @override
  String get bonusExpired => 'Kod eskirdi — yangilang';

  @override
  String get bonusRetry => 'Qayta urinish';

  @override
  String get bonusQrSemantics => 'Bonus karta QR kodi';

  @override
  String get bonusNoQr => 'Bu do‘kon uchun QR kod hozircha mavjud emas';

  @override
  String get bonusHistory => 'Tarix';

  @override
  String get bonusHistoryEmpty => 'Hali ball harakatlari yo‘q';

  @override
  String get bonusTypeEarn => 'Cashback';

  @override
  String get bonusTypeRedeem => 'Sarflandi';

  @override
  String get bonusTypeExpire => 'Muddati o‘tdi';

  @override
  String get bonusTypeAdjust => 'Tuzatish';

  @override
  String get bonusTypeSignup => 'Ro‘yxatdan o‘tish bonusi';

  @override
  String get bonusTypeRefund => 'Qaytarish';

  @override
  String get bonusTypeOther => 'Boshqa';

  @override
  String get bonusSourceStore => 'Do‘kon';

  @override
  String get bonusSourceOnline => 'Online';

  @override
  String bonusReceipt(String number) {
    return 'Chek №$number';
  }

  @override
  String get bonusGuestTitle => 'Bonus karta';

  @override
  String get bonusGuestText =>
      'Telefon raqamingiz bilan kiring — har xariddan cashback yig‘ing va ball bilan to‘lang';

  @override
  String get bonusNoStoresTitle => 'Hali bonus kartangiz yo‘q';

  @override
  String get bonusNoStoresText => 'Do‘kon tanlang — karta avtomatik ochiladi';

  @override
  String get bonusChooseStore => 'Do‘kon tanlash';

  @override
  String get storeChooseBranch => 'Filialni tanlang';

  @override
  String get storeUnavailableTitle => 'Do‘kon vaqtincha ishlamayapti';

  @override
  String get storeUnavailableText => 'Keyinroq qayta urinib ko‘ring.';

  @override
  String get navBonus => 'Bonus';

  @override
  String get favTitle => 'Sevimlilar';

  @override
  String get favAdd => 'Sevimlilarga qo‘shish';

  @override
  String get favRemove => 'Sevimlilardan olib tashlash';

  @override
  String get favLoginTitle => 'Sevimlilarga qo‘shish';

  @override
  String get favLoginText =>
      'Sevimli mahsulotlarni saqlash uchun telefon raqamingiz bilan kiring.';

  @override
  String get favEmpty => 'Sevimlilar hozircha bo‘sh';

  @override
  String get favEmptyHint =>
      'Mahsulotdagi ♥ belgisini bosing — u shu yerda saqlanadi';

  @override
  String get mkAll => 'Hammasi';

  @override
  String get mkClose => 'Yopish';

  @override
  String get mkStoryOpen => 'Batafsil';

  @override
  String mkEndsIn(String time) {
    return 'Tugashiga $time';
  }

  @override
  String mkDiscountEndsIn(String time) {
    return 'Aksiya tugashiga $time';
  }

  @override
  String get mkDiscountsTitle => 'Aksiyalar';

  @override
  String get mkDiscountsEmpty => 'Hozircha aksiyadagi mahsulotlar yo‘q';

  @override
  String mkDiscountsEntry(int count) {
    return 'Aksiyalar ($count)';
  }

  @override
  String get mkDiscountsEntryHint => 'Chegirmadagi barcha mahsulotlar';

  @override
  String get mkBuyAgain => 'Yana olasizmi?';

  @override
  String get mkTop10 => 'Top 10';

  @override
  String get mkRelated => 'Bular bilan birga olishadi';

  @override
  String get mkCartSuggestions => 'Qo‘shib qo‘ying';

  @override
  String get mkReorderTitle => 'Oxirgi buyurtmani takrorlash';

  @override
  String mkReorderSubtitle(String number, String sum) {
    return '№$number · $sum';
  }

  @override
  String get mkReorderButton => 'Takrorlash';

  @override
  String get mkNotifyMe => 'Kelganda xabar berish';

  @override
  String get mkNotifySubscribed => 'Kelganda xabar beramiz';

  @override
  String get mkNotifyOn => 'Mahsulot sotuvga chiqqanda xabar beramiz';

  @override
  String get mkNotifyOff => 'Xabarnoma bekor qilindi';

  @override
  String cdDaysHours(int days, int hours) {
    return '$days kun $hours soat';
  }

  @override
  String cdHoursMinutes(int hours, int minutes) {
    return '$hours soat $minutes daq';
  }

  @override
  String cdMinutes(int minutes) {
    return '$minutes daq';
  }

  @override
  String get searchHistory => 'Qidiruv tarixi';

  @override
  String get searchHistoryClear => 'Tozalash';

  @override
  String get scanTitle => 'Shtrix-kodni skanerlash';

  @override
  String get scanTorch => 'Chiroq';

  @override
  String get scanHint => 'Shtrix-kodni ramka ichiga joylashtiring';

  @override
  String get scanError =>
      'Kamerani ochib bo‘lmadi. Sozlamalarda kameraga ruxsat bering.';

  @override
  String get rateTitle => 'Buyurtmani baholang';

  @override
  String get rateSubtitle => 'Fikringiz xizmatni yaxshilashga yordam beradi';

  @override
  String get rateYourRating => 'Sizning bahoingiz';

  @override
  String get rateCommentHint => 'Izoh (ixtiyoriy)';

  @override
  String get rateSubmit => 'Yuborish';

  @override
  String get rateThanks => 'Rahmat! Bahoingiz qabul qilindi';

  @override
  String rateStars(int count) {
    return '$count yulduz';
  }

  @override
  String get rateTagFast => 'Tez yetkazildi';

  @override
  String get rateTagFresh => 'Mahsulotlar yangi';

  @override
  String get rateTagPolite => 'Xushmuomala kuryer';

  @override
  String get rateTagAccurate => 'Hammasi to‘g‘ri';

  @override
  String get rateTagLate => 'Kechikdi';

  @override
  String get rateTagDamaged => 'Shikastlangan';

  @override
  String get rateTagMissing => 'Nimadir yetishmadi';

  @override
  String get rateTagWrongItem => 'Boshqa mahsulot keldi';

  @override
  String get supportTitle => 'Yordam';

  @override
  String get supportText => 'Savolingiz bormi? Biz bilan bog‘laning';

  @override
  String get supportCall => 'Qo‘ng‘iroq qilish';

  @override
  String get supportTelegram => 'Telegram’da yozish';

  @override
  String get supportNone => 'Do‘kon aloqa ma’lumotlarini hali kiritmagan';

  @override
  String get refTitle => 'Do‘stni taklif qilish';

  @override
  String get refUnavailable =>
      'Bu do‘konda do‘stni taklif qilish hozircha mavjud emas';

  @override
  String refHeadline(String bonus) {
    return 'Do‘stingizni taklif qiling — $bonus oling';
  }

  @override
  String refExplain(String bonus) {
    return 'Do‘stingiz ham $bonus oladi — birinchi buyurtmasi yetkazilganda';
  }

  @override
  String refShareFallback(String code, String bonus) {
    return 'Mening kodim: $code. Ilovada kiriting va $bonus oling!';
  }

  @override
  String get refYourCode => 'Sizning kodingiz';

  @override
  String get refCopy => 'Nusxa olish';

  @override
  String get refCopied => 'Kod nusxalandi';

  @override
  String get refShare => 'Ulashish';

  @override
  String get refInvited => 'Taklif qilinganlar';

  @override
  String get refRewarded => 'Bonus olinganlar';

  @override
  String get refHaveCode => 'Do‘stingiz kodi bormi?';

  @override
  String get refHaveCodeHint => 'Birinchi buyurtmangizgacha kiriting';

  @override
  String get refCodeHint => 'Kod';

  @override
  String get refApplied => 'Kod qabul qilindi';

  @override
  String get tierNone => 'Daraja hali yo‘q';

  @override
  String tierCurrent(String name, String percent) {
    return '$name · $percent% cashback';
  }

  @override
  String tierNextLeft(String name, String sum) {
    return '$name darajasigacha $sum';
  }

  @override
  String get tierTop => 'Siz eng yuqori darajadasiz';

  @override
  String catOpensAt(String time) {
    return '$time da ochiladi';
  }

  @override
  String get checkoutWhenOpens => 'Do‘kon ochilganda';

  @override
  String checkoutWhenOpensAt(String time) {
    return 'Do‘kon ochilganda ($time)';
  }

  @override
  String get checkoutPreorderHint => 'Oldindan buyurtma';

  @override
  String get ordersPreorderTitle => 'Oldindan buyurtma';

  @override
  String ordersPreorderNote(String time) {
    return 'Do‘kon hozir yopiq. Buyurtmangiz $time da ko‘rib chiqiladi.';
  }

  @override
  String get ordersPreorderNoteNoTime =>
      'Do‘kon hozir yopiq. Buyurtmangiz do‘kon ochilganda ko‘rib chiqiladi.';

  @override
  String get courierTabOffers => 'Takliflar';

  @override
  String get courierOffersEmpty => 'Hozircha taklif yo‘q';

  @override
  String get courierOfferTitle => 'Yangi buyurtma';

  @override
  String courierOfferNumber(String number) {
    return 'Buyurtma #$number';
  }

  @override
  String get courierOfferExpired => 'Taklif vaqti tugadi';

  @override
  String get courierOfferAccept => 'Qabul qilaman';

  @override
  String get courierOfferDecline => 'Rad etaman';

  @override
  String get courierOfferTaken => 'Boshqa kuryer oldi';

  @override
  String get courierOfferClosed => 'Taklif yopildi';

  @override
  String get courierOfferAccepted => 'Buyurtma sizga biriktirildi';

  @override
  String get courierOfferCash => 'Naqd to‘lov';

  @override
  String get courierOfferPaidOrCard => 'Karta / to‘langan';

  @override
  String get courierBack => 'Orqaga';

  @override
  String get courierPickedUpFromStore => 'Do‘kondan oldim';

  @override
  String get courierAwaitingPickup => 'Do‘kondan olish';

  @override
  String get courierDonePickedUp => 'Buyurtma yo‘lda';

  @override
  String get courierReturnTask => 'Qaytarish';

  @override
  String courierReturnNumber(String number) {
    return 'Qaytarish #$number';
  }

  @override
  String get courierReturnPickedUp => 'Olib ketildi';

  @override
  String get courierReturnPickedUpConfirm =>
      'Tovarlarni mijozdan olganingizni tasdiqlaysizmi?';

  @override
  String get courierReturnDone => 'Qaytarish olib ketildi';

  @override
  String get courierReturnNoCash => 'Pul berilmaydi — do‘kon o‘zi qaytaradi';

  @override
  String get courierRejectItems => 'Ba’zi tovarlarni rad etdi';

  @override
  String get courierRejectTitle => 'Mijoz qaysi tovarlarni olmadi?';

  @override
  String get courierRejectedEdit => 'Rad etilganlarni o‘zgartirish';

  @override
  String get courierToCollect => 'Olinadigan summa';

  @override
  String get courierRejectApprox =>
      'Taxminiy — yakuniy summani server hisoblaydi';

  @override
  String get courierSave => 'Saqlash';

  @override
  String get returnReasonDamaged => 'Shikastlangan';

  @override
  String get returnReasonWrongItem => 'Noto‘g‘ri tovar';

  @override
  String get returnReasonQuality => 'Sifatsiz';

  @override
  String get returnReasonExpired => 'Muddati o‘tgan';

  @override
  String get returnReasonMissing => 'Yetishmaydi';

  @override
  String get returnReasonOther => 'Boshqa';

  @override
  String get refundMethodCash => 'Naqd (do‘konda)';

  @override
  String get refundMethodTransfer => 'Kartaga o‘tkazma';

  @override
  String get refundMethodBonus => 'Bonus ball';

  @override
  String get returnMethodCourier => 'Kuryer olib ketadi';

  @override
  String get returnMethodStore => 'Do‘konga o‘zingiz olib borasiz';

  @override
  String get returnStatusRequested => 'Ko‘rib chiqilmoqda';

  @override
  String get returnStatusApproved => 'Tasdiqlandi';

  @override
  String get returnStatusPickupAssigned => 'Kuryer biriktirildi';

  @override
  String get returnStatusPickedUp => 'Kuryer olib ketdi';

  @override
  String get returnStatusReceived => 'Do‘konga yetib keldi';

  @override
  String get returnStatusRefunded => 'Pul qaytarildi';

  @override
  String get returnStatusRejected => 'Rad etildi';

  @override
  String get returnStatusCancelled => 'Bekor qilindi';

  @override
  String get returnsTitle => 'Qaytarishlarim';

  @override
  String get returnsEmpty => 'Qaytarishlar yo‘q';

  @override
  String get returnAction => 'Qaytarish';

  @override
  String get returnRequestTitle => 'Tovarni qaytarish';

  @override
  String get returnChooseItems => 'Qaytariladigan tovarlar';

  @override
  String get returnNothingToReturn => 'Qaytariladigan tovar qolmagan';

  @override
  String get returnReason => 'Sabab';

  @override
  String get returnSelectReason => 'Sababni tanlang';

  @override
  String get returnComment => 'Izoh';

  @override
  String get returnCommentHint => 'Muammoni qisqacha yozing';

  @override
  String get returnPhotos => 'Rasmlar (3 tagacha)';

  @override
  String get returnAddPhoto => 'Rasm';

  @override
  String get returnRemovePhoto => 'Rasmni olib tashlash';

  @override
  String get returnRefundMethod => 'Pulni qaytarish usuli';

  @override
  String get returnCashbackNote =>
      'Shu tovarlar uchun berilgan keshbek ayiriladi.';

  @override
  String get returnAmount => 'Qaytariladigan summa';

  @override
  String get returnSubmit => 'So‘rov yuborish';

  @override
  String get returnSelectItems => 'Kamida bitta tovar tanlang';

  @override
  String get returnSent => 'So‘rov yuborildi';

  @override
  String returnNumber(String number) {
    return 'Qaytarish #$number';
  }

  @override
  String get returnWithheld => 'Keshbek ushlab qolindi';

  @override
  String get returnCancel => 'So‘rovni bekor qilish';

  @override
  String get returnCancelConfirm => 'Qaytarish so‘rovini bekor qilasizmi?';

  @override
  String get returnCancelled => 'So‘rov bekor qilindi';

  @override
  String returnRejectReason(String reason) {
    return 'Rad etish sababi: $reason';
  }

  @override
  String get returnTimeline => 'Holat';

  @override
  String get returnMethod => 'Qaytarish usuli';
}
