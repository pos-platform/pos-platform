using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>Tashkilot darajasidagi rol — har bir org o'z rollarini moslashtira oladi.</summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Tizim tomonidan avtomatik yaratilgan (Admin/Manager/Cashier). O'chirib bo'lmaydi.</summary>
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
