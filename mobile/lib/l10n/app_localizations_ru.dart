// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Russian (`ru`).
class AppLocalizationsRu extends AppLocalizations {
  AppLocalizationsRu([String locale = 'ru']) : super(locale);

  @override
  String get appTitle => 'Xo‘ja Market';

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

  @override
  String get getCode => 'Получить код';

  @override
  String codeSentTelegram(String phone) {
    return 'Код отправлен в Telegram на номер $phone';
  }

  @override
  String codeSentSms(String phone) {
    return 'Код отправлен на номер $phone';
  }

  @override
  String resendIn(String time) {
    return 'повторно через $time';
  }

  @override
  String get resendCode => 'Отправить код ещё раз';

  @override
  String get sendViaSms => 'Отправить по SMS';

  @override
  String get changeNumber => 'Изменить номер';

  @override
  String get invalidPhone => 'Введите номер полностью';

  @override
  String get retry => 'Повторить';

  @override
  String get signInRequired => 'Войдите, чтобы оформить заказ';

  @override
  String get signOut => 'Выйти';

  @override
  String profileAddressCount(int count) {
    return '$count шт.';
  }

  @override
  String get profilePersonalInfo => 'Личные данные';

  @override
  String get profileAddresses => 'Мои адреса';

  @override
  String get profileLanguage => 'Язык';

  @override
  String get profileLanguageName => 'Русский';

  @override
  String get profileLanguageLater =>
      'Смена языка будет в следующей версии. Сейчас используется язык устройства.';

  @override
  String get profileCancel => 'Отмена';

  @override
  String get profileSignOutTitle => 'Выйти из аккаунта?';

  @override
  String get profileSignOutMessage =>
      'Для повторного входа на ваш номер будет отправлен код.';

  @override
  String get profileDeleteAccount => 'Удалить аккаунт';

  @override
  String get profileDeleteTitle => 'Удалить аккаунт?';

  @override
  String get profileDeleteMessage =>
      'Имя, адреса и другие личные данные будут удалены, вы выйдете на всех устройствах. Это действие нельзя отменить.';

  @override
  String get profileDeleteConfirmWord => 'УДАЛИТЬ';

  @override
  String profileDeleteTypeHint(String word) {
    return 'Для подтверждения введите $word';
  }

  @override
  String get profileDeleteButton => 'Удалить';

  @override
  String get profileGuestTitle => 'Войдите в профиль';

  @override
  String get profileGuestText =>
      'Войдите по номеру телефона, чтобы видеть заказы, адреса и бонусы.';

  @override
  String get profileNameBannerTitle => 'Укажите ваше имя';

  @override
  String get profileNameBannerText =>
      'Чтобы магазин и курьер обращались к вам по имени.';

  @override
  String get profileFirstName => 'Имя';

  @override
  String get profileLastName => 'Фамилия';

  @override
  String get profilePhone => 'Номер телефона';

  @override
  String get profilePhoneVerified => 'Подтверждён';

  @override
  String get profileBirthDate => 'Дата рождения';

  @override
  String get profileBirthDateHint => 'В день рождения подарим бонусы';

  @override
  String get profileGender => 'Пол';

  @override
  String get profileGenderMale => 'Мужской';

  @override
  String get profileGenderFemale => 'Женский';

  @override
  String get profileEmail => 'Email';

  @override
  String get profileEmailHint =>
      'для электронных чеков и уведомлений, необязательно';

  @override
  String get profileEmailInvalid => 'Неверный email';

  @override
  String get profileFirstNameRequired => 'Введите имя';

  @override
  String get profileSave => 'Сохранить';

  @override
  String get profileSaved => 'Сохранено';

  @override
  String get addressesTitle => 'Мои адреса';

  @override
  String get addressAdd => 'Добавить новый адрес';

  @override
  String get addressDefaultBadge => 'Основной';

  @override
  String get addressEmpty => 'Адресов пока нет';

  @override
  String get addressOutOfZoneNote =>
      'Если адрес вне зоны доставки, заказ можно забрать в магазине.';

  @override
  String get addressNewTitle => 'Новый адрес';

  @override
  String get addressEditTitle => 'Редактировать адрес';

  @override
  String get addressLine => 'Адрес';

  @override
  String get addressLineHint => 'Улица, номер дома';

  @override
  String get addressLineRequired => 'Введите адрес';

  @override
  String get addressEntrance => 'Подъезд';

  @override
  String get addressFloor => 'Этаж';

  @override
  String get addressApartment => 'Квартира';

  @override
  String get addressIntercom => 'Домофон';

  @override
  String get addressLandmark => 'Ориентир';

  @override
  String get addressLandmarkHint => 'Например: рядом со школой';

  @override
  String get addressComment => 'Комментарий для курьера';

  @override
  String get addressCommentHint => 'Например: синие ворота';

  @override
  String get addressLabel => 'Название';

  @override
  String get addressLabelHome => 'Дом';

  @override
  String get addressLabelWork => 'Работа';

  @override
  String get addressLabelOther => 'Другое';

  @override
  String get addressLabelCustomHint => 'Например: У мамы';

  @override
  String get addressLabelRequired => 'Введите название адреса';

  @override
  String get addressMakeDefault => 'Основной адрес';

  @override
  String get addressSave => 'Сохранить адрес';

  @override
  String get addressDelete => 'Удалить адрес';

