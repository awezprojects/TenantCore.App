You are implementing a planned feature for **TenantCore.App**. $ARGUMENTS is the feature name (matches the folder under `plan/`).

---

## Step 1 — Load the plan and context

Read these files — nothing else until a specific file must be modified:

1. `plan/$ARGUMENTS/PLAN.md` — the approved implementation blueprint
2. `CLAUDE.md` — patterns, rules, absolute constraints

Then read only the ADRs that are relevant to the layers this plan touches (check the "Layers Affected" table in PLAN.md):

| Layer in plan | ADR to read |
|--------------|-------------|
| Domain | `.claude/docs/adr/ADR-002-domain-layer.md` |
| Application | `.claude/docs/adr/ADR-003-application-layer.md` |
| Infrastructure | `.claude/docs/adr/ADR-004-infrastructure-layer.md` |
| API | `.claude/docs/adr/ADR-005-api-layer.md` |
| Shared | `.claude/docs/adr/ADR-006-shared-layer.md` |
| Blazor Client | `.claude/docs/adr/ADR-007-blazor-client.md` |
| Multi-tenancy | `.claude/docs/adr/ADR-008-multi-tenancy.md` |

---

## Step 2 — Verify the plan is complete

Check PLAN.md has all required sections:

- [ ] Overview
- [ ] Entity properties table (if new entity)
- [ ] Files to Create — all layers listed
- [ ] Files to Modify table
- [ ] API Endpoints table
- [ ] Business Rules
- [ ] Implementation Order
- [ ] Multi-Tenancy Checklist (if tenant-scoped)

If any required section is missing or has unfilled placeholders, stop and say:
> "PLAN.md is incomplete — run `/plan-app-feature $ARGUMENTS` first or fill in the missing sections."

---

## Step 3 — Print pre-execution summary

Before writing a single file, print this summary so the user knows what is about to happen:

```
## Executing: <Feature Name>  [TenantCore.App]

Implementation order:
  1. <first step from plan>
  2. <second step>
  ... (all steps)

Files to create: N
Files to modify: N
EF migration required: Yes/No

Starting implementation now.
```

No approval needed — the plan was already approved. Proceed immediately after printing.

---

## Step 4 — Implement in the exact order from PLAN.md

Follow the Implementation Order section from PLAN.md step by step. Do not reorder.

Standard order for TenantCore.App features:
1. Shared DTOs
2. Domain entity
3. Domain repository interface
4. Infrastructure EF configuration
5. Infrastructure repository implementation
6. Modify ClinicDbContext
7. Modify Infrastructure DependencyInjection
8. Application commands + queries
9. Application validators
10. Application translator
11. Application handlers
12. API controller
13. Migration (reminder only — do not run)

### Shared Layer rules (`src/TenantCore.Shared/Dtos/`)

- DTOs are `record` types with `init`-only properties
- Read DTO (`XDto`): all fields the API consumer needs on a detail view
- Summary DTO (`XSummaryDto`): lean version for list views — Id, display name, status only
- Request DTOs (`CreateXRequest`, `UpdateXRequest`): fields the caller provides
- No methods, no validation attributes, no business logic

### Domain Layer rules (`src/TenantCore.Domain/`)

- Entities inherit `AuditableEntity` (when created-by/updated-by matters) or `BaseEntity` (lookup data)
- `Id` is always `Guid`
- Tenant-scoped entities always have `public Guid ApplicationId { get; set; }`
- No EF Data Annotations — configuration goes in Infrastructure
- No static factory methods — entity is a plain POCO
- Repository interface extends `IRepository<T>` and adds only methods the generic interface cannot satisfy

### Infrastructure Layer rules (`src/TenantCore.Infrastructure/`)

- EF configuration: one `IEntityTypeConfiguration<T>` class per entity in `Persistence/Configurations/`
- All constraints defined via Fluent API — `IsRequired()`, `HasMaxLength()`, `HasIndex()`, `IsRowVersion()`
- Relationships configured here, not in entity class
- Repository extends `ClinicRepository<T>` — provides base CRUD automatically
- Custom query methods always filter by `applicationId` for tenant-scoped entities
- Use `AsNoTracking()` on read-only queries
- Never expose `IQueryable<T>` — always materialize (`ToListAsync`, `FirstOrDefaultAsync`)

