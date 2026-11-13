import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/config.dart';
import '../../../core/theme/app_colors.dart';
import '../data/marketing_models.dart';
import '../shop_link.dart';

/// Banner karuseli (kelishuv 9.1): har 4 s avtomatik almashadi, pastda nuqtalar, bosilganda havola ochiladi.
/// Foydalanuvchi surayotganda va ekran ko'rinmayotganda (boshqa tab) to'xtaydi.
class BannerCarousel extends ConsumerStatefulWidget {
  const BannerCarousel({super.key, required this.banners});

  final List<ShopBanner> banners;

  @override
  ConsumerState<BannerCarousel> createState() => _BannerCarouselState();
}

class _BannerCarouselState extends ConsumerState<BannerCarousel> {
  final _page = PageController();
  Timer? _timer;
  int _index = 0;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _restart();
  }

  @override
  void didUpdateWidget(BannerCarousel old) {
    super.didUpdateWidget(old);
    if (_index >= widget.banners.length) _index = 0;
    _restart();
  }

  void _restart() {
    _timer?.cancel();
    if (widget.banners.length < 2 || !TickerMode.valuesOf(context).enabled) return;
    _timer = Timer.periodic(const Duration(seconds: 4), (_) {
      if (!mounted || !_page.hasClients) return;
      final next = (_index + 1) % widget.banners.length;
      _page.animateToPage(next, duration: const Duration(milliseconds: 450), curve: Curves.easeOutCubic);
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    _page.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final banners = widget.banners;
    return Column(
      children: [
        SizedBox(
          height: 150,
          child: NotificationListener<ScrollNotification>(
            onNotification: (n) {
              // Qo'lda surilganda taymer qaytadan boshlanadi.
              if (n is ScrollStartNotification && n.dragDetails != null) _timer?.cancel();
              if (n is ScrollEndNotification) _restart();
              return false;
            },
            child: PageView.builder(
              controller: _page,
              itemCount: banners.length,
              onPageChanged: (i) => setState(() => _index = i),
              itemBuilder: (_, i) => Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: _BannerCard(banner: banners[i]),
              ),
            ),
          ),
        ),
        if (banners.length > 1) ...[
          const SizedBox(height: 10),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              for (var i = 0; i < banners.length; i++)
                AnimatedContainer(
                  duration: const Duration(milliseconds: 250),
                  margin: const EdgeInsets.symmetric(horizontal: 3),
                  width: i == _index ? 18 : 6,
                  height: 6,
                  decoration: BoxDecoration(
                    color: i == _index ? AppColors.primary : AppColors.border,
                    borderRadius: BorderRadius.circular(3),
                  ),
                ),
            ],
          ),
        ],
      ],
    );
  }
}

class _BannerCard extends ConsumerWidget {
  const _BannerCard({required this.banner});

  final ShopBanner banner;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bg = AppColors.parseHex(banner.backgroundColor) ?? AppColors.primary;
    final image = AppConfig.imageUrl(banner.imageUrl);
    // Och fonda qora, to'q fonda oq matn.
    final dark = image != null || bg.computeLuminance() < 0.5;
    final fg = dark ? Colors.white : AppColors.text;
    return Semantics(
      button: banner.link.isActionable,
      label: banner.title,
      child: Material(
        color: bg,
        borderRadius: BorderRadius.circular(20),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: banner.link.isActionable ? () => openShopLinkFrom(GoRouter.of(context), ref, banner.link) : null,
          child: Stack(
            fit: StackFit.expand,
            children: [
              if (image != null) ...[
                Image.network(image, fit: BoxFit.cover, errorBuilder: (_, _, _) => const SizedBox.shrink()),
                const DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.centerLeft,
                      end: Alignment.centerRight,
                      colors: [Color(0xB3000000), Color(0x00000000)],
                    ),
                  ),
                ),
              ],
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 18, 20, 18),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    if (banner.title.isNotEmpty)
                      FractionallySizedBox(
                        widthFactor: 0.75,
                        child: Text(
                          banner.title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 19, fontWeight: FontWeight.w800, color: fg, height: 1.25),
                        ),
                      ),
                    if (banner.subtitle != null) ...[
                      const SizedBox(height: 6),
                      FractionallySizedBox(
                        widthFactor: 0.75,
                        child: Text(
                          banner.subtitle!,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 13, color: fg.withValues(alpha: 0.85)),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
