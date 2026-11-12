using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Kit.Test;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Identity.Test.Domain;

[Collection("Domain")]
public class RoleAggTests
{
    private static OperatorRecord CreateOperator(string code = "op", string name = "Operator")
        => new(Guid.NewGuid(), code, name);

    private static RoleAgg CreateRole(string code = "ROLE", string name = "TestRole",
        string? description = null, OperatorRecord? creator = null)
        => new(code, name, description, creator ?? CreateOperator());

    #region Construction

    [Fact]
    public void Constructor_SetsStatusToDraft()
    {
        var role = CreateRole();

        Assert.Equal(MasterDataStates.Draft, role.Status);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var role = CreateRole("ADMIN", "Admin Role", "Admin description");

        Assert.Equal("ADMIN", role.Code);
        Assert.Equal("Admin Role", role.Name);
        Assert.Equal("Admin description", role.Description);
    }

    [Fact]
    public void Constructor_CreatesAuditEvent()
    {
        var role = CreateRole();

        var events = role.CollectEventsAndClearForTest();
        Assert.Contains(events, e => e is MasterDataAuditLogEvent);
    }

    [Fact]
    public void IsEnabled_Draft_ReturnsFalse()
    {
        var role = CreateRole();

        Assert.False(role.IsEnabled);
    }

    [Fact]
    public void IsEnabled_Enabled_ReturnsTrue()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());

        Assert.True(role.IsEnabled);
    }

    [Fact]
    public void IsEnabled_Disabled_ReturnsFalse()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        role.Disable(CreateOperator());

        Assert.False(role.IsEnabled);
    }

    #endregion

    #region Enable / Disable

    [Fact]
    public void Enable_DraftToEnabled_StatusUpdated()
    {
        var role = CreateRole();

        role.Enable(CreateOperator());

        Assert.Equal(MasterDataStates.Enabled, role.Status);
    }

    [Fact]
    public void Enable_AlreadyEnabled_Throws()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());

        Assert.Throws<DomErrStatusInvalid>(() => role.Enable(CreateOperator()));
    }

    [Fact]
    public void Disable_EnabledToDisabled_StatusUpdated()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());

        role.Disable(CreateOperator());

        Assert.Equal(MasterDataStates.Disabled, role.Status);
    }

    [Fact]
    public void Disable_Draft_Throws()
    {
        var role = CreateRole();

        Assert.Throws<DomErrStatusInvalid>(() => role.Disable(CreateOperator()));
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ModifiesFields()
    {
        var role = CreateRole();
        var oper = CreateOperator();

        role.Update("NewRole", "New desc", oper);

        Assert.Equal("NewRole", role.Name);
        Assert.Equal("New desc", role.Description);
    }

    #endregion

    #region Permissions

    [Fact]
    public void BatchAssignPermissions_AddsNewPermissions()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        var oper = CreateOperator();

        role.BatchAssignPermissions(["perm1", "perm2"], oper);

        Assert.Equal(2, role.AllowedEndpoints.Count);
        Assert.Contains("perm1", role.AllowedEndpoints);
        Assert.Contains("perm2", role.AllowedEndpoints);
    }

    [Fact]
    public void BatchAssignPermissions_SkipsDuplicates()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        role.BatchAssignPermissions(["perm1"], CreateOperator());

        role.BatchAssignPermissions(["perm1"], CreateOperator());

        Assert.Single(role.AllowedEndpoints);
    }

    [Fact]
    public void BatchAssignPermissions_EmptyList_NoChange()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());

        role.BatchAssignPermissions([], CreateOperator());

        Assert.Empty(role.AllowedEndpoints);
    }

    [Fact]
    public void BatchAssignPermissions_Draft_Throws()
    {
        var role = CreateRole();

        Assert.Throws<DomErrStatusActionInvalid>(() =>
            role.BatchAssignPermissions(["perm1"], CreateOperator()));
    }

    [Fact]
    public void BatchDeletePermissions_RemovesPermissions()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        role.BatchAssignPermissions(["perm1", "perm2"], CreateOperator());

        role.BatchDeletePermissions(["perm1"], CreateOperator());

        Assert.Single(role.AllowedEndpoints);
        Assert.Contains("perm2", role.AllowedEndpoints);
    }

    [Fact]
    public void BatchDeletePermissions_EmptyList_NoChange()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        role.BatchAssignPermissions(["perm1"], CreateOperator());

        role.BatchDeletePermissions([], CreateOperator());

        Assert.Single(role.AllowedEndpoints);
    }

    [Fact]
    public void ClearPermissions_RemovesAll()
    {
        var role = CreateRole();
        role.Enable(CreateOperator());
        role.BatchAssignPermissions(["perm1", "perm2", "perm3"], CreateOperator());

        role.ClearPermissions(CreateOperator());

        Assert.Empty(role.AllowedEndpoints);
    }

    #endregion
}
