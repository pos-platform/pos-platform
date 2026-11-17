import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api/api_exception.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_theme.dart';
import '../../l10n/app_localizations.dart';
import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import '../catalog/store_selection.dart';
import '../catalog/widgets/common.dart';
import '../marketing/data/marketing_models.dart';
import '../marketing/data/marketing_repository.dart';
import '../profile/widgets/profile_widgets.dart' show formatPhone;
import 'data/lists_data.dart';

/// Oilaviy ro'yxat (CONTRACT-11 §2): a'zolar, bandlar (belgilash, o'chirish), katalogdan taklif, "Savatga o'tkazish".
/// Birinchi ochilishda ro'yxat bo'lmasa — avtomatik yaratiladi. Ekran ochiq paytda har 15 s yangilanadi.
class FamilyListScreen extends ConsumerStatefulWidget {
  const FamilyListScreen({super.key});

  @override
  ConsumerState<FamilyListScreen> createState() => _FamilyListScreenState();
}

class _FamilyListScreenState extends ConsumerState<FamilyListScreen> {
  ShoppingList? _list;
  String? _error;
  bool _busy = false;
  Timer? _poll;
  Timer? _debounce;
  final _input = TextEditingController();
  final _focus = FocusNode();
  SearchSuggest _suggest = const SearchSuggest();

  ListsRepository get _repo => ref.read(listsRepositoryProvider);

  /// Mahsulot narxi/qoldig'i shu do'kon bo'yicha (savatcha do'koni, bo'lmasa tanlangani).
  String? get _branchId => ref.read(cartControllerProvider).branchId ?? ref.read(storeSelectionProvider).value;

  @override
  void initState() {
    super.initState();
    Future.microtask(_init);
    _poll = Timer.periodic(const Duration(seconds: 15), (_) => _reload(silent: true));
  }

  @override
  void dispose() {
    _poll?.cancel();
    _debounce?.cancel();
    _input.dispose();
    _focus.dispose();
    super.dispose();
  }

  Future<void> _init() async {
    if (!ref.read(authControllerProvider).isSignedIn) return;
    try {
      final lists = await _repo.lists();
      final id = lists.isEmpty ? (await _repo.create()).id : (lists.where((l) => l.isOwner).firstOrNull ?? lists.first).id;
      final list = await _repo.get(id, branchId: _branchId);
      if (mounted) setState(() => _list = list);
    } catch (e) {
      if (mounted) setState(() => _error = ApiException.from(e).message);
    }
  }

  Future<void> _reload({bool silent = false}) async {
    final l = _list;
    if (l == null || (silent && _busy)) return;
    try {
      final fresh = await _repo.get(l.id, branchId: _branchId);
      if (mounted) setState(() => _list = fresh);
    } catch (e) {
      if (!silent && mounted) _snack(ApiException.from(e).message);
    }
  }

