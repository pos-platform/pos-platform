using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineMarketing.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineMarketing;

/// <summary>Do'kon admini: ilova reklamasi — bannerlar, kolleksiyalar, stories, push kampaniyalar.</summary>
public interface IOnlineMarketingService
{
    Task<IReadOnlyList<BannerAdminDto>> GetBannersAsync(CancellationToken ct = default);
    Task<BannerAdminDto> CreateBannerAsync(SaveBannerRequest request, CancellationToken ct = default);
    Task<BannerAdminDto> UpdateBannerAsync(Guid id, SaveBannerRequest request, CancellationToken ct = default);
    Task DeleteBannerAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BannerAdminDto>> ReorderBannersAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<CollectionAdminDto>> GetCollectionsAsync(CancellationToken ct = default);
    Task<CollectionAdminDto> GetCollectionAsync(Guid id, CancellationToken ct = default);
    Task<CollectionAdminDto> CreateCollectionAsync(SaveCollectionRequest request, CancellationToken ct = default);
    Task<CollectionAdminDto> UpdateCollectionAsync(Guid id, SaveCollectionRequest request, CancellationToken ct = default);
    Task DeleteCollectionAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<StoryAdminDto>> GetStoriesAsync(CancellationToken ct = default);
    Task<StoryAdminDto> GetStoryAsync(Guid id, CancellationToken ct = default);
    Task<StoryAdminDto> CreateStoryAsync(SaveStoryRequest request, CancellationToken ct = default);
    Task<StoryAdminDto> UpdateStoryAsync(Guid id, SaveStoryRequest request, CancellationToken ct = default);
    Task DeleteStoryAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StoryAdminDto>> ReorderStoriesAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<PushCampaignDto>> GetPushCampaignsAsync(CancellationToken ct = default);
    Task<PushCampaignDto> CreatePushCampaignAsync(SavePushCampaignRequest request, CancellationToken ct = default);
    Task<PushCampaignDto> UpdatePushCampaignAsync(Guid id, SavePushCampaignRequest request, CancellationToken ct = default);
    Task DeletePushCampaignAsync(Guid id, CancellationToken ct = default);

    /// <summary>Hozir yuborish: kampaniya darhol navbatga qo'yiladi (fon xizmati ~30 soniyada yuboradi).</summary>
    Task<PushCampaignDto> SendPushCampaignNowAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Xodim konteksti — tenant filtri ishlaydi, lekin tashkilot baribir aniq tekshiriladi (yangi yozuvga qo'lda qo'yiladi).
/// </summary>
public class OnlineMarketingService : IOnlineMarketingService
{
    public const int MaxBanners = 50;
    public const int MaxCollectionProducts = 200;
    public const int MaxStorySlides = 20;
    public const int MaxCampaignsPerDay = 3;
    private const int CampaignListLimit = 200;

    private static readonly Regex ColorRegex = new("^#[0-9A-F]{6}$", RegexOptions.Compiled);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public OnlineMarketingService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid OrgId => _currentUser.OrganizationId
        ?? throw new ValidationException("Bu amal tashkilot foydalanuvchisi uchun.");

    // ================= bannerlar =================

