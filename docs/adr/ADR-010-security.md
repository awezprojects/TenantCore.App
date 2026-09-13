# ADR-010: Security Analysis & Code Quality Standards

**Status:** Active
**Scope:** TenantCore.App — all layers

---

## Purpose

This ADR defines the complete security and code quality checklist for TenantCore.App. It is the authoritative reference for the `security-app-feature` command. Read this before performing any security review, code audit, or quality scan on this repo.

---

## Section 1 — Authentication & Authorization

### Rules
- Every controller class or action has an explicit `[Authorize(Policy = AuthPolicies.X)]` or `[AllowAnonymous]` — never rely on a global fallback
- Destructive operations (Delete, BulkDelete, Deactivate) require at minimum `RequireClinicAdmin`
- Read operations for authenticated users require at minimum `RequireAuthenticated`
- `[AllowAnonymous]` must be intentional — add an inline comment explaining why the endpoint is public

### Violations to detect
- Controller class with no `[Authorize]` attribute and no per-action attributes
- `DELETE` or `PUT` action with `RequireAuthenticated` instead of `RequireClinicAdmin`
- Action reading `User.Claims` directly instead of going through the authorization framework
- A new endpoint reachable without any auth attribute present

---

## Section 2 — Multi-Tenancy Isolation

### Rules
- Every tenant-scoped entity has `public Guid ApplicationId { get; set; }`
- Every command and query that touches tenant data carries `Guid ApplicationId` as a property
- Every repository query for tenant-scoped data filters by `applicationId` — no cross-tenant leakage
- Controllers always call `GetApplicationId()` from `ClinicControllerBase` — never read `X-Application-Id` header directly
- `ApplicationId` must come from `GetApplicationId()` only — never from `[FromBody]` or `[FromQuery]`

### Violations to detect
- Repository method loading all rows without an `applicationId` filter on a tenant-scoped entity
- Command or query missing `Guid ApplicationId` property
- Controller action containing `Request.Headers["X-Application-Id"]`
- Request DTO with an `ApplicationId` field (caller must not supply it)
- Handler setting `entity.ApplicationId = request.Request.ApplicationId` from body instead of from command parameter

---

## Section 3 — Input Validation

### Rules
- All string inputs: `NotEmpty()` + `MaximumLength(N)` in the FluentValidation validator class
- All ID route parameters: typed as `Guid` — never `string` or `int` for entity IDs
- Enum inputs: validated with `Must(v => Enum.IsDefined(typeof(MyEnum), v))` before use in handlers
- Paging: `pageSize` capped at 100 maximum — reject `pageSize=99999`
- No dynamic LINQ expressions or raw query predicates built from user-supplied string input

### Violations to detect
- Validator class with a string property that has `NotEmpty()` but no `MaximumLength()`
- Route parameter `{id}` bound to `string id` instead of `Guid id`
- `pageSize` parameter with no maximum cap enforced in validator or handler
- `(MyEnum)intFromRequest` cast without checking `Enum.IsDefined` first

---

## Section 4 — Data Access & SQL Injection

### Rules
- All queries go through EF Core LINQ — parameterized automatically
- `ExecuteSqlRaw` is banned; use `ExecuteSqlInterpolated` if raw SQL is absolutely required
- Never concatenate user input into any string passed to `FromSql`, `ExecuteSqlRaw`, or `ExecuteSqlInterpolated`
- `AsNoTracking()` on all read-only queries in repositories
- `IQueryable<T>` never exposed from any repository method — always materialize

### Violations to detect
- `ExecuteSqlRaw($"SELECT ... {variable}")` — direct injection risk
- `IQueryable<T>` as return type on any repository interface or implementation method
- Missing `AsNoTracking()` in a read repository method (GetAll, Search, GetBy*)
- `FromSql` with any string concatenation or interpolation involving request data

---

## Section 5 — Sensitive Data Handling

### Rules
- Passwords, JWT secrets, connection strings: never in log statements — use Serilog destructuring exclusions
- Response DTOs must not expose internal fields: no `PasswordHash`, no raw token values, no `RowVersion` bytes
- Error responses go through `ExceptionHandlingMiddleware` as `ProblemDetails` — no manual `catch` returning raw exception messages
- `CorrelationId` propagated on all outgoing HTTP calls to TenantCore.Auth

### Violations to detect
- `logger.LogInformation("Password: {pwd}", request.Password)` or similar
- DTO `record` with a `PasswordHash` or `TokenValue` property
- Controller `try/catch` returning `ex.Message` directly in an `Ok()` or `BadRequest()`
- External HTTP call to `"AuthApi"` missing the correlation ID header

---

## Section 6 — Business Logic Security

### Rules
- Ownership checks: before modifying or deleting an entity, verify it belongs to the caller's `ApplicationId`
- Tenant isolation: no entity can be accessed by a different clinic's `ApplicationId`
- Duplicate creation guarded: unique constraint in EF config + application-level check in handler before insert
- Hard-delete vs soft-delete is intentional — any `Delete` command should be verified as intentional in PLAN.md
- Concurrency: `RowVersion` on `BaseEntity` — EF optimistic concurrency handles race conditions on updates

### Violations to detect
- Update handler that does not verify `entity.ApplicationId == command.ApplicationId`
- Delete handler that calls `repository.Delete(entity)` without first confirming `entity.ApplicationId` matches
- Create handler with no uniqueness check where one is logically expected (e.g., unique MR number)
- Hard delete when the plan described a soft delete (or vice versa)

---

## Section 7 — OWASP Top 10 Quick Scan

