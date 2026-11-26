import 'dart:async';

import 'package:flutter/scheduler.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/theme/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/geocoding_repository.dart';

/// OpenStreetMap plitkalari (foydalanish qoidasi: User-Agent va atributsiya shart).
const osmTileUrl = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const osmUserAgentPackage = 'uz.xojamarket.app';

/// Standart markaz — Nurota (do'kon joylashgan shahar).
const defaultMapCenter = LatLng(40.5636, 65.6886);

TileLayer osmTileLayer() => TileLayer(urlTemplate: osmTileUrl, userAgentPackageName: osmUserAgentPackage);

/// OSM talab qiladigan "© OpenStreetMap contributors" yozuvi.
Widget osmAttribution() => RichAttributionWidget(
      showFlutterMapAttribution: false,
      attributions: [
        TextSourceAttribution(
          'OpenStreetMap contributors',
          onTap: () => launchUrl(Uri.parse('https://www.openstreetmap.org/copyright')),
        ),
      ],
    );

/// Xaritadan tanlov natijasi: nuqta va (aniqlangan bo'lsa) taklif qilinadigan manzil matni.
class MapPickResult {
  const MapPickResult({required this.point, this.address});

  final LatLng point;

  /// Nominatim aniqlagan qisqa manzil (null — aniqlanmadi).
  final String? address;
}

/// To'liq ekranli xarita: markazdagi pin ostidagi nuqta tanlanadi. Natija: `Navigator.pop(MapPickResult)`.
/// Router'ga qo'shilmagan — `MaterialPageRoute` bilan ochiladi ([pickLocationOnMap]).
///
/// - "Joylashuvimni aniqlash" (geolocator; ruxsat berilmasa — xabar, sozlamalarga o'tish).
/// - Pin ostidagi manzil: xarita to'xtagach (≥1.2 s) Nominatim reverse; kichik siljishda qayta so'ralmaydi.
/// - Qidiruv: faqat "qidirish" bosilganda (Nominatim avtoto'ldirishni taqiqlaydi), O'zbekiston bo'yicha 5 ta natija.
class MapPickerScreen extends ConsumerStatefulWidget {
  const MapPickerScreen({super.key, this.initial});

  final LatLng? initial;

  @override
  ConsumerState<MapPickerScreen> createState() => _MapPickerScreenState();
}

class _MapPickerScreenState extends ConsumerState<MapPickerScreen> {
  static const _debounce = Duration(milliseconds: 1200);

  /// Shundan kichik siljishda manzil qayta so'ralmaydi (metr).
  static const _sameSpotMeters = 15.0;

  final _map = MapController();
  final _search = TextEditingController();
  final _distance = const Distance();

  late LatLng _center = widget.initial ?? defaultMapCenter;
  Timer? _timer;
  int _requestSeq = 0;

  String? _address;
  LatLng? _addressPoint;
  bool _resolving = false;
  bool _locating = false;
  bool _searching = false;

  /// Null — natijalar ko'rsatilmaydi; bo'sh — "topilmadi".
  List<GeoPlace>? _results;

  @override
  void initState() {
    super.initState();
    if (widget.initial != null) _scheduleReverse();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _requestSeq++;
    _search.dispose();
    _map.dispose();
    super.dispose();
  }

  String get _lang => Localizations.localeOf(context).languageCode == 'ru' ? 'ru' : 'uz';

  void _onMoved(MapCamera camera) {
    _center = camera.center;
    _scheduleReverse();
  }

  /// Xarita harakatdan to'xtagach manzilni aniqlash (har siljishda taymer qayta boshlanadi).
  void _scheduleReverse() {
    _timer?.cancel();
    final known = _addressPoint;
    if (known != null && _distance(known, _center) < _sameSpotMeters) {
      _setResolving(false);
      return;
    }
    _setResolving(true);
    _timer = Timer(_debounce, _reverse);
  }

