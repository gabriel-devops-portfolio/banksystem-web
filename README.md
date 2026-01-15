# BankSystem.Web

Main banking web application microservice.

## Architecture
- ASP.NET Core 3.1 MVC
- Entity Framework Core with SQL Server
- AWS RDS with IAM authentication

## Getting Started

### Prerequisites
- .NET Core 3.1 SDK
- Docker
- AWS CLI configured

### Local Development

```bash
# Restore dependencies
dotnet restore src/BankSystem.sln

# Run locally
cd src/BankSystem.Web
dotnet run

# Run tests
dotnet test tests/BankSystem.Services.Tests/
```

### Docker Build

```bash
cd src
docker build -t banksystem-web:latest .
docker run -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="..." \
  banksystem-web:latest
```

### Deploy to EKS

```bash
# Update image in deployment
kubectl apply -f kubernetes/

# Check status
kubectl get pods -n banksystem
kubectl logs -f deployment/banksystem-web -n banksystem
```

## Configuration

Environment variables:
- `RdsAuthentication__UseIamAuthentication` - Enable IAM auth (true/false)
- `RdsAuthentication__RdsEndpoint` - RDS endpoint
- `RdsAuthentication__DbUser` - Database username
- `RdsAuthentication__AwsRegion` - AWS region
- `RdsAuthentication__FallbackPassword` - Password for fallback

## CI/CD

GitHub Actions automatically builds and deploys on push to `main`.

## License

MIT