| # | Check | What to look for |
|---|-------|-----------------|
| A01 Broken Access Control | Every resource action checks ownership or `applicationId` | Missing tenant filter or auth policy |
| A02 Cryptographic Failures | No sensitive data stored in plain text | Unencrypted passwords or tokens in DB |
| A03 Injection | Parameterized queries only | `ExecuteSqlRaw` with string interpolation |
| A04 Insecure Design | No security-through-obscurity | Hidden endpoints without `[Authorize]` |
| A05 Security Misconfiguration | Swagger not leaking internal endpoints in production | Swagger enabled without env guard |
| A07 Auth Failures | Tokens validated with clock skew = 0, expiry enforced | JWT config missing `ValidateLifetime = true` |
| A08 Integrity Failures | No untrusted deserialization of user data | `JsonSerializer.Deserialize` with user-supplied type discriminators |

---

## Section 8 — Code Smells (App-Specific)

| # | Smell | Pattern to detect | Fix |
|---|-------|------------------|-----|
| S1 | Business logic in controller | `if/else`, repository call, or calculation directly in action method body | Move to handler |
| S2 | Service injected in controller | Constructor with anything other than `ISender` | Remove; route through a command |
| S3 | AutoMapper in Application layer | `IMapper`, `_mapper.Map<T>()`, or `[AutoMap]` in any Application file | Replace with static `Translator` method |
| S4 | Non-sealed command or handler | `record` command or `class` handler missing `sealed` keyword | Add `sealed` |
| S5 | `SaveChangesAsync()` called before last write | Call appears mid-handler before another mutation | Move all calls to the very end |
| S6 | Multiple `SaveChangesAsync()` in one handler | More than one `await repo.SaveChangesAsync()` | Consolidate to a single call at end |
| S7 | EF Core in Application or Domain layer | `using Microsoft.EntityFrameworkCore` in Application or Domain project file | Move to Infrastructure; inject via repo interface |
| S8 | Data Annotations on Domain entity | `[Required]`, `[MaxLength]`, `[Key]` on entity property | Remove; configure in `IEntityTypeConfiguration<T>` |
| S9 | Translator as instance class | `FooTranslator` registered in DI as a non-static class | Convert to `static class` with `static` methods; remove DI registration |
| S10 | Missing `AsNoTracking()` on read | Read handler's repo call does not use `AsNoTracking()` | Add `AsNoTracking()` in the repository read method |
| S11 | `Guid.NewGuid()` in handler | `entity.Id = Guid.NewGuid()` set inside a handler method | Move ID generation to `Translator.ToEntity()` |
| S12 | Handler returns null on not-found | Handler returning `null` instead of throwing `EntityNotFoundException` | Throw `EntityNotFoundException` |

---

## Section 9 — Architectural Violations

| # | Violation | Symptom | Fix |
|---|-----------|---------|-----|
| V1 | Wrong dependency direction | Application project csproj references Infrastructure | Invert via domain interface |
| V2 | Wrong dependency direction | Domain project references Application or Infrastructure | Remove the reference |
| V3 | `ClinicControllerBase` not used | Controller inherits plain `ControllerBase` | Inherit `ClinicControllerBase` |
| V4 | Middleware pipeline order changed | `app.Use*` ordering in `Program.cs` differs from ADR-005 canonical order | Restore; document why if deviation is intentional |
| V5 | Feature files outside Features folder | Handler or validator placed directly in Application root | Move to `Features/{Area}/Handlers/` or `Features/{Area}/Validators/` |
| V6 | Repository exposes `DbContext` publicly | `ClinicDbContext` property exposed on a repository class | Remove; access only through repository methods |
| V7 | Command or query is not a `record` | `class` used instead of `sealed record` for a command or query | Change to `sealed record` |

---

## Section 10 — Over-Engineering

| # | Pattern | When it's a problem | Fix |
|---|---------|--------------------|----|
| O1 | Validator with only `ApplicationId` check | `AbstractValidator` with a single `RuleFor(x => x.ApplicationId).NotEmpty()` | Add the actual field rules; this is an incomplete validator |
| O2 | Translator method returning only ID | `ToDto` that copies only `Id` | Complete the mapping or explain the omission in a comment |
| O3 | Repository method duplicating base class | Custom `GetAllAsync` in repo that calls `_dbSet.ToListAsync()` when base already provides it | Remove the duplicate; use inherited method |
| O4 | Generic catch hiding specific errors | `catch (Exception)` in a handler swallowing all exceptions | Remove; let middleware handle; catch only named exceptions |
| O5 | Empty handler body | Handler with a `// TODO` comment and no implementation | Implement or remove before merge |

---

## Section 11 — Safe-Fix Guidelines

Follow these rules every time you apply a fix. Do not skip them even for trivial changes.

1. **Read the file first** — never edit without seeing the current content
2. **Find all callers** — if a method signature changes (e.g., adding a parameter), grep for all call sites before editing
3. **Preserve observable behavior** — every fix must leave external behavior (HTTP responses, DB writes, log output) identical; only structure changes
4. **One logical fix per edit call** — do not combine unrelated fixes in one `Edit` call
5. **Do not refactor surrounding code** — fix only the flagged line(s); leave adjacent untouched code as-is
6. **Migration files are immutable** — never edit files in `Persistence/ClinicMigrations/`
7. **Middleware pipeline order is immutable** — do not reorder `app.Use*` calls in `Program.cs`
8. **After fixing**: describe what changed and confirm no behavior change in your output message

**Never change without explicit user instruction:**
- Migration files
- `RowVersion` concurrency token configuration
- Middleware pipeline order
- JWT validation parameters in `Program.cs`
- `GetApplicationId()` implementation in `ClinicControllerBase`