**Before modifying `ClinicDbContext.cs`:** Read the file, then add `DbSet<T>` in alphabetical order with existing sets.

**Before modifying `DependencyInjection.cs`:** Read the file, then add `services.AddScoped<IXRepo, XRepo>()` following the existing grouping.

### Application Layer rules (`src/TenantCore.Application/Features/<Area>/`)

- Commands and queries: `sealed record` implementing `IRequest<TResponse>`
- Void commands (delete): `sealed record` implementing `IRequest`
- All commands include `Guid ApplicationId` as a property
- Handlers: `sealed class`, primary constructor injection, implements `IRequestHandler<TCommand, TResponse>`
- Handlers throw named domain exceptions on error — no `if/else` returning null
- `SaveChangesAsync()` called once at the end of write handlers — never mid-operation
- Validators: `sealed class` extending `AbstractValidator<TCommand>` — **no manual DI registration needed**
- Translator: `static class` with `static` methods — `ToEntity(command, applicationId)`, `ToDto(entity)`, `ToSummaryDto(entity)`
- Translator never calls `SaveChangesAsync` or any repository

### API Layer rules (`src/TenantCore.Api/Controllers/`)

- Inherit `ClinicControllerBase` — not plain `ControllerBase`
- Constructor injects only `ISender`
- Every action method passes `GetApplicationId()` into the command/query
- Action methods are 3–5 lines: build the command, call `sender.Send()`, return `ActionResult<T>`
- Decorate actions with `[ProducesResponseType]` for each possible response
- Apply `[Authorize(Policy = AuthPolicies.X)]` at class level with the minimum policy; override per-action where a stricter policy is needed

---

## Step 5 — Multi-tenancy verification

After all files are written, verify the multi-tenancy checklist from PLAN.md:

- [ ] `ApplicationId` on entity
- [ ] `ApplicationId` in all commands and queries
- [ ] Repository methods filter by `applicationId`
- [ ] Controller uses `GetApplicationId()`, never reads `X-Application-Id` header directly

If any item is unchecked, fix it before proceeding.

---

## Step 6 — Migration reminder

Print the migration command from PLAN.md. **Do not run it automatically.**

```
Migration ready to run:

dotnet ef migrations add <MigrationName> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api \
  --output-dir Persistence/ClinicMigrations

Run this when your local database is available.
```

---

## Step 7 — Update state snapshot

Edit `.claude/context/current-state.md` to reflect what was just added:

- Add the new `DbSet<Entity>` row to the DbSets table (alphabetical order, include Tenant-Scoped column)
- Increment "Total DbSets" count by the number of new DbSets added
- Add the new repository registration row to the Repositories table
- Update "Last verified" date to today

Do not rewrite the whole file — edit only the rows that changed.

---

## Step 8 — Update feature registry

Edit `plan/REGISTRY.md`:

- Move the feature from "Planned" to "Executed Features" table (if it was listed under Planned)
- If it was not listed (executed directly without a prior plan entry), add it fresh to "Executed Features"
- Add the new entity/domain to the "Domain Coverage Map" table so future plan sessions know it exists
- Fill in all columns: Feature, Plan Date, Execute Date, New Entities, New DbSets, Files Created, Files Modified

---

## Step 9 — Implementation summary

Print a completion table:

```
## Feature '$ARGUMENTS' implemented  [TenantCore.App]

| File | Action |
|------|--------|
| src/TenantCore.Shared/Dtos/XDto.cs | Created |
| src/TenantCore.Domain/Entities/X.cs | Created |
| ... | ... |
| src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs | Modified |

State snapshot: updated (.claude/context/current-state.md)
Feature registry: updated (plan/REGISTRY.md)
Migration: pending — run the command above before testing.

Next: run `/feature-security-analysis $ARGUMENTS` before merging.
```
