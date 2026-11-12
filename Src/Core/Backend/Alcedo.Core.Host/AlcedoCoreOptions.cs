using Alcedo.Core.Infrastructure;
using Alcedo.Core.Plugin.SmartDoc;
using Alcedo.Core.Presentation;

namespace Alcedo.Core.Host;

public sealed record AlcedoCoreOptions
{
    public AlcedoInfrastructureOptions Infrastructure { get; init; } = new();

    public AlcedoPresentationOptions Presentation { get; init; } = new();
}