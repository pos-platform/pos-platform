namespace PosCloud.Application.Features.Audit.Dtos;

/// <summary>Fiskal audit filtri uchun tanlov ro'yxatlari (tashkilotlar va kassalar).</summary>
public class FiscalFilterOptionsDto
{
    public IReadOnlyList<OrganizationOptionDto> Organizations { get; set; } = Array.Empty<OrganizationOptionDto>();
    public IReadOnlyList<string> Terminals { get; set; } = Array.Empty<string>();
}

public class OrganizationOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
