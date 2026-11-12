# Alcedo Framework

Alcedo Framework contains reusable Core and Framework components extracted
from the Alcedo codebase. Both backend implementations and their reusable
Blazor frontend libraries are included; product-specific applications are not.

## Repository Layout

- `Src/Core/Backend`: backend abstractions, domain, application, infrastructure, presentation, and hosting components
- `Src/Core/Frontend`: shared frontend abstractions, runtime, Blazor UI, and hosting components
- `Src/Framework/Backend`: reusable backend modules such as identity, administration, notifications, and formula evaluation
- `Src/Framework/Frontend`: frontend modules corresponding to the backend Framework modules
- `Tests`: tests grouped by Core/Framework and Backend/Frontend
- `Docs`: documentation grouped by Core/Framework and Backend/Frontend
- `Samples/Alcedo.Framework.Sample/back`: backend integration sample
- `Samples/Alcedo.Framework.Sample/front`: matching Blazor WebAssembly sample

## MVC UI Kit

`Alcedo.Core.Presentation.Mvc` contains MVC and HTMX infrastructure plus the
UI-driver abstraction. `Alcedo.Core.Presentation.Mvc.Kit.DaisyUI` contains the
DaisyUI/Tailwind views, controls, assets, and concrete UI driver.

An MVC layout opts into each package explicitly:

```cshtml
@await Html.PartialAsync("_AlcedoMvcCoreHead")
@await Html.PartialAsync("_AlcedoMvcDaisyUIHead")
```

Use `_AlcedoMvcDaisyUICdnHead` instead of `_AlcedoMvcDaisyUIHead` to load
DaisyUI and Tailwind from their CDNs. Do not load both DaisyUI heads.

## Requirements

- .NET SDK 10

## Build And Test

```shell
dotnet restore AlcedoFramework.slnx
dotnet build AlcedoFramework.slnx --configuration Release --no-restore
dotnet test AlcedoFramework.slnx --configuration Release --no-build
```

The sample configuration contains placeholders only. Supply development
credentials through environment variables, .NET user secrets, or an ignored
local configuration file.

## Documentation

Start with `Docs/Core/Backend/00-Alcedo.Core-Overview.md` or
`Docs/Core/Frontend/00-Alcedo.Frontend-Overview.md`. Framework backend documents
are available under `Docs/Framework/Backend`.

## License

No license has been selected yet. Add a license before publishing this
repository for third-party use.