  /// Xarita hodisasi build/layout vaqtida kelishi mumkin — u holda qayta chizish kadrdan keyin.
  void _setResolving(bool value) {
    if (_resolving == value) return;
    _resolving = value;
    if (SchedulerBinding.instance.schedulerPhase == SchedulerPhase.idle ||
        SchedulerBinding.instance.schedulerPhase == SchedulerPhase.postFrameCallbacks) {
      setState(() {});
    } else {
      SchedulerBinding.instance.addPostFrameCallback((_) {
        if (mounted) setState(() {});
      });
    }
  }

  Future<void> _reverse() async {
    final seq = ++_requestSeq;
    final point = _center;
    final place = await ref.read(geocodingRepositoryProvider).reverse(point.latitude, point.longitude, language: _lang);
    if (!mounted || seq != _requestSeq) return; // xarita yana surildi — eski natija kerak emas
    setState(() {
      _resolving = false;
      _address = place?.label;
      _addressPoint = point;
    });
  }

  void _moveTo(LatLng p, {double zoom = 17}) {
    _map.move(p, zoom);
    _center = p;
  }

  void _snack(String text, {SnackBarAction? action}) => ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(text), action: action));

  Future<void> _locate() async {
    final t = AppLocalizations.of(context);
    setState(() => _locating = true);
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        _snack(t.mapLocationOff,
            action: SnackBarAction(label: t.mapOpenSettings, onPressed: Geolocator.openLocationSettings));
        return;
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.deniedForever) {
        _snack(t.mapLocationDeniedForever,
            action: SnackBarAction(label: t.mapOpenSettings, onPressed: Geolocator.openAppSettings));
        return;
      }
      if (permission == LocationPermission.denied) {
        _snack(t.mapLocationDenied);
        return;
      }
      Position? pos;
      try {
        pos = await Geolocator.getCurrentPosition(
          locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, timeLimit: Duration(seconds: 10)),
        );
      } catch (_) {
        pos = await Geolocator.getLastKnownPosition();
      }
      if (!mounted) return;
      if (pos == null) {
        _snack(t.mapLocationFailed);
        return;
      }
      _moveTo(LatLng(pos.latitude, pos.longitude));
      _scheduleReverse();
    } catch (_) {
      if (mounted) _snack(t.mapLocationFailed);
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  Future<void> _runSearch() async {
    final q = _search.text.trim();
    if (q.length < 3 || _searching) return;
    FocusScope.of(context).unfocus();
    setState(() => _searching = true);
    final results = await ref.read(geocodingRepositoryProvider).search(q, language: _lang);
    if (!mounted) return;
    setState(() {
      _searching = false;
      _results = results;
    });
  }

  void _selectPlace(GeoPlace p) {
    final point = LatLng(p.lat, p.lng);
    _timer?.cancel();
    _requestSeq++;
    setState(() {
      _results = null;
      _address = p.label;
      _addressPoint = point;
      _resolving = false;
    });
    _moveTo(point);
  }

  void _confirm() {
    final known = _addressPoint;
    final fresh = known != null && _distance(known, _center) < _sameSpotMeters * 2;
    Navigator.of(context).pop(MapPickResult(point: _center, address: fresh ? _address : null));
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(t.mapPickerTitle)),
      body: Stack(
        children: [
          FlutterMap(
            mapController: _map,
            options: MapOptions(
              initialCenter: _center,
              initialZoom: widget.initial == null ? 12 : 17,
              minZoom: 4,
              maxZoom: 19,
              interactionOptions: const InteractionOptions(flags: InteractiveFlag.all & ~InteractiveFlag.rotate),
              onPositionChanged: (camera, _) => _onMoved(camera),
              onTap: (_, _) => FocusScope.of(context).unfocus(),
            ),
            children: [osmTileLayer(), osmAttribution()],
          ),
          // Pin uchi aynan xarita markaziga tushishi uchun ikona yuqoriga suriladi.
          IgnorePointer(
            child: Center(
              child: Padding(
                padding: EdgeInsets.only(bottom: 44),
                child: Icon(Icons.location_on, size: 48, color: AppColors.primary),
              ),
            ),
          ),
          Positioned(left: 16, right: 16, top: 12, child: _searchBox(t)),
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: [
                Align(
                  alignment: Alignment.centerRight,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(0, 0, 16, 28),
                    child: FloatingActionButton(
                      heroTag: null,
                      tooltip: t.mapMyLocation,
                      backgroundColor: AppColors.surface,
                      foregroundColor: AppColors.primary,
                      onPressed: _locating ? null : _locate,
                      child: _locating
                          ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5))
                          : const Icon(Icons.my_location),
                    ),
                  ),
                ),
                _bottomPanel(t),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _searchBox(AppLocalizations t) {
    final results = _results;
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Material(
          elevation: 3,
          shadowColor: Colors.black26,
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(14),
          child: TextField(
            controller: _search,
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => _runSearch(),
            onChanged: (_) {
              if (_results != null) setState(() => _results = null);
            },
            decoration: InputDecoration(
              hintText: t.mapSearchHint,
              filled: true,
              fillColor: AppColors.surface,
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
              enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _searching
                  ? const Padding(
                      padding: EdgeInsets.all(14),
                      child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
                    )
                  : IconButton(
                      tooltip: t.mapSearchHint,
                      icon: const Icon(Icons.arrow_forward),
                      onPressed: _runSearch,
                    ),
            ),
          ),
        ),
        if (results != null) ...[
          const SizedBox(height: 6),
          Material(
            elevation: 3,
            shadowColor: Colors.black26,
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(14),
            clipBehavior: Clip.antiAlias,
            child: results.isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(16),
                    child: Text(t.mapSearchEmpty, style: const TextStyle(color: AppColors.textSecondary)),
                  )
                : Column(mainAxisSize: MainAxisSize.min, children: [
                    for (final p in results)
                      ListTile(
                        dense: true,
                        leading: Icon(Icons.place_outlined, color: AppColors.primary),
                        title: Text(p.label, style: const TextStyle(fontWeight: FontWeight.w700)),
                        subtitle: p.fullName == null
                            ? null
                            : Text(p.fullName!, maxLines: 1, overflow: TextOverflow.ellipsis),
                        onTap: () => _selectPlace(p),
                      ),
                  ]),
          ),
        ],
      ],
    );
  }

  Widget _bottomPanel(AppLocalizations t) {
    final String text;
    final Color color;
    if (_resolving) {
      text = t.mapAddressLoading;
      color = AppColors.textSecondary;
    } else if (_address != null) {
      text = _address!;
      color = AppColors.text;
    } else {
      text = _addressPoint == null ? t.mapMoveHint : t.mapAddressUnknown;
      color = AppColors.textSecondary;
    }
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.vertical(top: Radius.circular(22)),
        boxShadow: [BoxShadow(color: Colors.black12, blurRadius: 12, offset: Offset(0, -2))],
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(children: [
                Icon(Icons.location_on_outlined, color: AppColors.primary),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(text,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700, color: color)),
                ),
                if (_resolving)
                  const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
              ]),
              const SizedBox(height: 12),
              FilledButton(onPressed: _confirm, child: Text(t.mapPickerConfirm)),
            ],
          ),
        ),
      ),
    );
  }
}

Future<MapPickResult?> pickLocationOnMap(BuildContext context, {LatLng? initial}) =>
    Navigator.of(context).push<MapPickResult>(
      MaterialPageRoute(builder: (_) => MapPickerScreen(initial: initial)),
    );

/// Kichik statik ko'rinish (manzil formasida tanlangan nuqta).
class MapPreview extends StatelessWidget {
  const MapPreview({super.key, required this.point, this.height = 140});

  final LatLng point;
  final double height;

  @override
  Widget build(BuildContext context) => ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: SizedBox(
          height: height,
          child: IgnorePointer(
            child: FlutterMap(
              key: ValueKey(point),
              options: MapOptions(
                initialCenter: point,
                initialZoom: 16,
                interactionOptions: const InteractionOptions(flags: InteractiveFlag.none),
              ),
              children: [
                osmTileLayer(),
                MarkerLayer(markers: [
                  Marker(
                    point: point,
                    width: 36,
                    height: 36,
                    alignment: Alignment.topCenter,
                    child: Icon(Icons.location_on, size: 36, color: AppColors.primary),
                  ),
                ]),
              ],
            ),
          ),
        ),
      );
}