  @override
  String get addressDeleteConfirm => 'Удалить этот адрес?';

  @override
  String get addressNotFound => 'Адрес не найден';

  @override
  String addressDetailEntrance(String value) {
    return 'подъезд $value';
  }

  @override
  String addressDetailFloor(String value) {
    return 'этаж $value';
  }

  @override
  String addressDetailApartment(String value) {
    return 'кв. $value';
  }

  @override
  String addressDetailIntercom(String value) {
    return 'домофон $value';
  }

  @override
  String get ordersTitle => 'Мои заказы';

  @override
  String get ordersTabActive => 'Активные';

  @override
  String ordersTabActiveCount(String count) {
    return 'Активные ($count)';
  }

  @override
  String get ordersTabHistory => 'История';

  @override
  String get ordersEmptyActive => 'Сейчас нет активных заказов';

  @override
  String get ordersEmptyHistory => 'История заказов пуста';

  @override
  String get ordersSignInPrompt => 'Войдите, чтобы видеть свои заказы';

  @override
  String get ordersTrack => 'Отследить';

  @override
  String get ordersReorder => 'Повторить';

  @override
  String get ordersDelivery => 'Доставка';

  @override
  String get ordersPickup => 'Самовывоз';

  @override
  String ordersItemKinds(String count) {
    return '$count шт.';
  }

  @override
  String get ordersToday => 'Сегодня';

  @override
  String get ordersYesterday => 'Вчера';

  @override
  String ordersOrderNumber(String number) {
    return 'Заказ #$number';
  }

  @override
  String get ordersCallStore => 'Позвонить в магазин';

  @override
  String get ordersBack => 'Назад';

  @override
  String ordersMoney(String amount) {
    return '$amount сум';
  }

  @override
  String get ordersStatusNew => 'Новый';

  @override
  String get ordersStatusAccepted => 'Подтверждён';

  @override
  String get ordersStatusPicking => 'Собирается';

  @override
  String get ordersStatusReady => 'Готов';

  @override
  String get ordersStatusAwaitingTransfer => 'Ожидает оплату';

  @override
  String get ordersStatusOnTheWay => 'В пути';

  @override
  String get ordersStatusReadyForPickup => 'Готов к выдаче';

  @override
  String get ordersStatusDelivered => 'Доставлен';

  @override
  String get ordersStatusPickedUp => 'Получен';

  @override
  String get ordersStatusRejected => 'Отклонён';

  @override
  String get ordersStatusCancelled => 'Отменён';

  @override
  String get ordersCardLabel => 'Статус заказа';

  @override
  String ordersScheduled(String time) {
    return 'Время доставки: $time';
  }

  @override
  String get ordersHeadlineNew => 'Ждём подтверждения магазина';

  @override
  String get ordersHeadlineAccepted => 'Магазин подтвердил заказ';

  @override
  String get ordersHeadlinePicking => 'Магазин собирает ваш заказ';

  @override
  String get ordersHeadlineReady => 'Заказ собран';

  @override
  String get ordersHeadlineAwaitingTransfer => 'Ожидаем ваш перевод';

  @override
  String get ordersHeadlineOnTheWay => 'Курьер в пути';

  @override
  String get ordersHeadlineReadyForPickup => 'Заказ можно забрать в магазине';

  @override
  String get ordersHeadlineDelivered => 'Заказ доставлен. Спасибо за покупку!';

  @override
  String get ordersStepPlaced => 'Заказ принят';

  @override
  String get ordersStepAccepted => 'Магазин подтвердил';

  @override
  String get ordersStepPicking => 'Собирается';

  @override
  String get ordersStepPickingSub => 'Кассир собирает товары';

  @override
  String get ordersStepDispatched => 'Передан курьеру';

  @override
  String get ordersStepReadyForPickup => 'Готов к выдаче';

  @override
  String get ordersStepDelivered => 'Доставлен';

  @override
  String get ordersStepPickedUp => 'Получен';

  @override
  String get ordersStepAwaitingTransfer => 'Ожидается подтверждение перевода';

  @override
  String get ordersPaymentUnpaid => 'Не оплачен';

  @override
  String get ordersPaymentPaid => 'Оплачен';

  @override
  String get ordersPaymentRefunded => 'Возвращён';

  @override
  String get ordersMethodCash => 'Наличные';

  @override
  String get ordersMethodCard => 'Карта (терминал курьера)';

  @override
  String get ordersMethodTransfer => 'Перевод на карту';

  @override
  String ordersItemsSummary(String count, String total) {
    return '$count товаров · $total';
  }

  @override
  String get ordersContents => 'Состав';

  @override
  String get ordersItemRemoved => 'Убран';

  @override
  String get ordersItemReplacement => 'Замена';

  @override
  String get ordersItemsTotal => 'Товары';

  @override
  String get ordersDeliveryFee => 'Доставка';

  @override
  String get ordersTotal => 'Итого';

  @override
  String get ordersCancel => 'Отменить';

  @override
  String get ordersCancelDisabled => 'Сборка началась — отменить нельзя';

  @override
  String get ordersCancelTitle => 'Отменить заказ?';

  @override
  String get ordersCancelReasonHint => 'Причина (необязательно)';

  @override
  String get ordersCancelDone => 'Заказ отменён';

  @override
  String get ordersRejectedTitle => 'Магазин отклонил заказ';

