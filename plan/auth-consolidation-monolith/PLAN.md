# Feature Plan: Auth Consolidation — Single Monolith (App absorbs Auth)

**Repo:** TenantCore.App (host) — TenantCore.Auth becomes frozen reference source
**Date:** 2026-07-21
**Domain area:** Identity / Authentication / Authorization (cross-cutting)
**Status:** Approved — ready for execution

---

## 1. Summary

Collapse the two-service topology (TenantCore.Auth microservice + TenantCore.App)
into a **single deployable ASP.NET Core application** hosted by `TenantCore.Api`.
All of Auth's functionality — registration, login, JWT issuance, 2FA, password
management, email verification, refresh tokens, sessions, invitations, roles/RBAC,
applications, clinics, and the email-notification publisher — moves into the App
solution **as-is, inside its own projects**, and its controllers are re-hosted in
`TenantCore.Api` under **identical routes**.

**Decisions locked by the user:**
- Single deployment; TenantCore.App is the core/only repo going forward.
- TenantCore.Auth repo is **left untouched** (frozen — reference/rollback only).
- **Single database** for now.

**Guiding principle:** *relocate the boundary, don't dissolve it.* Auth code is
brought in as separate projects with its **own DbContext** and **its own EF
migrations-history table** pointed at the same database. This satisfies "single DB
now" while keeping the seam clean enough that a future re-split into a standalone
service is a connection-string change, not a data-untangling project.

**Why this over a rewrite:** Auth uses a different internal style than App
(layered Command→Service→Repository, manual validation, `ApiResponseModel<T>`,
AutoMapper, no MediatR). Rewriting 44 commands + 10 services + the JWT/2FA
machinery into App's CQRS/MediatR pattern is high-risk, high-effort, and directly
contradicts "everything works as-is, no contract change." We therefore **preserve
Auth's architecture inside its own projects** as a deliberate, documented exception
to App's MediatR convention. Zero rewrite → zero behavioral regression risk.

---

## 2. Analysis

### 2.1 How the two services talk today

There are **two independent consumers** of Auth from the App side:

| Consumer | Location | Calls | Base URL source |
|----------|----------|-------|-----------------|
| **Server-side** (App API → Auth API) | `TenantCore.Infrastructure/ExternalServices/AuthApplicationService.cs` + `AuthClinicService.cs` | `api/Application/*`, `api/Clinic/*` | named HttpClient `"AuthApi"`, `AuthApi:BaseUrl` (Program.cs) |
| **Client-side** (Blazor WASM → Auth API) | `TenantCore.Web.Client/Clients/AuthApiClient.cs` | `api/auth/*`, `api/user/*` | `AuthApiClient` typed HttpClient, `AuthApiBaseUrl` config (Web.Client Program.cs) |

### 2.2 The "only the base URL changes" claim — verdict: **largely correct**, with one honest nuance

Your intuition holds **if** the Auth controllers are re-hosted in the App API under
the same route paths. Evidence found in the code:

- **Client-side is already wired for it.** `Web.Client/Program.cs` line 28:
  `var authApiBaseUrl = builder.Configuration["AuthApiBaseUrl"] ?? tenantApiBaseUrl;`
  The Blazor auth client **already falls back to the App's own API origin**.
  Point `AuthApiBaseUrl` at the App (or drop the setting) and the client hits the
  re-hosted Auth controllers — **config only, zero code change.** ✅
- **Server-side is also config-only.** `AuthApplicationService`/`AuthClinicService`
  keep working verbatim if `AuthApi:BaseUrl` is pointed at the App's **own origin
  (loopback)**. Same contracts, same `ApiResponseModel<T>` bodies, same status
  semantics. ✅

**The nuance (must be stated, not hidden):** pointing `AuthApi:BaseUrl` at the app
itself is a **loopback HTTP hop** — the process calls its own HTTP port. It works
and is the lowest-risk first step, but it still carries HTTP serialization + a
network round-trip to self. The *architecturally cleaner* end state converts those
server-side calls to **in-process** interface calls. We treat that as an **optional
Phase 2** so Phase 1 exactly matches your "only base URL" expectation and ships fast.

