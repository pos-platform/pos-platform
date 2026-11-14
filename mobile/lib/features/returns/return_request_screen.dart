import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../catalog/widgets/common.dart';
import '../orders/data/order_models.dart';
import '../orders/data/orders_repository.dart';
import '../orders/widgets/order_format.dart';
import 'data/return_models.dart';
import 'data/returns_repository.dart';
import 'widgets/return_format.dart';

/// "Qaytarish" (kelishuv 10.5): tovar va miqdor, sabab, izoh, 3 tagacha rasm, pulni qaytarish usuli.
/// Marshrut: `/orders/:id/return`. Faqat `canReturn` bo'lgan (Delivered) buyurtma uchun.
class ReturnRequestScreen extends ConsumerWidget {
  const ReturnRequestScreen({super.key, required this.orderId});

  final String orderId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final async = ref.watch(orderDetailProvider(orderId));
    return switch (async) {
      AsyncValue(value: final o?) => _ReturnForm(order: o),
      AsyncError(:final error) => Scaffold(
          appBar: AppBar(title: Text(t.returnRequestTitle)),
          body: ErrorRetry(
            message: ApiException.from(error).message,
            onRetry: () => ref.invalidate(orderDetailProvider(orderId)),
          ),
        ),
      _ => Scaffold(
          appBar: AppBar(title: Text(t.returnRequestTitle)),
          body: const Center(child: CircularProgressIndicator()),
        ),
    };
  }
}

class _ReturnForm extends ConsumerStatefulWidget {
  const _ReturnForm({required this.order});

  final OrderDetail order;

  @override
  ConsumerState<_ReturnForm> createState() => _ReturnFormState();
}

class _ReturnFormState extends ConsumerState<_ReturnForm> {
  static const _maxPhotos = 3;
  static const _maxBytes = 5 * 1024 * 1024;

  /// Qator ID → qaytariladigan miqdor.
  final _qty = <String, double>{};
  ReturnReason? _reason;
  RefundMethod _refund = RefundMethod.cash;
  final _comment = TextEditingController();
  final _photos = <ReturnPhoto>[];
  bool _submitting = false;

  List<OrderItem> get _items => widget.order.items.where((i) => i.returnableQuantity > 0 && i.id.isNotEmpty).toList();

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  void _snack(String text) =>
      ScaffoldMessenger.of(context)..hideCurrentSnackBar()..showSnackBar(SnackBar(content: Text(text)));

  /// Tarozi tovarida (kasr miqdor) qadam 0.1, donada 1.
  static double _step(OrderItem it) => it.returnableQuantity == it.returnableQuantity.roundToDouble() ? 1 : 0.1;

  void _change(OrderItem it, double delta) {
    final next = ((_qty[it.id] ?? 0) + delta).clamp(0, it.returnableQuantity).toDouble();
    setState(() => next <= 0.0001 ? _qty.remove(it.id) : _qty[it.id] = double.parse(next.toStringAsFixed(3)));
  }

  double get _total {
    var s = 0.0;
    for (final it in _items) {
      s += (_qty[it.id] ?? 0) * it.unitPrice;
    }
    return s;
  }