  @override
  String get ordersCancelledTitle => 'Заказ отменён';

  @override
  String ordersReason(String reason) {
    return 'Причина: $reason';
  }

  @override
  String get ordersTransferTitle => 'Перевод на карту';

  @override
  String ordersTransferHint(String amount) {
    return 'Переведите $amount на карту ниже и загрузите фото чека';
  }

  @override
  String get ordersTransferCard => 'Номер карты';

  @override
  String get ordersTransferHolder => 'Владелец карты';

  @override
  String get ordersTransferAmount => 'Сумма';

  @override
  String get ordersCopy => 'Копировать';

  @override
  String get ordersCopied => 'Скопировано';

  @override
  String get ordersUploadProof => 'Загрузить фото чека';

  @override
  String get ordersReuploadProof => 'Загрузить другое фото';

  @override
  String get ordersProofUploaded =>
      'Чек загружен — ждите подтверждения оплаты магазином';

  @override
  String get ordersTransferWaitReady =>
      'Номер карты появится после сборки заказа';

  @override
  String get ordersPaymentConfirmed => 'Оплата подтверждена';

  @override
  String get ordersFileTooBig => 'Фото не должно превышать 5 МБ';

  @override
  String catSum(String sum) {
    return '$sum сум';
  }

  @override
  String get catUnitKg => 'кг';

  @override
  String get catUnitPiece => 'шт';

  @override
  String get catDecrease => 'Уменьшить';

  @override
  String get catIncrease => 'Увеличить';

  @override
  String get catRetry => 'Повторить';

  @override
  String get catCancel => 'Отмена';

  @override
  String get catSwitchStoreTitle => 'Сменить магазин?';

  @override
  String get catSwitchStoreBody =>
      'В корзине товары другого магазина. При смене магазина корзина будет очищена.';

  @override
  String get catSwitchStoreConfirm => 'Очистить и сменить';

  @override
  String get catChooseStore => 'Выберите магазин';

  @override
  String get catChooseAnotherStore => 'Выбрать другой магазин';

  @override
  String get catChangeStore => 'Сменить магазин';

  @override
  String get catSearchStores => 'Название магазина';

  @override
  String get catNoStores => 'Магазины не найдены';

  @override
  String catDistanceKm(String km) {
    return '$km км';
  }

  @override
  String get catOpenNow => 'Открыто';

  @override
  String get catClosedNow => 'Закрыто';

  @override
  String get catSearchHint => 'Поиск товара или штрих-кода';

  @override
  String get catClear => 'Очистить';

  @override
  String get catAll => 'Все';

  @override
  String get catPopular => 'Популярное';

  @override
  String get catSearchResults => 'Результаты поиска';

  @override
  String get catSort => 'Сортировка';

  @override
  String get catSortPopular => 'Популярные';

  @override
  String get catSortName => 'По названию';

  @override
  String get catSortPriceAsc => 'Сначала дешёвые';

  @override
  String get catSortPriceDesc => 'Сначала дорогие';

  @override
  String get catNoProducts => 'Пока нет товаров';

  @override
  String get catNothingFound => 'Ничего не найдено';

  @override
  String get catDeliveryBannerTitle => 'Доставим до двери';

  @override
  String catFreeDeliveryFrom(String sum) {
    return 'Бесплатная доставка от $sum';
  }

  @override
  String get catAddToCart => 'Добавить в корзину';

  @override
  String catAddToCartSum(String sum) {
    return 'В корзину · $sum';
  }

  @override
  String get catAddedToCart => 'Добавлено в корзину';

  @override
  String get catBack => 'Назад';

  @override
  String catBarcode(String code) {
    return 'Штрих-код $code';
  }

  @override
  String catInStock(String qty) {
    return 'В наличии · $qty';
  }

  @override
  String get catOutOfStock => 'Нет в наличии';

  @override
  String get cartEmpty => 'Корзина пуста';

  @override
  String get cartEmptyHint => 'Выберите товары в каталоге';

  @override
  String get cartGoShopping => 'Перейти к покупкам';

  @override
  String get cartClear => 'Очистить';

  @override
  String get cartClearConfirm => 'Очистить корзину?';

  @override
  String get cartCheckout => 'Оформить';

  @override
  String cartFreeDeliveryLeft(String sum) {
    return 'До бесплатной доставки осталось $sum';
  }

  @override
  String get cartFreeDeliveryReached => 'Доставка бесплатно!';

  @override
  String cartMinOrderShortfall(String min, String left) {
    return 'Минимальный заказ $min. Добавьте ещё на $left.';
  }

  @override
  String get cartProblemNotFound => 'Больше не продаётся';

  @override
  String get cartProblemOutOfStock => 'Закончился';

  @override
  String cartProblemNotEnough(String qty) {
    return 'Доступно только $qty';
  }

  @override
  String get cartProblemInvalidQuantity => 'Неверное количество';

  @override
  String get cartFixQuantity => 'Уменьшить';

  @override
  String get cartRemove => 'Удалить';

  @override
  String cartItemsKinds(int count) {
    return 'Товары ($count поз.)';
  }

  @override
  String get cartDelivery => 'Доставка';

  @override
  String get cartFree => 'Бесплатно';

  @override
  String get cartTotal => 'Итого';

  @override
  String get checkoutTitle => 'Оформление';

  @override
  String get checkoutDelivery => 'Доставка';

