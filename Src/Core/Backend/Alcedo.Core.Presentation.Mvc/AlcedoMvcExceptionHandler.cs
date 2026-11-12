using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Alcedo.Core.Presentation.Mvc;

public sealed class AlcedoMvcExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Request.Headers.Accept.All(a => a?.Contains("text/html") != true))
        {
            return false;
        }
        
        bool isHxRequest = httpContext.Request.Headers.ContainsKey("HX-Request");
        
        if (isHxRequest)
        {
            httpContext.Response.Redirect("/Error?isHx=true");
        }
        else
        {
            httpContext.Response.Redirect("/Error");
        }
        
        return true;
    }
}