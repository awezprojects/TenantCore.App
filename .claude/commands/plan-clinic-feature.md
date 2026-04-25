Plan a new feature for the TenantCore.App codebase. The feature to plan is: $ARGUMENTS

---

## Step 1 — Derive plan metadata

Convert the feature description to a kebab-case slug (e.g. "user roles management" → "user-roles-management").
Check whether `plan/<slug>/PLAN.md` already exists. If it does, tell the user and stop — do not overwrite an existing plan.

---

## Step 2 — Explore the codebase

Read and understand what currently exists so the plan only creates what is actually missing. Scan all of:

- `src/TenantCore.Api/Controllers/` — controller patterns and existing routes
- `src/TenantCore.Application/Features/` — all feature areas: Commands, Queries, Handlers, Validators, Translators
- `src/TenantCore.Domain/Entities/` — existing domain entities and their properties
- `src/TenantCore.Domain/Interfaces/` — existing repository interfaces
- `src/TenantCore.Infrastructure/Repositories/` — existing repository implementations
- `src/TenantCore.Infrastructure/Persistence/Configurations/` — EF entity configurations
- `src/TenantCore.Shared/Dtos/` — existing DTOs (read + write)
- `src/TenantCore.Web.Client/Pages/` — Blazor WASM pages
- `src/TenantCore.Web.Client/Clients/` — typed API clients
- `src/TenantCore.Web.Client/Components/` (if present) — shared UI components

For each layer, note: what already exists for this domain area, what is missing, what needs to change.

---

## Step 3 — Write the plan

Create `plan/<slug>/PLAN.md` using exactly this structure:

```markdown
# Feature Plan: <Feature Title>

## Repo
TenantCore.App

## Overview
<2–3 sentence description: what the feature does, why it is needed, what problem it solves>

## Domain Area
`<FeatureName>` — <which layers are touched: Api, Application, Domain, Infrastructure, Shared, Web.Client>

---

## Files to Create

| File | Purpose |
|------|---------|
| `<exact/path/from/repo/root.cs>` | <one-line purpose> |

_(List every new file. If none, write "None.")_

---

## Files to Modify

| File | Change |
|------|--------|
| `<exact/path/from/repo/root.cs>` | <precise description of the change> |

_(List every modified file. If none, write "None.")_

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|--------------|----------|-------------|
| GET | /api/<resource> | — | `PagedResult<XxxDto>` | RequireAuthenticated |
| POST | /api/<resource> | `CreateXxxDto` | `XxxDto` (201) | RequireManagement |
| PUT | /api/<resource>/{id} | `UpdateXxxDto` | `XxxDto` | RequireManagement |
| DELETE | /api/<resource>/{id} | — | 204 | RequireAdmin |

_(Omit rows for endpoints that are not part of this feature.)_

---

## Entity Properties

| Property | Type | Constraints |
|----------|------|-------------|
| `Id` | `Guid` | PK |
| ... | ... | ... |

_(Only list properties for new or modified entities. Write "N/A" if no entity changes.)_

---

## Validation Rules

| Field | Rule |
|-------|------|
| `Name` | NotEmpty, MaxLength(200) |
| ... | ... |

_(List all FluentValidation rules for new commands. Write "N/A" if no new validators.)_

---

## Business Rules

- <Bullet list of domain invariants, uniqueness constraints, ownership rules, state machine rules>
- Write "N/A" if none.

---

## Exact Code Snippets

For every non-trivial file, include the complete or key code so the plan can be executed by reading it alone.
Follow these patterns exactly:

**Controller** — thin, primary constructor, only `sender.Send(...)`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class XxxController(ISender sender) : ControllerBase { ... }
```

**Command/Query** — `sealed record`, `IRequest<T>`:
```csharp
public sealed record CreateXxxCommand(string Name) : IRequest<XxxDto>;
```

**Handler** — `sealed class`, primary constructor, one per file:
```csharp
public sealed class CreateXxxHandler(IXxxRepository repository, ILogger<CreateXxxHandler> logger)
    : IRequestHandler<CreateXxxCommand, XxxDto> { ... }
```

**Translator** — `static class`, `static` methods, no AutoMapper:
```csharp
public static class XxxTranslator
{
    public static XxxDto ToDto(Xxx entity) => new() { ... };
}
```

**Entity** — static factory, private EF constructor:
```csharp
public class Xxx : AuditableEntity
{
    private Xxx() { }
    public static Xxx Create(...) => new() { Id = Guid.NewGuid(), ... };
}
```

**Read DTO** — `class` with `init` setters:
```csharp
public class XxxDto { public Guid Id { get; init; } ... }
```

**Write DTO** — `sealed record`:
```csharp
public sealed record CreateXxxDto(string Name);
```

---

## Implementation Order

Numbered list of every change, in the exact order to apply them:

1. `TenantCore.Domain/Entities/Xxx.cs` — create entity
2. `TenantCore.Domain/Interfaces/IXxxRepository.cs` — create interface
3. `TenantCore.Shared/Dtos/XxxDto.cs` — create read DTO
4. `TenantCore.Shared/Dtos/CreateXxxDto.cs` — create write DTOs
5. `TenantCore.Infrastructure/Persistence/Configurations/XxxConfiguration.cs` — create EF config
6. `TenantCore.Infrastructure/Repositories/XxxRepository.cs` — create repository
7. `TenantCore.Infrastructure/DependencyInjection.cs` — register repository
8. `TenantCore.Application/Features/Xxxs/Translators/XxxTranslator.cs` — create translator
9. `TenantCore.Application/Features/Xxxs/Commands/CreateXxxCommand.cs` — create command
10. `TenantCore.Application/Features/Xxxs/Handlers/CreateXxxHandler.cs` — create handler
11. `TenantCore.Application/Features/Xxxs/Validators/CreateXxxCommandValidator.cs` — create validator
12. `TenantCore.Api/Controllers/XxxsController.cs` — create controller
13. `TenantCore.Web.Client/Clients/XxxApiClient.cs` — create typed client
14. `TenantCore.Web.Client/Pages/Xxxs/XxxList.razor` — create list page
15. Run migration if DB changes: `dotnet ef migrations add <MigrationName>`

---

## Migration Name

<PascalCase name if schema changes are needed, e.g. "AddXxxEntity". Write "None" if no DB changes.>

---

## Execution Status

- **Status**: Not started
```

The plan must be self-contained and precise. Every file path must be exact (relative from repo root). Every code snippet must be complete enough that a developer can implement it without referring to anything else. Do not leave vague instructions like "implement as needed" — specify exactly what to write.
