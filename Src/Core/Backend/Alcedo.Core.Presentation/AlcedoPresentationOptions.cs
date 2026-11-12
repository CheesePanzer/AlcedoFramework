using Microsoft.AspNetCore.Mvc.Filters;

namespace Alcedo.Core.Presentation;

public sealed class AlcedoPresentationOptions
{
    public bool UseForwardedHeaders { get; set; } = true;
    
    public bool UseHsts { get; set; } = false;
    
    public bool UseHttpsRedirection { get; set; } = false;
    
    public bool UseCors { get; set; } = true;
    
    public bool DisplaySensitiveExceptions { get; set; } = false;
    
    public bool AddMvcSupport { get; set; } = false;

    public List<IFilterMetadata> AdditionalGlobalFilters = new();
}