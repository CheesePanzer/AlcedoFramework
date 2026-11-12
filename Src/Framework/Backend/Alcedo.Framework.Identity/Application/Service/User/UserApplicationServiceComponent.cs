using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Application.Service.User;

[RegisterSelf]
internal sealed class
    UserEditServiceComponent : MasterDataEditServiceComponent<UserAgg, UserCreateCommand, UserUpdateCommand>
{
    private readonly IUserRepository _repository;

    public UserEditServiceComponent(
        IUserRepository repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
        : base(repository,
            loggerFactory,
            entityRefRelationCheckManager, executor)
    {
        _repository = repository;
    }


    protected override async Task CheckBeforeCreateAsync(UserCreateCommand command, CancellationToken ct = default)
    {
        await base.CheckBeforeCreateAsync(command, ct);

        if (await _repository.CheckIfLoginNameExistsAsync(command.LoginName, ct))
        {
            throw new DomErrDuplicateEntry("LoginName");
        }
    }

    protected override UserAgg CreateAggregateInternal(UserCreateCommand command, OperatorRecord oper,
        CancellationToken ct = default)
    {
        var passwordHash = PasswordHasher.Hash(command.PlainPassword);
        return new UserAgg(command.Code, command.Name, command.LoginName, passwordHash,
            command.UserName, command.Email, command.Contact, oper);
    }

    protected override void UpdateAggregateInternal(UserAgg aggToBeUpdated, UserUpdateCommand command,
        OperatorRecord oper, CancellationToken ct = default)
    {
        aggToBeUpdated.Update(command.Name, command.UserName, command.Email, command.Contact, oper);
    }
}