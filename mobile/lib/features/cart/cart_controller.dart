import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../catalog/data/catalog_models.dart';

/// Savatchadagi qator: mahsulot nusxasi (ko'rsatish uchun) va miqdor.
/// Narx faqat taxminiy — yakuniy summa serverdan (`/quote`) olinadi.
class CartLine {
  const CartLine({
    required this.productId,
    required this.name,
    this.imageUrl,
    required this.unit,
    required this.isWeighed,
    required this.price,
    required this.quantity,
    this.available,
  });

  final String productId;
  final String name;
  final String? imageUrl;
  final String unit;
  final bool isWeighed;
  final double price;
  final double quantity;

  /// Qo'shilgan paytdagi qoldiq (stepper chegarasi uchun; server baribir tekshiradi).
  final double? available;

  bool get isKg => isWeighed || unit == 'Kilogram';
  double get step => CartState.stepFor(isKg);
  double get lineTotal => price * quantity;

  CartLine copyWith({double? quantity}) => CartLine(
        productId: productId,
        name: name,
        imageUrl: imageUrl,
        unit: unit,
        isWeighed: isWeighed,
        price: price,
        quantity: quantity ?? this.quantity,
        available: available,
      );

  Map<String, dynamic> toJson() => {
        'productId': productId,
        'name': name,
        'imageUrl': imageUrl,
        'unit': unit,
        'isWeighed': isWeighed,
        'price': price,
        'quantity': quantity,
        'available': available,
      };

  static CartLine? tryFromJson(Object? raw) {
    if (raw is! Map) return null;
    final id = raw['productId'], q = raw['quantity'];
    if (id is! String || q is! num || q <= 0) return null;
    return CartLine(
      productId: id,
      name: raw['name'] as String? ?? '',
      imageUrl: raw['imageUrl'] as String?,
      unit: raw['unit'] as String? ?? 'Piece',
      isWeighed: raw['isWeighed'] == true,
      price: (raw['price'] as num?)?.toDouble() ?? 0,
      quantity: q.toDouble(),
      available: (raw['available'] as num?)?.toDouble(),
    );
  }
}

class CartState {
  const CartState({this.branchId, this.lines = const {}});

  /// Savatcha qaysi do'konga tegishli. Bitta buyurtma — bitta do'kon.
  final String? branchId;

  /// productId → qator (qo'shilish tartibi saqlanadi).
  final Map<String, CartLine> lines;

  bool get isEmpty => lines.isEmpty;
  int get count => lines.length;
  double get estimatedTotal => lines.values.fold(0, (s, l) => s + l.lineTotal);
  double quantityOf(String productId) => lines[productId]?.quantity ?? 0;

  /// Tarozi tovari 0.1 kg qadam bilan, dona — butun son.
  static double stepFor(bool isKg) => isKg ? 0.1 : 1;

  /// Birinchi qo'shishdagi miqdor: tarozi tovari ham 1 kg dan boshlanadi (keyin 0.1 qadam bilan
  /// o'zgartiriladi). Qoldiq 1 kg dan kam bo'lsa — borini (0.1 ga yaxlitlab).
  static double firstQuantity(ShopProduct p) {
    if (!p.isKg) return 1;
    final avail = ((p.available + 1e-9) * 10).floorToDouble() / 10;
    return avail >= 1 ? 1 : (avail >= 0.1 ? round(avail) : 0.1);
  }

  /// Kasrdagi xatolarni yo'qotish (0.1 + 0.2 = 0.30000000000000004).
  static double round(double v) => (v * 1000).roundToDouble() / 1000;
}

/// Savatcha holati. Qurilmada (`shared_preferences`, JSON) saqlanadi va ilova ochilganda tiklanadi.
/// Do'kon almashganda savatcha tozalanadi — buni ekran so'raydi.
class CartController extends Notifier<CartState> {
  static const _storageKey = 'cart.v1';

  /// Foydalanuvchi tiklanishdan oldin savatchani o'zgartirgan bo'lsa — saqlangan nusxa e'tiborsiz qoladi.
  bool _touched = false;

  /// Boshlang'ich holat — alohida nusxa (const emas): Riverpod 3 `listenSelf` build natijasi uchun ham chaqiriladi,
  /// shu nusxa bilan uni ajratamiz. Aks holda ilova ochilishi bilan savatcha "o'zgardi" deb hisoblanib,
  /// saqlangani tiklanmasdi va qurilmadagi nusxa o'chib ketardi.
  // ignore: prefer_const_constructors
  final CartState _initial = CartState();

