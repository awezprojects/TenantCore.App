# Security Analysis: Prepaid OPD Fee Collection & Refund Workflow

**Date:** 2026-09-01
**Repo:** TenantCore.App
**Plan:** plan/prepaid-opd-fee-collection/PLAN.md
**ADR reference:** .claude/docs/adr/ADR-010-security.md

## Overall Risk Level

Low — after fixes, the two genuine gaps (an unguarded financial audit-trail hole and a write-on-read endpoint) are closed. The one remaining deviation from ADR-010's default policy (reception, not admin, owns the OPD delete/refund lifecycle) is a deliberate, accepted business decision, not an oversight.

## Findings

### CRITICAL
None

### HIGH
- **[H1]** `DELETE /api/opd-registrations/{id}` authorized at `RequireReception` instead of ADR-010's default `RequireClinicAdmin` for destructive operations — `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` — **Accepted** (see below)
- **[H2]** `DeleteOpdRegistrationHandler` didn't guard against deleting individually-collected `OpdParticular` line items — `src/TenantCore.Application/Features/OpdRegistrations/Handlers/DeleteOpdRegistrationHandler.cs` — **Fixed**

### MEDIUM
- **[M1]** `GetClinicFeatureFlagsHandler` performed a write (create + save) from a GET, inconsistent with the sibling `ClinicFeeConfig` read pattern — `src/TenantCore.Application/Features/ClinicSettings/Handlers/GetClinicFeatureFlagsHandler.cs` — **Fixed**

### LOW / Informational
None

### Code Smells
- **[S6]** Multiple `SaveChangesAsync()` calls within `CreateOpdRegistrationHandler` — pre-existing pattern (registration/payment are separate aggregates created across a MediatR round-trip) extended by one more call for the auto-accept step — **Deferred**, not a correctness risk
- **[S10]** `ClinicFeatureFlagsRepository.GetByApplicationAsync` doesn't call `AsNoTracking()` — mirrors an existing gap in `ClinicFeeConfigRepository`; the same method is also used by the write path, so a real fix needs a separate read-only overload — **Deferred**

### Architectural Violations
None

### Over-Engineering
None

## Accepted Risk — H1

**Decision:** Reception keeps `RequireReception` authorization for OPD registration create, fee collection, refund processing, and delete/cancel.

**Rationale (from the user):** In this clinic's real-time front-desk workflow, reception is solely responsible for booking OPD appointments and collecting fees — admin has no operational role in it. Routing every cancellation (e.g. several patients cancelling same-day appointments) through a Clinic Admin approval step is not feasible for real-time desk operations. The entire OPD lifecycle — registration, fee collection, refund, and cancel/delete — is intentionally handled end-to-end by reception.

**Compensating controls already in place:**
- Delete is only reachable for registrations already in `Cancelled` status (business-rule gate, not just an auth gate).
- Delete is blocked while any money is still outstanding — at both the payment level (`OpdPayment.RefundStatus != Refunded`) and now, after the H2 fix, at the individual-line-item level (`OpdParticular.PaymentStatus == Received`).
- Every write still passes through `GetApplicationId()`/tenant isolation, so no cross-clinic exposure is possible regardless of role.
- The action remains fully attributable — `GetCurrentUserId()` flows into the command chain for creation and refund actions.

This is documented here so the deviation from the ADR-010 default is traceable to an explicit decision rather than rediscovered as a surprise in a future review.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PARTIAL | H1 deviates from the ADR-010 default for destructive ops; accepted as an explicit business decision (see above) |
| Multi-Tenancy Isolation | PASS | `ApplicationId` present on `ClinicFeatureFlags`; carried on every new command/query; every repository read filters by it; controllers use `GetApplicationId()` throughout |
| Input Validation | PASS | `UpdateClinicFeatureFlagsCommandValidator` validates `ApplicationId`; `PrepaidOpdEnabled` is a bool with no further constraint to add |
| Data Access & SQL Injection | PASS | No raw SQL; no `IQueryable<T>` exposed; parameterized EF LINQ throughout |
| Sensitive Data Handling | PASS | No secrets logged; errors bubble through `ExceptionHandlingMiddleware`, no manual `catch` leaking exception text |
| Business Logic Security | PASS | Ownership check (`registration.ApplicationId != request.ApplicationId`) before delete; refund guard now covers both the aggregate payment and individually-collected items; hard delete is intentional per PLAN.md |
| OWASP Top 10 | PASS | A01 tenant/auth checks present on every new endpoint; A03 no injection surface introduced |
| Code Quality | PASS | Remaining S6/S10 notes are low-priority and pre-existing-pattern-adjacent, not newly broken |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| H1 | `src/TenantCore.Api/Controllers/OpdRegistrationsController.cs` | No code change — documented as accepted risk per explicit business decision | Accepted |
| H2 | `src/TenantCore.Application/Features/OpdRegistrations/Handlers/DeleteOpdRegistrationHandler.cs` | Added a guard that blocks delete when any `OpdParticular` has `PaymentStatus == Received`, mirroring the existing payment-level refund guard | Applied |
| M1 | `src/TenantCore.Application/Features/ClinicSettings/Handlers/GetClinicFeatureFlagsHandler.cs`, `Queries/GetClinicFeatureFlagsQuery.cs` | Query/handler now return `ClinicFeatureFlagsDto?` and no longer create a row on read; get-or-create logic stays only in `UpdateClinicFeatureFlagsHandler` (the write path), matching the `ClinicFeeConfig` precedent | Applied |

Test files updated to match: `GetClinicFeatureFlagsHandlerTests.cs` (null-return case replaces the old create-on-read case), `DeleteOpdRegistrationHandlerTests.cs` (added `Handle_IndividuallyCollectedParticularNotRefunded_ThrowsInvalidOperationException`). Full suite re-run: `TenantCore.Application.Tests` 259/260 passing (the 1 failure is `CloseCounterSessionHandlerTests`, pre-existing and unrelated to this feature), `TenantCore.Api.Tests` 237/237 passing.

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining
- [x] Ready to merge
