using System.Net.Http.Json;
using PosPlatform.Agent.Authentication;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Backend;

/// <summary>Backendning fiskal endpointlari bilan ishlaydigan typed HttpClient.</summary>
public class BackendClient
{
    private readonly HttpClient _http;

    public BackendClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<PendingReceipt>> GetPendingAsync(int max, CancellationToken cancellationToken)
    {
        var result = await _http.GetFromJsonAsync<List<PendingReceipt>>(
            $"api/fiscal/pending?max={max}", cancellationToken);

        return result ?? new List<PendingReceipt>();
    }

    public async Task PostResultAsync(Guid saleId, FiscalResult result, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync($"api/fiscal/{saleId}/result", result, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
