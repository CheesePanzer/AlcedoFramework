using Alcedo.Core.Presentation;
using Alcedo.Framework.FormulaEngine.Application.Command;
using Alcedo.Framework.FormulaEngine.Application.Dto;
using Alcedo.Framework.FormulaEngine.Application.SearchQuery;
using Alcedo.Framework.FormulaEngine.Application.Service;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.FormulaEngine.Controller;

[Area("formula-engine")]
[ApiVersion("1.0")]
[Resource("Formulas","Manage Formulas")]
public sealed class FormulasController : ReferenceDataApiControllerBase<FormulaDto, FormulaSearchQuery>
{
    private readonly IFormulaApplicationService _formulaService;

    public FormulasController(IFormulaApplicationService service) : base(service)
    {
        _formulaService = service;
    }

    [HttpPatch("{id}/inputparams")]
    [Operation("UpdateInputParams","Update the input parameters of a formula")]
    public async Task<IActionResult> UpdateInputParams([FromRoute]Guid id, [FromBody]FormulaInputUpdateCommand updateCommand,
        CancellationToken ct = default)
    {
        await _formulaService.UpdateInputParamsAsync(id, updateCommand, CurrentUser, ct);
        
        return Ok();
    }
    
    [HttpPatch("{id}/outputparams")]
    [Operation("UpdateOutputParams","Update the output parameters of a formula")]
    public async Task<IActionResult> UpdateOutputParams([FromRoute]Guid id, [FromBody]FormulaOutputUpdateCommand updateCommand,
        CancellationToken ct = default)
    {
        await _formulaService.UpdateOutputParamsAsync(id, updateCommand, CurrentUser, ct);
        
        return Ok();
    }
    
    [HttpPatch("{id}/expressions")]
    [Operation("UpdateExpressions","Create or Update the expressions of a formula")]
    public async Task<IActionResult> UpdateExpressions([FromRoute]Guid id, [FromBody]FormulaExpressionUpdateCommand updateCommand,
        CancellationToken ct = default)
    {
        await _formulaService.UpdateFormulaExpressionsAsync(id, updateCommand, CurrentUser, ct);
        
        return Ok();
    }
    
    [HttpGet("{id}/test")]
    [Operation("GetTestInputTemplate","Get A input demo template for formula")]
    public async Task<ActionResult<string>> GetTestInputStructure(Guid id, CancellationToken ct = default)
    {
        var template = await _formulaService.GetTestInputStructureAsync(id, ct);
        
        return Ok(template);
    }
    
    [HttpPost("{id}/test")]
    [Operation("TestFormula","Test A Formula")]
    public async Task<ActionResult<string>> TestFormula([FromRoute]Guid id, [FromBody]string inputJson,
        CancellationToken ct = default)
    {
        var result = await _formulaService.TestFormulaInSandboxAsync(id, inputJson, ct);
        
        return Ok(result);
    }
}