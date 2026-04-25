# TenantCore.App

Wrapper API + Blazor WebAssembly frontend that integrates with TenantCore.Auth for user identity and token management.

## Architecture

Clean Architecture + CQRS via MediatR. Three backend layers + two Blazor projects.

```
Api ──► Application ──► Domain
Api ──► Infrastructure ──► Domain
Web.Client ──► Shared
```

### Projects

| Project | Purpose |
|---|---|
| `TenantCore.Api` | ASP.NET Core Web API — thin controllers, middleware, JWT validation |
| `TenantCore.Application` | CQRS — commands, queries, handlers, validators, translators |
| `TenantCore.Domain` | Entities, repository interfaces, domain exceptions |
| `TenantCore.Infrastructure` | EF Core, repositories, Auth HTTP client |
| `TenantCore.Shared` | DTOs, authorization constants, `Result<T>`, `PagedResult<T>` |
| `TenantCore.Web` | Blazor Server host |
| `TenantCore.Web.Client` | Blazor WASM — typed API clients, pages, components |

## Coding Patterns

### Controller (`TenantCore.Api/Controllers/`)

Thin. Only routing + `ISender`. Zero business logic. Primary constructor injection.

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int page, int pageSize, string? search, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantsQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantByIdQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto, CancellationToken ct)
    {
        var result = await sender.Send(new CreateTenantCommand(dto.Name, dto.Subdomain), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantDto dto, CancellationToken ct)
        => Ok(await sender.Send(new UpdateTenantCommand(id, dto.Name), ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteTenantCommand(id), ct);
        return NoContent();
    }
}
```

### Command / Query (`TenantCore.Application/Features/{Area}/Commands|Queries/`)

`sealed record` implementing `IRequest<T>`. For delete/void: `IRequest` (no type param).

```csharp
// Commands
public sealed record CreateTenantCommand(string Name, string Subdomain) : IRequest<TenantDto>;
public sealed record UpdateTenantCommand(Guid Id, string Name) : IRequest<TenantDto>;
public sealed record DeleteTenantCommand(Guid Id) : IRequest;

