using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>Yorliq (narx chiptasi) chop etish vazifasi — agent polling orqali oladi.</summary>
public class LabelPrintJob : BaseEntity
{
    public bool Printed { get; set; }
    public DateTime? PrintedAt { get; set; }

    public ICollection<LabelPrintJobItem> Items { get; set; } = new List<LabelPrintJobItem>();
}
