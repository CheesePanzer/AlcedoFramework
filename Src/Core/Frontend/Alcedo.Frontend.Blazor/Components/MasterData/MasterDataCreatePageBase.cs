using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;
using Microsoft.AspNetCore.Components;

namespace Alcedo.Frontend.Blazor.Components.MasterData;

public abstract class MasterDataCreatePageBase<TDto, TSearch, TCreateCommand, TUpdateCommand> : ComponentBase
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase, new()
    where TCreateCommand : MasterDataCreateCommandBase, new()
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    [Inject] protected AlcedoRequestService RequestService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected AutoValidatorService Validator { get; set; } = null!;

    public abstract IMasterDataController<TDto, TSearch, TCreateCommand, TUpdateCommand> Client { get; set; }

    public abstract string BackHref { get; set; }
    public abstract string AfterCreateHref { get; set; }

    public abstract string Title { get; set; }

    protected TCreateCommand Command { get; } = new();

    protected async Task Save()
    {
        if (!await Validator.ValidateAsync(Command))
        {
            return;
        }

        var id = await RequestService.ExecuteAsync(
            () => Client.CreateAsync(Command),
            opt => opt.WithSuccessMsg());

        if (id == Guid.Empty)
        {
            return;
        }

        NavigationManager.NavigateTo(AfterCreateHref.Replace("{id}", id.ToString()));
    }
}