  @override
  String get checkoutPickup => 'Самовывоз';

  @override
  String get checkoutPickupFrom => 'Адрес самовывоза';

  @override
  String get checkoutAddress => 'Адрес доставки';

  @override
  String get checkoutAddAddress => 'Добавить адрес';

  @override
  String get checkoutNoAddress => 'Укажите адрес доставки';

  @override
  String checkoutEntrance(String v) {
    return 'подъезд $v';
  }

  @override
  String checkoutFloor(String v) {
    return 'этаж $v';
  }

  @override
  String checkoutApartment(String v) {
    return 'кв. $v';
  }

  @override
  String get checkoutTime => 'Время';

  @override
  String get checkoutAsap => 'Как можно скорее';

  @override
  String get checkoutStoreClosed => 'Магазин сейчас закрыт';

  @override
  String get checkoutPickTime => 'Выбрать время';

  @override
  String get checkoutPickTimeHint => 'Сегодня / завтра, интервал 1 час';

  @override
  String checkoutToday(String range) {
    return 'Сегодня $range';
  }

  @override
  String checkoutTomorrow(String range) {
    return 'Завтра $range';
  }

  @override
  String get checkoutNoSlots => 'На сегодня и завтра нет свободного времени';

  @override
  String get checkoutPayment => 'Способ оплаты';

  @override
  String get checkoutNoPayment => 'В магазине не настроены способы оплаты';

  @override
  String get checkoutPayCash => 'Наличные';

  @override
  String get checkoutPayCashHint => 'Оплата при получении';

  @override
  String get checkoutPayCard => 'Карта (терминал)';

  @override
  String get checkoutPayCardHint => 'Через терминал при получении';

  @override
  String get checkoutPayTransfer => 'Перевод на карту';

  @override
  String get checkoutPayTransferHint => 'Номер карты покажем после сборки';

  @override
  String get checkoutOutOfStock => 'Если товара нет';

  @override
  String get checkoutPolicyCallMe => 'Позвоните мне';

  @override
  String get checkoutPolicyReplace => 'Замените похожим';

  @override
  String get checkoutPolicyRemove => 'Уберите из заказа';

  @override
  String get checkoutCommentCourier => 'Комментарий курьеру';

  @override
  String get checkoutComment => 'Комментарий';

  @override
  String get checkoutCommentHint => 'Например: домофон не работает, позвоните';

  @override
  String get checkoutContactless => 'Оставить у двери (бесконтактно)';

  @override
  String get checkoutFixCart => 'Исправьте проблемы в корзине';

  @override
  String checkoutItemsPlusDelivery(String items, String delivery) {
    return 'Товары $items + доставка $delivery';
  }

  @override
  String checkoutItemsOnly(String items) {
    return 'Товары $items';
  }

  @override
  String get checkoutPlaceOrder => 'Оформить заказ';

  @override
  String get checkoutAddressRequired => 'Выберите адрес доставки';

  @override
  String get checkoutPaymentRequired => 'Выберите способ оплаты';

  @override
  String get checkoutTimeRequired => 'Выберите время';

  @override
  String get legalTerms => 'Условия использования';

  @override
  String get legalPrivacy => 'Политика конфиденциальности';

  @override
  String get legalAgreePrefix => 'Продолжая, вы соглашаетесь с ';

  @override
  String get legalAgreeMiddle => ' и ';

  @override
  String get legalAgreeSuffix => '.';

  @override
  String get legalTermsLink => 'условиями использования';

  @override
  String get legalPrivacyLink => 'политикой конфиденциальности';

  @override
  String get updateRequiredTitle => 'Требуется обновление';

  @override
  String get updateRequiredMessage =>
      'Эта версия приложения больше не поддерживается. Установите новую версию, чтобы продолжить.';

  @override
  String get updateButton => 'Обновить';

  @override
  String get updateAvailableTitle => 'Доступна новая версия';

  @override
  String get updateAvailableMessage =>
      'Рекомендуем установить новую версию приложения.';

  @override
  String updateAvailableVersion(String version) {
    return 'Вышла версия $version. Рекомендуем обновиться.';
  }

  @override
  String get updateLater => 'Позже';

  @override
  String get updateMaintenanceTitle => 'Технические работы';

  @override
  String get updateMaintenanceMessage =>
      'Сейчас проводятся технические работы. Попробуйте позже.';

  @override
  String get updateRetry => 'Повторить';

  @override
  String get langSheetTitle => 'Язык приложения';

  @override
  String get langUzbek => 'O‘zbekcha';

  @override
  String get langRussian => 'Русский';

  @override
  String get langDevice => 'Язык устройства';

  @override
  String get pushChannelName => 'Заказы';

  @override
  String get pushChannelDescription => 'Уведомления о статусе заказа';

  @override
  String get namePromptHint =>
      'Укажите имя — так к вам будут обращаться магазин и курьер.';

  @override
  String get namePromptSkip => 'Позже';

  @override
  String deliveryMinutesRange(String min, String max) {
    return '$min–$max мин';
  }

  @override
  String deliveryMinutesUpTo(String minutes) {
    return 'до $minutes мин';
  }

  @override
  String get mapPickerTitle => 'Отметить на карте';

  @override
  String get mapPickerConfirm => 'Выбрать это место';

  @override
  String get addressMapPick => 'Отметить на карте';

  @override
  String get addressMapChange => 'Изменить на карте';

