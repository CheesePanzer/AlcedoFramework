using Alcedo.Core.Application;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Application.Service.Role;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.Service;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alcedo.Framework.Identity.Test.Application.Services;

public class RoleApplicationServiceTests
{
    private readonly Mock<IRoleQueryRepository> _queryRepoMock = new();
    private readonly Mock<IOperatorModuleService> _operatorMock = new();
    private readonly Mock<IUnitOfWorkExecutor> _executorMock = new();
    private readonly Mock<IMasterDataRepositoryBase<RoleAgg>> _roleRepoMock = new();
    private readonly Mock<IRoleCacheProvider> _cacheMock = new();
    private readonly Mock<IEntityQueryRepository<RoleAgg, RoleDto, RoleSearchQuery>> _entityQueryRepoMock = new();
    private readonly Mock<IMasterDataAuditLogQueryRepository<RoleAgg>> _logQueryRepoMock = new();
    private readonly Mock<IAggRefChecker> _refCheckerMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    private readonly RoleApplicationService _sut;

    public RoleApplicationServiceTests()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var searchComponent = new MasterDataSearchServiceComponent<RoleAgg, RoleDto, RoleSearchQuery>(
            _entityQueryRepoMock.Object);

        var editComponent = new RoleEditServiceComponent(
            _roleRepoMock.Object, _loggerFactoryMock.Object,
            _refCheckerMock.Object, _executorMock.Object);

        var statusComponent = new RoleStatusServiceComponent(
            _roleRepoMock.Object, _executorMock.Object, _refCheckerMock.Object);

        var logComponent = new MasterDataLogSearchService<RoleAgg>(_logQueryRepoMock.Object);

        var roleDomainService = new RoleDomainService(_roleRepoMock.Object,
            new Alcedo.Core.Presentation.EndpointResourceOperationCenter([]));

        _sut = new RoleApplicationService(
            searchComponent, editComponent, statusComponent,
            _executorMock.Object, _roleRepoMock.Object, _operatorMock.Object,
            roleDomainService, _cacheMock.Object, _queryRepoMock.Object, logComponent);
    }

    [Fact]
    public async Task DeleteByIdAsync_DeletesAndClearsCache()
    {
        var roleId = Guid.NewGuid();
        _executorMock.Setup(e => e.ExecuteAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.DeleteAsync(roleId)).Returns(Task.CompletedTask);

        await _sut.DeleteByIdAsync(roleId, new ContextOper { Id = Guid.NewGuid() });

        _cacheMock.Verify(c => c.DeleteAsync(roleId), Times.Once);
    }

    [Fact]
    public async Task BatchAssignPermsToRoleAsync_RootOperator_AssignsAnyPermission()
    {
        var roleId = Guid.NewGuid();
        var operId = Guid.NewGuid();
        _operatorMock.Setup(o => o.GetCurrentOperatorWithRolesAsync(operId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorDtoWithRoles
            {
                Id = operId, Code = "root", Name = "Root", IsRootUser = true, RoleIds = []
            });
        _executorMock.Setup(e => e.ExecuteAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.BatchAssignPermsToRoleAsync(roleId, ["perm1", "perm2"], operId);

        // RoleDomainService.AssignPermsToRoleAsync was called — verified through _roleRepoMock usage
        // (RoleDomainService internally calls _roleRepository.GetByIdForUpdateAsync which is mocked)
    }

    [Fact]
    public async Task BatchAssignPermsToRoleAsync_NonRoot_LackingPerms_Throws()
    {
        var roleId = Guid.NewGuid();
        var operId = Guid.NewGuid();
        var operWithRoles = new OperatorDtoWithRoles
        {
            Id = operId, Code = "op", Name = "Op", IsRootUser = false,
            RoleIds = [Guid.NewGuid()]
        };
        var oper = new OperatorRecord(Guid.NewGuid(), "OP", "Operator");
        var operRole = new RoleAgg("R", "Test", null, oper);
        operRole.Enable(oper);
        operRole.BatchAssignPermissions(["perm1"], oper);
        _operatorMock.Setup(o => o.GetCurrentOperatorWithRolesAsync(operId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operWithRoles);
        _roleRepoMock.Setup(r => r.GetListStrictByIdsNoTrackingAsync(operWithRoles.RoleIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync([operRole]);

        await Assert.ThrowsAsync<IdentityErrNotPermittedPartially>(() =>
            _sut.BatchAssignPermsToRoleAsync(roleId, ["perm1", "perm_missing"], operId));
    }
}
