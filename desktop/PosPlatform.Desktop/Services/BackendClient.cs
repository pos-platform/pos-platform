using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PosPlatform.Desktop.Models;

namespace PosPlatform.Desktop.Services;

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

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/login", new { username, password });
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
        => (await _http.GetFromJsonAsync<List<ProductDto>>("api/products", Json)) ?? new();

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
        var resp = await _http.PostAsJsonAsync("api/shifts/open", new { openingCash });
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<ShiftDto>(Json))!;
    }

    public async Task CloseShiftAsync(decimal countedCash)
    {
        var resp = await _http.PostAsJsonAsync("api/shifts/close", new { countedCash });
        await EnsureOk(resp);
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/sales", request);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<SaleDto>(Json))!;
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
        throw new Exception(string.IsNullOrWhiteSpace(msg) ? $"Server xatosi: {(int)resp.StatusCode}" : msg);
    }
}
