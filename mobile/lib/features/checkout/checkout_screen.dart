import 'dart:math';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../cart/cart_controller.dart';
import '../cart/data/order_api.dart';
import '../cart/delivery_time.dart';
import '../catalog/data/catalog_models.dart';
import '../catalog/data/catalog_repository.dart';
import '../catalog/widgets/common.dart';
import '../profile/data/addresses_repository.dart';
import '../profile/data/bonus_repository.dart';
import 'checkout_options.dart';

/// Do'kon vaqti — UTC+5 (serverdagi `LocalOffset` bilan bir xil).
const _storeOffset = Duration(hours: 5);

/// Rasmiylashtirish (dizayn: Checkout.dc.html): yetkazish/olib ketish, manzil, vaqt, to'lov usuli,
/// mahsulot tugasa nima qilish, izoh, kontaktsiz, jami, "Buyurtma berish".
/// Takroriy bosish / tarmoq uzilishidan himoya: `clientRequestId` savatcha o'zgarmaguncha bir xil qoladi.
/// Promokod (savatchada kiritiladi), bonus va manzil [checkoutOptionsProvider] orqali hisobga (`/quote`) yuboriladi.
/// Yetkazish hududidan tashqari manzilda buyurtma tugmasi o'chadi. Payme/Click — yig'ilgandan keyin ilovada to'lanadi.
class CheckoutScreen extends ConsumerWidget {
  const CheckoutScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final branchId = ref.watch(cartControllerProvider.select((c) => c.branchId));
    if (branchId == null) {
      return Scaffold(
        appBar: AppBar(title: Text(t.checkoutTitle)),
        body: Center(child: Text(t.cartEmpty, style: const TextStyle(color: AppColors.textSecondary))),
      );
    }
    final store = ref.watch(storeProvider(branchId));
    return switch (store) {
      AsyncData(:final value) => _CheckoutForm(key: ValueKey(value.id), store: value),
      AsyncError(:final error) => Scaffold(
          appBar: AppBar(title: Text(t.checkoutTitle)),
          body: ErrorRetry(
            message: ApiException.from(error).message,
            onRetry: () => ref.invalidate(storeProvider(branchId)),
          ),
        ),
      _ => Scaffold(appBar: AppBar(title: Text(t.checkoutTitle)), body: const Center(child: CircularProgressIndicator())),
    };
  }
}

class _CheckoutForm extends ConsumerStatefulWidget {
  const _CheckoutForm({super.key, required this.store});

  final ShopStore store;

  @override
  ConsumerState<_CheckoutForm> createState() => _CheckoutFormState();
}

class _CheckoutFormState extends ConsumerState<_CheckoutForm> {
  late FulfillmentType _type;
  String? _addressId;

  /// Null — imkon qadar tez.
  DateTime? _slotUtc;
  bool _scheduled = false;
  _PayOption? _payment;
  OutOfStockPolicy _policy = OutOfStockPolicy.callMe;
  bool _contactless = false;
  final _comment = TextEditingController();
  bool _submitting = false;

  /// Idempotency kaliti va u qaysi savatcha holati uchun yaratilgani (savatcha o'zgarsa — yangi kalit).
  String? _requestId;
  CartState? _requestIdCart;

  ShopStore get _store => widget.store;

  List<_PayOption> get _payments => [
        if (_store.allowCash) _PayOption.cash,
        if (_store.allowCardOnDelivery) _PayOption.card,
        if (_store.allowTransfer) _PayOption.transfer,
        if (_store.allowPayme) _PayOption.payme,
        if (_store.allowClick) _PayOption.click,
      ];

