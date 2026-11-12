using System.Reflection;
using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Application;

public sealed class AlcedoCoreApplicationComponent : IAlcedoCoreComponent
{
    public Assembly Assembly => typeof(AlcedoCoreApplicationComponent).Assembly;
}