    public async Task<IReadOnlyList<BannerAdminDto>> GetBannersAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        return await _db.ShopBanners.AsNoTracking()
            .Where(b => b.OrganizationId == orgId)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.CreatedAt)
            .Select(b => new BannerAdminDto
            {
                Id = b.Id,
                BranchId = b.BranchId,
                BranchName = b.Branch != null ? b.Branch.Name : null,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ImageUrl = b.ImageUrl,
                BackgroundColor = b.BackgroundColor,
                LinkType = b.LinkType,
                LinkValue = b.LinkValue,
                StartsAt = b.StartsAt,
                EndsAt = b.EndsAt,
                SortOrder = b.SortOrder,
                IsActive = b.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<BannerAdminDto> CreateBannerAsync(SaveBannerRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        if (await _db.ShopBanners.CountAsync(b => b.OrganizationId == orgId, ct) >= MaxBanners)
            throw new ValidationException($"Ko'pi bilan {MaxBanners} ta banner bo'lishi mumkin.");

        var banner = new ShopBanner { OrganizationId = orgId };
        await ApplyBanner(banner, r, orgId, ct);
        _db.ShopBanners.Add(banner);
        await _db.SaveChangesAsync(ct);
        return await GetBannerDto(banner.Id, ct);
    }

    public async Task<BannerAdminDto> UpdateBannerAsync(Guid id, SaveBannerRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var banner = await _db.ShopBanners.FirstOrDefaultAsync(b => b.Id == id && b.OrganizationId == orgId, ct)
                     ?? throw new NotFoundException("Banner topilmadi.");
        await ApplyBanner(banner, r, orgId, ct);
        await _db.SaveChangesAsync(ct);
        return await GetBannerDto(banner.Id, ct);
    }

    public async Task DeleteBannerAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var banner = await _db.ShopBanners.FirstOrDefaultAsync(b => b.Id == id && b.OrganizationId == orgId, ct)
                     ?? throw new NotFoundException("Banner topilmadi.");
        _db.ShopBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BannerAdminDto>> ReorderBannersAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var banners = await _db.ShopBanners.Where(b => b.OrganizationId == orgId).ToListAsync(ct);
        ApplyOrder(banners, ids, b => b.Id, (b, i) => b.SortOrder = i);
        await _db.SaveChangesAsync(ct);
        return await GetBannersAsync(ct);
    }

    private async Task ApplyBanner(ShopBanner b, SaveBannerRequest r, Guid orgId, CancellationToken ct)
    {
        var title = Required(r.Title, "Sarlavha", 80);
        var color = NullIfEmpty(r.BackgroundColor)?.ToUpperInvariant();
        if (color is not null && !ColorRegex.IsMatch(color))
            throw new ValidationException("Fon rangi #RRGGBB ko'rinishida bo'lishi kerak.");
        ValidatePeriod(r.StartsAt, r.EndsAt);
        await ValidateBranch(r.BranchId, orgId, ct);
        var link = await ValidateLink(r.LinkType, r.LinkValue, orgId, ct);

        b.BranchId = r.BranchId;
        b.Title = title;
        b.Subtitle = Optional(r.Subtitle, "Izoh", 160);
        b.ImageUrl = Optional(r.ImageUrl, "Rasm", 500);
        b.BackgroundColor = color;
        b.LinkType = r.LinkType;
        b.LinkValue = link;
        b.StartsAt = Utc(r.StartsAt);
        b.EndsAt = Utc(r.EndsAt);
        b.SortOrder = r.SortOrder;
        b.IsActive = r.IsActive;
    }

    private async Task<BannerAdminDto> GetBannerDto(Guid id, CancellationToken ct) =>
        (await GetBannersAsync(ct)).First(b => b.Id == id);

    // ================= kolleksiyalar =================

    public async Task<IReadOnlyList<CollectionAdminDto>> GetCollectionsAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        var collections = await _db.ShopCollections.AsNoTracking()
            .Where(c => c.OrganizationId == orgId)
            .Include(c => c.Branch)
            .Include(c => c.Items)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return await ToCollectionDtos(collections, orgId, ct);
    }

