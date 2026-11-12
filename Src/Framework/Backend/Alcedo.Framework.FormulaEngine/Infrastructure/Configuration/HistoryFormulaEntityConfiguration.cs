using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.FormulaEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Configuration;

public sealed class HistoryFormulaEntityConfiguration : EntityConfigurationBase<HistoryFormulaEntity>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<HistoryFormulaEntity> builder)
    {
        builder.HasMany(f => f.InputParameters)
            .WithOne()
            .HasForeignKey(i => i.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(f => f.Expressions)
            .WithOne()
            .HasForeignKey(i => i.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(f => f.OutputParameters)
            .WithOne()
            .HasForeignKey(i => i.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    protected override void ConfigureProperties(EntityTypeBuilder<HistoryFormulaEntity> builder)
    {
        builder.Property(f => f.Description).IsTextColumn();
    }
    
    protected override void ConfigureIndexes(EntityTypeBuilder<HistoryFormulaEntity> builder)
    {
        builder.HasIndex(f => f.Code).IsUnique();
        
        builder.HasIndex(f => new { f.Code, f.OriginalVersion }).IsUnique();
    }
}

public class HistoryFormulaInputParamConfiguration : EntityConfigurationBase<HistoryFormulaInputEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<HistoryFormulaInputEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
    }

    protected override void ConfigureProperties(EntityTypeBuilder<HistoryFormulaInputEntity> builder)
    {
        builder.Property(f => f.ValidCondition).IsTextColumn();
        
        builder.Property(f => f.VarDescription).IsTextColumn();
    }
}

public class HistoryFormulaExpressionConfiguration : EntityConfigurationBase<HistoryFormulaExpressionEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<HistoryFormulaExpressionEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
        
        builder.HasIndex(f => new {f.FormulaId,f.Order}).IsUnique();
    }
    
    protected override void ConfigureProperties(EntityTypeBuilder<HistoryFormulaExpressionEntity> builder)
    {
        builder.Property(f => f.Expression).IsTextColumn();
        
        builder.Property(f => f.Note).IsTextColumn();
    }
}

public class HistoryFormulaOutputParamConfiguration : EntityConfigurationBase<HistoryFormulaOutputEntity>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<HistoryFormulaOutputEntity> builder)
    {  
        builder.HasIndex(f => new {f.FormulaId,f.VarName}).IsUnique();
    }
    
    protected override void ConfigureProperties(EntityTypeBuilder<HistoryFormulaOutputEntity> builder)
    {
        builder.Property(f => f.Expression).IsTextColumn();
        
        builder.Property(f => f.VarDescription).IsTextColumn();
    }
}