  @override
  String addressMapCoords(String coords) {
    return 'Координаты: $coords';
  }

  @override
  String get ordersProofCamera => 'Сфотографировать';

  @override
  String get ordersProofGallery => 'Выбрать из галереи';

  @override
  String get ordersProofPickFailed =>
      'Не удалось получить фото. Разрешите доступ к камере или галерее.';

  @override
  String get ordersReorderEmpty =>
      'В этом заказе нет товаров для добавления в корзину';

  @override
  String ordersCourier(String name) {
    return 'Курьер: $name';
  }

  @override
  String get ordersCallCourier => 'Позвонить курьеру';

  @override
  String ordersEta(String range) {
    return 'Ожидаемая доставка: $range';
  }

  @override
  String get ordersFiscalReceipt => 'Фискальный чек';

  @override
  String ordersReceiptNumber(String number) {
    return 'Чек № $number';
  }

  @override
  String get ordersRefundPending => 'Деньги будут возвращены';

  @override
  String get ordersRefundDone => 'Деньги возвращены';

  @override
  String get courierEntry => 'Войти как курьер';

  @override
  String get courierLoginTitle => 'Вход для курьера';

  @override
  String get courierLoginSubtitle => 'Войдите с логином и паролем от магазина';

  @override
  String get courierUsername => 'Логин';

  @override
  String get courierPassword => 'Пароль';

  @override
  String get courierSignIn => 'Войти';

  @override
  String get courierBackToCustomer => 'Войти как покупатель';

  @override
  String get courierNotCourier => 'Этот аккаунт не является курьерским';

  @override
  String get courierSessionExpired => 'Сессия истекла. Войдите снова.';

  @override
  String get courierTabActive => 'Активные';

  @override
  String get courierTabToday => 'Сегодня';

  @override
  String get courierRouteAll => 'Маршрут (все)';

  @override
  String get courierEmptyActive => 'Пока нет назначенных заказов';

  @override
  String get courierEmptyToday => 'Сегодня завершённых заказов нет';

  @override
  String courierMoney(String amount) {
    return '$amount сум';
  }

  @override
  String courierCollectCash(String amount) {
    return 'Получить $amount';
  }

  @override
  String get courierPaid => 'Оплачено';

  @override
  String get courierPaymentPending => 'Ожидается оплата';

  @override
  String courierDistanceKm(String value) {
    return '$value км';
  }

  @override
  String courierDistanceM(String value) {
    return '$value м';
  }

  @override
  String courierItemsCount(String count) {
    return 'Товаров: $count';
  }

  @override
  String get courierLogout => 'Выйти';

  @override
  String get courierLogoutConfirm => 'Выйти из режима курьера?';

  @override
  String get courierCancel => 'Отмена';

  @override
  String get courierRetry => 'Повторить';

  @override
  String get courierCall => 'Позвонить';

  @override
  String get courierCustomer => 'Клиент';

  @override
  String get courierAddress => 'Адрес';

  @override
  String get courierEntrance => 'Подъезд';

  @override
  String get courierFloor => 'Этаж';

  @override
  String get courierApartment => 'Квартира';

  @override
  String get courierIntercom => 'Домофон';

  @override
  String get courierLandmark => 'Ориентир';

  @override
  String get courierComment => 'Комментарий';

  @override
  String get courierContactless => 'Бесконтактная доставка';

  @override
  String get courierItems => 'Состав';

  @override
  String get courierTotal => 'Итого';

  @override
  String get courierYandexNavi => 'Яндекс Навигатор';

  @override
  String get courierGoogleMaps => 'Google Maps';

  @override
  String get courierDelivered => 'Доставлено';

  @override
  String get courierFailed => 'Не удалось доставить';

  @override
  String get courierCashTitle => 'Наличные получены';

  @override
  String get courierCashHint => 'Полученная сумма';

  @override
  String get courierConfirm => 'Подтвердить';

  @override
  String get courierDeliveredConfirm => 'Подтвердить доставку заказа?';

  @override
  String get courierFailedTitle => 'Почему не удалось доставить?';

  @override
  String get courierReasonNoAnswer => 'Клиент не ответил';

  @override
  String get courierReasonNotFound => 'Адрес не найден';

  @override
  String get courierReasonRefused => 'Клиент отказался';

  @override
  String get courierReasonOther => 'Другое';

  @override
  String get courierReasonOtherHint => 'Опишите причину';

  @override
  String get courierStatusNotDelivered => 'Не доставлено';

  @override
  String get courierOpenAppError => 'Не удалось открыть приложение';

  @override
  String get courierDoneDelivered => 'Заказ доставлен';

  @override
  String get courierDoneFailed => 'Заказ возвращён в магазин';

  @override
  String get courierMode => 'Курьер';

  @override
  String get courierNoCoordinates => 'Нет координат — поиск по адресу';

  @override
  String get cartPromoTitle => 'Промокод';

  @override
  String get cartPromoHint => 'Введите промокод';

  @override
  String get cartPromoApply => 'Применить';

  @override
  String get cartPromoRemove => 'Удалить';

  @override
  String cartPromoApplied(String amount) {
    return 'Скидка: −$amount';
  }

  @override
  String get cartPromoDiscount => 'Скидка по промокоду';

  @override
  String get cartBonusUsed => 'Бонусами';

  @override
  String get checkoutBonusTitle => 'Оплатить бонусами';

