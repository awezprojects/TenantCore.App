# TenantCore.App — Claude Instructions

Wrapper API + Blazor WebAssembly frontend that integrates with TenantCore.Auth for user identity and token management.

---

## ADR Documents (Architecture Decision Records)

**Always read the relevant ADR before planning or writing code.** All ADRs live in `docs/adr/` (not `.claude/docs/adr/`).

| ADR | Read When |
|-----|-----------|
| [ADR-000: Index & Quick Reference](docs/adr/ADR-000-index.md) | Every session — orientation, tech stack |
| [ADR-001: Solution Structure](docs/adr/ADR-001-solution-structure.md) | Adding a project, moving code between layers |
| [ADR-002: Domain Layer](docs/adr/ADR-002-domain-layer.md) | Adding/changing entities, repo interfaces, domain exceptions |
| [ADR-003: Application Layer — CQRS & MediatR](docs/adr/ADR-003-application-layer.md) | Adding features, commands, queries, handlers, validators |
| [ADR-004: Infrastructure Layer](docs/adr/ADR-004-infrastructure-layer.md) | Adding repositories, EF migrations, external services |
| [ADR-005: API Layer](docs/adr/ADR-005-api-layer.md) | Adding controllers, middleware, authorization policies |
| [ADR-006: Shared Layer](docs/adr/ADR-006-shared-layer.md) | Adding DTOs, enums, authorization constants |
| [ADR-007: Blazor WebAssembly Client](docs/adr/ADR-007-blazor-client.md) | Adding pages, components, typed HTTP clients — **always read the UI Theme System section before writing any page** |
| [ADR-008: Multi-Tenancy](docs/adr/ADR-008-multi-tenancy.md) | Any feature touching clinic/application context |
| [ADR-009: Unit Testing](docs/adr/ADR-009-unit-testing.md) | Writing or reviewing tests — **read before writing any test file; mandatory for every feature** |
| [ADR-010: Security & Code Quality](docs/adr/ADR-010-security.md) | Security analysis, code smell detection, architectural violations, quality review |
| [ADR-011: Observability & Logging](docs/adr/ADR-011-observability-logging.md) | **Every** new feature, command, endpoint, integration, webhook or background job — logging is mandatory and on by default |

---

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
| `TenantCore.Logging` | Independent, queued logging to Azure Table Storage (`ApiErrorLogs`, `FrontendErrorLogs`, `ActionLogs`, `ApiRequestLogs`) — mandatory, on by default (ADR-011) |
| `TenantCore.Web.Client` | Blazor WASM — typed API clients, pages, components |

> Claude Code auto-loads this `CLAUDE.md`, **not** `.clinerules` (that file is Cline's). The `.claude/commands/*` workflows read `.clinerules` explicitly.

---

## Coding Patterns

### Controller (`TenantCore.Api/Controllers/`)

Thin. All clinic-scoped controllers inherit `ClinicControllerBase`. Only `ISender` injected. Zero business logic.

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatientsController(ISender sender) : ClinicControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PatientSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetPatientsQuery(GetApplicationId()), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPatientByIdQuery(id, GetApplicationId()), ct));

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreatePatientCommand(request, GetApplicationId()), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}
```

- Use `GetApplicationId()` from `ClinicControllerBase` — never read `X-Application-Id` header directly
- Always pass `ApplicationId` into commands and queries (multi-tenancy — see ADR-008)

### Command / Query (`TenantCore.Application/Features/{Area}/Commands|Queries/`)

`sealed record` implementing `IRequest<T>`. For delete/void: `IRequest` (no type param).

```csharp
// Commands
public sealed record CreatePatientCommand(CreatePatientRequest Request, Guid ApplicationId) : IRequest<Guid>;
public sealed record UpdatePatientCommand(Guid Id, UpdatePatientRequest Request, Guid ApplicationId) : IRequest<PatientDto>;
public sealed record DeletePatientCommand(Guid Id, Guid ApplicationId) : IRequest;

// Queries
public sealed record GetPatientsQuery(Guid ApplicationId) : IRequest<IEnumerable<PatientSummaryDto>>;
public sealed record GetPatientByIdQuery(Guid Id, Guid ApplicationId) : IRequest<PatientDto>;
```

### Handler (`TenantCore.Application/Features/{Area}/Handlers/`)

`sealed class`. Primary constructor. One handler per command/query file.

```csharp
public sealed class CreatePatientHandler(
    IPatientRepository repository,
    ILogger<CreatePatientHandler> logger)
    : IRequestHandler<CreatePatientCommand, Guid>
{
    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating patient for application {ApplicationId}", request.ApplicationId);

        var patient = PatientTranslator.ToEntity(request.Request, request.ApplicationId);
        await repository.AddAsync(patient);
        await repository.SaveChangesAsync();

        return patient.Id;
    }
}
```

### Validator (`TenantCore.Application/Features/{Area}/Validators/`)

FluentValidation. Auto-registered by MediatR `ValidationBehavior` pipeline.

```csharp
public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
```

### Translator (`TenantCore.Application/Features/{Area}/Translators/`)

`static class` with `static` methods. **Never use AutoMapper.**

```csharp
public static class PatientTranslator
{
    public static Patient ToEntity(CreatePatientRequest request, Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = request.FirstName,
        LastName = request.LastName,
        ApplicationId = applicationId,
        CreatedAt = DateTime.UtcNow
    };

