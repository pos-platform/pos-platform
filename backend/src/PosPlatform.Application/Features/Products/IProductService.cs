using PosPlatform.Application.Features.Products.Dtos;

namespace PosPlatform.Application.Features.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Noyob shtrix-kod (kod tovara) generatsiya qiladi.</summary>
    Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default);

    /// <summary>Keyingi bo'sh PLU raqami (mavjud maksimal + 1).</summary>
    Task<int> GetNextPluAsync(CancellationToken cancellationToken = default);
}
