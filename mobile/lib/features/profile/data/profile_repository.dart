import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../auth/auth_controller.dart';

/// Backend `Gender` enumi (JSON'da satr: "Male" / "Female").
enum Gender {
  male('Male'),
  female('Female');

  const Gender(this.json);
  final String json;

  static Gender? fromJson(Object? value) {
    for (final g in Gender.values) {
      if (g.json == value) return g;
    }
    return null;
  }
}

/// `GET /api/me` javobi (`ShopProfileDto`).
class ShopProfile {
  const ShopProfile({
    required this.id,
    required this.phone,
    this.phoneVerified = false,
    this.firstName,
    this.lastName,
    this.birthDate,
    this.gender,
    this.email,
    this.addressCount = 0,
    this.profileCompleted = false,
  });

  final String id;
  final String phone;
  final bool phoneVerified;
  final String? firstName;
  final String? lastName;

  /// Faqat sana (DateOnly, "yyyy-MM-dd").
  final DateTime? birthDate;
  final Gender? gender;
  final String? email;
  final int addressCount;
  final bool profileCompleted;

  factory ShopProfile.fromJson(Map<String, dynamic> json) => ShopProfile(
        id: json['id'] as String,
        phone: json['phone'] as String? ?? '',
        phoneVerified: json['phoneVerified'] as bool? ?? false,
        firstName: json['firstName'] as String?,
        lastName: json['lastName'] as String?,
        birthDate: json['birthDate'] == null ? null : DateTime.tryParse(json['birthDate'] as String),
        gender: Gender.fromJson(json['gender']),
        email: json['email'] as String?,
        addressCount: json['addressCount'] as int? ?? 0,
        profileCompleted: json['profileCompleted'] as bool? ?? false,
      );
}

/// `PUT /api/me` so'rovi (`UpdateShopProfileRequest`).
class UpdateProfileRequest {
  const UpdateProfileRequest({
    required this.firstName,
    this.lastName,
    this.birthDate,
    this.gender,
    this.email,
  });

  final String firstName;
  final String? lastName;
  final DateTime? birthDate;
  final Gender? gender;
  final String? email;

  Map<String, dynamic> toJson() => {
        'firstName': firstName,
        'lastName': lastName,
        'birthDate': birthDate == null ? null : formatDateOnly(birthDate!),
        'gender': gender?.json,
        'email': email,
      };
}

/// DateOnly formati: "yyyy-MM-dd".
String formatDateOnly(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

class ProfileRepository {
  ProfileRepository(this._dio);

  final Dio _dio;

  Future<ShopProfile> get() => apiCall(() async {
        final res = await _dio.get<Map<String, dynamic>>('/api/me');
        return ShopProfile.fromJson(res.data!);
      });

  Future<ShopProfile> update(UpdateProfileRequest request) => apiCall(() async {
        final res = await _dio.put<Map<String, dynamic>>('/api/me', data: request.toJson());
        return ShopProfile.fromJson(res.data!);
      });

  /// Akkauntni o'chirish: server shaxsiy ma'lumotlarni tozalaydi va barcha sessiyalarni yopadi.
  Future<void> deleteAccount() => apiCall(() => _dio.delete<void>('/api/me'));
}

final profileRepositoryProvider = Provider<ProfileRepository>((ref) => ProfileRepository(ref.watch(dioProvider)));

/// Joriy mijoz profili. Mehmon/kirmagan — null.
final profileProvider = FutureProvider<ShopProfile?>((ref) async {
  final userId = ref.watch(authControllerProvider.select((s) => s.isSignedIn ? s.user?.id : null));
  if (userId == null) return null;
  return ref.watch(profileRepositoryProvider).get();
});
