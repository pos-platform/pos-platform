using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

// Bosqich 9: mobil ilova marketingi (banner, kolleksiya, stories, sevimlilar, obuna, savatcha, taklif, push kampaniya).

/// <summary>Ilova bosh sahifasidagi reklama banneri (tenant). BranchId null — barcha filiallar.</summary>
public class ShopBanner : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>#RRGGBB.</summary>
    public string? BackgroundColor { get; set; }

    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;

    /// <summary>Product/Category/Collection — Guid matni, Url — https havola.</summary>
    public string? LinkValue { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Mahsulotlar to'plami ("Nonushta uchun", "Yangi kelganlar") (tenant).</summary>
public class ShopCollection : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Bosh sahifada lenta sifatida ko'rsatiladi.</summary>
    public bool ShowOnHome { get; set; }

    public ICollection<ShopCollectionItem> Items { get; set; } = new List<ShopCollectionItem>();
}

public class ShopCollectionItem : BaseEntity
{
    public Guid CollectionId { get; set; }
    public ShopCollection? Collection { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Bosh sahifadagi "stories" (tenant).</summary>
public class ShopStory : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Title { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ShopStorySlide> Slides { get; set; } = new List<ShopStorySlide>();
}

public class ShopStorySlide : BaseEntity
{
    public Guid StoryId { get; set; }
    public ShopStory? Story { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;
    public string? LinkValue { get; set; }

    /// <summary>Ko'rsatish davomiyligi (3–15 s).</summary>
    public int DurationSec { get; set; } = 6;

    public int SortOrder { get; set; }
}

/// <summary>Mijozning sevimli mahsuloti. GLOBAL (OrganizationId = null).</summary>
public class ShopFavorite : BaseEntity
{
    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}

/// <summary>"Kelganda xabar berish" obunasi. GLOBAL (OrganizationId = null).</summary>
public class ShopStockSubscription : BaseEntity
{
    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Push yuborilgan vaqt (keyin yozuv o'chiriladi).</summary>
    public DateTime? NotifiedAt { get; set; }
}

/// <summary>
/// Mijoz savatchasining nusxasi (eslatma push uchun). GLOBAL. Savatcha o'zgargan vaqt — <see cref="ChangedAt"/>
/// (BaseEntity.UpdatedAt har qanday o'zgarishda, jumladan RemindedAt qo'yilganda ham yangilanadi).
/// </summary>
public class ShopCartSnapshot : BaseEntity
{
    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>[{ productId, quantity }] JSON.</summary>
    public string ItemsJson { get; set; } = "[]";

    /// <summary>Mahsulot turlari soni (push matni uchun).</summary>
    public int ItemCount { get; set; }

    /// <summary>Savatcha tarkibi oxirgi o'zgargan vaqt (mijoz PUT /api/me/cart).</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RemindedAt { get; set; }
}

/// <summary>Do'stni taklif qilish (tenant). Taklif qilingan mijoz tashkilot ichida bitta.</summary>
public class ShopReferral : BaseEntity
{
    public Guid ReferrerShopUserId { get; set; }
    public Guid RefereeShopUserId { get; set; }

    /// <summary>Mukofot berilgan vaqt (taklif qilinganning birinchi buyurtmasi yetkazilganda).</summary>
    public DateTime? RewardedAt { get; set; }

    /// <summary>Kod kiritilgan paytdagi bonuslar (ball).</summary>
    public decimal ReferrerBonus { get; set; }
    public decimal RefereeBonus { get; set; }
}

/// <summary>Do'kon adminining push kampaniyasi (tenant).</summary>
public class ShopPushCampaign : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;
    public string? LinkValue { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public PushCampaignStatus Status { get; set; } = PushCampaignStatus.Draft;

    /// <summary>Muvaffaqiyatli yuborilgan qurilmalar soni.</summary>
    public int SentCount { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
