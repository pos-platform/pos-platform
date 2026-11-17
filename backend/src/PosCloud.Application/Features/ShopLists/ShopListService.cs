using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopAuth;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopCatalog.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.ShopLists;

// Bosqich 11.8 (docs/mobile/CONTRACT-11.md §2): oilaviy xarid ro'yxati.
// Ro'yxatlar GLOBAL (OrganizationId = null) — IgnoreQueryFilters, ruxsat egasi/a'zo bo'yicha QO'LDA.

public class ShopListBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ItemCount { get; set; }
    public int UncheckedCount { get; set; }
}

public class ShopListPersonDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class ShopListMemberDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool Joined { get; set; }
}

public class ShopListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public ShopProductDto? Product { get; set; }
    public decimal? Quantity { get; set; }
    public bool IsChecked { get; set; }
    public string? CheckedByName { get; set; }
    public string AddedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ShopListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public ShopListPersonDto Owner { get; set; } = new();
    public List<ShopListMemberDto> Members { get; set; } = [];
    public List<ShopListItemDto> Items { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}

public class SaveShopListRequest
{
    public string? Name { get; set; }
}

public class AddListMemberRequest
{
    public string Phone { get; set; } = string.Empty;
}

public class AddListItemRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public decimal? Quantity { get; set; }
}

public class UpdateListItemRequest
{
    public string? Title { get; set; }
    public decimal? Quantity { get; set; }
    public bool? IsChecked { get; set; }
}

public interface IShopListService
{
    Task<IReadOnlyList<ShopListBriefDto>> GetListsAsync(Guid userId, CancellationToken ct = default);
    Task<ShopListDto> CreateAsync(Guid userId, SaveShopListRequest request, CancellationToken ct = default);
    Task<ShopListDto> GetAsync(Guid userId, Guid listId, Guid? branchId, CancellationToken ct = default);
    Task<ShopListDto> RenameAsync(Guid userId, Guid listId, SaveShopListRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid listId, CancellationToken ct = default);
    Task<ShopListDto> AddMemberAsync(Guid userId, Guid listId, AddListMemberRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid userId, Guid listId, Guid memberId, CancellationToken ct = default);
    Task<ShopListItemDto> AddItemAsync(Guid userId, Guid listId, AddListItemRequest request, CancellationToken ct = default);
    Task<ShopListItemDto> UpdateItemAsync(Guid userId, Guid listId, Guid itemId, UpdateListItemRequest request,
        CancellationToken ct = default);
    Task DeleteItemAsync(Guid userId, Guid listId, Guid itemId, CancellationToken ct = default);
    Task ClearCheckedAsync(Guid userId, Guid listId, CancellationToken ct = default);
}

public class ShopListService : IShopListService
{
    public const int MaxMembers = 10;
    public const int MaxItems = 200;
    public const int MaxOwnedLists = 10;
    public const string DefaultName = "Oila ro'yxati";

    private readonly IApplicationDbContext _db;
    private readonly IShopCatalogService _catalog;

