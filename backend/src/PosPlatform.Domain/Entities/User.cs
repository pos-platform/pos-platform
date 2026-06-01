using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Hisob faolmi. Faolsiz foydalanuvchi tizimga kira olmaydi (o'chirilmaydi, faqat bloklanadi).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Foydalanuvchi biriktirilgan filial (ixtiyoriy).</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}
