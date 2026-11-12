using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;
using Alcedo.Frontend.Blazor.Components.Auto;
using Mapster;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Alcedo.Frontend.Blazor.Components.MasterData;

public abstract class MasterDataDetailPageBase<TDto, TSearch, TCreateCommand, TUpdateCommand> : ComponentBase
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase, new()
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase, new()
{
    [Inject] protected AlcedoRequestService RequestService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected AutoValidatorService Validator { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    [Parameter, EditorRequired] public Guid Id { get; set; }

    protected virtual RenderFragment? EditChildContent(TUpdateCommand command) => null;

    protected abstract IMasterDataController<TDto, TSearch, TCreateCommand, TUpdateCommand> Client { get; }

    protected TDto? Dto;

    protected virtual IReadOnlyList<MasterDataFormActionToolBar.ActionMetadata> ExtraActions => [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshData();
            StateHasChanged();
        }
    }

    protected virtual async Task RefreshData()
    {
        Dto = await RequestService.ExecuteAsync(() => Client.GetAsync(Id));
        StateHasChanged();
    }

    protected virtual Task OnBeforeOpenEdit(TUpdateCommand command)
        => Task.CompletedTask;

    protected virtual async Task OpenEdit()
    {
        if (Dto is null)
        {
            return;
        }

        var command = Dto.Adapt<TUpdateCommand>();

        await OnBeforeOpenEdit(command);

        var parameters = new DialogParameters<AutoFormDialog<TUpdateCommand>>
        {
            { x => x.Model, command },
            { x => x.ChildContent, EditChildContent(command) }
        };

        var dialog = await DialogService.ShowAsync<AutoFormDialog<TUpdateCommand>>(
            "Edit", parameters,
            new DialogOptions
                { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true, CloseOnEscapeKey = true });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            if (!await Validator.ValidateAsync(command))
            {
                return;
            }

            await RequestService.ExecuteAsync(
                () => Client.UpdateAsync(Id, command),
                opt => opt.WithSuccessMsg());
            await RefreshData();
        }
    }
}