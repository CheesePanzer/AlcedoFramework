using Alcedo.Core.Domain;
using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Identity.Domain.Entity;

[AggregateRootDescription("User")]
public sealed class UserAgg : MasterDataAggregateRootBase
{
    protected override StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard =>
        new UserActionGuard();

    public string LoginName { get; private set; }

    public string? UserName { get; private set; }

    public string? Email { get; private set; }

    public string? Contact { get; private set; }

    public string PasswordHash { get; private set; }

    public bool RequiresPasswordChange { get; private set; }

    public string? TwoFactorSecret { get; private set; }

    public bool UseTwoFactorAuthenticator { get; private set; }

    public Guid? CurrentSession { get; private set; }

    public DateTimeOffset? JoinTime { get; private set; }

    public bool IsLocked { get; private set; }

    public bool IsRootUser { get; private set; }

    public bool CanAccessContext => !IsLocked && Status == MasterDataStates.Enabled;

    public IReadOnlyList<Guid> RoleIds
        => UserRoles.Select(x => x.RoleId).ToList().AsReadOnly();

    private List<UserRolesRelation> _userRoles = new();
    public IReadOnlyList<UserRolesRelation> UserRoles => _userRoles.AsReadOnly();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private UserAgg()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public UserAgg(
        string code,
        string name,
        string loginName,
        string passwordHash,
        string? userName,
        string? email,
        string? contact,
        OperatorRecord creator
    ) :
        base(code,
            name,
            creator)
    {
        LoginName = loginName;
        UserName = userName;
        Email = email;
        Contact = contact;
        PasswordHash = passwordHash;
        CurrentSession = null;
        RequiresPasswordChange = true;
    }

    public void Update(
        string name,
        string? userName,
        string? email,
        string? contact,
        OperatorRecord updater)
    {
        base.Update(name, updater);
        UserName = userName;
        Email = email;
        Contact = contact;
    }

    /// <summary>
    /// return a bool to identify whether the login is a success
    /// </summary>
    /// <param name="plainPassword"></param>
    /// <returns></returns>
    /// <exception cref="BErrorUserLocked"></exception>
    public void CreateNewSession()
    {
        CurrentSession = Guid.NewGuid();
    }

    public void LogOut()
    {
        CurrentSession = null;
    }

    /// <summary>
    /// Ensures the target user is not the Root user or the operator themselves.
    /// This prevents "suicide" actions, privilege escalation, or system deadlocks.
    /// </summary>
    /// <param name="operId"></param>
    private void ValidateTargetIsNotSelf(Guid operId)
    {
        if (Id == operId)
        {
            throw new DomErrRuleViolation("Cannot apply this operation to yourself");
        }
    }

    /// <summary>
    /// For user To change his password
    /// </summary>
    public void ChangePassword(string newpasswordHash, OperatorRecord oper)
    {
        PasswordHash = newpasswordHash;
        RequiresPasswordChange = false;
        CurrentSession = null; //Go Offline from current session
        ExecuteAction(UserStateActions.ChangePassword, oper);
    }

