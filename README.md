# Alcedo Framework

Alcedo Framework is a modular .NET framework containing reusable backend
building blocks, Blazor frontend libraries, framework modules, tests,
documentation, and paired backend/frontend samples.

The repository targets .NET 10. Product-specific applications are intentionally
excluded.

## Repository Structure

```text
Src/
  Core/
    Backend/       Domain, application, infrastructure, presentation, and hosting
    Frontend/      Frontend abstractions, runtime, Blazor components, and hosting
  Framework/
    Backend/       Shared, Identity, Admin, Notification, and FormulaEngine modules
    Frontend/      Blazor modules corresponding to the backend Framework modules
Tests/
  Core/            Core tests grouped by backend/frontend
  Framework/       Framework tests grouped by backend/frontend
Docs/
  Core/            Core backend and frontend documentation
  Framework/       Framework module documentation
Samples/
  Alcedo.Framework.Sample/
    back/           ASP.NET Core integration sample
    front/          Blazor WebAssembly integration sample
```

## Documentation

- Core backend overview: [`Docs/Core/Backend/00-Alcedo.Core-Overview.md`](Docs/Core/Backend/00-Alcedo.Core-Overview.md)
- Core frontend overview: [`Docs/Core/Frontend/00-Alcedo.Frontend-Overview.md`](Docs/Core/Frontend/00-Alcedo.Frontend-Overview.md)
- Framework backend documentation: [`Docs/Framework/Backend`](Docs/Framework/Backend)

## Quick Start

Install the .NET 10 SDK, then restore, build, and test the repository:

```shell
dotnet restore AlcedoFramework.slnx
dotnet build AlcedoFramework.slnx --configuration Release --no-restore
dotnet test AlcedoFramework.slnx --configuration Release --no-build
```

To run the paired sample, first provide local PostgreSQL, Redis, JWT, encryption,
and storage settings through environment variables, .NET user secrets, or an
ignored `appsettings.Development.json`. The committed configuration contains
placeholders only.

Start the backend:

```shell
dotnet run --project Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj --urls http://localhost:5149
```

In another terminal, start the Blazor frontend:

```shell
dotnet run --project Samples/Alcedo.Framework.Sample/front/Alcedo.Frontend.Sample.csproj --urls http://localhost:5173
```

The frontend sample is configured to call `http://localhost:5149`.

## License

Licensed under the [MIT License](LICENSE).
