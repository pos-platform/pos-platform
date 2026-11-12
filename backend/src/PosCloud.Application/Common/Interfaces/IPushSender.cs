namespace PosCloud.Application.Common.Interfaces;

/// <summary>Push yuborish natijasi.</summary>
public enum PushSendResult
{
    Sent,
    /// <summary>Token yaroqsiz yoki ilova o'chirilgan (FCM: UNREGISTERED/404) — tokenni o'chirish kerak.</summary>
    InvalidToken,
    /// <summary>Vaqtinchalik xato (tarmoq, 5xx, sozlama) — token saqlanadi.</summary>
    Failed
}

/// <summary>
/// Bitta qurilmaga push xabar yuborish (Infrastructure: FCM HTTP v1; sozlanmagan bo'lsa — faqat log).
/// Implementatsiya xatoni otmaydi, natijani qaytaradi.
/// </summary>
public interface IPushSender
{
    Task<PushSendResult> SendAsync(string token, string title, string body,
        IReadOnlyDictionary<string, string> data, CancellationToken ct = default);
}