  Future<void> _addPhoto() async {
    final t = AppLocalizations.of(context);
    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          ListTile(
            leading: Icon(Icons.photo_camera_outlined, color: AppColors.primary),
            title: Text(t.ordersProofCamera),
            onTap: () => Navigator.pop(ctx, ImageSource.camera),
          ),
          ListTile(
            leading: Icon(Icons.photo_library_outlined, color: AppColors.primary),
            title: Text(t.ordersProofGallery),
            onTap: () => Navigator.pop(ctx, ImageSource.gallery),
          ),
          const SizedBox(height: 8),
        ]),
      ),
    );
    if (source == null) return;
    // O'tkazma chekidagi kabi: siqiladi (80%, 1600 px gacha).
    final XFile? file;
    try {
      file = await ImagePicker().pickImage(source: source, maxWidth: 1600, imageQuality: 80);
    } catch (_) {
      if (mounted) _snack(t.ordersProofPickFailed);
      return;
    }
    if (file == null) return;
    final bytes = await file.readAsBytes();
    if (bytes.length > _maxBytes) {
      if (mounted) _snack(t.ordersFileTooBig);
      return;
    }
    final name = file.name.contains('.') ? file.name : '${file.name}.jpg';
    if (mounted) setState(() => _photos.add(ReturnPhoto(bytes: bytes, fileName: name)));
  }

  Future<void> _submit() async {
    final t = AppLocalizations.of(context);
    if (_qty.isEmpty) return _snack(t.returnSelectItems);
    if (_reason == null) return _snack(t.returnSelectReason);
    setState(() => _submitting = true);
    try {
      final created = await ref.read(returnsRepositoryProvider).create(
            widget.order.id,
            items: [for (final e in _qty.entries) ReturnRequestItem(orderItemId: e.key, quantity: e.value)],
            reason: _reason!,
            refundMethod: _refund,
            comment: _comment.text,
            photos: _photos,
          );
      if (!mounted) return;
      ref.invalidate(orderDetailProvider(widget.order.id));
      ref.invalidate(returnsListProvider);
      _snack(t.returnSent);
      if (created != null && created.id.isNotEmpty) {
        context.pushReplacement('/profile/returns/${created.id}');
      } else {
        context.pop();
      }
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => _submitting = false);
        _snack(e.message);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final items = _items;
    final title = Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800);

    Widget chip(String label, bool selected, VoidCallback onTap) => ChoiceChip(
          label: Text(label),
          selected: selected,
          showCheckmark: false,
          selectedColor: AppColors.primarySoft,
          backgroundColor: AppColors.surface,
          side: BorderSide(color: selected ? AppColors.primary : AppColors.border),
          labelStyle: TextStyle(fontWeight: FontWeight.w600, color: selected ? AppColors.primary : AppColors.text),
          shape: const StadiumBorder(),
          onSelected: (_) => onTap(),
        );

    return Scaffold(
      appBar: AppBar(title: Text(t.returnRequestTitle)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
        children: [
          Text(t.ordersOrderNumber('${widget.order.number}'),
              style: const TextStyle(color: AppColors.textSecondary, fontWeight: FontWeight.w600)),
          const SizedBox(height: 16),
          Text(t.returnChooseItems, style: title),
          const SizedBox(height: 8),
          if (items.isEmpty)
            Text(t.returnNothingToReturn, style: const TextStyle(color: AppColors.textSecondary))
          else
            for (final it in items)
              Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding: const EdgeInsets.fromLTRB(4, 6, 10, 6),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: _qty.containsKey(it.id) ? AppColors.primary : AppColors.borderSoft),
                ),
                child: Row(children: [
                  Checkbox(
                    value: _qty.containsKey(it.id),
                    activeColor: AppColors.primary,
                    onChanged: (v) => setState(() => v == true ? _qty[it.id] = it.returnableQuantity : _qty.remove(it.id)),
                  ),
                  OrderThumb(url: it.imageUrl, size: 40),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(it.productName, maxLines: 2, overflow: TextOverflow.ellipsis,
                          style: const TextStyle(fontWeight: FontWeight.w600)),
                      Text('${OrderFormat.quantity(it.returnableQuantity)} × ${OrderFormat.money(t, it.unitPrice)}',
                          style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    ]),
                  ),
                  if (_qty.containsKey(it.id))
                    QtyStepper(
                      label: OrderFormat.quantity(_qty[it.id]!),
                      height: 36,
                      buttonWidth: 32,
                      onMinus: () => _change(it, -_step(it)),
                      onPlus: (_qty[it.id] ?? 0) >= it.returnableQuantity ? null : () => _change(it, _step(it)),
                    ),
                ]),
              ),
          const SizedBox(height: 16),
          Text(t.returnReason, style: title),
          const SizedBox(height: 8),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (final r in ReturnReason.values)
              chip(returnReasonLabel(t, r), _reason == r, () => setState(() => _reason = r)),
          ]),
          const SizedBox(height: 16),
          Text(t.returnComment, style: title),
          const SizedBox(height: 8),
          TextField(
            controller: _comment,
            maxLength: 1000,
            minLines: 2,
            maxLines: 4,
            decoration: InputDecoration(hintText: t.returnCommentHint, counterText: ''),
          ),
          const SizedBox(height: 16),
          Text(t.returnPhotos, style: title),
          const SizedBox(height: 8),
          Wrap(spacing: 10, runSpacing: 10, children: [
            for (final (i, p) in _photos.indexed)
              Stack(clipBehavior: Clip.none, children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(12),
                  child: Image.memory(Uint8List.fromList(p.bytes), width: 84, height: 84, fit: BoxFit.cover),
                ),
                Positioned(
                  top: -8,
                  right: -8,
                  child: IconButton.filledTonal(
                    visualDensity: VisualDensity.compact,
                    tooltip: t.returnRemovePhoto,
                    onPressed: () => setState(() => _photos.removeAt(i)),
                    icon: const Icon(Icons.close, size: 16),
                  ),
                ),
              ]),
            if (_photos.length < _maxPhotos)
              InkWell(
                borderRadius: BorderRadius.circular(12),
                onTap: _addPhoto,
                child: Container(
                  width: 84,
                  height: 84,
                  decoration: BoxDecoration(
                    color: AppColors.chipSurface,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                    Icon(Icons.add_a_photo_outlined, color: AppColors.primary),
                    const SizedBox(height: 4),
                    Text(t.returnAddPhoto, style: const TextStyle(fontSize: 11, color: AppColors.textSecondary)),
                  ]),
                ),
              ),
          ]),
          const SizedBox(height: 16),
          Text(t.returnRefundMethod, style: title),
          const SizedBox(height: 8),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (final m in RefundMethod.values)
              chip(refundMethodLabel(t, m), _refund == m, () => setState(() => _refund = m)),
          ]),
          const SizedBox(height: 12),
          Text(t.returnCashbackNote, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
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
              Row(children: [
                Expanded(child: Text(t.returnAmount, style: const TextStyle(fontWeight: FontWeight.w700))),
                Text(OrderFormat.money(t, _total), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
              ]),
              const SizedBox(height: 8),
              FilledButton(
                onPressed: _submitting || items.isEmpty ? null : _submit,
                child: _submitting
                    ? const SizedBox(
                        width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
                    : Text(t.returnSubmit),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
