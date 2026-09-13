# ADR-003 — Application Layer: CQRS & MediatR

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Application`  
**Path:** `src/TenantCore.Application/`

---

## Decision

The Application layer implements **CQRS (Command Query Responsibility Segregation)** using **MediatR**. All business logic lives here. Features are organized by **domain feature folders**, not by technical type. FluentValidation runs automatically via a MediatR pipeline behavior.

---

## Folder Structure

```
TenantCore.Application/
├── Common/
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs       # Logs all requests/responses
│   │   └── ValidationBehavior.cs    # Runs FluentValidation before handler
│   └── IApplicationAccessValidator.cs  # Multi-tenant access check interface
├── Features/
│   ├── Applications/
│   │   ├── Commands/
│   │   ├── Handlers/
│   │   ├── Queries/
│   │   ├── Validators/
│   │   └── Translators/
│   ├── Patients/
│   │   ├── Commands/
│   │   ├── Handlers/
│   │   ├── Queries/
│   │   ├── Validators/
│   │   └── Translators/
│   ├── Medicines/
│   ├── MedicineTypes/
│   ├── MedicineDosageForms/
│   ├── Prescriptions/
│   ├── PrescriptionConfig/
│   ├── OpdRegistrations/
│   ├── IpdRegistrations/
│   ├── DosageRemarks/
│   ├── DoctorProfiles/
│   ├── DoctorSpecialities/
│   ├── Clinics/
│   └── ClinicSettings/
├── Services/                        # Interfaces for external dependencies
│   ├── IAuthApplicationService.cs
│   ├── IAuthClinicService.cs
│   ├── IBlobStorageService.cs
│   ├── IEmailService.cs
│   ├── IFileStorageService.cs
│   └── IPdfConversionService.cs
└── DependencyInjection.cs           # Registers MediatR + FluentValidation
```

---

## CQRS Pattern

### Commands (Write Operations)

Commands mutate state. They follow a naming convention: `{Verb}{Entity}Command`.

```csharp
// TenantCore.Application/Features/Patients/Commands/CreatePatientCommand.cs
public record CreatePatientCommand : IRequest<Guid>
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public Guid ApplicationId { get; init; }
    // ... other fields from the DTO
}
```

**Rules:**
- Commands are C# `record` types (immutable)
- Commands implement `IRequest<TResponse>` from MediatR
- Response type is typically `Guid` (new entity id) or `bool` (success)
- Commands carry everything the handler needs — no ambient state lookups inside handlers

### Command Handlers

```csharp
// TenantCore.Application/Features/Patients/Handlers/CreatePatientCommandHandler.cs
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Guid>
{
    private readonly IPatientRepository _patientRepository;

    public CreatePatientCommandHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = PatientTranslator.ToEntity(request);
        await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();
        return patient.Id;
    }
}
```

**Rules:**
- Handler filename: `{CommandName}Handler.cs`
- Inject ONLY repository interfaces and service interfaces (from Domain and Application respectively)
- Call `SaveChangesAsync()` at the end of write handlers — never in the middle
- One handler per command — do not put multiple commands in one handler class

### Queries (Read Operations)

Queries return data without mutating state. Naming convention: `Get{Entity}{Qualifier}Query`.

```csharp
// TenantCore.Application/Features/Patients/Queries/GetPatientByIdQuery.cs
public record GetPatientByIdQuery : IRequest<PatientDto>
{
    public Guid PatientId { get; init; }
    public Guid ApplicationId { get; init; }
}
```

**Rules:**
- Query response types are DTOs from `TenantCore.Shared`, not domain entities
- Queries NEVER call `SaveChangesAsync()`
- Queries may call multiple repositories if needed to compose the response
- Return `null` or a default DTO rather than throwing for "not found" scenarios (unless it's truly an error)

### Validators

Every command or query that accepts user input MUST have a validator.

```csharp
// TenantCore.Application/Features/Patients/Validators/CreatePatientCommandValidator.cs
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");
    }
}
```

**Rules:**
- Validator class name: `{CommandOrQueryName}Validator`
- Validators are auto-discovered and run by `ValidationBehavior` before the handler
- If validation fails, a `ValidationException` is thrown — the handler never executes
- Do NOT add validators for simple query-by-id operations — only where the caller provides free-form data

### Translators

Translators map between domain entities and DTOs/commands.

```csharp
// TenantCore.Application/Features/Patients/Translators/PatientTranslator.cs
public static class PatientTranslator
{
    public static Patient ToEntity(CreatePatientCommand command) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = command.FirstName,
        // ...
    };

    public static PatientDto ToDto(Patient entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        // ...
    };
}
```

**Rules:**
- Translators are `static` classes — they have no dependencies
- One translator per entity/feature
- Translators live in the `Translators/` subfolder of each feature

---

## MediatR Pipeline Behaviors

Behaviors run in a pipeline around every handler, in registration order:

```
Request → LoggingBehavior → ValidationBehavior → Handler → Response
```

### `LoggingBehavior<TRequest, TResponse>`
- Logs the request type and duration
- Runs for ALL requests (commands and queries)

### `ValidationBehavior<TRequest, TResponse>`
- Gathers all `IValidator<TRequest>` from DI
- Runs all validators
- If any validation fails, throws `ValidationException` (caught by `ExceptionHandlingMiddleware`)
- Runs BEFORE the handler

**Behaviors are registered in `DependencyInjection.cs`:**
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

---

## External Service Interfaces

The Application layer defines interfaces for services it needs but cannot implement:

| Interface | Purpose |
|-----------|---------|
| `IAuthApplicationService` | Create/update applications in TenantCore.Auth |
| `IAuthClinicService` | Manage clinic roles in TenantCore.Auth |
| `IBlobStorageService` | Store/retrieve files from Azure Blob |
| `IFileStorageService` | Local file storage fallback |
| `IEmailService` | Send emails |
| `IPdfConversionService` | Convert HTML reports to PDF |

**These interfaces are implemented in `TenantCore.Infrastructure`.** The application layer only references the interface — never the concrete class.

---

## Step-by-Step: Adding a New Feature

Example: Adding "Create Billing" feature.

1. Create folder: `Features/Billing/`
2. Create `Commands/CreateBillingCommand.cs` (record implementing `IRequest<Guid>`)
3. Create `Handlers/CreateBillingCommandHandler.cs` (implementing `IRequestHandler<CreateBillingCommand, Guid>`)
4. Create `Validators/CreateBillingCommandValidator.cs` (extending `AbstractValidator<CreateBillingCommand>`)
5. Create `Translators/BillingTranslator.cs` (static mapper between entity and DTO)
6. If query needed: create `Queries/GetBillingByIdQuery.cs` + handler + (optionally) validator
7. No manual DI registration needed — MediatR and FluentValidation auto-scan the assembly

---

## What NOT to Do

- Do NOT inject `HttpContext`, `IHttpContextAccessor`, or any ASP.NET Core types into handlers
- Do NOT call `SaveChangesAsync()` inside Translators or Validators
- Do NOT have handlers that contain both command and query logic
- Do NOT use AutoMapper — use static Translator classes
- Do NOT inject concrete repository implementations — always inject the interface
