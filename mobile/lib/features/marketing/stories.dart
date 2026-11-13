import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../core/theme/app_colors.dart';
import '../../l10n/app_localizations.dart';
import '../catalog/widgets/common.dart';
import 'data/marketing_models.dart';
import 'shop_link.dart';

/// Ko'rilgan stories ID'lari — qurilmada (kelishuv 9.3: "ko'rilganlari kulrang, lokal").
class SeenStories extends Notifier<Set<String>> {
  static const _key = 'stories.seen';

  @override
  Set<String> build() {
    _load();
    return const {};
  }

  Future<void> _load() async {
    try {
      final list = (await SharedPreferences.getInstance()).getStringList(_key);
      if (list != null) state = {...state, ...list};
    } catch (_) {}
  }

  Future<void> markSeen(String id) async {
    if (state.contains(id)) return;
    state = {...state, id};
    try {
      // Eski yozuvlar cheksiz o'smasin — oxirgi 200 tasi.
      final list = state.toList();
      await (await SharedPreferences.getInstance()).setStringList(
        _key,
        list.length > 200 ? list.sublist(list.length - 200) : list,
      );
    } catch (_) {}
  }
}

final seenStoriesProvider = NotifierProvider<SeenStories, Set<String>>(SeenStories.new);

/// Bosh sahifadagi stories doirachalari: ko'rilmaganlari brend rangli halqa, ko'rilganlari kulrang (oxirida).
class StoriesRow extends ConsumerWidget {
  const StoriesRow({super.key, required this.stories});

