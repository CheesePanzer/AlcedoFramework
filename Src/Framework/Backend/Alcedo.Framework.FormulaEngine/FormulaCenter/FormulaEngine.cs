using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.FormulaEngine.Contract;
using Alcedo.Framework.FormulaEngine.Contract.Abstraction;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.Domain.IRepository;
using Alcedo.Framework.FormulaEngine.EngineCore;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.FormulaEngine.FormulaCenter;

public sealed class FormulaEngine : IFormulaEngine
{
    private readonly IFormulaRepository _repository;
    
    private readonly IHistoryFormulaStore _store;
    
    private readonly IMapper _mapper;
    
    private readonly ILoggerFactory _loggerFactory;

    public FormulaEngine(IFormulaRepository repository, IHistoryFormulaStore store, IMapper mapper, ILoggerFactory loggerFactory)
    {
        _repository = repository;
        _store = store;
        _mapper = mapper;
        _loggerFactory = loggerFactory;
    }

    public async Task<EngineResult<TOutput>> ExecuteAsync<TFormula, TInput, TOutput>(TInput input, CancellationToken ct = default) where TFormula : Formula<TInput, TOutput> where TInput : class where TOutput : class, new()
    {
        var formulaMetadata = FormulaStoreCenter.GetByType<TFormula>() ?? throw new FormulaNotFoundException($"Formula type {typeof(TFormula).Name} not found");
        
        var formula = await _repository.GetByCodeAsync(formulaMetadata.Code) ?? throw new DomErrResourceNotFound<FormulaAggRoot>(formulaMetadata.Code);
        
        var inputParameters = _mapper.Map<IReadOnlyCollection<InputParameter>>(formula.InputParameters);
        
        var expressionParameters = _mapper.Map<IReadOnlyCollection<ExpressionParameter>>(formula.Expressions);
        
        var outputParameters = _mapper.Map<IReadOnlyCollection<OutputParameter>>(formula.OutputParameters);
        
        var core = new EngineCore<TInput, TOutput>(
            input, 
            formulaMetadata.Code, 
            formula.Version, 
            inputParameters, 
            expressionParameters, 
            outputParameters, 
            _loggerFactory.CreateLogger<EngineCore<TInput, TOutput>>()
            );
        
        return await core.ParseAsync(ct);
    }

    public async Task<EngineResult<TOutput>> ExecuteAsync<TFormula, TInput, TOutput>(TInput input, int version, CancellationToken ct) where TFormula : Formula<TInput, TOutput> where TInput : class where TOutput : class, new()
    {
        var formulaMetadata = FormulaStoreCenter.GetByType<TFormula>() ?? throw new FormulaNotFoundException($"Formula type {typeof(TFormula).Name} not found");

        IReadOnlyCollection<InputParameter> inputParameters;

        IReadOnlyCollection<ExpressionParameter> expressionParameters;

        IReadOnlyCollection<OutputParameter> outputParameters;

        var formula = await _repository.GetByCodeAndVersionAsync(formulaMetadata.Code, version);

        if (formula is null)
        {
           var formulaH = await _store.GetByCodeAndVersion(formulaMetadata.Code, version) ?? throw new DomErrResourceNotFound<FormulaAggRoot>(formulaMetadata.Code);
           
           inputParameters = _mapper.Map<IReadOnlyCollection<InputParameter>>(formulaH.InputParameters);
        
           expressionParameters = _mapper.Map<IReadOnlyCollection<ExpressionParameter>>(formulaH.Expressions);
        
           outputParameters = _mapper.Map<IReadOnlyCollection<OutputParameter>>(formulaH.OutputParameters);
        }
        else
        {
            inputParameters = _mapper.Map<IReadOnlyCollection<InputParameter>>(formula.InputParameters);
        
            expressionParameters = _mapper.Map<IReadOnlyCollection<ExpressionParameter>>(formula.Expressions);
        
            outputParameters = _mapper.Map<IReadOnlyCollection<OutputParameter>>(formula.OutputParameters);
        }
        
        var core = new EngineCore<TInput, TOutput>(
            input, 
            formulaMetadata.Code, 
            version, 
            inputParameters, 
            expressionParameters, 
            outputParameters, 
            _loggerFactory.CreateLogger<EngineCore<TInput, TOutput>>());
        
        return await core.ParseAsync(ct);
    }
}