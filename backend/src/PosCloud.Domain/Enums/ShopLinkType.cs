namespace PosCloud.Domain.Enums;

/// <summary>Banner, story slaydi va push kampaniya havolasi turi (ilovada bosilganda nima ochiladi).</summary>
public enum ShopLinkType
{
    None = 0,

    /// <summary>LinkValue — mahsulot ID.</summary>
    Product = 1,

    /// <summary>LinkValue — kategoriya ID.</summary>
    Category = 2,

    /// <summary>LinkValue — kolleksiya ID.</summary>
    Collection = 3,

    /// <summary>Aksiya sahifasi (LinkValue yo'q).</summary>
    Discounts = 4,

    /// <summary>LinkValue — https havola (tashqi brauzer).</summary>
    Url = 5
}