    public async Task<CollectionAdminDto> GetCollectionAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var collection = await _db.ShopCollections.AsNoTracking()
                             .Include(c => c.Branch)
                             .Include(c => c.Items)
                             .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, ct)
                         ?? throw new NotFoundException("Kolleksiya topilmadi.");
        return (await ToCollectionDtos([collection], orgId, ct))[0];
    }

    public async Task<CollectionAdminDto> CreateCollectionAsync(SaveCollectionRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var collection = new ShopCollection { OrganizationId = orgId };
        await ApplyCollection(collection, r, orgId, ct);
        _db.ShopCollections.Add(collection);
        await _db.SaveChangesAsync(ct);
        return await GetCollectionAsync(collection.Id, ct);
    }

    public async Task<CollectionAdminDto> UpdateCollectionAsync(Guid id, SaveCollectionRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var collection = await _db.ShopCollections.Include(c => c.Items)
                             .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, ct)
                         ?? throw new NotFoundException("Kolleksiya topilmadi.");
        await ApplyCollection(collection, r, orgId, ct);
        await _db.SaveChangesAsync(ct);
        return await GetCollectionAsync(id, ct);
    }

    public async Task DeleteCollectionAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var collection = await _db.ShopCollections.Include(c => c.Items)
                             .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, ct)
                         ?? throw new NotFoundException("Kolleksiya topilmadi.");
        _db.ShopCollectionItems.RemoveRange(collection.Items);
        _db.ShopCollections.Remove(collection);
        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyCollection(ShopCollection c, SaveCollectionRequest r, Guid orgId, CancellationToken ct)
    {
        var title = Required(r.Title, "Nom", 80);
        ValidatePeriod(r.StartsAt, r.EndsAt);
        await ValidateBranch(r.BranchId, orgId, ct);

        var ids = (r.ProductIds ?? []).Distinct().ToList();
        if (ids.Count > MaxCollectionProducts)
            throw new ValidationException($"Kolleksiyada ko'pi bilan {MaxCollectionProducts} ta mahsulot bo'lishi mumkin.");
        var known = await _db.Products.Where(p => p.OrganizationId == orgId && ids.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct);
        if (known.Count != ids.Count)
            throw new ValidationException("Mahsulot topilmadi.");

        c.BranchId = r.BranchId;
        c.Title = title;
        c.Description = Optional(r.Description, "Tavsif", 500);
        c.ImageUrl = Optional(r.ImageUrl, "Rasm", 500);
        c.StartsAt = Utc(r.StartsAt);
        c.EndsAt = Utc(r.EndsAt);
        c.SortOrder = r.SortOrder;
        c.IsActive = r.IsActive;
        c.ShowOnHome = r.ShowOnHome;

        // Mahsulotlar — to'liq ro'yxat, tartib saqlanadi.
        foreach (var old in c.Items.Where(i => !ids.Contains(i.ProductId)).ToList())
        {
            c.Items.Remove(old);
            _db.ShopCollectionItems.Remove(old);
        }
        for (var i = 0; i < ids.Count; i++)
        {
            var item = c.Items.FirstOrDefault(x => x.ProductId == ids[i]);
            if (item is null)
                c.Items.Add(new ShopCollectionItem { OrganizationId = orgId, CollectionId = c.Id, ProductId = ids[i], SortOrder = i });
            else
                item.SortOrder = i;
        }
    }

    private async Task<List<CollectionAdminDto>> ToCollectionDtos(List<ShopCollection> collections, Guid orgId,
        CancellationToken ct)
    {
        var productIds = collections.SelectMany(c => c.Items.Select(i => i.ProductId)).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.OrganizationId == orgId && productIds.Contains(p.Id))
            .Select(p => new CollectionProductDto { Id = p.Id, Name = p.Name, ImageUrl = p.ImageUrl, Price = p.Price })
            .ToDictionaryAsync(p => p.Id, ct);

        return collections.Select(c =>
        {
            var ordered = c.Items.OrderBy(i => i.SortOrder).Select(i => i.ProductId).ToList();
            return new CollectionAdminDto
            {
                Id = c.Id,
                BranchId = c.BranchId,
                BranchName = c.Branch?.Name,
                Title = c.Title,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                StartsAt = c.StartsAt,
                EndsAt = c.EndsAt,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ShowOnHome = c.ShowOnHome,
                ProductIds = ordered,
                Products = ordered.Where(products.ContainsKey).Select(id => products[id]).ToList()
            };
        }).ToList();
    }

    // ================= stories =================

    public async Task<IReadOnlyList<StoryAdminDto>> GetStoriesAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        var stories = await _db.ShopStories.AsNoTracking()
            .Where(s => s.OrganizationId == orgId)
            .Include(s => s.Branch)
            .Include(s => s.Slides)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt)
            .ToListAsync(ct);
        return stories.Select(ToStoryDto).ToList();
    }

    public async Task<StoryAdminDto> GetStoryAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var story = await _db.ShopStories.AsNoTracking()
                        .Include(s => s.Branch)
                        .Include(s => s.Slides)
                        .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == orgId, ct)
                    ?? throw new NotFoundException("Story topilmadi.");
        return ToStoryDto(story);
    }

    public async Task<StoryAdminDto> CreateStoryAsync(SaveStoryRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var story = new ShopStory { OrganizationId = orgId };
        await ApplyStory(story, r, orgId, ct);
        _db.ShopStories.Add(story);
        await _db.SaveChangesAsync(ct);
        return await GetStoryAsync(story.Id, ct);
    }

    public async Task<StoryAdminDto> UpdateStoryAsync(Guid id, SaveStoryRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var story = await _db.ShopStories.Include(s => s.Slides)
                        .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == orgId, ct)
                    ?? throw new NotFoundException("Story topilmadi.");
        await ApplyStory(story, r, orgId, ct);
        await _db.SaveChangesAsync(ct);
        return await GetStoryAsync(id, ct);
    }

    public async Task DeleteStoryAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var story = await _db.ShopStories.Include(s => s.Slides)
                        .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == orgId, ct)
                    ?? throw new NotFoundException("Story topilmadi.");
        _db.ShopStorySlides.RemoveRange(story.Slides);
        _db.ShopStories.Remove(story);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<StoryAdminDto>> ReorderStoriesAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var stories = await _db.ShopStories.Where(s => s.OrganizationId == orgId).ToListAsync(ct);
        ApplyOrder(stories, ids, s => s.Id, (s, i) => s.SortOrder = i);
        await _db.SaveChangesAsync(ct);
        return await GetStoriesAsync(ct);
    }

    private async Task ApplyStory(ShopStory s, SaveStoryRequest r, Guid orgId, CancellationToken ct)
    {
        var title = Required(r.Title, "Sarlavha", 40);
        var cover = Required(r.CoverImageUrl, "Muqova rasmi", 500);
        ValidatePeriod(r.StartsAt, r.EndsAt);
        await ValidateBranch(r.BranchId, orgId, ct);

        var slides = r.Slides ?? [];
        if (slides.Count == 0)
            throw new ValidationException("Kamida bitta slayd qo'shing.");
        if (slides.Count > MaxStorySlides)
            throw new ValidationException($"Ko'pi bilan {MaxStorySlides} ta slayd bo'lishi mumkin.");

        var normalized = new List<(SaveStorySlideRequest Req, string Image, string? Caption, string? Link)>();
        foreach (var slide in slides)
        {
            if (slide.DurationSec is < 3 or > 15)
                throw new ValidationException("Slayd davomiyligi 3–15 soniya bo'lishi kerak.");
            normalized.Add((slide, Required(slide.ImageUrl, "Slayd rasmi", 500), Optional(slide.Caption, "Slayd matni", 200),
                await ValidateLink(slide.LinkType, slide.LinkValue, orgId, ct)));
        }

        s.BranchId = r.BranchId;
        s.Title = title;
        s.CoverImageUrl = cover;
        s.StartsAt = Utc(r.StartsAt);
        s.EndsAt = Utc(r.EndsAt);
        s.SortOrder = r.SortOrder;
        s.IsActive = r.IsActive;

        // Slaydlar — to'liq ro'yxat: id bo'yicha yangilanadi, qolganlari o'chiriladi.
        var keepIds = normalized.Where(n => n.Req.Id is not null).Select(n => n.Req.Id!.Value).ToHashSet();
        foreach (var old in s.Slides.Where(x => !keepIds.Contains(x.Id)).ToList())
        {
            s.Slides.Remove(old);
            _db.ShopStorySlides.Remove(old);
        }
        for (var i = 0; i < normalized.Count; i++)
        {
            var n = normalized[i];
            var slide = n.Req.Id is { } sid ? s.Slides.FirstOrDefault(x => x.Id == sid) : null;
            if (slide is null)
            {
                slide = new ShopStorySlide { OrganizationId = orgId, StoryId = s.Id };
                s.Slides.Add(slide);
            }
            slide.ImageUrl = n.Image;
            slide.Caption = n.Caption;
            slide.LinkType = n.Req.LinkType;
            slide.LinkValue = n.Link;
            slide.DurationSec = n.Req.DurationSec;
            slide.SortOrder = i;
        }
    }

    private static StoryAdminDto ToStoryDto(ShopStory s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        BranchName = s.Branch?.Name,
        Title = s.Title,
        CoverImageUrl = s.CoverImageUrl,
        StartsAt = s.StartsAt,
        EndsAt = s.EndsAt,
        SortOrder = s.SortOrder,
        IsActive = s.IsActive,
        Slides = s.Slides.OrderBy(x => x.SortOrder).Select(x => new StorySlideAdminDto
        {
            Id = x.Id,
            ImageUrl = x.ImageUrl,
            Caption = x.Caption,
            LinkType = x.LinkType,
            LinkValue = x.LinkValue,
            DurationSec = x.DurationSec,
            SortOrder = x.SortOrder
        }).ToList()
    };

    // ================= push kampaniyalar =================

    public async Task<IReadOnlyList<PushCampaignDto>> GetPushCampaignsAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        return await _db.ShopPushCampaigns.AsNoTracking()
            .Where(c => c.OrganizationId == orgId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(CampaignListLimit)
            .Select(c => new PushCampaignDto
            {
                Id = c.Id,
                BranchId = c.BranchId,
                BranchName = c.Branch != null ? c.Branch.Name : null,
                Title = c.Title,
                Body = c.Body,
                LinkType = c.LinkType,
                LinkValue = c.LinkValue,
                ScheduledAt = c.ScheduledAt,
                SentAt = c.SentAt,
                Status = c.Status,
                SentCount = c.SentCount,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<PushCampaignDto> CreatePushCampaignAsync(SavePushCampaignRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var campaign = new ShopPushCampaign { OrganizationId = orgId, CreatedByUserId = _currentUser.UserId };
        await ApplyCampaign(campaign, r, orgId, ct);
        _db.ShopPushCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);
        return await GetCampaignDto(campaign.Id, ct);
    }

    public async Task<PushCampaignDto> UpdatePushCampaignAsync(Guid id, SavePushCampaignRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var campaign = await RequireEditableCampaign(id, orgId, ct);
        await ApplyCampaign(campaign, r, orgId, ct);
        await _db.SaveChangesAsync(ct);
        return await GetCampaignDto(id, ct);
    }

    public async Task DeletePushCampaignAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var campaign = await RequireEditableCampaign(id, orgId, ct);
        _db.ShopPushCampaigns.Remove(campaign);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PushCampaignDto> SendPushCampaignNowAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var campaign = await RequireEditableCampaign(id, orgId, ct);

        if (await PushCampaignRules.SentTodayAsync(_db, orgId, DateTime.UtcNow, ct) >= MaxCampaignsPerDay)
            throw new ValidationException($"Kuniga ko'pi bilan {MaxCampaignsPerDay} ta push kampaniya yuborish mumkin.");

        campaign.Status = PushCampaignStatus.Scheduled;
        campaign.ScheduledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetCampaignDto(id, ct);
    }

    private async Task ApplyCampaign(ShopPushCampaign c, SavePushCampaignRequest r, Guid orgId, CancellationToken ct)
    {
        var title = Required(r.Title, "Sarlavha", 60);
        var body = Required(r.Body, "Matn", 200);
        await ValidateBranch(r.BranchId, orgId, ct);
        var link = await ValidateLink(r.LinkType, r.LinkValue, orgId, ct);
        var scheduledAt = Utc(r.ScheduledAt);
        if (scheduledAt is { } at && at < DateTime.UtcNow.AddMinutes(-1))
            throw new ValidationException("Yuborish vaqti o'tib ketgan.");

        c.BranchId = r.BranchId;
        c.Title = title;
        c.Body = body;
        c.LinkType = r.LinkType;
        c.LinkValue = link;
        c.ScheduledAt = scheduledAt;
        c.Status = scheduledAt is null ? PushCampaignStatus.Draft : PushCampaignStatus.Scheduled;
    }

    private async Task<ShopPushCampaign> RequireEditableCampaign(Guid id, Guid orgId, CancellationToken ct)
    {
        var campaign = await _db.ShopPushCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, ct)
                       ?? throw new NotFoundException("Kampaniya topilmadi.");
        if (campaign.Status is not (PushCampaignStatus.Draft or PushCampaignStatus.Scheduled))
            throw new ValidationException("Yuborilgan kampaniyani o'zgartirib bo'lmaydi.");
        return campaign;
    }

    private async Task<PushCampaignDto> GetCampaignDto(Guid id, CancellationToken ct) =>
        (await GetPushCampaignsAsync(ct)).FirstOrDefault(c => c.Id == id)
        ?? throw new NotFoundException("Kampaniya topilmadi.");

    // ================= yordamchilar =================

    private async Task ValidateBranch(Guid? branchId, Guid orgId, CancellationToken ct)
    {
        if (branchId is not { } id) return;
        if (!await _db.Branches.AnyAsync(b => b.Id == id && b.OrganizationId == orgId, ct))
            throw new ValidationException("Filial topilmadi.");
    }

    /// <summary>Havola turi va qiymati; mahsulot/kategoriya/kolleksiya shu tashkilotga tegishli bo'lishi shart.</summary>
    private async Task<string?> ValidateLink(ShopLinkType type, string? value, Guid orgId, CancellationToken ct)
    {
        var link = ShopLinks.Normalize(type, value);
        if (link is null || type == ShopLinkType.Url) return link;

        var id = Guid.Parse(link);
        var exists = type switch
        {
            ShopLinkType.Product => await _db.Products.AnyAsync(p => p.Id == id && p.OrganizationId == orgId, ct),
            ShopLinkType.Category => await _db.Categories.AnyAsync(c => c.Id == id && c.OrganizationId == orgId, ct),
            ShopLinkType.Collection => await _db.ShopCollections.AnyAsync(c => c.Id == id && c.OrganizationId == orgId, ct),
            _ => true
        };
        if (!exists) throw new ValidationException("Havoladagi obyekt topilmadi.");
        return link;
    }

    private static void ApplyOrder<T>(List<T> rows, IReadOnlyList<Guid> ids, Func<T, Guid> key, Action<T, int> set)
    {
        var order = (ids ?? []).Distinct().ToList();
        if (order.Any(id => rows.All(r => key(r) != id)))
            throw new ValidationException("Ro'yxatda noma'lum ID bor.");
        for (var i = 0; i < order.Count; i++)
            set(rows.First(r => key(r) == order[i]), i);
        // Ro'yxatda yo'qlari — oxiriga (avvalgi tartibda).
        var next = order.Count;
        foreach (var r in rows.Where(r => !order.Contains(key(r))))
            set(r, next++);
    }

    private static void ValidatePeriod(DateTime? starts, DateTime? ends)
    {
        if (starts is { } s && ends is { } e && Utc(e) <= Utc(s))
            throw new ValidationException("Tugash vaqti boshlanishidan keyin bo'lishi kerak.");
    }

    private static DateTime? Utc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };

    private static string Required(string? value, string field, int max)
    {
        var v = NullIfEmpty(value) ?? throw new ValidationException($"{field} kiritilishi shart.");
        if (v.Length > max) throw new ValidationException($"{field} {max} belgidan oshmasligi kerak.");
        return v;
    }

    private static string? Optional(string? value, string field, int max)
    {
        var v = NullIfEmpty(value);
        if (v is { Length: var len } && len > max) throw new ValidationException($"{field} {max} belgidan oshmasligi kerak.");
        return v;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
