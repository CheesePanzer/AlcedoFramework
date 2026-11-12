using Alcedo.Core.Domain;
using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Identity.Domain.Entity;

[AggregateRootDescription("Role")]
public sealed class RoleAgg : MasterDataAggregateRootBase
{
    protected override StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard =>
        new RoleActionGuard();

    public string? Description { get; private set; }

    private List<RolePermissionsRelation> _rolePermissions = new();
    public IReadOnlyList<RolePermissionsRelation> RolePermissions => _rolePermissions.AsReadOnly();

    public IReadOnlyList<string> AllowedEndpoints => _rolePermissions.Select(p => p.PermissionEndpoint).ToList();

    public bool IsEnabled => Status == MasterDataStates.Enabled;

    private RoleAgg()
    {
    }

    public RoleAgg(
        string code,
        string name,
        string? description,
        OperatorRecord creator
    ) :
        base(code,
            name,
            creator)
    {
        Description = description;
    }

    public void Update(
        string name,
        string? description,
        OperatorRecord updater)
    {
        base.Update(name, updater);
        Description = description;
    }

    private void AddRoleChangedEvt()
    {
        AddEvent(new RoleCacheRefreshEvent(Id));
    }

    public void BatchAssignPermissions(IReadOnlyCollection<string> permEndpoints, OperatorRecord oper)
    {
        if (permEndpoints.Count == 0)
        {
            return;
        }

        var existingEndpoints = AllowedEndpoints.ToHashSet();

        var toAdd = permEndpoints
            .Where(id => !existingEndpoints.Contains(id))
            .Distinct()
            .ToList();

        foreach (var permId in toAdd)
        {
            _rolePermissions.Add(new RolePermissionsRelation(Id, permId));
        }

        if (toAdd.Count > 0)
        {
            ExecuteAction(RoleStateActions.BatchAssignPerms, oper, string.Join(";", toAdd));
            AddRoleChangedEvt();
        }
    }

    public void BatchDeletePermissions(IReadOnlyCollection<string> endPoints, OperatorRecord oper)
    {
        if (endPoints.Count == 0)
        {
            return;
        }

        var permEndpoints = endPoints.ToHashSet();

        var toRemove = _rolePermissions
            .Where(rp => permEndpoints.Contains(rp.PermissionEndpoint))
            .ToList();

        foreach (var rp in toRemove)
        {
            _rolePermissions.Remove(rp);
        }

        if (toRemove.Count > 0)
        {
            ExecuteAction(RoleStateActions.DeletePerm, oper, string.Join(";", toRemove));
            AddRoleChangedEvt();
        }
    }

    public void ClearPermissions(OperatorRecord oper)
    {
        _rolePermissions.Clear();
        ExecuteAction(RoleStateActions.ClearPerms, oper);
        AddRoleChangedEvt();
    }

    public override void Enable(OperatorRecord oper)
    {
        base.Enable(oper);
        AddRoleChangedEvt();
    }

    public override void Disable(OperatorRecord oper)
    {
        base.Disable(oper);
        AddRoleChangedEvt();
    }
}

public sealed class RolePermissionsRelation : EntityBase
{
    public Guid RoleId { get; private set; }

    public string PermissionEndpoint { get; private set; }

    private RolePermissionsRelation()
    {
    }

    public RolePermissionsRelation(Guid roleId, string permissionEndpoint)
    {
        RoleId = roleId;

        PermissionEndpoint = permissionEndpoint;
    }
}

public sealed record RoleCacheRefreshEvent : EntityCacheRefreshEvent
{
    public RoleCacheRefreshEvent(Guid aggregateId) : base(aggregateId)
    {
    }
}

internal class RoleStateActions : MasterDataStateActions
{
    public static readonly MasterDataStateAction AssignPerm = new MasterDataStateAction("AssignPerm");
    public static readonly MasterDataStateAction BatchAssignPerms = new MasterDataStateAction("BatchAssignPerms");
    public static readonly MasterDataStateAction DeletePerm = new MasterDataStateAction("DeletePerm");
    public static readonly MasterDataStateAction ClearPerms = new MasterDataStateAction("ClearPerms");
}

internal class RoleActionGuard : MasterDataDefaultOperationGuard
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [RoleStateActions.AssignPerm.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [RoleStateActions.BatchAssignPerms.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [RoleStateActions.DeletePerm.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [RoleStateActions.ClearPerms.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
        };
}