# Security Analysis: Endpoint Integration Hardening

**Date**: 2026-04-23
**Analyst**: Claude
**Plan**: plan/endpoint-integration-hardening/PLAN.md

## Summary

Overall risk level: **Medium**. The most critical finding — unauthenticated access to all tenant endpoints — is resolved by this feature's `[Authorize]` fix. However, two residual concerns remain in `TenantsController`: there are no role-based restrictions (any authenticated user can delete any tenant) and no maximum page-size cap on the paginated list endpoint. The 2FA management additions are well-structured, using existing validated DTOs and proxied through the established auth chain. The pagination and duplicate-`using` fixes carry no security implications.

## Findings

### CRITICAL (must fix before merge)

None. The pre-existing critical vulnerability (unauthenticated tenant access) is resolved by this feature.

### HIGH (should fix before merge)

- **[H1]** TenantsController: No role restriction on destructive operations
  - **Location**: `src/TenantCore.Api/Controllers/TenantsController.cs` — `DELETE /api/tenants/{id}`, `POST /api/tenants`, `PUT /api/tenants/{id}`
  - **Risk**: Any authenticated user — including Viewer or Staff roles — can create, update, or delete any tenant. A low-privilege user can destroy all tenant records.
  - **Fix**: Add `[Authorize(Policy = AuthPolicies.RequireAdmin)]` to `Delete`, and `[Authorize(Policy = AuthPolicies.RequireManagement)]` to `Create` and `Update`. `GET` endpoints can remain at class-level `[Authorize]`. Use constants from `TenantCore.Shared.Authorization.AuthorizationConstants`.

- **[H2]** TenantsController: No maximum page-size cap on `GET /api/tenants`
  - **Location**: `src/TenantCore.Api/Controllers/TenantsController.cs` `GetAll` action; `src/TenantCore.Application/Features/Tenants/Queries/GetTenantsQuery.cs`
  - **Risk**: A caller can pass `pageSize=99999`, forcing the database to scan and return the entire tenants table in one response — a cheap denial-of-service vector against the database and API memory.
  - **Fix**: In `GetTenantsHandler`, clamp page size before the repository call: `var safePageSize = Math.Min(pageSize, 100);`. Alternatively add a FluentValidation rule `RuleFor(x => x.PageSize).InclusiveBetween(1, 100)` on a `GetTenantsQueryValidator`.

### MEDIUM (fix in follow-up)

- **[M1]** TenantsController: No ownership or tenant-isolation check
  - **Location**: `UpdateTenantHandler`, `DeleteTenantHandler`
  - **Risk**: Authenticated user A can update or delete a tenant that belongs to user B — there is no check verifying the requesting user has a relationship to the tenant being modified.
  - **Fix**: Add a `CreatedBy` or `OwnerId` field to the `Tenant` entity and enforce ownership in the handler, or document that tenants are system-level resources managed only by admins (in which case H1's `RequireAdmin` policy is the correct mitigation).

- **[M2]** Profile.razor 2FA: No re-fetch of user profile after state change
  - **Location**: `src/TenantCore.Web.Client/Pages/Auth/Profile.razor` — `ConfirmEnable2faAsync`, `Disable2faAsync`
  - **Risk**: After enabling or disabling 2FA, `_isTwoFactorEnabled` is updated locally but `AuthState.CurrentUser.IsTwoFactorEnabled` is not refreshed from the server. If the user navigates away and back, the displayed status may be stale until they log out and in again.
  - **Fix**: After a successful enable/disable, call `AuthClient.GetUserByIdAsync(AuthState.CurrentUser.UserId)` and `AuthState.UpdateUserProfile(response.Data)` to sync the canonical user state.

### LOW / Informational

- **[L1]** AuthController is class-level `[AllowAnonymous]`
  - **Location**: `src/TenantCore.Api/Controllers/AuthController.cs`
  - **Risk**: Informational only. The controller is an intentional proxy; TenantCore.Auth enforces its own authorization on protected downstream routes. The `ForwardAsync` method copies the `Authorization` header so protected Auth endpoints still receive the bearer token.
  - **Action**: None required. Consider adding a comment documenting the intentional `[AllowAnonymous]` to prevent future developers from adding `[Authorize]` by mistake.

- **[L2]** 2FA Enable/Disable dialogs do not enforce numeric OTP in the UI
  - **Location**: `Profile.razor` — `_enable2faOtp` and `_disable2faOtp` text fields
  - **Risk**: Cosmetic only — server-side DTOs validate format (`^\d{6}$`). Non-numeric input is rejected server-side.
  - **Fix**: Add `InputType="InputType.Number"` or `Pattern="[0-9]{6}"` to the OTP `MudTextField` components for improved UX.

- **[L3]** TenantList.razor search fires only on Enter, not debounced on input
  - **Risk**: Informational. Not a security issue; consistent with existing search patterns in the codebase.
  - **Action**: None required for this feature.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PARTIAL | `[Authorize]` added to TenantsController (fixes critical); role-based restrictions on write/delete still missing (H1) |
| Input Validation | PARTIAL | 2FA DTOs validated server-side; no max pageSize cap on tenant list (H2) |
| Data Access & Injection | PASS | EF Core parameterized queries throughout; no raw SQL in changed files |
| Sensitive Data Handling | PASS | QR code delivered over authenticated channel; OTP never logged; no secrets in response models |
| Business Logic Security | PARTIAL | No ownership check on tenant mutations (M1); 2FA UI state sync gap (M2) |
| TenantCore.App Specific | PASS | Named `"AuthApi"` HttpClient used; auth failures surface as snackbar errors; correlation header forwarded |
| OWASP Top 10 | PARTIAL | A01 improved but not complete (role restrictions); A03 passes; A07 passes (JWT validation unchanged) |

## Recommended Actions

1. **(HIGH — before merge)** Add `[Authorize(Policy = AuthPolicies.RequireAdmin)]` to `Delete` and `[Authorize(Policy = AuthPolicies.RequireManagement)]` to `Create`/`Update` in `TenantsController`.
2. **(HIGH — before merge)** Clamp `pageSize` to a maximum of 100 in `GetTenantsHandler` or via a `GetTenantsQueryValidator`.
3. **(MEDIUM — follow-up)** Decide and document the ownership model for tenants. If admin-only, H1 fully covers it. If ownership-based, add `OwnerId` to the `Tenant` entity and check it in handlers.
4. **(MEDIUM — follow-up)** After successful 2FA enable/disable in `Profile.razor`, call `GetUserByIdAsync` and `AuthState.UpdateUserProfile` to keep canonical user state current.
5. **(LOW — polish)** Add `InputType="InputType.Number"` to the OTP `MudTextField` components in the new 2FA dialogs.

## Approval

- [ ] All CRITICAL findings resolved (none exist — ✅)
- [ ] All HIGH findings resolved or have accepted risk noted (H1 and H2 outstanding — ❌)
- [ ] Ready to merge (resolve H1 and H2 first — ❌)
