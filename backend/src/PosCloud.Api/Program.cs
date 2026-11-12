using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using PosCloud.Api.Authentication;
using PosCloud.Api.Authorization;
using PosCloud.Api.Middleware;
using PosCloud.Api.Services;
using PosCloud.Application;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Infrastructure;
using PosCloud.Infrastructure.Authentication;
using PosCloud.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Frontend (Vue) build'i shu papkadan SPA sifatida xizmat qilinadi.
    WebRootPath = "wwwroot"
});

const string CorsPolicy = "WebClient";

// --- Qatlamlarni ulash ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// To'lov shlyuzlari sozlamalari (Payme / Click).
builder.Services.AddSingleton<PosCloud.Application.Common.Interfaces.IPaymentGatewaySettings>(
    new PosCloud.Api.Services.PaymentGatewaySettings(builder.Configuration));

// Joriy foydalanuvchi (JWT) — HttpContext orqali.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- SignalR: online buyurtmalar (/hubs/orders). Enum'lar REST bilan bir xil — string. ---
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Mijozga push: navbat + fon vazifa (FCM so'rovni kutdirmaydi).
builder.Services.AddSingleton<PosCloud.Api.Background.OrderPushQueue>();
builder.Services.AddHostedService<PosCloud.Api.Background.OrderPushService>();
builder.Services.AddSingleton<IOnlineOrderNotifier, PosCloud.Api.Hubs.SignalROnlineOrderNotifier>();

// Fon vazifa: eskirgan mijoz OTP va refresh tokenlarini tozalash.
builder.Services.AddHostedService<PosCloud.Api.Background.ShopCleanupService>();
// Fon vazifa: do'kon belgilangan vaqtda qabul qilmagan "Yangi" online buyurtmalarni bekor qilish (har daqiqa).
builder.Services.AddHostedService<PosCloud.Api.Background.OnlineOrderAutoCancelService>();
// Fon vazifa: faolsizlik tufayli bonus ball muddati (kuniga bir marta).
builder.Services.AddHostedService<PosCloud.Api.Background.LoyaltyExpiryService>();

// --- Controllerlar + enum'larni string sifatida (JSON) ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();

// Health check — /health endpointga DB ulanishini tekshiradi.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        tags: ["db", "ready"]);


// --- Swagger (JWT Bearer bilan) ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PosCloud API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT tokenni kiriting (Bearer prefiks shart emas)."
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- JWT autentifikatsiya ---
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("appsettings.json da 'Jwt' bo'limi topilmadi.");

// --- Production maxfiylik tekshiruvi: standart/placeholder sirlar bilan ishga tushmaymiz ---
// Bu sozlamalarni production'da muhit o'zgaruvchilari (env) orqali bering, masalan Jwt__Key.
if (builder.Environment.IsProduction())
{
    var problems = new List<string>();
    var jwtKey = jwtSettings.Key ?? string.Empty;
    if (jwtKey.Contains("CHANGE_ME") || jwtKey.Length < 32)
        problems.Add("Jwt:Key kuchli, tasodifiy va kamida 32 belgi bo'lishi kerak");
    if ((builder.Configuration["Agent:ApiKey"] ?? string.Empty).Contains("CHANGE_ME"))
        problems.Add("Agent:ApiKey ni kuchli qiymatga almashtiring");
    if (builder.Configuration["Seed:AdminPassword"] is null or "Admin12345!")
        problems.Add("Seed:AdminPassword standart qiymatda — almashtiring");
    if (builder.Configuration["Seed:SuperAdminPassword"] is null or "Super12345!")
        problems.Add("Seed:SuperAdminPassword ni o'rnating/almashtiring");
    if ((builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty).Contains("Password=postgres"))
        problems.Add("PostgreSQL paroli standart 'postgres' — almashtiring");

    if (problems.Count > 0)
    {
        throw new InvalidOperationException(
            "Production muhitida xavfsiz emas — quyidagi maxfiy sozlamalarni to'g'rilang:\n - "
            + string.Join("\n - ", problems));
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents { OnMessageReceived = HubTokenFromQuery };
    })
    // Mobil ilova mijozi: alohida audience. Xodim sxemasi mijoz tokenini (va aksincha) qabul qilmaydi.
    .AddJwtBearer(ShopAuthDefaults.Scheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.ShopAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents { OnMessageReceived = HubTokenFromQuery };
    });

