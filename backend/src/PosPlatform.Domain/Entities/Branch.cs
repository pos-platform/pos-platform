using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
