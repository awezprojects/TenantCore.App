# Security Analysis: Subscription Billing Activation

**Date:** 2026-10-03
**Repo:** TenantCore.App
**Plan:** plan/subscription-billing-activation/PLAN.md
**ADR reference:** docs/adr/ADR-010-security.md  (condensed checklist: `.clinerules` §6)

## Overall Risk Level

Low — all new endpoints are internal-service-key–guarded, no new public surface area, no schema changes, no new tenant-isolation boundary.

## Findings

### CRITICAL
None

### HIGH
None

### MEDIUM

- **[M1]** `ApproveClinicOnboardingCommand` missing `IActionLogContext` — `src/TenantCore.Application/Features/Onboarding/Commands/ApproveClinicOnboardingCommand.cs` — **Deferred**
  The command implements `IBusinessAction` but not `IActionLogContext`. PLAN.md logging section requires `OnboardingRequestId`, `ApplicationId`, and `BillingMode` as searchable identifiers in the `ActionLogs` table. Without it the action log entry carries only the action name; operators cannot filter Log Explorer for billing-mode–specific approvals.
  Fix: Implement `IActionLogContext` with `ActionLogContext => $"requestId={Id}; billingMode={BillingModeOverride ?? BillingMode.Subscription}; adminUserId={AdminUserId}"`.

### LOW / Informational

- **[L1]** `BillingModeOverride` not validated with `Enum.IsDefined` — `src/TenantCore.Application/Features/Onboarding/Validators/ApproveClinicOnboardingCommandValidator.cs`:22 — **Deferred**
  The validator branches on the known enum values but does not reject unknown numeric inputs (e.g. `3`). An unrecognised value would flow into `StoreBillingIntent` and ultimately into `SetClinicBillingCommand`. Exploitability is negligible on this X-Internal-Service-Key–protected endpoint, but the ADR requires enum validation by default.
  Fix: Add `RuleFor(x => x.BillingModeOverride).Must(v => v == null || Enum.IsDefined(typeof(BillingMode), v)).WithMessage("Unknown billing mode.").When(x => x.BillingModeOverride.HasValue)`.

### Code Smells
None

### Architectural Violations

- **[V3-preexisting]** `InternalOnboardingController` and `InternalPlansController` inherit `ControllerBase` not `ClinicControllerBase` — pre-existing, **Accepted by design**
  Internal-service controllers are guarded by `InternalServiceKeyAuthenticationHandler` (not JWT/clinic-context). `ClinicControllerBase.GetApplicationId()` is irrelevant for platform-admin routes. This is the established pattern for all `Internal/` controllers; the new `Delete` endpoint on `InternalPlansController` follows the same convention correctly.

### Over-Engineering
None

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All new endpoints behind `InternalServiceKeyAuthenticationHandler`. Existing clinic routes unchanged. |
| Multi-Tenancy Isolation | PASS | Internal approve/delete routes are platform-wide, not clinic-scoped. `ActivateSubscriptionTaskHandler` reads `ProvisionedApplicationId` from the entity, not from any request field. |
| Input Validation | PASS (with L1 deferred) | All new fields have length/range validators. PAYG rates ≥ 0, ≤ 4 dp. FreeTestDays 30–90. `BillingMode` enum deferred. |
| Data Access & SQL Injection | PASS | EF Core LINQ only. `AsNoTracking()` present on read paths. No `ExecuteSqlRaw`. |
| Sensitive Data Handling | PASS | `AdminEmail` logged in `DeleteSubscriptionPlanHandler` is an admin audit field, not personal/clinical data. No passwords, tokens, or patient data logged. |
| Business Logic Security | PASS | Plan delete is blocked when active/upcoming subscriptions exist. Billing intent is applied after provisioning; wallet credit uses `OnboardingRequestId` as idempotency key. |
| OWASP Top 10 | PASS | A01: all data behind auth. A03: parameterised queries. A07: no JWT config changes. |
| Code Quality | PASS | All new commands are `sealed record`. Handlers are `sealed class`. No AutoMapper. Single `SaveChangesAsync` per handler path. No EF Core in Application layer. |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| M1 | `ApproveClinicOnboardingCommand.cs` | Add `IActionLogContext` with billing-mode context | Deferred |
| L1 | `ApproveClinicOnboardingCommandValidator.cs` | Add `Enum.IsDefined` guard on `BillingModeOverride` | Deferred |
| V3 | `InternalOnboardingController.cs`, `InternalPlansController.cs` | Pre-existing intentional deviation — accepted | Accepted |

## Approval

- [x] All CRITICAL findings resolved or accepted with documented risk
- [x] All HIGH findings resolved or have accepted risk noted
- [x] No architectural violations remaining (V3 is pre-existing/accepted)
- [ ] M1 (IActionLogContext) and L1 (enum validation) deferred — resolve before next billing-feature iteration