  @override
  void initState() {
    super.initState();
    _type = _store.deliveryEnabled ? FulfillmentType.delivery : FulfillmentType.pickup;
    _payment = _payments.firstOrNull;
    // Do'kon hozir yopiq bo'lsa — faqat vaqt tanlab buyurtma berish mumkin.
    _scheduled = !_store.isOpenNow;
  }

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  /// Bugun/ertaga 1 soatlik oraliqlar (boshlanish vaqti UTC). Server: kamida 30 daq keyin, 2 kun ichida, ish vaqtida.
  List<DateTime> _slots() {
    final nowUtc = DateTime.now().toUtc();
    final earliest = nowUtc.add(const Duration(minutes: 30));
    final latest = nowUtc.add(const Duration(days: 2));
    final local = nowUtc.add(_storeOffset);
    final midnight = DateTime.utc(local.year, local.month, local.day); // do'kon kunining boshi ("mahalliy" maydonlar)
    final result = <DateTime>[];
    for (var day = 0; day < 2; day++) {
      for (var h = 0; h < 24; h++) {
        final startUtc = midnight.add(Duration(days: day, hours: h)).subtract(_storeOffset);
        if (startUtc.isBefore(earliest) || startUtc.isAfter(latest)) continue;
        final m = h * 60;
        if (!_store.isWithinHours(m) || !_store.isWithinHours((m + 59) % 1440)) continue;
        result.add(startUtc);
      }
    }
    return result;
  }

  String _slotText(AppLocalizations t, DateTime startUtc) {
    final local = startUtc.add(_storeOffset);
    final today = DateTime.now().toUtc().add(_storeOffset);
    final isToday = local.year == today.year && local.month == today.month && local.day == today.day;
    String hh(int h) => '${(h % 24).toString().padLeft(2, '0')}:00';
    final range = '${hh(local.hour)}–${hh(local.hour + 1)}';
    return isToday ? t.checkoutToday(range) : t.checkoutTomorrow(range);
  }

