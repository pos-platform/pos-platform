using PosCloud.Agent.Authentication;
using PosCloud.Agent.Backend;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Devices;
using PosCloud.Agent.Services;
using PosCloud.Agent.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));

var agentOptions = builder.Configuration.GetSection(AgentOptions.SectionName).Get<AgentOptions>()
    ?? new AgentOptions();

// Har bir so'rovga API-kalit qo'shadigan handler.
builder.Services.AddTransient<ApiKeyHandler>();

// Backend bilan ishlaydigan typed HttpClient.
builder.Services.AddHttpClient<BackendClient>(client =>
    {
        client.BaseAddress = new Uri(agentOptions.BackendBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ApiKeyHandler>();

// OFD API uchun alohida HttpClient (auth handler yo'q — tashqi servis).
builder.Services.AddHttpClient("ofd", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

// YT.UZ FiscalDriveAPI lokal modul uchun HttpClient (eskirgan, JSON-RPC).
builder.Services.AddHttpClient("yt-fiscal", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// YT.UZ FiscalDriveService lokal modul uchun HttpClient (ФМ 0400, REST-API).
builder.Services.AddHttpClient("fds-fiscal", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Fiskal qurilma rejimi: FiscalMode ("Simulated"|"YtModule"|"Ofd"),
// yoki eskicha UseSimulatedFiscal (true=Simulated, false=Ofd).
var fiscalMode = agentOptions.FiscalMode?.Trim().ToLowerInvariant() switch
{
    "fiscaldriveservice" or "fds" => "fds",
    "ytmodule" or "yt"            => "yt",
    "ofd"                         => "ofd",
    "simulated" or "sim" or "test" => "simulated",
    _                             => agentOptions.UseSimulatedFiscal ? "simulated" : "ofd"
};

switch (fiscalMode)
{
    case "fds":
        builder.Services.AddSingleton<IFiscalDevice, FdsFiscalDevice>();
        break;
    case "yt":
        builder.Services.AddSingleton<IFiscalDevice, YtFiscalDevice>();
        break;
    case "ofd":
        builder.Services.AddSingleton<IFiscalDevice, OfdFiscalDevice>();
        break;
    default:
        builder.Services.AddSingleton<IFiscalDevice, SimulatedFiscalDevice>();
        break;
}

// Printer rejimi: PrinterMode ("Console"|"Network"|"Usb") yoki UseConsolePrinter (eski flag).
var printerMode = agentOptions.PrinterMode?.Trim().ToLowerInvariant() switch
{
    "network" => "network",
    "usb"     => "usb",
    "console" => "console",
    _         => agentOptions.UseConsolePrinter ? "console" : "network"
};

switch (printerMode)
{
    case "network":
        builder.Services.AddSingleton<IReceiptPrinter, EscPosPrinter>();
        break;
    case "usb":
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("USB printer rejimi faqat Windows da ishlaydi.");
        builder.Services.AddSingleton<IReceiptPrinter, UsbReceiptPrinter>();
        break;
    default:
        builder.Services.AddSingleton<IReceiptPrinter, ConsoleReceiptPrinter>();
        break;
}

// Lokal fiskalizatsiya daftari — takroriy (dublikat) fiskal chekni to'sadi.
builder.Services.AddSingleton<FiscalLedger>();

builder.Services.AddHostedService<FiscalWorker>();
builder.Services.AddHostedService<ShiftReportWorker>();
builder.Services.AddHostedService<LabelWorker>();

var host = builder.Build();
host.Run();
