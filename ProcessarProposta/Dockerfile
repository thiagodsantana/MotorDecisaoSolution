# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Porta esperada pelo Cloud Run
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Atenção: certifique-se de que o pacote Google.Cloud.Functions.Framework está instalado
ENTRYPOINT ["dotnet", "ProcessarProposta.dll"]