  Future<void> _pickSlot() async {
    final t = AppLocalizations.of(context);
    final slots = _slots();
    final picked = await showModalBottomSheet<DateTime>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (ctx) => SafeArea(
        child: ConstrainedBox(
          constraints: BoxConstraints(maxHeight: MediaQuery.sizeOf(ctx).height * 0.7),
          child: slots.isEmpty
              ? Padding(padding: const EdgeInsets.all(24), child: Text(t.checkoutNoSlots, textAlign: TextAlign.center))
              : ListView(shrinkWrap: true, children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
                    child: Text(t.checkoutPickTime, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                  ),
                  for (final s in slots)
                    ListTile(
                      title: Text(_slotText(t, s)),
                      trailing: s == _slotUtc ? const Icon(Icons.check, color: AppColors.primary) : null,
                      onTap: () => Navigator.pop(ctx, s),
                    ),
                ]),
        ),
      ),
    );
    if (picked != null) {
      setState(() {
        _slotUtc = picked;
        _scheduled = true;
      });
    }
  }

  Future<void> _pickAddress(List<ShopAddress> addresses) async {
    final t = AppLocalizations.of(context);
    final result = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (ctx) => SafeArea(
        child: ConstrainedBox(
          constraints: BoxConstraints(maxHeight: MediaQuery.sizeOf(ctx).height * 0.7),
          child: ListView(shrinkWrap: true, children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
              child: Text(t.checkoutAddress, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            ),
            for (final a in addresses)
              ListTile(
                leading: Icon(a.id == _addressId ? Icons.radio_button_checked : Icons.radio_button_off,
                    color: a.id == _addressId ? AppColors.primary : AppColors.iconMuted),
                title: Text(a.label.isEmpty ? a.addressLine : a.label,
                    style: const TextStyle(fontWeight: FontWeight.w700)),
                subtitle: Text(a.addressLine),
                onTap: () => Navigator.pop(ctx, a.id),
              ),
            ListTile(
              leading: const Icon(Icons.add, color: AppColors.primary),
              title: Text(t.checkoutAddAddress,
                  style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
              onTap: () => Navigator.pop(ctx, ''),
            ),
          ]),
        ),
      ),
    );
    if (!mounted || result == null) return;
    if (result.isEmpty) {
      await _addAddress();
    } else {
      setState(() => _addressId = result);
    }
  }

  Future<void> _addAddress() async {
    await context.push('/address-new');
    if (mounted) ref.invalidate(addressesProvider);
  }

  ShopAddress? _selectedAddress(List<ShopAddress> list) {
    if (list.isEmpty) return null;
    for (final a in list) {
      if (a.id == _addressId) return a;
    }
    // Tanlanmagan bo'lsa — asosiy manzil.
    return list.firstWhere((a) => a.isDefault, orElse: () => list.first);
  }

  Future<void> _submit(ShopAddress? address, OrderQuote quote, bool useBonus) async {
    final t = AppLocalizations.of(context);
    final cart = ref.read(cartControllerProvider);
    final options = ref.read(checkoutOptionsProvider);
    final messenger = ScaffoldMessenger.of(context);
    String? error;
    if (_type == FulfillmentType.delivery && address == null) error = t.checkoutAddressRequired;
    if (_payment == null) error = t.checkoutPaymentRequired;
    if (_scheduled && _slotUtc == null) error = t.checkoutTimeRequired;
    if (error != null) {
      messenger.showSnackBar(SnackBar(content: Text(error)));
      return;
    }

    if (_requestId == null || !identical(_requestIdCart, cart)) {
      _requestId = _newRequestId();
      _requestIdCart = cart;
    }

    setState(() => _submitting = true);
    try {
      final id = await ref.read(orderApiProvider).create(CreateOrderRequest(
            branchId: _store.id,
            fulfillmentType: _type,
            items: cart.lines.values.toList(),
            addressId: address?.id,
            paymentMethod: _payment!.method,
            onlinePaymentProvider: _payment!.provider,
            // Yaroqsiz promokod yuborilmaydi (server sababini hisobda aytgan).
            promoCode: quote.promoMessage == null ? options.promoCode : null,
            useBonus: useBonus,
            scheduledForUtc: _scheduled ? _slotUtc : null,
            contactless: _type == FulfillmentType.delivery && _contactless,
            comment: _comment.text,
            outOfStockPolicy: _policy,
            clientRequestId: _requestId,
          ));
      if (!mounted) return;
      ref.read(cartControllerProvider.notifier).clear();
      ref.read(checkoutOptionsProvider.notifier).reset();
      context.go('/orders/$id');
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _submitting = false);
      messenger.showSnackBar(SnackBar(content: Text(e.message)));
      ref.invalidate(cartQuoteProvider(_type)); // narx/qoldiq o'zgargan bo'lishi mumkin
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final isDelivery = _type == FulfillmentType.delivery;
    final quoteAsync = ref.watch(cartQuoteProvider(_type));
    final quote = quoteAsync.value;
    final addressesAsync = isDelivery ? ref.watch(addressesProvider) : null;
    final address = addressesAsync?.value == null ? null : _selectedAddress(addressesAsync!.value!);
    final options = ref.watch(checkoutOptionsProvider);
    // Tanlangan manzil hisobga ham yuboriladi (yetkazish hududi, masofa) — kadrdan keyin, build ichida holat o'zgarmaydi.
    if (isDelivery && addressesAsync?.value != null && options.addressId != address?.id) {
      final id = address?.id;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) ref.read(checkoutOptionsProvider.notifier).setAddress(id);
      });
    }
    final bonusBalance = _store.bonusPayEnabled ? ref.watch(bonusBalanceProvider(_store.id)).value ?? 0.0 : 0.0;
    final showBonus = _store.bonusPayEnabled && bonusBalance > 0;
    final useBonus = showBonus && options.useBonus;
    // Hudud: radiusdan tashqarida — buyurtma berilmaydi.
    final zoneBlocked = isDelivery && quote != null && !quote.deliveryAvailable;

    return Scaffold(
      appBar: AppBar(title: Text(t.checkoutTitle)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
        children: [
          if (_store.deliveryEnabled && _store.pickupEnabled) ...[
            _Segmented(
              options: [t.checkoutDelivery, t.checkoutPickup],
              selected: isDelivery ? 0 : 1,
              onChanged: (i) => setState(() => _type = i == 0 ? FulfillmentType.delivery : FulfillmentType.pickup),
            ),
            const SizedBox(height: 16),
          ],
          if (isDelivery)
            _addressCard(t, addressesAsync!, address, quote)
          else
            _InfoCard(
              icon: Icons.storefront_outlined,
              title: t.checkoutPickupFrom,
              lines: [_store.name, ?_store.address],
            ),
          if (isDelivery && quote?.deliveryMessage != null) ...[
            const SizedBox(height: 10),
            _ZoneNotice(
              text: quote!.deliveryMessage!,
              blocked: !quote.deliveryAvailable,
              onPickup: !quote.deliveryAvailable && _store.pickupEnabled
                  ? () => setState(() => _type = FulfillmentType.pickup)
                  : null,
            ),
          ] else if (zoneBlocked) ...[
            const SizedBox(height: 10),
            _ZoneNotice(
              text: t.checkoutDeliveryUnavailable,
              blocked: true,
              onPickup: _store.pickupEnabled ? () => setState(() => _type = FulfillmentType.pickup) : null,
            ),
          ],
          const SizedBox(height: 20),
          _SectionTitle(t.checkoutTime),
          Row(children: [
            Expanded(
              child: _OptionCard(
                title: t.checkoutAsap,
                subtitle: !_store.isOpenNow
                    ? t.checkoutStoreClosed
                    : isDelivery
                        ? deliveryTimeText(t, _store)
                        : null,
                selected: !_scheduled,
                onTap: _store.isOpenNow ? () => setState(() => _scheduled = false) : null,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _OptionCard(
                title: t.checkoutPickTime,
                subtitle: _scheduled && _slotUtc != null ? _slotText(t, _slotUtc!) : t.checkoutPickTimeHint,
                selected: _scheduled,
                onTap: _pickSlot,
              ),
            ),
          ]),
          const SizedBox(height: 20),
          _SectionTitle(t.checkoutPayment),
          if (_payments.isEmpty)
            Text(t.checkoutNoPayment, style: const TextStyle(color: AppColors.danger))
          else
            Column(children: [
              for (final p in _payments)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: _PaymentTile(
                    option: p,
                    selected: _payment == p,
                    onTap: () => setState(() => _payment = p),
                  ),
                ),
            ]),
          if (showBonus) ...[
            const SizedBox(height: 4),
            _BonusCard(
              balance: bonusBalance,
              used: useBonus ? quote?.bonusUsed : null,
              maxPercent: _store.bonusMaxPercent,
              value: useBonus,
              onChanged: (v) => ref.read(checkoutOptionsProvider.notifier).setUseBonus(v),
            ),
            const SizedBox(height: 8),
          ],
          const SizedBox(height: 12),
          _SectionTitle(t.checkoutOutOfStock),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (final p in OutOfStockPolicy.values)
              ChoiceChip(
                label: Text(switch (p) {
                  OutOfStockPolicy.callMe => t.checkoutPolicyCallMe,
                  OutOfStockPolicy.replaceSimilar => t.checkoutPolicyReplace,
                  OutOfStockPolicy.remove => t.checkoutPolicyRemove,
                }),
                selected: _policy == p,
                showCheckmark: false,
                selectedColor: AppColors.primarySoft,
                backgroundColor: AppColors.surface,
                side: BorderSide(color: _policy == p ? AppColors.primary : AppColors.border),
                labelStyle: TextStyle(
                    fontWeight: FontWeight.w600, color: _policy == p ? AppColors.primary : AppColors.text),
                shape: const StadiumBorder(),
                onSelected: (_) => setState(() => _policy = p),
              ),
          ]),
          const SizedBox(height: 20),
          _SectionTitle(isDelivery ? t.checkoutCommentCourier : t.checkoutComment),
          TextField(
            controller: _comment,
            maxLength: 500,
            minLines: 1,
            maxLines: 3,
            decoration: InputDecoration(hintText: t.checkoutCommentHint, counterText: ''),
          ),
          if (isDelivery)
            CheckboxListTile(
              value: _contactless,
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              activeColor: AppColors.primary,
              title: Text(t.checkoutContactless, style: const TextStyle(fontSize: 14)),
              onChanged: (v) => setState(() => _contactless = v ?? false),
            ),
          if (quote?.message != null) ...[
            const SizedBox(height: 8),
            Text(quote!.message!, style: const TextStyle(color: AppColors.danger)),
          ],
          if (quote != null && quote.minOrderShortfall > 0) ...[
            const SizedBox(height: 8),
            Text(
              t.cartMinOrderShortfall(
                  formatMoney(context, quote.minOrderAmount), formatMoney(context, quote.minOrderShortfall)),
              style: const TextStyle(color: AppColors.danger),
            ),
          ],
          if (quote != null && quote.lines.any((l) => l.problem != null)) ...[
            const SizedBox(height: 8),
            Text(t.checkoutFixCart, style: const TextStyle(color: AppColors.danger)),
          ],
        ],
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.borderSoft)),
        ),
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              if (quote != null && quote.promoDiscount > 0)
                _MiniRow(t.cartPromoDiscount, '−${formatMoney(context, quote.promoDiscount)}'),
              if (quote != null && quote.bonusUsed > 0)
                _MiniRow(t.cartBonusUsed, '−${formatMoney(context, quote.bonusUsed)}'),
              if (quote != null)
                Text(
                  isDelivery
                      ? t.checkoutItemsPlusDelivery(
                          formatMoney(context, quote.itemsTotal), formatMoney(context, quote.deliveryFee))
                      : t.checkoutItemsOnly(formatMoney(context, quote.itemsTotal)),
                  style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                ),
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(children: [
                  Expanded(
                    child: Text(t.cartTotal, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                  ),
                  Text(quote == null ? '…' : formatMoney(context, quote.totalAmount),
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                ]),
              ),
              FilledButton(
                onPressed: _submitting ||
                        quote == null ||
                        quoteAsync.isLoading ||
                        !quote.canOrder ||
                        zoneBlocked ||
                        _payments.isEmpty
                    ? null
                    : () => _submit(address, quote, useBonus),
                child: _submitting
                    ? const SizedBox(
                        width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                    : Text(t.checkoutPlaceOrder),
              ),
            ]),
          ),
        ),
      ),
    );
  }

  Widget _addressCard(AppLocalizations t, AsyncValue<List<ShopAddress>> async, ShopAddress? address, OrderQuote? quote) {
    if (async.hasError && async.value == null) {
      return ErrorRetry(message: ApiException.from(async.error!).message, onRetry: () => ref.invalidate(addressesProvider));
    }
    final list = async.value;
    if (list == null) {
      return const SizedBox(height: 72, child: Center(child: CircularProgressIndicator()));
    }
    if (address == null) {
      return _InfoCard(
        icon: Icons.add_location_alt_outlined,
        title: t.checkoutAddAddress,
        lines: [t.checkoutNoAddress],
        onTap: _addAddress,
      );
    }
    final details = [
      if (address.entrance?.isNotEmpty ?? false) t.checkoutEntrance(address.entrance!),
      if (address.floor?.isNotEmpty ?? false) t.checkoutFloor(address.floor!),
      if (address.apartment?.isNotEmpty ?? false) t.checkoutApartment(address.apartment!),
    ].join(' · ');
    // Masofa faqat shu manzil uchun hisoblangan bo'lsa ko'rsatiladi.
    final km = ref.read(checkoutOptionsProvider).addressId == address.id ? quote?.distanceKm : null;
    return _InfoCard(
      icon: Icons.location_on_outlined,
      title: address.label.isEmpty ? t.checkoutAddress : address.label,
      lines: [
        address.addressLine,
        if (details.isNotEmpty) details,
        if (km != null) t.checkoutDistance(km.toStringAsFixed(1)),
      ],
      onTap: () => _pickAddress(list),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: Text(text, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      );
}

/// Ikki variantli tugma (Yetkazib berish / Do'kondan olib ketish).
class _Segmented extends StatelessWidget {
  const _Segmented({required this.options, required this.selected, required this.onChanged});

  final List<String> options;
  final int selected;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(4),
        decoration: BoxDecoration(color: AppColors.chipSurface, borderRadius: BorderRadius.circular(16)),
        child: Row(children: [
          for (final (i, label) in options.indexed)
            Expanded(
              child: Semantics(
                selected: i == selected,
                button: true,
                child: Material(
                  color: i == selected ? AppColors.surface : Colors.transparent,
                  borderRadius: BorderRadius.circular(12),
                  child: InkWell(
                    borderRadius: BorderRadius.circular(12),
                    onTap: () => onChanged(i),
                    child: SizedBox(
                      height: 44,
                      child: Center(
                        child: Text(label,
                            style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                color: i == selected ? AppColors.text : AppColors.textSecondary)),
                      ),
                    ),
                  ),
                ),
              ),
            ),
        ]),
      );
}

