# ADR: Endpoint Integration Hardening

**Date**: 2026-04-23
**Status**: Proposed
**Repo**: TenantCore.App

## Context

A full audit was performed comparing every controller endpoint, MediatR handler, Blazor API client method, and UI page against each other. The goal was to verify that every backend route has a working UI path and that nothing is broken, missing, or insecure.

Four defects were found. All are in TenantCore.App. TenantCore.Auth has no defects — its controllers, commands, services, and translators are fully wired.

## Decisions

### 1. Add `[Authorize]` to `TenantsController`

**Decision**: Add `[Authorize]` at the class level, mirroring the existing pattern on `ApplicationController`.

**Rationale**: Without this attribute, all five tenant endpoints (`GET /api/tenants`, `GET /api/tenants/{id}`, `POST`, `PUT`, `DELETE`) are accessible to any unauthenticated HTTP client. The UI pages are protected by `AuthorizedLayout`, but the API itself is completely open — a direct `curl` or Swagger call bypasses all front-end guards.

**Alternatives considered**: Per-action attributes — rejected because all actions require authentication equally; class-level is simpler and consistent with `ApplicationController`.

---

### 2. Fix pagination re-fetch in `TenantList.razor`

**Decision**: Use `@bind-Selected:after="LoadTenantsAsync"` on `MudPagination`.

**Rationale**: MudBlazor's `@bind-X:after` callback fires after the bound value changes, which is exactly when a server-side refetch is needed. The current binding only updates `_currentPage` in memory — `LoadTenantsAsync` is never triggered again. The bottom `MudPagination` is meaningless without this fix; users who navigate to page 2+ always see page 1 data.

**Alternatives considered**: `ValueChanged` event with a manual handler — rejected because `@bind-Selected:after` is the idiomatic MudBlazor approach and is already used for `_typeFilter` in `ApplicationList.razor`.

---

### 3. Add 2FA management to `Profile.razor` (Enable + Disable dialogs)

**Decision**: Inline dialogs within `Profile.razor` rather than a separate page or component.

**Rationale**: All three API methods (`EnableTwoFactorAsync`, `ConfirmEnableTwoFactorAsync`, `DisableTwoFactorAsync`) exist in `IAuthApiClient` and are proxied through `AuthController`. The entire API stack is operational but the UI entry point is missing. The Security section of `Profile.razor` is the natural, expected location for this action. Adding a dedicated separate page would introduce unnecessary routing complexity for two simple dialogs.

Enable 2FA is a two-step dialog (initiate → QR shown → OTP confirm) because the backend requires calling `enable` first to generate the TOTP secret and QR, then `confirm` with the OTP to activate it. Disable is a single-step dialog (OTP verification only).

**Alternatives considered**:
- Separate `/auth/two-factor-settings` page — rejected; adds navigation friction and a new route for 3 API calls that logically belong on the profile page
- Reusing `TwoFactor.razor` — rejected; that page is exclusively for the login 2FA flow and is guarded by `AuthState.TempLoginToken`

---

### 4. Remove duplicate `using` in `AuthApiClient.cs`

**Decision**: Delete the second occurrence of `using Microsoft.AspNetCore.Components.WebAssembly.Http;`.

**Rationale**: Duplicate `using` directives are a compiler warning (CS0105) and dead code. Simple cleanup.

---

## Consequences

- **Positive**: Tenant data is now protected at the API layer; pagination works correctly server-side; users can manage 2FA from their profile without leaving the app; code is warning-free.
- **Negative / trade-offs**: None significant. The `[Authorize]` addition means any existing integration tests that hit tenant endpoints without a token will start failing — those tests should be updated to include a bearer token.
- **Migration required**: No
- **Breaking changes**: `TenantsController` endpoints now return 401 for unauthenticated requests instead of 200. This is the correct behavior; it was the previous behavior that was broken.
