# Security Analysis: Centralized Logging Service

**Date:** 2026-09-07
**Repo:** TenantCore.App
**Plan:** plan/centralized-logging-service/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

**Low (post-fix).** Two CRITICAL defects would have made the feature non-functional or crashed the app outright (verified empirically by starting the app), and one HIGH finding allowed log-forgery via the anonymous ingestion endpoint. All were fixed and re-verified against a running instance. Remaining items are informational and pre-existing/adjacent, not blockers.

## Findings

### CRITICAL
- **[C1]** `IErrorLogger` (Scoped) injected into `ExceptionHandlingMiddleware`'s constructor — app fails to start entirely. — `src/TenantCore.Api/Middleware/ExceptionHandlingMiddleware.cs` — **Fixed**
- **[C2]** *(discovered during fix verification, not in the original scan)* `AzureTableLogWriter` threw eagerly in its constructor when `AppLogging:ConnectionString` was empty. Because C1's fix makes `IErrorLogger` a method parameter, ASP.NET Core resolves it on **every request**, not just on the exception path — so this took down every single API call (not just logging) whenever the connection string wasn't configured, which is the exact starting state this feature ships in. — `src/TenantCore.Logging/AzureTableLogWriter.cs` — **Fixed**

### HIGH
- **[H1]** Client-supplied `ApplicationId` in the anonymous frontend-error POST body took precedence over the trusted, server-resolved `GetApplicationId()` — allowed log forgery/misattribution across tenants. — `src/TenantCore.Api/Controllers/LogsController.cs` — **Fixed**

### MEDIUM
- **[M1]** `ExceptionType` and `AdditionalContext` had no `MaximumLength` validation, unlike the other string fields on the same anonymous endpoint. — `src/TenantCore.Application/Features/Logs/Validators/LogFrontendErrorCommandValidator.cs` — **Fixed**
- **[M2]** `CorrelationId` not forwarded on outbound Auth API calls (pre-existing gap, not introduced by this feature) — undermines cross-referencing the new `AuthApplicationService` error logs with TenantCore.Auth's own logs for the same request. — `src/TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs` — **Fixed**

### LOW / Informational
- **[L1]** Anonymous `LogsController` endpoint has no rate limiting — already an accepted risk noted in PLAN.md's Open Questions. — Accepted
- **[L2]** `ApiErrorLogs`/`FrontendErrorLogs` will store full, unredacted stack traces/messages by design (the feature's stated purpose) — access to Table Storage and any future admin read endpoint must be scoped tightly. — Accepted
- **[L3]** `LogFrontendErrorCommand` carries `ApplicationId` nested in `Request`/`ContextApplicationId` rather than as a single top-level `ApplicationId` property like other commands — deliberate, since this data is intentionally cross-tenant/optional. — Accepted

### Code Smells
- **[S1]** `ApplicationId` precedence logic lived in the controller action body instead of the handler. — `src/TenantCore.Api/Controllers/LogsController.cs` — **Fixed** (same edit as H1 — precedence now resolved in `LogFrontendErrorHandler`)

### Architectural Violations
None.

### Over-Engineering
None — the two `catch (Exception)` blocks (`ErrorLoggingService.LogAsync`, `LoggingApiClient.LogErrorAsync`) are deliberate "must never throw" boundaries required by the plan's Business Rule 2, not smell O4.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | `[AllowAnonymous]` present with an inline justifying comment; intentional design |
| Multi-Tenancy Isolation | PASS (post-fix) | Trusted `GetApplicationId()` now takes precedence over body-supplied value (H1 fixed); no ClinicDbContext entity involved |
| Input Validation | PASS (post-fix) | All `FrontendErrorLogRequest` string fields now have `MaximumLength` rules (M1 fixed) |
| Data Access & SQL Injection | PASS | No SQL/EF in this feature — Azure Table Storage only, no user input used in partition/row keys |
| Sensitive Data Handling | PARTIAL | CorrelationId now forwarded (M2 fixed); L2 (unredacted stack traces by design) remains an accepted, inherent risk |
| Business Logic Security | PASS | N/A — no create/update/delete of tenant data in this feature |
| OWASP Top 10 | PASS (post-fix) | A01 addressed by H1 fix; no injection surface; JWT config untouched |
| Code Quality | PASS (post-fix) | S1 fixed; no architectural violations or over-engineering found |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| C1 | src/TenantCore.Api/Middleware/ExceptionHandlingMiddleware.cs | Moved `IErrorLogger` from constructor to `InvokeAsync` method parameter so it resolves from the per-request scope instead of the root provider | Applied |
| C2 | src/TenantCore.Logging/AzureTableLogWriter.cs | Made `TableServiceClient` construction lazy (inside `WriteAsync`) instead of throwing in the constructor, so DI activation never fails and the existing `ErrorLoggingService` catch handles the missing-connection-string case as intended | Applied |
| H1 / S1 | src/TenantCore.Api/Controllers/LogsController.cs, src/TenantCore.Application/Features/Logs/Commands/LogFrontendErrorCommand.cs, src/TenantCore.Application/Features/Logs/Handlers/LogFrontendErrorHandler.cs | Added `ContextApplicationId` to the command; controller now passes the raw header-derived value instead of deciding precedence itself; handler resolves precedence with the trusted value winning | Applied |
| M1 | src/TenantCore.Application/Features/Logs/Validators/LogFrontendErrorCommandValidator.cs | Added `MaximumLength` rules for `ExceptionType` (500) and `AdditionalContext` (2000) | Applied |
| M2 | src/TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs | `CreateClient()` now forwards `HttpContext.TraceIdentifier` as `X-Correlation-Id` on outbound Auth API calls | Applied |

Tests updated/added: `LogFrontendErrorHandlerTests` (2 new precedence tests), `LogFrontendErrorCommandValidatorTests` (2 new MaxLength tests) — 17/17 passing. App startup and both fixed endpoints (`/swagger`, `/api/logs/frontend-error`, including a simulated `ApplicationId`-spoofing request) verified live against a running instance.

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
