import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/push/push_service.dart';

import 'data/auth_models.dart';
import 'data/auth_repository.dart';
import 'data/token_storage.dart';

enum AuthStatus {
  /// Ilova ochilmoqda — saqlangan sessiya o'qilmoqda.
  unknown,

  /// Kirmagan (login ekrani).
  signedOut,

  /// "Hozircha kirmasdan ko'rish" — katalog ochiq, buyurtma uchun kirish so'raladi.
  guest,

  signedIn,
}

class AuthState {
  const AuthState(this.status, {this.user});

  final AuthStatus status;
  final ShopUser? user;

  bool get isSignedIn => status == AuthStatus.signedIn;
}

/// Mijoz sessiyasi. Tokenlar [TokenStorage]da, yangilash — `SessionManager` (api_client.dart).
class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    _restore();
    return const AuthState(AuthStatus.unknown);
  }

  Future<void> _restore() async {
    final session = await ref.read(tokenStorageProvider).read();
    state = session == null
        ? const AuthState(AuthStatus.signedOut)
        : AuthState(AuthStatus.signedIn, user: session.user);
  }

  AuthRepository get _repo => ref.read(authRepositoryProvider);

  /// "+998" + 9 raqam.
  static String fullPhone(String nineDigits) => '+998$nineDigits';

  Future<OtpSent> sendCode(String nineDigits, {bool preferSms = false}) =>
      _repo.sendOtp(fullPhone(nineDigits), preferSms: preferSms);

  Future<void> verify(String nineDigits, String code) async {
    final tokens = await _repo.verify(fullPhone(nineDigits), code);
    await ref.read(tokenStorageProvider).write(tokens);
    state = AuthState(AuthStatus.signedIn, user: tokens.user);
  }

  void continueAsGuest() => state = const AuthState(AuthStatus.guest);

  /// Mehmon rejimidan kirish ekraniga qaytish.
  void requestSignIn() => state = const AuthState(AuthStatus.signedOut);

  Future<void> updateUser(ShopUser user) async {
    await ref.read(tokenStorageProvider).updateUser(user);
    state = AuthState(AuthStatus.signedIn, user: user);
  }

  Future<void> signOut() async {
    final storage = ref.read(tokenStorageProvider);
    final session = await storage.read();
    if (session != null) {
      await _repo.logout(session.refreshToken, deviceToken: PushService.currentToken);
    }
    await storage.clear();
    state = const AuthState(AuthStatus.signedOut);
  }

  /// Refresh rad etilganda (boshqa qurilmada chiqildi, akkaunt bloklandi).
  void onSessionExpired() => state = const AuthState(AuthStatus.signedOut);
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(AuthController.new);
