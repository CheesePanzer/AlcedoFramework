namespace Alcedo.Frontend.Abstraction.Dto;

public record PagedResult<T>
{
    public required int PageNo { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public int TotalPages
    {
        get
        {
            if (PageSize <= 0)
            {
                return 0;
            }
            else
            {
                return (int)Math.Ceiling(1.0 * TotalCount / PageSize);
            }
        }
    }

    public required IReadOnlyCollection<T> Items { get; init; } = new List<T>();
}