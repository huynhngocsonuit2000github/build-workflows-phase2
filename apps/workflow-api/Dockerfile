FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/WorkflowApi.Domain/WorkflowApi.Domain.csproj src/WorkflowApi.Domain/
COPY src/WorkflowApi.Api/WorkflowApi.Api.csproj src/WorkflowApi.Api/
RUN dotnet restore src/WorkflowApi.Api/WorkflowApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/WorkflowApi.Api/WorkflowApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
USER $APP_UID
COPY --from=build --chown=$APP_UID:$APP_UID /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorkflowApi.Api.dll"]
