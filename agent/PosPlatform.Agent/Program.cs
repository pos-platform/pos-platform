using PosPlatform.Agent.Authentication;
using PosPlatform.Agent.Backend;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Devices;
using PosPlatform.Agent.Workers;

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

// Fiskal qurilma: UseSimulatedFiscal=true bo'lsa simulyatsiya, aks holda haqiqiy OFD API.
if (agentOptions.UseSimulatedFiscal)
    builder.Services.AddSingleton<IFiscalDevice, SimulatedFiscalDevice>();
else
    builder.Services.AddSingleton<IFiscalDevice, OfdFiscalDevice>();

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

builder.Services.AddHostedService<FiscalWorker>();
builder.Services.AddHostedService<ShiftReportWorker>();

var host = builder.Build();
host.Run();