  /// Amal + qayta yuklash; xato — snackbar.
  Future<void> _run(Future<void> Function() action) async {
    setState(() => _busy = true);
    try {
      await action();
      await _reload();
    } catch (e) {
      if (mounted) _snack(ApiException.from(e).message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _snack(String text) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  void _onInput(String q) {
    setState(() {});
    _debounce?.cancel();
    final branchId = _branchId;
    if (q.trim().length < 2 || branchId == null) {
      setState(() => _suggest = const SearchSuggest());
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      try {
        final s = await ref.read(marketingRepositoryProvider).searchSuggest(branchId, q.trim());
        if (mounted && _input.text == q) setState(() => _suggest = s);
      } catch (_) {}
    });
  }

  Future<void> _add({String? productId, String? title}) async {
    final l = _list;
    final text = (title ?? _input.text).trim();
    if (l == null || text.isEmpty) return;
    _input.clear();
    setState(() => _suggest = const SearchSuggest());
    await _run(() => _repo.addItem(l.id, title: text, productId: productId));
  }

  Future<void> _toggle(ListItem item) async {
    final l = _list;
    if (l == null) return;
    // Darhol ko'rinadi, server javobi keyin keladi.
    setState(() => _list = l.copyWith(items: [
          for (final i in l.items) i.id == item.id ? i.copyWith(isChecked: !item.isChecked) : i,
        ]));
    await _run(() => _repo.setChecked(l.id, item.id, !item.isChecked));
  }

  List<ListItem> get _toCart => [
        for (final i in _list?.items ?? const <ListItem>[])
          if (!i.isChecked && i.product != null && i.product!.inStock) i,
      ];

  void _moveToCart() {
    final t = AppLocalizations.of(context);
    final branchId = _branchId;
    if (branchId == null) return;
    final n = ref.read(cartControllerProvider.notifier).addMany(branchId, [
      for (final i in _toCart) (i.product!, i.quantity ?? CartState.firstQuantity(i.product!)),
    ]);
    _snack(t.cartAddedCount(n));
  }

  Future<void> _addMember() async {
    final l = _list;
    if (l == null) return;
    final phone = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _AddMemberSheet(),
    );
    if (phone == null || phone.isEmpty) return;
    await _run(() => _repo.addMember(l.id, phone));
  }

  Future<void> _memberActions(ListMember m, {required bool isMe}) async {
    final t = AppLocalizations.of(context);
    final l = _list;
    if (l == null || (!l.isOwner && !isMe)) return;
    final ok = await showModalBottomSheet<bool>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text(m.name ?? formatPhone(m.phone), style: AppTheme.display(17)),
            if (m.name != null)
              Text(formatPhone(m.phone), style: const TextStyle(fontSize: 14, color: AppColors.textSecondary)),
            const SizedBox(height: 16),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.ink),
              onPressed: () => Navigator.pop(ctx, true),
              child: Text(isMe ? t.listLeave : t.listRemoveMember),
            ),
          ]),
        ),
      ),
    );
    if (ok != true) return;
    if (isMe) {
      try {
        await _repo.removeMember(l.id, m.id);
        if (mounted) Navigator.of(context).maybePop();
      } catch (e) {
        if (mounted) _snack(ApiException.from(e).message);
      }
    } else {
      await _run(() => _repo.removeMember(l.id, m.id));
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final signedIn = ref.watch(authControllerProvider.select((a) => a.isSignedIn));
    final myPhone = ref.watch(authControllerProvider.select((a) => a.user?.phone));
    final l = _list;

    Widget body;
    if (!signedIn) {
      body = Center(
        child: Padding(
          padding: const EdgeInsets.all(36),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Text(t.listLoginText,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 14, height: 1.5, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
            const SizedBox(height: 18),
            FilledButton(
              style: FilledButton.styleFrom(minimumSize: const Size(0, 52), padding: const EdgeInsets.symmetric(horizontal: 32)),
              onPressed: () => ref.read(authControllerProvider.notifier).requestSignIn(),
              child: Text(t.login),
            ),
          ]),
        ),
      );
    } else if (l == null) {
      body = _error != null
          ? ErrorRetry(
              message: _error!,
              onRetry: () {
                setState(() => _error = null);
                _init();
              })
          : const Center(child: CircularProgressIndicator());
    } else {
      final checked = l.items.where((i) => i.isChecked).length;
      body = Column(children: [
        Expanded(
          child: RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
              children: [
                _Members(
                  list: l,
                  myPhone: myPhone,
                  onAdd: l.isOwner ? _addMember : null,
                  onMember: (m) => _memberActions(m, isMe: m.phone == myPhone),
                ),
                const SizedBox(height: 14),
                _AddField(controller: _input, focus: _focus, busy: _busy, onChanged: _onInput, onSubmit: () => _add()),
                if (_suggest.products.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Wrap(spacing: 6, runSpacing: 6, children: [
                    for (final p in _suggest.products.take(5))
                      ActionChip(
                        avatar: const Icon(Icons.add, size: 16),
                        label: Text(p.name, overflow: TextOverflow.ellipsis),
                        backgroundColor: AppColors.surface,
                        side: BorderSide.none,
                        shape: const StadiumBorder(),
                        onPressed: () => _add(productId: p.id, title: p.name),
                      ),
                  ]),
                ],
                const SizedBox(height: 14),
                if (l.items.isEmpty)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 48, horizontal: 24),
                    child: Text(t.listEmpty,
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 14, height: 1.5, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
                  )
                else
                  Material(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(AppTheme.radiusCard),
                    clipBehavior: Clip.antiAlias,
                    child: Column(children: [
                      for (final (i, item) in l.items.indexed) ...[
                        if (i > 0) const Divider(height: 1, indent: 16, endIndent: 16, color: AppColors.line),
                        Dismissible(
                          key: ValueKey(item.id),
                          direction: DismissDirection.endToStart,
                          background: Container(
                            color: AppColors.primarySoft,
                            alignment: Alignment.centerRight,
                            padding: const EdgeInsets.only(right: 20),
                            child: Icon(Icons.delete_outline, color: AppColors.primary),
                          ),
                          onDismissed: (_) {
                            setState(() => _list = l.copyWith(items: [...l.items]..removeWhere((x) => x.id == item.id)));
                            _run(() => _repo.deleteItem(l.id, item.id));
                          },
                          child: _ItemRow(item: item, onToggle: () => _toggle(item)),
                        ),
                      ],
                    ]),
                  ),
                if (checked > 0)
                  Align(
                    child: TextButton(
                      style: TextButton.styleFrom(foregroundColor: AppColors.textSecondary),
                      onPressed: _busy ? null : () => _run(() => _repo.clearChecked(l.id)),
                      child: Text(t.listClearChecked),
                    ),
                  ),
              ],
            ),
          ),
        ),
        DecoratedBox(
          decoration: const BoxDecoration(
            color: AppColors.surface,
            border: Border(top: BorderSide(color: AppColors.borderSoft)),
          ),
          child: SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
              child: FilledButton(
                onPressed: _toCart.isEmpty ? null : _moveToCart,
                child: Text(t.listToCart(_toCart.length)),
              ),
            ),
          ),
        ),
      ]);
    }

    return Scaffold(
      appBar: AppBar(title: Text(l?.name ?? t.listTitle)),
      body: body,
    );
  }
}

