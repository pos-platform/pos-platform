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

// Qurilma seam'lari — hozir simulyatsiya/console; keyin haqiqiy implementatsiya bilan almashtiriladi.
builder.Services.AddSingleton<IFiscalDevice, SimulatedFiscalDevice>();
builder.Services.AddSingleton<IReceiptPrinter, ConsoleReceiptPrinter>();

builder.Services.AddHostedService<FiscalWorker>();

var host = builder.Build();
host.Run();
