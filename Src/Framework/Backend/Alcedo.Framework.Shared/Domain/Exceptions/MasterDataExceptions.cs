using Alcedo.Core.Abstractions;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.Exceptions;

public class MdErrRequiredStatusResourceNotFound<TEntity> : AlcedoNotFoundException
    where TEntity : MasterDataAggregateRootBase
{
    private const string Friendly = "The requested resource under valid status was not found.";

    public MdErrRequiredStatusResourceNotFound(string name, IReadOnlyCollection<MasterDataStateValue> states)
        : base($"{typeof(TEntity).Name} with {name} under {string.Join(",",states.Select(s => s.Value))} was not found.", Friendly)
    {
    }

    public MdErrRequiredStatusResourceNotFound(Guid id, IReadOnlyCollection<MasterDataStateValue> states)
        : base($"{typeof(TEntity).Name} with {id} under  {string.Join(",",states.Select(s => s.Value))}  was not found.", Friendly)
    {
    }

    public MdErrRequiredStatusResourceNotFound(IEnumerable<string> names, IReadOnlyCollection<MasterDataStateValue> states)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", names)}) under  {string.Join(",",states.Select(s => s.Value))}  was not found", Friendly)
    {
    }

    public MdErrRequiredStatusResourceNotFound(IEnumerable<Guid> ids, IReadOnlyCollection<MasterDataStateValue> states)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", ids)}) under  {string.Join(",",states.Select(s => s.Value))}  was not found.", Friendly)
    {
    }
}

public class MdErrResourceNotFoundOrNotEnabled<TEntity> : AlcedoNotFoundException
    where TEntity : MasterDataAggregateRootBase
{
    private const string Friendly = "The requested resource was not found or is not enabled.";

    public MdErrResourceNotFoundOrNotEnabled(string name)
        : base($"{typeof(TEntity).Name} with {name} was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(Guid id)
        : base($"{typeof(TEntity).Name} with {id} was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(IEnumerable<string> names)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", names)}) was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(IEnumerable<Guid> ids)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", ids)}) was not found or is not enabled.", Friendly)
    {
    }
}

public class MdErrResourceNotFoundOrNotEnabled : AlcedoNotFoundException
{
    private const string Friendly = "The requested resource was not found or is not enabled.";

    public MdErrResourceNotFoundOrNotEnabled(Type aggType,string name)
        : base($"{aggType.Name} with {name} was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(Type aggType,Guid id)
        : base($"{aggType.Name} with {id} was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(Type aggType,IEnumerable<string> names)
        : base($"{aggType.Name} with ({string.Join(",", names)}) was not found or is not enabled.", Friendly)
    {
    }

    public MdErrResourceNotFoundOrNotEnabled(Type aggType,IEnumerable<Guid> ids)
        : base($"{aggType.Name} with ({string.Join(",", ids)}) was not found or is not enabled.", Friendly)
    {
    }
}