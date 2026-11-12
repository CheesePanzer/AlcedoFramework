using System.Data;
using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation;

public static class AlcedoProblemDetailsFactory
{
    public static bool DisplaySensitiveExceptions { get; set; } = false;

    public static ProblemDetails Create(
        HttpContext httpContext,
        Exception exception,
        string? originalRequestPath = null)
    {
        var (statusCode, title) = exception switch
        {
            AlcedoNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            AlcedoUnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            AlcedoForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            AlcedoBadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            AlcedoConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            AlcedoTooManyRequestsException => (StatusCodes.Status429TooManyRequests, "Too Many Requests"),
            AlcedoException => (StatusCodes.Status500InternalServerError, "Business Error"),
            DBConcurrencyException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        string detail;

        if (!DisplaySensitiveExceptions)
        {
            detail = exception is AlcedoException ae
                ? ae.FriendlyMessage
                : "Unhandled Exception";
        }
        else
        {
            detail = exception.Message;
        }


        var pd = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = originalRequestPath ?? httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        if (exception is AlcedoException ae1)
        {
            pd.Extensions["innerErrorCode"] = ae1.Code;
        }

        if (exception is AlcedoException ae2 && ae2.ClientAction is not null)
        {
            pd.Extensions["clientAction"] = ae2.ClientAction;
        }

        return pd;
    }
}