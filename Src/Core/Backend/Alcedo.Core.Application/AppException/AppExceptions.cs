using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Dto;

namespace Alcedo.Core.Application.AppException;

public class AppErrGeneric : AlcedoBadRequestException
{
    public AppErrGeneric(string message) : base(message)
    {
    }

    public AppErrGeneric(string message, string friendly) : base(message, friendly)
    {
    }
}

public class AppErrQueryResultNotFound<TDto> : AlcedoNotFoundException
    where TDto : EntityDtoBase
{
    private const string Friendly = "The requested search has no result.";

    public AppErrQueryResultNotFound(Guid id) : base($"result {typeof(TDto)} with id:{id} was not found.", Friendly)
    {
    }
}

public class AppErrValidationFailed : AlcedoBadRequestException
{
    public AppErrValidationFailed(string errors) : base($"Validation failed: {errors}")
    {
    }
}