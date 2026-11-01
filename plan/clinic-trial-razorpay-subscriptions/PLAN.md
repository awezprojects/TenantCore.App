# Feature Plan: Approval-Based Clinic Onboarding with Razorpay Payment Links (Durable, Retryable)

**Repo:** TenantCore.App
**Date:** 2026-09-23
**Domain area:** Clinic onboarding / Subscriptions / Payments
**Status:** Approved — ready for execution (supersedes the earlier self-serve-trial draft at this path)
**Paired with:** `TenantCore.Auth/plan/clinic-trial-razorpay-subscriptions/PLAN.md` — **execute Auth first** (the App's provisioning step calls Auth's new internal endpoint).
**Followed by:** a TenantCore.Admin plan (review queue UI) to be written after go-ahead. This plan exposes every API the Admin portal needs.

---

## Overview

Clinic creation stops being self-serve. Any user can still sign up. The dashboard then shows the clinics they are mapped to plus a **"Request a new clinic"** card instead of "Create clinic". The request, with clinic and doctor details, goes to an internal review queue. An internal admin approves it, choosing a paid plan or granting a trial, or rejects it. Approval of a paid plan automatically creates a **Razorpay Payment Link**, which is emailed to the doctor. When Razorpay confirms the payment, the system automatically provisions the clinic in TenantCore.Auth with the user as owner, Clinic Admin and Doctor. It then activates the subscription and emails "your clinic is ready". Renewals for existing clinics use the same payment-link path. Paid plans can no longer be activated for free.

Every step that talks to something outside this database runs through a **durable workflow**, not inline in the HTTP request:
- the external services are Razorpay, TenantCore.Auth and email
- every state change is committed together with the task that continues it
- a background processor retries each task with backoff until it succeeds
- idempotency keys make retries safe
- a reconciliation sweep catches anything a webhook missed
- anything that still cannot complete is surfaced to an admin with a one-click retry

Nothing is ever silently dropped.

---

## Decisions (confirmed by the user)

| # | Decision |
|---|----------|
| 1 | Admin portal does not exist yet — it will be built next, reads the same databases read-only, and performs every mutation through the internal App APIs defined here |
| 2 | Provisioning after payment is **automatic** |
| 3 | Existing clinics and their current subscriptions keep working unchanged |
| 4 | **The admin sets the amount at approval.** The Approve dialog pre-fills the plan's list price, and the admin can change it to any amount within configured limits, giving a reason whenever it differs from the list price. The payment link is then created automatically with that exact amount. If the amount needs to change after the link has gone out, the admin uses "Change amount". That safely cancels the old link and issues a new one. The plan still decides the subscription duration. Renewals started by a Clinic Admin from the subscription page charge the plan's list price. *(Confirmed 2026-09-27)* |
| 5 | Rejected or expired requests are closed; the user submits a fresh request |
| 6 | **Service-to-service auth stays a shared key for the virtual machine launch.** Every credential, including Auth, blob, storage and SQL, moves to Entra ID with managed identity in a later, separate change. Nothing in this plan switches to Entra. *(Confirmed 2026-09-27)* |
| — | Every onboarding/payment operation must be retryable until it succeeds or is explicitly handed to a human |

---

## Reliability Design (the core of this plan)

### Guarantee, stated precisely
No automated system can promise 100% success when an outside party can be down for days or reject data. What this design guarantees:
1. **Nothing is lost.** Every intent is written to SQL Server before any outside call is made.
2. **Every step is retried automatically** until it succeeds. Transient failures are timeouts, 5xx, 429, network errors and app restarts.
3. **Retries never duplicate effects.** There is one payment link per payment, one clinic per request, one subscription per payment, and one email per purpose.
4. **Permanent failures and exhausted retries become visible.** A request is flagged **NeedsAttention** with the exact error. The admin fixes the cause, for example a clinic code that is already taken, and presses Retry. The workflow resumes from the failed step, not from the start.
5. **Missed webhooks are recovered** by a periodic reconciliation that asks Razorpay directly.

### Building blocks

| Block | What it does |
|-------|--------------|
| **Transactional outbox (`WorkflowTask` table)** | Any handler that changes state also inserts the next WorkflowTask in the **same SaveChanges**, so a state change and the work that continues it either both commit or neither does. |
| **Workflow processor (`WorkflowTaskProcessor`, BackgroundService)** | Polls every 10 s for due tasks, where status is Pending or Failed-Retryable and NextAttemptAt has passed. It leases each task by setting LockedUntil to now + 5 min with a RowVersion optimistic update, so only one App instance runs a task even on scale-out. It then runs the task's handler in a fresh DI scope and records the outcome. A crashed instance's lease simply expires and another instance picks the task up. |
| **Retry policy** | Exponential backoff with jitter: 30 s, 1 m, 2 m, 5 m, 15 m, 30 m, then hourly. The default ceiling is 72 attempts, roughly 3 days. The handler classifies each error. **Transient** errors reschedule the task. **Permanent** errors stop retrying and flag NeedsAttention immediately; these are a 4xx other than 408 or 429, a validation rejection, or a code conflict. Exhausting the ceiling also flags NeedsAttention. All values are configurable. |
| **Idempotency keys** | Every task carries a unique IdempotencyKey, for example `provision:{requestId}` or `link:{paymentId}`, with a unique index so the same task can never be enqueued twice. Every outside call also carries its own key. Razorpay payment links use `reference_id` = our payment id. Auth provisioning uses `ProvisioningReference` = request id, and Auth returns the existing clinic on a repeat. Subscription activation checks for an existing subscription for the payment before inserting, backed by a unique index. |
| **Webhook inbox (`PaymentWebhookEvent` table)** | The webhook endpoint only verifies the signature, stores the raw event keyed by Razorpay's `x-razorpay-event-id` with a unique index, enqueues a `ProcessWebhookEvent` task, and returns 200. Duplicates are ignored at insert. The processing work lives in a retryable task, so a failure never loses the event. |
| **Reconciliation sweep (`PaymentReconciliationService`, BackgroundService, every 15 min)** | For every payment still in `LinkCreated` that is more than 10 minutes old, it fetches the link from Razorpay. If the link is paid, it enqueues the same `ConfirmPayment` task the webhook would, which is idempotent. If it is expired or cancelled, it marks the payment and request accordingly. This makes the system correct **even if Razorpay webhooks never arrive**. **Silent-webhook alert:** if the sweep finds a paid link with no matching webhook event after 30 minutes, it enqueues a SendEmail to `Onboarding:OpsNotificationEmail`, at most once per hour. This usually means Razorpay has disabled the webhook after 24 hours of failed deliveries and it needs re-enabling in the dashboard. |
| **Instant check on return** | After paying, Razorpay redirects the doctor to our callback page. That page calls `POST api/onboarding/requests/{id}/check-payment`, owner only and limited to once per 30 seconds per request. It enqueues the same idempotent ConfirmPayment task, so activation starts within seconds. The redirect itself is never trusted, because ConfirmPayment always re-checks with Razorpay's API. |
| **Self-healing sweep (same service)** | It checks one invariant: every request or payment in a non-terminal state must have an open task. A row in `PaymentReceived` or `Provisioning` with no Pending or InProgress task gets its next task re-enqueued, using the same idempotency key so this is a no-op if one exists. This covers bugs and manual database edits. |
| **In-call resilience** | The Razorpay and Auth named HttpClients use `Microsoft.Extensions.Http.Resilience`'s standard handler for a small number of fast in-request retries plus timeouts and a circuit breaker. That handles blips, and the durable retry above handles longer outages. |
| **Audit** | Every task attempt is written to the existing `ActionLogs` table via `IActionLogger`, with task type, attempt, outcome, duration and error. User and admin commands implement `IBusinessAction`. |

