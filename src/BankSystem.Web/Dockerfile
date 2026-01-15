# =========================
# 1. Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src

# Copy solution and csproj files for better restore caching
COPY BankSystem.sln ./

# Common projects
COPY Common/BankSystem.Common/BankSystem.Common.csproj Common/BankSystem.Common/

# Data layer
COPY Data/BankSystem/BankSystem.Data/BankSystem.Data.csproj Data/BankSystem/BankSystem.Data/
COPY Data/BankSystem/BankSystem.Models/BankSystem.Models.csproj Data/BankSystem/BankSystem.Models/

# Services layer
COPY Services/BankSystem/BankSystem.Services/BankSystem.Services.csproj Services/BankSystem/BankSystem.Services/
COPY Services/BankSystem/BankSystem.Services.Models/BankSystem.Services.Models.csproj Services/BankSystem/BankSystem.Services.Models/

# Web project
COPY Web/BankSystem.Web/BankSystem.Web.csproj Web/BankSystem.Web/

# Restore dependencies
RUN dotnet restore Web/BankSystem.Web/BankSystem.Web.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/Web/BankSystem.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# =========================
# 2. Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS runtime

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure ASP.NET Core to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000 \
 ASPNETCORE_ENVIRONMENT=Production \
 DOTNET_RUNNING_IN_CONTAINER=true \
 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose HTTP port
EXPOSE 5000

# Health check (adjust path if you have a dedicated health endpoint)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
 CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "BankSystem.Web.dll"]
