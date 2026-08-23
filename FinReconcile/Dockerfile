# Estágio 1: Build da aplicação .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura as dependências
COPY ["FinReconcile.csproj", "./"]
RUN dotnet restore "./FinReconcile.csproj"

# Copia todo o código fonte e compila
COPY . .
RUN dotnet publish "FinReconcile.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 2: Runtime enxuto para execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinReconcile.dll"]