### Failure scenarios covered

| What fails | What happens |
|-----------|--------------|
| App crashes right after admin approval | The approval and the `CreatePaymentLink` task were committed together, so the processor creates the link after restart |
| Razorpay API down while creating a link | The task retries with backoff. The request shows "Preparing payment link" to the admin, and NeedsAttention only after about 3 days |
| Link created at Razorpay but our response times out | The retry sends the same `reference_id`. Razorpay rejects the duplicate, so the handler looks up the existing link by reference and adopts it, and no second link is created |
| Payment-link email fails | A separate `SendEmail` task retries. Razorpay also sends its own notify email and SMS, so the doctor still gets the link |
| Webhook never arrives, or is lost or rejected | The reconciliation sweep finds the paid link within 15 minutes |
| Webhook delivered twice | The unique event id means the second copy is ignored |
| Webhook processing throws | The event stays in the inbox and its task retries |
| Auth down during provisioning | The `ProvisionClinic` task retries. The payment is safe and the request shows "Payment received, setting up clinic" |
| Auth created the clinic but the response was lost | The retry sends the same ProvisioningReference, and Auth returns the already-created clinic |
| Clinic code taken by the time of provisioning | Permanent error, and the request is flagged NeedsAttention with "clinic code taken". The admin changes the code and retries |
| Subscription insert fails | The `ActivateSubscription` task retries. The unique index on payment id prevents a double subscription |
| "Clinic ready" email fails | The `SendEmail` task retries. The clinic is already usable from the dashboard |
| Two App instances pick the same task | The RowVersion lease lets only one win. The other skips it |
| Admin changes the amount while the doctor is paying the old link | The replacement link is created only after the old link's cancellation succeeds at Razorpay. If the cancel finds the old link already paid, that payment is accepted and provisioning continues, and the replacement is never created. The doctor can never pay twice |
| Payment made but the request was rejected or cancelled meanwhile | A payment can only be created for an approved request, and rejection cancels any open link at Razorpay through a retryable task. If money still arrives, the request is flagged NeedsAttention for a manual refund and nothing is auto-provisioned |
| Link expires unpaid | Payment becomes Expired and the request becomes PaymentLinkExpired, which is terminal. The user is emailed and can submit a new request |

---

## Onboarding State Machine (`ClinicOnboardingRequest.Status`)

| Status | Meaning | Next |
|--------|---------|------|
| Submitted | User sent the request | Approved / Rejected / Cancelled |
| Approved | Admin approved; transient while the link is being made (or provisioning for a trial grant) | AwaitingPayment / Provisioning |
| AwaitingPayment | Payment link exists and is emailed | PaymentReceived / PaymentLinkExpired |
| PaymentReceived | Razorpay confirmed payment | Provisioning |
| Provisioning | Clinic being created in Auth + subscription activation | Active |
| Active | Clinic exists and subscription is live (terminal) | — |
| Rejected | Admin rejected (terminal) | — |
| Cancelled | User withdrew while still Submitted (terminal) | — |
| PaymentLinkExpired | Link expired unpaid (terminal) | — |

`NeedsAttention` is a **flag** (bool + reason), not a status, so the admin always sees where the request was stuck. Clearing it happens when an admin retries or the step later succeeds.

**Rules:** a user may have at most **one open request**, meaning Submitted, Approved, AwaitingPayment, PaymentReceived or Provisioning. Terminal requests never reopen; the user submits a new one (decision 5). Once Active, the user may request another clinic.

---

## Entities

### ClinicOnboardingRequest (new) — **not tenant-scoped** (no clinic exists yet; scoped to the requesting user)
Base: AuditableEntity, private setters, static `Submit(...)` factory, state-transition methods that throw `InvalidOperationException` on illegal transitions.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| RequestedByUserId | Guid | required, indexed |
| RequesterName / RequesterEmail | string | required, max 150 / 256 — snapshotted from the JWT at submit (the worker has no user token later) |
| RequesterPhone | string | required, max 20 |
| ClinicName | string | required, max 200 |
| PreferredClinicCode | string | required, max 20, upper-cased, letters/digits/hyphen |
| Address, City, State, Pincode | string | required, max 500 / 100 / 100 / 10 |
| ClinicContactNumber, OfficialEmail, Website | string? | max 20 / 256 / 256 |
| DoctorName | string | required, max 150 |
| MedicalRegistrationNumber, MedicalCouncil | string | required, max 50 / 150 |
| ExpectedStaffCount | int | 1–500 |
| ReferralSource, Notes | string? | max 100 / 1000 |
| Status | ClinicOnboardingStatus | required, indexed |
| ApprovedPlanId / ApprovedPlanCode | Guid? / enum? | set on approve |
| PlanListPrice | decimal(18,2)? | snapshot of the plan price at approval, kept so reports show the discount |
| ApprovedAmount | decimal(18,2)? | the amount the doctor must pay, set by the admin at approval and updated by Change amount |
| AmountReason | string? | max 500 — required when ApprovedAmount differs from PlanListPrice |
| LastPaymentCheckAt | DateTime? | throttles the doctor's "check payment" call |
| IsTrialGrant | bool | true when admin approved with Trial (no payment) |
| ApprovedClinicCode | string? | max 20 — admin may override the preferred code |
| ReviewedByAdminId / ReviewedByAdminEmail / ReviewedAt / ReviewNote | Guid? / string? max 256 / DateTime? / string? max 1000 | |
| RejectionReason | string? | max 1000 |
| CurrentPaymentId | Guid? | the SubscriptionPayment for this request |
| ProvisionedApplicationId | Guid? | set once Auth returns the clinic |
| ClinicSubscriptionId | Guid? | set on activation |
| ActivatedAt | DateTime? | |
| NeedsAttention | bool | indexed |
| AttentionReason | string? | max 2000 |
| RowVersion | byte[] | concurrency |

