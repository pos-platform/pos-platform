import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import 'data/addresses_repository.dart';
import 'widgets/profile_widgets.dart';

/// Manzillarim (dizayn: Addresses.dc.html).
class AddressesScreen extends ConsumerWidget {
  const AddressesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final addresses = ref.watch(addressesProvider);

    return Scaffold(
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ProfilePageHeader(title: t.addressesTitle),
            Expanded(
              child: switch (addresses) {
                AsyncData(:final value) => RefreshIndicator(
                    onRefresh: () => ref.refresh(addressesProvider.future),
                    child: ListView(
                      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                      children: [
                        if (value.isEmpty)
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 24),
                            child: Text(t.addressEmpty,
                                textAlign: TextAlign.center,
                                style: const TextStyle(fontSize: 15, color: AppColors.textSecondary)),
                          ),
                        for (final a in value) ...[
                          AddressCard(address: a, onEdit: () => context.push('/profile/addresses/${a.id}')),
                          const SizedBox(height: 12),
                        ],
                        const SizedBox(height: 4),
                        _AddButton(onTap: () => context.push('/profile/addresses/new')),
                        const SizedBox(height: 16),
                        _InfoNote(text: t.addressOutOfZoneNote),
                      ],
                    ),
                  ),
                AsyncError(:final error) => ProfileErrorView(
                    error: error,
                    onRetry: () => ref.invalidate(addressesProvider),
                  ),
                _ => const Center(child: CircularProgressIndicator()),
              },
            ),
          ],
        ),
      ),
    );
  }
}

/// Manzil nomini ko'rsatish: saqlangan "Uy"/"Ish" joriy tilda.
String addressLabelText(AppLocalizations t, String label) => switch (label) {
      addressLabelHome => t.addressLabelHome,
      addressLabelWork => t.addressLabelWork,
      _ => label,
    };

/// Serverga yoziladigan standart nomlar (til bo'yicha o'zgarmaydi).
const addressLabelHome = 'Uy';
const addressLabelWork = 'Ish';

/// Podyezd · qavat · xonadon · domofon.
String addressDetails(AppLocalizations t, ShopAddress a) {
  bool has(String? s) => s != null && s.trim().isNotEmpty;
  return [
    if (has(a.entrance)) t.addressDetailEntrance(a.entrance!.trim()),
    if (has(a.floor)) t.addressDetailFloor(a.floor!.trim()),
    if (has(a.apartment)) t.addressDetailApartment(a.apartment!.trim()),
    if (has(a.intercom)) t.addressDetailIntercom(a.intercom!.trim()),
    if (has(a.landmark)) a.landmark!.trim(),
  ].join(' · ');
}

/// Manzil kartasi (asosiy manzil — yashil ramka va "Asosiy" belgisi).
class AddressCard extends StatelessWidget {
  const AddressCard({super.key, required this.address, this.onEdit});

  final ShopAddress address;
  final VoidCallback? onEdit;

  IconData get _icon => switch (address.label) {
        addressLabelHome => Icons.home_outlined,
        addressLabelWork => Icons.work_outline,
        _ => Icons.location_on_outlined,
      };

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final main = address.isDefault;
    final details = addressDetails(t, address);

    return Material(
      color: AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: main
            ? BorderSide(color: AppColors.primary, width: 2)
            : const BorderSide(color: AppColors.borderSoft),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: onEdit,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 6, 14),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: main ? AppColors.primarySoft : AppColors.chipSurface,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(_icon, size: 20, color: main ? AppColors.primary : AppColors.text),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Flexible(
                          child: Text(
                            addressLabelText(t, address.label),
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
                          ),
                        ),
                        if (main) ...[
                          const SizedBox(width: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(
                              color: AppColors.primarySoft,
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Text(t.addressDefaultBadge,
                                style: TextStyle(
                                    fontSize: 11, fontWeight: FontWeight.w800, color: AppColors.primary)),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: 3),
                    Text(address.addressLine, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                    if (details.isNotEmpty) ...[
                      const SizedBox(height: 3),
                      Text(details, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    ],
                  ],
                ),
              ),
              if (onEdit != null)
                IconButton(
                  tooltip: t.addressEditTitle,
                  onPressed: onEdit,
                  icon: const Icon(Icons.edit_outlined, size: 18, color: AppColors.textSecondary),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AddButton extends StatelessWidget {
  const _AddButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    // Dizaynda punktir ramka; Flutter'da oddiy ramka bilan (punktir uchun paket kerak emas).
    return OutlinedButton.icon(
      style: OutlinedButton.styleFrom(
        foregroundColor: AppColors.primary,
        minimumSize: const Size.fromHeight(56),
        side: BorderSide(color: AppColors.primary, width: 1.5),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
      ),
      onPressed: onTap,
      icon: const Icon(Icons.add, size: 20),
      label: Text(t.addressAdd),
    );
  }
}

class _InfoNote extends StatelessWidget {
  const _InfoNote({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(color: const Color(0xFFEAF1F8), borderRadius: BorderRadius.circular(14)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.info_outline, size: 20, color: Color(0xFF1F4E79)),
          const SizedBox(width: 10),
          Expanded(
            child: Text(text, style: const TextStyle(fontSize: 13, height: 1.45, color: Color(0xFF1F3550))),
          ),
        ],
      ),
    );
  }
}
