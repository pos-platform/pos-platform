import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../l10n/app_localizations.dart';
import '../api/api_exception.dart';
import '../theme/app_colors.dart';
import 'store_app.dart';
import 'store_branding.dart';

/// Do'kon rejimi: do'kon o'chirilgan/bloklangan (404) yoki birinchi ochilishda internet yo'q (kesh ham yo'q).
class StoreUnavailableScreen extends ConsumerWidget {
  const StoreUnavailableScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final state = ref.watch(storeAppProvider);
    final error = state.error;
    final unavailable = error == null || error is StoreUnavailableException;
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(children: [
            const Spacer(),
            const StoreLogo(size: 72),
            const SizedBox(height: 24),
            Text(
              unavailable ? t.storeUnavailableTitle : ApiException.from(error).message,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
            ),
            if (unavailable) ...[
              const SizedBox(height: 8),
              Text(t.storeUnavailableText,
                  textAlign: TextAlign.center, style: const TextStyle(fontSize: 15, color: AppColors.textSecondary)),
            ],
            const Spacer(),
            FilledButton(
              onPressed: state.isLoading ? null : () => ref.invalidate(storeAppProvider),
              child: state.isLoading
                  ? const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(strokeWidth: 2.5))
                  : Text(t.retry),
            ),
            const SizedBox(height: 8),
            // Kuryer rejimi do'kon holatidan mustaqil.
            TextButton(onPressed: () => context.go('/courier/login'), child: Text(t.courierEntry)),
          ]),
        ),
      ),
    );
  }
}
