using System.ComponentModel.DataAnnotations;
using Alcedo.Framework.Identity.Application.Helper;
using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.Framework.Identity.Application.Command;

public sealed record UserCreateCommand : MasterDataCreateCommandBase, IValidatableObject
{
    public required string LoginName { get; init; }

    public string? UserName { get; init; }

    public string? Email { get; init; }

    public string? Contact { get; init; }

    public required string PlainPassword { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PasswordStrengthChecker.IsStrong(PlainPassword))
        {
            yield return new ValidationResult("Password Is too Weak", new[] { nameof(PlainPassword) });
        }

        if (Contact is not null && !PhoneNumberFormatValidator.IsValid(Contact, out var result))
        {
            yield return new ValidationResult(result ?? "Contact Is Not Valid", new[] { nameof(Contact) });
        }

        if (Email is not null && !EmailFormatChecker.IsValid(Email, out var emailResult))
        {
            yield return new ValidationResult(emailResult ?? "Email Is Not Valid", new[] { nameof(Email) });
        }
    }
}

public sealed record UserUpdateCommand : MasterDataUpdateCommandBase, IValidatableObject
{
    public string? UserName { get; init; }

    public string? Email { get; init; }

    public string? Contact { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Contact is not null && !PhoneNumberFormatValidator.IsValid(Contact, out var result))
        {
            yield return new ValidationResult(result ?? "Contact Is Not Valid", new[] { nameof(Contact) });
        }

        if (Email is not null && !EmailFormatChecker.IsValid(Email, out var emailResult))
        {
            yield return new ValidationResult(emailResult ?? "Email Is Not Valid", new[] { nameof(Email) });
        }
    }
}