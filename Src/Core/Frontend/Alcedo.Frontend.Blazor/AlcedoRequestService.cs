using Alcedo.Frontend.Abstraction;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Alcedo.Frontend.Blazor;

public sealed class RequestOptions
{
    public Func<Task>? OnSuccess { get; private set; } 
    
    public Func<Task>? OnFailure { get; private set; } 

    public string? SuccessMessage { get; private set; }
    public string? ConfirmMessage { get; private set; }
    public string ConfirmTitle { get; private set; } = "Confirm";

    public bool UseOverlay { get; private set; } = true;

    public RequestOptions WithSuccessMsg(string? message = null)
    {
        SuccessMessage = message ?? "Success";
        return this;
    }

    public RequestOptions ExecuteOnSuccess(Func<Task>? action = null)
    {
        OnSuccess  = action;
        return this;
    }
    
    public RequestOptions ExecuteOnSuccess(Action? action)
    {
        OnSuccess = action is null ? null : () => { action(); return Task.CompletedTask; };
        return this;
    }
    
    public RequestOptions ExecuteOnFailure(Func<Task>? action = null)
    {
        OnFailure  = action;
        return this;
    }
    
    public RequestOptions ExecuteOnFailure(Action? action)
    {
        OnFailure = action is null ? null : () => { action(); return Task.CompletedTask; };
        return this;
    }

    public RequestOptions WithConfirmation(string? message = null, string title = "Confirm")
    {
        ConfirmMessage = message ?? "This Action needs your confirmation";
        ConfirmTitle = title;
        return this;
    }

    public RequestOptions DisableOverlay()
    {
        UseOverlay = false;
        return this;
    }
}

public class AlcedoRequestService(IDialogService dialogService, ISnackbar snackbar, GlobalOverlayService overlay)
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task ExecuteAsync(Func<Task> action, Action<RequestOptions>? configure = null)
    {
        bool isSuccess = false;
        
        var options = new RequestOptions();

        configure?.Invoke(options);

        if (!await ConfirmIfNeededAsync(options))
        {
            return;
        }
        
        if (options.UseOverlay)
        {
            overlay.Show();
        }

        await Lock.WaitAsync();
        try
        {
            await action();
            isSuccess = true;
            NotifySuccess(options);
        }
        catch (ApiBusinessException ex)
        {
            await dialogService.ShowMessageBoxAsync("Request Error", ex.Message, yesText: "Ok");
        }
        finally
        {
            if (options.UseOverlay)
            {
                overlay.Hide();
            }

            Lock.Release();
        }
        
        await ExecuteAfterActions(isSuccess, options);
    }

    private async Task ExecuteAfterActions(bool isSuccess, RequestOptions options)
    {
        if (isSuccess)
        {
            if (options.OnSuccess is not null)
            {
                await options.OnSuccess();
            }
        }
        else
        {
            if (options.OnFailure is not null)
            {
                await options.OnFailure();
            }
        }
    }

    public async Task ExecuteAsync(IReadOnlyCollection<Func<Task>> actions, Action<RequestOptions>? configure = null)
    {
        bool isSuccess = false;
        
        var options = new RequestOptions();

        configure?.Invoke(options);

        if (!await ConfirmIfNeededAsync(options))
        {
            return;
        }

        if (options.UseOverlay)
        {
            overlay.Show();
        }

        List<ApiBusinessException> exceptions = new();

        await Lock.WaitAsync();
        try
        {
            foreach (var action in actions)
            {
                try
                {
                    await action();
                }
                catch (ApiBusinessException ex)
                {
                    exceptions.Add(ex);
                }
            }
        }
        finally
        {
            if (options.UseOverlay)
            {
                overlay.Hide();
            }

            Lock.Release();
        }

        if (exceptions.Count == 0)
        {
            NotifySuccess(options);
            isSuccess = true;
        }
        else
        {
            await dialogService.ShowMessageBoxAsync(
                $"Request Error: ({exceptions.Count} Failed/ {actions.Count} Total)",
                string.Join(",", exceptions.Select(e => e.Message)), yesText: "Ok");
        }
        
        await ExecuteAfterActions(isSuccess, options);
    }

    public async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, Action<RequestOptions>? configure = null)
    {
        bool isSuccess = false;
        
        var options = new RequestOptions();

        configure?.Invoke(options);

        if (!await ConfirmIfNeededAsync(options))
        {
            return default;
        }

        if (options.UseOverlay)
        {
            overlay.Show();
        }

        T? result = default;

        await Lock.WaitAsync();
        try
        {
            result = await action();
            NotifySuccess(options);
            isSuccess = true;
        }
        catch (ApiBusinessException ex)
        {
            await dialogService.ShowMessageBoxAsync("Request Error", ex.Message, yesText: "Ok");
        }
        finally
        {
            if (options.UseOverlay)
            {
                overlay.Hide();
            }

            Lock.Release();
        }
        
        await ExecuteAfterActions(isSuccess, options);
        return result;
    }

    public async Task<IReadOnlyCollection<T?>> ExecuteAsync<T>(IReadOnlyCollection<Func<Task<T>>> actions,
        Action<RequestOptions>? configure = null)
    {
        bool isSuccess = false;
        
        var options = new RequestOptions();

        configure?.Invoke(options);

        if (!await ConfirmIfNeededAsync(options))
        {
            return new T?[actions.Count];
        }

        if (options.UseOverlay)
        {
            overlay.Show();
        }

        List<ApiBusinessException> exceptions = new();
        List<T?> results = new(actions.Count);

        await Lock.WaitAsync();
        try
        {
            foreach (var action in actions)
            {
                try
                {
                    var result = await action();
                    results.Add(result);
                }
                catch (ApiBusinessException ex)
                {
                    exceptions.Add(ex);
                    results.Add(default);
                }
            }
        }
        finally
        {
            if (options.UseOverlay)
            {
                overlay.Hide();
            }

            Lock.Release();
        }

        if (exceptions.Count == 0)
        {
            NotifySuccess(options);
        }
        else
        {
            await dialogService.ShowMessageBoxAsync(
                $"Request Error: ({exceptions.Count} Failed/ {actions.Count} Total)",
                string.Join(",", exceptions.Select(e => e.Message)),
                yesText: "Ok");
        }
        
        await ExecuteAfterActions(isSuccess, options);

        return results;
    }

    private async Task<bool> ConfirmIfNeededAsync(RequestOptions? options)
    {
        if (options?.ConfirmMessage is null)
        {
            return true;
        }

        var result = await dialogService.ShowMessageBoxAsync(
            options.ConfirmTitle,
            options.ConfirmMessage,
            yesText: "Confirm",
            cancelText: "Cancel");

        return result == true;
    }

    private void NotifySuccess(RequestOptions? options)
    {
        if (options?.SuccessMessage is not null)
        {
            snackbar.Clear();
            snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
            snackbar.Add(options.SuccessMessage, Severity.Success);
        }
    }
}