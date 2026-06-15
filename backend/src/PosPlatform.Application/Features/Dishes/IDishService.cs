using PosPlatform.Application.Features.Dishes.Dtos;

namespace PosPlatform.Application.Features.Dishes;

public interface IDishService
{
    /// <summary>Barcha ovqatlar (ProductType=Dish) + retsepti.</summary>
    Task<IReadOnlyList<DishDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DishDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ovqat retseptini to'liq almashtiradi (eski qatorlar o'chadi).</summary>
    Task<DishDto> SetRecipeAsync(Guid dishId, SetRecipeRequest request, CancellationToken cancellationToken = default);
}
