using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.FormulaEngine.BackendInterfaces;

public sealed record FormulaSearchQuery : ReferenceDataSearchQueryBase
{
    
}

[AutoTableLink(nameof(Code), "/formula-engine/formulas/{Id}")]
public sealed record FormulaDto : ReferenceDataDtoBase
{
    public string Description { get; set; } = null!;

    public List<FormulaInputDto> InputParameters { get; set; } = new List<FormulaInputDto>();

    public List<FormulaExpressionDto> Expressions { get; set; } = new List<FormulaExpressionDto>();

    public List<FormulaOutputDto> OutputParameters { get; set; } = new List<FormulaOutputDto>();
}

public sealed record FormulaInputDto : EntityDtoBase
{
    public Guid FormulaId { get; set; }
    
    public string VarName { get; set; } = null!;

    public  string? VarDescription { get; set; }
    
    public  string VarType { get; set; } = null!;

    public  string? ValidCondition { get; set; }
    
    public  string? DefaultValue { get; set; }
}

public sealed record FormulaExpressionDto : EntityDtoBase
{
    public  Guid FormulaId { get; set; }
    
    public  int Order { get; set; }
    
    public  string VarName { get; set; } = null!;

    public  string Expression { get; set; } = null!;

    public  string? Note { get; set; }
}

public sealed record FormulaOutputDto : EntityDtoBase
{
    public  Guid FormulaId { get; set; }
    
    public  string VarName { get; set; } = null!;

    public  string? VarDescription { get; set; }
    
    public  string VarType { get; set; } = null!;

    public  string Expression { get; set; } = null!;
}

public sealed record FormulaExpressionUpdateCommand
{
    public required int FormulaVersion { get; set; }
    
    public required IReadOnlyCollection<FormulaExpressionUpdateCommandItem> Items { get; set; } = new List<FormulaExpressionUpdateCommandItem>();
}

public sealed record FormulaExpressionUpdateCommandItem
{
    public required int Order { get; set; }

    public required string VarName { get; set; } = null!;

    public required string Expression { get; set; } = null!;
    
    public required string? Note { get; set; }
}

public sealed record FormulaInputUpdateCommand()
{
    public required int FormulaVersion { get; set; }

    public required IReadOnlyList<FormulaInputUpdateCommandItem> Items { get; set; } = new List<FormulaInputUpdateCommandItem>();
}

public sealed record FormulaInputUpdateCommandItem()
{
    public required Guid InputParamId { get; set; }
    
    public required string? ValidCondition { get; set; }
    
    public required string? DefaultValue { get; set; }
}

public sealed record FormulaOutputUpdateCommand()
{
    public  int FormulaVersion { get; set; }

    public  IReadOnlyList<FormulaOutputUpdateCommandItem> Items { get; set; } = new List<FormulaOutputUpdateCommandItem>();
}


public sealed record FormulaOutputUpdateCommandItem()
{
    public Guid OutputParamId { get; set; }

    public string Expression { get; set; } = null!;
}

[ApiClient(Area = "formula-engine", Controller = "formulas", Version = 1)]
public interface IFormula : IReferenceController<FormulaDto, FormulaSearchQuery>
{
    [HttpPatch("{id}/inputparams")]
    Task UpdateInputParams([ToRoute] Guid id, [ToBody] FormulaInputUpdateCommand updateCommand,
        CancellationToken ct = default);
    
    [HttpPatch("{id}/outputparams")]
    Task UpdateOutputParams([ToRoute] Guid id, [ToBody] FormulaOutputUpdateCommand updateCommand,
        CancellationToken ct = default);
    
    [HttpPatch("{id}/expressions")]
    Task UpdateExpressions([ToRoute] Guid id, [ToBody] FormulaExpressionUpdateCommand updateCommand,
        CancellationToken ct = default);
    
    [HttpPost("{id}/test")]
    Task<string> TestFormula([ToRoute] Guid id, [ToBody] string inputJson,
        CancellationToken ct = default);

    [HttpGet("{id}/test")]
    Task<string> GetTestInputStructure([ToRoute]Guid id, CancellationToken ct = default);
}