using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.Command;

namespace Alcedo.Frontend.Identity.Clients.User;

public sealed record UserCreateCommand : MasterDataCreateCommandBase
{
    public string LoginName { get; set; } = null!;

    public string? UserName { get; set; }

    [AutoValidateEmail] public string? Email { get; set; }

    [AutoValidatePhone] public string? Contact { get; set; }

    [AutoFormIgnoreRender] public string PlainPassword { get; set; } = null!;
}

public sealed record UserUpdateCommand : MasterDataUpdateCommandBase
{
    public string? UserName { get; set; }

    [AutoValidateEmail] public string? Email { get; set; }

    [AutoValidatePhone] public string? Contact { get; set; }
}