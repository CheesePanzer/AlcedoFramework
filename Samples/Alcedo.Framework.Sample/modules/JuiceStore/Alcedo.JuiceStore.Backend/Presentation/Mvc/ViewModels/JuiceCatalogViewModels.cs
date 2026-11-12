using Alcedo.Core.Application;
using Alcedo.Core.Presentation.Mvc.Kit.DaisyUI.ViewModels;
using Alcedo.JuiceStore.Backend.Application.Dto;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;

namespace Alcedo.JuiceStore.Backend.Presentation.Mvc.ViewModels;

public sealed class JuiceCatalogPageVm
{
    public required JuiceCatalogQuery Query { get; init; }

    public required JuiceCatalogListVm Content { get; init; }
}

public sealed class JuiceCatalogListVm
{
    public required IReadOnlyCollection<JuiceCatalogItemDto> Items { get; init; }

    public required PaginationControlVm Pagination { get; init; }

    public required int TotalCount { get; init; }

    public static JuiceCatalogListVm From(PagedResult<JuiceCatalogItemDto> result, string queryString,
        string targetAction) => new()
    {
        Items = result.Items,
        TotalCount = result.TotalCount,
        Pagination = new PaginationControlVm
        {
            PageNo = result.PageNo,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            CurrentQueryString = queryString,
            TargetAction = targetAction
        }
    };
}
