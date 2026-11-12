using System.ComponentModel.DataAnnotations;
using Alcedo.Framework.Identity.Application.Helper;
using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Dto.User;

public sealed record UserDto : MasterDataDtoBase
{
    public required string LoginName { get; init; }

    public required string? UserName { get; init; }

    public required string? Email { get; init; }

    public required string? Contact { get; init; }

    public required bool RequiresPasswordChange { get; init; }

    public required bool IsLocked { get; init; }

    public required bool UseTwoFactorAuthenticator { get; init; }

    public required IReadOnlyList<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}

public sealed record UserChangePasswordCommand : IValidatableObject
{
    public required string OldPassword { get; init; }

    public required string NewPassword { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PasswordStrengthChecker.IsStrong(NewPassword))
        {
            yield return new ValidationResult("Password Is too Weak", new[] { nameof(NewPassword) });
        }
    }
}