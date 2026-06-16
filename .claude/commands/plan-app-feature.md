You are planning a feature for **TenantCore.App**. $ARGUMENTS format: `<feature-name> - <description>`

TenantCore.App is a multi-tenant clinic management system built with Clean Architecture + CQRS via MediatR. Read the CLAUDE.md and relevant ADRs before producing any plan.

---

## Step 0 — Verify state snapshot and read registry

Read these two files first — before anything else:

1. `.claude/context/current-state.md` — snapshot of current DbSets and DI registrations
2. `plan/REGISTRY.md` — all existing domain areas and previously executed features

**Then verify the snapshot is accurate (Option B auto-verify):**

- Read `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs`
- Count the `DbSet<T>` lines in the actual file
- Compare the count to "Total DbSets" in `current-state.md`
- **If counts match:** snapshot is current — do not re-read the DI file this session
- **If counts differ:** the snapshot is stale (manual edit happened outside this workflow)
  - Update `current-state.md` to reflect the actual DbContext
  - Read `src/TenantCore.Infrastructure/DependencyInjection.cs` and update the DI section too
  - Continue with the now-corrected snapshot

**Registry check:** Scan the Domain Coverage Map in `REGISTRY.md`. If the feature being requested overlaps with a pre-existing or already-executed domain area, flag it in the pre-plan brief before proceeding.

---

## Step 1 — Load context

Read these files in order:

1. `CLAUDE.md` — coding patterns, layer rules, absolute constraints
2. `.claude/docs/adr/ADR-000-index.md` — quick orientation

Then read **only the ADRs relevant to this feature** (do not read all ADRs):

| If the feature touches… | Read this ADR |
|------------------------|--------------|
| A new entity or data model | ADR-002 (Domain Layer) |
| Commands, queries, handlers, validators | ADR-003 (Application Layer) |
| Repositories, EF migrations, external services | ADR-004 (Infrastructure Layer) |
| Controllers, middleware, authorization | ADR-005 (API Layer) |
| DTOs, enums, constants | ADR-006 (Shared Layer) |
| Blazor pages or components | ADR-007 (Blazor Client) |
| Clinic/tenant data isolation | ADR-008 (Multi-Tenancy) |
| Test coverage | ADR-009 (Unit Testing) |

---

## Step 2 — Show the pre-plan brief and ask for approval

Present this concise overview — **no code blocks, no file lists yet**. This is a high-level checkpoint only.

```
## Feature: <Feature Name>  [TenantCore.App]

**Domain area:** <e.g., Billing, Patients, Prescriptions>
**Layers affected:** <list only layers that change, e.g., Domain, Application, Infrastructure, API>
**Is tenant-scoped?** Yes / No  (does data belong to a specific clinic?)
**Requires EF migration?** Yes / No

### What will be created (~N new files)
- Domain: <entity name> + repository interface
- Application: <N> handlers, <N> validators, translator
- Infrastructure: EF config, repository implementation
- API: <ControllerName> with <N> endpoints
- Shared: <N> DTOs

### What will be modified (~N files)
- ClinicDbContext — add DbSet
- Infrastructure DependencyInjection — register repository
- (any other existing file that needs a change)

### Key decisions / open questions
- <Any architectural decision that needs confirmation, e.g., "Should this be tenant-scoped?">
- <Any ambiguity in the feature description>
- <Any dependency on TenantCore.Auth changes?>

**Confirm to generate the full PLAN.md, or redirect the scope.**
```

**Stop here. Wait for explicit user confirmation before writing any files.**

---

## Step 3 — After approval: create the plan folder and PLAN.md

Create directory: `plan/<feature-name>/`

Write `plan/<feature-name>/PLAN.md` using the template below.

**Rules for the plan document:**
- No code blocks anywhere in the plan
- File lists describe what each file contains, not how it's implemented
- Tables for endpoints, entity fields, validation rules
- Implementation order must follow the dependency sequence (Domain → Infrastructure → Application → API)

---

### PLAN.md Template

```markdown
# Feature Plan: <Feature Name>

**Repo:** TenantCore.App
**Date:** <today's date>
**Domain area:** <Area>
**Status:** Approved — ready for execution

---

## Overview

<3–5 sentences: what the feature does, which clinic users interact with it, 
why it exists, and what data it manages. No implementation detail.>

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Domain | New entity + repository interface |
| Infrastructure | EF config, repository impl, migration |
| Application | Commands, queries, handlers, validators, translator |
| API | New controller with N endpoints |
| Shared | N new DTOs |

---

## Entity: <EntityName>

**Tenant-scoped:** Yes / No
**Base class:** AuditableEntity / BaseEntity

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| ApplicationId | Guid | FK to clinic — required if tenant-scoped |
| <Field> | <Type> | required / maxlength(N) / unique / nullable |
| CreatedAt | DateTime | set by EF |
| UpdatedAt | DateTime | set by EF |

---

## Files to Create

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Dtos/<Entity>Dto.cs` | Read response — all entity fields visible to the API consumer |
| `Dtos/<Entity>SummaryDto.cs` | Lean list response — only fields needed in list views |
| `Dtos/Create<Entity>Request.cs` | POST body — fields the caller provides on creation |
| `Dtos/Update<Entity>Request.cs` | PUT body — fields the caller can update |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/<Entity>.cs` | Domain entity — inherits AuditableEntity/BaseEntity |
| `Interfaces/I<Entity>Repository.cs` | Repository contract — extends IRepository<Entity>, adds domain-specific query methods |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/<Entity>Configuration.cs` | Fluent API EF config — constraints, indexes, relationships |
| `Repositories/<Entity>Repository.cs` | Implements I<Entity>Repository — extends ClinicRepository<Entity> |

