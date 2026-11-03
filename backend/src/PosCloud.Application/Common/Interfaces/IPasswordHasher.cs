namespace PosCloud.Application.Common.Interfaces;

/// <summary>Parolni hash qilish va tekshirish (BCrypt implementatsiyasi Infrastructure'da).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
