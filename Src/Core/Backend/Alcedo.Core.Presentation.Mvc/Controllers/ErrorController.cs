using Alcedo.Core.Presentation.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation.Mvc.Controllers;

[AllowAnonymous]
public sealed class ErrorController : AlcedoPageControllerBase
{
    [HttpGet("")]
    public IActionResult Index([FromQuery] bool isHx = false)
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;
        
        if (exception is null)
        {
            if (isHx)
            {
                return PartialView("_ErrorPartial", new ErrorVm());
            }
            return View(new ErrorVm());
        }
        
        var originalPath = feature?.Path ?? HttpContext.Request.Path.Value;

        var pd = AlcedoProblemDetailsFactory.Create(HttpContext, exception, originalPath);

        var vm = ErrorVm.FromProblemDetails(pd, HttpContext.TraceIdentifier);

        if (isHx)
        {
            return PartialView("_ErrorPartial", vm);
        }

        return View(vm);
    }
    
    [HttpGet("status")]
    public IActionResult Status()
    {
        var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var originalPath = feature?.OriginalPath ?? "Unknown";
        return View(new PageNotFoundVm
        {
            OriginalPath = originalPath
        });
    }
}