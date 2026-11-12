using System.Reflection;
using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Infrastructure;

public sealed class AlcedoCoreInfrastructureComponent : IAlcedoCoreComponent
{
    public Assembly Assembly => typeof(AlcedoCoreInfrastructureComponent).Assembly;
}