### SubscriptionPayment (new) — one Razorpay payment link and its outcome
Tenant-scoped **once a clinic exists**: ApplicationId is null for onboarding payments until provisioning, and always set for renewals.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK; also the Razorpay `reference_id` (formatted "sp_" + 32 hex chars, fits Razorpay's 40-char limit) |
| Purpose | PaymentPurpose | Onboarding / Renewal |
| OnboardingRequestId | Guid? | FK-by-convention, indexed |
| ApplicationId | Guid? | indexed — set for renewals, and back-filled at provisioning for onboarding |
| SubscriptionPlanId, PlanCode, PlanName | Guid / enum / string max 100 | snapshot |
| Amount, AmountInMinorUnits, Currency | decimal(18,2) / long / string max 3 | onboarding: the request's ApprovedAmount; renewal: the plan price. Always set server-side and never taken from the doctor's browser |
| PlanListPrice | decimal(18,2) | plan price at creation, for discount reporting |
| ReplacesPaymentId / ReplacedByPaymentId | Guid? / Guid? | links the chain when an admin changes the amount |
| PayerName, PayerEmail, PayerPhone | string | snapshot for the link's customer block |
| GatewayPaymentLinkId | string? | max 64, unique filtered |
| PaymentLinkUrl | string? | max 500 (Razorpay short_url) |
| LinkExpiresAt | DateTime? | |
| Status | SubscriptionPaymentStatus | Pending (link not yet created) / LinkCreated / Paid / Expired / Cancelled / Superseded (replaced by a Change amount) |
| GatewayPaymentId | string? | max 64, unique filtered |
| Method | string? | max 20 (card / upi) |
| PaidAt | DateTime? | |
| ClinicSubscriptionId | Guid? | unique filtered — one subscription per payment |
| InitiatedByUserId | Guid? | Clinic Admin (renewal) or null (onboarding — admin-initiated) |
| RowVersion | byte[] | |

### WorkflowTask (new) — durable outbox job. Not tenant-scoped (internal).

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| TaskType | WorkflowTaskType | CreatePaymentLink / CancelPaymentLink / SendEmail / ConfirmPayment / ProvisionClinic / ActivateSubscription / ProcessWebhookEvent |
| IdempotencyKey | string | required, max 200, **unique** |
| AggregateType / AggregateId | string max 50 / Guid | which request / payment / webhook event it acts on; indexed together |
| PayloadJson | string? | nvarchar(max) — only for SendEmail (template + recipient) |
| Status | WorkflowTaskStatus | Pending / InProgress / Succeeded / Failed (permanent or exhausted) |
| AttemptCount / MaxAttempts | int | |
| NextAttemptAt | DateTime | indexed with Status (the processor's poll query) |
| LockedUntil / LockedBy | DateTime? / string? max 100 | lease |
| LastError | string? | max 2000 — no secrets |
| CompletedAt | DateTime? | |
| RowVersion | byte[] | |

### PaymentWebhookEvent (new) — inbox. Not tenant-scoped.

| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK |
| Gateway | string | max 20 |
| EventId | string | max 100, **unique** (from `x-razorpay-event-id`) |
| EventType | string | max 100 |
| PayloadJson | string | nvarchar(max) — raw body |
| ReceivedAt / ProcessedAt | DateTime / DateTime? | |

### ClinicSubscription (existing) — additions
| Property | Type | Constraints |
|----------|------|-------------|
| SubscriptionPaymentId | Guid? | unique filtered — idempotency guard for activation |
| OnboardingRequestId | Guid? | trial grants have no payment; this guards against a second trial for the same request |
| PurchasedByUserId | Guid? | owner (onboarding) or Clinic Admin (renewal) |

---

## Files to Create

### Shared (`src/TenantCore.Shared/`)

| File | Purpose |
|------|---------|
| `Enums/ClinicOnboardingStatus.cs`, `Enums/SubscriptionPaymentStatus.cs`, `Enums/PaymentPurpose.cs` | Enums above (WorkflowTask enums live in Domain — never exposed to the client) |
| `Dtos/Onboarding/SubmitClinicOnboardingRequest.cs` | User form body (all user-editable fields above) |
| `Dtos/Onboarding/ClinicOnboardingRequestDto.cs` | User-facing view: fields, Status, friendly status text, PaymentLinkUrl + expiry when AwaitingPayment, ProvisionedApplicationId when Active, RejectionReason. **Never** exposes NeedsAttention internals or admin notes beyond RejectionReason |
| `Dtos/Onboarding/ApproveClinicOnboardingRequest.cs` | Internal: SubscriptionPlanId, GrantTrial, Amount (required for a paid plan; the Admin portal pre-fills the list price), AmountReason, ClinicCode (optional override), ReviewNote, AdminUserId, AdminEmail |
| `Dtos/Onboarding/ChangePaymentAmountRequest.cs` | Internal: Amount, Reason, AdminUserId, AdminEmail |
| `Dtos/Onboarding/RejectClinicOnboardingRequest.cs` | Internal: Reason, AdminUserId, AdminEmail |
| `Dtos/Onboarding/RetryClinicOnboardingRequest.cs` | Internal: optional new ClinicCode, AdminUserId, AdminEmail |
| `Dtos/Onboarding/AdminActionRequest.cs` | Internal: AdminUserId, AdminEmail (resend link, retry task) |
| `Dtos/Subscriptions/CreateRenewalPaymentLinkRequest.cs` | Clinic Admin: SubscriptionPlanId |
| `Dtos/Subscriptions/SubscriptionPaymentDto.cs` | Payment history row incl. PaymentLinkUrl while LinkCreated |
| `Errors/OnboardingErrorCodes.cs` | OpenRequestExists, SelfServiceCreationDisabled, PaidPlanRequiresPaymentLink, PaymentsNotConfigured |

### Domain (`src/TenantCore.Domain/`)

| File | Purpose |
|------|---------|
| `Entities/ClinicOnboardingRequest.cs` | Entity + transitions: Approve, Reject, Cancel, MarkAwaitingPayment, MarkPaymentReceived, MarkProvisioning, SetProvisionedClinic, Activate, MarkLinkExpired, FlagAttention, ClearAttention, OverrideClinicCode |
| `Entities/SubscriptionPayment.cs` | Entity + transitions: CreateForOnboarding, CreateForRenewal, SetLink, MarkPaid (idempotent if same GatewayPaymentId), MarkExpired, MarkCancelled, AttachClinic, AttachSubscription |
| `Entities/WorkflowTask.cs` | Entity + Enqueue factory, Lease, Succeed, ScheduleRetry(error, nextAt), Fail(error), ResetForManualRetry |
| `Entities/PaymentWebhookEvent.cs` | Entity + MarkProcessed |
| `Enums/WorkflowTaskType.cs`, `Enums/WorkflowTaskStatus.cs` | Internal enums |
| `Exceptions/PermanentWorkflowException.cs` | Thrown by task handlers for non-retryable failures (the processor stops retrying and flags attention) |
| `Interfaces/IClinicOnboardingRequestRepository.cs` | GetOpenForUserAsync(userId), GetForUserAsync(userId) (AsNoTracking, newest first, max 50), GetByIdForUserAsync(id, userId), GetByIdAsync(tracked), GetStuckAsync (non-terminal with no open task — for self-healing) |
| `Interfaces/ISubscriptionPaymentRepository.cs` | GetByGatewayLinkIdAsync, GetDueForReconciliationAsync(olderThan, batch), GetForClinicAsync(applicationId) (AsNoTracking, max 100), GetOpenRenewalForClinicAsync(applicationId) |
| `Interfaces/IWorkflowTaskRepository.cs` | EnqueueAsync (no-op when IdempotencyKey exists), LeaseDueAsync(batch, instanceId, leaseUntil) → leased tasks, GetOpenForAggregateAsync, GetFailedForAggregateAsync |
| `Interfaces/IPaymentWebhookEventRepository.cs` | TryAddAsync (returns false on duplicate EventId) |

### Infrastructure (`src/TenantCore.Infrastructure/`)

| File | Purpose |
|------|---------|
| `Persistence/Configurations/Clinic/ClinicOnboardingRequestConfiguration.cs` | Lengths, indexes (RequestedByUserId, Status, NeedsAttention), enum as int, RowVersion |
| `Persistence/Configurations/Clinic/SubscriptionPaymentConfiguration.cs` | Lengths, decimal precision, unique filtered indexes on GatewayPaymentLinkId / GatewayPaymentId / ClinicSubscriptionId, RowVersion |
| `Persistence/Configurations/Clinic/WorkflowTaskConfiguration.cs` | Unique IdempotencyKey, index (Status, NextAttemptAt), index (AggregateType, AggregateId), RowVersion |
| `Persistence/Configurations/Clinic/PaymentWebhookEventConfiguration.cs` | Unique EventId |
| `Repositories/ClinicOnboardingRequestRepository.cs`, `SubscriptionPaymentRepository.cs`, `WorkflowTaskRepository.cs`, `PaymentWebhookEventRepository.cs` | Implementations. Unique-index violations are translated into "already exists" results inside the repository, and lease conflicts into "not leased", so EF exceptions never reach Application |
| `ExternalServices/Razorpay/RazorpayOptions.cs` | KeyId, KeySecret, WebhookSecret, ApiBaseUrl, Currency, PaymentLinkExpiryDays, CallbackUrl, NotifyByEmail, NotifyBySms, IsConfigured |
| `ExternalServices/Razorpay/RazorpayPaymentGateway.cs` | Implements `IPaymentGateway` over the named client "RazorpayApi" (Basic auth KeyId:KeySecret). Handles: create payment link, get a link by id, find a link by reference_id (adopt-on-duplicate), cancel a link, and verify the webhook signature with HMAC-SHA256 of the raw body using fixed-time comparison. It maps HTTP outcomes into an Application-level result with a Transient/Permanent classification. It never logs keys, signatures or full bodies |
| `ExternalServices/Razorpay/RazorpayDtos.cs` | Internal JSON shapes |
| `ExternalServices/AuthProvisioningService.cs` | Implements `IAuthProvisioningService` over a **new** named client "AuthInternalApi". It sends the `X-Internal-Service-Key` header from config instead of a user token, because the worker has no user token. It calls Auth's `POST api/internal/clinics/provision` and classifies the response: success, or already exists (both mean OK and return the clinic id). A code conflict or an inactive or missing owner is Permanent. A 5xx or timeout is Transient |
| `BackgroundJobs/WorkflowTaskProcessor.cs` | BackgroundService per the reliability design; dispatches by TaskType to the matching `IWorkflowTaskHandler` resolved from a fresh scope; computes backoff; writes ActionLogs; on Failed, flags the related request NeedsAttention |
| `BackgroundJobs/PaymentReconciliationService.cs` | BackgroundService: reconciliation + self-healing sweeps every 15 min |
| `BackgroundJobs/WorkflowOptions.cs` | PollIntervalSeconds, BatchSize, LeaseMinutes, MaxAttempts, BackoffScheduleSeconds, ReconciliationIntervalMinutes, Enabled |
| `ExternalServices/InternalServiceKeyAuthenticationHandler.cs` | ASP.NET Core authentication scheme "InternalService". It validates the `X-Internal-Service-Key` header against `InternalApi:AdminPortalKey` with a fixed-time compare and issues a principal with the claim role=InternalService. Used only by `api/internal/*` |

### Application (`src/TenantCore.Application/`)

| File | Purpose |
|------|---------|
| `Services/IPaymentGateway.cs` | Gateway contract + result records (GatewayResult with IsTransient, error text; PaymentLinkInfo with Id, ShortUrl, Status, PaymentId, Method, AmountPaid, ExpireBy) |
| `Services/IAuthProvisioningService.cs` | ProvisionClinicAsync(ProvisioningReference, OwnerUserId, clinic details) → result with ApplicationId or classified error |
| `Common/Workflow/IWorkflowTaskHandler.cs` | Contract: TaskType + HandleAsync(task, ct). Throw PermanentWorkflowException for permanent failures, any other exception = transient |
| `Common/Workflow/IWorkflowEnqueuer.cs` + `WorkflowEnqueuer.cs` | Helper that builds WorkflowTask rows with standard idempotency keys; used by handlers so enqueue always happens inside the same unit of work |
| `Features/Onboarding/Commands/SubmitClinicOnboardingCommand.cs` | UserId, name/email from claims, request body → Guid. Rejects if an open request exists (409). Enqueues SendEmail tasks: "request received" to the user and "new request" to `Onboarding:OpsNotificationEmail` |
| `Features/Onboarding/Commands/CancelClinicOnboardingCommand.cs` | Owner only; Submitted only |
| `Features/Onboarding/Commands/CheckOnboardingPaymentCommand.cs` + handler | Owner only. Valid only in AwaitingPayment, otherwise 409. Throttled by a LastPaymentCheckAt column on the request, and a call within 30 s throws InvalidOperationException, which the controller maps to 429. Otherwise it enqueues ConfirmPayment for the current payment. Idempotent |
| `Features/Onboarding/Commands/ApproveClinicOnboardingCommand.cs` | Internal. Only valid from Submitted. When the plan is paid, it stores PlanListPrice, ApprovedAmount and AmountReason on the request, creates a SubscriptionPayment (Pending) for ApprovedAmount, moves the request to Approved and enqueues CreatePaymentLink. When the admin grants a trial, it moves the request to Provisioning and enqueues ProvisionClinic. Approval is refused (409) when payments are not configured and the plan is paid |
| `Features/Onboarding/Commands/RejectClinicOnboardingCommand.cs` | Internal; Submitted/Approved/AwaitingPayment → Rejected; enqueues CancelPaymentLink if a link exists and a "request rejected" SendEmail |
| `Features/Onboarding/Commands/ResendPaymentLinkEmailCommand.cs` | Internal; AwaitingPayment only; enqueues SendEmail with a fresh idempotency suffix |
| `Features/Onboarding/Commands/ChangePaymentAmountCommand.cs` | Internal. Valid only while the request is Approved or AwaitingPayment and its current payment is not Paid. A new amount equal to the current one returns 409. It updates ApprovedAmount and AmountReason, creates a replacement SubscriptionPayment (Pending) linked to the old one, and points the request at the replacement. If the old payment has no link yet, it is marked Superseded and CreatePaymentLink is enqueued for the replacement. Otherwise it enqueues CancelPaymentLink for the old link, and that handler creates the replacement link only after the cancellation succeeds. All in one save. Implements `IBusinessAction` |
| `Features/Onboarding/Commands/RetryClinicOnboardingCommand.cs` | Internal. It optionally overrides the clinic code, clears NeedsAttention, resets the request's Failed task to Pending with AttemptCount 0, and records admin plus time. If no Failed task exists, the self-healing sweep logic re-enqueues the correct next step |
| `Features/Onboarding/Commands/RetryWorkflowTaskCommand.cs` | Internal. TaskId + admin fields. Only a Failed task can be retried, otherwise 409. Resets it to Pending with AttemptCount 0 and NextAttemptAt now, records the admin, and clears NeedsAttention on the related request if there is one |
| `Features/Onboarding/Queries/GetMyClinicOnboardingRequestsQuery.cs`, `GetMyClinicOnboardingRequestByIdQuery.cs` | User's own requests only |
| `Features/Onboarding/Validators/*` | One per command (rules below) |
| `Features/Onboarding/Translators/ClinicOnboardingTranslator.cs` | Static: ToEntity (via Submit factory), ToDto, status → friendly text |
| `Features/Onboarding/Emails/OnboardingEmailTemplates.cs` | Static HTML builders: RequestReceived, OpsNewRequest, PaymentLink (plan, amount, link, expiry), ClinicReady, Rejected, LinkExpired, RenewalPaymentLink, RenewalActivated. All user values HTML-encoded |
| `Features/Onboarding/Tasks/CreatePaymentLinkTaskHandler.cs` | Loads the payment. If it is already LinkCreated, it does nothing. Otherwise it calls the gateway with reference_id = payment id, the payment row's amount, customer details, notify flags, expire_by and the callback URL. A duplicate-reference response means the handler finds and adopts the existing link. It then stores the link and moves the request to AwaitingPayment, or leaves a renewal as is, and enqueues the payment-link SendEmail — all in one save |
| `Features/Onboarding/Tasks/CancelPaymentLinkTaskHandler.cs` | Cancels the link at Razorpay, where already cancelled or expired counts as success. **Rejection case:** a link found already paid flags NeedsAttention "refund required". **Change amount case:** after a successful cancel it marks the old payment Superseded and enqueues CreatePaymentLink for the replacement. If the old link turns out to be already paid, it keeps that payment, enqueues ConfirmPayment for it, marks the replacement Cancelled without ever creating its link, and moves the request back to the old payment |
| `Features/Onboarding/Tasks/SendEmailTaskHandler.cs` | Sends via existing `IEmailService`; payload holds template id + recipient + model JSON |
| `Features/Onboarding/Tasks/ProcessWebhookEventTaskHandler.cs` | Parses the stored event. `payment_link.paid` enqueues ConfirmPayment. `payment_link.expired` and `payment_link.cancelled` update the payment and request and send the expiry email. Unknown events are marked processed |
| `Features/Onboarding/Tasks/ConfirmPaymentTaskHandler.cs` | Shared by the webhook and reconciliation. It re-fetches the link from Razorpay rather than trusting the payload, and confirms that it is paid and that the amount and currency match that payment row's own amount. This is the amount the admin approved, not the plan price; a mismatch is a Permanent failure plus attention. It marks the payment Paid, which is idempotent. For onboarding it moves the request to PaymentReceived and then Provisioning and enqueues ProvisionClinic. If the request was rejected or cancelled, it flags "refund required" and stops. For renewals it enqueues ActivateSubscription |
| `Features/Onboarding/Tasks/ProvisionClinicTaskHandler.cs` | If ProvisionedApplicationId is already set, it skips ahead. Otherwise it calls Auth with ProvisioningReference = request id and the approved code or the preferred one, stores the ApplicationId on the request and payment, and enqueues ActivateSubscription |
| `Features/Onboarding/Tasks/ActivateSubscriptionTaskHandler.cs` | If a ClinicSubscription already exists for this payment or request, it just links it. Otherwise it creates one, using the Trial plan for a trial grant or the paid plan with the existing renewal start-date rule. PricePaid is the amount actually paid, and the duration comes from the plan. It snapshots the billing contact from stored request or payment data, with no Auth call, and sets PurchasedByUserId. It moves the request to Active and enqueues the ClinicReady email, or the RenewalActivated email for a renewal |
| `Features/Onboarding/Services/OnboardingSelfHealer.cs` | Used by the reconciliation service and RetryClinicOnboardingCommand: given a request/payment in a non-terminal state, enqueue the correct next task (idempotent) |
| `Features/Subscriptions/Commands/CreateRenewalPaymentLinkCommand.cs` + handler + validator | Clinic Admin, clinic-scoped. Paid plans only; one open renewal link per clinic, and an existing open link is returned instead of a new one. Creates a Pending payment and enqueues CreatePaymentLink |
| `Features/Subscriptions/Queries/GetSubscriptionPaymentsQuery.cs` + handler | Clinic Admin payment history |
| `Features/Subscriptions/Commands/RecordPaymentWebhookCommand.cs` + handler + validator | Not clinic-scoped. Verifies the signature (401 on failure), then calls TryAdd on the inbox and enqueues ProcessWebhookEvent in one save. A duplicate returns success |
| Handlers for every command/query above | `Features/Onboarding/Handlers/*`, `Features/Subscriptions/Handlers/*` |

### API (`src/TenantCore.Api/Controllers/`)

| File | Purpose |
|------|---------|
| `ClinicOnboardingController.cs` | User endpoints (`api/onboarding/requests`) — RequireAuthenticated, user id from token, never ApplicationId |
| `SubscriptionPaymentsController.cs` | `api/subscriptions/payments` — RequireClinicAdmin, clinic-scoped via GetApplicationId(); under the guard-exempt `/api/subscriptions` prefix so an expired clinic can renew |
| `RazorpayWebhookController.cs` | `api/payments/razorpay/webhook` — `[AllowAnonymous]` (commented: authenticity = HMAC signature); reads raw body + `X-Razorpay-Signature` + `x-razorpay-event-id`; RequestSizeLimit 64 KB |
| `Internal/InternalOnboardingController.cs` | `api/internal/onboarding` — `[Authorize(AuthenticationSchemes = "InternalService")]`, excluded from Swagger outside Development. For the Admin portal |

---

## Files to Modify

| File | Change |
|------|--------|
| `Domain/Entities/ClinicSubscription.cs` | Add SubscriptionPaymentId, OnboardingRequestId, PurchasedByUserId and an optional pricePaid override as factory params. The override defaults to the plan price, so existing callers are unchanged |
| `Domain/Interfaces/IClinicSubscriptionRepository.cs` + `Infrastructure/Repositories/ClinicSubscriptionRepository.cs` | GetByPaymentIdAsync, GetByOnboardingRequestIdAsync |
| `Infrastructure/Persistence/Configurations/Clinic/ClinicSubscriptionConfiguration.cs` | Map new columns; unique filtered indexes on SubscriptionPaymentId and OnboardingRequestId |
| `Infrastructure/Persistence/ClinicDbContext.cs` | Add 4 DbSets: ClinicOnboardingRequests, SubscriptionPayments, WorkflowTasks, PaymentWebhookEvents (44 total) |
| `Infrastructure/DependencyInjection.cs` | Registers the 4 repositories, the RazorpayOptions/WorkflowOptions/InternalApi options, the "RazorpayApi" and "AuthInternalApi" HttpClients with the standard resilience handler, IPaymentGateway, IAuthProvisioningService, both hosted services, and all IWorkflowTaskHandler implementations (Scoped, assembly scan). Missing Razorpay keys must not break startup |
| `Application/DependencyInjection.cs` | Register IWorkflowEnqueuer, OnboardingSelfHealer |
| `Api/Program.cs` | Add the "InternalService" authentication scheme **alongside** JWT (no middleware reordering) |
| `Api/Middleware/SubscriptionGuardMiddleware.cs` | Add `/api/onboarding`, `/api/internal`, `/api/payments/razorpay` to the exempt prefixes (defensive — none send X-Application-Id) |
| `Api/TenantCore.Api.csproj` or Infrastructure csproj | Add `Microsoft.Extensions.Http.Resilience` (8.x) |
| `Application/Features/Clinics/Handlers/CreateClinicHandler.cs` | When `Onboarding:Mode` = "Approval" (default), throw InvalidOperationException SelfServiceCreationDisabled (409) — Auth enforces the same independently |
| `Application/Features/Subscriptions/Handlers/SubscribeToPlanHandler.cs` | Paid plans → 409 PaidPlanRequiresPaymentLink. Trial allowed only for a legacy clinic with **no subscription history at all** (keeps decision 3; new clinics always have history) |
| `Application/Features/Subscriptions/Handlers/GetSubscriptionPlansHandler.cs` | Trial AlreadyUsed = true whenever the clinic has any subscription history |
| `Api/appsettings.json` and `appsettings.Development.json` | Placeholder sections (see Configuration) |
| `Web.Client/Pages/Doctor/ClinicLanding.razor` | Replace the create card with "Request a new clinic" for every user. Show the user's open request, if any, as a status card: pending review, "Pay now" button when awaiting payment, "Setting up your clinic" when provisioning, "Open clinic" when active. Data loads once on page load, with a manual Refresh button and no polling loop, per the cost rule |
| `Web.Client/Layout/DoctorPortalLayout.razor` | Nav item "Register Clinic" becomes "Request Clinic" → `/clinic-requests` |
| `Web.Client/Pages/Doctor/DoctorRegisterClinic.razor` | Replaced by a redirect to `/clinic-requests/new` (route kept so old links work) |
| `Web.Client/Pages/Subscription/SubscriptionPlans.razor` | Paid plan: the button becomes "Get payment link", which calls CreateRenewalPaymentLink. It shows the pending link with a "Pay now" button that opens in a new tab and an "I've paid, refresh status" button. A Payment history table is loaded once. The Trial card is shown only if the legacy rule allows it |
| `Web.Client/Clients/ISubscriptionApiClient.cs` + `SubscriptionApiClient.cs` | CreateRenewalPaymentLinkAsync, GetPaymentsAsync |
| `Web.Client/Program.cs` | Register IOnboardingApiClient |
| `Web.Client/Components/Subscription/SubscriptionBanner.razor` | "Renew now" link to /subscription for Clinic Admins when expiring soon |

### New Web.Client files

| File | Purpose |
|------|---------|
| `Clients/IOnboardingApiClient.cs` + `OnboardingApiClient.cs` | Submit, GetMine, GetById, Cancel |
| `Pages/Onboarding/ClinicRequestForm.razor` (`/clinic-requests/new`) | opd-* form, client-side required/length checks mirroring validators; blocked with a message when an open request exists |
| `Pages/Onboarding/MyClinicRequests.razor` (`/clinic-requests`) | opd-table of the user's requests with status badges. Actions: Cancel (through ConfirmDialog) when Submitted, "Pay now" when AwaitingPayment, "Open clinic" when Active, and "Submit a new request" when the latest request is terminal and unsuccessful. When opened from Razorpay's post-payment redirect, which is `Razorpay:CallbackUrl`, the page calls check-payment once for the awaiting request and shows "Confirming your payment". A manual Refresh button re-reads the status, with no polling loop. After Active, "Open clinic" refreshes the token so the new clinic appears in `app_ids`; verify the existing refresh path during execution |

---

## API Endpoints

| Method | Route | Body | Response | Auth |
|--------|-------|------|----------|------|
| POST | `api/onboarding/requests` | SubmitClinicOnboardingRequest | Guid (201) | RequireAuthenticated |
| GET | `api/onboarding/requests/mine` | — | IEnumerable\<ClinicOnboardingRequestDto\> | RequireAuthenticated |
| GET | `api/onboarding/requests/{id:guid}` | — | ClinicOnboardingRequestDto (own only, else 404) | RequireAuthenticated |
| POST | `api/onboarding/requests/{id:guid}/cancel` | — | 204 | RequireAuthenticated (owner) |
| POST | `api/onboarding/requests/{id:guid}/check-payment` | — | 202 (409 unless AwaitingPayment; 429 if called within 30 s) | RequireAuthenticated (owner) |
| POST | `api/subscriptions/payments/links` | CreateRenewalPaymentLinkRequest | SubscriptionPaymentDto (201) | RequireClinicAdmin |
| GET | `api/subscriptions/payments` | — | IEnumerable\<SubscriptionPaymentDto\> | RequireClinicAdmin |
| POST | `api/payments/razorpay/webhook` | raw JSON + signature/event-id headers | 200 / 401 | AllowAnonymous + HMAC |
| POST | `api/internal/onboarding/requests/{id:guid}/approve` | ApproveClinicOnboardingRequest | 204 | InternalService |
| POST | `api/internal/onboarding/requests/{id:guid}/reject` | RejectClinicOnboardingRequest | 204 | InternalService |
| POST | `api/internal/onboarding/requests/{id:guid}/resend-payment-link` | AdminActionRequest | 204 | InternalService |
| POST | `api/internal/onboarding/requests/{id:guid}/change-amount` | ChangePaymentAmountRequest | 204 | InternalService |
| POST | `api/internal/onboarding/requests/{id:guid}/retry` | RetryClinicOnboardingRequest | 204 | InternalService |
| POST | `api/internal/workflow-tasks/{id:guid}/retry` | AdminActionRequest | 204 (409 unless task is Failed) | InternalService |
| POST | `api/Clinic` *(existing)* | — | 409 SelfServiceCreationDisabled in Approval mode | RequireAuthenticated |
| POST | `api/subscriptions/subscribe` *(existing)* | — | paid plan → 409; Trial only for legacy clinics | RequireClinicAdmin |

The Admin portal **reads** requests, payments, tasks and webhook events straight from the database (read-only) and calls only the six internal endpoints above to change anything. The workflow-task retry covers failures with no onboarding request behind them, such as a renewal payment whose ActivateSubscription task failed.

**Idempotency of internal calls:** approve, reject and retry are guarded by state, so a repeated call after a timeout returns 409 rather than acting twice. The Admin portal then re-reads the row to show the true outcome. Resend-payment-link intentionally sends another email each time.

---

## Validation Rules

| Command | Field | Rules |
|---------|-------|-------|
| SubmitClinicOnboarding | ClinicName, DoctorName, Address, City, State | NotEmpty, MaxLength per entity table |
| | PreferredClinicCode | NotEmpty, 3–20, regex letters/digits/hyphen |
| | RequesterPhone, ClinicContactNumber | NotEmpty (phone only), MaxLength(20), digits/+/space/hyphen |
| | Pincode | NotEmpty, exactly 6 digits |
| | OfficialEmail | optional, valid email, MaxLength(256) |
| | MedicalRegistrationNumber, MedicalCouncil | NotEmpty, MaxLength(50/150) |
| | ExpectedStaffCount | 1–500 |
| | ReferralSource, Notes, Website | optional, MaxLength(100/1000/256) |
| | UserId | NotEmpty |
| Approve | Id, AdminUserId | NotEmpty; AdminEmail NotEmpty + email; SubscriptionPlanId NotEmpty unless GrantTrial; ClinicCode optional same rule as PreferredClinicCode; ReviewNote MaxLength(1000) |
| Approve (paid plan) | Amount | Required. At least `Onboarding:MinPaymentAmount` (default 1.00, Razorpay's minimum). At most `Onboarding:MaxPaymentAmount` (default 500000) to catch typos. At most 2 decimal places |
| Approve (paid plan) | AmountReason | Required and 3–500 chars when Amount differs from the plan price; otherwise optional, MaxLength(500) |
| ChangePaymentAmount | Amount | Same rules as Approve |
| ChangePaymentAmount | Reason | NotEmpty, 3–500 chars; admin fields as above |
| Reject | Reason | NotEmpty, MaxLength(1000); admin fields as above |
| Retry | ClinicCode | optional, same code rule; admin fields |
| CreateRenewalPaymentLink | ApplicationId, SubscriptionPlanId, ActingUserId | NotEmpty |
| RecordPaymentWebhook | RawBody | NotEmpty, MaxLength(65536); Signature, EventId NotEmpty, MaxLength(128/100) |

---

## Business Rules

1. Self-serve clinic creation is disabled in Approval mode. App and Auth both enforce this independently.
2. A user has at most one open onboarding request, and terminal requests never reopen.
3. Only approved requests can have a payment link. An onboarding link charges the admin-approved amount, and a renewal link charges the plan price. Amounts are always set server-side, and the doctor's browser never supplies one.
3a. Only one payment per request can be payable at a time. A replacement link is created only after the previous link is confirmed cancelled at Razorpay.
4. A paid link that turns out to belong to a rejected or cancelled request is never auto-provisioned. It is flagged for a manual refund.
5. Provisioning always uses ProvisioningReference = request id, so Auth can create at most one clinic per request.
6. There is exactly one ClinicSubscription per payment and at most one per onboarding request, enforced by unique indexes.
7. A renewal bought during an active term starts the day after it ends. This is the existing rule.
8. Payment confirmation always re-checks Razorpay and never trusts webhook payload amounts alone.
9. Existing clinics and subscriptions are untouched. The only change for them is that paid plans now go through payment links.
10. Every failure is either retried automatically or flagged NeedsAttention with a reason. No code path swallows an error without doing one of those.

---

## Configuration (placeholders only in checked-in files)

| Key | Placeholder / default | Notes |
|-----|----------------------|-------|
| `Razorpay:KeyId` | "rzp_test_REPLACE_ME" | sandbox key id |
| `Razorpay:KeySecret` | "" | sandbox secret |
| `Razorpay:WebhookSecret` | "" | set when creating the webhook in Razorpay |
| `Razorpay:ApiBaseUrl` | "https://api.razorpay.com/v1/" | |
| `Razorpay:Currency` | "INR" | |
| `Razorpay:PaymentLinkExpiryDays` | 7 | |
| `Razorpay:CallbackUrl` | "https://localhost:REPLACE/clinic-requests" | where Razorpay redirects after payment (display only; never trusted) |
| `Razorpay:NotifyByEmail` / `NotifyBySms` | true / true | Razorpay's own link notifications |
| `AuthInternalApi:BaseUrl` | same as AuthApi:BaseUrl | |
| `AuthInternalApi:ServiceKey` | "" | **must equal** Auth's `InternalApi:ServiceKey` |
| `InternalApi:AdminPortalKey` | "" | the Admin portal sends this in `X-Internal-Service-Key` |
| `Onboarding:Mode` | "Approval" | "SelfServe" reserved for a later phase |
| `Onboarding:OpsNotificationEmail` | "" | where "new request" alerts go |
| `Onboarding:MinPaymentAmount` / `MaxPaymentAmount` | 1.00 / 500000 | bounds for admin-entered amounts |
| `Workflow:Enabled` | true | |
| `Workflow:PollIntervalSeconds` / `BatchSize` / `LeaseMinutes` / `MaxAttempts` | 10 / 20 / 5 / 72 | |
| `Workflow:ReconciliationIntervalMinutes` | 15 | |

---

## Multi-Tenancy Checklist

- [ ] ClinicOnboardingRequest is user-scoped: every user query filters by RequestedByUserId from the token and never trusts a body user id
- [ ] SubscriptionPayment renewal endpoints use GetApplicationId(); history filtered by applicationId
- [ ] Internal endpoints are unreachable without the InternalService scheme; no X-Application-Id involved
- [ ] Webhook and background tasks derive ApplicationId only from stored rows
- [ ] ClinicSubscription created by the worker uses the ApplicationId returned by Auth for that request's ProvisioningReference
- [ ] Blazor renewal calls send X-Application-Id via the existing client handling

---

## EF Migration

**Migration name:** `AddClinicOnboardingWorkflow` — creates ClinicOnboardingRequests, SubscriptionPayments, WorkflowTasks, PaymentWebhookEvents; adds three columns + indexes to ClinicSubscriptions.

Print, do not auto-run:
```
dotnet ef migrations add AddClinicOnboardingWorkflow --project src/TenantCore.Infrastructure --startup-project src/TenantCore.Api --output-dir Persistence/ClinicMigrations
```

---

## Implementation Order

1. Shared enums, DTOs, error codes
2. Domain enums, exception, 4 entities, ClinicSubscription changes
3. Domain repository interfaces
4. Infrastructure EF configurations; ClinicSubscription config change
5. Infrastructure repositories
6. ClinicDbContext — 4 DbSets
7. Application service interfaces (IPaymentGateway, IAuthProvisioningService) and workflow contracts
8. Infrastructure Razorpay gateway, Auth provisioning client, options
9. Infrastructure workflow processor, reconciliation service, internal auth handler
10. Infrastructure + Application DI; Program.cs auth scheme; resilience package
11. Application commands, queries, validators, translator, email templates
12. Application task handlers, self-healer
13. Application handler modifications (CreateClinic, SubscribeToPlan, GetSubscriptionPlans)
14. API controllers; guard exempt prefixes; appsettings placeholders
15. Web.Client clients, pages, layout/landing/subscription changes
16. Unit tests (below) and fix existing tests affected by rule changes
17. Print migration command

---

## Test Files to Create

Under `tests/TenantCore.Application.Tests/Features/`:

| File | Covers |
|------|--------|
| `Onboarding/Commands/SubmitClinicOnboardingHandlerTests.cs` | Happy path saves the request and enqueues 2 emails in one save. An open request already existing returns 409. Requester snapshot comes from the command |
| `Onboarding/Commands/CancelClinicOnboardingHandlerTests.cs` | Covers: the owner cancels a Submitted request, a non-Submitted request returns 409, another user's request returns 404 |
| `Onboarding/Commands/CheckOnboardingPaymentHandlerTests.cs` | Owner in AwaitingPayment gets ConfirmPayment enqueued; a second call within 30 s is rejected; wrong status returns 409; another user's request returns 404 |
| `Onboarding/Commands/ChangePaymentAmountHandlerTests.cs` | Covers: with no link yet, the old payment is superseded and a new link is enqueued; with a link, CancelPaymentLink is enqueued and no replacement link yet; the request points at the replacement; an unchanged amount returns 409; a Paid payment or a wrong status returns 409; a missing request returns 404 |
| `Onboarding/Commands/ApproveClinicOnboardingHandlerTests.cs` | Covers: a paid plan creates a Pending payment for the admin's amount, not the plan price, and stores PlanListPrice and AmountReason, plus a CreatePaymentLink task, and a trial grant moves the request to Provisioning with a ProvisionClinic task. Also: a code override is stored, and approval returns 409 when the request is not Submitted, the plan is inactive, or payments are unconfigured for a paid plan |
| `Onboarding/Commands/RejectClinicOnboardingHandlerTests.cs` | Rejects from each allowed state; enqueues CancelPaymentLink only when a link exists; email enqueued |
| `Onboarding/Commands/RetryClinicOnboardingHandlerTests.cs` | Failed task reset + attention cleared; code override applied; no failed task → self-healer enqueues next step |
| `Onboarding/Tasks/CreatePaymentLinkTaskHandlerTests.cs` | Covers: the happy path, an already-created link, duplicate-reference adoption, a transient error that propagates, and a permanent error that throws PermanentWorkflowException |
| `Onboarding/Tasks/ConfirmPaymentTaskHandlerTests.cs` | Covers: the paid path for onboarding and for renewal, an amount mismatch that is Permanent and flags attention, a link that is not paid yet so it is Transient and retried, and a rejected request that flags a refund. Idempotent on repeat |
| `Onboarding/Tasks/ProvisionClinicTaskHandlerTests.cs` | Covers: success stores the ApplicationId and enqueues activation, already-provisioned skips the call, a code conflict is Permanent, an Auth 5xx is Transient, and the approved code overrides the preferred one |
| `Onboarding/Tasks/ActivateSubscriptionTaskHandlerTests.cs` | Covers: trial and paid activation, an existing subscription for the payment being linked rather than duplicated, the renewal start-date rule, and the request becoming Active with a ClinicReady email enqueued |
| `Onboarding/Tasks/ProcessWebhookEventTaskHandlerTests.cs` | paid / expired / cancelled / unknown events |
| `Onboarding/Tasks/CancelPaymentLinkTaskHandlerTests.cs`, `SendEmailTaskHandlerTests.cs` | Rejection: success, already cancelled, and a paid link raising refund attention. Change amount: a successful cancel supersedes the old payment and enqueues the replacement link. An old link found already paid is kept and confirmed, and the replacement is cancelled without a link being created. Email payload rendering |
| `Onboarding/Validators/ApproveAndChangeAmountValidatorTests.cs` | Amount at 0.99, 1.00, max and max + 0.01; 3 decimal places rejected; reason required only when the amount differs from the list price, with 2/3/500/501-char boundaries |
| `Onboarding/Commands/RetryWorkflowTaskHandlerTests.cs` | A Failed task is reset and the related request's attention is cleared. A non-Failed task returns 409 and a missing task returns 404 |
| `Onboarding/Services/OnboardingSelfHealerTests.cs` | Correct next task per state; no duplicate when open task exists |
| `Onboarding/Validators/*Tests.cs` | Every rule's boundary (at-limit passes, over-limit fails), regex cases, empty Guids |
| `Onboarding/Translators/ClinicOnboardingTranslatorTests.cs` | All fields; friendly status text per status |
| `Subscriptions/Commands/CreateRenewalPaymentLinkHandlerTests.cs` | Covers: a paid plan succeeds, Trial returns 409, an existing open link is returned instead of a new one, and cross-tenant isolation |
| `Subscriptions/Commands/RecordPaymentWebhookHandlerTests.cs` | Bad signature → 401; new event stored + task enqueued; duplicate → success, nothing enqueued |
| *(modify)* `Subscriptions/Commands/SubscribeToPlanHandlerTests.cs`, `Queries/GetSubscriptionPlansHandlerTests.cs` | New paid-plan rejection and legacy-trial rule |

Under `tests/TenantCore.Infrastructure.Tests/`:

| File | Covers |
|------|--------|
| `BackgroundJobs/WorkflowTaskProcessorTests.cs` | Covers: backoff schedule and jitter bounds, a transient error rescheduling the task, a permanent error failing it and flagging attention, exhausted attempts failing it, a lease conflict skipping the task, and handler dispatch by type |
| `ExternalServices/RazorpayPaymentGatewayTests.cs` | Webhook HMAC accepted and tampered cases, status-code-to-classification mapping, and duplicate-reference detection, all via a fake HttpMessageHandler |
| `ExternalServices/AuthProvisioningServiceTests.cs` | Service key header sent, no bearer token; response classification |
| `ExternalServices/InternalServiceKeyAuthenticationHandlerTests.cs` | Correct key → authenticated; wrong/missing key → fail |

---

## Sandbox Setup Notes (for the user)

1. In the Razorpay Dashboard, switch to **Test Mode** and generate API keys. Put KeyId and KeySecret into `appsettings.Local.json`, or into `appsettings.Development.json` if you prefer.
2. Create a webhook to `https://<api-host>/api/payments/razorpay/webhook` with the events `payment_link.paid`, `payment_link.expired` and `payment_link.cancelled`, and a secret that you copy into `Razorpay:WebhookSecret`. For localhost, use a tunnel such as ngrok or Dev Tunnels. Without a webhook, the 15-minute reconciliation still completes payments, only more slowly.
3. Turn on **auto-capture** in the Razorpay payment settings.
4. Generate one random value, put it in both App `AuthInternalApi:ServiceKey` and Auth `InternalApi:ServiceKey`, and a different random value in App `InternalApi:AdminPortalKey`.
5. Set `Onboarding:OpsNotificationEmail` to the inbox that should hear about new requests.
6. Until the Admin portal exists, test approvals by calling the internal approve endpoint from Swagger in Development, with the admin key header.

---

## Open Questions / Risks

- **Hosting on a virtual machine:** the background processor needs the App process to stay running. Run the API as a Windows Service or a systemd unit with automatic restart on failure and at boot. After any restart, work resumes automatically, since nothing is lost, only delayed.
- **Shared keys on the virtual machine (decision 6):** until the Entra ID move, keep the service keys out of checked-in appsettings. Put them in `appsettings.Local.json` or machine-level environment variables that only the service account can read. Use a long random value of at least 32 bytes, serve both APIs over HTTPS only, and restrict Auth's `api/internal/*` in the VM firewall or reverse proxy to the App server's IP address.
- **Email deliverability:** the retries cover the send API failing, but not a spam folder. Razorpay's own email and SMS notification is a second channel for the payment link.
- **Refunds** remain manual in the Razorpay dashboard. The system only flags when one is needed.
- **Admin authentication:** the App trusts the Admin portal's service key and the admin id and email it sends. Authenticating individual admins belongs in the Admin plan.
- **Checked-in secret:** `appsettings.json` already contains a real-looking Azure Storage key. It should be rotated and moved to local settings. This is flagged only, not fixed.
- **Registry overlap:** this extends `clinic-subscription-gating` and reverses its "activation is immediate, no payment" and "trial picked by the Clinic Admin" decisions. The registry's note that email is deferred to an Azure Function applied to subscription reminders only. Onboarding emails are sent by the App's workflow processor.
- **Planned `auth-consolidation-monolith`:** if executed first, the provisioning client becomes an in-process call. The idempotency design stays the same.