  @override
  String checkoutBonusBalance(String amount) {
    return 'Баланс: $amount';
  }

  @override
  String checkoutBonusWillUse(String amount) {
    return 'Спишется $amount';
  }

  @override
  String checkoutBonusMaxPercent(String percent) {
    return 'до $percent% суммы';
  }

  @override
  String get checkoutDeliveryUnavailable => 'На этот адрес доставка недоступна';

  @override
  String get checkoutSwitchToPickup => 'Выбрать самовывоз';

  @override
  String checkoutDistance(String km) {
    return 'Расстояние: $km км';
  }

  @override
  String get checkoutPayPayme => 'Payme';

  @override
  String get checkoutPayClick => 'Click';

  @override
  String get checkoutPayOnlineHint => 'Оплата в приложении после сборки';

  @override
  String get ordersPayOnlineTitle => 'Онлайн-оплата';

  @override
  String ordersPayOnlineHint(String amount) {
    return 'Заказ собран. Оплатите итоговую сумму в приложении: $amount';
  }

  @override
  String ordersPayWith(String provider) {
    return 'Оплатить через $provider';
  }

  @override
  String get ordersPayOpenFailed => 'Не удалось открыть страницу оплаты';

  @override
  String get mapMyLocation => 'Определить моё местоположение';

  @override
  String get mapLocationOff => 'Геолокация (GPS) выключена';

  @override
  String get mapLocationDenied => 'Доступ к геолокации не предоставлен';

  @override
  String get mapLocationDeniedForever =>
      'Доступ к геолокации запрещён в настройках';

  @override
  String get mapLocationFailed => 'Не удалось определить местоположение';

  @override
  String get mapOpenSettings => 'Настройки';

  @override
  String get mapSearchHint => 'Поиск адреса';

  @override
  String get mapSearchEmpty => 'Ничего не найдено';

  @override
  String get mapAddressLoading => 'Определяем адрес…';

  @override
  String get mapAddressUnknown => 'Адрес не определён — введите вручную';

  @override
  String get mapMoveHint => 'Двигайте карту — адрес под меткой определится';

  @override
  String get addressReplaceTitle => 'Заменить текст адреса?';

  @override
  String get addressReplace => 'Заменить';

  @override
  String get addressKeep => 'Оставить';

  @override
  String get bonusTitle => 'Бонусная карта';

  @override
  String get bonusBalance => 'Баланс';

  @override
  String bonusPoints(String value) {
    return '$value баллов';
  }

  @override
  String get bonusHint =>
      'Покажите этот QR на кассе — кешбэк начислится автоматически, баллами можно оплатить покупку';

  @override
  String bonusRefreshIn(String time) {
    return 'Код обновится через $time';
  }

  @override
  String get bonusRefreshing => 'Обновляем код…';

  @override
  String get bonusRefreshFailed => 'Не удалось обновить код';

  @override
  String get bonusExpired => 'Код устарел — обновите';

  @override
  String get bonusRetry => 'Повторить';

  @override
  String get bonusQrSemantics => 'QR-код бонусной карты';

  @override
  String get bonusNoQr => 'Для этого магазина QR-код пока недоступен';

  @override
  String get bonusHistory => 'История';

  @override
  String get bonusHistoryEmpty => 'Пока нет операций с баллами';

  @override
  String get bonusTypeEarn => 'Кешбэк';

  @override
  String get bonusTypeRedeem => 'Списано';

  @override
  String get bonusTypeExpire => 'Сгорели';

  @override
  String get bonusTypeAdjust => 'Корректировка';

  @override
  String get bonusTypeSignup => 'Бонус за регистрацию';

  @override
  String get bonusTypeRefund => 'Возврат';

  @override
  String get bonusTypeOther => 'Прочее';

  @override
  String get bonusSourceStore => 'Магазин';

  @override
  String get bonusSourceOnline => 'Онлайн';

  @override
  String bonusReceipt(String number) {
    return 'Чек №$number';
  }

  @override
  String get bonusGuestTitle => 'Бонусная карта';

  @override
  String get bonusGuestText =>
      'Войдите по номеру телефона — копите кешбэк с каждой покупки и оплачивайте баллами';

  @override
  String get bonusNoStoresTitle => 'У вас пока нет бонусной карты';

  @override
  String get bonusNoStoresText =>
      'Выберите магазин — карта откроется автоматически';

  @override
  String get bonusChooseStore => 'Выбрать магазин';

  @override
  String get storeChooseBranch => 'Выберите филиал';

  @override
  String get storeUnavailableTitle => 'Магазин временно не работает';

  @override
  String get storeUnavailableText => 'Попробуйте позже.';

  @override
  String get navBonus => 'Бонусы';

  @override
  String get favTitle => 'Избранное';

  @override
  String get favAdd => 'Добавить в избранное';

  @override
  String get favRemove => 'Убрать из избранного';

  @override
  String get favLoginTitle => 'Добавить в избранное';

  @override
  String get favLoginText =>
      'Чтобы сохранять избранные товары, войдите по номеру телефона.';

  @override
  String get favEmpty => 'В избранном пока пусто';

  @override
  String get favEmptyHint => 'Нажмите ♥ на товаре — он сохранится здесь';

  @override
  String get mkAll => 'Все';

  @override
  String get mkClose => 'Закрыть';

