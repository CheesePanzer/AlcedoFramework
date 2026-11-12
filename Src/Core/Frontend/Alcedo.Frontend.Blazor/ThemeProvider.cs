using Microsoft.JSInterop;

namespace Alcedo.Frontend.Blazor;

public class ThemeStateProvider(IJSRuntime JS)
{
    public event Func<Task>? OnThemeChanged;

    public async Task<bool> IsDarkMode()
    {
        var saved = await JS.InvokeAsync<string?>("localStorage.getItem", "dark_mode");
        Console.WriteLine(saved);
        return saved == "true";
    }

    public async Task SetDarkMode()
    {
        await JS.InvokeVoidAsync("localStorage.setItem", "dark_mode", "true");
        Console.WriteLine("Set dark mode");
        OnThemeChanged?.Invoke();
    }

    public async Task UnsetDarkMode()
    {
        await JS.InvokeVoidAsync("localStorage.setItem", "dark_mode", "false");
        Console.WriteLine("UnSet dark mode");
        OnThemeChanged?.Invoke();
    }
}