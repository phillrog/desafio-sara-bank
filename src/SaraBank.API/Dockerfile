# Estágio base para execução (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# O Cloud Run usa a porta 8080 por padrão
EXPOSE 5001

# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SaraBank.API/SaraBank.API.csproj", "SaraBank.API/"]
COPY ["src/SaraBank.Application/SaraBank.Application.csproj", "SaraBank.Application/"]
COPY ["src/SaraBank.Domain/SaraBank.Domain.csproj", "SaraBank.Domain/"]
COPY ["src/SaraBank.Infrastructure/SaraBank.Infrastructure.csproj", "SaraBank.Infrastructure/"]
COPY ["src/SaraBank.Worker/SaraBank.Worker.csproj", "SaraBank.Worker/"]

# Restaura as dependências apontando para o arquivo principal
RUN dotnet restore "SaraBank.API/SaraBank.API.csproj"

# Copia todo o conteúdo da pasta 'src' para o container
COPY src/ .

# Define o diretório de trabalho para a API e compila
WORKDIR "/src/SaraBank.API"
RUN dotnet build "SaraBank.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Estágio de Publicação
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SaraBank.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Estágio Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Comando de entrada
ENTRYPOINT ["dotnet", "SaraBank.API.dll"]