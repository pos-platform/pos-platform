import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:latlong2/latlong.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import 'addresses_screen.dart';
import 'data/addresses_repository.dart';
import 'widgets/map_picker.dart';
import 'widgets/profile_widgets.dart';

/// Manzil qo'shish / tahrirlash (dizayn: AddressEdit.dc.html).
///
/// `addressId == null` — yangi manzil. Saqlangach `pop(ShopAddress)` qiladi — checkout ham shu ekranni
/// push qilib, natijani olishi mumkin.
///
/// Xaritada belgilash: [MapPickerScreen] (OpenStreetMap) — koordinata saqlanadi; aniqlangan manzil matni
/// bo'sh maydonga yoziladi, matn bo'lsa almashtirish so'raladi.
class AddressEditScreen extends ConsumerWidget {
  const AddressEditScreen({super.key, this.addressId});

  final String? addressId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final id = addressId;
    final title = id == null ? t.addressNewTitle : t.addressEditTitle;

    Widget body;
    if (id == null) {
      body = const _AddressForm(address: null);
    } else {
      body = switch (ref.watch(addressesProvider)) {
        AsyncData(:final value) => switch (value.where((a) => a.id == id).firstOrNull) {
            final a? => _AddressForm(key: ValueKey(a.id), address: a),
            null => Center(
                child: Text(t.addressNotFound, style: const TextStyle(color: AppColors.textSecondary)),
              ),
          },
        AsyncError(:final error) => ProfileErrorView(
            error: error,
            onRetry: () => ref.invalidate(addressesProvider),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      };
    }

    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        bottom: false,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ProfilePageHeader(title: title),
            Expanded(child: body),
          ],
        ),
      ),
    );
  }
}

enum _LabelKind { home, work, other }

class _AddressForm extends ConsumerStatefulWidget {
  const _AddressForm({super.key, required this.address});

  final ShopAddress? address;

  @override
  ConsumerState<_AddressForm> createState() => _AddressFormState();
}

class _AddressFormState extends ConsumerState<_AddressForm> {
  final _formKey = GlobalKey<FormState>();
  late final ShopAddress? _a = widget.address;
  late final _line = TextEditingController(text: _a?.addressLine ?? '');
  late final _entrance = TextEditingController(text: _a?.entrance ?? '');
  late final _floor = TextEditingController(text: _a?.floor ?? '');
  late final _apartment = TextEditingController(text: _a?.apartment ?? '');
  late final _intercom = TextEditingController(text: _a?.intercom ?? '');
  late final _landmark = TextEditingController(text: _a?.landmark ?? '');
  late final _comment = TextEditingController(text: _a?.comment ?? '');
  late final _customLabel = TextEditingController(text: _kindOf(_a?.label) == _LabelKind.other ? _a!.label : '');
  late _LabelKind _kind = _kindOf(_a?.label);
  late bool _isDefault = _a?.isDefault ?? false;
  late LatLng? _point =
      _a?.latitude != null && _a?.longitude != null ? LatLng(_a!.latitude!, _a.longitude!) : null;
  bool _busy = false;

  static _LabelKind _kindOf(String? label) => switch (label) {
        null || addressLabelHome => _LabelKind.home,
        addressLabelWork => _LabelKind.work,
        _ => _LabelKind.other,
      };

  @override
  void dispose() {
    for (final c in [_line, _entrance, _floor, _apartment, _intercom, _landmark, _comment, _customLabel]) {
      c.dispose();
    }
    super.dispose();
  }

  String? _v(TextEditingController c) {
    final s = c.text.trim();
    return s.isEmpty ? null : s;
  }