class _InfoCard extends StatelessWidget {
  const _InfoCard({required this.icon, required this.title, required this.lines, this.onTap});

  final IconData icon;
  final String title;
  final List<String> lines;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Material(
        color: AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTheme.radiusCard),
          side: const BorderSide(color: AppColors.borderSoft),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.circular(12)),
                child: Icon(icon, color: AppColors.primary),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(title, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
                  for (final l in lines)
                    Text(l, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
                ]),
              ),
              if (onTap != null) const Icon(Icons.chevron_right, color: AppColors.textSecondary),
            ]),
          ),
        ),
      );
}

class _OptionCard extends StatelessWidget {
  const _OptionCard({required this.title, this.subtitle, required this.selected, required this.onTap});

  final String title;
  final String? subtitle;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Semantics(
        selected: selected,
        button: true,
        child: Material(
          color: selected ? AppColors.primarySoft : AppColors.surface,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(color: selected ? AppColors.primary : AppColors.border, width: selected ? 2 : 1),
          ),
          child: InkWell(
            borderRadius: BorderRadius.circular(16),
            onTap: onTap,
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: 72),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(title,
                        style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w800,
                            color: onTap == null ? AppColors.iconMuted : AppColors.text)),
                    if (subtitle != null)
                      Text(subtitle!, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                  ],
                ),
              ),
            ),
          ),
        ),
      );
}

