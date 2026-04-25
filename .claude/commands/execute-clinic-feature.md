Execute a feature plan from the `plan/` folder. Argument received: $ARGUMENTS

---

## Behavior — No Argument (Listing Mode)

If `$ARGUMENTS` is empty or blank:

1. Use Glob to find all files matching `plan/*/PLAN.md`.
2. Extract the folder name for each (e.g. `plan/endpoint-integration-hardening/PLAN.md` → `endpoint-integration-hardening`).
3. Sort the folder names **alphabetically** (A–Z, case-insensitive).
4. For each plan, read its `## Execution Status` section and extract the `**Status**:` value. If no status section exists, treat it as `Not started`.
5. Assign letters starting from A. Display:

```
Available feature plans:

  A. endpoint-integration-hardening        — Not started
  B. user-roles-management                 — Development done
  C. another-feature                       — Plan fully executed and completed

Which plan do you want to execute? Enter a letter (A, B, C...):
```

6. Stop here and wait for the user to reply with a letter.

---

## Behavior — Letter Argument (Execution Mode)

If `$ARGUMENTS` is a single letter (case-insensitive, A–Z):

1. Glob `plan/*/PLAN.md`, sort folder names alphabetically, map A→1st, B→2nd, C→3rd, etc.
2. If the letter is out of range, respond: "No plan found for letter **X**. Run `/execute-clinic-feature` without arguments to see available plans." Stop.
3. Read the plan's `## Execution Status` section.
4. If `**Status**: Plan fully executed and completed` → respond:
   > "**Plan already completed.** The `<plan-name>` plan has been fully executed and marked complete. No further action needed."
   Stop.
5. Otherwise proceed through the four phases below.

---

## Phase 1 — Mark In Progress

Edit `plan/<slug>/PLAN.md`: find or append the `## Execution Status` section and set it to:

```markdown
## Execution Status

- **Status**: Plan execution in progress
- **Started**: <today's date YYYY-MM-DD>
```

If the section already exists with older values, replace them. The status line must always be the single source of truth.

---

## Phase 2 — Development

Read the full PLAN.md. Execute every item listed in **Files to Create** and **Files to Modify**, in the exact **Implementation Order** specified in the plan.

Rules to follow during implementation:
- Controllers: thin, primary constructor, only `sender.Send(...)`. No direct service injection.
- Commands and Queries: `sealed record` implementing `IRequest<T>`. Delete/void: `IRequest` (no type param).
- Handlers: `sealed class`, primary constructor, one handler per file, named to match the command/query.
- Translators: `static class` with `static` methods. Never use AutoMapper.
- Validators: FluentValidation, auto-registered via MediatR pipeline — do not manually register.
- Entities: static factory, private constructor for EF Core.
- Read DTOs: `class` with `init` setters. Write DTOs: `sealed record`.
- Error handling: throw domain exceptions (`NotFoundException`, `DomainValidationException`, `InvalidOperationException`). Never return error DTOs from handlers.
- Authorization: use constants from `TenantCore.Shared.Authorization.AuthorizationConstants` — never hardcode role strings.
- New repositories: register in `src/TenantCore.Infrastructure/DependencyInjection.cs`.

If the plan includes a **Migration Name** (not "None"):
```bash
dotnet ef migrations add <MigrationName> \
  --project src/TenantCore.Infrastructure \
  --startup-project src/TenantCore.Api
```

After all code changes are complete, update `## Execution Status` in PLAN.md:

```markdown
## Execution Status

- **Status**: Development done
- **Started**: <original start date>
- **Development completed**: <today's date YYYY-MM-DD>
```

---

## Phase 3 — Security Check

Review every file that was created or modified during Phase 2. Produce a security report and write it to `plan/<slug>/SECURITY.md`.

### What to check

| Category | Questions |
|----------|-----------|
| Authentication & Authorization | Are all controller endpoints decorated with `[Authorize]`? Are destructive endpoints (POST, PUT, DELETE) protected by `RequireManagement` or `RequireAdmin` policies? |
| Input Validation | Are all inputs validated via FluentValidation? Is there a max page-size cap (≤100) on every paginated endpoint? |
| Data Access & Injection | Are all DB queries via EF Core (parameterized)? No raw SQL? No string-concatenated queries? |
| Sensitive Data Handling | Are secrets, tokens, or PII handled safely? Not logged? Not returned in response bodies unnecessarily? |
| Business Logic Security | Are ownership and tenant-isolation checks present where required? Can a lower-privilege user affect resources belonging to another user? |
| OWASP Top 10 | A01 Broken Access Control, A03 Injection, A07 Identification & Authentication Failures |

### SECURITY.md format

```markdown
# Security Analysis: <Feature Name>

**Date**: <today's date>
**Analyst**: Claude
**Plan**: plan/<slug>/PLAN.md

## Summary

Overall risk level: **<Critical | High | Medium | Low>**. <2–3 sentence summary of the security posture.>

## Findings

### CRITICAL (must fix before merge)

- **[C1]** <Title>
  - **Location**: `<file>` — `<method or line>`
  - **Risk**: <what an attacker can do>
  - **Fix**: <exact fix>

_(Write "None." if no critical findings.)_

### HIGH (should fix before merge)

- **[H1]** <Title>
  - **Location**: ...
  - **Risk**: ...
  - **Fix**: ...

_(Write "None." if no high findings.)_

### MEDIUM (fix in follow-up)

_(List or write "None.")_

### LOW / Informational

_(List or write "None.")_

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS / PARTIAL / FAIL | |
| Input Validation | PASS / PARTIAL / FAIL | |
| Data Access & Injection | PASS / PARTIAL / FAIL | |
| Sensitive Data Handling | PASS / PARTIAL / FAIL | |
| Business Logic Security | PASS / PARTIAL / FAIL | |
| OWASP Top 10 | PASS / PARTIAL / FAIL | |

## Recommended Actions

1. **(CRITICAL — before merge)** ...
2. **(HIGH — before merge)** ...
3. **(MEDIUM — follow-up)** ...

## Approval

- [ ] All CRITICAL findings resolved
- [ ] All HIGH findings resolved or have accepted risk noted
- [ ] Ready to merge
```

After writing SECURITY.md, update `## Execution Status` in PLAN.md:

```markdown
## Execution Status

- **Status**: Security check done
- **Started**: <original start date>
- **Development completed**: <development date>
- **Security check completed**: <today's date YYYY-MM-DD>
```

---

## Phase 4 — Completion

Update `## Execution Status` in PLAN.md to the final state:

```markdown
## Execution Status

- **Status**: Plan fully executed and completed
- **Started**: <original start date>
- **Development completed**: <development date>
- **Security check completed**: <security date>
- **Completed**: <today's date YYYY-MM-DD>
```

Then report a summary to the user:

```
## Execution Complete: <plan-name>

**What was implemented:**
- <bullet list of files created/modified>

**Security posture:** <overall risk level>

**Outstanding security items (if any):**
- [H1] <title> — <one-line fix>
- [H2] ...

**Next steps:**
- <Any HIGH findings to resolve before merge>
- <Any MEDIUM follow-ups>
- Run the application and verify <key user-facing behaviour>
```
