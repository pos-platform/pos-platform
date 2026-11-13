using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineMarketing.Dtos;

// ---------------- bannerlar ----------------

public class SaveBannerRequest
{
    /// <summary>Null — barcha filiallar.</summary>
    public Guid? BranchId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? Subtitle { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>#RRGGBB.</summary>
    [MaxLength(7)]
    public string? BackgroundColor { get; set; }

    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;

    [MaxLength(500)]
    public string? LinkValue { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BannerAdminDto : SaveBannerRequest
{
    public Guid Id { get; set; }
    public string? BranchName { get; set; }
}

/// <summary>PUT .../reorder: ID'lar yangi tartibda (sortOrder = indeks).</summary>
public class ReorderRequest
{
    public List<Guid> Ids { get; set; } = [];
}

// ---------------- kolleksiyalar ----------------

public class SaveCollectionRequest
{
    public Guid? BranchId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnHome { get; set; }

    /// <summary>Mahsulotlar (tartib saqlanadi).</summary>
    public List<Guid> ProductIds { get; set; } = [];
}

public class CollectionProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class CollectionAdminDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool ShowOnHome { get; set; }
    public List<Guid> ProductIds { get; set; } = [];
    public List<CollectionProductDto> Products { get; set; } = [];
}

// ---------------- stories ----------------

public class SaveStorySlideRequest
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;

    [MaxLength(500)]
    public string? LinkValue { get; set; }

    /// <summary>3–15 soniya (standart 6).</summary>
    public int DurationSec { get; set; } = 6;
}

public class SaveStoryRequest
{
    public Guid? BranchId { get; set; }

    [Required]
    [MaxLength(40)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string CoverImageUrl { get; set; } = string.Empty;

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Slaydlar (tartib saqlanadi) — to'liq ro'yxat sifatida almashtiriladi.</summary>
    public List<SaveStorySlideRequest> Slides { get; set; } = [];
}

public class StorySlideAdminDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public ShopLinkType LinkType { get; set; }
    public string? LinkValue { get; set; }
    public int DurationSec { get; set; }
    public int SortOrder { get; set; }
}

public class StoryAdminDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<StorySlideAdminDto> Slides { get; set; } = [];
}

// ---------------- push kampaniyalar ----------------

public class SavePushCampaignRequest
{
    /// <summary>Berilsa — faqat shu filialda buyurtma berganlarga.</summary>
    public Guid? BranchId { get; set; }

    [Required]
    [MaxLength(60)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Body { get; set; } = string.Empty;

    public ShopLinkType LinkType { get; set; } = ShopLinkType.None;

    [MaxLength(500)]
    public string? LinkValue { get; set; }

    /// <summary>Null — qoralama (Draft); berilsa — shu vaqtda yuboriladi (Scheduled).</summary>
    public DateTime? ScheduledAt { get; set; }
}

public class PushCampaignDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public ShopLinkType LinkType { get; set; }
    public string? LinkValue { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public PushCampaignStatus Status { get; set; }
    public int SentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