class _PaymentTile extends StatelessWidget {
  const _PaymentTile({required this.option, required this.selected, required this.onTap});

  final _PayOption option;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final (icon, name, hint) = switch (option) {
      _PayOption.cash => (Icons.payments_outlined, t.checkoutPayCash, t.checkoutPayCashHint),
      _PayOption.card => (Icons.credit_card, t.checkoutPayCard, t.checkoutPayCardHint),
      _PayOption.transfer => (Icons.swap_horiz, t.checkoutPayTransfer, t.checkoutPayTransferHint),
      _PayOption.payme => (Icons.account_balance_wallet_outlined, t.checkoutPayPayme, t.checkoutPayOnlineHint),
      _PayOption.click => (Icons.account_balance_wallet_outlined, t.checkoutPayClick, t.checkoutPayOnlineHint),
    };
    return Material(
      color: AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: selected ? AppColors.primary : AppColors.border, width: selected ? 2 : 1),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(children: [
            Icon(icon, color: AppColors.primary),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(name, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                Text(hint, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
              ]),
            ),
            Icon(selected ? Icons.radio_button_checked : Icons.radio_button_off,
                color: selected ? AppColors.primary : AppColors.iconMuted),
          ]),
        ),
      ),
    );
  }
}

/// Checkout'dagi to'lov varianti. Payme/Click — server uchun `Transfer` + `onlinePaymentProvider`.
enum _PayOption {
  cash(PaymentMethod.cash),
  card(PaymentMethod.card),
  transfer(PaymentMethod.transfer),
  payme(PaymentMethod.transfer, OnlineProvider.payme),
  click(PaymentMethod.transfer, OnlineProvider.click);

