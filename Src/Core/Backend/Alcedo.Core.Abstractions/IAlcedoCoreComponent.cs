using System.Reflection;

namespace Alcedo.Core.Abstractions;

internal interface IAlcedoCoreComponent
{
    public Assembly Assembly { get; }
}