// WebSocket'da header yuborib bo'lmaydi — hub uchun token query'dan (?access_token=) olinadi. Faqat /hubs yo'lida.
static Task HubTokenFromQuery(MessageReceivedContext context)
{
    var token = context.Request.Query["access_token"];
    if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
        context.Token = token;
    return Task.CompletedTask;
}

if (string.Equals(jwtSettings.ShopAudience, jwtSettings.Audience, StringComparison.Ordinal))
    throw new InvalidOperationException("Jwt:ShopAudience Jwt:Audience dan farq qilishi shart.");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ShopAuthDefaults.Policy, policy => policy
        .AddAuthenticationSchemes(ShopAuthDefaults.Scheme)
        .RequireAuthenticatedUser()
        .RequireRole(PosCloud.Application.Features.ShopAuth.ShopRoles.Customer));
});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// --- Reverse proxy (nginx/IIS) ortida ishlash: original sxema (https) va client IP'ni o'qiymiz ---
// Ilova faqat ishonchli proxy orqali ochilishi kerak (localhost'ga bog'lanadi).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- Rate limiting: login endpointini brute-force'dan himoya (IP bo'yicha) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,        // bir IP'dan daqiqasiga 10 ta urinish
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Mobil ilova: kod so'rash (SMS pulli) — bir IP'dan daqiqasiga 5 ta.
    options.AddPolicy("otp", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"status\":429,\"error\":\"Juda ko'p urinish. Bir daqiqa kutib, qayta urinib ko'ring.\"}", token);
    };
});

// --- CORS ---
// Production'da front backend bilan bitta domendan (poscloud.uz) beriladi — same-origin,
// shuning uchun CORS shart emas. Quyidagi origin'lar dev server va front alohida
// joylangan holatlar uchun ruxsat etilgan (desktop/agent brauzer emas — ularga CORS ta'sir qilmaydi).
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(
                  "http://localhost:5173",
                  "http://localhost:5174",
                  "https://poscloud.uz",
                  "https://www.poscloud.uz")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- Migratsiya + boshlang'ich ma'lumot (admin, standart kategoriya) ---
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
    await initializer.MigrateAsync();
    await initializer.SeedAsync();
}

// Reverse proxy header'larini eng birinchi o'qiymiz (qolgan middleware to'g'ri sxema/IP ko'rishi uchun).
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Statik fayllar: Vue SPA build'i + yuklangan rasmlar (wwwroot, wwwroot/uploads).
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads"));
app.UseStaticFiles();

// Desktop (kassa) yangilanishlari — Velopack release fayllari: releases.win.json, *.nupkg, Setup.exe.
// publish-desktop-update.ps1 -Upload ularni ContentRoot/updates/velopack ga tashlaydi.
// Alohida tarmoq: topilmagan fayl SPA index.html'ga tushmay 404 qaytaradi, auth/litsenziya/rate limit
// ham qo'llanmaydi (kassa login'dan oldin tekshiradi, katta fayllar limitga urilmasin).
var velopackDir = Path.Combine(app.Environment.ContentRootPath, "updates", "velopack");
Directory.CreateDirectory(velopackDir);
app.Map("/updates/velopack", updates =>
{
    var contentTypes = new FileExtensionContentTypeProvider();
    contentTypes.Mappings[".nupkg"] = "application/octet-stream";
    updates.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(velopackDir),
        ContentTypeProvider = contentTypes,
        // releases.win.json keshlanmasin — yangi versiya kassalarga darhol ko'rinsin.
        OnPrepareResponse = ctx =>
        {
            if (ctx.File.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
    });
    updates.Run(ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });
});

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Obuna/hisob holatini har bir so'rovda qayta tekshiramiz (auth'dan keyin).
app.UseMiddleware<LicenseValidationMiddleware>();

app.MapControllers();
app.MapHub<PosCloud.Api.Hubs.OrdersHub>(PosCloud.Api.Hubs.OrdersHub.Path);

// Health check — autentifikatsiyasiz, monitoring uchun.
app.MapHealthChecks("/health").AllowAnonymous();

// SPA fallback: API/statik fayllarga tushmagan so'rovlar Vue index.html'ga (client routing).
app.MapFallbackToFile("index.html");

app.Run();
