using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Contracts;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;

namespace Alcedo.Framework.Identity.Application.Service.Perm;

public interface IPermApplicationService
{
    PagedResult<PermDto> GetPagedDtosAsync(PermSearchQuery query,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);
}

[RegisterAs(typeof(IPermApplicationService))]
internal sealed class PermApplicationService : IPermApplicationService
{
    private readonly EndpointResourceOperationCenter _service;

    private readonly IMapper _mapper;

    public PermApplicationService(EndpointResourceOperationCenter service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public PagedResult<PermDto> GetPagedDtosAsync(PermSearchQuery query,
        PaginationQuery pagiQuery,
        CancellationToken ct = default)
    {
        var result = _service.GetPagedEndpointMetadata(pagiQuery.PageNo, pagiQuery.PageSize, query.Area, query.Resource,
            query.Operation, query.ResourceDescription, query.OperationDescription,customPredicate: meta => 
        {
            var hasControllerSkip = meta.ResourceAttributes.Any(a => 
                a is AllowAnonymousAttribute or SkipPermissionCheckAttribute);
            
            var hasActionSkip = meta.OperationAttributes.Any(a => 
                a is AllowAnonymousAttribute or SkipPermissionCheckAttribute);
            
            return !hasControllerSkip && !hasActionSkip;
        });

        var dtos = _mapper.Map<PermDto[]>(result.Metas);

        return new PagedResult<PermDto>()
        {
            Items = dtos,
            PageNo = pagiQuery.PageNo,
            PageSize = pagiQuery.PageSize,
            TotalCount = result.Count
        };
    }
}