# syntax=docker/dockerfile:1.6

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Tasky.sln ./
COPY src/TaskyV2.Api/TaskyV2.Api.csproj src/TaskyV2.Api/
COPY src/TaskyV2.Application/TaskyV2.Application.csproj src/TaskyV2.Application/
COPY src/TaskyV2.Domain/TaskyV2.Domain.csproj src/TaskyV2.Domain/
COPY src/TaskyV2.Infrastructure/TaskyV2.Infrastructure.csproj src/TaskyV2.Infrastructure/

RUN dotnet restore src/TaskyV2.Api/TaskyV2.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/TaskyV2.Api/TaskyV2.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskyV2.Api.dll"]
