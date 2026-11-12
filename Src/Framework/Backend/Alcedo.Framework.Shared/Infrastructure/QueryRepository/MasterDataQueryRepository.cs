using System.Linq.Expressions;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Infrastructure.QueryRepository;

public class MasterDataQueryRepositoryBaseBase<TEntity, TDto, TQuery>  : EntityQueryRepositoryBase<TEntity, TDto, TQuery> , IMasterDataQueryRepositoryBase<TEntity, TDto, TQuery> 
    where TEntity : MasterDataAggregateRootBase
    where TDto : MasterDataDtoBase
    where TQuery : MasterDataSearchQueryBase
{
    public MasterDataQueryRepositoryBaseBase(AlcedoDbContext context) : base(context)
    {
    }
/*
    protected override Expression<Func<TEntity, bool>>[] BuildCustomPredicates(TQuery query)
    {
        var list = new List<Expression<Func<TEntity, bool>>>();
        if (!string.IsNullOrWhiteSpace(query.CreatorCode))
        {
            list.Add(e => e.Creator.Code.Contains(query.CreatorCode));
        }

        if (!string.IsNullOrWhiteSpace(query.CreatorName))
        {
            list.Add(e => e.Creator.Name.Contains(query.CreatorName));
        }

        if (!string.IsNullOrWhiteSpace(query.UpdaterCode))
        {
            list.Add(e => e.Updater.Code.Contains(query.UpdaterCode));
        }

        if (!string.IsNullOrWhiteSpace(query.UpdaterName))
        {
            list.Add(e => e.Updater.Name.Contains(query.UpdaterName));
        }

        return list.ToArray();
    }*/
}