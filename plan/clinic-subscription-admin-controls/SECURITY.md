# Security Review — clinic-subscription-admin-controls [TenantCore.App]

**Date:** 2026-10-01
**Reviewer:** /feature-security-analysis
**Risk:** LOW
**Verdict:** Safe to merge

---

## Summary

No security vulnerabilities found. The feature correctly enforces internal-service-key auth
on all new admin endpoints, applies fixed-time key comparison, maintains multi-tenant
isolation throughout, and keeps log context identifier-only.

---

## Passing Checks

| Check | Result |
|-------|--------|
| Internal endpoints use `[Authorize(AuthenticationSchemes = "InternalService")]` | ✅ |
| `CryptographicOperations.FixedTimeEquals` for key comparison | ✅ |
| Multi-tenant isolation — all handlers use `GetByIdForClinicAsync` | ✅ |
| Cross-tenant subscription cancel guard (`subscription.ApplicationId != command.ApplicationId`) | ✅ |
| `ActionLogContext` identifiers only — no PII, free text, email or reason logged | ✅ |
| `ClinicSuspended.razor` suspension message rendered as plain text (not `MarkupString`) | ✅ |
| `/api/internal` correctly exempt from `SubscriptionGuardMiddleware` | ✅ |
| Webhook-check 30s throttle via `payment.LastCheckAt` | ✅ |
| All commands are `sealed record` with `IBusinessAction + IActionLogContext` | ✅ |
| All handlers are `sealed class` | ✅ |

---

## Findings

### LOW

**L1 — Missing `ILogger` in three plan offer / visibility handlers** *(not fixed — skipped)*

`CreateClinicPlanOfferHandler`, `WithdrawClinicPlanOfferHandler`, and
`SetClinicPlanVisibilityHandler` inject no `ILogger<T>`. The pipeline
`ActionLoggingBehavior` covers the full audit trail, so there is no audit gap —
but ADR-011 expects every data-changing handler to emit its own structured log.
Handler-level DB failures will surface through the pipeline only, without a
handler-specific entity-ID context.

### Code Smells

**S1 — Duplicate `using TenantCore.Domain.Exceptions` in `ConfirmPaymentTaskHandlerTests.cs`** *(not fixed — skipped)*

Lines 6 and 9 both import the same namespace. No runtime impact; a compiler
warning in non-`TreatWarningsAsErrors` projects.

### Architectural Violations

**V1 — `SubscriptionPlan.CreateCustom` minimum-price guard is application-layer only** *(not fixed — skipped)*

`PlanDetailsValidator` enforces `price >= planSettings.MinimumCustomPlanPriceRupees`;
the domain factory accepts any price ≥ 0. Future commands that call the factory
directly could bypass this floor. Business invariants should live in the domain entity.

**V2 — Internal controllers don't inherit `ClinicControllerBase`** *(accepted by design)*

`InternalClinicsController` and `InternalPlansController` use `ControllerBase` directly
and use the `InternalService` auth scheme, not JWT. `ClinicControllerBase.GetApplicationId()`
is JWT-based and does not apply here. Both controllers document this in comments.
No fix required.

---

## JWT Impact Note

None — this feature adds no new JWT claims and does not change the token structure.
TenantCore.Auth requires no coordinated update.
