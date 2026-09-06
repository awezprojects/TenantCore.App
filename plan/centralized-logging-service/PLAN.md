# Feature Plan: Centralized Logging Service

**Repo:** TenantCore.App
**Date:** 2026-09-07
**Domain area:** Cross-cutting / Observability
**Status:** Approved — ready for execution

---

## Overview

Introduces an independent, swappable error-logging capability used across the entire App backend and its Blazor WebAssembly frontend. Today every unhandled API exception and every frontend crash is only surfaced to the user as a sanitized, constructed message — the real exception type, stack trace, and call context are lost. This feature captures that real information and persists it to Azure Table Storage, in two categories (Api, Frontend), so a future admin panel can read recent errors across all clinics. The design deliberately isolates the storage-writing mechanics behind one small interface and one independent project, so switching the backing provider later (Coralogix, Application Insights) means changing configuration and one implementation file — not touching any handler, repository, or controller that calls it. TenantCore.Auth is explicitly out of scope; the only Auth-related coverage is logging failures of App's own outbound calls to the Auth service.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| New project | `TenantCore.Logging` — independent log-writer library (Azure Table Storage today) |
| Domain | New `IErrorLogger` interface — callable from Application and Infrastructure |
| Infrastructure | Adapter implementing `IErrorLogger`, DI registration, wiring into `AuthApplicationService`'s existing catch blocks |
| API | New `LogsController` (frontend error ingestion), `ExceptionHandlingMiddleware` updated to log full detail |
| Shared | New enum (log category) and a request DTO for frontend error submission |
| Web.Client | Typed HTTP client + a root error boundary + JS interop for uncaught JS errors, all forwarding to the API |

No `ClinicDbContext` change, no EF migration — Azure Table Storage is a separate store, not the relational clinic database.

---

## Data Model: Log Entry (stored in Azure Table Storage, not EF)

**Tables:** `ApiErrorLogs`, `FrontendErrorLogs` (configurable table names; both live in the same storage account)

| Field | Type | Notes |
|-------|------|-------|
| PartitionKey | string | Date bucket (yyyy-MM-dd) — keeps writes spread and queries by day cheap |
| RowKey | string | Reverse-chronological sortable id (so latest errors list first) |
| Source | string | Origin — e.g. `Api.Middleware`, `Application.CreatePatientHandler`, `Infrastructure.AuthApplicationService`, `Frontend.<ComponentName or Url>` |
| Message | string | Real internal message (never the user-facing constructed one) |
| ExceptionType | string | Full CLR type name, or `"JsError"` / `"UnhandledPromiseRejection"` for frontend JS-level errors |
| StackTrace | string | Full stack trace text |
| ApplicationId | string (nullable) | Clinic context, when available |
| UserId | string (nullable) | Authenticated user, when available |
| RequestPath | string (nullable) | API route or frontend URL |
| AdditionalContext | string (nullable) | Free-form extra detail (e.g. correlation id, user agent) |
| Environment | string | Development/Staging/Production |
| TimestampUtc | DateTime | Set at write time |

---

## Files to Create

### New Project: `TenantCore.Logging` (independent — no reference to Domain/Application/Infrastructure)

| File | Purpose |
|------|---------|
| `TenantCore.Logging.csproj` | New class library, targets net8.0, references only `Azure.Data.Tables` + `Microsoft.Extensions.Options`/DI abstractions |
| `LogEntry.cs` | Plain model matching the table schema above — the only shape this project knows about |
| `IAppLogWriter.cs` | Low-level contract: write one `LogEntry` to a named table |
| `AzureTableLogWriter.cs` | Current implementation — writes to Azure Table Storage via `Azure.Data.Tables`; creates the table if missing |
| `AppLoggingOptions.cs` | Bound from config: connection string, table names for Api/Frontend, provider selector (reserved for future Coralogix/AppInsights switch) |
| `LoggingServiceCollectionExtensions.cs` | `AddAppLogging(IConfiguration)` — binds options, registers `IAppLogWriter` (this is the single place a future provider swap happens) |

### Shared Layer (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/LogCategory.cs` | `Api`, `Frontend` — selects which table an entry goes to |
| `Dtos/FrontendErrorLogRequest.cs` | POST body from the Blazor client: message, exception type, stack, component/url, user agent, additional context |

