using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.EfCore;

public sealed class AlcedoDbContext : DbContext
{
    public AlcedoDbContext(DbContextOptions<AlcedoDbContext> options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>()
            .HavePrecision(AlcedoStorageLengthConvention.DefaultDecimalPrecision, AlcedoStorageLengthConvention.DefaultDecimalScale);

        configurationBuilder.Properties<string>()
            .HaveMaxLength(AlcedoStorageLengthConvention.DefaultStringMaxLength);
    }
    
    public DbSet<TEntity> AggRoot<TEntity>() where TEntity : AggregateRoot
    {
        return Set<TEntity>();
    }
    
    public DbSet<TEntity> RawEntity<TEntity>() where TEntity : EntityBase
    {
        return Set<TEntity>();
    }
}