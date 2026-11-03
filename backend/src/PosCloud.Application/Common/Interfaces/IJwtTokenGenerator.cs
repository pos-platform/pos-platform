using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>Foydalanuvchi uchun JWT access token yaratadi.</summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string> permissions);
}
