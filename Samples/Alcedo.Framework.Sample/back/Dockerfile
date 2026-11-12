FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj", "Samples/Alcedo.Framework.Sample/back/"]
COPY ["Src/Core/Backend/Alcedo.Core.Host/Alcedo.Core.Host.csproj", "Src/Core/Backend/Alcedo.Core.Host/"]
COPY ["Src/Core/Backend/Alcedo.Core.Abstractions/Alcedo.Core.Abstractions.csproj", "Src/Core/Backend/Alcedo.Core.Abstractions/"]
COPY ["Src/Core/Backend/Alcedo.Core.Infrastructure/Alcedo.Core.Infrastructure.csproj", "Src/Core/Backend/Alcedo.Core.Infrastructure/"]
COPY ["Src/Core/Backend/Alcedo.Core.Application/Alcedo.Core.Application.csproj", "Src/Core/Backend/Alcedo.Core.Application/"]
COPY ["Src/Core/Backend/Alcedo.Core.Domain/Alcedo.Core.Domain.csproj", "Src/Core/Backend/Alcedo.Core.Domain/"]
COPY ["Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/Alcedo.Core.Plugin.SmartDoc.csproj", "Src/Core/Backend/Alcedo.Core.Plugin.SmartDoc/"]
COPY ["Src/Core/Backend/Alcedo.Core.Presentation.Mvc/Alcedo.Core.Presentation.Mvc.csproj", "Src/Core/Backend/Alcedo.Core.Presentation.Mvc/"]
COPY ["Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI.csproj", "Src/Core/Backend/Alcedo.Core.Presentation.Mvc.Kit.DaisyUI/"]
COPY ["Src/Core/Backend/Alcedo.Core.Presentation/Alcedo.Core.Presentation.csproj", "Src/Core/Backend/Alcedo.Core.Presentation/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Admin/Alcedo.Framework.Admin.csproj", "Src/Framework/Backend/Alcedo.Framework.Admin/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Alcedo.Framework.FormulaEngine.csproj", "Src/Framework/Backend/Alcedo.Framework.FormulaEngine/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/Alcedo.Framework.FormulaEngine.Contract.csproj", "Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Shared/Alcedo.Framework.Shared.csproj", "Src/Framework/Backend/Alcedo.Framework.Shared/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/Alcedo.Framework.Identity.Contracts.csproj", "Src/Framework/Backend/Alcedo.Framework.Identity.Contracts/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Identity/Alcedo.Framework.Identity.csproj", "Src/Framework/Backend/Alcedo.Framework.Identity/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts.csproj", "Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts/"]
COPY ["Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Alcedo.Framework.Notification.csproj", "Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/"]
RUN dotnet restore "Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj"
COPY . .
WORKDIR "/src/Samples/Alcedo.Framework.Sample/back"
RUN dotnet build "./Alcedo.Framework.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Alcedo.Framework.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Alcedo.Framework.Sample.dll"]
