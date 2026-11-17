using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Features.OnlineMarketing;
using PosCloud.Application.Features.ShopCatalog.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.ShopCatalog;

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §1): ilovada retseptlar.

public class ShopRecipeBriefDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int? CookMinutes { get; set; }
    public int? Servings { get; set; }
    public int ItemCount { get; set; }
}

public class ShopRecipeItemDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Note { get; set; }

    /// <summary>Filialda sotiladi va qoldig'i bor. Aks holda <see cref="Product"/> null bo'lishi mumkin.</summary>
    public bool Available { get; set; }

    public ShopProductDto? Product { get; set; }
}

public class ShopRecipeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CookMinutes { get; set; }
    public int? Servings { get; set; }
    public List<string> Steps { get; set; } = [];
    public List<ShopRecipeItemDto> Items { get; set; } = [];
}

public partial class ShopCatalogService
{
    private const int HomeRecipeLimit = 5;
    private const int RecipeListLimit = 100;

    private IQueryable<ShopRecipe> ActiveRecipes(Guid orgId, Guid branchId) =>
        _db.ShopRecipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.OrganizationId == orgId && r.IsActive && (r.BranchId == null || r.BranchId == branchId));

    private static IQueryable<ShopRecipeBriefDto> ToBrief(IQueryable<ShopRecipe> q) =>
        q.OrderBy(r => r.SortOrder).ThenBy(r => r.CreatedAt)
            .Select(r => new ShopRecipeBriefDto
            {
                Id = r.Id,
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                CookMinutes = r.CookMinutes,
                Servings = r.Servings,
                ItemCount = r.Items.Count
            });

    public async Task<IReadOnlyList<ShopRecipeBriefDto>> GetRecipesAsync(Guid branchId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        return await ToBrief(ActiveRecipes(orgId, branchId)).Take(RecipeListLimit).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, ShopProductDto>> GetProductsByIdsAsync(Guid branchId,
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<Guid, ShopProductDto>();
        var orgId = await RequireStoreOrganization(branchId, ct);
        var promotions = await LoadActivePromotions(orgId, ct);
        return await LoadByIdsAsync(orgId, branchId, ids.Distinct().ToList(), false, promotions, ct);
    }

    private Task<List<ShopRecipeBriefDto>> HomeRecipesAsync(Guid orgId, Guid branchId, CancellationToken ct) =>
        ToBrief(ActiveRecipes(orgId, branchId).Where(r => r.ShowOnHome)).Take(HomeRecipeLimit).ToListAsync(ct);

    public async Task<ShopRecipeDto> GetRecipeAsync(Guid branchId, Guid recipeId, Guid? shopUserId, CancellationToken ct = default)
    {
        var orgId = await RequireStoreOrganization(branchId, ct);
        var recipe = await ActiveRecipes(orgId, branchId).Include(r => r.Items)
                         .FirstOrDefaultAsync(r => r.Id == recipeId, ct)
                     ?? throw new NotFoundException("Retsept topilmadi.");

        var items = recipe.Items.OrderBy(i => i.SortOrder).ToList();
        var ids = items.Select(i => i.ProductId).ToList();
        var promotions = await LoadActivePromotions(orgId, ct);
        var products = await LoadByIdsAsync(orgId, branchId, ids, false, promotions, ct);
        var names = await _db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.OrganizationId == orgId && ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var dto = new ShopRecipeDto
        {
            Id = recipe.Id,
            Title = recipe.Title,
            Description = recipe.Description,
            ImageUrl = recipe.ImageUrl,
            CookMinutes = recipe.CookMinutes,
            Servings = recipe.Servings,
            Steps = RecipeAdminService.SplitSteps(recipe.Steps),
            Items = items.Where(i => names.ContainsKey(i.ProductId)).Select(i =>
            {
                var p = products.GetValueOrDefault(i.ProductId);
                return new ShopRecipeItemDto
                {
                    ProductId = i.ProductId,
                    Name = p?.Name ?? names[i.ProductId],
                    Quantity = i.Quantity,
                    Note = i.Note,
                    Available = p is { InStock: true },
                    Product = p
                };
            }).ToList()
        };
        await MarkFavoritesAsync(dto.Items.Where(i => i.Product is not null).Select(i => i.Product!), shopUserId, ct);
        return dto;
    }
}
