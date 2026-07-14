namespace PosPlatform.Agent.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>Backend API manzili (cloud), masalan https://poscloud.uz.</summary>
    public string BackendBaseUrl { get; set; } = "https://poscloud.uz";

    /// <summary>Backend "Agent:ApiKey" qiymati bilan mos kalit.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Navbatni tekshirish oralig'i (soniya).</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Bir tsiklda olinadigan cheklar soni.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Fiskal modul (terminal) identifikatori.</summary>
    public string FiscalModuleId { get; set; } = "SIM-FISCAL-0001";

    /// <summary>
    /// Kassa apparatining soliq tizimidagi ro'yxatga olish raqami (RS — Ro'yxat raqami).
    /// Har bir qurilmaga bir marta beriladi. Fiskal modul javobida bo'lmasa, chekda shu qiymat chop etiladi.
    /// </summary>
    public string FiscalRegistrationNumber { get; set; } = string.Empty;

    // ─── Fiskal qurilma ──────────────────────────────────────────────────────

    /// <summary>
    /// Fiskal rejim: "Simulated" (test), "YtModule" (YT.UZ FiscalDriveAPI lokal modul),
    /// "Ofd" (umumiy OFD API). Bo'sh bo'lsa UseSimulatedFiscal flagiga qaytadi.
    /// </summary>
    public string FiscalMode { get; set; } = string.Empty;

    /// <summary>[Eskirgan] FiscalMode bo'sh bo'lganda: true = Simulated, false = Ofd.</summary>
    public bool UseSimulatedFiscal { get; set; } = true;

    /// <summary>
    /// [Eskirgan — ФМ 0400 uchun ishlamaydi] YT.UZ FiscalDriveAPI lokal JSON-RPC manzili.
    /// Standart: http://127.0.0.1:3448/rpc/api. Yangi modullar uchun FiscalDriveService ishlatiladi.
    /// </summary>
    public string FiscalDriveApiUrl { get; set; } = "http://127.0.0.1:3448/rpc/api";

    /// <summary>
    /// YT.UZ FiscalDriveService lokal REST-API manzili (ФМ 0400 uchun, FiscalDriveAPI o'rniga).
    /// Standart: http://127.0.0.1:3449.
    /// </summary>
    public string FiscalDriveServiceUrl { get; set; } = "http://127.0.0.1:3449";

    /// <summary>
    /// Fiskal modulning zavod raqami (FactoryID). Bo'sh qoldirilsa, agent
    /// <c>/FiscalDrive/List</c> orqali ulangan modulni (applet 0400) avtomatik topadi.
    /// </summary>
    public string FiscalFactoryId { get; set; } = string.Empty;

    /// <summary>Haqiqiy OFD API manzili, masalan https://ofd.soliq.uz/api.</summary>
    public string OfdApiUrl { get; set; } = "https://ofd.soliq.uz/api";

    /// <summary>OFD API autentifikatsiya tokeni (Bearer).</summary>
    public string OfdApiToken { get; set; } = string.Empty;

    // ─── Printer ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Printer rejimi: "Console" | "Network" | "Usb".
    /// Bo'sh qoldirilsa UseConsolePrinter flagiga qaytadi (ortga moslik).
    /// </summary>
    public string PrinterMode { get; set; } = string.Empty;

    /// <summary>
    /// [Eskirgan] PrinterMode bo'sh bo'lganda ishlatiladi.
    /// true = Console, false = Network.
    /// </summary>
    public bool UseConsolePrinter { get; set; } = true;

    /// <summary>Network rejimi uchun TCP manzil, masalan 192.168.1.100:9100.</summary>
    public string PrinterAddress { get; set; } = "192.168.1.100:9100";

    /// <summary>USB rejimi uchun Windows printer nomi, masalan "EPSON TM-T20 Receipt".</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>TCP ulanish timeout (ms).</summary>
    public int PrinterTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Chek kengligi belgilarda: 42 = 80mm qog'oz (standart), 32 = 57mm qog'oz.
    /// </summary>
    public int PrintWidth { get; set; } = 42;
}
