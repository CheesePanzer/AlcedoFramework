namespace Alcedo.Core.Application.Test;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_PageSizeZero_ReturnsZero()
    {
        var result = new PagedResult<object>
        {
            TotalCount = 100,
            PageSize = 0,
            PageNo = 1,
            Items = new List<object>()
        };

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ExactDivision_ReturnsQuotient()
    {
        var result = new PagedResult<object>
        {
            TotalCount = 10,
            PageSize = 2,
            PageNo = 1,
            Items = new List<object>()
        };

        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public void TotalPages_Remainder_RoundsUp()
    {
        var result = new PagedResult<object>
        {
            TotalCount = 5,
            PageSize = 2,
            PageNo = 1,
            Items = new List<object>()
        };

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ZeroTotalCount_ReturnsZero()
    {
        var result = new PagedResult<object>
        {
            TotalCount = 0,
            PageSize = 10,
            PageNo = 1,
            Items = new List<object>()
        };

        Assert.Equal(0, result.TotalPages);
    }
}