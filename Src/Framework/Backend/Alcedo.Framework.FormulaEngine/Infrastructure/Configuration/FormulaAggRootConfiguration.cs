using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Configuration;

public class FormulaAggRootConfiguration : ReferenceDataConfigurationBase<FormulaAggRoot>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<FormulaAggRoot> builder)
    {
        builder.OwnsOne<OperatorRecord>(f => f.Updater);
        
        builder.HasChildEntities(f => f.InputParameters, i => i.FormulaId);
        
        builder.HasChildEntities(f => f.Expressions, i => i.FormulaId);
        
        builder.HasChildEntities(f => f.OutputParameters, i => i.FormulaId);
    }

    protected override void ConfigureProperties(EntityTypeBuilder<FormulaAggRoot> builder)
    {
        builder.Property(f => f.Description).IsTextColumn();
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<FormulaAggRoot> builder)
    {
        builder.HasIndex(f => new { f.Code, f.Version });
    }
}

public class FormulaInputParamConfiguration : EntityConfigurationBase<FormulaInputEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<FormulaInputEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
    }

    protected override void ConfigureProperties(EntityTypeBuilder<FormulaInputEntity> builder)
    {
        builder.Property(f => f.ValidCondition).IsTextColumn();
        
        builder.Property(f => f.VarDescription).IsTextColumn();
    }
}

public class FormulaExpressionConfiguration : EntityConfigurationBase<FormulaExpressionEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<FormulaExpressionEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
        
        builder.HasIndex(f => new {f.FormulaId,f.Order}).IsUnique();
    }
    
    protected override void ConfigureProperties(EntityTypeBuilder<FormulaExpressionEntity> builder)
    {
        builder.Property(f => f.Expression).IsTextColumn();
        
        builder.Property(f => f.Note).IsTextColumn();
    }
}

public class FormulaOutputParamConfiguration : EntityConfigurationBase<FormulaOutputEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<FormulaOutputEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
    }
    
    protected override void ConfigureProperties(EntityTypeBuilder<FormulaOutputEntity> builder)
    {
        builder.Property(f => f.Expression).IsTextColumn();
        
        builder.Property(f => f.VarDescription).IsTextColumn();
    }
}