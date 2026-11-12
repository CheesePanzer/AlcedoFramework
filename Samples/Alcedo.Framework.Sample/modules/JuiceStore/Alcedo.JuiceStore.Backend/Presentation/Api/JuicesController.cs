using Alcedo.Core.Presentation;
using Alcedo.Framework.Shared.Controller;
using Alcedo.JuiceStore.Backend.Application.Command;
using Alcedo.JuiceStore.Backend.Application.Dto;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;
using Alcedo.JuiceStore.Backend.Application.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.JuiceStore.Backend.Presentation.Api;

[Area("juicestore")]
[ApiVersion("1.0")]
[Resource("Juices", "Manage the juice catalog")]
public sealed class JuicesController : MasterDataControllerBase<JuiceDto, JuiceSearchQuery,
    JuiceCreateCommand, JuiceUpdateCommand>
{
    public JuicesController(IJuiceApplicationService service) : base(service)
    {
    }
}
