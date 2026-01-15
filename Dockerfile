FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src

# Copy solution and all project files
COPY src/*.sln ./
COPY src/BankSystem.Models/*.csproj BankSystem.Models/
COPY src/BankSystem.Data/*.csproj BankSystem.Data/
COPY src/BankSystem.Services.Models/*.csproj BankSystem.Services.Models/
COPY src/BankSystem.Services/*.csproj BankSystem.Services/
COPY src/BankSystem.Web/*.csproj BankSystem.Web/

# Restore dependencies
RUN dotnet restore BankSystem.Web/BankSystem.Web.csproj

# Copy everything else
COPY src/ .

# Build and publish
WORKDIR /src/BankSystem.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build /app/publish .

# Create non-root user
RUN groupadd -g 1000 appuser && \
 useradd -u 1000 -g appuser -s /bin/bash appuser && \
 chown -R appuser:appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
 CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "BankSystem.Web.dll"]
