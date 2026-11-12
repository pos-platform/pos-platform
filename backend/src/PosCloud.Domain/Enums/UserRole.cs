namespace PosCloud.Domain.Enums;

/// <summary>
/// Foydalanuvchi roli (huquqlar darajasi).
/// </summary>
public enum UserRole
{
    Admin = 0,
    Manager = 1,
    Cashier = 2,

    /// <summary>Platforma egasi — barcha tashkilotlarni va obunalarni boshqaradi. Litsenziyaga bog'liq emas.</summary>
    SuperAdmin = 3,

    /// <summary>Ofitsiant (restoran) — stolda buyurtma oladi, oshxonaga yuboradi.</summary>
    Waiter = 4,

    /// <summary>Kuryer — faqat mobil ilovaning kuryer bo'limi (o'ziga biriktirilgan online buyurtmalar). Litsenziya talab qilmaydi, desktop kassaga kirmaydi.</summary>
    Courier = 5
}
