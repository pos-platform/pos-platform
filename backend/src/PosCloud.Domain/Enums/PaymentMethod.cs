namespace PosCloud.Domain.Enums;

/// <summary>To'lov turi.</summary>
public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    /// <summary>Aralash to'lov (naqd + karta).</summary>
    Mixed = 2,
    /// <summary>Nasiya (qarz) — qarzdor balansiga yoziladi.</summary>
    Debt = 3,
    /// <summary>Bank o'tkazmasi (Payme, Click, naqd pul o'tkazmasi).</summary>
    Transfer = 4
}
