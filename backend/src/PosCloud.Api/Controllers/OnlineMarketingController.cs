using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.OnlineMarketing;
using PosCloud.Application.Features.OnlineMarketing.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>
/// Do'kon admini: ilova reklamasi — bannerlar, kolleksiyalar, stories, push kampaniyalar.
/// Ruxsatlar online do'kon sozlamalari bilan bir xil: o'qish — sales.view, o'zgartirish — settings.manage.
/// Rasmlar mavjud POST /api/products/upload-image orqali yuklanadi.
/// </summary>
[ApiController]
[Authorize]
[Route("api/online-store")]
public class OnlineMarketingController : ControllerBase
{
    private readonly IOnlineMarketingService _service;

    public OnlineMarketingController(IOnlineMarketingService service)
    {
        _service = service;
    }

    // ---------------- bannerlar ----------------

    [HttpGet("banners")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<BannerAdminDto>>> GetBanners(CancellationToken ct) =>
        Ok(await _service.GetBannersAsync(ct));

    [HttpPost("banners")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<BannerAdminDto>> CreateBanner(SaveBannerRequest request, CancellationToken ct) =>
        Ok(await _service.CreateBannerAsync(request, ct));

    /// <summary>Tartib: { ids } — yangi tartibda.</summary>
    [HttpPut("banners/reorder")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<IReadOnlyList<BannerAdminDto>>> ReorderBanners(ReorderRequest request, CancellationToken ct) =>
        Ok(await _service.ReorderBannersAsync(request.Ids, ct));

    [HttpPut("banners/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<BannerAdminDto>> UpdateBanner(Guid id, SaveBannerRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateBannerAsync(id, request, ct));

    [HttpDelete("banners/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken ct)
    {
        await _service.DeleteBannerAsync(id, ct);
        return NoContent();
    }

    // ---------------- retseptlar (11.8) ----------------

    [HttpGet("recipes")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<RecipeAdminDto>>> GetRecipes([FromServices] IRecipeAdminService recipes,
        CancellationToken ct) => Ok(await recipes.GetAllAsync(ct));

    [HttpGet("recipes/{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<RecipeAdminDto>> GetRecipe(Guid id, [FromServices] IRecipeAdminService recipes,
        CancellationToken ct) => Ok(await recipes.GetAsync(id, ct));

    [HttpPost("recipes")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<RecipeAdminDto>> CreateRecipe(SaveRecipeRequest request,
        [FromServices] IRecipeAdminService recipes, CancellationToken ct) => Ok(await recipes.CreateAsync(request, ct));

    [HttpPut("recipes/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<RecipeAdminDto>> UpdateRecipe(Guid id, SaveRecipeRequest request,
        [FromServices] IRecipeAdminService recipes, CancellationToken ct) => Ok(await recipes.UpdateAsync(id, request, ct));

    [HttpDelete("recipes/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> DeleteRecipe(Guid id, [FromServices] IRecipeAdminService recipes, CancellationToken ct)
    {
        await recipes.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---------------- kolleksiyalar ----------------

    [HttpGet("collections")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<CollectionAdminDto>>> GetCollections(CancellationToken ct) =>
        Ok(await _service.GetCollectionsAsync(ct));

    [HttpGet("collections/{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<CollectionAdminDto>> GetCollection(Guid id, CancellationToken ct) =>
        Ok(await _service.GetCollectionAsync(id, ct));

    [HttpPost("collections")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<CollectionAdminDto>> CreateCollection(SaveCollectionRequest request, CancellationToken ct) =>
        Ok(await _service.CreateCollectionAsync(request, ct));

    [HttpPut("collections/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<CollectionAdminDto>> UpdateCollection(Guid id, SaveCollectionRequest request,
        CancellationToken ct) =>
        Ok(await _service.UpdateCollectionAsync(id, request, ct));

    [HttpDelete("collections/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
    {
        await _service.DeleteCollectionAsync(id, ct);
        return NoContent();
    }

    // ---------------- stories ----------------

    [HttpGet("stories")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<StoryAdminDto>>> GetStories(CancellationToken ct) =>
        Ok(await _service.GetStoriesAsync(ct));

    [HttpGet("stories/{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<StoryAdminDto>> GetStory(Guid id, CancellationToken ct) =>
        Ok(await _service.GetStoryAsync(id, ct));

    [HttpPost("stories")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<StoryAdminDto>> CreateStory(SaveStoryRequest request, CancellationToken ct) =>
        Ok(await _service.CreateStoryAsync(request, ct));

    [HttpPut("stories/reorder")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<IReadOnlyList<StoryAdminDto>>> ReorderStories(ReorderRequest request, CancellationToken ct) =>
        Ok(await _service.ReorderStoriesAsync(request.Ids, ct));

    [HttpPut("stories/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<StoryAdminDto>> UpdateStory(Guid id, SaveStoryRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateStoryAsync(id, request, ct));

    [HttpDelete("stories/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> DeleteStory(Guid id, CancellationToken ct)
    {
        await _service.DeleteStoryAsync(id, ct);
        return NoContent();
    }

    // ---------------- push kampaniyalar ----------------

    [HttpGet("push-campaigns")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<PushCampaignDto>>> GetPushCampaigns(CancellationToken ct) =>
        Ok(await _service.GetPushCampaignsAsync(ct));

    /// <summary>scheduledAt berilsa — Scheduled, aks holda Draft.</summary>
    [HttpPost("push-campaigns")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<PushCampaignDto>> CreatePushCampaign(SavePushCampaignRequest request, CancellationToken ct) =>
        Ok(await _service.CreatePushCampaignAsync(request, ct));

    /// <summary>Faqat Draft/Scheduled.</summary>
    [HttpPut("push-campaigns/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<PushCampaignDto>> UpdatePushCampaign(Guid id, SavePushCampaignRequest request,
        CancellationToken ct) =>
        Ok(await _service.UpdatePushCampaignAsync(id, request, ct));

    /// <summary>Faqat Draft/Scheduled.</summary>
    [HttpDelete("push-campaigns/{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> DeletePushCampaign(Guid id, CancellationToken ct)
    {
        await _service.DeletePushCampaignAsync(id, ct);
        return NoContent();
    }

    /// <summary>Hozir yuborish (kuniga ko'pi bilan 3 ta). Yuborish fon xizmatida ~30 soniyada boshlanadi.</summary>
    [HttpPost("push-campaigns/{id:guid}/send")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<PushCampaignDto>> SendPushCampaign(Guid id, CancellationToken ct) =>
        Ok(await _service.SendPushCampaignNowAsync(id, ct));
}
