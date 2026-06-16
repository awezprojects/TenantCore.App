You are performing a security and code quality review of a feature implemented in **TenantCore.App**. $ARGUMENTS is the feature name (matches the folder under `plan/`).

---

## Step 1 — Load the feature context

Read these files in order:

1. `.claude/docs/adr/ADR-010-security.md` — the complete security and quality checklist for this repo
2. `CLAUDE.md` — architectural patterns and absolute constraints

Then check whether a plan exists:

- If `plan/$ARGUMENTS/PLAN.md` exists: read it — it gives you the intended design (endpoints, entity fields, validation rules, business rules) to compare against the implementation
- If it does not exist: continue without it — the code scan is the single source of truth

---

## Step 2 — Locate the implementation files

**If PLAN.md was read:** collect the exact file paths from the "Files to Create" and "Files to Modify" tables.

**If no PLAN.md:** locate the feature by convention:

- Controller: `src/TenantCore.Api/Controllers/<Feature>sController.cs`
- Domain entity: `src/TenantCore.Domain/Entities/<Feature>.cs`
- Repository interface: `src/TenantCore.Domain/Interfaces/I<Feature>Repository.cs`
- EF config: `src/TenantCore.Infrastructure/Persistence/Configurations/<Feature>Configuration.cs`
- Repository impl: `src/TenantCore.Infrastructure/Repositories/<Feature>Repository.cs`
- Feature folder: `src/TenantCore.Application/Features/<Feature>/`

Read each file that exists. Do not skip files — every layer matters.

Also read these shared files that are always modified by a feature:
- `src/TenantCore.Infrastructure/Persistence/ClinicDbContext.cs` — check the new DbSet
- `src/TenantCore.Infrastructure/DependencyInjection.cs` — check DI registration
- Any DTO files under `src/TenantCore.Shared/Dtos/` related to this feature

---

## Step 3 — Run the security checklist

Work through every section of ADR-010 against the actual file contents you read. For each item that fails, record:

- The finding ID (C = critical, H = high, M = medium, L = low / informational)
- File and line reference
- What the code does that is wrong
- What the correct code should be (describe the fix — no code blocks yet)

### Auth & Authorization (ADR-010 Section 1)
- [ ] Every controller class or action has `[Authorize(Policy = ...)]` or `[AllowAnonymous]`
- [ ] Destructive operations use `RequireClinicAdmin` or stricter
- [ ] No endpoint reachable without any auth attribute

### Multi-Tenancy (ADR-010 Section 2)
- [ ] Entity has `public Guid ApplicationId { get; set; }`
- [ ] Commands and queries carry `Guid ApplicationId`
- [ ] Repository queries filter by `applicationId`
- [ ] Controller calls `GetApplicationId()` — not the header directly
- [ ] `ApplicationId` not accepted from `[FromBody]`

### Input Validation (ADR-010 Section 3)
- [ ] All string inputs have `NotEmpty()` + `MaximumLength(N)` in validator
- [ ] ID route parameters typed as `Guid` not `string`
- [ ] Enum inputs validated before cast
- [ ] PageSize capped if applicable

### Data Access & SQL Injection (ADR-010 Section 4)
- [ ] No `ExecuteSqlRaw` with user input
- [ ] No `IQueryable<T>` returned from repositories
- [ ] `AsNoTracking()` on read queries
- [ ] No raw string interpolation in any DB query

### Sensitive Data (ADR-010 Section 5)
- [ ] No passwords or secrets in log statements
- [ ] DTOs have no internal/sensitive fields
- [ ] Errors go through middleware — no manual `catch` returning raw messages
- [ ] CorrelationId forwarded on AuthApi calls

### Business Logic Security (ADR-010 Section 6)
- [ ] Ownership/tenant check before modify or delete
- [ ] Duplicate creation guarded where required
- [ ] Concurrency token (`RowVersion`) present on entity for update operations

### OWASP Quick Scan (ADR-010 Section 7)
- [ ] A01: every data resource checks `applicationId` or auth policy
- [ ] A03: parameterized queries only
- [ ] A07: JWT validation config is correct

---

## Step 4 — Run the code quality scan

Now scan for code smells, architectural violations, and over-engineering using ADR-010 Sections 8–10. Record each issue in the same format.

### Code Smells (ADR-010 Section 8 — S1 through S12)
For each file read, check:
- Business logic in controller action body (S1)
- Non-`ISender` injection in controller constructor (S2)
- AutoMapper in Application layer (S3)
- Non-sealed command, query, or handler (S4)
- `SaveChangesAsync()` mid-handler or called multiple times (S5, S6)
- EF Core referenced in Application or Domain project (S7)
- Data Annotations on Domain entity (S8)
- Non-static Translator class (S9)
- Missing `AsNoTracking()` in read repository method (S10)
- `Guid.NewGuid()` in handler instead of Translator (S11)
- Handler returning `null` instead of throwing exception (S12)

