using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.StateMachine;

namespace Alcedo.Frontend.Identity.Clients.User;

[AutoTableLink(nameof(Code), "/identity/users/{Id}")]
public sealed record UserDto : MasterDataDtoBase
{
    [AutoFormRenderHelper("The login Account of the user")]
    public string LoginName { get; set; } = null!;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Contact { get; set; }

    [AutoFormIgnoreRender] public bool RequiresPasswordChange { get; set; }

    [AutoFormIgnoreRender] public bool IsLocked { get; set; }

    [AutoFormIgnoreRender] public bool UseTwoFactorAuthenticator { get; set; }

    [AutoFormIgnoreRender] public IReadOnlyList<Guid> RoleIds { get; set; } = Array.Empty<Guid>();
}

internal class UserStateActions : MasterDataStateActions
{
    public static readonly MasterDataStateAction Lock = new MasterDataStateAction("Lock");
    public static readonly MasterDataStateAction UnLock = new MasterDataStateAction("UnLock");
    public static readonly MasterDataStateAction ResetPassword = new MasterDataStateAction("ResetPassword");
    public static readonly MasterDataStateAction ChangePassword = new MasterDataStateAction("ChangePassword");
    public static readonly MasterDataStateAction EnableTotp = new MasterDataStateAction("EnableTotp");
    public static readonly MasterDataStateAction DisableTotp = new MasterDataStateAction("DisableTotp");
    public static readonly MasterDataStateAction AssignRole = new MasterDataStateAction("AssignRole");
    public static readonly MasterDataStateAction BatchAssignRoles = new MasterDataStateAction("BatchAssignRoles");
    public static readonly MasterDataStateAction DeleteRole = new MasterDataStateAction("DeleteRole");
    public static readonly MasterDataStateAction ClearRoles = new MasterDataStateAction("ClearRoles");
}

internal class UserActionGuard : MasterDataArchiveOperationGuard
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [UserStateActions.Lock.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.UnLock.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.ResetPassword.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.ChangePassword.Value] = new HashSet<MasterDataStateValue> { MasterDataStates.Enabled },
            [UserStateActions.EnableTotp.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.DisableTotp.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.AssignRole.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.BatchAssignRoles.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.DeleteRole.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.ClearRoles.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
        };
}