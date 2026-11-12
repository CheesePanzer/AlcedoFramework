using System.ComponentModel.DataAnnotations;

namespace Alcedo.Frontend.Abstraction.SearchQuery;

public sealed record PaginationQuery
{
    public int PageNo { get; set; } = 1;

    [Range(1, 1000)] public int PageSize { get; set; } = 50;
}