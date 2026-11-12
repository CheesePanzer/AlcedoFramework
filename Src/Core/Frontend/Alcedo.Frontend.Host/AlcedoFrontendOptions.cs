using Alcedo.Frontend.Blazor;
using Alcedo.Frontend.Core;

namespace Alcedo.Frontend.Host;

public class AlcedoFrontendOptions
{
    public AlcedoFrontendCoreOptions Core { get; set; } = new();
    
    public AlcedoFrontendBlazorOptions Blazor { get; set; } = new();
}