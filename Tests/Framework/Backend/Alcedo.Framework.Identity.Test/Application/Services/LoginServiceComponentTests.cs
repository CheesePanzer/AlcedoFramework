using Alcedo.Core.Application;
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Factory;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Application.Service.Auth;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared.Domain.Entity;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Alcedo.Framework.Identity.Test.Application.Services;

public class LoginServiceComponentTests
{
    private readonly Mock<ILoginThrottleRedisStore> _throttleMock = new();
    private readonly Mock<IRefreshTokenRedisStore> _refreshTokenMock = new();
    private readonly Mock<IEntityCacheProvider<UserCacheDto>> _userCacheMock = new();
    private readonly Mock<ILoginContextRedisStore> _loginContextMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IUnitOfWorkExecutor> _executorMock = new();
    private readonly Mock<IDistributedLockFactory> _lockFactoryMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly SecurityOptions _securityOptions;
    private readonly LoginServiceComponent _sut;

    public LoginServiceComponentTests()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        var lockProvider = new ApplicationLockProvider(_lockFactoryMock.Object, _loggerFactoryMock.Object);
        _securityOptions = new SecurityOptions
        {
            AccountLockThresholdCount = 5,
            LoginContextValiditySeconds = 180,
            AccessTokenValidMinutes = 15,
            RefreshTokenValidMinutes = 480
        };
        var key = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("a-32-character-secret-key-ok!"));
        var jwtProvider = new JwtProvider(
            Options.Create(new JwtOptions { Issuer = "Test", Audience = "Test", Key = key }),
            Options.Create(_securityOptions));
        var totpProvider = new TotpProvider(Options.Create(new TotpOptions { Issuer = "Alcedo" }),
            NullLogger<TotpProvider>.Instance);
        var throttleFactory = new LoginThrottleDecisionFactory(Options.Create(_securityOptions));

        _sut = new LoginServiceComponent(
            _throttleMock.Object, throttleFactory, _refreshTokenMock.Object,
            _userCacheMock.Object, _loginContextMock.Object, _userRepoMock.Object,
            jwtProvider, _mapperMock.Object, Options.Create(_securityOptions),
            _executorMock.Object, totpProvider, lockProvider, NullLogger<LoginServiceComponent>.Instance);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_Throws()
    {
        _userRepoMock.Setup(r => r.GetEnabledByLoginNameAsync("nouser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAgg?)null);
        await Assert.ThrowsAsync<IdentityErrWrongPasswordOrUsername>(() =>
            _sut.LoginAsync("nouser", "pass", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        var oper = new OperatorRecord(Guid.NewGuid(), "OP", "Operator");
        var user = new UserAgg("USR", "Test", "testlogin",
            PasswordHasher.Hash("correct"), null, null, null, oper);
        user.Enable(oper);
        _userRepoMock.Setup(r => r.GetEnabledByLoginNameAsync("testlogin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        await Assert.ThrowsAsync<IdentityErrWrongPasswordOrUsername>(() =>
            _sut.LoginAsync("testlogin", "wrongpass", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_UserIsLocked_Throws()
    {
        var oper = new OperatorRecord(Guid.NewGuid(), "OP", "Operator");
        var user = new UserAgg("USR", "Test", "lockeduser",
            PasswordHasher.Hash("correct"), null, null, null, oper);
        user.Enable(oper);
        user.Lock(oper);
        _userRepoMock.Setup(r => r.GetEnabledByLoginNameAsync("lockeduser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        await Assert.ThrowsAsync<IdentityErrWrongPasswordOrUsername>(() =>
            _sut.LoginAsync("lockeduser", "correct", "127.0.0.1", CancellationToken.None));
    }
}
