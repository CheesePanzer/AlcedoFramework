FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Samples/Alcedo.Framework.Sample/front/Alcedo.Frontend.Sample.csproj", "Samples/Alcedo.Framework.Sample/front/"]
COPY ["Src/Framework/Frontend/Alcedo.Frontend.Admin/Alcedo.Frontend.Admin.csproj", "Src/Framework/Frontend/Alcedo.Frontend.Admin/"]
COPY ["Src/Core/Frontend/Alcedo.Frontend.Abstraction/Alcedo.Frontend.Abstraction.csproj", "Src/Core/Frontend/Alcedo.Frontend.Abstraction/"]
COPY ["Src/Core/Frontend/Alcedo.Frontend.Blazor/Alcedo.Frontend.Blazor.csproj", "Src/Core/Frontend/Alcedo.Frontend.Blazor/"]
COPY ["Src/Core/Frontend/Alcedo.Frontend.Core/Alcedo.Frontend.Core.csproj", "Src/Core/Frontend/Alcedo.Frontend.Core/"]
COPY ["Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/Alcedo.Frontend.FormulaEngine.csproj", "Src/Framework/Frontend/Alcedo.Frontend.FormulaEngine/"]
COPY ["Src/Core/Frontend/Alcedo.Frontend.Host/Alcedo.Frontend.Host.csproj", "Src/Core/Frontend/Alcedo.Frontend.Host/"]
COPY ["Src/Framework/Frontend/Alcedo.Frontend.Identity/Alcedo.Frontend.Identity.csproj", "Src/Framework/Frontend/Alcedo.Frontend.Identity/"]
COPY ["Src/Framework/Frontend/Alcedo.Frontend.Notification/Alcedo.Frontend.Notification.csproj", "Src/Framework/Frontend/Alcedo.Frontend.Notification/"]
RUN dotnet restore "Samples/Alcedo.Framework.Sample/front/Alcedo.Frontend.Sample.csproj"
COPY . .
WORKDIR "/src/Samples/Alcedo.Framework.Sample/front"
RUN dotnet build "./Alcedo.Frontend.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Alcedo.Frontend.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM nginx:1.29-alpine AS final
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
