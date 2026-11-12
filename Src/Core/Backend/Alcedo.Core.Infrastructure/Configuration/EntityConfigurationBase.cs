using Alcedo.Core.Domain.Entity;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Core.Infrastructure.Configuration;

public interface IEntityTypeDefaultConfigurator<T> where T : EntityBase
{
    void Apply(EntityTypeBuilder<T> builder);
    
    void ApplyTableConstraints(EntityTypeBuilder<T> builder, TableBuilder<T> table);
}

public class EntityTypeDefaultConfigurator<T> : IEntityTypeDefaultConfigurator<T>
    where T : EntityBase
{
    private void ConfigureKey(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
    }

    public virtual void Apply(EntityTypeBuilder<T> builder)
    {
        ConfigureKey(builder);
    }

    public virtual void ApplyTableConstraints(EntityTypeBuilder<T> builder, TableBuilder<T> table)
    {
        
    }
}

public abstract class EntityConfigurationBase<T> : IEntityTypeConfiguration<T>
    where T : EntityBase
{
    protected virtual string Comment => typeof(T).Name.Pluralize().Underscore();
    
    protected virtual string TableName => typeof(T).Name.Pluralize().Underscore();

    protected virtual string? Schema => null;

    protected virtual bool ExcludeFromMigrations => false;

    protected virtual IEntityTypeDefaultConfigurator<T> DefaultConfigurator => new EntityTypeDefaultConfigurator<T>();


    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        DefaultConfigurator.Apply(builder);
        //Hook Methods
        ConfigureProperties(builder);//
        
        ConfigureTable(builder);
        
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
        ConfigureQueryFilters(builder);
        ConfigureConverters(builder);
        SeedData(builder);
    }

    private void ConfigureTable(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(TableName, Schema, t =>
        {
            t.HasComment(Comment);
            
            if (ExcludeFromMigrations)
            {
                t.ExcludeFromMigrations();
            }
            
            DefaultConfigurator.ApplyTableConstraints(builder, t);

            ConfigureTableConstraints(builder, t);
        });
    }

    /// <summary>
    /// For Adding Table Constraint and comment etc.
    /// </summary>
    protected virtual void ConfigureTableConstraints(
        EntityTypeBuilder<T> entityBuilder,
        TableBuilder tableBuilder)
    {
    }


    /// <summary>
    /// Properties like builder.Property(x => x.Name).HasMaxLength(100);
    /// </summary>
    protected virtual void ConfigureProperties(EntityTypeBuilder<T> builder)
    {
    }

    /// <summary>
    /// Indexes like builder.HasIndex(x => x.Email).IsUnique();
    /// </summary>
    protected virtual void ConfigureIndexes(EntityTypeBuilder<T> builder)
    {
    }

    /// <summary>
    /// Fk like builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    /// </summary>
    protected virtual void ConfigureRelationships(EntityTypeBuilder<T> builder)
    {
    }

    /// <summary>
    /// query filter like builder.HasQueryFilter(x => !x.IsDeleted);
    /// </summary>
    protected virtual void ConfigureQueryFilters(EntityTypeBuilder<T> builder)
    {
    }

    protected virtual void ConfigureConverters(EntityTypeBuilder<T> builder)
    {
    }

    /// <summary>
    /// If need to seed some data
    /// </summary>
    protected virtual void SeedData(EntityTypeBuilder<T> builder)
    {
    }
}



