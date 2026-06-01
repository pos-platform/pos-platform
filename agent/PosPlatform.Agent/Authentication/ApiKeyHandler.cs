using Microsoft.Extensions.Options;
using PosPlatform.Agent.Configuration;

namespace PosPlatform.Agent.Authentication;

public static class AgentHeaders
{
    public const string ApiKey = "X-Agent-Key";
}

/// <summary>Har bir so'rovga agent API-kalitini qo'shuvchi DelegatingHandler.</summary>
public class ApiKeyHandler : DelegatingHandler
{
    private readonly AgentOptions _options;

    public ApiKeyHandler(IOptions<AgentOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(AgentHeaders.ApiKey);
        request.Headers.Add(AgentHeaders.ApiKey, _options.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
