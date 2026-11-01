# ADR-011: Observability & Logging (mandatory for every feature)

**Status:** Accepted, 2026-09-29
**Applies to:** TenantCore.App. The same contract applies in TenantCore.Auth (its own ADR-011) and TenantCore.Admin (CLAUDE.md → Logging).
**Read with:** ADR-003 (pipeline behaviours), ADR-005 (middleware order), ADR-010 (sensitive data).

## Decision

**Logging is on by default.** It is not something a feature opts into. Every command, API request, outbound call, webhook, background job and unhandled error produces a row in Azure Table Storage, in the **same storage account and row schema** as TenantCore.Auth and TenantCore.Admin. The Admin **Log Explorer** (`/logs`) reads all of them.

A new feature is **not done** until its logs appear in the Log Explorer.

## What is captured, and by what

| Table | Written by | When |
|---|---|---|
| `ActionLogs` | `ActionLoggingBehavior` (MediatR) | **Every request whose type name ends in `Command`**, plus anything implementing `IBusinessAction`. Writes a Started row, then a Completed or Failed row, tied by a correlation id. |
| `ActionLogs` (Category `Outbound`) | `OutboundCallLoggingHandler`, attached to **every** `IHttpClientFactory` client via `ConfigureHttpClientDefaults` | Every outbound HTTP call (Razorpay, TenantCore.Auth, any future client): host, method, path without the query string, status received, duration. A status ≥ 400 or an exception is recorded as Failed. |
| `ActionLogs` | `WorkflowTaskProcessor`, `PaymentReconciliationService` | Every workflow task (`Workflow:{TaskType}`). Reconciliation and self-heal sweeps are logged only when they did work, with their counters. |
| `ApiRequestLogs` | `ApiRequestLoggingMiddleware` | Every `/api` request that **failed** (≥ 400, including handled 4xx), was **slow** (≥ `SlowRequestMs`), or **changed data** (POST/PUT/PATCH/DELETE). Records method, route template, actual status, duration, tenant, user and correlation id. |
| `ApiErrorLogs` | `ExceptionHandlingMiddleware` → `IErrorLogger` | Every unhandled API exception, with the actual status returned, method, path and correlation id. |
| `ApiErrorLogs` (Category `Server`) | `TableStorageErrorSink` (Serilog, picked up via `ReadFrom.Services`) | Every `logger.LogError` or higher (configurable) outside the middleware: background jobs, hosted services, EF and startup. |
| `FrontendErrorLogs` | `POST api/logs/frontend` | Blazor `AppErrorBoundary` crashes. The content is client-supplied and untrusted. |

Writes go through `QueuedAppLogWriter`: the caller enqueues and returns, and a background service writes to Table Storage. Logging never adds a storage round-trip to a request. It never throws, and it drops entries when the queue is full.

## Rules for new work

1. **New command.** Name it `…Command`. That alone makes it logged. Implement `IBusinessAction` only to give it a friendlier name. When the operation has identifiers worth searching (payment id, provider event id, gateway link id), implement `IActionLogContext` and return `k=v; k=v` pairs.
2. **Opting out** (`ISkipActionLog`) needs a comment explaining why, and a reviewer's agreement. It exists only for commands that are themselves log writes. It is never used to hide a business operation. A test (`RealCommands_AreLoggedByDefault…`) lists the permitted opt-outs.
3. **New endpoint.** Nothing to do: request and error logging cover it. Confirm in the plan that its failures surface as a status code or an exception, not as an HTTP 200 carrying an error body.
4. **New external integration.** Register its `HttpClient` through `IHttpClientFactory`. `ConfigureHttpClientDefaults` then logs it. Never `new HttpClient()`.
5. **New webhook receiver.** Send it through a `…Command` implementing `IActionLogContext` (provider and event id). Verify the signature inside the command, so a bad signature becomes a Failed row. Never log the raw body; the event row in the database is the payload record.
6. **New background job / hosted service.** Log each run that did work through `IActionLogger` (`Job: <Name>` with counters in the context). Log failures with `logger.LogError(ex, …)`, and the sink copies them to `ApiErrorLogs`.
7. **Never log:** request or response bodies, clinical or personal data (patient names, phone numbers, diagnoses), passwords, OTPs, tokens, API keys, webhook signatures or query strings. Context carries **identifiers and outcomes only**. `LogScrubber` masks accidental secrets as a second line of defence; it is not the plan.
8. **Tests.** A feature's tests assert the log contract where the feature adds one: a new `IActionLogContext`, a job's run row, or an opt-out.
9. **Schema.** `LogEntry` is shared with Auth and Admin. Add a column in all three and in the Admin explorer's parser. Never rename or retype one.

## Configuration (`AppLogging`)

| Key | Default | Meaning |
|---|---|---|
| `ConnectionString` | "" | The shared log storage account. Empty = table logging off. The real value lives in `appsettings.Development.json` / `appsettings.Local.json` / Key Vault. |
| `ApiErrorTable` / `FrontendErrorTable` / `ActionLogTable` / `RequestLogTable` | `ApiErrorLogs` / `FrontendErrorLogs` / `ActionLogs` / `ApiRequestLogs` | Table names; the Admin explorer's source list uses the same names. |
| `MinimumLevel` | `Error` | Lowest ILogger level copied to `ApiErrorLogs` by the sink. |
| `SlowRequestMs` | 2000 | A request at or above this duration is logged even when it succeeded. |
| `LogMutatingRequests` | true | Log every successful POST/PUT/PATCH/DELETE. |
| `QueueCapacity` | 5000 | In-memory buffer before entries are dropped. |

## Consequences

- Volume grows with usage: roughly one action pair per command and one request row per data change. Table Storage is cheap per row, and PartitionKey = UTC date keeps retention simple: delete whole old day partitions. A retention job is a follow-up.
- Correlation: request and error rows carry the HTTP correlation id (`X-Correlation-Id`, which Admin also sends). Action rows carry their own pair id.
