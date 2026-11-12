using System.Reflection;
using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Domain;

public sealed class AlcedoCoreDomainComponent : IAlcedoCoreComponent
{
    public Assembly Assembly => typeof(AlcedoCoreDomainComponent).Assembly;
}