using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Application.Dto.Auth;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared.Domain.Entity;
using MapsterMapper;

namespace Alcedo.Framework.Identity.Application.Service.Auth;

public interface IAuthApplicationService
{
    Task<(AuthTokenPasswordPhaseResult result, Guid refreshToken)> LoginAsync(string loginName, string password,
        string ip, CancellationToken ct = default);

    Task LogoutAsync(Guid refreshToken);
    Task<(string accessToken, Guid refreshToken)> RefreshJwtAsync(Guid refreshToken, CancellationToken ct = default);
    Task RefreshUserCacheAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken ct = default);
    Task ChangePasswordAsync(UserChangePasswordCommand command, Guid userId, CancellationToken ct = default);

    Task<string> EnableTwoFactorAuthenticationAsync(Guid userId, CancellationToken ct = default);

    Task DisableTwoFactorAuthenticationAsync(Guid userId, CancellationToken ct = default);

    Task<(AuthTokenTotpPhaseResult result, Guid refreshToken)> VerifyTotpAsync(string pendingToken, string totpCode,
        string ip,
        CancellationToken ct = default);
}

[RegisterAs(typeof(IAuthApplicationService))]
internal sealed class AuthApplicationService : IAuthApplicationService
{
    private readonly IRefreshTokenRedisStore _refreshTokenRedisStore;

    private readonly IUnitOfWorkExecutor _executor;

    private readonly IUserRepository _userRepository;

    private readonly JwtProvider _jwtProvider;

    private readonly IMapper _mapper;

    private readonly IEntityCacheProvider<UserCacheDto> _userCacheProvider;

    private readonly IUserQueryRepository _userQueryRepository;

    private readonly LoginServiceComponent _loginServiceComponent;

    private readonly TotpProvider _totpProvider;

    public AuthApplicationService(IUserRepository userRepository,
        IUnitOfWorkExecutor executor, IMapper mapper,
        IRefreshTokenRedisStore refreshTokenRedisStore, JwtProvider jwtProvider,
        IEntityCacheProvider<UserCacheDto> userCacheProvider,
        IUserQueryRepository userQueryRepository,
        LoginServiceComponent loginServiceComponent, TotpProvider totpProvider)
    {
        _userRepository = userRepository;
        _executor = executor;
        _mapper = mapper;
        _refreshTokenRedisStore = refreshTokenRedisStore;
        _jwtProvider = jwtProvider;
        _userCacheProvider = userCacheProvider;
        _userQueryRepository = userQueryRepository;
        _loginServiceComponent = loginServiceComponent;
        _totpProvider = totpProvider;
    }


    public Task<(AuthTokenPasswordPhaseResult result, Guid refreshToken)> LoginAsync(string loginName, string password,
        string ip, CancellationToken ct = default)
        => _loginServiceComponent.LoginAsync(loginName, password, ip, ct);

    public Task<(AuthTokenTotpPhaseResult result, Guid refreshToken)> VerifyTotpAsync(string pendingToken,
        string totpCode, string ip, CancellationToken ct = default)
        => _loginServiceComponent.VerifyTotpAsync(pendingToken, totpCode, ip, ct);

    public Task LogoutAsync(Guid refreshToken)
        => _loginServiceComponent.LogoutAsync(refreshToken);

    public Task<(string accessToken, Guid refreshToken)> RefreshJwtAsync(Guid refreshToken,
        CancellationToken ct = default) => _loginServiceComponent.RefreshJwtAsync(refreshToken, ct);

    public Task RefreshUserCacheAsync(Guid userId, CancellationToken ct = default) =>
        _userCacheProvider.RefreshFromDbAsync(userId, ct: ct);

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userQueryRepository.GetByIdAsync(userId, ct)
                   ?? throw new DomErrResourceNotFound<UserAgg>(userId);

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task ChangePasswordAsync(UserChangePasswordCommand command, Guid userId,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId, ct)
                       ?? throw new DomErrResourceNotFound<UserAgg>(userId);

            if (!PasswordHasher.Verify(command.OldPassword, user.PasswordHash))
            {
                throw new IdentityErrPasswordIncorrect();
            }

            user.ChangePassword(PasswordHasher.Hash(command.NewPassword),
                new OperatorRecord(user.Id, user.Code, user.Name));
        }, ct);

        await _loginServiceComponent.ClearUserSessionAsync(userId);
    }

    public Task<string> EnableTwoFactorAuthenticationAsync(Guid userId, CancellationToken ct = default) =>
        _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId, ct)
                       ?? throw new DomErrResourceNotFound<UserAgg>(userId);
            var setup = _totpProvider.GenerateSetupInfo(user.LoginName);

            user.EnableTwoFactorAuthenticator(setup.Base32Secret,
                new OperatorRecord(user.Id, user.Code, user.Name));

            return setup.QrCodeUri;
        }, ct);

    public Task DisableTwoFactorAuthenticationAsync(Guid userId, CancellationToken ct = default) =>
        _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId, ct)
                       ?? throw new DomErrResourceNotFound<UserAgg>(userId);

            user.DisableTwoFactorAuthenticator(new OperatorRecord(user.Id, user.Code, user.Name));
        }, ct);
}
