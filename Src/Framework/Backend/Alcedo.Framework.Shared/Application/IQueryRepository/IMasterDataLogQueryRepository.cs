using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.IQueryRepository;

public interface IMasterDataAuditLogQueryRepository<TMasterData> : IEntityQueryRepository<MasterDataAuditLogEntity,
    MasterDataAuditLogDto, MasterDataLogSearchQuery>
    where TMasterData : MasterDataAggregateRootBase;