using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.ReferenceIntegrity;

/// <summary>
/// Defines the behavior of the reference integrity check.
/// </summary>
[Flags]
public enum ReferenceMode
{
    /// <summary>
    /// Strict: Ensures referential integrity. 
    /// The child entity verifies its reference to this parent to maintain full consistency.
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Snapshot: Allows decoupled references. 
    /// The child entity does not verify the reference, since the parent treats 
    /// the child as a snapshot and allows it to be deleted/disabled/modified etc.
    /// </summary>
    Snapshot = 2,

    /// <summary>
    /// Ignore Checking
    /// </summary>
    IgnoreCheck = 0
}

/// <summary>
/// Detailed Route of how source to principal
/// </summary>
public record NavigationStep(
    Type ParentEntityType,
    string PropertyName,
    Type PropertyType,
    bool IsCollection
);

/// <summary>
/// Metadata for a reference relationship.
/// Describes "Who is referring to Whom" at a low-level data structure.
/// </summary>
public record ReferenceConfiguration
{
    public ReferenceConfiguration(
        Type sourceAggregateType,
        Type dependentEntityType,
        Type principalAggregateType,
        string foreignKeyProperty,
        ReferenceMode referenceMode,
        List<NavigationStep> navigationPath)
    {
        SourceAggregateType = sourceAggregateType;
        DependentEntityType = dependentEntityType;
        PrincipalAggregateType = principalAggregateType;
        ForeignKeyProperty = foreignKeyProperty;
        ReferenceMode = referenceMode;
        NavigationPath = navigationPath;
    }

    /// <summary>
    /// The top-level Aggregate Root type that owns the reference.
    /// </summary>
    public Type SourceAggregateType { get; init; }

    /// <summary>
    /// The specific entity type holding the Foreign Key (could be the root itself or a child entity/line item).
    /// </summary>
    public Type DependentEntityType { get; init; }

    /// <summary>
    /// The target entity type being referred to (usually another Aggregate Root).
    /// </summary>
    public Type PrincipalAggregateType { get; init; }

    /// <summary>
    /// The name of the Foreign Key property used for reflection or expression building.
    /// </summary>
    public string ForeignKeyProperty { get; init; }

    /// <summary>
    /// Defines how the reference is treated: Strict or Snapshot.
    /// </summary>
    public ReferenceMode ReferenceMode { get; init; }

    /// <summary>
    /// The detailed route of how SourceAggregateType goes to DependentEntityType;
    /// If DependentEntityType is SourceAggregateType, then this is empty
    /// eg: [ { "Items", typeof(OrderItem), true }, { "Details", typeof(ItemDetail), false } ]
    /// </summary>
    public IReadOnlyList<NavigationStep> NavigationPath { get; init; } = Array.Empty<NavigationStep>();
}

/// <summary>
/// Interface for marking a class as a reference configuration.
/// Used to define how an aggregate root relates to or depends on other aggregate roots.
/// </summary>
/// <typeparam name="T">The source Aggregate Root type that initiates the reference.</typeparam>
public interface IAggregateRootReferenceConfiguration<T> where T : AggregateRoot
{
    /// <summary>
    /// Configures the reference mapping rules.
    /// </summary>
    void Configure(ReferenceEntityBuilder<T> builder);
}

/// <summary>
/// Builder for collecting all reference configurations within the system.
/// </summary>
public class ReferenceModelBuilder
{
    private readonly List<ReferenceConfiguration> _configurations = new();

    /// <summary>
    /// Starts configuring references for a specific Aggregate Root.
    /// </summary>
    public ReferenceEntityBuilder<T> Entity<T>() where T : AggregateRoot
    {
        return new ReferenceEntityBuilder<T>(this);
    }

    internal void AddConfig(ReferenceConfiguration config) => _configurations.Add(config);

    /// <summary>
    /// Retrieves all collected reference configurations.
    /// </summary>
    internal IReadOnlyList<ReferenceConfiguration> GetConfigs() => _configurations.AsReadOnly();
}

public abstract class ReferenceBuilderBase<TRoot> where TRoot : AggregateRoot
{
    protected readonly ReferenceModelBuilder ModelBuilder;
    protected readonly List<NavigationStep> Path;

    protected ReferenceBuilderBase(ReferenceModelBuilder modelBuilder, List<NavigationStep>? path = null)
    {
        ModelBuilder = modelBuilder;
        Path = path ?? new List<NavigationStep>();
    }

    protected void AddInternal<TPrincipal, TDependent>(
        Expression<Func<TDependent, Guid?>> expression,
        ReferenceMode mode)
        where TPrincipal : AggregateRoot
    {
        var member = GetMemberInfo(expression);

        ModelBuilder.AddConfig(new ReferenceConfiguration
        (
            sourceAggregateType: typeof(TRoot),
            principalAggregateType: typeof(TPrincipal),
            dependentEntityType: typeof(TDependent),
            foreignKeyProperty: member.Name,
            referenceMode: mode,
            navigationPath: Path.ToList()
        ));
    }

