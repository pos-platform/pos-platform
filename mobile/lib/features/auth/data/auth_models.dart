/// Mijoz akkaunti (backend `ShopUserDto`).
class ShopUser {
  const ShopUser({
    required this.id,
    required this.phone,
    this.firstName,
    this.lastName,
    this.profileCompleted = false,
  });

  final String id;
  final String phone;
  final String? firstName;
  final String? lastName;
  final bool profileCompleted;

  String get displayName {
    final name = [firstName, lastName].where((s) => s != null && s.trim().isNotEmpty).join(' ');
    return name.isEmpty ? phone : name;
  }

  factory ShopUser.fromJson(Map<String, dynamic> json) => ShopUser(
        id: json['id'] as String,
        phone: json['phone'] as String? ?? '',
        firstName: json['firstName'] as String?,
        lastName: json['lastName'] as String?,
        profileCompleted: json['profileCompleted'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'phone': phone,
        'firstName': firstName,
        'lastName': lastName,
        'profileCompleted': profileCompleted,
      };
}

/// `POST /api/customer-auth/send-otp` javobi.
class OtpSent {
  const OtpSent({
    required this.phone,
    required this.channel,
    required this.codeLength,
    required this.expiresInSeconds,
    required this.resendAfterSeconds,
  });

  final String phone;

  /// telegram | sms | log
  final String channel;
  final int codeLength;
  final int expiresInSeconds;
  final int resendAfterSeconds;

  factory OtpSent.fromJson(Map<String, dynamic> json) => OtpSent(
        phone: json['phone'] as String? ?? '',
        channel: json['channel'] as String? ?? 'sms',
        codeLength: json['codeLength'] as int? ?? 5,
        expiresInSeconds: json['expiresInSeconds'] as int? ?? 300,
        resendAfterSeconds: json['resendAfterSeconds'] as int? ?? 120,
      );
}

/// Tokenlar juftligi (verify / refresh javobi).
class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.accessTokenExpiresAt,
    required this.refreshToken,
    required this.user,
  });

  final String accessToken;
  final DateTime accessTokenExpiresAt;
  final String refreshToken;
  final ShopUser user;

  factory AuthTokens.fromJson(Map<String, dynamic> json) => AuthTokens(
        accessToken: json['accessToken'] as String,
        accessTokenExpiresAt: DateTime.parse(json['accessTokenExpiresAt'] as String).toUtc(),
        refreshToken: json['refreshToken'] as String,
        user: ShopUser.fromJson(json['user'] as Map<String, dynamic>),
      );
}
