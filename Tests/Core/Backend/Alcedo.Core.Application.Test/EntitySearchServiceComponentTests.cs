using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.Dto;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Application.Test;

public class TestSearchEntity : EntityBase;

public record TestSearchDto : EntityDtoBase;

public record TestSearchQuery : EntitySearchQueryBase;

public class TestSearchComponent : EntitySearchServiceComponent<TestSearchEntity, TestSearchDto, TestSearchQuery>
{
    public TestSearchComponent(IEntityQueryRepository<TestSearchEntity, TestSearchDto, TestSearchQuery> repo) :
        base(repo)
    {
    }
}

public class EntitySearchServiceComponentTests
{
    [Fact]
    public async Task GetPagedDtosAsync_ReturnsCorrectPagedResult()
    {
        var items = new List<TestSearchDto> { new() { Id = Guid.NewGuid() } };
        var mockRepo = new MockQueryRepository(items, 100);
        var component = new TestSearchComponent(mockRepo);
        var query = new TestSearchQuery();
        var sort = new SortQuery();
        var pagination = new PaginationQuery { PageNo = 2, PageSize = 10 };

        var result = await component.GetPagedDtosAsync(query, sort, pagination);

        Assert.Equal(items, result.Items);
        Assert.Equal(2, result.PageNo);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(10, result.TotalPages);
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenFound_ReturnsDto()
    {
        var expected = new TestSearchDto { Id = Guid.NewGuid() };
        var mockRepo = new MockQueryRepository(new List<TestSearchDto> { expected }, 1)
        {
            ByIdResult = expected
        };
        var component = new TestSearchComponent(mockRepo);

        var result = await component.GetDtoByIdAsync(expected.Id);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetDtoByIdAsync_WhenNotFound_ThrowsAppErrQueryResultNotFound()
    {
        var mockRepo = new MockQueryRepository(new List<TestSearchDto>(), 0);
        var component = new TestSearchComponent(mockRepo);

        await Assert.ThrowsAsync<AppErrQueryResultNotFound<TestSearchDto>>(() =>
            component.GetDtoByIdAsync(Guid.NewGuid()));
    }

    private class MockQueryRepository : IEntityQueryRepository<TestSearchEntity, TestSearchDto, TestSearchQuery>
    {
        private readonly List<TestSearchDto> _items;
        private readonly int _totalCount;

        public TestSearchDto? ByIdResult { get; init; }

        public IReadOnlyList<TestSearchDto> ByIdsResult { get; init; }

        public MockQueryRepository(List<TestSearchDto> items, int totalCount)
        {
            _items = items;
            _totalCount = totalCount;
        }

        public Task<(IReadOnlyList<TestSearchDto> Items, int TotalCount)> GetPagedAsync(
            TestSearchQuery query, PaginationQuery pagiQuery, SortQuery sortQuery, CancellationToken ct = default)
        {
            return Task.FromResult<(IReadOnlyList<TestSearchDto> Items, int TotalCount)>((_items, _totalCount));
        }

        public Task<IReadOnlyList<TestSearchDto>> GetListAsync(
            TestSearchQuery query, SortQuery sortQuery, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<TestSearchDto>>(_items);
        }

        public Task<TestSearchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(ByIdResult);
        }

        public Task<IReadOnlyList<TestSearchDto>> GetListByIdsAsync(IReadOnlyCollection<Guid> id,
            CancellationToken ct = default)
        {
            return Task.FromResult(ByIdsResult);
        }
    }
}