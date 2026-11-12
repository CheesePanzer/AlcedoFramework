using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.BackednInterface;

namespace Alcedo.Frontend.Identity.Clients.Perm;

[ApiClient(Area = "identity", Controller = "perms", Version = 1)]
public interface IPermClient : IPagedQueryController<PermDto, PermSearchQuery>
{
}