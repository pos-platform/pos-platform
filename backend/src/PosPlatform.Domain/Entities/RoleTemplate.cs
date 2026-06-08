namespace PosPlatform.Domain.Entities;

/// <summary>
/// Yangi tashkilot yaratilganda ular uchun avtomatik yaratiladigan rollar shabloni.
/// BaseEntity'dan meros olmaydi — tashkilotga bog'liq emas, global sozlama.
/// </summary>
public class RoleTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Tizim shabloni — o'chirib bo'lmaydi, faqat ruxsatlar tahrirlanadi.</summary>
    public bool IsSystem { get; set; }

    /// <summary>JSON array — ruxsat kodlari ro'yxati, masalan ["users.view","sales.create"].</summary>
    public string PermissionsJson { get; set; } = "[]";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
