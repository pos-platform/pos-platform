import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../catalog/widgets/common.dart';
import 'data/marketing_models.dart';
import 'data/marketing_repository.dart';

/// Qidiruv tarixi: oxirgi 10 ta so'rov, faqat qurilmada.
class SearchHistory extends Notifier<List<String>> {
  static const _key = 'search.history';
  static const max = 10;

  @override
  List<String> build() {
    _load();
    return const [];
  }

  Future<void> _load() async {
    try {
      final list = (await SharedPreferences.getInstance()).getStringList(_key);
      if (list != null && state.isEmpty) state = list.take(max).toList();
    } catch (_) {}
  }

  Future<void> add(String query) async {
    final q = query.trim();
    if (q.length < 2) return;
    state = [q, ...state.where((e) => e.toLowerCase() != q.toLowerCase())].take(max).toList();
    await _save();
  }

  Future<void> remove(String query) async {
    state = [...state.where((e) => e != query)];
    await _save();
  }

  Future<void> clear() async {
    state = const [];
    await _save();
  }

  Future<void> _save() async {
    try {
      await (await SharedPreferences.getInstance()).setStringList(_key, state);
    } catch (_) {}
  }
}

final searchHistoryProvider = NotifierProvider<SearchHistory, List<String>>(SearchHistory.new);

/// Yozish paytidagi takliflar (debounce 300 ms) yoki — maydon bo'sh bo'lsa — qidiruv tarixi.
class SearchSuggestionsPanel extends ConsumerStatefulWidget {
  const SearchSuggestionsPanel({
    super.key,
    required this.branchId,
    required this.query,
    required this.onQuery,
    required this.onProduct,
    required this.onCategory,
  });

  final String branchId;
  final String query;

  /// Tarixdagi so'rov bosildi.
  final ValueChanged<String> onQuery;
  final ValueChanged<String> onProduct;
  final void Function(String id, String name) onCategory;

  @override
  ConsumerState<SearchSuggestionsPanel> createState() => _SearchSuggestionsPanelState();
}

class _SearchSuggestionsPanelState extends ConsumerState<SearchSuggestionsPanel> {
  Timer? _debounce;
  CancelToken? _cancel;
  SearchSuggest? _result;
  String _resultFor = '';

  @override
  void initState() {
    super.initState();
    _schedule();
  }

  @override
  void didUpdateWidget(SearchSuggestionsPanel old) {
    super.didUpdateWidget(old);
    if (old.query.trim() != widget.query.trim()) _schedule();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _cancel?.cancel();
    super.dispose();
  }

  void _schedule() {
    _debounce?.cancel();
    final q = widget.query.trim();
    if (q.length < 2) return;
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      _cancel?.cancel();
      final cancel = _cancel = CancelToken();
      try {
        final r = await ref.read(marketingRepositoryProvider).searchSuggest(widget.branchId, q, cancelToken: cancel);
        if (mounted && !cancel.isCancelled) {
          setState(() {
            _result = r;
            _resultFor = q;
          });
        }
      } catch (_) {
        // Takliflar ixtiyoriy — xatoda oddiy qidiruv natijalari qoladi.
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final q = widget.query.trim();
    if (q.isEmpty) {
      final history = ref.watch(searchHistoryProvider);
      if (history.isEmpty) return const SizedBox.shrink();
      return _Panel(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 6, 4, 0),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    t.searchHistory,
                    style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.textSecondary),
                  ),
                ),
                TextButton(
                  onPressed: () => ref.read(searchHistoryProvider.notifier).clear(),
                  child: Text(t.searchHistoryClear),
                ),
              ],
            ),
          ),
          for (final h in history)
            ListTile(
              dense: true,
              leading: const Icon(Icons.history, size: 20, color: AppColors.textSecondary),
              title: Text(h, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600)),
              trailing: IconButton(
                tooltip: t.catClear,
                icon: const Icon(Icons.close, size: 18, color: AppColors.iconMuted),
                onPressed: () => ref.read(searchHistoryProvider.notifier).remove(h),
              ),
              onTap: () => widget.onQuery(h),
            ),
        ],
      );
    }
    final r = _result;
    if (r == null || r.isEmpty || _resultFor.isEmpty) return const SizedBox.shrink();
    return _Panel(
      children: [
        if (r.categories.isNotEmpty)
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final c in r.categories)
                  ActionChip(
                    avatar: const Icon(Icons.category_outlined, size: 16),
                    label: Text(c.name),
                    onPressed: () => widget.onCategory(c.id, c.name),
                  ),
              ],
            ),
          ),
        for (final p in r.products)
          ListTile(
            dense: true,
            leading: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: SizedBox(width: 40, height: 40, child: ProductImage(url: p.imageUrl, iconSize: 18)),
            ),
            title: Text(
              p.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
            ),
            trailing: Text(
              formatMoney(context, p.price),
              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
            ),
            onTap: () => widget.onProduct(p.id),
          ),
      ],
    );
  }
}

class _Panel extends StatelessWidget {
  const _Panel({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(top: 8),
    padding: const EdgeInsets.only(bottom: 6),
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: AppColors.borderSoft),
    ),
    child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: children),
  );
}

/// Shtrix-kod skaneri: birinchi o'qilgan kod bilan yopiladi (natija — kod matni).
class BarcodeScannerScreen extends StatefulWidget {
  const BarcodeScannerScreen({super.key});

  @override
  State<BarcodeScannerScreen> createState() => _BarcodeScannerScreenState();
}

class _BarcodeScannerScreenState extends State<BarcodeScannerScreen> {
  final _controller = MobileScannerController(detectionSpeed: DetectionSpeed.noDuplicates);
  bool _done = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_done) return;
    for (final b in capture.barcodes) {
      final v = b.rawValue?.trim();
      if (v != null && v.isNotEmpty) {
        _done = true;
        Navigator.of(context).pop(v);
        return;
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        title: Text(t.scanTitle),
        actions: [
          IconButton(
            tooltip: t.scanTorch,
            icon: const Icon(Icons.flash_on),
            onPressed: () => _controller.toggleTorch(),
          ),
        ],
      ),
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
            errorBuilder: (_, error) => Center(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Text(
                  t.scanError,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white),
                ),
              ),
            ),
          ),
          Center(
            child: Container(
              width: 260,
              height: 160,
              decoration: BoxDecoration(
                border: Border.all(color: Colors.white, width: 2),
                borderRadius: BorderRadius.circular(16),
              ),
            ),
          ),
          Positioned(
            left: 24,
            right: 24,
            bottom: MediaQuery.paddingOf(context).bottom + 32,
            child: Text(
              t.scanHint,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}
