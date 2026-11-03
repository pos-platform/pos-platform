namespace PosCloud.Domain.Enums;

/// <summary>
/// Chekning fiskal holati. Fiskalizatsiya keyinchalik alohida lokal .NET agent
/// (FDS / Rahmat MK) orqali amalga oshiriladi.
/// </summary>
public enum FiscalStatus
{
    /// <summary>Hali soliq tizimiga yuborilmagan.</summary>
    NotSent = 0,
    /// <summary>Fiskal agentga yuborish navbatida.</summary>
    Queued = 1,
    /// <summary>Muvaffaqiyatli fiskalizatsiya qilingan (fiskal belgi olingan).</summary>
    Registered = 2,
    /// <summary>Xatolik bilan yakunlangan.</summary>
    Failed = 3
}
