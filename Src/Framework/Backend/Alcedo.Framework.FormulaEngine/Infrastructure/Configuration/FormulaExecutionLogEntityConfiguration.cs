using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.FormulaEngine.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Configuration;

public class FormulaExecutionLogEntityConfiguration : EntityConfigurationBase<FormulaExecutionLogEntity>
{
    protected override void ConfigureProperties(EntityTypeBuilder<FormulaExecutionLogEntity> builder)
    {
        builder.Property(l => l.InputJson).IsTextColumn();
        
        builder.Property(l => l.OutputJson).IsTextColumn();
    }
}