    public static PatientDto ToDto(Patient entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName
    };

    public static IEnumerable<PatientSummaryDto> ToDtoList(IEnumerable<Patient> entities)
        => entities.Select(e => new PatientSummaryDto { Id = e.Id, FullName = $"{e.FirstName} {e.LastName}" });
}
```

### Domain Entity (`TenantCore.Domain/Entities/`)

Inherit `AuditableEntity` (for user-trackable entities) or `BaseEntity` (for lookup data). No EF Data Annotations.

```csharp
public class Patient : AuditableEntity   // AuditableEntity → BaseEntity (Id, CreatedAt, UpdatedAt, RowVersion)
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }   // Tenant ID — always present on tenant-scoped entities
    public Gender Gender { get; set; }
}
```

Base classes: `BaseEntity` (Id Guid, CreatedAt, UpdatedAt, RowVersion), `AuditableEntity : BaseEntity` (+ CreatedBy, UpdatedBy).

### Repository Interface (`TenantCore.Domain/Interfaces/`)

```csharp
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByMrNumberAsync(string mrNumber, Guid applicationId);
    Task<IEnumerable<Patient>> SearchAsync(string term, Guid applicationId);
}
```

Generic base: `IRepository<T>` provides GetByIdAsync, GetAllAsync, AddAsync, Update, Delete, ExistsAsync, SaveChangesAsync.

### Repository Implementation (`TenantCore.Infrastructure/Repositories/`)

Inherit `ClinicRepository<T>`. Always filter by `applicationId` for tenant-scoped queries.

```csharp
public class PatientRepository : ClinicRepository<Patient>, IPatientRepository
{
    public PatientRepository(ClinicDbContext context) : base(context) { }

    public async Task<Patient?> GetByMrNumberAsync(string mrNumber, Guid applicationId) =>
        await _dbSet
            .AsNoTracking()
            .Where(p => p.MrNumber == mrNumber && p.ApplicationId == applicationId)
            .FirstOrDefaultAsync();
}
```

### EF Entity Configuration (`TenantCore.Infrastructure/Persistence/Configurations/`)

```csharp
internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
```

### DTOs (`TenantCore.Shared/Dtos/`)

```csharp
// Read DTO — record with init setters
public record PatientDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

// Request DTOs — for [FromBody] parameters
public record CreatePatientRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
}
```

---

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
    CreateFooRequest.cs
    UpdateFooRequest.cs

tests/
  TenantCore.Application.Tests/Features/Foos/
    Commands/
      CreateFooHandlerTests.cs
      UpdateFooHandlerTests.cs
      DeleteFooHandlerTests.cs
    Queries/
      GetFoosHandlerTests.cs
      GetFooByIdHandlerTests.cs
    Validators/
      CreateFooCommandValidatorTests.cs
      UpdateFooCommandValidatorTests.cs
    Translators/
      FooTranslatorTests.cs
```

---

## Error Handling

Throw domain exceptions — middleware (`ExceptionHandlingMiddleware`) converts to ProblemDetails automatically.

| Exception | HTTP Status |
|---|---|
| `ValidationException` (FluentValidation via pipeline) | 400 |
| `EntityNotFoundException` | 404 |
| `InvalidOperationException` | 409 |
| `UnauthorizedAccessException` | 401 |

Never return error responses manually from handlers or controllers.

---

## Authorization

```csharp
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]   // Any valid JWT
[Authorize(Policy = AuthPolicies.RequireClinicAdmin)]     // ClinicAdmin for current clinic
[Authorize(Policy = AuthPolicies.RequireReception)]       // Reception for current clinic
[Authorize(Policy = AuthPolicies.RequireClinical)]        // Clinical for current clinic
```

Constants in `TenantCore.Shared/Authorization/AuthorizationConstants.cs`.

---

## Multi-Tenancy (Critical)

- The current clinic is identified by `X-Application-Id` HTTP header
- `ClinicContextMiddleware` validates it and stores the result in `HttpContext.Items`
- `ClinicControllerBase.GetApplicationId()` retrieves it — always use this, never read the header directly
- **Every command and query that touches tenant data must include `ApplicationId`**
- **Every repository query for tenant-scoped entities must filter by `applicationId`**

---

## Dependency Injection

