# Analysis: Multi-Clinic User Identity & Per-Clinic Role Mapping

**Repos covered:** TenantCore.Auth (identity/authorization source of truth), TenantCore.App (consumer)
**Trigger:** Architectural review of "one login, many clinics, different role per clinic" — requested comparison against GitHub's org/repo access model, and whether this is sound architecture/product and a potential USP.
**Date:** 2026-11-16
**Status:** For review — no code changed as part of this analysis.

---

## 1. Executive Summary

**The model is already built, and the core design is correct.** This is not a proposal — TenantCore.Auth already implements exactly what was described: a single `UserProfile` can hold an active `UserApplication` (membership) in any number of clinics, and an independent `UserRole` (role) per clinic, via `ApplicationId`-scoped junction tables. The GitHub analogy is apt and intentional-looking: it matches GitHub's "one account, org membership per org, role per repo/org" shape, and more precisely mirrors Slack/Google Workspace's "one identity, many workspaces, different role per workspace" model.

Three qualifiers:

1. **The happy path (App's "Add Existing User" UI) is implemented correctly and safely** — global typeahead search returns minimal PII, the admin picks a per-clinic role, and the target user must *accept* an invitation before any mapping is created. Nothing is force-mapped.
2. **Two legacy/parallel endpoints in TenantCore.Auth bypass the per-clinic authorization check entirely** (Section 4) — a real, live Broken Object-Level Authorization (BOLA) gap, not a hypothetical one.
3. **There is no "last owner/admin" protection** for a clinic, and the JWT's flat role claim is a latent footgun for future code (Section 4.3–4.4).

As a product angle: this is a legitimate differentiator for a multi-clinic SaaS, not just an engineering nicety (Section 6). It's also worth knowing that TenantCore.Auth's `Role` table is **already generalized across verticals** (`ApplicationType`: Clinic / School / HR), not clinic-specific — the identity service was architected as a reusable multi-tenant IAM platform from day one (Section 6.3).

---

## 2. Current Architecture (as built)

### 2.1 Data model

```
UserProfile (1) ──────< UserApplication >────── (1) Application  ("clinic")
     │  UserId                UserApplicationId        ApplicationId
     │                        UserId, ApplicationId     ApplicationName
     │                        IsOwner, IsActive          ApplicationType (Clinic/School/HR)
     │
     └──────────────< UserRole >────────────────── (1) Role
              UserRoleId                               RoleId, RoleName
              UserId, ApplicationId, RoleId             ApplicationType (null = global; else scoped to a vertical)
              IsActive
```

- `TenantCore.Auth.Domain/Entities/UserApplication.cs` — membership row: which clinics a user belongs to, `IsOwner`, `IsActive` (soft-removable without losing history).
- `TenantCore.Auth.Domain/Entities/UserRole.cs` — role row: **scoped by `ApplicationId`**, so the same `UserId` can have a `Doctor` `UserRole` row for Clinic B and a `Clinic Admin` `UserRole` row for Clinic A simultaneously. This is the literal answer to "can one user be Admin in A, Doctor in B, Reception in C?" — yes, and the schema already supports an arbitrary number of such rows per user.
- `Role.ApplicationType` (nullable) — roles are catalogued per vertical, not per individual clinic instance. `Clinic Admin`, `Doctor`, `Nurse`, `Receptionist`, `Pharmacist`, `Lab Technician`, `Clinic Manager` are seeded once for `ApplicationType = Clinic` (`...Migrations/20260106080050_SeedApplicationSpecificRoles.cs`) and reused across every clinic tenant. There is no per-clinic custom role.

### 2.2 Token shape

`JwtTokenService.GenerateAccessToken` (`TenantCore.Auth.Application/Services/Implementations/JwtTokenService.cs:66-118`) issues:

| Claim | Content |
|---|---|
| `app_ids` | every `ApplicationId` (clinic) the user belongs to |
| `app_roles` | JSON array `[{appId, appName, roleId, roleName}, ...]` — the per-clinic role list |
| `role` (`ClaimTypes.Role`) | **flattened, distinct role names across every clinic, with no clinic association** |

### 2.3 Enforcement

- TenantCore.App: `ClinicContextMiddleware` reads `X-Application-Id`, cross-checks it against the `app_ids` claim, 403s otherwise (`ADR-008-multi-tenancy.md`). `ClinicRoleAuthorizationHandler` then checks the role **for that one header-selected clinic** against `app_roles` — so `RequireClinicAdmin` passes for Clinic A and fails for the same user on Clinic B, exactly as described.
- TenantCore.Auth: the equivalent is `AppAuthorizeAttribute` (`TenantCore.Auth.API/Attributes/AppAuthorizeAttribute.cs`) — takes `applicationId` from the route (or an `X-ClinicApp-Id` header), checks it's in the user's `app_ids`, then checks the named role against `app_roles` for that specific app id.

### 2.4 The "map an existing user" flow (what was asked about)

This is live, not hypothetical — `TenantCore.App/src/TenantCore.Web.Client/Pages/Admin/UserManagement.razor`:

1. **Search** (`OnSearchEmailChanged`, line ~1077) calls `AuthClient.SearchUsersByEmailAsync` → App's `AuthController.SearchUsersByEmailAsync` → Auth's `GET api/auth/user/search` (`AuthController.cs:241-250`), backed by `SearchUsersByEmailCommand` → `UserService.SearchUsersByEmailAsync`. This is a **global, cross-tenant** search over `UserProfile` by email fragment — by design, the same way GitHub's "add collaborator" search isn't scoped to people already in your org. It returns only `UserId, FirstName, MiddleName, LastName, FullName, EmailId, MobileNo, IsActive` — **no clinic affiliation is disclosed**, so Clinic A's admin cannot see which other clinics a searched user belongs to.
2. **Pick a role, send invite** (`AddExistingUserAsync`, line ~1105) calls `AppClient.InviteExistingUserAsync` → App's `ApplicationController` → Auth's `POST api/Application/invite-existing-user`, guarded by `[AppAuthorize("Admin", "Clinic Admin")]` — i.e. the caller must actually hold an admin role **for that specific `applicationId`**. This creates an `Invitation` row with `IsExistingUser = true, ExistingUserId = <found user>` (`UserService.InviteExistingUserByEmailAsync`, `UserService.cs:675-730`) and emails the found user an accept link.
3. **Acceptance is required** — `AcceptExistingUserInvitationCommand` / `ApplicationService.AcceptExistingUserInvitationAsync` only creates the `UserApplication` + `UserRole` rows once the invited user (holder of the emailed token) accepts. The admin cannot silently attach a role to someone else's account.

**This is the correct shape**: global discovery, no PII leakage of cross-tenant membership, explicit consent via invitation, and the mapping step itself is authorization-checked against the *target* clinic — not just "any authenticated user."

---

## 3. Is the architecture correct? (direct answer)

**Yes, for the core pattern.** A single identity (`UserProfile`) with N independent `(ApplicationId, RoleId)` pairs is the right shape for "one person, many organizations, different role in each." It avoids the two wrong alternatives this is often confused with:

- **Wrong alternative A** — one account per clinic (force the user to register separately at each clinic with the same email under different accounts). This is what the user is correctly avoiding; it was never implemented this way, and good that it isn't — duplicate accounts per email would fragment 2FA, audit trail, and password resets across N copies of the same person.
- **Wrong alternative B** — one global role per user, applied everywhere they have access (e.g. a single `Role` foreign key on `UserProfile`). This was also avoided — `UserRole.ApplicationId` is exactly what prevents "Doctor at Clinic B" from becoming "Doctor everywhere, including Clinic A where they're meant to be Admin."

So the GitHub mental model the user described — "access to different repos [clinics] with different roles [permissions], same account" — is the right one, and it's what's actually built, not a reinterpretation. The remaining question is execution quality, covered next.

---

## 4. Where execution falls short of the design intent

### 4.1 Finding — Unscoped invite endpoint bypasses per-clinic authorization (High)

`TenantCore.Auth.API/Controllers/UserController.cs:31-41` — `POST api/user/invite`:

```csharp
[Authorize(AuthenticationSchemes = "AuthToken")]   // controller-level — ANY authenticated user
public class UserController : ControllerBase
{
    [HttpPost("invite")]
    public async Task<ActionResult<...>> InviteUser(
        [FromServices] IInviteUserCommand command,
        [FromBody] InviteUserRequestModel request)   // ApplicationId + RoleId come from the BODY
        => await command.ExecuteAsync(request, User.GetUserId());
}
```

There is no `[AppAuthorize]` and no check anywhere in `InviteUserCommand` → `UserService.InviteUserAsync` that the caller holds an admin role for `request.ApplicationId`. Compare this to the endpoint that is actually wired up to the UI, `ApplicationController.cs:239` (`POST api/Application/invite-user`), which carries `[AppAuthorize("Admin", "Clinic Admin")]` with `applicationId` taken from the **route**, checked against the caller's own `app_roles` for that id.

**Impact:** any authenticated user — a Receptionist at Clinic C, say — can call `POST api/user/invite` directly with `{ ApplicationId: <Clinic A's id>, RoleId: <Clinic Admin's role id> }` and create a valid invitation to add a brand-new (or existing-but-unmapped) user as Clinic Admin of Clinic A, a clinic they have no relationship to. This directly violates ADR-008's own stated rule ("Do NOT trust the `ApplicationId` from the request body") — the rule exists, this endpoint just doesn't follow it. Not currently called by TenantCore.App's UI (which uses the `ApplicationController` path instead), but Auth is an independently deployed, directly callable service — "not called by this one frontend" is not the same as "not reachable."

### 4.2 Finding — Unscoped activate/deactivate endpoints (High)

`TenantCore.Auth.API/Controllers/AuthController.cs:461-497` — `PATCH api/auth/user/{userId}/activate` and `/deactivate`:

```csharp
[Authorize(AuthenticationSchemes = "AuthToken")]
public async Task<...> DeactivateUser(
    [FromServices] IDeactivateUserCommand command,
    [FromRoute] Guid userId,
    [FromQuery] Guid applicationId)   // query string, no ownership check
    => await command.ExecuteAsync(userId, applicationId, User.GetUserId());
```

Same shape of gap: `applicationId` is attacker-supplied (query string), and nothing checks the caller has an admin role for it. This toggles the target user's `UserApplication.IsActive` for that clinic — i.e. **any authenticated user can deactivate any other user's membership in any clinic**, severing their access. This is distinct from, and bypasses, the correctly-guarded `PATCH api/Application/{applicationId}/users/{userId}/status` (`ApplicationController.cs:403-404`, `[AppAuthorize("Admin", "Clinic Admin")]`) which is the one `ToggleUserApplicationMapping` path the App UI actually drives. TenantCore.App's own `AuthApiClient.cs` (lines ~366-386) already has client wrapper methods pointed at this unscoped pair, so the unsafe surface is reachable from App's own codebase, not just a theoretical direct-API call — worth confirming no Razor page currently wires them up, and removing the wrappers if genuinely dead.

**Root cause pattern for 4.1/4.2:** the per-clinic role check (`AppAuthorize`) was correctly built and correctly applied on the `ApplicationController` family, but two older/parallel endpoints on `UserController`/`AuthController` that perform the same mutations were never retrofitted with it. This looks like incremental feature growth (`ApplicationController` endpoints added later as the "real" admin surface) without deleting or locking down the earlier ones.

### 4.3 Finding — No "last admin" / orphaned-tenant protection (Medium)

No code in `ApplicationService` (searched for owner/last-admin guards) prevents:
- The only `Clinic Admin` at a clinic deactivating their own mapping or being deactivated by another admin-equivalent caller, leaving the clinic with zero administrators.
- `ChangeUserRole` (`ApplicationController.cs:514`, "Deactivates the old role and assigns the new one") being used to demote the last admin to e.g. Receptionist, same effect.

Once a clinic has no active `Clinic Admin`, nobody *inside* that clinic can invite a new one (that action itself requires `AppAuthorize("Admin", "Clinic Admin")`) — only a platform-level TenantCore.Admin intervention (direct DB/Auth action) can recover it. GitHub, Slack, and Google Workspace all special-case this ("cannot remove the last owner" / "must transfer ownership first"); this codebase doesn't yet.

### 4.4 Finding — Flat role claim is a latent authorization footgun (Low today, worth closing)

`JwtTokenService.GenerateAccessToken` (lines 79-84) puts **every distinct role name the user holds across all their clinics** onto the token as plain `ClaimTypes.Role` claims, with no clinic binding — a user who is `Clinic Admin` at Clinic A and `Receptionist` at Clinic B carries both role claims on one token. ASP.NET Core's built-in `User.IsInRole("Clinic Admin")` / `[Authorize(Roles = "Clinic Admin")]` reads exactly this collection. Checked: **no customer-facing endpoint currently uses `[Authorize(Roles = ...)]`** (only the separate platform-admin token path does, which deliberately omits `app_ids` and is a different, single-tenant concept — that usage is fine). So this isn't exploited today, but it's one `[Authorize(Roles = "Clinic Admin")]` away from silently granting Clinic-A-admin-level access to that same user at Clinic B. Worth either dropping the flat claim for customer tokens entirely (nothing currently reads it) or documenting it as "never gate authorization on this claim — use `app_roles` + the active clinic only," so a future contributor doesn't reach for the obvious-looking built-in attribute.

### 4.5 Known, already-logged defects (context, not new findings)

`TenantCore.Auth/docs/testing/OPD_MultiReception_Auth_Defects.md` already tracks, as of 07-Nov-2026, open items adjacent to this feature: the existing-user invite email still renders as a raw key/value dump rather than the real template (cross-check against root `CLAUDE.md` rule 9, which states template rendering was fixed 2026-11-01 — worth a quick re-verify, since the two dates are close and may simply reflect the QA doc predating the fix), `DEF-UM-04` (HTML injected into an invited user's name rendering unescaped), an expired-invite/duplicate-row issue, and join/created timestamps with no UTC offset. None of these are specific to the cross-clinic *authorization* model, but they sit in the same code paths this analysis covers.

---

## 5. Direct comparison to the GitHub mental model

| GitHub concept | TenantCore equivalent | Match quality |
|---|---|---|
| One GitHub account | One `UserProfile` (one email) | Exact match |
| Org/repo membership | `UserApplication` (`ApplicationId` = clinic) | Exact match |
| Role per repo (read/write/admin) | `UserRole` scoped by `ApplicationId` | Exact match |
| Search by username, invite as collaborator | Email typeahead → `invite-existing-user` → acceptance required | Exact match, including the consent step |
| Can't see a user's other orgs when inviting them | Search response has no clinic/app data | Exact match |
| Can't remove the last org owner without transferring ownership | **No equivalent — Finding 4.3** | Gap |
| Fine-grained custom roles per repo (Enterprise) | Roles are global per `ApplicationType`, not per clinic instance | Intentional simplification, not a defect — flag only if product wants per-clinic custom roles later |
| Org-level audit log of membership/role changes | Generic `ActionLoggingBehavior`/`AuthActionLogs` (ADR-011) — logs the action, not obviously queryable "show me every role change for this clinic" in Admin's Log Explorer today | Partial — worth a one-line check against the Admin Log Explorer's actual filters before calling this closed |

Conclusion: the shape is right. The gaps are in authorization-check coverage consistency (4.1, 4.2), lifecycle edge cases (4.3), and claim hygiene (4.4) — not in the fundamental data model or the primary UI flow.

---

## 6. Product perspective — is this a USP?

**Yes, conditionally.** Multi-clinic staff is a real, common pattern in this domain — locum/visiting doctors, multi-branch clinic groups under one owner, pharmacists or lab techs contracting across sites, a clinic manager who also sees the books at a sister clinic. A competitor that forces a new account (and a new password, a new 2FA enrollment, a new "forgot password" history) per clinic membership is measurably worse UX for exactly this user, and this product already doesn't do that.

Three things make it a stronger USP than "we also support multi-tenancy" generically:

1. **No per-seat billing friction.** Per the workspace's own `CLAUDE.md` (cross-repo rule), billing/wallet/usage is PAYG-only — there's no seat-based subscription tier where adding the same person to a third clinic triggers a cost or a plan-limit conversation. GitHub/Slack/Workspace all have to manage "does inviting this person to another org consume a seat" as a real pricing/product problem; this platform's billing model sidesteps it entirely for this specific feature. That's worth saying explicitly in sales/positioning material, not just leaving as an accident of the billing design.
2. **Consent-based, not admin-forced.** The invitation-must-be-accepted step (Section 2.4.3) is a trust signal worth keeping and marketing: a clinic admin cannot silently attach someone's real identity to their clinic without that person agreeing.
3. **Auth is already vertical-agnostic.** Section 6.3 below.

### 6.1 What's missing to make this a *marketable* feature, not just a working mechanism

- A **self-service view** for the end user: "You're a member of: Clinic One Care (Doctor), Riverside Clinic (Receptionist)" with a way to leave a clinic themselves, not just be removed by an admin. Didn't find one in `UserManagement.razor` or elsewhere — the flow today is entirely admin-initiated after the invite is accepted. Worth checking whether a "my clinics" switcher page already covers this (the App's `/clinics` page referenced in the QA defect doc does something adjacent — a "Joined" dashboard — but that's a landing page, not a membership-management page).
- **4.3's last-admin protection** — this is also a trust/support-cost issue, not just a security one: a clinic that locks itself out of its own admin role becomes a support ticket, not just a bug report.
- An explicit **"switch clinic" UX moment** that's fast and clearly communicates which hat you're wearing right now (role for the active clinic) — important because the whole value proposition collapses if a user is confused about which role applies to which screen they're looking at.

### 6.2 Product risk to flag, not just an upside

The flip side of "any clinic can invite any existing user" is that it also means **any clinic admin can discover whether a given email address is already a registered user** of the platform at all (search returns real matches vs. empty), even if they can't see where. That's a mild user-enumeration surface, standard for this kind of global-search pattern (GitHub has the same property) and generally an acceptable trade-off — flagging it so it's a conscious decision, not an unconsidered one, especially since this is a clinical/healthcare context where even "is this email a patient-facing identity on the platform" can be sensitive in some jurisdictions.

### 6.3 A bigger strategic observation

The `Role` seed data (`SeedApplicationSpecificRoles.cs`) defines full role catalogs for `ApplicationType = Clinic`, `School`, and `HR Management` — this identity service was **already designed to be reused across verticals**, not built clinic-first and later generalized. Combined with the cross-clinic-mapping pattern this analysis is about, TenantCore.Auth is closer to a standalone **multi-tenant IAM product** (one identity, membership + role in any number of independent tenant organizations, across different product verticals) than to "the login screen for the clinic app." Whether or not there's current appetite to productize Auth separately, it's worth the team being aware that's the shape it's already in — because decisions made for clinic-only convenience (e.g. hard-coding "clinic" terminology into a shared service) would be actively working against an asset that already has that optionality.

---

## 7. Recommendations

**Security (do before relying on this further in production):**
1. Add `[AppAuthorize("Admin", "Clinic Admin")]` (with `applicationId` sourced from the route, not the body) to `UserController.InviteUser` (4.1), or delete the endpoint if the `ApplicationController` path has fully superseded it.
2. Add the same scoping to `AuthController.ActivateUser`/`DeactivateUser` (4.2), or delete them + their `AuthApiClient.cs` wrappers if genuinely unused.
3. Treat any endpoint that accepts an `applicationId` from the request body or query string instead of an authorization-checked route value as a review flag — this pattern repeated twice; worth a quick audit pass across the rest of `AuthController`/`UserController` for the same shape.

**Correctness/robustness:**
4. Add a "last admin" guard to deactivation, mapping-toggle, and role-change paths for a clinic's admin role — refuse the action (or require designating a successor first) if it would leave the clinic with zero active `Clinic Admin`/owner.
5. Either stop emitting the flat `ClaimTypes.Role` claim for customer tokens (nothing currently reads it) or add a code comment at the emission site warning future contributors not to gate authorization on it.

**Product (if leaning into this as a USP):**
6. Build a self-service "my clinics" membership view with a user-initiated "leave this clinic" action.
7. Make the active-clinic context (whose role applies right now) persistently visible in the App UI, not just inferred from the last-selected header value.
8. Consider whether per-clinic custom roles (beyond the fixed catalog) matter for any target customer segment before committing to the current "shared role catalog per vertical" ceiling — currently a reasonable scope limit, not a defect.

**Logging/audit (per root `CLAUDE.md` rule 8, already mandatory):**
9. Confirm the Admin Log Explorer can actually answer "show me every membership/role change for clinic X" and "show me every clinic user Y has ever been added to or removed from" — both are the kind of question this feature will generate support/compliance requests for.

---

## 8. What was verified vs. assumed

Verified by reading code directly: entity shapes (`UserApplication`, `UserRole`, `Role`, `Invitation`, `UserProfile`), the full search→invite→accept flow end to end across both repos, the JWT claim generation, both the correctly-scoped and the unscoped endpoint families, the role seed data, and the absence of last-admin protection (grepped `ApplicationService.cs` for owner/last-admin logic — none found).

Not verified (would need a running environment or further digging, flagged above rather than assumed): whether the Admin Log Explorer's current filters actually support clinic-scoped or user-scoped membership-change queries; whether the email-template rendering defect noted 07-Nov-2026 is still live given root `CLAUDE.md`'s 2026-11-01 fix note; whether any Razor page still actively calls the two unscoped `AuthController` activate/deactivate client methods.
