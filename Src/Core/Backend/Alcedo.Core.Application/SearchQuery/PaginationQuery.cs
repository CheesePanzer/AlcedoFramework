using System.ComponentModel.DataAnnotations;

namespace Alcedo.Core.Application.SearchQuery;

public sealed record PaginationQuery
{
    public int PageNo { get; init; } = 1;

    [Range(1, 1000)] public int PageSize { get; init; } = 50;
}