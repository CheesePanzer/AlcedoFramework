using Microsoft.JSInterop;

namespace Alcedo.Frontend.Identity.Providers;

public class AuthStateProvider(IJSRuntime js)
{
    private const string AtKey = "access_token";
    private const string ValidUntilKey = "token_valid_until";
    private const string NeedPasswordChange = "need_password_change";

    public event Action? OnUnauthenticated;

    public async Task<bool> GetAuthenticationStateAsync()
    {
        var token = await js.InvokeAsync<string?>("sessionStorage.getItem", AtKey);
        var validUntilStr = await js.InvokeAsync<string?>("sessionStorage.getItem", ValidUntilKey);

        if (string.IsNullOrEmpty(token) || !TryParseValid(validUntilStr, out var validUntil) ||
            validUntil <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        return await js.InvokeAsync<string>("sessionStorage.getItem", AtKey);
    }

    public async Task<DateTimeOffset?> GetTokenValidUntilAsync()
    {
        bool success = DateTimeOffset.TryParse(await js.InvokeAsync<string?>("sessionStorage.getItem", ValidUntilKey),
            out var dt);
        if (!success)
        {
            await ClearAsync();
            return null;
        }

        return dt;
    }

    public async Task SetTokenAsync(string token)
    {
        await js.InvokeVoidAsync("sessionStorage.setItem", AtKey, token);
    }

    public async Task<bool> NeedPasswordChangeAsync()
    {
        var rawStr = await js.InvokeAsync<string?>("sessionStorage.getItem", NeedPasswordChange);
        if (string.IsNullOrEmpty(rawStr))
        {
            return false;
        }

        bool success = bool.TryParse(rawStr, out var need);

        if (!success)
        {
            return false;
        }

        return need;
    }


    public async Task LoginAsync(string token, DateTimeOffset validUntil, bool needPasswordChange)
    {
        await js.InvokeVoidAsync("sessionStorage.setItem", AtKey, token);
        await js.InvokeVoidAsync("sessionStorage.setItem", ValidUntilKey, validUntil.ToString("O"));
        await js.InvokeVoidAsync("sessionStorage.setItem", NeedPasswordChange, needPasswordChange);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", AtKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", ValidUntilKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", NeedPasswordChange);
        OnUnauthenticated?.Invoke();
    }

    private static bool TryParseValid(string? s, out DateTimeOffset result) =>
        DateTimeOffset.TryParse(s, out result);
}