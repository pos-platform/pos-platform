import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/order_models.dart';
import '../data/orders_repository.dart';
import 'order_format.dart';

/// Kartaga o'tkazma bo'limi: karta raqami (nusxa olish), egasi, summa va chek rasmini yuklash.
/// Karta server tomonidan faqat yakuniy summa ma'lum bo'lgach (Ready'dan) beriladi.
class TransferSection extends ConsumerStatefulWidget {
  const TransferSection({super.key, required this.order});

  final OrderDetail order;

  /// Bo'lim ko'rsatiladimi.
  static bool visibleFor(OrderDetail o) =>
      o.paymentMethod == PaymentMethod.transfer &&
      !o.isOnlinePayment && // Payme/Click tanlangan — karta o'rniga to'lov tugmalari
      !o.status.isFailed &&
      (o.status != OrderStatus.delivered || o.paymentStatus == PaymentStatus.paid);

  @override
  ConsumerState<TransferSection> createState() => _TransferSectionState();
}

class _TransferSectionState extends ConsumerState<TransferSection> {
  static const _maxBytes = 5 * 1024 * 1024;
  bool _uploading = false;

  OrderDetail get o => widget.order;

  void _snack(String text) =>
      ScaffoldMessenger.of(context)..hideCurrentSnackBar()..showSnackBar(SnackBar(content: Text(text)));

  Future<void> _copy(String value) async {
    final t = AppLocalizations.of(context);
    await Clipboard.setData(ClipboardData(text: value.replaceAll(' ', '')));
    if (mounted) _snack(t.ordersCopied);
  }

  Future<void> _upload() async {
    final t = AppLocalizations.of(context);
    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined, color: AppColors.primary),
              title: Text(t.ordersProofCamera),
              onTap: () => Navigator.pop(ctx, ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined, color: AppColors.primary),
              title: Text(t.ordersProofGallery),
              onTap: () => Navigator.pop(ctx, ImageSource.gallery),
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
    if (source == null) return;
    // Rasm siqiladi (sifat 80%, eni 1600 px gacha) — yuklash tezroq, server limiti 5 MB.
    final XFile? file;
    try {
      file = await ImagePicker().pickImage(source: source, maxWidth: 1600, imageQuality: 80);
    } catch (_) {
      // Kamera/galereyaga ruxsat berilmagan yoki qurilmada kamera yo'q.
      if (mounted) _snack(t.ordersProofPickFailed);
      return;
    }
    if (file == null) return;
    final bytes = await file.readAsBytes();
    if (bytes.length > _maxBytes) {
      if (mounted) _snack(t.ordersFileTooBig);
      return;
    }
    // Server kengaytma bo'yicha tekshiradi — nomda kengaytma bo'lmasa .jpg qo'shamiz.
    final name = file.name.contains('.') ? file.name : '${file.name}.jpg';
    setState(() => _uploading = true);
    try {
      await ref.read(orderDetailProvider(o.id).notifier).uploadTransferProof(bytes, name);
      if (mounted) _snack(t.ordersProofUploaded);
    } catch (e) {
      if (mounted) _snack('$e');
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final card = o.transferCardNumber;

    final Widget body;
    if (o.paymentStatus == PaymentStatus.paid) {
      body = _note(Icons.check_circle, t.ordersPaymentConfirmed, AppColors.primary);
    } else if (card == null) {
      body = _note(Icons.schedule, t.ordersTransferWaitReady, AppColors.textSecondary);
    } else {
      final proof = AppConfig.imageUrl(o.transferProofUrl);
      body = Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(t.ordersTransferHint(OrderFormat.money(t, o.totalAmount)),
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
          const SizedBox(height: 12),
          _row(t.ordersTransferCard, _formatCard(card), copy: card),
          if (o.transferCardHolder != null) _row(t.ordersTransferHolder, o.transferCardHolder!),
          _row(t.ordersTransferAmount, OrderFormat.money(t, o.totalAmount),
              copy: o.totalAmount.toStringAsFixed(0)),
          const SizedBox(height: 12),
          if (proof != null) ...[
            Row(
              children: [
                OrderThumb(url: proof, size: 56),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(t.ordersProofUploaded,
                      style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.primary)),
                ),
              ],
            ),
            const SizedBox(height: 12),
          ],
          if (o.canUploadTransferProof)
            SizedBox(
              height: 52,
              child: proof == null
                  ? FilledButton.icon(
                      onPressed: _uploading ? null : _upload,
                      icon: _uploading ? _spinner(Colors.white) : const Icon(Icons.upload),
                      label: Text(t.ordersUploadProof),
                    )
                  : OutlinedButton.icon(
                      onPressed: _uploading ? null : _upload,
                      style: OutlinedButton.styleFrom(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      ),
                      icon: _uploading ? _spinner(AppColors.primary) : const Icon(Icons.refresh),
                      label: Text(t.ordersReuploadProof),
                    ),
            ),
        ],
      );
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(t.ordersTransferTitle, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          body,
        ],
      ),
    );
  }

  Widget _spinner(Color c) =>
      SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: c));

  Widget _note(IconData icon, String text, Color color) => Row(
        children: [
          Icon(icon, size: 20, color: color),
          const SizedBox(width: 8),
          Expanded(child: Text(text, style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: color))),
        ],
      );

  Widget _row(String label, String value, {String? copy}) {
    final t = AppLocalizations.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                Text(value, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, letterSpacing: 0.3)),
              ],
            ),
          ),
          if (copy != null)
            IconButton(
              tooltip: t.ordersCopy,
              onPressed: () => _copy(copy),
              icon: const Icon(Icons.copy_rounded, color: AppColors.primary),
            ),
        ],
      ),
    );
  }

  /// "8600123412341234" → "8600 1234 1234 1234".
  static String _formatCard(String raw) {
    final d = raw.replaceAll(RegExp(r'\s'), '');
    final b = StringBuffer();
    for (var i = 0; i < d.length; i++) {
      if (i > 0 && i % 4 == 0) b.write(' ');
      b.write(d[i]);
    }
    return b.toString();
  }
}