  String get _label => switch (_kind) {
        _LabelKind.home => addressLabelHome,
        _LabelKind.work => addressLabelWork,
        _LabelKind.other => _customLabel.text.trim(),
      };

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _busy = true);
    final request = SaveAddressRequest(
      label: _label,
      addressLine: _line.text.trim(),
      entrance: _v(_entrance),
      floor: _v(_floor),
      apartment: _v(_apartment),
      intercom: _v(_intercom),
      landmark: _v(_landmark),
      comment: _v(_comment),
      latitude: _point?.latitude,
      longitude: _point?.longitude,
      isDefault: _isDefault,
    );
    try {
      final repo = ref.read(addressesRepositoryProvider);
      final saved = _a == null ? await repo.create(request) : await repo.update(_a.id, request);
      ref.invalidate(addressesProvider);
      if (mounted) Navigator.of(context).pop(saved);
    } on ApiException catch (e) {
      if (mounted) showProfileSnack(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pickOnMap() async {
    final picked = await pickLocationOnMap(context, initial: _point);
    if (picked == null || !mounted) return;
    setState(() => _point = picked.point);
    final suggested = picked.address?.trim();
    if (suggested == null || suggested.isEmpty) return;
    final current = _line.text.trim();
    if (current == suggested) return;
    if (current.isEmpty) {
      _line.text = suggested;
      return;
    }
    // Mijoz yozgan matn bor — so'ramasdan almashtirilmaydi.
    final t = AppLocalizations.of(context);
    final replace = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(t.addressReplaceTitle),
        content: Text(suggested),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.addressKeep)),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text(t.addressReplace)),
        ],
      ),
    );
    if (replace == true && mounted) _line.text = suggested;
  }

  /// "Xaritada belgilash" tugmasi + tanlangan nuqta (kichik xarita va koordinata).
  Widget _mapSection(AppLocalizations t) {
    final p = _point;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (p != null) ...[
          MapPreview(point: p),
          const SizedBox(height: 6),
          Text(
            t.addressMapCoords('${p.latitude.toStringAsFixed(5)}, ${p.longitude.toStringAsFixed(5)}'),
            style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 6),
        ],
        OutlinedButton.icon(
          onPressed: _busy ? null : _pickOnMap,
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(48),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
          icon: const Icon(Icons.map_outlined, size: 20),
          label: Text(p == null ? t.addressMapPick : t.addressMapChange),
        ),
      ],
    );
  }

  Future<void> _delete() async {
    final a = _a;
    if (a == null) return;
    final t = AppLocalizations.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        content: Text(t.addressDeleteConfirm),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(t.profileCancel)),
          TextButton(
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(t.profileDeleteButton),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    setState(() => _busy = true);
    try {
      await ref.read(addressesRepositoryProvider).delete(a.id);
      ref.invalidate(addressesProvider);
      if (mounted) Navigator.of(context).pop();
    } on ApiException catch (e) {
      if (mounted) showProfileSnack(context, e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    const fill = AppColors.background;

    Widget field(String label, TextEditingController c, int max, {String? hint, TextInputType? type}) => LabeledField(
          label: label,
          controller: c,
          hint: hint,
          maxLength: max,
          keyboardType: type,
          fillColor: fill,
        );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
              children: [
                LabeledField(
                  label: t.addressLine,
                  controller: _line,
                  hint: t.addressLineHint,
                  maxLength: 300,
                  fillColor: fill,
                  textCapitalization: TextCapitalization.sentences,
                  validator: (v) => (v ?? '').trim().isEmpty ? t.addressLineRequired : null,
                ),
                const SizedBox(height: 10),
                _mapSection(t),
                const SizedBox(height: 14),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: field(t.addressEntrance, _entrance, 20, type: TextInputType.number)),
                    const SizedBox(width: 10),
                    Expanded(child: field(t.addressFloor, _floor, 20, type: TextInputType.number)),
                    const SizedBox(width: 10),
                    Expanded(child: field(t.addressApartment, _apartment, 20)),
                  ],
                ),
                const SizedBox(height: 14),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: field(t.addressIntercom, _intercom, 50)),
                    const SizedBox(width: 10),
                    Expanded(child: field(t.addressLandmark, _landmark, 200, hint: t.addressLandmarkHint)),
                  ],
                ),
                const SizedBox(height: 14),
                field(t.addressComment, _comment, 500, hint: t.addressCommentHint),
                const SizedBox(height: 16),
                Text(t.addressLabel,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted)),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _LabelChip(
                      text: t.addressLabelHome,
                      selected: _kind == _LabelKind.home,
                      onTap: () => setState(() => _kind = _LabelKind.home),
                    ),
                    _LabelChip(
                      text: t.addressLabelWork,
                      selected: _kind == _LabelKind.work,
                      onTap: () => setState(() => _kind = _LabelKind.work),
                    ),
                    _LabelChip(
                      text: t.addressLabelOther,
                      selected: _kind == _LabelKind.other,
                      onTap: () => setState(() => _kind = _LabelKind.other),
                    ),
                  ],
                ),
                if (_kind == _LabelKind.other) ...[
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _customLabel,
                    maxLength: 50,
                    textCapitalization: TextCapitalization.sentences,
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                    decoration: profileInputDecoration(hint: t.addressLabelCustomHint, fillColor: fill),
                    validator: (v) => (v ?? '').trim().isEmpty ? t.addressLabelRequired : null,
                  ),
                ],
                const SizedBox(height: 8),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  activeThumbColor: AppColors.primary,
                  title: Text(t.addressMakeDefault, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w700)),
                  value: _isDefault,
                  onChanged: (v) => setState(() => _isDefault = v),
                ),
                if (_a != null) ...[
                  const SizedBox(height: 8),
                  TextButton.icon(
                    style: TextButton.styleFrom(
                      foregroundColor: AppColors.danger,
                      minimumSize: const Size.fromHeight(48),
                    ),
                    onPressed: _busy ? null : _delete,
                    icon: const Icon(Icons.delete_outline, size: 18),
                    label: Text(t.addressDelete),
                  ),
                ],
              ],
            ),
          ),
        ),
        BottomActionBar(label: t.addressSave, busy: _busy, onPressed: _save),
      ],
    );
  }
}

class _LabelChip extends StatelessWidget {
  const _LabelChip({required this.text, required this.selected, required this.onTap});

  final String text;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      child: InkWell(
        borderRadius: BorderRadius.circular(999),
        onTap: onTap,
        child: Container(
          height: 38,
          padding: const EdgeInsets.symmetric(horizontal: 14),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: selected ? AppColors.text : Colors.transparent,
            borderRadius: BorderRadius.circular(999),
            border: selected ? null : Border.all(color: const Color(0xFFD2CFC5)),
          ),
          child: Text(
            text,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w700,
              color: selected ? Colors.white : AppColors.text,
            ),
          ),
        ),
      ),
    );
  }
}