    /// <summary>
    /// Reset Password Form Backend
    /// </summary>
    public void ResetPassword(string newpasswordHash, OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);
        PasswordHash = newpasswordHash;
        RequiresPasswordChange = true;
        CurrentSession = null; //Go Offline from current session
        ExecuteAction(UserStateActions.ResetPassword, oper);
    }

    public void Lock(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        IsLocked = true;
        ExecuteAction(UserStateActions.Lock, oper);
        AddUserCacheRefreshEvt();
    }

    public void Unlock(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        IsLocked = false;
        ExecuteAction(UserStateActions.UnLock, oper);
        AddUserCacheRefreshEvt();
    }

    private void AddUserCacheRefreshEvt()
    {
        AddEvent(new UserCacheRefreshEvent(Id));
    }

    public void BatchAssignRoles(IReadOnlyCollection<Guid> roleIds, OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        var existingRoleIds = UserRoles.Select(ur => ur.RoleId).ToHashSet();

        var toAdd = roleIds
            .Where(id => !existingRoleIds.Contains(id))
            .Distinct()
            .ToList();

        foreach (var roleId in toAdd)
        {
            _userRoles.Add(new UserRolesRelation(Id, roleId));
        }

        if (toAdd.Count > 0)
        {
            ExecuteAction(UserStateActions.BatchAssignRoles, oper, string.Join(";", toAdd));
            AddUserCacheRefreshEvt();
        }
    }

    public void BatchDeleteRoles(IReadOnlyCollection<Guid> roleIds, OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        var roleIdSet = roleIds.ToHashSet();

        var toRemove = UserRoles
            .Where(ur => roleIdSet.Contains(ur.RoleId))
            .ToList();

        foreach (var ur in toRemove)
        {
            _userRoles.Remove(ur);
        }

        if (toRemove.Count > 0)
        {
            ExecuteAction(UserStateActions.DeleteRole, oper, string.Join(";", toRemove.Select(t => t.RoleId)));
            AddUserCacheRefreshEvt();
        }
    }

    public void ClearRoles(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        _userRoles.Clear();
        ExecuteAction(UserStateActions.ClearRoles, oper);
        AddUserCacheRefreshEvt();
    }

    public override void Enable(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);
        JoinTime = DateTimeOffset.UtcNow;
        base.Enable(oper);
        AddUserCacheRefreshEvt();
    }

    public override void Disable(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        base.Disable(oper);
        AddUserCacheRefreshEvt();
    }

    public override void Delete(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        base.Delete(oper);
    }

    public void Archive(OperatorRecord oper)
    {
        ValidateTargetIsNotSelf(oper.Id);

        ExecuteAction(MasterDataStateActions.Archive, oper);
    }

    public void EnableTwoFactorAuthenticator(string secret, OperatorRecord oper)
    {
        if (oper.Id != Id)
        {
            throw new DomErrRuleViolation("Cannot enable two-factor authentication for another user");
        }
        UseTwoFactorAuthenticator = true;

        TwoFactorSecret = secret;

        ExecuteAction(UserStateActions.EnableTotp, oper);
    }

    public void DisableTwoFactorAuthenticator(OperatorRecord oper)
    {
        UseTwoFactorAuthenticator = false;

        TwoFactorSecret = null;

        ExecuteAction(UserStateActions.DisableTotp, oper);
    }
}

internal class UserReferenceConfiguration : IAggregateRootReferenceConfiguration<UserAgg>
{
    public void Configure(ReferenceEntityBuilder<UserAgg> builder)
    {
        builder
            .Through(ur => ur.UserRoles)
            .References<RoleAgg>(ur => ur.RoleId);
    }
}

public sealed class UserRolesRelation : EntityBase
{
    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    private UserRolesRelation()
    {
    }

    public UserRolesRelation(Guid userId, Guid roleId)
    {
        UserId = userId;

        RoleId = roleId;
    }
}

public sealed record UserCacheRefreshEvent : EntityCacheRefreshEvent
{
    public UserCacheRefreshEvent(Guid aggregateId) : base(aggregateId)
    {
    }
}

public sealed class HistoryUserEntity : EntityBase
{
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string LoginName { get; private set; }

    public string? UserName { get; private set; }

    public string? Email { get; private set; }

    public string? Contact { get; private set; }

    public bool IsLocked { get; private set; }

    public DateTimeOffset JoinTime { get; private set; }

    public DateTimeOffset ArchivedAt { get; private set; }

    public OperatorRecord Operator { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HistoryUserEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public HistoryUserEntity(
        UserAgg userAgg,
        OperatorRecord oper)
    {
        Id = userAgg.Id;
        Code = userAgg.Code;
        Name = userAgg.Name;
        LoginName = userAgg.LoginName;
        UserName = userAgg.UserName;
        Email = null;
        Contact = null;
        IsLocked = userAgg.IsLocked;
        JoinTime = userAgg.JoinTime ?? default;
        ArchivedAt = DateTimeOffset.UtcNow;
        Operator = oper;
    }
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