using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Labels.Dtos;

public class LabelPrintJobDto
{
    public Guid Id { get; set; }
    public List<LabelItemDto> Items { get; set; } = new();

    public static LabelPrintJobDto FromEntity(LabelPrintJob job) => new()
    {
        Id = job.Id,
        Items = job.Items.Select(LabelItemDto.FromEntity).ToList()
    };
}

public class LabelItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Barcode { get; set; }
    public int Copies { get; set; }

    public static LabelItemDto FromEntity(LabelPrintJobItem i) => new()
    {
        ProductName = i.ProductName,
        Price = i.Price,
        Barcode = i.Barcode,
        Copies = i.Copies
    };
}

public class CreateLabelJobRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateLabelItemRequest> Items { get; set; } = new();
}

public class CreateLabelItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 100)]
    public int Copies { get; set; } = 1;
}
