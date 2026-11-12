namespace Alcedo.Framework.Identity.Contracts;

public sealed record UserInfoDto(
    Guid Id,
    string Code,
    string Name,
    string LoginName,
    string? UserName,
    string? Email,
    string? Contact
);