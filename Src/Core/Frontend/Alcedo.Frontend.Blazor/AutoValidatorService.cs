using Alcedo.Frontend.Abstraction.Helpers;
using MudBlazor;

namespace Alcedo.Frontend.Blazor;

public class AutoValidatorService(IDialogService dialogService)
{
    public async Task<bool> ValidateAsync<T>(T model, params string[] additionalErrors) where T : class
    {
        var errors = AutoValidator.Validate(model);
        errors.AddRange(additionalErrors.Where(e => !string.IsNullOrEmpty(e)));

        if (errors.Count == 0)
        {
            return true;
        }

        var content = string.Join("\n", errors.Select((e, i) => $"{i + 1}. {e}"));
        await dialogService.ShowMessageBoxAsync("Validation Error", content, yesText: "Ok");
        return false;
    }
}