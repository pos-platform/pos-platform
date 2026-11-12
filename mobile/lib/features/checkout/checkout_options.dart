import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../cart/cart_controller.dart';

/// Savatcha va rasmiylashtirish ekranlari uchun umumiy tanlovlar: promokod, bonus, manzil.
/// Hisob (`/quote`) shu qiymatlar bilan so'raladi — chegirma va bonus faqat serverda hisoblanadi.
class CheckoutOptions {
  const CheckoutOptions({this.promoCode, this.useBonus = false, this.addressId});

  /// Kiritilgan promokod (null — yo'q). Yaroqliligini server aytadi (`promoMessage`).
  final String? promoCode;
  final bool useBonus;

  /// Yetkazish manzili (rasmiylashtirishda tanlanadi; savatchada ham ma'lum bo'lsa yuboriladi).
  final String? addressId;

  CheckoutOptions copyWith({String? Function()? promoCode, bool? useBonus, String? Function()? addressId}) =>
      CheckoutOptions(
        promoCode: promoCode == null ? this.promoCode : promoCode(),
        useBonus: useBonus ?? this.useBonus,
        addressId: addressId == null ? this.addressId : addressId(),
      );
}

class CheckoutOptionsController extends Notifier<CheckoutOptions> {
  @override
  CheckoutOptions build() {
    // Boshqa do'kon savatchasi yoki savatcha tozalansa — tanlovlar ham tozalanadi.
    ref.listen(cartControllerProvider.select((c) => c.branchId), (prev, next) {
      if (prev != next) state = const CheckoutOptions();
    });
    return const CheckoutOptions();
  }

  void applyPromo(String code) {
    final c = code.trim();
    state = state.copyWith(promoCode: () => c.isEmpty ? null : c);
  }

  void removePromo() => state = state.copyWith(promoCode: () => null);

  void setUseBonus(bool value) {
    if (state.useBonus != value) state = state.copyWith(useBonus: value);
  }

  void setAddress(String? id) {
    if (state.addressId != id) state = state.copyWith(addressId: () => id);
  }

  void reset() => state = const CheckoutOptions();
}

final checkoutOptionsProvider =
    NotifierProvider<CheckoutOptionsController, CheckoutOptions>(CheckoutOptionsController.new);
