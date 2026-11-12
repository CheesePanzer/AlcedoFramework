using Alcedo.Core.Abstractions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.EfCore;

internal class AlcedoOptionsExtension : IDbContextOptionsExtension
{
    private IReadOnlyCollection<IAlcedoModule> Modules { get; }

    private AlcedoInfrastructureOptions Options { get; }

    public AlcedoOptionsExtension(IReadOnlyCollection<IAlcedoModule> modules, AlcedoInfrastructureOptions options)
    {
        Modules = modules;
        Options = options;
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(Modules);
        services.AddSingleton(Options);
    }

    public void Validate(IDbContextOptions options)
    {
    }

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    private class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "AlcedoModuleExtension";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}