### 2.3 JWT / token validation — the one genuinely non-trivial merge

| Concern | Auth service today | App today | Merged target |
|---------|-------------------|-----------|---------------|
| Secret config key | `Jwt:SecretKey` | `Jwt:Secret` | **One key.** Keep `Jwt:Secret`; set Auth's reader to the same value (they are already required to be identical per workspace rule). |
| Schemes | `AuthToken` + `TempToken` (dual) | default `Bearer` (single) | **Three registered schemes:** `Bearer` (App's default, unchanged), `AuthToken`, `TempToken`. All share the one secret. |
| Issuer / Audience | `ApplicationAuth` / `ApplicationAuthUsers` (+ `TempLogin`) | validated only if configured | Register `AuthToken`/`TempToken` with Auth's issuer/audience values; leave App's default scheme as-is (it already accepts Auth-issued tokens today, which is how login works). |
| Role claim | — | `RoleClaimType="role"`, per-clinic via `app_roles` claim + `ClinicRoleAuthorizationHandler` | Unchanged — App policies keep working. |

Because App **already validates tokens Auth issues today** (that's the current login
flow), the shared secret + issuer/audience are already compatible. The merge just
means **registering the two extra named schemes** in App's `AddJwtAuthentication`
so the `[Authorize(AuthenticationSchemes = "AuthToken")]` / `"TempToken"` attributes
on the Auth controllers resolve.

### 2.4 Header-name mismatch (preserve, do not "fix")

- App's `ClinicContextMiddleware` reads **`X-Application-Id`**.
- Auth's `AppAuthorizeAttribute` + `ICurrentUserService` read **`X-ClinicApp-Id`**.

`AuthApplicationService` already injects `X-ClinicApp-Id` when forwarding admin
calls. Under loopback this still works end-to-end. **Do not unify these headers in
Phase 1** — it would be an out-of-scope behavioral change. Note it and move on.

### 2.5 Registry check

No overlap conflict. The App registry has no identity/auth domain area; Auth's
registry lists all pre-existing features being relocated. This is a structural/
platform migration, not a new business feature, so it does not duplicate any
executed feature. **Flag:** this plan intentionally sits outside the normal
single-entity App feature template because it is a multi-domain platform merge.

### 2.6 Entra ID vs. manual auth (decision record)

Decision: **keep the manual TenantCore.Auth system.** Rationale captured for the
record: Entra External ID would force a full re-wire of login/2FA/invitation flows,
a remap of the `Application`/`Role`/`Feature` RBAC model onto Entra app roles, and a
forced password reset for every existing user (custom hashes can't be imported).
Under a launch timeline with a solo developer, that is net-negative. The existing
system gives full control at zero marginal cost. Entra can be added later as an
*additional* SSO option without removing what exists, if a customer ever requires it.

---

## 3. Gaps (GPS — what must be resolved during execution)

| # | Gap | Resolution in this plan |
|---|-----|------------------------|
| G1 | App has no `AuthToken`/`TempToken` JWT schemes registered | Add both named schemes in `AddJwtAuthentication` (§6, step 6) |
| G2 | Two DbContexts over one DB will collide on `__EFMigrationsHistory` | Give Auth's context a distinct history table `__AuthMigrationsHistory` (§6, step 4) |
| G3 | Auth secret key name differs (`Jwt:SecretKey` vs `Jwt:Secret`) | Standardize on `Jwt:Secret`; update Auth's Program-equivalent registration reader | 
| G4 | Auth controllers depend on `AppAuthorizeAttribute`, `RequestLoggingMiddleware`, `ICurrentUserService`, Auth's Swagger security defs | Bring `AppAuthorizeAttribute` + `ICurrentUserService` across; middleware optional; App Swagger already has Bearer def |
| G5 | Auth's notification publisher is Azure Service Bus (email triggers) | Bring `INotificationPublisher` + `ServiceBusNotificationPublisher` + `AzureServiceBus` config into App (§7) |
| G6 | Auth registers its own `HttpContextAccessor`, AutoMapper, CORS | Reconcile with App's existing registrations (App already has `AddHttpContextAccessor`); register Auth AutoMapper profiles additively |
| G7 | Refresh-token + logout use HttpOnly cookies via `Request`/`Response` | Works unchanged in-process/loopback; verify CORS `AllowCredentials` + same-origin so cookies flow |
| G8 | Existing Auth data (users, roles, applications, hashed passwords, active sessions) must survive | Single-DB: point Auth context at App's DB; run Auth's existing migrations into that DB (creates Auth tables alongside clinic tables). See §8 data strategy. |
| G9 | Two `Application` concepts: Auth `Application` (Guid Id, tenant) vs App multi-tenancy keyed by `X-Application-Id` (same Guid) | These are the **same identifier** already (App's `ApplicationId` == Auth's application Guid). No remap needed — keep as-is. |

---

## 4. Issues & Risks

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Rewriting Auth into MediatR introduces regressions | High (if attempted) | **Do not rewrite.** Import Auth projects as-is. |
| JWT scheme misconfiguration locks everyone out | High | Keep App's default `Bearer` scheme untouched; add named schemes additively; test login + a protected App endpoint + a protected Auth endpoint before cutover. |
| Migrations-history collision drops/duplicates tables | High | Distinct history table for Auth context (G2). Never point Auth migrations at App's history. |
| Loopback HTTP self-call deadlock/port issues | Medium | Use the app's actual bound origin; alternatively Phase 2 in-process removes the hop entirely. |
| Cookie auth (refresh/logout) breaks cross-origin | Medium | Same-origin after merge (Blazor + API + Auth all one host) — actually *improves*; verify CORS/credentials. |
| Service Bus connection string missing in App config | Medium | Add `AzureServiceBus:*` to App config; email triggers no-op/throw without it. |
| Namespace/assembly clashes (`Application`, `ApplicationService`) between repos | Medium | Auth types keep `TenantCore.Auth.*` namespaces — no clash with `TenantCore.*`. |
| Single-DB makes future re-split costlier | Low (accepted) | Separate DbContext + history table keeps the seam; documented trade-off. |
| Auth's `ApiResponseModel` always returns HTTP 200 | Low | Preserved — App's `AuthApplicationService` already parses the body's `Success` field. |

---

## 5. Auth Feature & Contract Inventory (complete — nothing omitted)

### 5.1 Entities relocated (10) → new `AuthDbContext` (same DB)

| Entity | Purpose |
|--------|---------|
| UserProfile | User identity, password hash, email-verification + 2FA fields |
| Application | Tenant/application record (Guid Id == App's `ApplicationId`) |
| UserApplication | User↔Application mapping (active/deactivated) |
| Role | RBAC role (per application type) |
| Feature | Permission/feature unit |
| RoleFeature | Role↔Feature mapping |
| UserRole | User↔Role assignment (per application) |
| RefreshToken | Rotating refresh tokens |
| ActiveSession | Session tracking for logout-all |
| Invitation | Full invitation lifecycle (new + existing user) |

### 5.2 Services relocated (10)

AuthenticationService, JwtTokenService, PasswordService, RefreshTokenService,
TwoFactorAuthService (TOTP via Otp.NET + QRCoder), UserService, ApplicationService,
RoleManagementService, ClinicService, CurrentUserService.

### 5.3 Commands relocated (44)

All files under `TenantCore.Auth.Application/Commands/Implementations/` — imported
unchanged. (Registration, Login, VerifyEmail, ResendEmailVerification,
ForgotPassword, ResetPassword, ChangePassword, Enable/Confirm/Disable/ValidateTwoFactor,
RefreshToken, Logout, LogoutAll, GetUserById, UpdateUserProfile, SearchUsersByEmail,
Activate/DeactivateUser, all Application CRUD + user-mapping + role + invitation
commands, CreateClinic, GetDoctorClinicDashboard, GetRolesByApplicationType.)

### 5.4 Endpoint contracts (re-hosted verbatim — routes unchanged)

**`AuthController` — `api/Auth`**

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| POST | `register` | Anonymous | `ApiResponseModel<UserProfileResponseModel>` |
| POST | `login` | Anonymous | `ApiResponseModel<LoginResponseModel>` (temp token if 2FA) |
| GET | `verify-email?userId&verificationCode` | Anonymous | `ApiResponseModel` |
| POST | `resend-email-verification` | Anonymous | `ApiResponseModel` |
| POST | `reset-password` | Anonymous | `ApiResponseModel` |
| POST | `accept-invitation` | Anonymous | `ApiResponseModel<UserProfileResponseModel>` |
| GET | `accept-existing-invitation?token` | Anonymous | `ApiResponseModel` |
| POST | `forgot-password` | Anonymous | `ApiResponseModel` |
| POST | `refresh` | Anonymous (cookie) | `ApiResponseModel<LoginResponseModel>` |
| GET | `user/{userId}` | AuthToken | `ApiResponseModel<UserProfileResponseModel>` |
| GET | `user/search?email` | AuthToken | `ApiResponseModel<List<UserSearchResponseModel>>` |
| POST | `logout` | AuthToken | `ApiResponseModel` |
| POST | `logout-all` | AuthToken | `ApiResponseModel` |
| POST | `change-password/{userId}` | AuthToken | `ApiResponseModel` |
| PUT | `user/{userId}/profile` | AuthToken | `ApiResponseModel<UserProfileResponseModel>` |
| PATCH | `user/{userId}/activate?applicationId&modifiedBy` | AuthToken | `ApiResponseModel` |
| PATCH | `user/{userId}/deactivate?applicationId&modifiedBy` | AuthToken | `ApiResponseModel` |
| POST | `2fa/enable/{userId}` | AuthToken | `ApiResponseModel<EnableTwoFactorResponseModel>` |
| POST | `2fa/confirm/{userId}` | AuthToken | `ApiResponseModel` |
| POST | `2fa/disable/{userId}` | AuthToken | `ApiResponseModel` |
| POST | `2fa/validate-login` | **TempToken** | `ApiResponseModel<LoginResponseModel>` |

**`UserController` — `api/User`** (`[Authorize(AuthToken)]`)

| Method | Route | Response |
|--------|-------|----------|
| POST | `invite?invitedBy` | `ApiResponseModel<InvitationResponseModel>` |

**`ApplicationController` — `api/Application`** (`[Authorize(AuthToken)]`, several `[AppAuthorize(...)]`)

| Method | Route | Response |
|--------|-------|----------|
| POST | `?ownerId` | `ApiResponseModel<ApplicationResponseModel>` |
| PUT | `{applicationId}` | `ApiResponseModel<ApplicationResponseModel>` |
| GET | `by-code/{code}` | `ApiResponseModel<ApplicationResponseModel>` |
| GET | `{applicationId}` | `ApiResponseModel<ApplicationResponseModel>` |
| GET | `get-all` | `ApiResponseModel<List<ApplicationResponseModel>>` |
| GET | `by-type/{applicationType}` | `ApiResponseModel<List<ApplicationResponseModel>>` |
| GET | `{applicationId}/users` | `ApiResponseModel<List<ApplicationUserResponseModel>>` |
| GET | `{applicationId}/users/deactivated` | `ApiResponseModel<List<ApplicationUserResponseModel>>` |
| POST | `invite-user?invitedBy` | `ApiResponseModel<InvitationResponseModel>` |
| POST | `invite-existing-user?invitedBy` | `ApiResponseModel` |
| DELETE | `{applicationId}` | `ApiResponseModel` |
| POST | `{applicationId}/users/{userId}/assign?roleId&assignedBy` | `ApiResponseModel` |
| POST | `{applicationId}/users/{userId}/mapping?assignedBy` | `ApiResponseModel` |
| DELETE | `{applicationId}/users/{userId}?removedBy` | `ApiResponseModel` |
| PATCH | `{applicationId}/users/{userId}/status?modifiedBy` | `ApiResponseModel` |
| PATCH | `{applicationId}/status?modifiedBy` | `ApiResponseModel` |
| PUT | `{applicationId}/users/{userId}/role?modifiedBy` | `ApiResponseModel` |
| GET | `{applicationId}/invitations` | `ApiResponseModel<List<InvitationResponseModel>>` |
| DELETE | `{applicationId}/invitations/{invitationId}` | `ApiResponseModel` |
| POST | `{applicationId}/invitations/{invitationId}/reinvite?reinvitedBy` | `ApiResponseModel` |

**`RoleController` — `api/Role`**

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `by-application/{applicationId}` | `[AppAuthorize("Admin","Clinic Admin")]` | `ApiResponseModel<GetRolesByApplicationTypeResponseModel>` |

**`ClinicController` — `api/Clinic`** (`[Authorize(AuthToken)]`)

| Method | Route | Response |
|--------|-------|----------|
| POST | `` (create) | `ApiResponseModel<ApplicationResponseModel>` |
| GET | `dashboard` | `ApiResponseModel<List<ClinicDashboardItemResponseModel>>` |

### 5.5 Security features carried over (unchanged)

- **Password hashing** — `PasswordService` (existing algorithm; never returns hash).
- **JWT issuance** — `JwtTokenService`; dual audience (AuthToken/TempToken).
- **2FA / TOTP** — `TwoFactorAuthService` (Otp.NET), QR via QRCoder; temp-token
  gate between password step and OTP step. `TempToken` endpoints keep explicit
  `[Authorize(AuthenticationSchemes = "TempToken")]`.
- **Refresh tokens** — rotation + HttpOnly cookie; `RefreshToken` entity.
- **Sessions** — `ActiveSession`; logout / logout-all revocation.
- **Email triggers** — `INotificationPublisher` → Service Bus queue for: email
  verification, resend verification, forgot/reset password, invitation (new +
  existing user), reinvite. Consumed by the external email worker (unchanged).
- **Never-return rule** — `TwoFactorSecret` and `PasswordHash` never serialized.

---

## 6. Plan Approach — Structural steps

> No new App-domain entity is created. This is a project-import + host-wiring merge.
> Follow this order to keep the solution compiling at each step.

**Step 1 — Bring Auth projects into the App solution (as-is).**
Copy these four projects into the App repo and add them to the App `.sln`:
`TenantCore.Auth.Domain`, `TenantCore.Auth.Application`, `TenantCore.Auth.Infrastructure`,
`TenantCore.Auth.Models`. Keep namespaces `TenantCore.Auth.*` (no clash with `TenantCore.*`).
Do **not** import `TenantCore.Auth.API` as a second web host — its controllers move in Step 3.

**Step 2 — Add project references.**
`TenantCore.Api` references `TenantCore.Auth.Application`, `TenantCore.Auth.Infrastructure`,
`TenantCore.Auth.Models` (and transitively `.Domain`). Add NuGet packages Auth needs
that App lacks: `Otp.NET`, `QRCoder`, `Azure.Messaging.ServiceBus`, AutoMapper.

**Step 3 — Move the 6 Auth controllers + supporting API bits into `TenantCore.Api`.**
Copy `AuthController`, `UserController`, `ApplicationController`, `RoleController`,
`ClinicController`, (`HomeController` optional) into `TenantCore.Api/Controllers/Auth/`.
Also copy `AppAuthorizeAttribute` (+ its `ICurrentUserService` dependency, which lives
in Auth.Application). Routes stay identical (`api/Auth`, `api/User`, `api/Application`,
`api/Role`, `api/Clinic`).
⚠ **Route-collision check:** App may already expose an `ApplicationsController` or
similar. Auth's is `api/Application` (singular). Verify no existing App controller
owns `api/Application`, `api/Auth`, `api/User`, `api/Role`, `api/Clinic` before
cutover; if a collision exists, that is the *only* place a route may need adjusting
(and the corresponding client base path).

**Step 4 — Wire Auth's DbContext to the single database with an isolated history table.**
In App's infrastructure DI, register `ApplicationAuthDbContext` with
`options.UseSqlServer(DefaultConnection, sql => sql.MigrationsHistoryTable("__AuthMigrationsHistory"))`.
Same connection string as `ClinicDbContext`; separate history table (G2).

**Step 5 — Register Auth's DI in App's `Program.cs`.**
Call Auth's `AddApplicationServices()` and `AddInfrastructureServices(configuration)`
(from the imported projects) after App's `AddApplication()`/`AddInfrastructure()`.
Reconcile duplicates: `AddHttpContextAccessor` already present in App — keep one.
Register Auth AutoMapper profiles additively.

**Step 6 — Consolidate JWT (the critical step).**
In `AddJwtAuthentication`, **keep the existing default `Bearer` scheme untouched**,
then **add** two named JWT bearer schemes using the same `Jwt:Secret` key:
- `"AuthToken"` — issuer `Jwt:Issuer` (ApplicationAuth), audience `ApplicationAuthUsers`.
- `"TempToken"` — issuer, audience `TempLogin`.
Update Auth's config reader to use `Jwt:Secret` (drop `Jwt:SecretKey`). Confirm
`Jwt:Issuer`/`Jwt:Audience` in App config match what `JwtTokenService` stamps.

**Step 7 — Point base URLs at the app itself (your "only base URL" change).**
- Server-side: set `AuthApi:BaseUrl` = the app's own origin (loopback). No code change
  to `AuthApplicationService`/`AuthClinicService`.
- Client-side: set `AuthApiBaseUrl` = App origin, or remove it (already falls back to
  `TenantApiBaseUrl`). No change to `AuthApiClient`.

**Step 8 — Bring over email-notification + Service Bus.**
Register `INotificationPublisher → ServiceBusNotificationPublisher` (Singleton, as in
Auth). Add `AzureServiceBus:ConnectionString` + `AzureServiceBus:QueueName` to App
config. Email-trigger behavior is then identical.

**Step 9 — Retire the separate Auth deployment.**
Stop deploying `TenantCore.Auth.API`. The TenantCore.Auth repo stays frozen. Remove
the now-defunct external Auth base-URL from production config once loopback verified.

**Step 10 — Optional Phase 2 (later, not required for launch): in-process calls.**
Replace `AuthApplicationService`/`AuthClinicService` HTTP bodies with direct calls to
the imported Auth command/service interfaces, dropping the loopback hop. Contracts to
the Blazor client stay identical. Defer until after a stable launch.

---

## 7. Files to Create / Move / Modify

### Move into App (from Auth, unchanged)

| Source (TenantCore.Auth) | Destination (TenantCore.App) |
|--------------------------|------------------------------|
| `TenantCore.Auth.Domain` project | App solution (referenced) |
| `TenantCore.Auth.Application` project | App solution (referenced) |
| `TenantCore.Auth.Infrastructure` project | App solution (referenced) |
| `TenantCore.Auth.Models` project | App solution (referenced) |
| 5–6 controllers | `TenantCore.Api/Controllers/Auth/` |
| `AppAuthorizeAttribute` | `TenantCore.Api/Authorization/` (or `/Attributes/`) |
| EF migrations (10 files under Auth.Infrastructure/Migrations) | travel with the Infrastructure project |

### Modify (App)

| File | Change |
|------|--------|
| `TenantCore.App.sln` | Add the 4 imported Auth projects |
| `src/TenantCore.Api/TenantCore.Api.csproj` | Reference imported Auth projects + add Otp.NET, QRCoder, Azure.Messaging.ServiceBus, AutoMapper |
| `src/TenantCore.Api/Program.cs` | Call Auth `AddApplicationServices()` + `AddInfrastructureServices()`; register `AuthDbContext`; register `INotificationPublisher`; keep single `AddHttpContextAccessor` |
| `src/TenantCore.Api/Extensions/ServiceCollectionExtensions.cs` | Add named `AuthToken` + `TempToken` JWT schemes (§6 step 6) |
| `src/TenantCore.Api/appsettings*.json` | Add `Jwt:Issuer`/`Jwt:Audience`/temp values, `AzureServiceBus:*`; set `AuthApi:BaseUrl` to loopback |
| Auth Infrastructure DI (imported) | DbContext → `MigrationsHistoryTable("__AuthMigrationsHistory")`; secret reader → `Jwt:Secret` |
| `src/TenantCore.Web.Client/wwwroot/appsettings*.json` | Set/remove `AuthApiBaseUrl` (point at App origin) |

### Unchanged (verify only)

`AuthApplicationService.cs`, `AuthClinicService.cs`, `AuthApiClient.cs` — no code
edits in Phase 1; behavior driven entirely by base-URL config.

---

## 8. Database Strategy (single DB)

- **One database, two DbContexts:** `ClinicDbContext` (21 clinic DbSets) and
  `ApplicationAuthDbContext` (10 auth DbSets) both point at `DefaultConnection`.
- **Separate migrations history:** Auth context uses `__AuthMigrationsHistory`;
  clinic context keeps default `__EFMigrationsHistory`. They never touch each other.
- **Bringing existing Auth data across:** run Auth's existing migrations against the
  App database (creates the 10 Auth tables next to the clinic tables). If there is
  live Auth data in a separate Auth DB today, migrate rows with a one-time data copy
  (SQL `INSERT ... SELECT` across databases or a `bacpac`/generate-scripts export);
  hashed passwords, 2FA secrets, sessions, and invitations copy verbatim — no
  re-hashing, no forced password reset.
- **Startup migration:** App already health-checks SQL Server; add Auth-context
  `Database.Migrate()` (guarded by pending-migrations check, mirroring Auth's current
  Program.cs) so Auth tables are created/updated on boot.
- **Future re-split cost (accepted):** because the contexts and history tables are
  separate, re-splitting later = create a new DB, point `ApplicationAuthDbContext` at
  it, copy the 10 tables. No schema untangling. This is the insurance the separate
  context buys you at zero cost today.

---

## 9. Security Checklist

- [ ] `AuthToken` + `TempToken` schemes registered; App default `Bearer` untouched.
- [ ] One secret (`Jwt:Secret`) drives all three schemes; `Jwt:SecretKey` removed.
- [ ] `TempToken` endpoints keep explicit `[Authorize(AuthenticationSchemes="TempToken")]`.
- [ ] `PasswordHash` / `TwoFactorSecret` never serialized (preserved from Auth).
- [ ] Refresh/logout cookies flow same-origin; CORS `AllowCredentials` verified.
- [ ] `AppAuthorizeAttribute` + `ICurrentUserService` imported and resolvable.
- [ ] `X-ClinicApp-Id` (Auth) vs `X-Application-Id` (App) header split preserved, not merged.
- [ ] Service Bus connection string present; email triggers fire.
- [ ] Secrets live in `appsettings.Local.json` / environment, never committed.

---

## 10. Verification / Cutover Test Plan

Before retiring the Auth deployment, on the merged app confirm:

1. **Register → verify-email → login (no 2FA)** issues an `AuthToken`.
2. **Login with 2FA** → `TempToken` → `2fa/validate-login` → full `AuthToken`.
3. **Protected App endpoint** (e.g. patients) works with the Auth-issued token.
4. **Protected Auth endpoint** (`api/Application/get-all`) works with same token.
5. **Clinic-admin invite flow** (Blazor → App `ApplicationApiClient` → loopback Auth
   controller) creates an invitation and fires the Service Bus email message.
6. **Refresh + logout + logout-all** operate via cookies same-origin.
7. **Role-gated endpoints** (`[AppAuthorize]`, and App's `RequireClinicAdmin`) both
   enforce correctly with the merged claims.
8. Auth tables present in the App DB under `__AuthMigrationsHistory`; clinic tables
   untouched.

---

## 11. Open Questions / Notes for Executor

- **Route collision** (Step 3 ⚠) is the single most likely blocker — check App for any
  existing `api/Application`, `api/Auth`, `api/User`, `api/Role`, `api/Clinic` owner
  before cutover. This is the only place a contract might need to move.
- **Phase 2 (in-process)** is optional and explicitly out of scope for launch.
- **This plan deliberately departs from App's MediatR/CQRS convention** for the
  imported Auth code — that code keeps its own layered style inside its own projects.
  Document this as an accepted exception in the App ADRs post-merge.
- TenantCore.Auth repo remains frozen; no edits there per user decision.
```
