using System.Reflection;

namespace Alcedo.Core.Presentation;

public sealed class AlcedoCorePresentationComponent
{
    public Assembly Assembly => typeof(AlcedoCorePresentationComponent).Assembly;
}