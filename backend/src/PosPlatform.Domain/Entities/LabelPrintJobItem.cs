using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

public class LabelPrintJobItem : BaseEntity
{
    public Guid LabelPrintJobId { get; set; }
    public LabelPrintJob? LabelPrintJob { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Barcode { get; set; }
    public int Copies { get; set; } = 1;
}
