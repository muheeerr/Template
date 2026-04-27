# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["Template.Api/Template.Api.csproj", "Template.Api/"]
COPY ["Template.Modules/Template.Modules.csproj", "Template.Modules/"]
COPY ["Template.Integration/Template.Integration.csproj", "Template.Integration/"]
COPY ["Template.Infrastructure/Template.Infrastructure.csproj", "Template.Infrastructure/"]
RUN dotnet restore "./Template.Api/Template.Api.csproj"
COPY . .
WORKDIR "/src/Template.Api"
RUN dotnet build "./Template.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Template.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Template.Api.dll"]