  final List<ShopStory> stories;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final seen = ref.watch(seenStoriesProvider);
    final ordered = [...stories.where((s) => !seen.contains(s.id)), ...stories.where((s) => seen.contains(s.id))];
    return SizedBox(
      height: 98,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 20),
        itemCount: ordered.length,
        separatorBuilder: (_, _) => const SizedBox(width: 12),
        itemBuilder: (_, i) {
          final s = ordered[i];
          final isSeen = seen.contains(s.id);
          return Semantics(
            button: true,
            label: s.title,
            child: GestureDetector(
              onTap: () => openStories(context, ordered, i),
              child: SizedBox(
                width: 72,
                child: Column(
                  children: [
                    Container(
                      width: 68,
                      height: 68,
                      padding: const EdgeInsets.all(3),
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        border: Border.all(color: isSeen ? AppColors.border : AppColors.primary, width: 2.5),
                      ),
                      child: ClipOval(
                        child: ProductImage(
                          url: s.coverImageUrl ?? s.slides.first.imageUrl,
                          iconSize: 22,
                          fit: BoxFit.cover,
                        ),
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      s.title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: isSeen ? AppColors.textSecondary : AppColors.text,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

/// To'liq ekran ko'rish (pastki menyu ustida).
Future<void> openStories(BuildContext context, List<ShopStory> stories, int index) =>
    Navigator.of(context, rootNavigator: true).push(
      PageRouteBuilder<void>(
        opaque: false,
        barrierColor: Colors.black,
        transitionDuration: const Duration(milliseconds: 220),
        pageBuilder: (_, _, _) => StoryViewer(stories: stories, initialIndex: index),
        transitionsBuilder: (_, a, _, child) => FadeTransition(opacity: a, child: child),
      ),
    );

/// Stories ko'ruvchi: tepada progress chiziqlari; o'ng tomonga bosish — keyingi, chapga — oldingi;
/// ushlab turish — pauza; pastga surish — yopish; slaydda havola bo'lsa — tugma.
class StoryViewer extends ConsumerStatefulWidget {
  const StoryViewer({super.key, required this.stories, required this.initialIndex});

  final List<ShopStory> stories;
  final int initialIndex;

  @override
  ConsumerState<StoryViewer> createState() => _StoryViewerState();
}

class _StoryViewerState extends ConsumerState<StoryViewer> with SingleTickerProviderStateMixin {
  late final AnimationController _progress;
  late int _story = widget.initialIndex;
  int _slide = 0;
  double _dragY = 0;

  ShopStory get _current => widget.stories[_story];

  @override
  void initState() {
    super.initState();
    _progress = AnimationController(vsync: this)
      ..addStatusListener((s) {
        if (s == AnimationStatus.completed) _next();
      });
    _start();
  }

  @override
  void dispose() {
    _progress.dispose();
    super.dispose();
  }

  void _start() {
    ref.read(seenStoriesProvider.notifier).markSeen(_current.id);
    _progress.duration = Duration(seconds: _current.slides[_slide].durationSec);
    _progress.forward(from: 0);
  }

  void _next() {
    if (_slide < _current.slides.length - 1) {
      setState(() => _slide++);
    } else if (_story < widget.stories.length - 1) {
      setState(() {
        _story++;
        _slide = 0;
      });
    } else {
      _close();
      return;
    }
    _start();
  }

  void _prev() {
    if (_slide > 0) {
      setState(() => _slide--);
    } else if (_story > 0) {
      setState(() {
        _story--;
        _slide = 0;
      });
    }
    _start();
  }

  void _close() {
    _progress.stop();
    if (mounted) Navigator.of(context).maybePop();
  }

  Future<void> _openLink(ShopLink link) async {
    final router = GoRouter.of(context);
    final categories = ref.read(homeCategoryRequestProvider.notifier);
    _close();
    await openShopLink(router, categories, link);
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final story = _current;
    final slide = story.slides[_slide];
    final pad = MediaQuery.paddingOf(context);
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTapUp: (d) => d.globalPosition.dx < MediaQuery.sizeOf(context).width / 3 ? _prev() : _next(),
        onLongPressStart: (_) => _progress.stop(),
        onLongPressEnd: (_) => _progress.forward(),
        onVerticalDragStart: (_) => _progress.stop(),
        onVerticalDragUpdate: (d) => setState(() => _dragY = (_dragY + d.delta.dy).clamp(0, 400)),
        onVerticalDragEnd: (d) {
          if (_dragY > 120 || (d.primaryVelocity ?? 0) > 700) {
            _close();
          } else {
            setState(() => _dragY = 0);
            _progress.forward();
          }
        },
        child: Transform.translate(
          offset: Offset(0, _dragY),
          child: Opacity(
            opacity: (1 - _dragY / 500).clamp(0.3, 1.0),
            child: ColoredBox(
              color: Colors.black,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  ProductImage(key: ValueKey(slide.id), url: slide.imageUrl, iconSize: 64, fit: BoxFit.cover),
                  const DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        stops: [0, 0.2, 0.65, 1],
                        colors: [Color(0x99000000), Color(0x00000000), Color(0x00000000), Color(0xB3000000)],
                      ),
                    ),
                  ),
                  Positioned(
                    top: pad.top + 8,
                    left: 12,
                    right: 12,
                    child: Column(
                      children: [
                        Row(
                          children: [
                            for (var i = 0; i < story.slides.length; i++)
                              Expanded(
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(horizontal: 2),
                                  child: ClipRRect(
                                    borderRadius: BorderRadius.circular(2),
                                    child: i == _slide
                                        ? AnimatedBuilder(
                                            animation: _progress,
                                            builder: (_, _) => _Bar(value: _progress.value),
                                          )
                                        : _Bar(value: i < _slide ? 1 : 0),
                                  ),
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                story.title,
                                style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700),
                              ),
                            ),
                            IconButton(
                              tooltip: t.mkClose,
                              onPressed: _close,
                              icon: const Icon(Icons.close, color: Colors.white),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Positioned(
                    left: 20,
                    right: 20,
                    bottom: pad.bottom + 24,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (slide.caption != null)
                          Text(
                            slide.caption!,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.w700,
                              height: 1.35,
                            ),
                          ),
                        if (slide.link.isActionable) ...[
                          const SizedBox(height: 16),
                          FilledButton(
                            style: FilledButton.styleFrom(
                              backgroundColor: Colors.white,
                              foregroundColor: AppColors.text,
                            ),
                            onPressed: () => _openLink(slide.link),
                            child: Text(t.mkStoryOpen),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Bar extends StatelessWidget {
  const _Bar({required this.value});

  final double value;

  @override
  Widget build(BuildContext context) => LinearProgressIndicator(
    value: value,
    minHeight: 3,
    color: Colors.white,
    backgroundColor: Colors.white.withValues(alpha: 0.35),
  );
}