### Application Layer (`src/TenantCore.Application/Features/<Area>/`)

| File | Purpose |
|------|---------|
| `Commands/Create<Entity>Command.cs` | Write command — carries all fields for creation + ApplicationId |
| `Commands/Update<Entity>Command.cs` | Write command — carries Id, updated fields, ApplicationId |
| `Commands/Delete<Entity>Command.cs` | Write command — carries entity Id and ApplicationId |
| `Queries/Get<Entity>sQuery.cs` | Read query — list, carries ApplicationId + optional filters |
| `Queries/Get<Entity>ByIdQuery.cs` | Read query — single by Id, carries ApplicationId |
| `Handlers/Create<Entity>Handler.cs` | Handles Create command — calls repo, saves, returns Id |
| `Handlers/Update<Entity>Handler.cs` | Handles Update command — validates existence, updates, saves |
| `Handlers/Delete<Entity>Handler.cs` | Handles Delete command — validates existence, removes, saves |
| `Handlers/Get<Entity>sHandler.cs` | Handles list query — returns mapped summary DTOs |
| `Handlers/Get<Entity>ByIdHandler.cs` | Handles single query — returns mapped full DTO |
| `Validators/Create<Entity>CommandValidator.cs` | FluentValidation rules for Create command |
| `Validators/Update<Entity>CommandValidator.cs` | FluentValidation rules for Update command |
| `Translators/<Entity>Translator.cs` | Static translator — ToEntity, ToDto, ToSummaryDto |

### API Layer (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|------|---------|
| `<Entity>sController.cs` | Inherits ClinicControllerBase — all CRUD endpoints for this entity |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` | Add `DbSet<<Entity>> <Entity>s` |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Register `I<Entity>Repository → <Entity>Repository` as Scoped |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| GET | `api/<area>s` | — | `IEnumerable<<Entity>SummaryDto>` | RequireAuthenticated |
| GET | `api/<area>s/{id}` | — | `<Entity>Dto` | RequireAuthenticated |
| POST | `api/<area>s` | `Create<Entity>Request` | `Guid` (201 Created) | RequireClinicAdmin |
| PUT | `api/<area>s/{id}` | `Update<Entity>Request` | `<Entity>Dto` | RequireClinicAdmin |
| DELETE | `api/<area>s/{id}` | — | 204 No Content | RequireClinicAdmin |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| <Field> | NotEmpty, MaxLength(N) |
| ApplicationId | NotEmpty — always required |

---

## Business Rules

Rules enforced in handlers — throw named domain exceptions on violation:

1. <Rule description> — throws `EntityNotFoundException` / `InvalidOperationException`
2. <Rule description> — e.g., "cannot delete an entity with active child records"

---

## Multi-Tenancy Checklist

- [ ] `ApplicationId` property present on entity
- [ ] `ApplicationId` passed in all commands and queries
- [ ] Repository queries filter by `applicationId`
- [ ] Controller uses `GetApplicationId()` from `ClinicControllerBase`
- [ ] Blazor client sends `X-Application-Id` header (if applicable)

---

## EF Migration

**Migration name:** `Add<EntityName>`

Run after all infrastructure files are created:
```
dotnet ef migrations add Add<EntityName> --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

Execute in this sequence to avoid compile errors:

1. Shared DTOs
2. Domain entity
3. Domain repository interface
4. Infrastructure EF configuration
5. Infrastructure repository implementation
6. Modify ClinicDbContext — add DbSet
7. Modify Infrastructure DependencyInjection — register repository
8. Application commands + queries
9. Application validators
10. Application translator
11. Application handlers
12. API controller
13. Run EF migration

---

## Test Coverage Required

| Test class | What it tests |
|-----------|--------------|
| `Create<Entity>HandlerTests` | Handler calls AddAsync and SaveChangesAsync with correct data; ApplicationId set on entity |
| `Update<Entity>HandlerTests` | Handler updates correct fields; throws EntityNotFoundException when entity not found |
| `Delete<Entity>HandlerTests` | Handler removes entity; throws EntityNotFoundException when not found |
| `Get<Entity>ByIdHandlerTests` | Returns mapped DTO; returns null for unknown Id |
| `Create<Entity>CommandValidatorTests` | Required fields fail when empty; valid command passes |
| `<Entity>TranslatorTests` | ToEntity maps all fields; ToDto maps all fields |

---

## Open Questions / Risks

- <Any unresolved design decision>
- <Any dependency on TenantCore.Auth>
- <Any concern about data volume or query performance>
```

---

## Step 4 — Output after PLAN.md is written

Print:
```
Plan written: TenantCore.App/plan/<feature-name>/PLAN.md

Files to create: N
Files to modify: N
EF migration required: Yes/No

Next step: run /execute-feature <feature-name> inside TenantCore.App to implement.
```
