using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Presentation.Middlewares;

public sealed class AlcedoErrorExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AlcedoErrorExceptionHandler> _logger;

    public AlcedoErrorExceptionHandler(ILogger<AlcedoErrorExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Log(exception, httpContext);

        var pd = AlcedoProblemDetailsFactory.Create(httpContext, exception);

        httpContext.Response.StatusCode = pd.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(pd, cancellationToken);

        return true;
    }

    private void Log(Exception exception, HttpContext httpContext)
    {
        _logger.LogError(exception, "Type={Type} Path={Path} Message={Message}",
            exception.GetType().Name,
            httpContext.Request.Path,
            exception.Message);
    }
}