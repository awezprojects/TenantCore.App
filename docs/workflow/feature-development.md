# Feature Development Workflow — TenantCore.App

## Overview

All new features follow a **plan-first, execute-second** workflow using Claude slash commands. The plan lives in `plan/<feature-name>/` at the repo root and is the single source of truth for implementation.

```
/plan-feature  →  plan/<name>/PLAN.md + ADR.md
/execute-feature  →  implement all files from PLAN.md
/feature-security-analysis  →  plan/<name>/SECURITY.md
```

---

## Step-by-step

### 1. Plan the feature

```
/plan-feature <feature-name> - <description>
```

**Example:**
```
/plan-feature application-settings - allow tenants to store key-value configuration settings per application
```

Claude will:
- Read `CLAUDE.md` (one file, no full scan)
- Create `plan/application-settings/PLAN.md` — full blueprint with file list, signatures, endpoints, validation, business rules, implementation order
- Create `plan/application-settings/ADR.md` — architectural decisions for this feature

**Review the plan before proceeding.** Edit `PLAN.md` if any requirements are wrong.

---

### 2. Run security analysis (recommended before execution)

```
/feature-security-analysis <feature-name>
```

Claude will read the plan and output `plan/<feature-name>/SECURITY.md` with:
- OWASP Top 10 checklist
- Auth and authorization gaps
- Input validation gaps
- Data exposure risks
- Prioritized findings (CRITICAL / HIGH / MEDIUM / LOW)

Fix any CRITICAL findings in the plan before executing.

---

### 3. Execute the feature

```
/execute-feature <feature-name>
```

Claude will:
- Read `plan/<feature-name>/PLAN.md` only (no codebase scan)
- Implement all files in the correct order
- Modify `AppDbContext` and `DependencyInjection.cs` as listed in the plan
- Print a completion table and the migration command to run

---

### 4. Run the migration

After reviewing the generated code:

```bash
dotnet ef migrations add Add<EntityName> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

Then apply:
```bash
dotnet ef database update \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

---

## Plan folder structure

```
plan/
  <feature-name>/
    PLAN.md       ← implementation blueprint (created by /plan-feature)
    ADR.md        ← architectural decisions (created by /plan-feature)
    SECURITY.md   ← security review (created by /feature-security-analysis)
```

Plan folders are committed to the repo alongside the feature branch — they serve as the feature spec and review artifact.

---

## Architecture quick reference

Pattern: **Clean Architecture + CQRS (MediatR)**

| Layer | Project | Key Types |
|-------|---------|-----------|
| API | `TenantCore.Api` | `*Controller` — thin, only `sender.Send(...)` |
| Application | `TenantCore.Application` | `*Command`, `*Query` (sealed records), `*Handler` (sealed class), `*Validator`, `*Translator` (static) |
| Domain | `TenantCore.Domain` | Entities (static factory), `I*Repository` interfaces, domain exceptions |
| Infrastructure | `TenantCore.Infrastructure` | `*Repository`, EF `*Configuration`, `AuthApplicationService` |
| Shared | `TenantCore.Shared` | `*Dto`, `Create*Dto`, `Update*Dto`, `AuthorizationConstants` |

**Error handling**: throw `NotFoundException`, `InvalidOperationException`, `DomainValidationException` — middleware converts to `ProblemDetails` automatically. Never return error objects from handlers.

**Auth**: JWT Bearer from TenantCore.Auth. Policies in `AuthorizationConstants`. Apply at controller action level.

---

## ADR workflow

When a feature introduces a new architectural decision (new pattern, new library, new design choice):

1. Copy `docs/adr/ADR-template.md`
2. Name it `ADR-NNN-<short-title>.md` (next sequential number)
3. Fill all sections
4. Link it in the feature's `plan/<name>/ADR.md`

Existing ADRs: [ADR-001](../adr/ADR-001-clean-architecture.md) | [ADR-002](../adr/ADR-002-cqrs-mediatr.md)
