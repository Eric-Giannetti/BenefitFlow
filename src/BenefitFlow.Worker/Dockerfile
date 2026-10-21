FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/ ./src/
RUN dotnet restore src/BenefitFlow.Worker/BenefitFlow.Worker.csproj
RUN dotnet publish src/BenefitFlow.Worker/BenefitFlow.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "BenefitFlow.Worker.dll"]
