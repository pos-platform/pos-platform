import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../checkout/checkout_options.dart';
import '../cart_controller.dart';

double _d(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
double? _dn(Object? v) => v == null ? null : _d(v);

enum FulfillmentType { delivery, pickup }

enum PaymentMethod { cash, card, transfer }

enum OutOfStockPolicy { callMe, replaceSimilar, remove }

/// Online to'lov provayderi (yig'ilgandan keyin ilovada to'lanadi). Server nomi: `Payme`, `Click`.
enum OnlineProvider { payme, click }

/// Server enum nomi (`Delivery`, `CallMe` ...).
String apiEnum(Enum e) => e.name[0].toUpperCase() + e.name.substring(1);

class QuoteLine {
  const QuoteLine({
    required this.productId,
    required this.name,
    this.imageUrl,
    required this.unit,
    required this.isWeighed,
    required this.quantity,
    required this.unitPrice,
    this.oldPrice,
    required this.lineTotal,
    required this.available,
    this.problem,
  });

  final String productId;
  final String name;
  final String? imageUrl;
  final String unit;
  final bool isWeighed;
  final double quantity;
  final double unitPrice;
  final double? oldPrice;
  final double lineTotal;
  final double available;

  /// Null — muammo yo'q. `not_found | out_of_stock | not_enough | invalid_quantity`.
  final String? problem;

  factory QuoteLine.fromJson(Map<String, dynamic> j) => QuoteLine(
        productId: j['productId'] as String,
        name: j['name'] as String? ?? '',
        imageUrl: j['imageUrl'] as String?,
        unit: j['unit'] as String? ?? 'Piece',
        isWeighed: j['isWeighed'] == true,
        quantity: _d(j['quantity']),
        unitPrice: _d(j['unitPrice']),
        oldPrice: _dn(j['oldPrice']),
        lineTotal: _d(j['lineTotal']),
        available: _d(j['available']),
        problem: j['problem'] as String?,
      );
}

/// Savatcha hisobi — narx va summa serverda hisoblanadi.
class OrderQuote {
  const OrderQuote({
    required this.lines,
    required this.itemsTotal,
    required this.deliveryFee,
    required this.totalAmount,
    required this.minOrderAmount,
    required this.minOrderShortfall,
    this.freeDeliveryRemaining,
    this.freeDeliveryFrom,
    required this.canOrder,
    this.message,
    this.promoDiscount = 0,
    this.promoMessage,
    this.bonusAvailable = 0,
    this.bonusUsed = 0,
    this.deliveryAvailable = true,
    this.deliveryMessage,
    this.distanceKm,
  });

  final List<QuoteLine> lines;
  final double itemsTotal;
  final double deliveryFee;
  final double totalAmount;
  final double minOrderAmount;
  final double minOrderShortfall;
  final double? freeDeliveryRemaining;
  final double? freeDeliveryFrom;
  final bool canOrder;
  final String? message;

  /// Promokod chegirmasi. Kod yaroqsiz bo'lsa — 0 va sababi [promoMessage] da.
  final double promoDiscount;
  final String? promoMessage;

  /// Mijozning shu do'kondagi bonusi va hisobda ishlatilgani (1 ball = 1 so'm).
  final double bonusAvailable;
  final double bonusUsed;

  /// Manzil yetkazish hududida emas bo'lsa — false (buyurtma berib bo'lmaydi), sabab [deliveryMessage] da.
  /// true bo'lsa ham [deliveryMessage] ogohlantirish bo'lishi mumkin (masalan, manzil koordinatasi yo'q).
  final bool deliveryAvailable;
  final String? deliveryMessage;
  final double? distanceKm;

  QuoteLine? line(String productId) {
    for (final l in lines) {
      if (l.productId == productId) return l;
    }
    return null;
  }

  factory OrderQuote.fromJson(Map<String, dynamic> j) => OrderQuote(
        lines: (j['lines'] as List? ?? const []).map((e) => QuoteLine.fromJson(e as Map<String, dynamic>)).toList(),
        itemsTotal: _d(j['itemsTotal']),
        deliveryFee: _d(j['deliveryFee']),
        totalAmount: _d(j['totalAmount']),
        minOrderAmount: _d(j['minOrderAmount']),
        minOrderShortfall: _d(j['minOrderShortfall']),
        freeDeliveryRemaining: _dn(j['freeDeliveryRemaining']),
        freeDeliveryFrom: _dn(j['freeDeliveryFrom']),
        canOrder: j['canOrder'] == true,
        message: j['message'] as String?,
        promoDiscount: _d(j['promoDiscount']),
        promoMessage: _s(j['promoMessage']),
        bonusAvailable: _d(j['bonusAvailable']),
        bonusUsed: _d(j['bonusUsed']),
        deliveryAvailable: j['deliveryAvailable'] != false,
        deliveryMessage: _s(j['deliveryMessage']),
        distanceKm: _dn(j['distanceKm']),
      );
}

String? _s(Object? v) => v is String && v.trim().isNotEmpty ? v : null;

class CreateOrderRequest {
  const CreateOrderRequest({
    required this.branchId,
    required this.fulfillmentType,
    required this.items,
    this.addressId,
    required this.paymentMethod,
    this.scheduledForUtc,
    required this.contactless,
    this.comment,
    required this.outOfStockPolicy,
    this.clientRequestId,
    this.promoCode,
    this.useBonus = false,
    this.onlinePaymentProvider,
  });

  final String branchId;
  final FulfillmentType fulfillmentType;
  final List<CartLine> items;
  final String? addressId;
  final PaymentMethod paymentMethod;

  /// Null — imkon qadar tez.
  final DateTime? scheduledForUtc;
  final bool contactless;
  final String? comment;
  final OutOfStockPolicy outOfStockPolicy;

  /// Takroriy yuborishdan himoya: bir xil ID — server mavjud buyurtmani qaytaradi.
  final String? clientRequestId;

  final String? promoCode;
  final bool useBonus;

  /// Payme/Click tanlansa — `paymentMethod` Transfer bo'lishi kerak (karta raqami o'rniga to'lov tugmasi).
  final OnlineProvider? onlinePaymentProvider;

  Map<String, dynamic> toJson() => {
        'branchId': branchId,
        'fulfillmentType': apiEnum(fulfillmentType),
        'items': _items(items),
        'addressId': fulfillmentType == FulfillmentType.delivery ? addressId : null,
        'paymentMethod': apiEnum(paymentMethod),
        'scheduledFor': scheduledForUtc?.toUtc().toIso8601String(),
        'contactless': contactless,
        'comment': (comment?.trim().isEmpty ?? true) ? null : comment!.trim(),
        'outOfStockPolicy': apiEnum(outOfStockPolicy),
        'clientRequestId': clientRequestId,
        'promoCode': promoCode,
        'useBonus': useBonus,
        if (onlinePaymentProvider != null) 'onlinePaymentProvider': apiEnum(onlinePaymentProvider!),
      };
}

List<Map<String, dynamic>> _items(Iterable<CartLine> lines) =>
    [for (final l in lines) {'productId': l.productId, 'quantity': l.quantity}];

class OrderApi {
  OrderApi(this._dio);

  final Dio _dio;

  Future<OrderQuote> quote(
    String branchId,
    FulfillmentType type,
    Iterable<CartLine> lines, {
    CheckoutOptions options = const CheckoutOptions(),
    CancelToken? cancelToken,
  }) =>
      apiCall(() async {
        final r = await _dio.post<Map<String, dynamic>>(
          '/api/shop/orders/quote',
          data: {
            'branchId': branchId,
            'fulfillmentType': apiEnum(type),
            'items': _items(lines),
            'promoCode': options.promoCode,
            'useBonus': options.useBonus,
            'addressId': type == FulfillmentType.delivery ? options.addressId : null,
          },
          cancelToken: cancelToken,
        );
        return OrderQuote.fromJson(r.data!);
      });

  /// Buyurtma yaratadi. Qaytaradi: buyurtma ID.
  Future<String> create(CreateOrderRequest request) => apiCall(() async {
        final r = await _dio.post<Map<String, dynamic>>('/api/shop/orders', data: request.toJson());
        return r.data!['id'] as String;
      });
}

final orderApiProvider = Provider<OrderApi>((ref) => OrderApi(ref.watch(dioProvider)));

/// Savatcha hisobi: savatcha yoki tanlovlar (promokod, bonus, manzil) o'zgarganda 400 ms kutib serverdan qayta so'raladi (debounce).
/// Yuklanayotganda oldingi qiymat `AsyncValue.value` da qoladi.
final cartQuoteProvider = FutureProvider.autoDispose.family<OrderQuote?, FulfillmentType>((ref, type) async {
  final cart = ref.watch(cartControllerProvider);
  final options = ref.watch(checkoutOptionsProvider);
  final branchId = cart.branchId;
  if (cart.isEmpty || branchId == null) return null;

  final cancel = CancelToken();
  var disposed = false;
  ref.onDispose(() {
    disposed = true;
    cancel.cancel();
  });
  await Future<void>.delayed(const Duration(milliseconds: 400));
  if (disposed) return null; // natija e'tiborsiz qoldiriladi — yangi hisob boshlangan
  return ref.read(orderApiProvider).quote(branchId, type, cart.lines.values, options: options, cancelToken: cancel);
}, retry: (count, _) => count < 2 ? const Duration(seconds: 2) : null);
