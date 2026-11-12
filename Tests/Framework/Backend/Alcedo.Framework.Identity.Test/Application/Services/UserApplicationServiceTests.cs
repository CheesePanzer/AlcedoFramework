using Alcedo.Core.Application;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Factory;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.Auth;
using Alcedo.Framework.Identity.Application.Service.User;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Domain.Service;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Alcedo.Framework.Identity.Test.Application.Services;

internal class FakeLockHandle : IAsyncDisposable
{
    public bool Disposed { get; private set; }
    public ValueTask DisposeAsync() { Disposed = true; return default; }
}

[Collection("Domain")]
public class UserApplicationServiceTests
{
    private readonly Mock<IAggRefChecker> _refCheckerMock = new();
    private readonly Mock<IUnitOfWorkExecutor> _executorMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ILoginThrottleRedisStore> _throttleMock = new();
    private readonly Mock<IUserQueryRepository> _queryRepoMock = new();
    private readonly Mock<IMasterDataRepositoryBase<UserAgg>> _masterDataRepoMock = new();
    private readonly Mock<IMasterDataRepositoryBase<RoleAgg>> _roleRepoMock = new();
    private readonly Mock<IEntityQueryRepository<UserAgg, UserDto, UserSearchQuery>> _entityQueryRepoMock = new();
    private readonly Mock<IMasterDataAuditLogQueryRepository<UserAgg>> _logQueryRepoMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<IRefreshTokenRedisStore> _refreshTokenMock = new();
    private readonly Mock<IEntityCacheProvider<UserCacheDto>> _userCacheMock = new();
    private readonly Mock<ILoginContextRedisStore> _loginContextMock = new();
    private readonly Mock<MapsterMapper.IMapper> _mapperMock = new();
    private readonly Mock<IDistributedLockFactory> _lockFactoryMock = new();

    private readonly UserApplicationService _sut;

    public UserApplicationServiceTests()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var editComponent = new UserEditServiceComponent(
            _userRepoMock.Object, _loggerFactoryMock.Object,
            _refCheckerMock.Object, _executorMock.Object);

        var searchComponent = new MasterDataSearchServiceComponent<UserAgg, UserDto, UserSearchQuery>(
            _entityQueryRepoMock.Object);

        var statusComponent = new MasterDataStatusServiceComponent<UserAgg>(
            _masterDataRepoMock.Object, _executorMock.Object, _refCheckerMock.Object);

        var logComponent = new MasterDataLogSearchService<UserAgg>(_logQueryRepoMock.Object);

        var userDomainService = new UserDomainService(_userRepoMock.Object, _roleRepoMock.Object);

        var totpProvider = new TotpProvider(Options.Create(new TotpOptions { Issuer = "Test" }),
            NullLogger<TotpProvider>.Instance);

        var securityOptions = new SecurityOptions
        {
            AccountLockThresholdCount = 5, AccessTokenValidMinutes = 15,
            RefreshTokenValidMinutes = 480, LoginContextValiditySeconds = 180
        };
        var key = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("a-32-character-secret-key-ok!"));
        var jwtProvider = new JwtProvider(
            Options.Create(new JwtOptions { Issuer = "Test", Audience = "Test", Key = key }),
            Options.Create(securityOptions));
        var throttleFactory = new LoginThrottleDecisionFactory(Options.Create(securityOptions));

        _lockFactoryMock
            .Setup(f => f.TryAcquireReadLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeLockHandle());
        _lockFactoryMock
            .Setup(f => f.TryAcquireWriteLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeLockHandle());

        var lockProvider = new ApplicationLockProvider(_lockFactoryMock.Object, _loggerFactoryMock.Object);

        var loginService = new LoginServiceComponent(
            _throttleMock.Object, throttleFactory,
            _refreshTokenMock.Object, _userCacheMock.Object,
            _loginContextMock.Object, _userRepoMock.Object,
            jwtProvider, _mapperMock.Object,
            Options.Create(securityOptions), _executorMock.Object,
            totpProvider, lockProvider,
            NullLogger<LoginServiceComponent>.Instance);

        _sut = new UserApplicationService(
            searchComponent, editComponent, statusComponent,
            _refCheckerMock.Object, _executorMock.Object, _userRepoMock.Object,
            userDomainService, _throttleMock.Object, _queryRepoMock.Object,
            loginService, logComponent, _refCheckerMock.Object);
    }

    [Fact]
    public async Task LockUserAsync_UserFound_LocksAndClearsSession()
    {
        var userId = Guid.NewGuid();
        var oper = new ContextOper { Id = Guid.NewGuid(), Code = "OP", Name = "Operator" };
        var userOper = new OperatorRecord(oper.Id, oper.Code, oper.Name);
        var user = new UserAgg("USR", "TestUser", "login",
            PasswordHasher.Hash("pass"), null, null, null, userOper);
        user.Enable(userOper);
        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _executorMock.Setup(e => e.ExecuteAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((fn, _) => fn());

        await _sut.LockUserAsync(userId, oper);

        Assert.True(user.IsLocked);
    }

    [Fact]
    public async Task UnlockUserAsync_UserFound_UnlocksAndResetsThrottle()
    {
        var userId = Guid.NewGuid();
        var oper = new ContextOper { Id = Guid.NewGuid(), Code = "OP", Name = "Operator" };
        var userOper = new OperatorRecord(oper.Id, oper.Code, oper.Name);
        var user = new UserAgg("USR", "TestUser", "login",
            PasswordHasher.Hash("pass"), null, null, null, userOper);
        user.Enable(userOper);
        user.Lock(userOper);
        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _executorMock.Setup(e => e.ExecuteAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<string>> fn, CancellationToken _) => fn());
        _throttleMock.Setup(t => t.ResetFailureCountByLoginNameAsync("login"))
            .Returns(Task.CompletedTask);

        await _sut.UnlockUserAsync(userId, oper);

        Assert.False(user.IsLocked);
        _throttleMock.Verify(t => t.ResetFailureCountByLoginNameAsync("login"), Times.Once);
    }
}
