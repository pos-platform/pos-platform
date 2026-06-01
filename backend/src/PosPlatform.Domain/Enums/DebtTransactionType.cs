namespace PosPlatform.Domain.Enums;

/// <summary>Qarzdor tranzaksiya turi.</summary>
public enum DebtTransactionType
{
    /// <summary>Qarz qo'shildi (mijoz nasiyaga oldi) — balans oshadi.</summary>
    Debt = 0,
    /// <summary>To'lov (mijoz qarzini to'ladi) — balans kamayadi.</summary>
    Payment = 1
}
