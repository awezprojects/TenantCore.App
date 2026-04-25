Fix a bug in the TenantCore.App codebase. Bug context: $ARGUMENTS

---

## Step 1 — Parse the bug context

Read `$ARGUMENTS` carefully. Extract:
- **Symptom**: what the user observes (error message, wrong behavior, HTTP status, etc.)
- **Trigger**: what action or input causes it (endpoint called, command sent, page visited, etc.)
- **Scope hint**: any file names, layer names, or feature area the user mentioned

If `$ARGUMENTS` is empty or too vague to locate the bug, stop and ask the user to provide:
1. The error message or unexpected behavior
2. The endpoint, page, or action that triggers it
3. Any relevant file names or feature area

---

## Step 2 — Locate the bug

Using the scope hint and symptom, search the relevant layers. Follow the call chain from the outside in:

1. **Controller** (`src/TenantCore.Api/Controllers/`) — routing issues, missing `[Authorize]`, wrong HTTP verb/status code
2. **Command/Query** (`src/TenantCore.Application/Features/*/Commands|Queries/`) — wrong property, missing field
3. **Handler** (`src/TenantCore.Application/Features/*/Handlers/`) — business logic errors, missing null checks, wrong repository call
4. **Validator** (`src/TenantCore.Application/Features/*/Validators/`) — missing or wrong validation rules
5. **Translator** (`src/TenantCore.Application/Features/*/Translators/`) — wrong mapping, missing property
6. **Domain Entity** (`src/TenantCore.Domain/Entities/`) — wrong state change, missing method
7. **Repository** (`src/TenantCore.Infrastructure/Repositories/`) — wrong query, missing include, off-by-one pagination
8. **EF Configuration** (`src/TenantCore.Infrastructure/Persistence/Configurations/`) — wrong constraint, missing index
9. **DTOs** (`src/TenantCore.Shared/Dtos/`) — wrong type, missing property
10. **DI Registration** (`src/TenantCore.Infrastructure/DependencyInjection.cs`, `src/TenantCore.Application/DependencyInjection.cs`) — missing or duplicate service registration
11. **Blazor client** (`src/TenantCore.Web.Client/`) — API client errors, wrong endpoint, missing auth header

Read every relevant file. Do not guess — confirm by reading the actual code before forming a hypothesis.

---

## Step 3 — Diagnose

State clearly:
- **Root cause**: the exact line(s) and file(s) where the bug lives, and why it is wrong
- **Impact**: what the bug causes (data corruption, wrong response, crash, silent failure, etc.)
- **Fix**: the precise change needed — no vague descriptions like "add a null check"; specify exactly what to write

If multiple files are involved, list them all.

---

## Step 4 — Apply the fix

Edit only the files that are broken. Follow these rules strictly:

- Controllers: thin, primary constructor, only `sender.Send(...)`. No direct service injection.
- Commands and Queries: `sealed record`. Delete/void: `IRequest` (no type param).
- Handlers: `sealed class`, primary constructor, one per file.
- Translators: `static class`, `static` methods. Never AutoMapper.
- Validators: FluentValidation, auto-registered — do not manually register.
- Entities: static factory, private EF constructor.
- Read DTOs: `class` with `init` setters. Write DTOs: `sealed record`.
- Error handling: throw domain exceptions (`NotFoundException`, `DomainValidationException`, `InvalidOperationException`). Never return error DTOs from handlers.
- Authorization: use `AuthorizationConstants` — never hardcode role strings.

Do not refactor surrounding code. Do not add unrelated improvements. Fix only what is broken.

If the fix requires a new EF Core migration:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

---

## Step 5 — Report

After applying the fix, report:

```
## Bug Fix Applied

**Symptom:** <what the user reported>

**Root cause:** <file(s) and line(s) — what was wrong and why>

**Fix applied:**
- `<file path>` — <one-line description of the change>
- `<file path>` — <one-line description of the change>

**Verify by:**
- <specific action to confirm the bug is gone, e.g. "Call GET /api/tenants/invalid-id and confirm 404 is returned">
- <any side effects to watch for>
```

If you discovered additional issues while investigating (but did not fix them because they are out of scope), list them under **Other issues noticed** so the user can decide whether to address them.