### Architectural Violations (ADR-010 Section 9 — V1 through V7)
- Wrong dependency direction (V1, V2)
- Controller not inheriting `ClinicControllerBase` (V3)
- Middleware pipeline order changed (V4)
- Feature files placed outside `Features/{Area}/` folder (V5)
- `DbContext` exposed via repository property (V6)
- Command or query is a `class` instead of `sealed record` (V7)

### Over-Engineering (ADR-010 Section 10 — O1 through O5)
Flag only genuine problems — see ADR-010 Section 10 for when each pattern is acceptable vs flagged.

---

## Step 5 — Compile and display findings

Group all findings by severity. Print in this format:

```
## Security & Quality Review — <Feature Name>  [TenantCore.App]

### CRITICAL — must fix before merge
<!-- use "None" if no critical findings -->
- **[C1]** <Title>
  File: <path>
  Issue: <what the code does wrong>
  Risk: <what can go wrong if left unfixed>
  Fix: <what to change, described in plain language>

### HIGH — should fix before merge
<!-- use "None" if none -->
- **[H1]** ...

### MEDIUM — fix in follow-up
<!-- use "None" if none -->

### LOW / Informational
<!-- use "None" if none -->

### Code Smells
<!-- smell ID from ADR-010 Section 8 -->
- **[S1]** <Title>
  File: <path>
  Issue: <what violates the pattern>
  Fix: <what to change>

### Architectural Violations
<!-- violation ID from ADR-010 Section 9 -->
- **[V1]** <Title> ...

### Over-Engineering
<!-- over-engineering ID from ADR-010 Section 10 -->
- **[O1]** <Title> ...

---
Summary: N critical, N high, N medium, N low, N smells, N violations, N over-engineering
```

---

## Step 6 — Ask for fix permission

After displaying the findings, stop and print:

```
Which issues should I fix?

Options:
  fix all            — apply every finding above
  fix critical       — apply only CRITICAL findings
  fix security       — apply CRITICAL + HIGH + MEDIUM security findings (no code smells)
  fix quality        — apply code smells and architectural violations only
  fix [C1,H2,S3]    — apply specific findings by ID
  skip               — write SECURITY.md only, no code changes

Reply with your choice.
```

**Wait for the user's reply before proceeding.**

---

## Step 7 — Apply approved fixes

For each approved finding, in order from CRITICAL → HIGH → MEDIUM → code smells → violations:

1. Re-read the file if needed (only if another fix in this session already modified it)
2. Apply a targeted edit — change only the flagged lines
3. Do not reformat, rename, or restructure surrounding code
4. After each edit, report: `Fixed [ID]: <one sentence describing what changed and that behavior is preserved>`
5. If a fix requires changing a method signature, grep for all callers before editing and update them all

Follow ADR-010 Section 11 safe-fix guidelines at all times.

**Do not run migrations.** If a fix changes an entity property, note that a migration may be needed and let the user decide.

---

## Step 8 — Write SECURITY.md

Write or overwrite `plan/$ARGUMENTS/SECURITY.md` using this template:

```markdown
# Security Analysis: <Feature Name>

**Date:** <today's date>
**Repo:** TenantCore.App
**Plan:** plan/$ARGUMENTS/PLAN.md  (or "no plan — code-only scan")
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

<Low / Medium / High> — <one sentence summary>

## Findings

### CRITICAL
<!-- "None" if none -->
- **[C1]** <title> — <file> — <status: Fixed / Deferred / Accepted>

### HIGH
<!-- "None" if none -->

### MEDIUM

### LOW / Informational

### Code Smells
<!-- "None" if none -->

### Architectural Violations

### Over-Engineering

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS / FAIL / PARTIAL | |
| Multi-Tenancy Isolation | PASS / FAIL / PARTIAL | |
| Input Validation | PASS / FAIL / PARTIAL | |
| Data Access & SQL Injection | PASS / FAIL / PARTIAL | |
| Sensitive Data Handling | PASS / FAIL / PARTIAL | |
| Business Logic Security | PASS / FAIL / PARTIAL | |
| OWASP Top 10 | PASS / FAIL / PARTIAL | |
| Code Quality | PASS / FAIL / PARTIAL | |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| C1 | <file> | <description> | Applied / Skipped |

## Approval

- [ ] All CRITICAL findings resolved or accepted with documented risk
- [ ] All HIGH findings resolved or have accepted risk noted
- [ ] No architectural violations remaining
- [ ] Ready to merge
```

---

## Step 9 — Output

Print:

```
Security analysis complete: plan/$ARGUMENTS/SECURITY.md

Risk level: <Low | Medium | High>
Findings: N critical, N high, N medium, N low
Code quality: N smells, N violations, N over-engineering flags
Fixes applied: N  |  Deferred: N

Next: resolve any remaining CRITICAL or HIGH findings before merging.
```