  const _PayOption(this.method, [this.provider]);
  final PaymentMethod method;
  final OnlineProvider? provider;
}

/// Pastki paneldagi kichik qator (promokod, bonus).
class _MiniRow extends StatelessWidget {
  const _MiniRow(this.label, this.value);

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 2),
        child: Row(children: [
          Expanded(child: Text(label, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary))),
          Text(value, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.primary)),
        ]),
      );
}

/// Yetkazish hududi haqida xabar: [blocked] — buyurtma berib bo'lmaydi (olib ketish taklif qilinadi), aks holda ogohlantirish.
class _ZoneNotice extends StatelessWidget {
  const _ZoneNotice({required this.text, required this.blocked, this.onPickup});

  final String text;
  final bool blocked;
  final VoidCallback? onPickup;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final color = blocked ? AppColors.danger : AppColors.accent;
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      decoration: BoxDecoration(
        color: blocked ? const Color(0xFFFCECEA) : const Color(0xFFFDF0E6),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Icon(blocked ? Icons.wrong_location_outlined : Icons.info_outline, size: 20, color: color),
          const SizedBox(width: 10),
          Expanded(child: Text(text, style: const TextStyle(fontSize: 14, color: AppColors.text))),
        ]),
        if (onPickup != null)
          Align(
            alignment: Alignment.centerRight,
            child: TextButton(onPressed: onPickup, child: Text(t.checkoutSwitchToPickup)),
          ),
      ]),
    );
  }
}