/// A'zolar qatori: egasi, a'zolar (qo'shilmaganlari xira), "+".
class _Members extends StatelessWidget {
  const _Members({required this.list, required this.myPhone, required this.onAdd, required this.onMember});

  final ShoppingList list;
  final String? myPhone;
  final VoidCallback? onAdd;
  final ValueChanged<ListMember> onMember;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    Widget avatar(String label, {bool dim = false, Color bg = AppColors.ink}) => Opacity(
          opacity: dim ? 0.45 : 1,
          child: Container(
            width: 44,
            height: 44,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(16)),
            child: Text(label, style: AppTheme.display(14, color: Colors.white)),
          ),
        );
    String initial(String? name, String phone) =>
        (name != null && name.trim().isNotEmpty) ? name.trim()[0].toUpperCase() : phone.substring(phone.length - 2);

    return SizedBox(
      height: 70,
      child: ListView(scrollDirection: Axis.horizontal, children: [
        _Person(
          avatar: avatar(initial(list.ownerName, list.ownerPhone), bg: AppColors.primary),
          label: list.ownerName.isEmpty ? t.listOwner : list.ownerName,
        ),
        for (final m in list.members)
          _Person(
            avatar: avatar(initial(m.name, m.phone), dim: !m.joined),
            label: m.joined ? (m.name ?? formatPhone(m.phone)) : t.listPending,
            onTap: () => onMember(m),
          ),
        if (onAdd != null)
          _Person(
            avatar: Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.circular(16)),
              child: const Icon(Icons.person_add_alt_1_outlined, size: 21),
            ),
            label: t.listInvite,
            onTap: onAdd,
          ),
      ]),
    );
  }
}

class _Person extends StatelessWidget {
  const _Person({required this.avatar, required this.label, this.onTap});

