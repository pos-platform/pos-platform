using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Common.Interfaces;

/// <summary>Foydalanuvchi uchun JWT access token yaratadi.</summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
