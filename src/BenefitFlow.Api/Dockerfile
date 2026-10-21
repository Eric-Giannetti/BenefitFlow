FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/ ./src/
RUN dotnet restore src/BenefitFlow.Api/BenefitFlow.Api.csproj
RUN dotnet publish src/BenefitFlow.Api/BenefitFlow.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BenefitFlow.Api.dll"]
