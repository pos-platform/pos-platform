import 'package:material_ui/material_ui.dart';

import '../../core/theme/app_colors.dart';

/// Saqlangan sessiya o'qilguncha ko'rinadi (bir zum).
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(child: CircularProgressIndicator(color: AppColors.primary)),
    );
  }
}
