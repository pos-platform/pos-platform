using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Backend qaytargan HTTP xatosi. <see cref="StatusCode"/> bo'yicha xatoni ajratamiz:
/// 400/404/409 — so'rovning o'zida muammo, qayta yuborish natija bermaydi;
/// 5xx, 408, 429, 401/403 — vaqtinchalik, keyinroq qayta urinib ko'rish mumkin.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message) : base(message)
        => StatusCode = statusCode;

    /// <summary>Xato vaqtinchalikmi (qayta yuborish ma'noga egami).</summary>
    public bool IsTransient =>
        (int)StatusCode >= 500
        || StatusCode == HttpStatusCode.RequestTimeout
        || (int)StatusCode == 429
        || StatusCode == HttpStatusCode.Unauthorized
        || StatusCode == HttpStatusCode.Forbidden;
}

/// <summary>Backend REST API bilan ishlaydigan klient (online-first).</summary>
public class BackendClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public BackendClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Tizimga kiradi. <paramref name="terminalId"/> — Sozlamalardagi kassa ID; loginga kassa
    /// biriktirilgan bo'lsa server mos kelmasa kirishni rad etadi (bitta login = bitta kassa).
    /// </summary>
    public async Task<AuthResponse> LoginAsync(string username, string password, string? terminalId = null)
    {
        // Qurilma ID ham ketadi: login birinchi kirgan kompyuterga bog'lanadi va keyin
        // boshqa kompyuterdan shu login/parol bilan kirib bo'lmaydi.
        var resp = await _http.PostAsJsonAsync("api/auth/login", new
        {
            username,
            password,
            terminalId,
            deviceId = DeviceIdentity.Get(),
            deviceName = DeviceIdentity.MachineName
        });
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    /// <summary>Joriy foydalanuvchi o'z parolini o'zgartiradi (joriy parolni tasdiqlab).</summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword });
        await EnsureOk(resp);
    }

    public async Task<List<ProductDto>> GetProductsAsync()
        => (await _http.GetFromJsonAsync<List<ProductDto>>("api/products/pos", Json)) ?? new();

    /// <summary>
    /// Katalog tamg'asi — mahsulot/kategoriya soni va oxirgi o'zgarish vaqti.
    /// Javob juda kichik, shu sabab qisqa muddat (8 sek) qo'yiladi: sekin tarmoqda
    /// kassa sinxronlash sikli shu so'rovni kutib qolmasin.
    /// </summary>
    public async Task<PosCatalogStamp?> GetPosCatalogStampAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var resp = await _http.GetAsync("api/products/pos/stamp", cts.Token);
        await EnsureOk(resp);
        return await resp.Content.ReadFromJsonAsync<PosCatalogStamp>(Json, cts.Token);
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
        => (await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories", Json)) ?? new();

    public async Task<ShiftDto?> GetCurrentShiftAsync()
    {
        var resp = await _http.GetAsync("api/shifts/current");
        if (resp.StatusCode == HttpStatusCode.NoContent) return null;
        await EnsureOk(resp);
        return await resp.Content.ReadFromJsonAsync<ShiftDto>(Json);
    }

    public async Task<ShiftDto> OpenShiftAsync(decimal openingCash)
    {
        var resp = await _http.PostAsJsonAsync("api/shifts/open",
            new { openingCash, terminalId = AppState.TerminalId });
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<ShiftDto>(Json))!;
    }

    /// <summary>Smenani yopadi va Z-hisobot ma'lumotini qaytaradi.</summary>
    /// <param name="printedLocally">true bo'lsa hisobot shu kassada chop etiladi —
    /// backend uni agent navbatiga qo'ymaydi (ikkita hisobot chiqmasligi uchun).</param>
    public async Task<ShiftDto?> CloseShiftAsync(decimal countedCash, bool printedLocally = false)
    {
        var resp = await _http.PostAsJsonAsync("api/shifts/close", new { countedCash, printedLocally });
        await EnsureOk(resp);
        return await resp.Content.ReadFromJsonAsync<ShiftDto>(Json);
    }

    /// <summary>Savdo yaratadi. <paramref name="cancellationToken"/> bilan kassa o'z
    /// muddatini qo'ya oladi — javob kechiksa kutib qolmasdan oflayn navbatga o'tadi.</summary>
    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("api/sales", request, cancellationToken);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<SaleDto>(Json, cancellationToken))!;
    }

    public async Task<List<SaleListDto>> GetSalesAsync()
        => (await _http.GetFromJsonAsync<List<SaleListDto>>("api/sales", Json)) ?? new();

    /// <summary>Tanlangan kun savdolari. Kun chegarasi sana-only yuboriladi (backend UTC kalendar kuni deb oladi).</summary>
    public async Task<List<SaleListDto>> GetSalesByDayAsync(DateTime day)
    {
        var from = day.Date.ToString("yyyy-MM-dd");
        var to = day.Date.AddDays(1).ToString("yyyy-MM-dd");
        var url = $"api/sales?from={from}&to={to}";
        return (await _http.GetFromJsonAsync<List<SaleListDto>>(url, Json)) ?? new();
    }

    /// <summary>Bitta savdoni (fiskal chek ma'lumoti bilan) Id bo'yicha oladi.</summary>
    public async Task<SaleDto?> GetSaleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resp = await _http.GetAsync($"api/sales/{id}", cancellationToken);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureOk(resp);
        return await resp.Content.ReadFromJsonAsync<SaleDto>(Json, cancellationToken);
    }

    public async Task<List<DebtorDto>> GetDebtorsAsync()
        => (await _http.GetFromJsonAsync<List<DebtorDto>>("api/debtors", Json)) ?? new();

    public async Task<SaleDto> RefundAsync(Guid saleId)
    {
        var resp = await _http.PostAsync($"api/sales/{saleId}/refund", null);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<SaleDto>(Json))!;
    }

    // ===================== Restoran =====================

    public async Task<List<TableDto>> GetTablesAsync()
        => (await _http.GetFromJsonAsync<List<TableDto>>("api/tables", Json)) ?? new();

    public async Task<List<DishDto>> GetDishesAsync()
        => (await _http.GetFromJsonAsync<List<DishDto>>("api/dishes", Json)) ?? new();

    public async Task<OrderDto> OpenOrderAsync(OpenOrderRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/sales/orders/open", request);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<OrderDto>(Json))!;
    }

    public async Task<OrderDto?> GetOpenOrderByTableAsync(Guid tableId)
    {
        var resp = await _http.GetAsync($"api/sales/orders/by-table/{tableId}");
        if (resp.StatusCode == HttpStatusCode.NoContent) return null;
        await EnsureOk(resp);
        return await resp.Content.ReadFromJsonAsync<OrderDto>(Json);
    }

    public async Task<OrderDto> SaveOrderItemsAsync(Guid orderId, List<OrderItemRequest> items)
    {
        var resp = await _http.PutAsJsonAsync($"api/sales/orders/{orderId}/items", new { items });
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<OrderDto>(Json))!;
    }

    public async Task SendToKitchenAsync(Guid orderId)
    {
        var resp = await _http.PostAsync($"api/sales/orders/{orderId}/send-to-kitchen", null);
        await EnsureOk(resp);
    }

    public async Task<SaleDto> PayOrderAsync(Guid orderId, PayOrderRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/sales/orders/{orderId}/pay", request);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<SaleDto>(Json))!;
    }

    public async Task CancelOrderAsync(Guid orderId)
    {
        var resp = await _http.PostAsync($"api/sales/orders/{orderId}/cancel", null);
        await EnsureOk(resp);
    }

    /// <summary>Backend tirik ekanligini tekshiradi (/health endpoint).</summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resp = await _http.GetAsync("health", cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnsureOk(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        var msg = body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e))
                msg = e.GetString() ?? body;
        }
        catch { /* ignore */ }
        // Token eskirganda backend bo'sh tanali 401 qaytaradi — kassirga tushunarli xabar.
        if (resp.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(msg))
            throw new ApiException(resp.StatusCode, "Sessiya muddati tugadi, qayta login qilib kiring.");
        throw new ApiException(resp.StatusCode,
            string.IsNullOrWhiteSpace(msg) ? $"Server xatosi: {(int)resp.StatusCode}" : msg);
    }
}
