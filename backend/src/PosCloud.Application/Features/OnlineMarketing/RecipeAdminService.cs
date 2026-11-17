using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.OnlineMarketing;

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §1): retseptlarni admin panelda boshqarish.

public class SaveRecipeItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? Note { get; set; }
}

public class SaveRecipeRequest
{
    public Guid? BranchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CookMinutes { get; set; }
    public int? Servings { get; set; }
    public List<string>? Steps { get; set; }
    public List<SaveRecipeItemRequest>? Items { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnHome { get; set; }
}

public class RecipeItemAdminDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsWeighted { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}

public class RecipeAdminDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CookMinutes { get; set; }
    public int? Servings { get; set; }
    public List<string> Steps { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool ShowOnHome { get; set; }
    public List<RecipeItemAdminDto> Items { get; set; } = [];
}

public interface IRecipeAdminService
{
    Task<IReadOnlyList<RecipeAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<RecipeAdminDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<RecipeAdminDto> CreateAsync(SaveRecipeRequest request, CancellationToken ct = default);
    Task<RecipeAdminDto> UpdateAsync(Guid id, SaveRecipeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class RecipeAdminService : IRecipeAdminService
{
    public const int MaxItems = 50;
    public const int MaxSteps = 30;
    public const int MaxStepLength = 500;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RecipeAdminService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid OrgId => _currentUser.OrganizationId
        ?? throw new ValidationException("Bu amal tashkilot foydalanuvchisi uchun.");

    public async Task<IReadOnlyList<RecipeAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        var recipes = await _db.ShopRecipes.AsNoTracking()
            .Where(r => r.OrganizationId == orgId)
            .Include(r => r.Branch)
            .Include(r => r.Items)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.CreatedAt)
            .ToListAsync(ct);
        return await ToDtos(recipes, orgId, ct);
    }

