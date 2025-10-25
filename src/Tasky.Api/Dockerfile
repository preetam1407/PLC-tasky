# syntax=docker/dockerfile:1.6

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Tasky.sln ./
COPY src/Tasky.Api/Tasky.Api.csproj src/Tasky.Api/
COPY src/Tasky.Application/Tasky.Application.csproj src/Tasky.Application/
COPY src/Tasky.Domain/Tasky.Domain.csproj src/Tasky.Domain/
COPY src/Tasky.Infrastructure/Tasky.Infrastructure.csproj src/Tasky.Infrastructure/

RUN dotnet restore src/Tasky.Api/Tasky.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Tasky.Api/Tasky.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tasky.Api.dll"]
