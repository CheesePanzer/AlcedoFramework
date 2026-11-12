using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Infrastructure.SearchQuery;

namespace Alcedo.Core.Infrastructure.Test;

public enum MetaTestStatus
{
    Active,
    Inactive
}

public record MetaTestAtom(string Value) : ValueAtom(Value);

public class MetaTestEntity : EntityBase
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public long Count { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; }
    public Guid CategoryId { get; set; }
    public MetaTestStatus Status { get; set; }
    public MetaTestAtom? Tag { get; set; }
    public List<Guid>? TagIds { get; set; }
    public List<int>? Scores { get; set; }
    public List<long>? Longs { get; set; }
    public List<decimal>? Decimals { get; set; }
    public List<string>? Labels { get; set; }
    public List<MetaTestAtom>? Atoms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record MetaTestQuery : EntitySearchQueryBase
{
    [SearchQueryBuilderIgnore] public string? Ignored { get; set; }

    [DateRangeFilter("CreatedAt", DateRangeBound.Start)]
    public DateTimeOffset? Start { get; set; }

    public string? Name { get; set; }
    public int? Age { get; set; }
    public long? Count { get; set; }
    public decimal? Price { get; set; }
    public bool? Active { get; set; }
    public Guid? CategoryId { get; set; }
    public MetaTestStatus? Status { get; set; }
    public MetaTestAtom? Tag { get; set; }
    public List<Guid>? TagIds { get; set; }
    public List<int>? Scores { get; set; }
    public List<long>? Longs { get; set; }
    public List<decimal>? Decimals { get; set; }
    public List<string>? Labels { get; set; }
    public List<MetaTestAtom>? Atoms { get; set; }
    public string? MissingProp { get; set; }
}

public record MetaTestQueryMapped : EntitySearchQueryBase
{
    [FilterField("Name")] public string? SearchName { get; set; }

    [StringSearchMode(StringSearchMode.Contains)]
    public string? Name { get; set; }
}

public class SearchQueryMetaCacheTests
{
    [Fact]
    public void GetOrBuild_IgnoreAttribute_ReturnsIgnore()
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQuery), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQuery.Ignored));
        Assert.Equal(FilterKind.Ignore, meta.Kind);
    }

    [Fact]
    public void GetOrBuild_DateRangeAttribute_ReturnsDateRange()
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQuery), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQuery.Start));
        Assert.Equal(FilterKind.DateRange, meta.Kind);
        Assert.NotNull(meta.DateRange);
        Assert.Equal("CreatedAt", meta.DateRange!.EntityPropertyName);
        Assert.Equal(DateRangeBound.Start, meta.DateRange.Bound);
    }

    [Fact]
    public void GetOrBuild_MissingEntityProperty_ReturnsSkip()
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQuery), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQuery.MissingProp));
        Assert.Equal(FilterKind.Skip, meta.Kind);
    }

    [Theory]
    [InlineData(nameof(MetaTestQuery.Name), (int)FilterKind.String)]
    [InlineData(nameof(MetaTestQuery.Age), (int)FilterKind.Int)]
    [InlineData(nameof(MetaTestQuery.Count), (int)FilterKind.Long)]
    [InlineData(nameof(MetaTestQuery.Price), (int)FilterKind.Decimal)]
    [InlineData(nameof(MetaTestQuery.Active), (int)FilterKind.Bool)]
    [InlineData(nameof(MetaTestQuery.CategoryId), (int)FilterKind.Guid)]
    [InlineData(nameof(MetaTestQuery.Status), (int)FilterKind.Enum)]
    [InlineData(nameof(MetaTestQuery.Tag), (int)FilterKind.ValueAtom)]
    public void GetOrBuild_SimpleTypes_ResolveCorrectly(string propName, int expectedKind)
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQuery), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == propName);
        Assert.Equal((FilterKind)expectedKind, meta.Kind);
    }

    [Fact]
    public void GetOrBuild_DoubleProperty_ThrowsNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
            SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQueryDouble), typeof(MetaTestEntityWithDouble)));

        Assert.Contains("double", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(nameof(MetaTestQuery.TagIds), (int)FilterKind.GuidList)]
    [InlineData(nameof(MetaTestQuery.Scores), (int)FilterKind.IntList)]
    [InlineData(nameof(MetaTestQuery.Longs), (int)FilterKind.LongList)]
    [InlineData(nameof(MetaTestQuery.Decimals), (int)FilterKind.DecimalList)]
    [InlineData(nameof(MetaTestQuery.Labels), (int)FilterKind.StringList)]
    [InlineData(nameof(MetaTestQuery.Atoms), (int)FilterKind.ValueAtomList)]
    public void GetOrBuild_ListTypes_ResolveCorrectly(string propName, int expectedKind)
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQuery), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == propName);
        Assert.Equal((FilterKind)expectedKind, meta.Kind);
    }

    [Fact]
    public void GetOrBuild_UnsupportedList_ReturnsUnsupported()
    {
        var queryType = typeof(MetaTestQueryUnsupported);
        var entityType = typeof(MetaTestEntityWithUnsupportedList);
        var metas = SearchQueryMetaCache.GetOrBuild(queryType, entityType);
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQueryUnsupported.UnsupportedList));
        Assert.Equal(FilterKind.Unsupported, meta.Kind);
    }

    [Fact]
    public void GetOrBuild_FilterFieldAttribute_MapsEntityPropertyName()
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQueryMapped), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQueryMapped.SearchName));
        Assert.Equal(FilterKind.String, meta.Kind);
        Assert.Equal("Name", meta.EntityPropertyName);
    }

    [Fact]
    public void GetOrBuild_StringSearchModeAttribute_Resolved()
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(MetaTestQueryMapped), typeof(MetaTestEntity));
        var meta = Assert.Single(metas, m => m.QueryProp.Name == nameof(MetaTestQueryMapped.Name));
        Assert.Equal(FilterKind.String, meta.Kind);
        Assert.Equal(StringSearchMode.Contains, meta.StringMode);
    }

    private record MetaTestQueryDouble : EntitySearchQueryBase
    {
        public double? Weight { get; set; }
    }

    private class MetaTestEntityWithDouble : EntityBase
    {
        public double Weight { get; set; }
    }

    private class MetaTestEntityWithUnsupportedList : EntityBase
    {
        public List<double>? UnsupportedList { get; set; }
    }

    private record MetaTestQueryUnsupported : EntitySearchQueryBase
    {
        public List<double>? UnsupportedList { get; set; }
    }
}