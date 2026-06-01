namespace PosPlatform.Agent.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>Backend API manzili, masalan http://localhost:5000.</summary>
    public string BackendBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Backend "Agent:ApiKey" qiymati bilan mos kalit.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Navbatni tekshirish oralig'i (soniya).</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Bir tsiklda olinadigan cheklar soni.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Fiskal modul (terminal) identifikatori — simulyatsiya uchun.</summary>
    public string FiscalModuleId { get; set; } = "SIM-FISCAL-0001";
}