### Domain Layer (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Interfaces/IErrorLogger.cs` | The contract Application and Infrastructure code depend on — `LogAsync(LogCategory, source, exception, message, applicationId?, userId?, additionalContext?)`. This is what makes the capability reachable from BAL commands/handlers and from repositories, not just the middleware |

### Infrastructure Layer (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Services/ErrorLoggingService.cs` | Implements `IErrorLogger` — builds a `LogEntry`, resolves the target table from `LogCategory`, delegates to `IAppLogWriter` from `TenantCore.Logging`. This is the adapter boundary: Domain/Application never see `TenantCore.Logging` types directly |

### API Layer (`src/TenantCore.Api/`)

| File | Purpose |
|------|---------|
| `Controllers/LogsController.cs` | `POST api/logs/frontend-error` — accepts `FrontendErrorLogRequest`, calls `IErrorLogger.LogAsync(LogCategory.Frontend, ...)`, returns 204. Anonymous-allowed (see Business Rules) |

### Web.Client Layer (`src/TenantCore.Web.Client/`)

| File | Purpose |
|------|---------|
| `Clients/ILoggingApiClient.cs` | Typed client contract — one method, `LogErrorAsync(FrontendErrorLogRequest)` |
| `Clients/LoggingApiClient.cs` | Implementation — POSTs to `api/logs/frontend-error`, swallows its own failures (never let logging itself crash the app or surface an error to the user) |
| `Components/AppErrorBoundary.razor` | Wraps the root render tree; on an unhandled render/lifecycle exception, calls `ILoggingApiClient` with the real exception, then shows the existing generic error UI |
| `wwwroot/js/errorLogging.js` | Hooks `window.onerror` and `window.onunhandledrejection`; forwards message + stack to a JS-invokable .NET method that calls `ILoggingApiClient` |

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Api/Middleware/ExceptionHandlingMiddleware.cs` | Inject `IErrorLogger`; before building the `ProblemDetails` response, call `LogAsync(LogCategory.Api, "Api.Middleware", exception, exception.Message, applicationId: from context if resolvable, additionalContext: correlationId)` |
| `src/TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs` | Inject `IErrorLogger`; log its existing catch blocks with `LogCategory.Api`, source `"Infrastructure.AuthApplicationService"` — captures failures of App's outbound calls to the Auth service without touching the Auth repo |
| `src/TenantCore.Infrastructure/DependencyInjection.cs` | Call `services.AddAppLogging(configuration)`; register `services.AddScoped<IErrorLogger, ErrorLoggingService>()` |
| `src/TenantCore.Api/TenantCore.Api.csproj` | Add project reference to `TenantCore.Logging`; add `Azure.Data.Tables` package reference |
| `src/TenantCore.Api/appsettings.json` | Add `AppLogging` section: `ConnectionString` (empty, dev-only per your note), `ApiErrorTable` (`ApiErrorLogs`), `FrontendErrorTable` (`FrontendErrorLogs`) |
| `src/TenantCore.Web.Client/Program.cs` | Register `IErrorLogger`-equivalent typed client (`AddHttpClient<ILoggingApiClient, LoggingApiClient>`) pointed at `tenantApiBaseUrl`; register JS interop init |
| `src/TenantCore.Web.Client/App.razor` (or root layout) | Wrap root content in `AppErrorBoundary` |
| `TenantCore.App.sln` | Add the new `TenantCore.Logging` project |

---

## API Endpoints

| Method | Route | Request Body | Response | Auth Policy |
|--------|-------|-------------|----------|-------------|
| POST | `api/logs/frontend-error` | `FrontendErrorLogRequest` | 204 No Content | Anonymous (see Business Rules — captures pre-login crashes too) |

---

## Validation Rules

| Field | Rules |
|-------|-------|
| `FrontendErrorLogRequest.Message` | NotEmpty, MaxLength(2000) |
| `FrontendErrorLogRequest.Source` (component/url) | NotEmpty, MaxLength(500) |
| `FrontendErrorLogRequest.StackTrace` | MaxLength(8000), optional |

---

## Business Rules

1. `LogsController`'s endpoint is anonymous-allowed so that crashes on the login/pre-auth screens are still captured — it never returns anything other than 204/400 and never echoes user data back.
2. Logging failures must never surface to the end user or block the original request/render — `ErrorLoggingService` and `LoggingApiClient` both swallow their own internal exceptions after a best-effort attempt (write-and-forget semantics; no retries, no queueing, in this iteration).
3. The user-facing message pipeline (`UserMessages.*`, `ProblemDetails.Detail`) is unchanged — this feature only adds a parallel write of the *real* exception detail; it never changes what the user sees.
4. `IErrorLogger` is available to any Application handler or Infrastructure service going forward (inject it like any other dependency); this plan wires it into the two concrete points that need it today (global middleware, Auth outbound calls) rather than retrofitting every existing handler.
5. Provider swap (e.g. to Coralogix) happens entirely inside `TenantCore.Logging` (new `IAppLogWriter` implementation + `AppLoggingOptions.Provider` switch in `LoggingServiceCollectionExtensions`) — no other project changes.

---

## Multi-Tenancy Checklist

- [x] `ApplicationId` captured on `LogEntry` when resolvable (nullable — not every error has clinic context, e.g. pre-login)
- [x] `IErrorLogger.LogAsync` accepts `applicationId` as an optional parameter, passed through by callers that have it
- [ ] Not applicable: the log store itself is intentionally global/cross-tenant, not filtered per clinic (by design, for a future cross-tenant admin view)

---

## EF Migration

None — Azure Table Storage, not SQL Server. No `dotnet ef migrations add` step for this feature.

---

## Implementation Order

1. `TenantCore.Logging` project — model, `IAppLogWriter`, `AzureTableLogWriter`, `AppLoggingOptions`, `LoggingServiceCollectionExtensions`; add to solution
2. Shared — `LogCategory` enum, `FrontendErrorLogRequest` DTO
3. Domain — `IErrorLogger` interface
4. Infrastructure — `ErrorLoggingService` implementing `IErrorLogger`
5. Infrastructure `DependencyInjection.cs` — `AddAppLogging` + `IErrorLogger` registration
6. `TenantCore.Api.csproj` — project reference + `Azure.Data.Tables` package; `appsettings.json` — `AppLogging` section
7. `ExceptionHandlingMiddleware.cs` — inject and call `IErrorLogger`
8. `AuthApplicationService.cs` — inject and call `IErrorLogger` in existing catch blocks
9. `Api/Controllers/LogsController.cs` — frontend ingestion endpoint
10. Application validator for `FrontendErrorLogRequest` (via the controller's request pipeline — note: since this bypasses MediatR, validate directly in the controller or via a lightweight FluentValidation call, per existing validator conventions)
11. Web.Client — `ILoggingApiClient`/`LoggingApiClient`, register in `Program.cs`
12. Web.Client — `AppErrorBoundary.razor`, `wwwroot/js/errorLogging.js`, wire into root layout
13. Unit tests — `ErrorLoggingService`, `LogsController` (or its validator), `AzureTableLogWriter` if practically testable (otherwise cover via the service layer with a mocked `IAppLogWriter`)

---

## Test Files to Create

All test files live under `tests/TenantCore.Application.Tests/` (or a new `tests/TenantCore.Infrastructure.Tests/` if one doesn't already exist for Infrastructure-level services — check before creating).

| File | What it covers |
|------|---------------|
| `ErrorLoggingServiceTests.cs` | Correct table/category routing (Api vs Frontend), all fields mapped onto `LogEntry`, `applicationId`/`userId` optional and correctly nulled when absent, swallow-and-continue behavior when the underlying writer throws |
| `FrontendErrorLogRequestValidatorTests.cs` | Required fields, `MaxLength` boundaries on `Message`/`Source`/`StackTrace` |
| `LogsControllerTests.cs` (or handler-equivalent) | Valid request returns 204 and calls `IErrorLogger` once with `LogCategory.Frontend`; invalid request returns 400 without calling the logger |

---

## Open Questions / Risks

- `TenantCore.Logging`'s `AzureTableLogWriter` itself is not unit-tested against real Table Storage in this plan (would need an emulator/integration test setup) — covered indirectly via `ErrorLoggingService` tests against a mocked `IAppLogWriter`.
- No retry/queueing on write failure in this iteration (per Business Rule 2) — acceptable for an initial setup per your instructions; revisit if log loss during storage outages becomes a concern.
- Frontend error volume is unbounded (anonymous endpoint) — no rate limiting in this iteration; flag if abuse becomes a concern before the admin panel ships.
