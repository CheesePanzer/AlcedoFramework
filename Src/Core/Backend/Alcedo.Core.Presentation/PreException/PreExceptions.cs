using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Presentation.PreException;

/// <summary>
/// Thrown when model binding or FluentValidation rejects the HTTP request body.
/// Maps to 400.
/// </summary>
public class PreErrRequestInvalid : AlcedoBadRequestException
{
    public PreErrRequestInvalid(string errors) : base($"Request validation failed: {errors}")
    {
    }
}

public class PreErrIdempotencyKeyRequired : AlcedoBadRequestException
{
    public PreErrIdempotencyKeyRequired() : base("Request is missing a required idempotency key.")
    {
    }
}

public class PreErrIdempotencyKeyExpired : AlcedoBadRequestException
{
    public PreErrIdempotencyKeyExpired() : base("Idempotency key has expired. Please refresh and retry.")
    {
    }
}

public class PreErrIdempotencyDuplicate : AlcedoBadRequestException
{
    public PreErrIdempotencyDuplicate() : base("Duplicate request detected. Please do not resubmit.")
    {
    }
}

public class PreErrIdempotencyFieldMissing : AlcedoBadRequestException
{
    public PreErrIdempotencyFieldMissing() : base("No fields defined for content-based idempotency key.")
    {
    }

    public PreErrIdempotencyFieldMissing(string field) : base($"Content-based idempotency field not found: {field}")
    {
    }

    public PreErrIdempotencyFieldMissing(IEnumerable<string> fields) : base(
        $"Content-based idempotency fields not found: {string.Join(", ", fields)}")
    {
    }
}