- Application DI: `src/TenantCore.Application/DependencyInjection.cs` (`AddApplication()`)
- Infrastructure DI: `src/TenantCore.Infrastructure/DependencyInjection.cs` (`AddInfrastructure()`)
- Handlers and validators are **auto-registered** via MediatR assembly scan — no manual registration
- New repositories → register in Infrastructure DI as `Scoped`

---

## Database Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api \
  --output-dir Persistence/ClinicMigrations

dotnet ef database update \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

---

## Auth API Integration

`TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs` calls TenantCore.Auth.
Named HttpClient: `"AuthApi"`. Base URL: `AuthApi:BaseUrl` in appsettings.

---

## Email Notifications

**Onboarding/payment emails are published to Azure Service Bus, not sent via SMTP.** `SendEmailTaskHandler` (a `WorkflowTaskType.SendEmail` handler) calls `INotificationPublisher.PublishEmailNotificationAsync` (`ServiceBusNotificationPublisher` in `TenantCore.Infrastructure/Services/`), which publishes an `EmailNotificationDto` to the same `email-notifications-queue` TenantCore.Auth publishes to (`AzureServiceBus:ConnectionString` / `AzureServiceBus:QueueName` — copy the connection string from Auth's config, it's the same Service Bus namespace for both repos). An external Azure Function (`Clinic.Notification`, out of scope) consumes the queue and sends the actual email.

- `OnboardingEmailTemplates.RenderForQueue(payloadJson)` turns the stored `{template, to, model}` payload into `(To, Template, Subject, TemplateData)` — reuses `Render()`'s per-template subject text so the two never drift, but does **not** send the HTML body it also builds (that HTML is currently unused for this path — see the workspace root `CLAUDE.md` → Cross-Repo Rule 9 for why).
- **`RecipientName` must not be empty** or the consumer dead-letters the message without attempting delivery. `SendEmailTaskHandler` derives it from the first non-empty field in `RequesterName` → `DoctorName` → `ClinicName` (most onboarding models only have `ClinicName`, no person's name).
- When adding a new onboarding email template in `OnboardingEmailTemplates`, you don't need to touch the notification consumer to make the message deliverable — but per root Rule 9, the recipient will see a plain-text dump of `TemplateData`, not your HTML, until that consumer supports real templates.
- **Prescription emails are a separate path and still use direct SMTP** — `SubmitPrescriptionHandler` calls `IEmailService.SendAsync` (`TenantCore.Infrastructure/Services/EmailService.cs`) directly, because it needs to attach a generated PDF, which the Service Bus/`EmailNotificationDto` contract doesn't support. That path needs real `Email:Host`/`Email:From`/etc. config to work — `EmailService` throws (not silently no-ops) if that section is missing, so a misconfiguration shows up as a failed/retried task instead of a false "success."

---

## Rules (Never Violate)

- **Logging is mandatory and on by default ([ADR-011](docs/adr/ADR-011-observability-logging.md)).**
  - Every `…Command` is action-logged automatically (`ActionLoggingBehavior`).
  - Every `/api` request that fails, is slow or changes data goes to `ApiRequestLogs`.
  - Every `IHttpClientFactory` call is logged as an outbound row.
  - Every `LogError` reaches `ApiErrorLogs`.

  For new work:
  - **New commands:** name them `…Command`, and add `IActionLogContext` for searchable identifiers (payment, event and link ids).
  - **New integrations:** use `IHttpClientFactory` only.
  - **New jobs:** log each run that did work via `IActionLogger`.
  - **Opting out:** `ISkipActionLog` needs a documented reason.
  - **Never log** bodies, patient or personal data, secrets, tokens, signatures or query strings.
  - A feature is **not done** until its logs appear in the Admin Log Explorer.
- Controllers inherit `ClinicControllerBase` and only call `sender.Send(...)` — no service injection
- Commands and queries are `sealed record`; handlers are `sealed class`
- Translators are `static class` with `static` methods — never use AutoMapper
- No business logic in controllers, repositories, or middleware
- No EF Core in Application or Domain layers
- No Data Annotations on Domain entities — use Fluent API configurations
- Always include `ApplicationId` on tenant-scoped commands/queries
- Always filter by `applicationId` in tenant-scoped repository queries
- Throw exceptions for errors; never return error DTOs from handlers
- Do not change the middleware pipeline order (see ADR-005)
- **Every feature must ship with unit tests** — all handlers, validators, and translators must have tests covering happy path, not-found errors, cross-tenant isolation, and all validator boundary/edge cases (see ADR-009)
- **Every delete/remove/clear action must confirm first** — use the shared `ConfirmDialog` component (`TenantCore.Web.Client/Components/ConfirmDialog.razor`); never wire a destructive action straight to a button's `@onclick` (see ADR-007, "Destructive Action Confirmation")