  @override
  CartState build() {
    listenSelf((_, next) {
      if (identical(next, _initial)) return;
      _touched = true;
      _persist(next);
    });
    _restore();
    return _initial;
  }

  Future<void> _restore() async {
    try {
      final raw = (await SharedPreferences.getInstance()).getString(_storageKey);
      if (raw == null || _touched) return;
      final j = jsonDecode(raw);
      if (j is! Map) return;
      final branchId = j['branchId'];
      final lines = <String, CartLine>{
        for (final l in (j['lines'] as List? ?? const []).map(CartLine.tryFromJson).nonNulls) l.productId: l,
      };
      if (branchId is! String || lines.isEmpty) return;
      state = CartState(branchId: branchId, lines: lines);
    } catch (_) {
      // Xotira ishlamasa (test, buzilgan JSON) — bo'sh savatcha bilan davom etiladi.
    }
  }

  /// Yozuvlar ketma-ket bajariladi — tez o'zgarishlarda eski holat yangisining ustiga yozilmaydi.
  Future<void> _writes = Future.value();

  void _persist(CartState s) => _writes = _writes.then((_) => _write(s));

  Future<void> _write(CartState s) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      if (s.isEmpty || s.branchId == null) {
        await prefs.remove(_storageKey);
      } else {
        await prefs.setString(
          _storageKey,
          jsonEncode({'branchId': s.branchId, 'lines': [for (final l in s.lines.values) l.toJson()]}),
        );
      }
    } catch (_) {}
  }

  /// "Qayta buyurtma": qatorlarni savatchaga qo'shadi. Shu do'kon savatchasi bo'lsa — birlashtiriladi
  /// (bir xil mahsulotda buyurtmadagi miqdor olinadi), boshqa do'konniki bo'lsa — almashtiriladi.
  void addLines(String branchId, Iterable<CartLine> lines) {
    final map = Map<String, CartLine>.of(state.branchId == branchId ? state.lines : const <String, CartLine>{});
    for (final l in lines) {
      if (l.quantity > 0) map[l.productId] = l;
    }
    state = map.isEmpty ? const CartState() : CartState(branchId: branchId, lines: map);
  }

  /// Savatchada boshqa do'kon mahsuloti bormi (do'kon almashtirishdan oldin so'rash uchun).
  bool conflictsWith(String branchId) => !state.isEmpty && state.branchId != branchId;

  /// Miqdorni o'rnatadi (0 yoki kam — olib tashlanadi). Boshqa do'kon savatchasi bo'lsa — almashtiriladi.
  void setQuantity(String branchId, ShopProduct product, double quantity) {
    final base = state.branchId == branchId ? state.lines : const <String, CartLine>{};
    final lines = Map<String, CartLine>.of(base);
    final q = CartState.round(quantity);
    if (q <= 0) {
      lines.remove(product.id);
    } else {
      lines[product.id] = CartLine(
        productId: product.id,
        name: product.name,
        imageUrl: product.imageUrl,
        unit: product.unit,
        isWeighed: product.isWeighed,
        price: product.price,
        quantity: q,
        available: product.available,
      );
    }
    state = CartState(branchId: lines.isEmpty ? null : branchId, lines: lines);
  }

  void increment(String branchId, ShopProduct product) {
    final step = CartState.stepFor(product.isKg);
    final current = state.branchId == branchId ? state.quantityOf(product.id) : 0.0;
    setQuantity(branchId, product, current > 0 ? current + step : CartState.firstQuantity(product));
  }

  void decrement(String branchId, ShopProduct product) {
    final step = CartState.stepFor(product.isKg);
    setQuantity(branchId, product, state.quantityOf(product.id) - step);
  }

  /// Savatcha ekranida: mavjud qator miqdorini o'zgartirish.
  void changeLine(String productId, double delta) {
    final line = state.lines[productId];
    if (line == null) return;
    final lines = Map<String, CartLine>.of(state.lines);
    final q = CartState.round(line.quantity + delta);
    if (q <= 0) {
      lines.remove(productId);
    } else {
      lines[productId] = line.copyWith(quantity: q);
    }
    state = CartState(branchId: lines.isEmpty ? null : state.branchId, lines: lines);
  }

  /// Server aytgan miqdorga tushirish (masalan, "faqat 3 ta mavjud").
  void setLineQuantity(String productId, double quantity) {
    final line = state.lines[productId];
    if (line == null) return;
    changeLine(productId, quantity - line.quantity);
  }

  void remove(String productId) {
    final line = state.lines[productId];
    if (line != null) changeLine(productId, -line.quantity);
  }

  void clear() => state = const CartState();
}

final cartControllerProvider = NotifierProvider<CartController, CartState>(CartController.new);
