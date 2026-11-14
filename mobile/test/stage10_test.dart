import 'package:flutter_test/flutter_test.dart';
import 'package:poscloud_market/core/error_reporter.dart';
import 'package:poscloud_market/features/catalog/data/catalog_models.dart';
import 'package:poscloud_market/features/courier/data/courier_models.dart';
import 'package:poscloud_market/features/orders/data/order_models.dart';
import 'package:poscloud_market/features/returns/data/return_models.dart';
import 'package:poscloud_market/features/returns/return_detail_screen.dart';

/// Bosqich 10 modellari: tolerant parsing (yangi maydonlar ixtiyoriy).
void main() {
  group('10.1 oldindan buyurtma', () {
    test('ShopStore: acceptWhenClosed va nextOpenAt (UTC → mahalliy)', () {
      final s = ShopStore.fromJson({
        'id': 'b1',
        'isOpenNow': false,
        'isAcceptingOrders': true,
        'acceptWhenClosed': true,
        'nextOpenAt': '2026-11-15T04:00:00Z',
      });
      expect(s.acceptsPreorder, isTrue);
      final local = DateTime.utc(2026, 11, 15, 4).toLocal();
      expect(s.nextOpenTime, formatHhMm(local));
    });

    test('ShopStore: eski server — maydonlar yo\'q', () {
      final s = ShopStore.fromJson({'id': 'b1', 'isOpenNow': false, 'isAcceptingOrders': true});
      expect(s.acceptWhenClosed, isFalse);
      expect(s.acceptsPreorder, isFalse);
      expect(s.nextOpenTime, isNull);
    });

    test('OrderDetail: isPreorder, scheduledFor, canReturn', () {
      final o = OrderDetail.fromJson({
        'id': 'o1',
        'isPreorder': true,
        'scheduledFor': '2026-11-15T04:00:00',
        'canReturn': true,
        'items': [
          {'id': 'i1', 'quantity': 3, 'pickedQuantity': 2, 'returnedQuantity': 1},
          {'id': 'i2', 'quantity': 1, 'isRemoved': true},
          {'id': 'i3', 'quantity': 5, 'returnableQuantity': 4},
        ],
      });
      expect(o.isPreorder, isTrue);
      expect(o.canReturn, isTrue);
      expect(o.scheduledFor, DateTime.utc(2026, 11, 15, 4).toLocal());
      expect(o.items[0].returnableQuantity, 1);
      expect(o.items[1].returnableQuantity, 0);
      expect(o.items[2].returnableQuantity, 4);
    });
  });

  group('10.5 qaytarish', () {
    test('ShopReturn: to\'liq javob', () {
      final r = ShopReturn.fromJson({
        'id': 'r1',
        'number': 12,
        'status': 'PickupAssigned',
        'method': 'CourierPickup',
        'reason': 'WrongItem',
        'refundMethod': 'Bonus',
        'photoUrls': ['/uploads/a.jpg', ''],
        'refundAmount': 15000.5,
        'createdAt': '2026-11-14T10:00:00Z',
        'items': [
          {'onlineOrderItemId': 'i1', 'productName': 'Non', 'quantity': 2, 'unitPrice': 4000},
        ],
      });
      expect(r.number, '12');
      expect(r.status, ReturnStatus.pickupAssigned);
      expect(r.method, ReturnMethod.courierPickup);
      expect(r.reason, ReturnReason.wrongItem);
      expect(r.refundMethod, RefundMethod.bonus);
      expect(r.photoUrls, ['/uploads/a.jpg']);
      expect(r.items.single.orderItemId, 'i1');
      expect(r.itemsTotal, 8000);
      expect(r.canCancel, isFalse);
    });

    test('ShopReturn: noma\'lum enum va bo\'sh maydonlar', () {
      final r = ShopReturn.fromJson({'id': 'r2', 'status': 'Weird', 'reason': null});
      expect(r.status, ReturnStatus.requested);
      expect(r.reason, ReturnReason.other);
      expect(r.method, isNull);
      expect(r.canCancel, isTrue);
    });

    test('Ro\'yxat: massiv yoki { items }', () {
      expect(ShopReturn.listFromJson([{'id': 'a'}]).length, 1);
      expect(ShopReturn.listFromJson({'items': [{'id': 'a'}, {'id': 'b'}], 'total': 2}).length, 2);
      expect(ShopReturn.listFromJson(null), isEmpty);
    });

    test('Timeline: do\'konga olib borishda kuryer qadamlari yo\'q', () {
      final r = ShopReturn.fromJson({'id': 'r', 'status': 'Received', 'method': 'BringToStore'});
      final steps = returnTimeline(r);
      expect(steps.map((s) => s.status), [
        ReturnStatus.requested,
        ReturnStatus.approved,
        ReturnStatus.received,
        ReturnStatus.refunded,
      ]);
      expect(steps.where((s) => s.done).length, 3);
    });
  });

  group('Kuryer', () {
    test('return_pickup vazifasi va rad etilgan summa', () {
      final t = CourierOrder.fromJson({
        'kind': 'return_pickup',
        'id': 'ret1',
        'number': 'R-5',
        'status': 'PickupAssigned',
        'address': 'Chilonzor 1',
        'items': [
          {'productName': 'Sut', 'quantity': 1, 'unitPrice': 9000},
        ],
      });
      expect(t.isReturnPickup, isTrue);
      expect(t.isReturnPending, isTrue);
      expect(t.addressLine, 'Chilonzor 1');

      final o = CourierOrder.fromJson({
        'id': 'o1',
        'status': 'OnTheWay',
        'totalAmount': 50000,
        'items': [
          {'id': 'i1', 'productName': 'Non', 'quantity': 2, 'unitPrice': 5000, 'lineTotal': 10000},
        ],
      });
      expect(o.kind, CourierTaskKind.delivery);
      expect(o.items.single.id, 'i1');
      const rej = [CourierRejectedItem(itemId: 'i1', quantity: 1, reason: 'Damaged', unitPrice: 5000)];
      expect(courierAmountToCollect(o, rej), 45000);
      expect(rej.first.toJson(), {'itemId': 'i1', 'quantity': 1.0, 'reason': 'Damaged'});
    });

    test('Ready (qabul qilingan) buyurtma — do\'kondan olish kutilmoqda', () {
      final o = CourierOrder.fromJson({'id': 'o', 'status': 'Ready'});
      expect(o.isAwaitingPickup, isTrue);
      expect(o.isActive, isFalse);
    });

    test('CourierOffer: parsing va muddati', () {
      final now = DateTime.utc(2026, 11, 14, 10).toLocal();
      final o = CourierOffer.fromJson({
        'offerId': 'f1',
        'orderId': 'o1',
        'number': 1023,
        'distanceKm': 2.4,
        'totalAmount': 85000,
        'paymentMethod': 'Cash',
        'expiresAt': '2026-11-14T10:02:00Z',
      });
      expect(o.number, '1023');
      expect(o.isCash, isTrue);
      expect(o.remaining(now), const Duration(minutes: 2));
      expect(o.isExpired(now.add(const Duration(minutes: 3))), isTrue);
    });
  });

  group('10.4 xatoliklar', () {
    setUp(ErrorReporter.resetForTest);

    test('Dublikat 1 daqiqada bir marta, maxfiy ma\'lumot yashiriladi', () {
      final t0 = DateTime(2026, 11, 14, 10);
      final p = ErrorReporter.buildPayload(StateError('Bearer abc.def token=xyz +998 90 123 45 67'), null, now: t0);
      expect(p, isNotNull);
      expect(p!['source'], 'Mobile');
      expect(p['level'], 'Error');
      final msg = p['message'] as String;
      expect(msg.contains('abc.def'), isFalse);
      expect(msg.contains('xyz'), isFalse);
      expect(msg.contains('123 45 67'), isFalse);
      expect(ErrorReporter.buildPayload(StateError('Bearer abc.def token=xyz +998 90 123 45 67'), null,
          now: t0.add(const Duration(seconds: 30))), isNull);
      expect(ErrorReporter.buildPayload(StateError('Bearer abc.def token=xyz +998 90 123 45 67'), null,
          now: t0.add(const Duration(seconds: 61))), isNotNull);
    });

    test('Uzun matn qisqartiriladi', () {
      expect(ErrorReporter.truncate('a' * 2000, 1000).length, 1000);
    });
  });
}
