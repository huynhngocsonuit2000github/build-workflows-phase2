FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/TaskLite.Api/TaskLite.Api.csproj src/TaskLite.Api/
RUN dotnet restore src/TaskLite.Api/TaskLite.Api.csproj

COPY src/TaskLite.Api/ src/TaskLite.Api/
RUN dotnet publish src/TaskLite.Api/TaskLite.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /data

USER $APP_UID

COPY --from=build --chown=$APP_UID:$APP_UID /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskLite.Api.dll"]
