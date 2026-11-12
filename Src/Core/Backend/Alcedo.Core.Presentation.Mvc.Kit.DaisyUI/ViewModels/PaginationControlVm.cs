namespace Alcedo.Core.Presentation.Mvc.Kit.DaisyUI.ViewModels;

public sealed class PaginationControlVm
{
    public int PageNo { get; init; }
    public int TotalPages { get; init; }
    public int PageSize { get; init; }
    public string CurrentQueryString { get; init; } = string.Empty;
    public string TargetAction { get; init; } = "";
}
