namespace PosCloud.Domain.Common;

/// <summary>
/// Barcha entitylar uchun umumiy bazaviy klass.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Ko'p-ijarachilik (multi-tenant) uchun: yozuv qaysi tashkilotga tegishli.
    /// Yaratishda joriy foydalanuvchi tashkilotidan avtomatik to'ldiriladi va
    /// global query filter orqali har bir so'rov shu tashkilot bilan cheklanadi.
    /// Organization entity'sining o'ziga taalluqli emas (u tomonda Ignore qilingan).
    /// </summary>
    public Guid? OrganizationId { get; set; }
}
