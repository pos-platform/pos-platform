namespace PosPlatform.Domain.Enums;

/// <summary>
/// Tashkilot faoliyat turi. Restoran rejimida stol/oshxona/ovqat-retsept
/// modullari yoqiladi; do'kon rejimida klassik chakana savdo.
/// </summary>
public enum BusinessType
{
    /// <summary>Do'kon (chakana savdo).</summary>
    Store = 0,

    /// <summary>Restoran / kafe.</summary>
    Restaurant = 1
}