  @override
  String get mkStoryOpen => 'Подробнее';

  @override
  String mkEndsIn(String time) {
    return 'До конца $time';
  }

  @override
  String mkDiscountEndsIn(String time) {
    return 'До конца акции $time';
  }

  @override
  String get mkDiscountsTitle => 'Акции';

  @override
  String get mkDiscountsEmpty => 'Сейчас нет товаров по акции';

  @override
  String mkDiscountsEntry(int count) {
    return 'Акции ($count)';
  }

  @override
  String get mkDiscountsEntryHint => 'Все товары со скидкой';

  @override
  String get mkBuyAgain => 'Купите снова?';

  @override
  String get mkTop10 => 'Топ 10';

  @override
  String get mkRelated => 'С этим покупают';

  @override
  String get mkCartSuggestions => 'Добавьте к заказу';

  @override
  String get mkReorderTitle => 'Повторить последний заказ';

  @override
  String mkReorderSubtitle(String number, String sum) {
    return '№$number · $sum';
  }

  @override
  String get mkReorderButton => 'Повторить';

  @override
  String get mkNotifyMe => 'Сообщить о поступлении';

  @override
  String get mkNotifySubscribed => 'Сообщим о поступлении';

  @override
  String get mkNotifyOn => 'Сообщим, когда товар появится';

  @override
  String get mkNotifyOff => 'Уведомление отменено';

  @override
  String cdDaysHours(int days, int hours) {
    return '$days дн $hours ч';
  }

  @override
  String cdHoursMinutes(int hours, int minutes) {
    return '$hours ч $minutes мин';
  }

  @override
  String cdMinutes(int minutes) {
    return '$minutes мин';
  }

  @override
  String get searchHistory => 'История поиска';

  @override
  String get searchHistoryClear => 'Очистить';

  @override
  String get scanTitle => 'Сканировать штрихкод';

  @override
  String get scanTorch => 'Фонарик';

  @override
  String get scanHint => 'Наведите рамку на штрихкод';

  @override
  String get scanError =>
      'Не удалось открыть камеру. Разрешите доступ к камере в настройках.';

  @override
  String get rateTitle => 'Оцените заказ';

  @override
  String get rateSubtitle => 'Ваш отзыв поможет нам стать лучше';

  @override
  String get rateYourRating => 'Ваша оценка';

  @override
  String get rateCommentHint => 'Комментарий (необязательно)';

  @override
  String get rateSubmit => 'Отправить';

  @override
  String get rateThanks => 'Спасибо! Оценка принята';

  @override
  String rateStars(int count) {
    return '$count из 5';
  }

  @override
  String get rateTagFast => 'Быстрая доставка';

  @override
  String get rateTagFresh => 'Свежие продукты';

  @override
  String get rateTagPolite => 'Вежливый курьер';

  @override
  String get rateTagAccurate => 'Всё точно';

  @override
  String get rateTagLate => 'Опоздание';

  @override
  String get rateTagDamaged => 'Повреждено';

  @override
  String get rateTagMissing => 'Чего-то не хватило';

  @override
  String get rateTagWrongItem => 'Привезли не тот товар';

  @override
  String get supportTitle => 'Помощь';

  @override
  String get supportText => 'Есть вопрос? Свяжитесь с нами';

  @override
  String get supportCall => 'Позвонить';

  @override
  String get supportTelegram => 'Написать в Telegram';

  @override
  String get supportNone => 'Магазин пока не указал контакты';

  @override
  String get refTitle => 'Пригласить друга';

  @override
  String get refUnavailable =>
      'В этом магазине приглашение друзей пока недоступно';

  @override
  String refHeadline(String bonus) {
    return 'Пригласите друга — получите $bonus';
  }

  @override
  String refExplain(String bonus) {
    return 'Друг тоже получит $bonus — когда его первый заказ будет доставлен';
  }

  @override
  String refShareFallback(String code, String bonus) {
    return 'Мой код: $code. Введите его в приложении и получите $bonus!';
  }

  @override
  String get refYourCode => 'Ваш код';

  @override
  String get refCopy => 'Копировать';

  @override
  String get refCopied => 'Код скопирован';

  @override
  String get refShare => 'Поделиться';

  @override
  String get refInvited => 'Приглашено';

  @override
  String get refRewarded => 'Получено бонусов';

  @override
  String get refHaveCode => 'Есть код друга?';

  @override
  String get refHaveCodeHint => 'Введите до первого заказа';

  @override
  String get refCodeHint => 'Код';

  @override
  String get refApplied => 'Код принят';

  @override
  String get tierNone => 'Уровня пока нет';

  @override
  String tierCurrent(String name, String percent) {
    return '$name · кешбэк $percent%';
  }

  @override
  String tierNextLeft(String name, String sum) {
    return 'До уровня $name: $sum';
  }

  @override
  String get tierTop => 'У вас максимальный уровень';

  @override
  String catOpensAt(String time) {
    return 'Откроется в $time';
  }

  @override
  String get checkoutWhenOpens => 'Когда магазин откроется';

  @override
  String checkoutWhenOpensAt(String time) {
    return 'Когда магазин откроется ($time)';
  }

  @override
  String get checkoutPreorderHint => 'Предзаказ';

  @override
  String get ordersPreorderTitle => 'Предзаказ';

  @override
  String ordersPreorderNote(String time) {
    return 'Магазин сейчас закрыт. Заказ рассмотрят в $time.';
  }

