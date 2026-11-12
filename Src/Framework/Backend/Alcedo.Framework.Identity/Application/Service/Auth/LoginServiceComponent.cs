using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Identity.Application.Dto.Auth;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Event;
using Alcedo.Framework.Identity.Application.Factory;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Domain.Entity;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Application.Service.Auth;

[RegisterSelf]
internal sealed class LoginServiceComponent
{
    private readonly ILoginThrottleRedisStore _loginThrottleRedisStore;
    private readonly LoginThrottleDecisionFactory _throttleDecisionFactory;
    private readonly IRefreshTokenRedisStore _refreshTokenRedisStore;
    private readonly IEntityCacheProvider<UserCacheDto> _userCacheProvider;
    private readonly ILoginContextRedisStore _loginContextRedisStore;
    private readonly IUserRepository _userRepository;
    private readonly JwtProvider _jwtProvider;
    private readonly IMapper _mapper;
    private readonly SecurityOptions _securityOptions;
    private readonly IUnitOfWorkExecutor _executor;
    private readonly TotpProvider _totpProvider;
    private readonly ApplicationLockProvider _applicationLockProvider;
    private readonly ILogger<LoginServiceComponent> _logger;
    private const string LoginScope = "Login";

    public LoginServiceComponent(ILoginThrottleRedisStore loginThrottleRedisStore,
        LoginThrottleDecisionFactory throttleDecisionFactory, IRefreshTokenRedisStore refreshTokenRedisStore,
        IEntityCacheProvider<UserCacheDto> userCacheProvider, ILoginContextRedisStore loginContextRedisStore,
        IUserRepository userRepository,
        JwtProvider jwtProvider, IMapper mapper,
        IOptions<SecurityOptions> securityOptions, IUnitOfWorkExecutor executor, TotpProvider totpProvider,
        ApplicationLockProvider applicationLockProvider, ILogger<LoginServiceComponent> logger)
    {
        _loginThrottleRedisStore = loginThrottleRedisStore;
        _throttleDecisionFactory = throttleDecisionFactory;
        _refreshTokenRedisStore = refreshTokenRedisStore;
        _userCacheProvider = userCacheProvider;
        _loginContextRedisStore = loginContextRedisStore;
        _userRepository = userRepository;
        _jwtProvider = jwtProvider;
        _mapper = mapper;
        _securityOptions = securityOptions.Value;
        _executor = executor;
        _totpProvider = totpProvider;
        _applicationLockProvider = applicationLockProvider;
        _logger = logger;
    }

    #region Public Entry

    public async Task<(AuthTokenPasswordPhaseResult result, Guid refreshToken)> LoginAsync(
        string loginName, string password, string ip, CancellationToken ct)
    {
        var (success, userId, useTotp) =
            await InternalVerifyCredentialsAsync(loginName, password, ct);

        if (!success)
        {
            await ProcessLoginFailureAsync(loginName, ip, "Password Incorrect", ct);
            throw new IdentityErrWrongPasswordOrUsername();
        }

        return useTotp
            ? await HandlePendingTotpFlowAsync(userId)
            : await HandleDirectLoginFlowAsync(userId, ip, ct);
    }

