using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation.Mvc;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;
using Alcedo.JuiceStore.Backend.Application.Service;
using Alcedo.JuiceStore.Backend.Presentation.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.JuiceStore.Backend.Presentation.Mvc;

[AllowAnonymous]
[Route("juices")]
public sealed class JuiceCatalogController(IJuiceCatalogService catalogService) : AlcedoPageControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] JuiceCatalogQuery query,
        [FromQuery] PaginationQuery pagination, CancellationToken ct = default)
    {
        var result = await catalogService.GetPageAsync(query, pagination, ct);
        var listVm = JuiceCatalogListVm.From(result, Request.QueryString.Value ?? string.Empty,
            Url.Action(nameof(Index))!);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_JuiceList", listVm);
        }

        return View(new JuiceCatalogPageVm
        {
            Query = query,
            Content = listVm
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var juice = await catalogService.GetByIdAsync(id, ct);
        return juice is null ? NotFound() : View(juice);
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct = default)
    {
        var juice = await catalogService.GetByIdAsync(id, ct);
        return juice is null ? NotFound() : PartialView("_JuicePreview", juice);
    }
}