  final Widget avatar;
  final String label;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(right: 10),
        child: InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: onTap,
          child: SizedBox(
            width: 62,
            child: Column(children: [
              avatar,
              const SizedBox(height: 4),
              Text(label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
            ]),
          ),
        ),
      );
}

class _AddField extends StatelessWidget {
  const _AddField({
    required this.controller,
    required this.focus,
    required this.busy,
    required this.onChanged,
    required this.onSubmit,
  });

  final TextEditingController controller;
  final FocusNode focus;
  final bool busy;
  final ValueChanged<String> onChanged;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    return TextField(
      controller: controller,
      focusNode: focus,
      maxLength: 120,
      textInputAction: TextInputAction.done,
      onChanged: onChanged,
      onSubmitted: (_) => onSubmit(),
      decoration: InputDecoration(
        hintText: t.listAddHint,
        counterText: '',
        filled: true,
        fillColor: AppColors.surface,
        prefixIcon: const Icon(Icons.edit_note_rounded, color: AppColors.textSecondary),
        suffixIcon: Padding(
          padding: const EdgeInsets.only(right: 6),
          child: IconButton.filled(
            style: IconButton.styleFrom(backgroundColor: AppColors.primary),
            onPressed: busy || controller.text.trim().isEmpty ? null : onSubmit,
            icon: const Icon(Icons.add, color: Colors.white),
          ),
        ),
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  const _ItemRow({required this.item, required this.onToggle});

  final ListItem item;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final p = item.product;
    final sub = [
      if (item.quantity != null) formatQty(context, item.quantity!, p?.isKg ?? false),
      item.isChecked && item.checkedByName != null ? t.listCheckedBy(item.checkedByName!) : t.listAddedBy(item.addedByName),
    ].join(' · ');
    return InkWell(
      onTap: onToggle,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(6, 6, 16, 6),
        child: Row(children: [
          Checkbox(
            value: item.isChecked,
            onChanged: (_) => onToggle(),
            activeColor: AppColors.primary,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
          ),
          if (p != null) ...[
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(color: AppColors.tintFor(p.id), borderRadius: BorderRadius.circular(12)),
              clipBehavior: Clip.antiAlias,
              child: ProductImage(url: p.imageUrl, iconSize: 16, fit: BoxFit.cover),
            ),
            const SizedBox(width: 10),
          ],
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(item.title,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: item.isChecked ? AppColors.faint : AppColors.text,
                    decoration: item.isChecked ? TextDecoration.lineThrough : null,
                  )),
              Text(sub, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.faint)),
            ]),
          ),
          if (p != null && !item.isChecked)
            Text(formatMoney(context, p.price), style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800)),
        ]),
      ),
    );
  }
}

class _AddMemberSheet extends StatefulWidget {
  const _AddMemberSheet();

  @override
  State<_AddMemberSheet> createState() => _AddMemberSheetState();
}

class _AddMemberSheetState extends State<_AddMemberSheet> {
  final _phone = TextEditingController();

  @override
  void dispose() {
    _phone.dispose();
    super.dispose();
  }

  String get _digits => _phone.text.replaceAll(RegExp(r'\D'), '');

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final ok = _digits.length == 9;
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 8, 20, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(t.listAddMember, style: AppTheme.display(18)),
        const SizedBox(height: 6),
        Text(t.listAddMemberHint,
            style: const TextStyle(fontSize: 13, height: 1.45, fontWeight: FontWeight.w500, color: AppColors.textSecondary)),
        const SizedBox(height: 16),
        TextField(
          controller: _phone,
          autofocus: true,
          keyboardType: TextInputType.phone,
          maxLength: 12,
          onChanged: (_) => setState(() {}),
          style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700, letterSpacing: 0.5),
          decoration: const InputDecoration(
            counterText: '',
            prefixText: '+998 ',
            hintText: '90 123 45 67',
          ),
        ),
        const SizedBox(height: 14),
        FilledButton(
          onPressed: ok ? () => Navigator.pop(context, '+998$_digits') : null,
          child: Text(t.listInvite),
        ),
      ]),
    );
  }
}