/// "Bonus bilan to'lash": balans, ishlatiladigan summa (serverdan) va foiz chegarasi.
class _BonusCard extends StatelessWidget {
  const _BonusCard({
    required this.balance,
    required this.used,
    required this.maxPercent,
    required this.value,
    required this.onChanged,
  });

  final double balance;

  /// Hisobda ishlatilgan bonus (null — o'chiq yoki hisob hali kelmagan).
  final double? used;
  final double? maxPercent;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final pct = maxPercent;
    final usedNow = used;
    final sub = [
      t.checkoutBonusBalance(formatMoney(context, balance)),
      if (usedNow != null && usedNow > 0) t.checkoutBonusWillUse(formatMoney(context, usedNow)),
      if (pct != null && pct > 0 && pct < 100) t.checkoutBonusMaxPercent(pct.toStringAsFixed(0)),
    ];
    return Material(
      color: AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: value ? AppColors.primary : AppColors.border, width: value ? 2 : 1),
      ),
      child: SwitchListTile(
        value: value,
        onChanged: onChanged,
        activeThumbColor: AppColors.primary,
        secondary: const Icon(Icons.stars_outlined, color: AppColors.primary),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Text(t.checkoutBonusTitle, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
        subtitle: Text(sub.join(' · '), style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
      ),
    );
  }
}

/// UUID v4 ko'rinishidagi tasodifiy ID (paketsiz, `Random.secure`).
String _newRequestId() {
  final r = Random.secure();
  final b = List<int>.generate(16, (_) => r.nextInt(256));
  b[6] = (b[6] & 0x0f) | 0x40;
  b[8] = (b[8] & 0x3f) | 0x80;
  final h = b.map((e) => e.toRadixString(16).padLeft(2, '0')).join();
  return '${h.substring(0, 8)}-${h.substring(8, 12)}-${h.substring(12, 16)}-${h.substring(16, 20)}-${h.substring(20)}';
}
