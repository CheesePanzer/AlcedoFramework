using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.FormulaEngine.Application.Command;
using Alcedo.Framework.FormulaEngine.Application.Dto;
using Alcedo.Framework.FormulaEngine.Application.Helpers;
using Alcedo.Framework.FormulaEngine.Application.SearchQuery;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.EngineCore;
using Alcedo.Framework.FormulaEngine.FormulaCenter;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.ReferenceData;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using MapsterMapper;

namespace Alcedo.Framework.FormulaEngine.Application.Service;

public interface IFormulaApplicationService : IReferenceDataApplicationService<FormulaDto,FormulaSearchQuery>
{
    Task UpdateInputParamsAsync(Guid id, FormulaInputUpdateCommand updateCommand, ContextOper @operator, CancellationToken ct = default);

    Task UpdateOutputParamsAsync(Guid id, FormulaOutputUpdateCommand updateCommand, ContextOper @operator, CancellationToken ct = default);

    Task UpdateFormulaExpressionsAsync(Guid id, FormulaExpressionUpdateCommand command, ContextOper @operator, 
        CancellationToken ct = default);

    Task<string> TestFormulaInSandboxAsync(Guid id, string inputJson, CancellationToken ct = default);

    Task<string> GetTestInputStructureAsync(Guid id, CancellationToken ct = default);
}

[RegisterAs(typeof(IFormulaApplicationService))]
internal sealed class FormulaApplicationService : IFormulaApplicationService
{
    private readonly ReferenceDataSearchServiceComponent<FormulaAggRoot, FormulaDto,
        FormulaSearchQuery> _search;
    
    private readonly IUnitOfWorkExecutor _executor;

    private readonly IReferenceDataRepositoryBase<FormulaAggRoot> _repository;
    
    private readonly IMapper _mapper;

    public FormulaApplicationService(IReferenceDataRepositoryBase<FormulaAggRoot> repository, IUnitOfWorkExecutor executor, ReferenceDataSearchServiceComponent<FormulaAggRoot, FormulaDto, FormulaSearchQuery> search, IMapper mapper)
    {
        _repository = repository;
        _executor = executor;
        _search = search;
        _mapper = mapper;
    }

    public Task<PagedResult<FormulaDto>> GetPagedDtosAsync(FormulaSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<FormulaDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => _search.GetDtoByIdAsync(id, ct);

    public Task UpdateInputParamsAsync(Guid id, FormulaInputUpdateCommand updateCommand, ContextOper @operator, CancellationToken ct = default)
    => _executor.ExecuteAsync(async () =>
        {
            var formula = await _repository.GetByIdForUpdateAsync(id, ct) ??
                          throw new DomErrResourceNotFound<FormulaAggRoot>(id);

            if (formula.Version != updateCommand.FormulaVersion)
            {
                throw new DomErrAggVersionConflict<FormulaAggRoot>(formula.Version, updateCommand.FormulaVersion);
            }
            
            formula.UpdateInputParams(updateCommand.Items.Select(i => (i.InputParamId, i.ValidCondition, i.DefaultValue)).ToList(), OperatorRecord.From(@operator));
        }, ct);
    
    public Task UpdateOutputParamsAsync(Guid id, FormulaOutputUpdateCommand updateCommand, ContextOper @operator, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var formula = await _repository.GetByIdForUpdateAsync(id, ct) ??
                          throw new DomErrResourceNotFound<FormulaAggRoot>(id);

            if (formula.Version != updateCommand.FormulaVersion)
            {
                throw new DomErrAggVersionConflict<FormulaAggRoot>(formula.Version, updateCommand.FormulaVersion);
            }
            
            formula.UpdateOutputParams(updateCommand.Items.Select(i => (i.OutputParamId, i.Expression)).ToList(), OperatorRecord.From(@operator));
        }, ct);

    public Task UpdateFormulaExpressionsAsync(Guid id, FormulaExpressionUpdateCommand command, ContextOper @operator, 
        CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var formula = await _repository.GetByIdForUpdateAsync(id, ct) ??
                          throw new DomErrResourceNotFound<FormulaAggRoot>(id);
            
            if (formula.Version != command.FormulaVersion)
            {
                throw new DomErrAggVersionConflict<FormulaAggRoot>(formula.Version, command.FormulaVersion);
            }
            
            formula.CreateOrUpdateExpressions(command.Items.Select(i => (i.Order,i.VarName,i.Expression, i.Note)).ToList(), OperatorRecord.From(@operator));
        }, ct);

    public async Task<string> GetTestInputStructureAsync(Guid id, CancellationToken ct = default)
    {
        var formula = await _repository.GetByIdForUpdateAsync(id, ct) ??
                      throw new DomErrResourceNotFound<FormulaAggRoot>(id);
            
        var metadata = FormulaStoreCenter.GetByCode(formula.Code) ??
                       throw new FormulaNotFoundException($"Formula Of Code: {formula.Code} not found");

        return TestFormulaHelper.BuildInputTemplate(metadata.InputType);
    }

    public Task<string> TestFormulaInSandboxAsync(Guid id, string inputJson, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var formula = await _repository.GetByIdForUpdateAsync(id, ct) ??
                          throw new DomErrResourceNotFound<FormulaAggRoot>(id);
            
            var metadata = FormulaStoreCenter.GetByCode(formula.Code) ??
                           throw new FormulaNotFoundException($"Formula Of Code: {formula.Code} not found");
            
            var inputParameters = _mapper.Map<IReadOnlyCollection<InputParameter>>(formula.InputParameters);
        
            var expressionParameters = _mapper.Map<IReadOnlyCollection<ExpressionParameter>>(formula.Expressions);
        
            var outputParameters = _mapper.Map<IReadOnlyCollection<OutputParameter>>(formula.OutputParameters);
            
            return await EngineSandboxRunner.RunSandboxAsync(inputJson, metadata.InputType, metadata.OutputType,metadata.Code,formula.Version, inputParameters, expressionParameters, outputParameters, ct);
        }, ct);

}