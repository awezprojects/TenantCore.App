# TenantCore

A production-ready multi-tenant management platform built with **Clean Architecture**, **CQRS**, and **MudBlazor**.

---

## Architecture

```
TenantCore.sln
├── src/
│   ├── TenantCore.Domain          # Entities, exceptions, domain interfaces — zero infrastructure deps
│   ├── TenantCore.Application     # MediatR CQRS, FluentValidation, pipeline behaviors, translators
│   ├── TenantCore.Infrastructure  # EF Core, SQL Server, migrations, repository implementations
│   ├── TenantCore.Api             # ASP.NET Core Web API — controllers, middleware, JWT, Serilog, Swagger
│   ├── TenantCore.Web             # Blazor Web App (Server) — MudBlazor UI, typed API clients
│   └── TenantCore.Shared          # Cross-cutting contracts: DTOs, Result<T>, PagedResult<T>
└── tests/
    ├── TenantCore.Application.Tests
    └── TenantCore.Domain.Tests
```

### Dependency Rules
```
Web        → Shared
Api        → Application, Infrastructure, Shared
Application → Domain, Shared
Infrastructure → Application, Domain
Domain     → (none)
```

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (or SQL Server Express / LocalDB)

### 1. Configure the database connection

Edit `src/TenantCore.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TenantCoreDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### 2. Apply migrations

```bash
dotnet ef database update \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api \
  --context AppDbContext
```

### 3. Run the API

```bash
dotnet run --project src/TenantCore.Api
# Swagger UI: https://localhost:7001/swagger
# Health check: https://localhost:7001/health
```

### 4. Run the Blazor UI

```bash
dotnet run --project src/TenantCore.Web
# Opens at https://localhost:7002
```

### 5. Run tests

```bash
dotnet test
```

---

## How to Add a New Feature (Vertical Slice)

Follow the pattern used for `Tenants`. Replace `Foo` with your entity name.

### 1. Domain — `src/TenantCore.Domain/`
```
Entities/Foo.cs                    # Aggregate root, factory method, domain logic
Interfaces/IFooRepository.cs       # Repository contract
```

### 2. Shared — `src/TenantCore.Shared/Dtos/`
```
FooDto.cs          # Read DTO
CreateFooDto.cs    # Input DTO for creation
UpdateFooDto.cs    # Input DTO for update
```

### 3. Application — `src/TenantCore.Application/Features/Foos/`
```
Commands/
  CreateFooCommand.cs
  UpdateFooCommand.cs
  DeleteFooCommand.cs
Queries/
  GetFooByIdQuery.cs
  GetFoosQuery.cs
Handlers/
  CreateFooHandler.cs
  UpdateFooHandler.cs
  DeleteFooHandler.cs
  GetFooByIdHandler.cs
  GetFoosHandler.cs
Validators/
  CreateFooCommandValidator.cs
  UpdateFooCommandValidator.cs
Translators/
  FooTranslator.cs
```

### 4. Infrastructure — `src/TenantCore.Infrastructure/`
```
Persistence/Configurations/FooConfiguration.cs   # IEntityTypeConfiguration<Foo>
Repositories/FooRepository.cs                    # ITenantRepository implementation
```
Register in `DependencyInjection.cs`:
```csharp
services.AddScoped<IFooRepository, FooRepository>();
```
Create migration:
```bash
dotnet ef migrations add AddFoo \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

### 5. API — `src/TenantCore.Api/Controllers/`
```
FoosController.cs   # Thin controller: routing + status codes only, delegates to ISender
```

### 6. Web — `src/TenantCore.Web/Components/Pages/Foos/`
```
FooList.razor
FooCreate.razor
FooEdit.razor
```
Add a typed client method in `Clients/ITenantApiClient.cs` and `TenantApiClient.cs`, add nav link in `NavMenu.razor`.

---

## Key Conventions

| Convention | Rule |
|---|---|
| Commands/Queries | `*Command`, `*Query` — records implementing `IRequest<T>` |
| Handlers | `*Handler` — implementing `IRequestHandler<TRequest, TResponse>` |
| Validators | `*Validator` — `AbstractValidator<T>`, auto-registered from assembly |
| Repositories | `I*Repository` (Domain), `*Repository` (Infrastructure) |
| Translators | `*Translator` static class with `ToDto()` methods |
| DTOs | Live in `TenantCore.Shared`, never expose Domain entities outside Infrastructure |
| Errors | `ProblemDetails` format with `correlationId` extension on all API errors |
| Logging | `ILogger<T>` everywhere — structured via Serilog |

---

## JWT Authentication

The API validates Bearer tokens issued by an external auth service. Configure in `appsettings.json`:
```json
"Jwt": {
  "Secret": "your-secret",
  "Issuer": "YourAuth.Service",
  "Audience": "TenantCore.Api"
}
```
Pass `Authorization: Bearer <token>` header on all protected endpoints.
