using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Kit.Test;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Identity.Test.Domain;

[Collection("Domain")]
public class UserAggTests
{
    private static OperatorRecord CreateOperator(string code = "op", string name = "Operator")
        => new(Guid.NewGuid(), code, name);

    private static UserAgg CreateUser(string loginName = "testlogin", string code = "USR", string name = "TestUser",
        string passwordHash = "$2a$12$hash", string? userName = null, string? email = null, string? contact = null,
        OperatorRecord? creator = null)
        => new(code, name, loginName, passwordHash, userName, email, contact, creator ?? CreateOperator());

    #region Construction

    [Fact]
    public void Constructor_SetsStatusToDraft()
    {
        var user = CreateUser();

        Assert.Equal(MasterDataStates.Draft, user.Status);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var user = CreateUser("mylogin", "USR", "MyName", "hash123", "MyUserName", "a@b.com", "12345");

        Assert.Equal("mylogin", user.LoginName);
        Assert.Equal("USR", user.Code);
        Assert.Equal("MyName", user.Name);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal("MyUserName", user.UserName);
        Assert.Equal("a@b.com", user.Email);
        Assert.Equal("12345", user.Contact);
    }

    [Fact]
    public void Constructor_SetsRequiresPasswordChangeTrue()
    {
        var user = CreateUser();

        Assert.True(user.RequiresPasswordChange);
    }

    [Fact]
    public void Constructor_CurrentSessionIsNull()
    {
        var user = CreateUser();

        Assert.Null(user.CurrentSession);
    }

    [Fact]
    public void Constructor_IsLockedIsFalse()
    {
        var user = CreateUser();

        Assert.False(user.IsLocked);
    }

    [Fact]
    public void Constructor_UseTwoFactorAuthenticatorIsFalse()
    {
        var user = CreateUser();

        Assert.False(user.UseTwoFactorAuthenticator);
    }

    [Fact]
    public void Constructor_CreatesAuditEvent()
    {
        var user = CreateUser();

        var events = user.CollectEventsAndClearForTest();
        Assert.Contains(events, e => e is MasterDataAuditLogEvent);
    }

    [Fact]
    public void Constructor_RoleIdsIsEmpty()
    {
        var user = CreateUser();

        Assert.Empty(user.RoleIds);
    }

    #endregion

    #region Enable / Disable

    [Fact]
    public void Enable_DraftToEnabled_StatusUpdatedAndJoinTimeSet()
    {
        var user = CreateUser();
        var oper = CreateOperator();

        user.Enable(oper);

        Assert.Equal(MasterDataStates.Enabled, user.Status);
        Assert.NotNull(user.JoinTime);
        Assert.True(user.JoinTime!.Value > DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Enable_AlreadyEnabled_Throws()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        Assert.Throws<DomErrStatusInvalid>(() => user.Enable(CreateOperator()));
    }

    [Fact]
    public void Disable_EnabledToDisabled_StatusUpdated()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        user.Disable(CreateOperator());

        Assert.Equal(MasterDataStates.Disabled, user.Status);
    }

    [Fact]
    public void Disable_Draft_Throws()
    {
        var user = CreateUser();

        Assert.Throws<DomErrStatusInvalid>(() => user.Disable(CreateOperator()));
    }

    [Fact]
    public void Enable_FromDisabled_StatusUpdated()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Disable(CreateOperator());

        user.Enable(CreateOperator());

