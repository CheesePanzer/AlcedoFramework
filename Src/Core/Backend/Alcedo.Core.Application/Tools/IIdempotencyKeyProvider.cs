namespace Alcedo.Core.Application.Tools;

public interface IIdempotencyKeyProvider
{
    Task<string> GenerateStrictIdempotencyKeyAsync(string endpoint, int? expireyMinutes);

    Task<bool> ValidateAndConsumeStrictGeneratedIdempotencyKeyAsync(string endpoint, string token);

    Task<bool> ValidateContentBasedIdempotencyKeyAsync(string contentMd, string endpoint, int? expireyMinutes);

    Task<bool> ValidateAndRegisterSoftIdempotencyKeyAsync(string token, string endpoint, int? expireyMinutes);
}