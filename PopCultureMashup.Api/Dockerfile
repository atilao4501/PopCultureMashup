# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Kestrel escuta em 8080 por padrão nas imagens .NET 8; garantimos explicitamente
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copia arquivos de projeto primeiro para cache de restore
COPY ["PopCultureMashup.sln", "/src/"]
COPY ["PopCultureMashup.Api/PopCultureMashup.Api.csproj", "PopCultureMashup.Api/"]
COPY ["PopCultureMashup.Application/PopCultureMashup.Application.csproj", "PopCultureMashup.Application/"]
COPY ["PopCultureMashup.Domain/PopCultureMashup.Domain.csproj", "PopCultureMashup.Domain/"]
COPY ["PopCultureMashup.Infrastructure/PopCultureMashup.Infrastructure.csproj", "PopCultureMashup.Infrastructure/"]
RUN dotnet restore "PopCultureMashup.Api/PopCultureMashup.Api.csproj"

# Copia o restante do código
COPY . .
WORKDIR "/src/PopCultureMashup.Api"
RUN dotnet build "./PopCultureMashup.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PopCultureMashup.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PopCultureMashup.Api.dll"]
