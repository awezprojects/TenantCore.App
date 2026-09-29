# Security Analysis: Approval-Based Clinic Onboarding & Razorpay Subscriptions

**Date:** 2026-09-28
**Repo:** TenantCore.App
**Plan:** plan/clinic-trial-razorpay-subscriptions/PLAN.md
**ADR reference:** docs/adr/ADR-010-security.md (condensed checklist: `.clinerules` §6)

## Overall Risk Level

**Medium, now Low after fixes.** The two CRITICAL findings were both defects in the durable-workflow engine itself — the exact "100%-reliability" mechanism the feature was built around — not in authentication, tenant isolation, or input handling, all of which were solid on first read. Both are now fixed, tested, and verified against the full suite.

## Findings

### CRITICAL

- **[C1]** Paid onboarding activations silently activated the Trial plan instead of the plan the customer paid for — `src/TenantCore.Application/Features/Onboarding/Tasks/ProvisionClinicTaskHandler.cs` — **Fixed**
- **[C2]** Permanent/exhausted failures on payment-link creation, cancellation, and paid-plan activation never flagged the request, and every recovery path (self-healer, admin Retry) was a no-op for them — `src/TenantCore.Infrastructure/BackgroundJobs/WorkflowTaskProcessor.cs`, `src/TenantCore.Application/Features/Onboarding/Services/OnboardingSelfHealer.cs` — **Fixed**

### HIGH

None.

### MEDIUM

- **[M1]** `AuthProvisioningService` never forwarded a CorrelationId to TenantCore.Auth (ADR-010 §5) — `src/TenantCore.Infrastructure/ExternalServices/AuthProvisioningService.cs` — **Fixed** (new `IWorkflowCorrelationContext`, scoped per workflow-task attempt)
- **[M2]** `PaymentWebhookEventRepository.TryAddAsync` had a check-then-add race on `EventId` that could surface as an unhandled 500 under genuine concurrent duplicate delivery — `src/TenantCore.Infrastructure/Repositories/PaymentWebhookEventRepository.cs` — **Fixed**

### LOW / Informational

- `api/internal/onboarding/*` trusts `AdminUserId`/`AdminEmail` from the request body at face value — anyone holding the shared `X-Internal-Service-Key` can attribute an action to any admin id. Explicitly plan-acknowledged, deferred to the Admin-portal auth plan — **Accepted, not fixed**
- `POST /api/internal/workflow-tasks/{taskId}/retry` is declared with an absolute route override inside `InternalOnboardingController` rather than a controller of its own. Organizational only — **Accepted, not fixed**

### Code Smells

- **[S6]** `ProcessWebhookEventTaskHandler.HandleExpiredOrCancelledAsync` + its caller called `SaveChangesAsync()` three times for one logical webhook-processing operation — **Fixed** (consolidated to one call)
- **[S10]** `ClinicOnboardingRequestRepository.GetOpenForUserAsync` (a pure existence check) didn't use `AsNoTracking()` — **Fixed**

### Architectural Violations

None. `InternalOnboardingController` and `RazorpayWebhookController` deliberately inherit plain `ControllerBase`, not `ClinicControllerBase` — both are legitimately non-tenant-scoped (shared-key or anonymous-HMAC callers), so `ClinicControllerBase`'s JWT-derived helpers would be meaningless there.

### Over-Engineering

None.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | Every controller/action has an explicit `[Authorize(...)]` or `[AllowAnonymous]` with a justifying comment; internal endpoints correctly restricted to the `InternalService` scheme only |
| Multi-Tenancy Isolation | PASS | `ClinicOnboardingRequest` is deliberately user-scoped (no clinic exists yet), consistently filtered by `RequestedByUserId` from the token; `SubscriptionPaymentsController` correctly uses `GetApplicationId()`/`GetCurrentUserId()` |
| Input Validation | PASS | Every validator matches the plan's Validation Rules table; the one FluentValidation `.When()` scoping bug (required-Amount check silently skipped) was caught and fixed during implementation, before this review |
| Data Access & SQL Injection | PASS | No raw SQL anywhere in the new code; `AsNoTracking()` present on all other read paths (one miss found and fixed — S10) |
| Sensitive Data Handling | PASS | No secrets/signatures/keys logged anywhere in the Razorpay gateway or Auth provisioning client; webhook signature verified with a fixed-time comparison |
| Business Logic Security | PARTIAL → PASS after fixes | C1 and C2 were both business-logic-security-class defects (wrong entity state committed; failures invisible) — both fixed and covered by new regression tests |
| OWASP Top 10 | PASS | A01/A03/A07 all clean on inspection; webhook auth is HMAC-only by design (A04 non-issue) |
| Code Quality | PASS after fixes | S6 and S10 fixed; no architectural violations or over-engineering found |

