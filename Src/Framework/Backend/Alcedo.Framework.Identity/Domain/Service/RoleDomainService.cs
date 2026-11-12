using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Identity.Domain.Service;

[RegisterSelf]
internal sealed class RoleDomainService
{
    private readonly IMasterDataRepositoryBase<RoleAgg> _roleRepository;

    private readonly EndpointResourceOperationCenter _endpointResourceOperationCenter;

    public RoleDomainService(IMasterDataRepositoryBase<RoleAgg> roleRepository,
        EndpointResourceOperationCenter endpointResourceOperationCenter)
    {
        _roleRepository = roleRepository;
        _endpointResourceOperationCenter = endpointResourceOperationCenter;
    }

    public async Task AssignPermsToRoleAsync(Guid roleId, IReadOnlyCollection<string> permEndpoints,
        OperatorRecord oper,
        CancellationToken ct = default)
    {
        var role = await _roleRepository.GetByIdForUpdateAsync(roleId, ct) ??
                   throw new DomErrResourceNotFound<RoleAgg>(roleId);

        foreach (var perm in permEndpoints)
        {
            if (!_endpointResourceOperationCenter.IfEndPointKeyExists(perm))
            {
                throw new ArgumentException($"Endpoint {perm} not found");
            }
        }

        role.BatchAssignPermissions(permEndpoints, oper);
    }
}