    public async Task<(AuthTokenTotpPhaseResult result, Guid refreshToken)> VerifyTotpAsync(
        string pendingToken, string totpCode, string ip, CancellationToken ct)
    {
        var context = await _loginContextRedisStore.GetAsync(pendingToken) ??
                      throw new IdentityErrWrongPasswordOrUsername();
        var user = await _userRepository.GetEnabledByIdAsync(context.UserId, ct) ??
                   throw new IdentityErrWrongPasswordOrUsername();

        if (!user.CanAccessContext)
        {
            throw new IdentityErrUserLocked();
        }

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret) ||
            !_totpProvider.VerifyCode(user.TwoFactorSecret, totpCode))
        {
            await _loginContextRedisStore.DeleteAsync(pendingToken);
            await ProcessLoginFailureAsync(user.LoginName, ip, "Totp Incorrect", ct);
            throw new IdentityErrMfa();
        }

        await _loginContextRedisStore.DeleteAsync(pendingToken);

        var result = await FinalizeLoginOnSuccess(context.UserId, ip, ct);

        return (new AuthTokenTotpPhaseResult
        {
            AccessToken = result.accessToken,
            RequirePasswordReset = result.requirePasswordChange,
            ValidUntil = DateTimeOffset.UtcNow.AddMinutes(_securityOptions.RefreshTokenValidMinutes)
        }, result.refreshToken);
    }

    public async Task LogoutAsync(Guid refreshToken)
    {
        var userId = await _refreshTokenRedisStore.RemoveByRefreshTokenAsync(refreshToken);

        if (userId is null)
        {
            return;
        }

        await _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId.Value)
                       ?? throw new IdentityErrInvalidUser();
            user.LogOut();
            _logger.LogInformation("User {UserId} logged out", userId);
        });

        await _userCacheProvider.DeleteAsync(userId.Value);
    }

    public async Task ClearUserSessionAsync(Guid userId)
    {
        try
        {
            await using var locks =
                await _applicationLockProvider.CreateLock().AddS<UserAgg>(userId, LoginScope).LockAsync(5);
            await Task.WhenAll(
                _refreshTokenRedisStore.RemoveByUserIdAsync(userId),
                _userCacheProvider.DeleteAsync(userId)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear user session for user {userId}", userId);
            throw;
        }
    }

    #endregion

    #region Password Login along with other mfa method

    private async Task<(AuthTokenPasswordPhaseResult, Guid)> HandleDirectLoginFlowAsync(Guid userId, string ip,
        CancellationToken ct)
    {
        var result = await FinalizeLoginOnSuccess(userId, ip, ct);

        return (new AuthTokenPasswordPhaseResult
        {
            AccessToken = result.accessToken,
            UseTotp = false,
            PendingToken = null,
            RequirePasswordReset = result.requirePasswordChange,
            ValidUntil = DateTimeOffset.UtcNow.AddMinutes(_securityOptions.RefreshTokenValidMinutes)
        }, result.refreshToken);
    }

    private async Task<(bool Success, Guid userId, bool UseTotp)>
        InternalVerifyCredentialsAsync(string loginName, string password, CancellationToken ct)
    {
        var user = await _userRepository.GetEnabledByLoginNameAsync(loginName, ct);

        if (user is null || !user.CanAccessContext)
        {
            PasswordHasher.FakeVerify(password);
            return (false, Guid.Empty, false);
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            return (false, Guid.Empty, false);
        }

        return (true, user.Id, user.UseTwoFactorAuthenticator);
    }

    private async Task<(AuthTokenPasswordPhaseResult, Guid)> HandlePendingTotpFlowAsync(Guid userId)
    {
        var pendingToken = Guid.NewGuid();
        var context = new LoginContextData(userId);
        await _loginContextRedisStore.SaveAsync(pendingToken.ToString(), context,
            TimeSpan.FromSeconds(_securityOptions.LoginContextValiditySeconds));

        return (new AuthTokenPasswordPhaseResult
        {
            AccessToken = string.Empty,
            UseTotp = true,
            PendingToken = pendingToken,
            RequirePasswordReset = false,
            ValidUntil = null
        }, Guid.Empty);
    }

    #endregion

    #region Record login log on success or failure

    private Task RecordLoginLogAsync(Guid? userId, string loginName, string ip, bool success, Guid? sessionId,
        string? reason, CancellationToken ct) =>
        _executor.ExecuteAsync(() =>
        {
            _executor.AddApplicationEvent(
                new LoginLogEvent(userId, loginName, ip, success, sessionId, reason, DateTimeOffset.UtcNow));
        }, ct);

    #endregion

    #region If the last validation is passed then

    private async Task<(string accessToken, Guid refreshToken, bool requirePasswordChange)> FinalizeLoginOnSuccess(
        Guid userId, string ip, CancellationToken ct)
    {
        var (cache, requirePasswordChange) = await PrepareUserSessionAsync(userId, ct);

        var refreshToken = Guid.NewGuid();
        var jwt = _jwtProvider.GenerateJwt(cache.CurrentSession!.Value, cache.Id, cache.Code, cache.Name);

        await RecordLoginLogAsync(cache.Id, cache.LoginName, ip, true, cache.CurrentSession, null, ct);

        await PrepareRedisLoginSessionAsync(cache, refreshToken, ct);

        return (jwt, refreshToken, requirePasswordChange);
    }

    private async Task PrepareRedisLoginSessionAsync(UserCacheDto cache, Guid refreshToken, CancellationToken ct)
    {
        try
        {
            await using var locks = await _applicationLockProvider.CreateLock().AddS<UserAgg>(cache.Id, LoginScope)
                .LockAsync(5, ct: ct); // Protection of Concurrency which may not possibly happen
            await _refreshTokenRedisStore.SetTokenForUserIdAsync(cache.Id, refreshToken,
                TimeSpan.FromMinutes(_securityOptions.RefreshTokenValidMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write refresh token into redis For User: {userId}", cache.Id);
            throw;
        }

        await Task.WhenAll(
            _userCacheProvider.SetAsync(cache),
            _loginThrottleRedisStore.ResetFailureCountByLoginNameAsync(cache.LoginName)
        );
    }

    private Task<(UserCacheDto Cache, bool RequirePasswordChange)> PrepareUserSessionAsync(Guid userId,
        CancellationToken ct)
        => _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetEnabledByIdForUpdateAsync(userId, ct)
                       ?? throw new IdentityErrUserLocked();
            user.CreateNewSession();
            return (_mapper.Map<UserCacheDto>(user), user.RequiresPasswordChange);
        }, ct);

    #endregion

    #region If any validation process is not passed then

    private async Task ProcessLoginFailureAsync(string loginName, string ip, string reason, CancellationToken ct)
    {
        long count = await _loginThrottleRedisStore.IncrementFailureCountByLoginNameAsync(loginName);
        await RecordLoginLogAsync(null, loginName, ip, false, null, reason, ct);

        var decision = _throttleDecisionFactory.Evaluate(count);
        if (decision.Kind == LoginThrottleKind.PermanentLock)
        {
            await _executor.ExecuteAsync(async () =>
            {
                var user = await _userRepository.GetEnabledByLoginNameForUpdateAsync(loginName, ct);
                if (user is null)
                {
                    return;
                }

                user.Lock(new OperatorRecord(SystemUser.Id, SystemUser.Code, SystemUser.Name));
            }, ct);
            _logger.LogWarning(
                "Account {LoginName} permanently locked after {Count} failed attempts from IP {Ip}",
                loginName, count, ip);
            throw new IdentityErrUserLocked();
        }
    }

    #endregion

    #region Refresh AccessToken AND Rotate RefreshToken

    public async Task<(string accessToken, Guid refreshToken)> RefreshJwtAsync(Guid refreshToken,
        CancellationToken ct = default)
    {
        var currentUserId = await _refreshTokenRedisStore.ValidateTokenAsync(refreshToken)
                            ?? throw new IdentityErrRefreshTokenExpired();

        UserCacheDto cachedUser = await _userCacheProvider.GetOrFallbackDbAsync(currentUserId, ct);

        if (cachedUser.CurrentSession is null)
        {
            throw new IdentityErrSessionInvalid(cachedUser.Id, cachedUser.CurrentSession);
        }

        var newRt = Guid.NewGuid();

        try
        {
            await using var locks =
                await _applicationLockProvider.CreateLock().AddX<UserAgg>(cachedUser.Id, LoginScope)
                    .LockAsync(5, ct: ct);
            await _refreshTokenRedisStore.RotateTokenForUserIdAsync(
                currentUserId,
                refreshToken,
                newRt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh accessToken and refreshToken in redis For User: {userId}",
                cachedUser.Id);
            throw;
        }

        var newJwt = _jwtProvider.GenerateJwt(cachedUser.CurrentSession.Value, cachedUser.Id, cachedUser.Code,
            cachedUser.Name);

        return (newJwt, newRt);
    }

    #endregion
}