  @override
  String get ordersPreorderNoteNoTime =>
      'Магазин сейчас закрыт. Заказ рассмотрят, когда магазин откроется.';

  @override
  String get courierTabOffers => 'Предложения';

  @override
  String get courierOffersEmpty => 'Пока нет предложений';

  @override
  String get courierOfferTitle => 'Новый заказ';

  @override
  String courierOfferNumber(String number) {
    return 'Заказ #$number';
  }

  @override
  String get courierOfferExpired => 'Время предложения истекло';

  @override
  String get courierOfferAccept => 'Принимаю';

  @override
  String get courierOfferDecline => 'Отказываюсь';

  @override
  String get courierOfferTaken => 'Заказ взял другой курьер';

  @override
  String get courierOfferClosed => 'Предложение закрыто';

  @override
  String get courierOfferAccepted => 'Заказ закреплён за вами';

  @override
  String get courierOfferCash => 'Оплата наличными';

  @override
  String get courierOfferPaidOrCard => 'Карта / оплачен';

  @override
  String get courierBack => 'Назад';

  @override
  String get courierPickedUpFromStore => 'Забрал из магазина';

  @override
  String get courierAwaitingPickup => 'Забрать в магазине';

  @override
  String get courierDonePickedUp => 'Заказ в пути';

  @override
  String get courierReturnTask => 'Возврат';

  @override
  String courierReturnNumber(String number) {
    return 'Возврат #$number';
  }

  @override
  String get courierReturnPickedUp => 'Забрал';

  @override
  String get courierReturnPickedUpConfirm =>
      'Подтверждаете, что забрали товары у клиента?';

  @override
  String get courierReturnDone => 'Возврат забран';

  @override
  String get courierReturnNoCash => 'Деньги не выдаются — их вернёт магазин';

  @override
  String get courierRejectItems => 'Клиент отказался от части товаров';

  @override
  String get courierRejectTitle => 'От каких товаров отказался клиент?';

  @override
  String get courierRejectedEdit => 'Изменить отказ';

  @override
  String get courierToCollect => 'К получению';

  @override
  String get courierRejectApprox => 'Примерно — итог пересчитает сервер';

  @override
  String get courierSave => 'Сохранить';

  @override
  String get returnReasonDamaged => 'Повреждён';

  @override
  String get returnReasonWrongItem => 'Не тот товар';

  @override
  String get returnReasonQuality => 'Плохое качество';

  @override
  String get returnReasonExpired => 'Просрочен';

  @override
  String get returnReasonMissing => 'Не хватает';

  @override
  String get returnReasonOther => 'Другое';

  @override
  String get refundMethodCash => 'Наличными (в магазине)';

  @override
  String get refundMethodTransfer => 'Перевод на карту';

  @override
  String get refundMethodBonus => 'Бонусные баллы';

  @override
  String get returnMethodCourier => 'Заберёт курьер';

  @override
  String get returnMethodStore => 'Принесёте в магазин';

  @override
  String get returnStatusRequested => 'На рассмотрении';

  @override
  String get returnStatusApproved => 'Одобрен';

  @override
  String get returnStatusPickupAssigned => 'Назначен курьер';

  @override
  String get returnStatusPickedUp => 'Курьер забрал';

  @override
  String get returnStatusReceived => 'Получен магазином';

  @override
  String get returnStatusRefunded => 'Деньги возвращены';

  @override
  String get returnStatusRejected => 'Отклонён';

  @override
  String get returnStatusCancelled => 'Отменён';

  @override
  String get returnsTitle => 'Мои возвраты';

  @override
  String get returnsEmpty => 'Возвратов нет';

  @override
  String get returnAction => 'Вернуть товар';

  @override
  String get returnRequestTitle => 'Возврат товара';

  @override
  String get returnChooseItems => 'Товары для возврата';

  @override
  String get returnNothingToReturn => 'Нет товаров для возврата';

  @override
  String get returnReason => 'Причина';

  @override
  String get returnSelectReason => 'Выберите причину';

  @override
  String get returnComment => 'Комментарий';

  @override
  String get returnCommentHint => 'Кратко опишите проблему';

  @override
  String get returnPhotos => 'Фото (до 3)';

  @override
  String get returnAddPhoto => 'Фото';

  @override
  String get returnRemovePhoto => 'Удалить фото';

  @override
  String get returnRefundMethod => 'Способ возврата денег';

  @override
  String get returnCashbackNote =>
      'Кешбэк, начисленный за эти товары, будет списан.';

  @override
  String get returnAmount => 'Сумма к возврату';

  @override
  String get returnSubmit => 'Отправить запрос';

  @override
  String get returnSelectItems => 'Выберите хотя бы один товар';

  @override
  String get returnSent => 'Запрос отправлен';

  @override
  String returnNumber(String number) {
    return 'Возврат #$number';
  }

  @override
  String get returnWithheld => 'Удержан кешбэк';

  @override
  String get returnCancel => 'Отменить запрос';

  @override
  String get returnCancelConfirm => 'Отменить запрос на возврат?';

  @override
  String get returnCancelled => 'Запрос отменён';

  @override
  String returnRejectReason(String reason) {
    return 'Причина отказа: $reason';
  }

  @override
  String get returnTimeline => 'Статус';

  @override
  String get returnMethod => 'Способ возврата';
}
