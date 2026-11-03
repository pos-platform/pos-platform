namespace PosCloud.Agent.Models;

public class LabelItem
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Barcode { get; set; }
    public int Copies { get; set; } = 1;
}

public class LabelPrintJobDto
{
    public Guid Id { get; set; }
    public List<LabelItem> Items { get; set; } = new();
}
