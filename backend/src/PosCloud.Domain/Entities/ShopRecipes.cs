using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md): retseptlar va oilaviy xarid ro'yxati.

/// <summary>Retsept (tenant): tashkilot admini yaratadi. BranchId null — barcha filiallar.</summary>
public class ShopRecipe : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CookMinutes { get; set; }
    public int? Servings { get; set; }

    /// <summary>Tayyorlash qadamlari — "\n" bilan ajratilgan.</summary>
    public string Steps { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Ilova bosh sahifasidagi "Bugun nima pishiramiz?" bloki.</summary>
    public bool ShowOnHome { get; set; }

    public ICollection<ShopRecipeItem> Items { get; set; } = new List<ShopRecipeItem>();
}

/// <summary>Retsept ingrediyenti — do'kondagi mahsulot va miqdori (tarozi tovari — kg).</summary>
public class ShopRecipeItem : BaseEntity
{
    public Guid RecipeId { get; set; }
    public ShopRecipe? Recipe { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; } = 1;

    /// <summary>Masalan "1 stakan", "maydalangan".</summary>
    public string? Note { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Xarid ro'yxati (oilaviy). GLOBAL (OrganizationId = null) — egasi va a'zolar bo'yicha qo'lda tekshiriladi.</summary>
public class ShopList : BaseEntity
{
    public Guid OwnerId { get; set; }
    public ShopUser? Owner { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<ShopListMember> Members { get; set; } = new List<ShopListMember>();
    public ICollection<ShopListItem> Items { get; set; } = new List<ShopListItem>();
}

/// <summary>Ro'yxat a'zosi — telefon raqami bilan qo'shiladi; shu raqamli foydalanuvchi kirganda bog'lanadi.</summary>
public class ShopListMember : BaseEntity
{
    public Guid ListId { get; set; }
    public ShopList? List { get; set; }

    /// <summary>+998XXXXXXXXX (ShopUser.Phone bilan bir xil format).</summary>
    public string Phone { get; set; } = string.Empty;

    public Guid? ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }
}

/// <summary>Ro'yxat bandi: erkin matn, ixtiyoriy katalog mahsuloti.</summary>
public class ShopListItem : BaseEntity
{
    public Guid ListId { get; set; }
    public ShopList? List { get; set; }

    public string Title { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal? Quantity { get; set; }

    public bool IsChecked { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string? CheckedByName { get; set; }

    public Guid AddedByUserId { get; set; }
    public string AddedByName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