    protected void AddInternal<TDependent>(
        string tPrincipalCode,
        Expression<Func<TDependent, Guid?>> expression,
        ReferenceMode mode)
    {
        var tPrincipal = AggregateRootMetadataCenter.GetAggregateTypeByIdentity(tPrincipalCode);

        if (!typeof(AggregateRoot).IsAssignableFrom(tPrincipal))
        {
            throw new InvalidOperationException($"{tPrincipal} is not a {nameof(AggregateRoot)}");
        }

        var member = GetMemberInfo(expression);

        ModelBuilder.AddConfig(new ReferenceConfiguration
        (
            sourceAggregateType: typeof(TRoot),
            principalAggregateType: tPrincipal,
            dependentEntityType: typeof(TDependent),
            foreignKeyProperty: member.Name,
            referenceMode: mode,
            navigationPath: Path.ToList()
        ));
    }

    protected MemberInfo GetMemberInfo(LambdaExpression expression)
    {
        var body = expression.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is not MemberExpression member)
        {
            throw new ArgumentException("Must be a property expression");
        }

        return member.Member;
    }

    protected ReferenceSubEntityBuilder<TRoot, TNext> CreateSubBuilder<TCurrent, TNext>(
        MemberInfo member, bool isCollection)
        where TNext : EntityBase
    {
        var nextPath = new List<NavigationStep>(Path)
        {
            new NavigationStep(typeof(TCurrent), member.Name, typeof(TNext), isCollection)
        };
        return new ReferenceSubEntityBuilder<TRoot, TNext>(ModelBuilder, nextPath);
    }
}

public class ReferenceEntityBuilder<TRoot> : ReferenceBuilderBase<TRoot> where TRoot : AggregateRoot
{
    public ReferenceEntityBuilder(ReferenceModelBuilder builder) : base(builder)
    {
    }

    public void References<TPrincipal>(Expression<Func<TRoot, Guid?>> fk,
        ReferenceMode mode = ReferenceMode.Strict) where TPrincipal : AggregateRoot
        => AddInternal<TPrincipal, TRoot>(fk, mode);

    public void References(string tPrincipalCode, Expression<Func<TRoot, Guid?>> fk,
        ReferenceMode mode = ReferenceMode.Strict)
        => AddInternal(tPrincipalCode, fk, mode);

    public ReferenceSubEntityBuilder<TRoot, TSubEntity> Through<TSubEntity>(
        Expression<Func<TRoot, TSubEntity>> nav) where TSubEntity : EntityBase
        => CreateSubBuilder<TRoot, TSubEntity>(GetMemberInfo(nav), isCollection: false);

    public ReferenceSubEntityBuilder<TRoot, TSubEntity> Through<TSubEntity>(
        Expression<Func<TRoot, IEnumerable<TSubEntity>>> nav) where TSubEntity : EntityBase
        => CreateSubBuilder<TRoot, TSubEntity>(GetMemberInfo(nav), isCollection: true);
}

public class ReferenceSubEntityBuilder<TRoot, TSubEntity> : ReferenceBuilderBase<TRoot>
    where TRoot : AggregateRoot where TSubEntity : EntityBase
{
    public ReferenceSubEntityBuilder(ReferenceModelBuilder builder, List<NavigationStep> path)
        : base(builder, path)
    {
    }

    public void References<TPrincipal>(Expression<Func<TSubEntity, Guid?>> fk,
        ReferenceMode mode = ReferenceMode.Strict) where TPrincipal : AggregateRoot
        => AddInternal<TPrincipal, TSubEntity>(fk, mode);

    public void References(string tPrincipalCode, Expression<Func<TSubEntity, Guid?>> fk,
        ReferenceMode mode = ReferenceMode.Strict)
        => AddInternal(tPrincipalCode, fk, mode);

    public ReferenceSubEntityBuilder<TRoot, TNext> Through<TNext>(
        Expression<Func<TSubEntity, TNext>> nav) where TNext : EntityBase
        => CreateSubBuilder<TSubEntity, TNext>(GetMemberInfo(nav), isCollection: false);

    public ReferenceSubEntityBuilder<TRoot, TNext> Through<TNext>(
        Expression<Func<TSubEntity, IEnumerable<TNext>>> nav) where TNext : EntityBase
        => CreateSubBuilder<TSubEntity, TNext>(GetMemberInfo(nav), isCollection: true);
}

public static class ReferenceConfigurationCenter
{
    public static IReadOnlyList<ReferenceConfiguration> Configurations = new List<ReferenceConfiguration>();

    public static void Initialize(IEnumerable<Assembly> assemblies)
    {
        Configurations = GetBuilderConfigs(assemblies);
    }
    
    private static IReadOnlyList<ReferenceConfiguration> GetBuilderConfigs(IEnumerable<Assembly> assemblies)
    {
        var builder = new ReferenceModelBuilder();

        var configTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   i.GetGenericTypeDefinition() ==
                                                   typeof(IAggregateRootReferenceConfiguration<>)));

        foreach (var type in configTypes)
        {
            var interfaceType = type.GetInterfaces()
                .First(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IAggregateRootReferenceConfiguration<>));

            var entityType = interfaceType.GetGenericArguments()[0];

            var entityBuilderMethod = typeof(ReferenceModelBuilder)
                .GetMethod(nameof(ReferenceModelBuilder.Entity))!
                .MakeGenericMethod(entityType);
            var entityBuilder = entityBuilderMethod.Invoke(builder, null);

            var configInstance = Activator.CreateInstance(type);
            var configureMethod =
                type.GetMethod(nameof(IAggregateRootReferenceConfiguration<AggregateRoot>.Configure))!;
            configureMethod.Invoke(configInstance, new[] { entityBuilder });
        }

        return builder.GetConfigs();
    }
}