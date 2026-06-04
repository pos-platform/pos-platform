using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PosPlatform.Api.Middleware;
using PosPlatform.Api.Services;
using PosPlatform.Application;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Infrastructure;
using PosPlatform.Infrastructure.Authentication;
using PosPlatform.Infrastructure.Persistence;

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

// Joriy foydalanuvchi (JWT) — HttpContext orqali.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PosPlatform API", Version = "v1" });

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
    });

builder.Services.AddAuthorization();

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
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"status\":429,\"error\":\"Juda ko'p urinish. Bir daqiqa kutib, qayta urinib ko'ring.\"}", token);
    };
});

// --- CORS (Vue dev server) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
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

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Obuna/hisob holatini har bir so'rovda qayta tekshiramiz (auth'dan keyin).
app.UseMiddleware<LicenseValidationMiddleware>();

app.MapControllers();

// Health check — autentifikatsiyasiz, monitoring uchun.
app.MapHealthChecks("/health").AllowAnonymous();

// SPA fallback: API/statik fayllarga tushmagan so'rovlar Vue index.html'ga (client routing).
app.MapFallbackToFile("index.html");

app.Run();