    public ShopListService(IApplicationDbContext db, IShopCatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    private IQueryable<ShopList> Lists => _db.ShopLists.IgnoreQueryFilters();

    public async Task<IReadOnlyList<ShopListBriefDto>> GetListsAsync(Guid userId, CancellationToken ct = default)
    {
        var me = await LoadUser(userId, ct);
        await LinkMemberships(me, ct);
        return await Lists.AsNoTracking()
            .Where(l => l.OwnerId == userId || l.Members.Any(m => m.ShopUserId == userId))
            .OrderBy(l => l.CreatedAt)
            .Select(l => new ShopListBriefDto
            {
                Id = l.Id,
                Name = l.Name,
                IsOwner = l.OwnerId == userId,
                OwnerName = ((l.Owner!.FirstName ?? "") + " " + (l.Owner.LastName ?? "")).Trim(),
                MemberCount = l.Members.Count,
                ItemCount = l.Items.Count,
                UncheckedCount = l.Items.Count(i => !i.IsChecked)
            })
            .ToListAsync(ct);
    }

    public async Task<ShopListDto> CreateAsync(Guid userId, SaveShopListRequest request, CancellationToken ct = default)
    {
        await LoadUser(userId, ct);
        if (await Lists.CountAsync(l => l.OwnerId == userId, ct) >= MaxOwnedLists)
            throw new ValidationException($"Ko'pi bilan {MaxOwnedLists} ta ro'yxat yaratish mumkin.");
        var list = new ShopList { OwnerId = userId, Name = CleanName(request.Name) ?? DefaultName, OrganizationId = null };
        _db.ShopLists.Add(list);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(userId, list.Id, null, ct);
    }

    public async Task<ShopListDto> GetAsync(Guid userId, Guid listId, Guid? branchId, CancellationToken ct = default)
    {
        var me = await LoadUser(userId, ct);
        await LinkMemberships(me, ct);
        var list = await Lists.AsNoTracking()
                       .Include(l => l.Owner)
                       .Include(l => l.Members).ThenInclude(m => m.ShopUser)
                       .Include(l => l.Items)
                       .FirstOrDefaultAsync(l => l.Id == listId, ct)
                   ?? throw new NotFoundException("Ro'yxat topilmadi.");
        EnsureAccess(list, userId);

        IReadOnlyDictionary<Guid, ShopProductDto> products = new Dictionary<Guid, ShopProductDto>();
        var productIds = list.Items.Where(i => i.ProductId != null).Select(i => i.ProductId!.Value).Distinct().ToList();
        if (branchId is { } bid && productIds.Count > 0)
        {
            try
            {
                products = await _catalog.GetProductsByIdsAsync(bid, productIds, ct);
            }
            catch (NotFoundException)
            {
                // Do'kon topilmadi / yopilgan — bandlar mahsulotsiz qaytadi.
            }
        }

        var items = list.Items
            .OrderBy(i => i.IsChecked)
            .ThenBy(i => i.IsChecked ? -(i.CheckedAt ?? i.CreatedAt).Ticks : i.SortOrder)
            .Select(i => ToItemDto(i, i.ProductId is { } pid ? products.GetValueOrDefault(pid) : null))
            .ToList();

        var updated = new[] { list.UpdatedAt ?? list.CreatedAt }
            .Concat(list.Items.Select(i => i.UpdatedAt ?? i.CreatedAt))
            .Max();

        return new ShopListDto
        {
            Id = list.Id,
            Name = list.Name,
            IsOwner = list.OwnerId == userId,
            Owner = new ShopListPersonDto { Name = DisplayName(list.Owner!), Phone = list.Owner!.Phone },
            Members = list.Members.OrderBy(m => m.CreatedAt).Select(m => new ShopListMemberDto
            {
                Id = m.Id,
                Phone = m.Phone,
                Name = m.ShopUser is { } u ? NullIfEmpty(FullName(u)) : null,
                Joined = m.ShopUserId != null
            }).ToList(),
            Items = items,
            UpdatedAt = updated
        };
    }

    public async Task<ShopListDto> RenameAsync(Guid userId, Guid listId, SaveShopListRequest request, CancellationToken ct = default)
    {
        var list = await OwnedList(userId, listId, ct);
        list.Name = CleanName(request.Name) ?? throw new ValidationException("Nom kiritilishi shart.");
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(userId, listId, null, ct);
    }

    public async Task DeleteAsync(Guid userId, Guid listId, CancellationToken ct = default)
    {
        var list = await OwnedList(userId, listId, ct);
        _db.ShopLists.Remove(list);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ShopListDto> AddMemberAsync(Guid userId, Guid listId, AddListMemberRequest request, CancellationToken ct = default)
    {
        var list = await Lists.Include(l => l.Owner).Include(l => l.Members)
                       .FirstOrDefaultAsync(l => l.Id == listId, ct)
                   ?? throw new NotFoundException("Ro'yxat topilmadi.");
        if (list.OwnerId != userId)
        {
            EnsureAccess(list, userId);
            throw new ValidationException("A'zolarni faqat ro'yxat egasi qo'sha oladi.");
        }

        var phone = ShopAuthService.NormalizePhone(request.Phone);
        if (phone == list.Owner!.Phone) throw new ValidationException("Bu sizning raqamingiz.");
        if (list.Members.Any(m => m.Phone == phone)) throw new ValidationException("Bu raqam ro'yxatda bor.");
        if (list.Members.Count >= MaxMembers) throw new ValidationException($"Ko'pi bilan {MaxMembers} ta a'zo qo'shish mumkin.");

        var memberUserId = await _db.ShopUsers.IgnoreQueryFilters()
            .Where(u => u.Phone == phone && u.DeletedAt == null)
            .Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
        _db.ShopListMembers.Add(new ShopListMember
        {
            ListId = list.Id, Phone = phone, ShopUserId = memberUserId, OrganizationId = null
        });
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(userId, listId, null, ct);
    }

    public async Task RemoveMemberAsync(Guid userId, Guid listId, Guid memberId, CancellationToken ct = default)
    {
        var list = await Lists.Include(l => l.Members).FirstOrDefaultAsync(l => l.Id == listId, ct)
                   ?? throw new NotFoundException("Ro'yxat topilmadi.");
        EnsureAccess(list, userId);
        var member = list.Members.FirstOrDefault(m => m.Id == memberId) ?? throw new NotFoundException("A'zo topilmadi.");
        if (list.OwnerId != userId && member.ShopUserId != userId)
            throw new ValidationException("A'zoni faqat ro'yxat egasi o'chira oladi.");
        _db.ShopListMembers.Remove(member);
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ShopListItemDto> AddItemAsync(Guid userId, Guid listId, AddListItemRequest request, CancellationToken ct = default)
    {
        var me = await LoadUser(userId, ct);
        var list = await AccessibleList(userId, listId, ct);
        var title = Clean(request.Title) ?? throw new ValidationException("Nomi kiritilishi shart.");
        if (title.Length > 120) throw new ValidationException("Nomi 120 belgidan oshmasligi kerak.");
        ValidateQuantity(request.Quantity);
        if (request.ProductId is { } pid && !await _db.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == pid, ct))
            throw new ValidationException("Mahsulot topilmadi.");

        var count = await _db.ShopListItems.IgnoreQueryFilters().CountAsync(i => i.ListId == listId, ct);
        if (count >= MaxItems) throw new ValidationException($"Ro'yxatda ko'pi bilan {MaxItems} ta band bo'lishi mumkin.");
        var maxSort = await _db.ShopListItems.IgnoreQueryFilters().Where(i => i.ListId == listId)
            .MaxAsync(i => (int?)i.SortOrder, ct) ?? 0;

        var item = new ShopListItem
        {
            ListId = list.Id,
            Title = title,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            AddedByUserId = userId,
            AddedByName = DisplayName(me),
            SortOrder = maxSort + 1,
            OrganizationId = null
        };
        _db.ShopListItems.Add(item);
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToItemDto(item, null);
    }

    public async Task<ShopListItemDto> UpdateItemAsync(Guid userId, Guid listId, Guid itemId, UpdateListItemRequest request,
        CancellationToken ct = default)
    {
        var me = await LoadUser(userId, ct);
        var list = await AccessibleList(userId, listId, ct);
        var item = await _db.ShopListItems.IgnoreQueryFilters()
                       .FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId, ct)
                   ?? throw new NotFoundException("Band topilmadi.");

        if (request.Title is not null)
        {
            var title = Clean(request.Title) ?? throw new ValidationException("Nomi kiritilishi shart.");
            if (title.Length > 120) throw new ValidationException("Nomi 120 belgidan oshmasligi kerak.");
            item.Title = title;
        }
        if (request.Quantity is not null)
        {
            ValidateQuantity(request.Quantity);
            item.Quantity = request.Quantity;
        }
        if (request.IsChecked is { } check && check != item.IsChecked)
        {
            item.IsChecked = check;
            item.CheckedAt = check ? DateTime.UtcNow : null;
            item.CheckedByName = check ? DisplayName(me) : null;
        }
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToItemDto(item, null);
    }

    public async Task DeleteItemAsync(Guid userId, Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var list = await AccessibleList(userId, listId, ct);
        var item = await _db.ShopListItems.IgnoreQueryFilters()
                       .FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId, ct)
                   ?? throw new NotFoundException("Band topilmadi.");
        _db.ShopListItems.Remove(item);
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearCheckedAsync(Guid userId, Guid listId, CancellationToken ct = default)
    {
        var list = await AccessibleList(userId, listId, ct);
        var done = await _db.ShopListItems.IgnoreQueryFilters()
            .Where(i => i.ListId == listId && i.IsChecked).ToListAsync(ct);
        if (done.Count == 0) return;
        _db.ShopListItems.RemoveRange(done);
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- yordamchilar ----------------

    private async Task<ShopUser> LoadUser(Guid userId, CancellationToken ct) =>
        await _db.ShopUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct)
        ?? throw new NotFoundException("Foydalanuvchi topilmadi.");

    /// <summary>Raqami bo'yicha qo'shilgan, lekin hali bog'lanmagan a'zoliklarni shu foydalanuvchiga bog'laydi.</summary>
    private async Task LinkMemberships(ShopUser me, CancellationToken ct)
    {
        var pending = await _db.ShopListMembers.IgnoreQueryFilters()
            .Where(m => m.ShopUserId == null && m.Phone == me.Phone)
            .ToListAsync(ct);
        if (pending.Count == 0) return;
        foreach (var m in pending) m.ShopUserId = me.Id;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<ShopList> AccessibleList(Guid userId, Guid listId, CancellationToken ct)
    {
        var list = await Lists.Include(l => l.Members).FirstOrDefaultAsync(l => l.Id == listId, ct)
                   ?? throw new NotFoundException("Ro'yxat topilmadi.");
        EnsureAccess(list, userId);
        return list;
    }

    private async Task<ShopList> OwnedList(Guid userId, Guid listId, CancellationToken ct)
    {
        var list = await AccessibleList(userId, listId, ct);
        if (list.OwnerId != userId) throw new ValidationException("Bu amal faqat ro'yxat egasi uchun.");
        return list;
    }

    /// <summary>Begona ro'yxat — 404 (borligini ham bildirmaymiz).</summary>
    private static void EnsureAccess(ShopList list, Guid userId)
    {
        if (list.OwnerId != userId && list.Members.All(m => m.ShopUserId != userId))
            throw new NotFoundException("Ro'yxat topilmadi.");
    }

    private static void ValidateQuantity(decimal? q)
    {
        if (q is { } v && (v <= 0 || v > 1000)) throw new ValidationException("Miqdor 0 dan katta bo'lishi kerak.");
    }

    private static ShopListItemDto ToItemDto(ShopListItem i, ShopProductDto? product) => new()
    {
        Id = i.Id,
        Title = i.Title,
        ProductId = i.ProductId,
        Product = product,
        Quantity = i.Quantity,
        IsChecked = i.IsChecked,
        CheckedByName = i.CheckedByName,
        AddedByName = i.AddedByName,
        CreatedAt = i.CreatedAt
    };

    private static string FullName(ShopUser u) => $"{u.FirstName} {u.LastName}".Trim();

    private static string DisplayName(ShopUser u)
    {
        var name = FullName(u);
        if (name.Length > 0) return name.Length > 100 ? name[..100] : name;
        // Ism yo'q — raqamning oxirgi 4 raqami.
        return u.Phone.Length >= 4 ? "•••" + u.Phone[^4..] : u.Phone;
    }

    private static string? CleanName(string? name)
    {
        var n = Clean(name);
        if (n is { Length: > 60 }) throw new ValidationException("Nom 60 belgidan oshmasligi kerak.");
        return n;
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string? NullIfEmpty(string s) => s.Length == 0 ? null : s;
}
