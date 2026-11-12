using System.Net.Http.Json;
using System.Text.Json;
using Alcedo.Frontend.Abstraction;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Alcedo.Frontend.Core.HttpClient;

public class IdempotencyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method == HttpMethod.Post)
        {
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }

        return base.SendAsync(request, ct);
    }
}

public class CredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}

public class ApiErrorHandler() : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        string detail;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            detail = body.GetProperty("detail").GetString()!;
        }
        catch (Exception ex)
        {
            detail = $"Unexpected error ({ex.Message})";
        }

        throw new ApiBusinessException(detail);
    }
}