## Fixes Applied

| ID | File | Change | Status |
|----|------|--------|--------|
| C1 | `Features/Onboarding/Tasks/ProvisionClinicTaskHandler.cs` | Added `ActivationAggregate(request)` helper; the `ActivateSubscription` enqueue now uses `nameof(SubscriptionPayment)`/`request.CurrentPaymentId` for a paid plan instead of always `nameof(ClinicOnboardingRequest)` | Applied |
| C2 | `Infrastructure/BackgroundJobs/WorkflowTaskProcessor.cs` | Added `ResolveRelatedRequestIdAsync` — `FailAndFlagAsync` now resolves the related request via the task's `SubscriptionPayment.OnboardingRequestId` when the aggregate isn't the request itself | Applied |
| C2 | `Application/Features/Onboarding/Services/OnboardingSelfHealer.cs` | Replaced the aggregate-mismatched `GetOpenForAggregateAsync` pre-check with a new `EnsureTaskAsync` helper that checks/repairs by the step's exact idempotency key (via `GetByIdempotencyKeyAsync`) and resets a `Failed` task instead of silently no-op'ing | Applied |
| M1 | `Application/Common/IWorkflowCorrelationContext.cs` (new), `Infrastructure/Services/WorkflowCorrelationContext.cs` (new), `Infrastructure/DependencyInjection.cs`, `Infrastructure/BackgroundJobs/WorkflowTaskProcessor.cs`, `Infrastructure/ExternalServices/AuthProvisioningService.cs` | New Scoped per-attempt correlation holder, set by the processor and read by `AuthProvisioningService` to send `X-Correlation-Id` — additive, no interface signature changes | Applied |
| M2 | `Infrastructure/Repositories/PaymentWebhookEventRepository.cs` | Overrode `SaveChangesAsync` to catch the `IX_PaymentWebhookEvents_EventId` unique-index violation and treat it as a duplicate (return 0) instead of an unhandled exception | Applied |
| S6 | `Application/Features/Onboarding/Tasks/ProcessWebhookEventTaskHandler.cs` | Removed the two extra `SaveChangesAsync()` calls in `HandleExpiredOrCancelledAsync`; everything now commits once, in `HandleAsync` | Applied |
| S10 | `Infrastructure/Repositories/ClinicOnboardingRequestRepository.cs` | Added `AsNoTracking()` to `GetOpenForUserAsync` | Applied |

**Test changes required by the fixes** (behavior-preserving, not new findings): `AuthProvisioningServiceTests.cs` updated for the new constructor parameter (+2 new tests for CorrelationId forwarding); `WorkflowTaskProcessorTests.cs` updated to register the two new DI dependencies and gained 2 new regression tests for C2; `OnboardingSelfHealerTests.cs`'s `HealRequestAsync_OpenTaskExists_IsNoOp` rewritten (the old assertion tested an implementation detail — "GetByIdAsync never called" — that's no longer true now that the correct aggregate key must be resolved per-state; the *actual* behavior it cared about, "nothing gets (re-)enqueued when something is already in flight," is preserved and now correctly tested against the right aggregate) plus 1 new regression test for the Failed-task-revival fix. Full suite: **1110/1110 passing**, 0 build errors.

## Approval

- [x] All CRITICAL findings resolved
- [x] All MEDIUM findings resolved (no HIGH findings existed)
- [x] No architectural violations remaining
- [x] Ready to merge
