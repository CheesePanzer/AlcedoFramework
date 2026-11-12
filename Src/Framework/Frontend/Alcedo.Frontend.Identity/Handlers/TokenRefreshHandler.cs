using System.Net;
using System.Text.Json;
using Alcedo.Frontend.Identity.Exceptions;
using Alcedo.Frontend.Identity.Providers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;

namespace Alcedo.Frontend.Identity.Handlers;

public class TokenRefreshHandler(
    AuthStateProvider authState,
    IJSRuntime js,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (IsAuthEndpoint(request.RequestUri))
        {
            return await base.SendAsync(request, ct);
        }

        var oldToken = await authState.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(oldToken))
        {
            request.Headers.Authorization = new("Bearer", oldToken);
        }

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        string? clientAction = null;
        string rawBody = string.Empty;
        try
        {
            rawBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(rawBody);
            if (doc.RootElement.TryGetProperty("clientAction", out var actionProp))
            {
                clientAction = actionProp.GetString();
            }
        }
        catch
        {
            //Ignore
        }

        if (clientAction != "RefreshAccessToken")
        {
            await authState.ClearAsync();
            response.Content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            return response;
        }

        await Lock.WaitAsync(ct);
        try
        {
            var currentToken = await authState.GetAccessTokenAsync();

            if (currentToken != oldToken && !string.IsNullOrEmpty(currentToken))
            {
                var retryRequest = await CloneRequestAsync(request, currentToken);
                return await base.SendAsync(retryRequest, ct);
            }

            var refreshHttp = httpClientFactory.CreateClient("refresh");

            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/jwt:refresh")
            {
                Content = null
            };

            refreshRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            string idempotencyKey = Guid.NewGuid().ToString("N");
            refreshRequest.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var refreshResponse = await refreshHttp.SendAsync(refreshRequest, ct);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                throw new AuthenticationExceptions("Failed to refresh token");
            }

            var accessToken = await refreshResponse.Content.ReadAsStringAsync(ct);
            accessToken = accessToken.Trim('"');

            await authState.SetTokenAsync(accessToken);

            var finalRetryRequest = await CloneRequestAsync(request, accessToken);
            return await base.SendAsync(finalRetryRequest, ct);
        }
        catch
        {
            await authState.ClearAsync();
            return response;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request, string token)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        clone.Headers.Authorization = new("Bearer", token);

        foreach (var header in request.Headers.Where(h => h.Key != "Authorization"))
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static bool IsAuthEndpoint(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        IReadOnlyCollection<string> excludedPaths =
        [
            "/api/v1/identity/auth/login",
            "/api/v1/identity/auth/logout",
            "/api/v1/identity/auth/jwt:refresh"
        ];

        var path = uri.AbsolutePath;

        return excludedPaths.Any(excludedPath =>
            path.Contains(excludedPath, StringComparison.InvariantCultureIgnoreCase));
    }
}