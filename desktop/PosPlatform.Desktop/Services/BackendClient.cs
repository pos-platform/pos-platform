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

    public async Task<List<ProductDto>> GetProductsAsync()
        => (await _http.GetFromJsonAsync<List<ProductDto>>("api/products", Json)) ?? new();

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

    public async Task<List<DebtorDto>> GetDebtorsAsync()
        => (await _http.GetFromJsonAsync<List<DebtorDto>>("api/debtors", Json)) ?? new();

    public async Task<SaleDto> RefundAsync(Guid saleId)
    {
        var resp = await _http.PostAsync($"api/sales/{saleId}/refund", null);
        await EnsureOk(resp);
        return (await resp.Content.ReadFromJsonAsync<SaleDto>(Json))!;
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