        Assert.Equal(MasterDataStates.Enabled, user.Status);
    }

    #endregion

    #region Delete

    [Fact]
    public void Delete_Draft_Allowed()
    {
        var user = CreateUser();
        var oper = CreateOperator();

        user.Delete(oper);
    }

    [Fact]
    public void Delete_Enabled_Throws()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        Assert.Throws<DomErrStatusActionInvalid>(() => user.Delete(CreateOperator()));
    }

    [Fact]
    public void Delete_Disabled_Throws()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Disable(CreateOperator());

        Assert.Throws<DomErrStatusActionInvalid>(() => user.Delete(CreateOperator()));
    }

    #endregion

    #region Archive

    [Fact]
    public void Archive_Disabled_Allowed()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Disable(CreateOperator());
        var oper = CreateOperator();

        user.Archive(oper);
    }

    [Fact]
    public void Archive_Enabled_Throws()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        Assert.Throws<DomErrStatusActionInvalid>(() => user.Archive(CreateOperator()));
    }

    [Fact]
    public void Archive_Draft_Throws()
    {
        var user = CreateUser();

        Assert.Throws<DomErrStatusActionInvalid>(() => user.Archive(CreateOperator()));
    }

    #endregion

    #region Lock / Unlock

    [Fact]
    public void Lock_Enabled_IsLockedTrue()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = CreateOperator();

        user.Lock(oper);

        Assert.True(user.IsLocked);
    }

    [Fact]
    public void Lock_Disabled_IsLockedTrue()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Disable(CreateOperator());
        var oper = CreateOperator();

        user.Lock(oper);

        Assert.True(user.IsLocked);
    }

    [Fact]
    public void Lock_Draft_Throws()
    {
        var user = CreateUser();

        Assert.Throws<DomErrStatusActionInvalid>(() => user.Lock(CreateOperator()));
    }

    [Fact]
    public void Unlock_Locked_IsLockedFalse()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = CreateOperator();
        user.Lock(oper);

        user.Unlock(CreateOperator());

        Assert.False(user.IsLocked);
    }

    #endregion

    #region Session

    [Fact]
    public void CreateNewSession_SetsCurrentSession()
    {
        var user = CreateUser();

        user.CreateNewSession();

        Assert.NotNull(user.CurrentSession);
        Assert.NotEqual(Guid.Empty, user.CurrentSession!.Value);
    }

    [Fact]
    public void CreateNewSession_EachCallProducesUniqueSession()
    {
        var user = CreateUser();
        user.CreateNewSession();
        var session1 = user.CurrentSession;

        user.CreateNewSession();
        var session2 = user.CurrentSession;

        Assert.NotEqual(session1, session2);
    }

    [Fact]
    public void LogOut_ClearsCurrentSession()
    {
        var user = CreateUser();
        user.CreateNewSession();

        user.LogOut();

        Assert.Null(user.CurrentSession);
    }

    #endregion

    #region CanAccessContext

    [Fact]
    public void CanAccessContext_EnabledAndNotLocked_ReturnsTrue()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        Assert.True(user.CanAccessContext);
    }

    [Fact]
    public void CanAccessContext_Locked_ReturnsFalse()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Lock(CreateOperator());

        Assert.False(user.CanAccessContext);
    }

    [Fact]
    public void CanAccessContext_NotEnabled_ReturnsFalse()
    {
        var user = CreateUser();

        Assert.False(user.CanAccessContext);
    }

    [Fact]
    public void CanAccessContext_Disabled_ReturnsFalse()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.Disable(CreateOperator());

        Assert.False(user.CanAccessContext);
    }

    #endregion

    #region Password

    [Fact]
    public void ChangePassword_UpdatesHashAndClearsSession()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.CreateNewSession();
        var oper = CreateOperator();

        user.ChangePassword("newhash", oper);

        Assert.Equal("newhash", user.PasswordHash);
        Assert.False(user.RequiresPasswordChange);
        Assert.Null(user.CurrentSession);
    }

    [Fact]
    public void ChangePassword_WhenDraft_Throws()
    {
        var user = CreateUser();

        Assert.Throws<DomErrStatusActionInvalid>(() => user.ChangePassword("new", CreateOperator()));
    }

    [Fact]
    public void ResetPassword_UpdatesHashAndRequiresChange()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = CreateOperator();

        user.ResetPassword("newhash", oper);

        Assert.Equal("newhash", user.PasswordHash);
        Assert.True(user.RequiresPasswordChange);
        Assert.Null(user.CurrentSession);
    }

    #endregion

    #region Roles

    [Fact]
    public void BatchAssignRoles_AddsNewRoles()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = CreateOperator();
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        user.BatchAssignRoles([r1, r2], oper);

        Assert.Equal(2, user.RoleIds.Count);
        Assert.Contains(r1, user.RoleIds);
        Assert.Contains(r2, user.RoleIds);
    }

    [Fact]
    public void BatchAssignRoles_SkipsDuplicates()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var r1 = Guid.NewGuid();
        user.BatchAssignRoles([r1], CreateOperator());

        user.BatchAssignRoles([r1], CreateOperator());

        Assert.Single(user.RoleIds);
    }

    [Fact]
    public void BatchAssignRoles_EmptyList_NoChange()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());

        user.BatchAssignRoles([], CreateOperator());

        Assert.Empty(user.RoleIds);
    }

    [Fact]
    public void BatchAssignRoles_Draft_Throws()
    {
        var user = CreateUser();

        Assert.Throws<DomErrStatusActionInvalid>(() =>
            user.BatchAssignRoles([Guid.NewGuid()], CreateOperator()));
    }

    [Fact]
    public void BatchDeleteRoles_RemovesRoles()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();
        user.BatchAssignRoles([r1, r2], CreateOperator());

        user.BatchDeleteRoles([r1], CreateOperator());

        Assert.Single(user.RoleIds);
        Assert.Contains(r2, user.RoleIds);
        Assert.DoesNotContain(r1, user.RoleIds);
    }

    [Fact]
    public void BatchDeleteRoles_EmptyList_NoChange()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var r1 = Guid.NewGuid();
        user.BatchAssignRoles([r1], CreateOperator());

        user.BatchDeleteRoles([], CreateOperator());

        Assert.Single(user.RoleIds);
    }

    [Fact]
    public void ClearRoles_RemovesAllRoles()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        user.BatchAssignRoles([Guid.NewGuid(), Guid.NewGuid()], CreateOperator());

        user.ClearRoles(CreateOperator());

        Assert.Empty(user.RoleIds);
    }

    #endregion

    #region TwoFactor

    [Fact]
    public void EnableTwoFactorAuthenticator_SetsSecretAndFlag()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = new OperatorRecord(user.Id, user.Code, user.Name);

        user.EnableTwoFactorAuthenticator("TOTP_SECRET_123", oper);

        Assert.True(user.UseTwoFactorAuthenticator);
        Assert.Equal("TOTP_SECRET_123", user.TwoFactorSecret);
    }

    [Fact]
    public void EnableTwoFactorAuthenticator_Draft_Throws()
    {
        var user = CreateUser();
        var oper = new OperatorRecord(user.Id, user.Code, user.Name);

        Assert.Throws<DomErrStatusActionInvalid>(() =>
            user.EnableTwoFactorAuthenticator("secret", oper));
    }

    [Fact]
    public void DisableTwoFactorAuthenticator_ClearsSecretAndFlag()
    {
        var user = CreateUser();
        user.Enable(CreateOperator());
        var oper = new OperatorRecord(user.Id, user.Code, user.Name);
        user.EnableTwoFactorAuthenticator("secret", oper);

        user.DisableTwoFactorAuthenticator(oper);

        Assert.False(user.UseTwoFactorAuthenticator);
        Assert.Null(user.TwoFactorSecret);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ModifiesFields()
    {
        var user = CreateUser();
        var oper = CreateOperator();

        user.Update("NewName", "NewUserName", "new@b.com", "999", oper);

        Assert.Equal("NewName", user.Name);
        Assert.Equal("NewUserName", user.UserName);
        Assert.Equal("new@b.com", user.Email);
        Assert.Equal("999", user.Contact);
    }

    #endregion
}