    public async Task<RecipeAdminDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var recipe = await _db.ShopRecipes.AsNoTracking()
                         .Include(r => r.Branch)
                         .Include(r => r.Items)
                         .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId, ct)
                     ?? throw new NotFoundException("Retsept topilmadi.");
        return (await ToDtos([recipe], orgId, ct))[0];
    }

    public async Task<RecipeAdminDto> CreateAsync(SaveRecipeRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var recipe = new ShopRecipe { OrganizationId = orgId };
        await Apply(recipe, r, orgId, ct);
        _db.ShopRecipes.Add(recipe);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(recipe.Id, ct);
    }

    public async Task<RecipeAdminDto> UpdateAsync(Guid id, SaveRecipeRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var recipe = await _db.ShopRecipes.Include(x => x.Items)
                         .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct)
                     ?? throw new NotFoundException("Retsept topilmadi.");
        await Apply(recipe, r, orgId, ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var recipe = await _db.ShopRecipes.Include(x => x.Items)
                         .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct)
                     ?? throw new NotFoundException("Retsept topilmadi.");
        _db.ShopRecipeItems.RemoveRange(recipe.Items);
        _db.ShopRecipes.Remove(recipe);
        await _db.SaveChangesAsync(ct);
    }

    private async Task Apply(ShopRecipe recipe, SaveRecipeRequest r, Guid orgId, CancellationToken ct)
    {
        var title = Clean(r.Title) ?? throw new ValidationException("Nom kiritilishi shart.");
        if (title.Length > 80) throw new ValidationException("Nom 80 belgidan oshmasligi kerak.");
        var description = Clean(r.Description);
        if (description is { Length: > 1000 }) throw new ValidationException("Tavsif 1000 belgidan oshmasligi kerak.");
        var image = Clean(r.ImageUrl);
        if (image is { Length: > 500 }) throw new ValidationException("Rasm manzili juda uzun.");
        if (r.CookMinutes is < 1 or > 1440) throw new ValidationException("Tayyorlash vaqti 1–1440 daqiqa bo'lishi kerak.");
        if (r.Servings is < 1 or > 50) throw new ValidationException("Porsiya 1–50 bo'lishi kerak.");

        var steps = (r.Steps ?? []).Select(s => Clean(s)?.Replace('\n', ' ').Replace('\r', ' ')).OfType<string>().ToList();
        if (steps.Count > MaxSteps) throw new ValidationException($"Ko'pi bilan {MaxSteps} ta qadam.");
        if (steps.Any(s => s.Length > MaxStepLength))
            throw new ValidationException($"Har bir qadam {MaxStepLength} belgidan oshmasligi kerak.");

        if (r.BranchId is { } branchId && !await _db.Branches.AnyAsync(b => b.Id == branchId && b.OrganizationId == orgId, ct))
            throw new ValidationException("Filial topilmadi.");

        var items = (r.Items ?? []).GroupBy(i => i.ProductId).Select(g => g.First()).ToList();
        if (items.Count > MaxItems) throw new ValidationException($"Retseptda ko'pi bilan {MaxItems} ta mahsulot bo'lishi mumkin.");
        if (items.Any(i => i.Quantity <= 0 || i.Quantity > 1000)) throw new ValidationException("Miqdor 0 dan katta bo'lishi kerak.");
        if (items.Any(i => Clean(i.Note) is { Length: > 80 })) throw new ValidationException("Izoh 80 belgidan oshmasligi kerak.");
        var ids = items.Select(i => i.ProductId).ToList();
        var known = await _db.Products.Where(p => p.OrganizationId == orgId && ids.Contains(p.Id)).CountAsync(ct);
        if (known != ids.Count) throw new ValidationException("Mahsulot topilmadi.");

        recipe.BranchId = r.BranchId;
        recipe.Title = title;
        recipe.Description = description;
        recipe.ImageUrl = image;
        recipe.CookMinutes = r.CookMinutes;
        recipe.Servings = r.Servings;
        recipe.Steps = string.Join('\n', steps);
        recipe.SortOrder = r.SortOrder;
        recipe.IsActive = r.IsActive;
        recipe.ShowOnHome = r.ShowOnHome;

        foreach (var old in recipe.Items.Where(i => !ids.Contains(i.ProductId)).ToList())
        {
            recipe.Items.Remove(old);
            _db.ShopRecipeItems.Remove(old);
        }
        for (var i = 0; i < items.Count; i++)
        {
            var req = items[i];
            var item = recipe.Items.FirstOrDefault(x => x.ProductId == req.ProductId);
            if (item is null)
            {
                item = new ShopRecipeItem { OrganizationId = orgId, RecipeId = recipe.Id, ProductId = req.ProductId };
                recipe.Items.Add(item);
            }
            item.Quantity = req.Quantity;
            item.Note = Clean(req.Note);
            item.SortOrder = i;
        }
    }

    private async Task<List<RecipeAdminDto>> ToDtos(List<ShopRecipe> recipes, Guid orgId, CancellationToken ct)
    {
        var productIds = recipes.SelectMany(r => r.Items.Select(i => i.ProductId)).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.OrganizationId == orgId && productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ImageUrl, p.Price, p.IsWeighed })
            .ToDictionaryAsync(p => p.Id, ct);

        return recipes.Select(r => new RecipeAdminDto
        {
            Id = r.Id,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            Title = r.Title,
            Description = r.Description,
            ImageUrl = r.ImageUrl,
            CookMinutes = r.CookMinutes,
            Servings = r.Servings,
            Steps = SplitSteps(r.Steps),
            SortOrder = r.SortOrder,
            IsActive = r.IsActive,
            ShowOnHome = r.ShowOnHome,
            Items = r.Items.OrderBy(i => i.SortOrder)
                .Where(i => products.ContainsKey(i.ProductId))
                .Select(i =>
                {
                    var p = products[i.ProductId];
                    return new RecipeItemAdminDto
                    {
                        ProductId = p.Id, Name = p.Name, ImageUrl = p.ImageUrl, Price = p.Price, IsWeighted = p.IsWeighed,
                        Quantity = i.Quantity, Note = i.Note
                    };
                }).ToList()
        }).ToList();
    }

    public static List<string> SplitSteps(string? steps) =>
        string.IsNullOrWhiteSpace(steps) ? [] : steps.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