// Queries
public sealed record GetTenantsQuery(int Page, int PageSize, string? Search) : IRequest<PagedResult<TenantDto>>;
public sealed record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;
```

### Handler (`TenantCore.Application/Features/{Area}/Handlers/`)

`sealed class`. Primary constructor. One handler per command/query file.

```csharp
public sealed class CreateTenantHandler(
    ITenantRepository repository,
    ILogger<CreateTenantHandler> logger)
    : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating tenant {TenantName}", request.Name);

        var existing = await repository.GetBySubdomainAsync(request.Subdomain, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Tenant with subdomain '{request.Subdomain}' already exists.");

        var tenant = Tenant.Create(request.Name, request.Subdomain);
        await repository.AddAsync(tenant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return TenantTranslator.ToDto(tenant);
    }
}
```

### Validator (`TenantCore.Application/Features/{Area}/Validators/`)

FluentValidation. Auto-registered by MediatR `ValidationBehavior` pipeline.

```csharp
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .MaximumLength(63)
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain must be lowercase alphanumeric with hyphens.");
    }
}
```

### Translator (`TenantCore.Application/Features/{Area}/Translators/`)

`static class` with `static` methods. Never use AutoMapper.

```csharp
public static class TenantTranslator
{
    public static TenantDto ToDto(Tenant entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Subdomain = entity.Subdomain,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static IEnumerable<TenantDto> ToDtoList(IEnumerable<Tenant> entities)
        => entities.Select(ToDto);
}
```

### Domain Entity (`TenantCore.Domain/Entities/`)

Static factory. Private constructor for EF Core. Methods encapsulate state changes.

```csharp
public class Tenant : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Subdomain { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string subdomain) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Subdomain = subdomain,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
```

Base classes: `BaseEntity` (Id, CreatedAt, UpdatedAt?, RowVersion?), `AuditableEntity : BaseEntity` (+ CreatedBy, UpdatedBy).

### Repository Interface (`TenantCore.Domain/Interfaces/`)

```csharp
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<(IEnumerable<Tenant> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    void Update(Tenant tenant);
    void Remove(Tenant tenant);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### Repository Implementation (`TenantCore.Infrastructure/Repositories/`)

```csharp
public class TenantRepository(AppDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IEnumerable<Tenant> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = context.Tenants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search) || t.Subdomain.Contains(search));
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => await context.Tenants.AddAsync(tenant, ct);

    public void Update(Tenant tenant) => context.Tenants.Update(tenant);
    public void Remove(Tenant tenant) => context.Tenants.Remove(tenant);
    public async Task SaveChangesAsync(CancellationToken ct = default) => await context.SaveChangesAsync(ct);
}
```

### EF Entity Configuration (`TenantCore.Infrastructure/Persistence/Configurations/`)

```csharp
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Subdomain).IsRequired().HasMaxLength(63);
        builder.HasIndex(t => t.Subdomain).IsUnique();
        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}
```

### DTOs (`TenantCore.Shared/Dtos/`)

```csharp
// Read DTO — class with init setters
public class TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Write DTOs — sealed records
public sealed record CreateTenantDto(string Name, string Subdomain);
public sealed record UpdateTenantDto(string Name);
```

## File Layout for a New Feature (`Foo`)

```
src/
  TenantCore.Api/Controllers/
    FoosController.cs

  TenantCore.Application/Features/Foos/
    Commands/
      CreateFooCommand.cs
      UpdateFooCommand.cs
      DeleteFooCommand.cs
    Queries/
      GetFoosQuery.cs
      GetFooByIdQuery.cs
    Handlers/
      CreateFooHandler.cs
      UpdateFooHandler.cs
      DeleteFooHandler.cs
      GetFoosHandler.cs
      GetFooByIdHandler.cs
    Validators/
      CreateFooCommandValidator.cs
      UpdateFooCommandValidator.cs
    Translators/
      FooTranslator.cs

  TenantCore.Domain/
    Entities/Foo.cs
    Interfaces/IFooRepository.cs

  TenantCore.Infrastructure/
    Persistence/Configurations/FooConfiguration.cs
    Repositories/FooRepository.cs

  TenantCore.Shared/Dtos/
    FooDto.cs
    CreateFooDto.cs
    UpdateFooDto.cs
```

## Error Handling

Throw domain exceptions — middleware converts to ProblemDetails automatically.

| Exception | HTTP Status |
|---|---|
| `ValidationException` (FluentValidation via pipeline) | 400 |
| `DomainValidationException` | 400 |
| `NotFoundException` | 404 |
| `InvalidOperationException` | 409 |
| `UnauthorizedAccessException` | 401 |

Never return error responses manually from handlers or controllers.

## Authorization

```csharp
[Authorize(Policy = AuthPolicies.RequireAdmin)]
[Authorize(Policy = AuthPolicies.RequireManagement)]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
```

Roles: `SystemAdmin`, `ClinicAdmin`, `SchoolAdmin`, `Manager`, `Staff`, `User`, `Viewer`

Constants: `TenantCore.Shared.Authorization.AuthorizationConstants`

## Dependency Injection

- Application DI: `src/TenantCore.Application/DependencyInjection.cs`
- Infrastructure DI: `src/TenantCore.Infrastructure/DependencyInjection.cs`
- New repositories → Infrastructure DI
- Validators + handlers → auto-registered via MediatR assembly scanning (no manual registration needed)

## Key Namespaces

```
TenantCore.Api.Controllers
TenantCore.Application.Features.{Area}.Commands
TenantCore.Application.Features.{Area}.Queries
TenantCore.Application.Features.{Area}.Handlers
TenantCore.Application.Features.{Area}.Validators
TenantCore.Application.Features.{Area}.Translators
TenantCore.Domain.Entities
TenantCore.Domain.Interfaces
TenantCore.Infrastructure.Persistence.Configurations
TenantCore.Infrastructure.Repositories
TenantCore.Shared.Dtos
```

## Auth API Integration

`TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs` calls TenantCore.Auth.
Named HttpClient: `"AuthApi"`. Base URL: `AuthApi:BaseUrl` in appsettings.

## Database Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

## Rules (Never Violate)

- Controllers only call `sender.Send(...)` — no direct service injection
- Commands and queries are `sealed record`
- Handlers are `sealed class`
- Translators are `static class` with `static` methods
- Never use AutoMapper — use Translator pattern
- Throw exceptions for errors; never return error DTOs from handlers
- Validators are auto-wired — do not manually register them
- One handler per file, named to match its command/query
