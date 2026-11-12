using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.Service.ContractImpl;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Shared.Application.Dto;
using Moq;

namespace Alcedo.Framework.Identity.Test.Application.Services;

public class OperatorModuleServiceTests
{
    private readonly Mock<IUserQueryRepository> _repoMock = new();
    private readonly OperatorModuleService _sut;

    public OperatorModuleServiceTests()
    {
        _sut = new OperatorModuleService(_repoMock.Object);
    }

    [Fact]
    public async Task GetCurrentOperatorAsync_UserFound_ReturnsOperator()
    {
        var userId = Guid.NewGuid();
        var expected = new OperatorDto { Id = userId, Code = "OP01", Name = "TestOp" };
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetCurrentOperatorAsync(userId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetCurrentOperatorAsync_UserDisabled_Throws()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperatorDto?)null);

        await Assert.ThrowsAsync<IdentityErrInvalidUser>(
            () => _sut.GetCurrentOperatorAsync(userId));
    }

    [Fact]
    public async Task TryGetCurrentOperatorAsync_UserFound_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        var expected = new OperatorDto { Id = userId, Code = "OP01", Name = "TestOp" };
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.TryGetCurrentOperatorAsync(userId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TryGetCurrentOperatorAsync_UserNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperatorDto?)null);

        var result = await _sut.TryGetCurrentOperatorAsync(userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentOperatorWithRolesAsync_UserFound_ReturnsWithRoles()
    {
        var userId = Guid.NewGuid();
        var expected = new OperatorDtoWithRoles
        {
            Id = userId, Code = "OP01", Name = "TestOp",
            RoleIds = [Guid.NewGuid()], IsRootUser = false
        };
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetCurrentOperatorWithRolesAsync(userId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetCurrentOperatorWithRolesAsync_UserNotFound_Throws()
    {
        _repoMock.Setup(r => r.GetEnabledUnlockedByIdWithRolesAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperatorDtoWithRoles?)null);

        await Assert.ThrowsAsync<IdentityErrInvalidUser>(
            () => _sut.GetCurrentOperatorWithRolesAsync(Guid.NewGuid()));
    }
}
