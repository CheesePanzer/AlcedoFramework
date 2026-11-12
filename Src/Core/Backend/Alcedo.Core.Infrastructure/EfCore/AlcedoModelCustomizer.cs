using Alcedo.Core.Abstractions;
using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alcedo.Core.Infrastructure.EfCore;

internal class AlcedoModelCustomizer : RelationalModelCustomizer
{
    private readonly IReadOnlyCollection<IAlcedoModule> _alcedoModules;

    private readonly AlcedoInfrastructureOptions _options;

    public AlcedoModelCustomizer(ModelCustomizerDependencies dependencies,
        IReadOnlyCollection<IAlcedoModule> alcedoModules, AlcedoInfrastructureOptions options)
        : base(dependencies)
    {
        _alcedoModules = alcedoModules;
        _options = options;
    }

    public override void Customize(ModelBuilder builder, DbContext context)
    {
        base.Customize(builder, context);

        ConfigureInfrastructure(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AlcedoDbContext).Assembly);

        foreach (var module in _alcedoModules)
        {
            if (module.Assembly == typeof(AlcedoDbContext).Assembly)
            {
                continue;
            }

            builder.ApplyConfigurationsFromAssembly(module.Assembly);

            var moduleEntities = builder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Assembly == module.Assembly && e.GetSchema() is null);

            foreach (var entityType in moduleEntities)
            {
                entityType.SetSchema(module.ModuleCode);
            }
        }
    }

    private void ConfigureInfrastructure(ModelBuilder builder)
    {
        builder.HasPostgresExtension("pg_trgm");
        var mtSchema = _options.MasstransitScheme ?? "masstransit";
        builder.AddInboxStateEntity(b => b.ToTable("inbox_state", mtSchema));
        builder.AddOutboxMessageEntity(b => b.ToTable("outbox_message", mtSchema));
        builder.AddOutboxStateEntity(b => b.ToTable("outbox_state", mtSchema));
        var qtzSchema = _options.QuartzScheme ?? "quartz";
        builder.AddQuartz(ibuilder => ibuilder.UsePostgreSql(schema: qtzSchema));
    }
}