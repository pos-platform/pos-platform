namespace PosCloud.Infrastructure.Push;

/// <summary>
/// appsettings.json dagi "Push" bo'limi. Service account sirini env orqali bering:
/// Push__Fcm__ServiceAccountJson (JSON matni) yoki Push__Fcm__ServiceAccountPath (fayl yo'li).
/// Sozlanmagan bo'lsa push faqat logga yoziladi (dev).
/// </summary>
public class PushSettings
{
    public const string SectionName = "Push";

    public FcmSettings Fcm { get; set; } = new();
}

public class FcmSettings
{
    /// <summary>Firebase loyiha ID. Bo'sh bo'lsa service account'dagi project_id olinadi.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Google service account JSON matni (Firebase Console → Project settings → Service accounts).</summary>
    public string ServiceAccountJson { get; set; } = string.Empty;

    /// <summary>Yoki service account JSON fayliga yo'l.</summary>
    public string ServiceAccountPath { get; set; } = string.Empty;

    /// <summary>Android bildirishnoma kanali (mobil ilovada shu ID bilan yaratiladi).</summary>
    public string AndroidChannelId { get; set; } = "orders";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceAccountJson) || !string.IsNullOrWhiteSpace(ServiceAccountPath);
}
