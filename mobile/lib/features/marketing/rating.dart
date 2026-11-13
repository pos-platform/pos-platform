import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../orders/data/order_models.dart';
import '../orders/data/orders_repository.dart';
import 'data/marketing_repository.dart';

const _positiveTags = ['fast', 'fresh', 'polite', 'accurate'];
const _negativeTags = ['late', 'damaged', 'missing', 'wrong_item'];

String rateTagLabel(AppLocalizations t, String tag) => switch (tag) {
  'fast' => t.rateTagFast,
  'fresh' => t.rateTagFresh,
  'polite' => t.rateTagPolite,
  'accurate' => t.rateTagAccurate,
  'late' => t.rateTagLate,
  'damaged' => t.rateTagDamaged,
  'missing' => t.rateTagMissing,
  'wrong_item' => t.rateTagWrongItem,
  _ => tag,
};

/// Buyurtma ochilganda baho so'rash oynasi bir marta taklif qilinadi (ko'rsatilganlar qurilmada saqlanadi).
Future<void> maybePromptRating(BuildContext context, WidgetRef ref, OrderDetail order) async {
  if (!order.canRate) return;
  const key = 'rating.prompted';
  try {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(key) ?? const [];
    if (list.contains(order.id)) return;
    await prefs.setStringList(key, [...list.length > 100 ? list.sublist(list.length - 100) : list, order.id]);
  } catch (_) {
    return;
  }
  if (context.mounted) await showRatingSheet(context, ref, order);
}

/// Baho oynasi: yulduzlar, teglar, izoh → `POST /api/shop/orders/{id}/rate`.
Future<void> showRatingSheet(BuildContext context, WidgetRef ref, OrderDetail order, {int initial = 0}) async {
  final ok = await showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => _RatingSheet(orderId: order.id, initial: initial),
  );
  if (ok == true) {
    await ref.read(orderDetailProvider(order.id).notifier).refreshSilently();
    ref.invalidate(ordersListProvider);
  }
}

class _RatingSheet extends ConsumerStatefulWidget {
  const _RatingSheet({required this.orderId, required this.initial});

  final String orderId;
  final int initial;

  @override
  ConsumerState<_RatingSheet> createState() => _RatingSheetState();
}

class _RatingSheetState extends ConsumerState<_RatingSheet> {
  late int _rating = widget.initial;
  final _tags = <String>{};
  final _comment = TextEditingController();
  bool _sending = false;
  String? _error;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_rating == 0 || _sending) return;
    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      final allowed = _rating >= 4 ? _positiveTags : _negativeTags;
      await ref
          .read(marketingRepositoryProvider)
          .rateOrder(
            widget.orderId,
            rating: _rating,
            comment: _comment.text,
            tags: [
              for (final t in _tags)
                if (allowed.contains(t)) t,
            ],
          );
      if (!mounted) return;
      ScaffoldMessenger.maybeOf(context)
          ?.showSnackBar(SnackBar(content: Text(AppLocalizations.of(context).rateThanks)));
      Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        setState(() {
          _sending = false;
          _error = ApiException.from(e).message;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tags = _rating == 0 ? const <String>[] : (_rating >= 4 ? _positiveTags : _negativeTags);
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                t.rateTitle,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 4),
              Text(
                t.rateSubtitle,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 12),
              StarsInput(value: _rating, size: 40, onChanged: (v) => setState(() => _rating = v)),
              if (tags.isNotEmpty) ...[
                const SizedBox(height: 14),
                Wrap(
                  alignment: WrapAlignment.center,
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final tag in tags)
                      FilterChip(
                        label: Text(rateTagLabel(t, tag)),
                        selected: _tags.contains(tag),
                        showCheckmark: false,
                        selectedColor: AppColors.primarySoft,
                        backgroundColor: AppColors.surface,
                        side: BorderSide(color: _tags.contains(tag) ? AppColors.primary : AppColors.border),
                        labelStyle: TextStyle(
                          fontWeight: FontWeight.w600,
                          color: _tags.contains(tag) ? AppColors.primary : AppColors.text,
                        ),
                        shape: const StadiumBorder(),
                        onSelected: (v) => setState(() => v ? _tags.add(tag) : _tags.remove(tag)),
                      ),
                  ],
                ),
              ],
              const SizedBox(height: 14),
              TextField(
                controller: _comment,
                maxLength: 500,
                minLines: 2,
                maxLines: 4,
                decoration: InputDecoration(hintText: t.rateCommentHint),
              ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(_error!, style: const TextStyle(color: AppColors.danger)),
                ),
              FilledButton(
                onPressed: _rating == 0 || _sending ? null : _submit,
                child: _sending
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white),
                      )
                    : Text(t.rateSubmit),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Yulduzlar (1–5). [onChanged] null — faqat ko'rsatish.
class StarsInput extends StatelessWidget {
  const StarsInput({super.key, required this.value, this.onChanged, this.size = 32});

  final int value;
  final ValueChanged<int>? onChanged;
  final double size;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      mainAxisSize: MainAxisSize.min,
      children: [
        for (var i = 1; i <= 5; i++)
          Semantics(
            button: onChanged != null,
            selected: i <= value,
            label: t.rateStars(i),
            child: InkResponse(
              radius: size * 0.7,
              onTap: onChanged == null ? null : () => onChanged!(i),
              child: Padding(
                padding: EdgeInsets.all(size * 0.08),
                child: Icon(
                  i <= value ? Icons.star_rounded : Icons.star_outline_rounded,
                  size: size,
                  color: i <= value ? const Color(0xFFF5A524) : AppColors.iconMuted,
                ),
              ),
            ),
          ),
      ],
    );
  }
}

/// Buyurtma sahifasidagi baho kartasi: baholangan bo'lsa — natija, `canRate` bo'lsa — yulduzlar (bosilsa oyna).
class OrderRatingCard extends ConsumerWidget {
  const OrderRatingCard({super.key, required this.order});

  final OrderDetail order;

  static bool visibleFor(OrderDetail o) => o.rating != null || o.canRate;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final rated = order.rating != null;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusCard),
        border: Border.all(color: AppColors.borderSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            rated ? t.rateYourRating : t.rateTitle,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Center(
            child: StarsInput(
              value: order.rating ?? 0,
              size: 34,
              onChanged: rated ? null : (v) => showRatingSheet(context, ref, order, initial: v),
            ),
          ),
          if (rated && order.ratingTags.isNotEmpty) ...[
            const SizedBox(height: 8),
            Wrap(
              alignment: WrapAlignment.center,
              spacing: 6,
              runSpacing: 6,
              children: [
                for (final tag in order.ratingTags)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(color: AppColors.chipSurface, borderRadius: BorderRadius.circular(999)),
                    child: Text(
                      rateTagLabel(t, tag),
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                    ),
                  ),
              ],
            ),
          ],
          if (rated && order.ratingComment != null) ...[
            const SizedBox(height: 8),
            Text(
              '“${order.ratingComment!}”',
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 14, color: AppColors.textMuted),
            ),
          ],
        ],
      ),
    );
  }
}
