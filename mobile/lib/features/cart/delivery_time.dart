import '../../l10n/app_localizations.dart';
import '../catalog/data/catalog_models.dart';

/// Do'kon sozlamasidagi taxminiy yetkazish vaqti: "60–90 daqiqa", faqat bittasi bo'lsa "90 daqiqagacha".
/// Null — do'kon vaqt ko'rsatmagan yoki yetkazish o'chiq.
String? deliveryTimeText(AppLocalizations t, ShopStore store) {
  if (!store.deliveryEnabled) return null;
  final min = store.deliveryMinMinutes, max = store.deliveryMaxMinutes;
  if (min != null && max != null && max > min) return t.deliveryMinutesRange('$min', '$max');
  final single = max ?? min;
  if (single == null || single <= 0) return null;
  return t.deliveryMinutesUpTo('$single');
}
