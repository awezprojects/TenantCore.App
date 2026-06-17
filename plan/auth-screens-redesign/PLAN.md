# Feature Plan: Auth Screens UI Redesign

**Repo:** TenantCore.App
**Date:** 2026-06-18
**Domain area:** Blazor Client — Authentication UI
**Status:** Approved — ready for execution

---

## Overview

Redesign all six pre-authentication Blazor screens (Login, Forgot Password, Email Verification, Two-Factor QR Setup, Two-Factor OTP Entry, and Error) with a consistent modern split-panel layout. The left panel (45% width, white) renders the form; the right panel (55% width, royal-blue `#1a56db`) displays clinic branding, a decorative circular illustration with orbiting clinic icons, and a tagline. Design language is clean and professional — matching the NovaSyncer reference screenshot but themed for a clinic management cloud platform. No backend changes; this is a pure Blazor client UI change.

---

## Layers Affected

| Layer | Scope of Change |
|-------|----------------|
| Blazor Client | `AuthLayout.razor` redesign + 8 auth page updates |
| Domain | None |
| Infrastructure | None |
| Application | None |
| API | None |
| Shared | None |

No EF migration required. No DI registration changes.

---

## Design System

All auth screens share these design tokens (defined via CSS custom properties on `.auth-shell`):

| Token | Value | Usage |
|-------|-------|-------|
| `--auth-primary` | `#1a56db` | Button fill, links, right panel bg |
| `--auth-primary-dark` | `#1447b3` | Button hover |
| `--auth-primary-light` | `#e8f0fe` | Subtle accent tints |
| `--auth-text` | `#111827` | Headings |
| `--auth-muted` | `#6B7280` | Subtitles, labels |
| `--auth-border` | `#E5E7EB` | Input borders, dividers |
| `--auth-white` | `#FFFFFF` | Left panel bg, right panel text |
| `--auth-ring-a` | `rgba(255,255,255,0.08)` | Outer decorative ring |
| `--auth-ring-b` | `rgba(255,255,255,0.14)` | Middle decorative ring |
| `--auth-ring-c` | `rgba(255,255,255,0.22)` | Inner decorative ring |

MudBlazor `Color.Primary` already maps to `#1a56db` via the existing theme — do not redefine it. Only override where MudBlazor defaults would conflict with the new layout.

---

## Layout Architecture

### AuthLayout.razor — New Shell Structure

The layout renders a full-viewport flex row (`min-height: 100vh`) with no scrolling on the shell itself. Internal panels scroll independently.

**Outer shell** — `div.auth-shell`: `display: flex; flex-direction: row; min-height: 100vh; background: #F9FAFB`

**Left panel** — `div.auth-left`: `flex: 0 0 45%; background: #fff; display: flex; align-items: center; justify-content: center; padding: 3rem 4rem; overflow-y: auto`

- Renders `@Body` directly. No MudGrid/MudItem/MudPaper wrapper in the layout.
- On screens ≤ 768 px: `flex: 0 0 100%` and right panel hidden.

**Right panel** — `div.auth-right`: `flex: 1; background: #1a56db; position: relative; overflow: hidden; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 4rem 3rem`

Right panel internal structure (top to bottom):

1. **Decorative rings** — 3 absolutely positioned concentric circles. Center of rings is horizontally centered, vertically at ~40% panel height.
   - `.auth-ring-outer`: `width: 520px; height: 520px; border-radius: 50%; border: 1px solid var(--auth-ring-a)` absolute, centered
   - `.auth-ring-mid`: `width: 380px; height: 380px; border-radius: 50%; border: 1px solid var(--auth-ring-b)` absolute, centered
   - `.auth-ring-inner`: `width: 240px; height: 240px; border-radius: 50%; border: 1px solid var(--auth-ring-c)` absolute, centered

2. **Illustration hub** (positioned relative, above the tagline) — `div.auth-illustration`: `position: relative; width: 520px; height: 340px; margin-bottom: 3rem`
   - Center hub: white circle `72px × 72px`, `border-radius: 50%`, `background: white`, centered in illustration div. Inside: `MudIcon` of `Icons.Material.Filled.LocalHospital` in `#1a56db`, `font-size: 2.2rem`
   - 3 orbiting icon bubbles (white circle `48px × 48px`, `border-radius: 50%`, `background: white`, `box-shadow: 0 4px 12px rgba(0,0,0,0.15)`). Position using absolute CSS, evenly distributed:
     - Top-left orbit (~`top: 30px; left: 60px`): `Icons.Material.Filled.People` in `#1a56db`
     - Left orbit (~`top: 50%; left: -10px; transform: translateY(-50%)`): `Icons.Material.Filled.Medication` in `#1a56db`
     - Bottom-right orbit (~`bottom: 30px; right: 80px`): `Icons.Material.Filled.CalendarMonth` in `#1a56db`
   - A sample "dashboard card" mock on the right side of the hub: white rounded rectangle `~200px × 160px`, `border-radius: 12px`, `background: white`, `box-shadow: 0 8px 24px rgba(0,0,0,0.12)`. Inside: 3 rows of gray skeleton bars (div with `background: #E5E7EB; border-radius: 4px; height: 10px`) and a small avatar circle per row to simulate a patient list.

3. **Tagline** — `div.auth-tagline`: `text-align: center; z-index: 1; position: relative`
   - `h2.auth-tagline-title`: `color: white; font-size: 1.5rem; font-weight: 700; margin-bottom: 0.5rem` — text: `"Manage Your Clinic Brilliantly."`
   - `p.auth-tagline-sub`: `color: rgba(255,255,255,0.75); font-size: 0.9rem; max-width: 300px; line-height: 1.6` — text: `"Patients, prescriptions, and reports — all in one seamless platform."`

4. **Pagination dots** — `div.auth-dots`: `display: flex; gap: 8px; margin-top: 2rem`
   - 3 `span` elements. First dot: `width: 24px; height: 8px; border-radius: 4px; background: white`. Other two: `width: 8px; height: 8px; border-radius: 50%; background: rgba(255,255,255,0.4)`.

**Responsive breakpoint** (`@media (max-width: 768px)`):
- `.auth-shell`: `flex-direction: column`
- `.auth-right`: `display: none`
- `.auth-left`: `flex: 1; padding: 2rem 1.5rem`

Remove all previous `.auth-background`, `.auth-overlay`, `.auth-container` styles.

MudBlazor provider components (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) remain at the top of the file, unchanged.

---

## Per-Screen Left Panel Design

Each page renders its content in a `div.auth-form-body` (max-width: 400px, width: 100%). The outer `MudGrid`/`MudItem`/`MudPaper` wrapper currently in each page must be **removed**. Pages render directly in `@Body` with no elevation card — the left panel provides the white background.

### Shared left-panel elements across all pages

- **App logo area** — `div.auth-brand`: `display: flex; align-items: center; gap: 10px; margin-bottom: 2rem`
  - Icon bubble: `MudAvatar` `Size.Small` with `Color.Primary` (blue circle) and `MudIcon Icons.Material.Filled.LocalHospital` white
  - App name: `MudText Typo.subtitle1` `font-weight: 700; color: #1a56db` — text: `"ClinicCore"`
- **Page heading** — `MudText Typo.h5` `font-weight: 700; color: #111827; margin-bottom: 0.25rem`
- **Page subtitle** — `MudText Typo.body2` `color: #6B7280; margin-bottom: 2rem`
- **Form fields** — `MudTextField` with `Variant.Outlined` (existing pattern — keep unchanged)
- **Primary button** — `MudButton Variant.Filled Color.Primary FullWidth Size.Large` `height: 48px; text-transform: none; border-radius: 8px; font-weight: 600`
- **Footer link line** — `MudText Typo.body2 Color.Secondary Align.Center` with embedded `MudLink`

---

### 1. Login.razor

**Page heading:** "Log in to your Account"
**Subtitle:** "Welcome back! Enter your credentials to continue."

Left panel body structure (top to bottom):
1. App logo area (shared)
2. Heading + subtitle
3. `EditForm` containing:
   - Email `MudTextField` with email icon adornment (existing)
   - Password `MudTextField` with visibility toggle (existing)
   - Forgot password link — right-aligned `MudLink` `Href="/auth/forgot-password"` `Typo.body2 Color.Primary font-weight: 600` text: "Forgot Password?"
   - Error alert (existing logic, keep `MudAlert Severity.Error`)
   - Sign In button (`MudButton` primary, full-width, height 48px, loading spinner state)
4. Divider line — thin `hr` with `color: #E5E7EB` or `MudDivider`
5. Register line — `MudText Typo.body2 Align.Center`: "Don't have an account? [Create one]"
6. Resend verification line — `MudText Typo.body2 Align.Center`: "Need to verify? [Resend verification]"

Remove: the "OR" separator and MudChip "WebAssembly" badge. Remove `MudAvatar` logo (replaced by brand area). Keep all `@code` logic unchanged.

---

### 2. ForgotPassword.razor

**Page heading:** "Reset Password"
**Subtitle:** "Enter your email and we'll send a reset link."

Left panel body — two states:

**State A (form):**
1. App logo area
2. Heading + subtitle
3. `EditForm`:
   - Email `MudTextField` with email icon adornment
   - Error alert (if `_errorMessage` set)
   - Send Reset Link `MudButton` (full-width, primary, 48px, loading spinner)
4. Back to Login — `MudLink Href="/auth/login"` with left-arrow icon, centered, `Typo.body2 Color.Secondary`

**State B (submitted / success):**
1. App logo area
2. Large success icon — `MudIcon Icons.Material.Filled.MarkEmailRead` `font-size: 4rem; color: #22C55E` centered
3. Heading: "Check Your Email" in `Typo.h5 font-weight: 700`
4. Body text: "If this email is registered, a reset link has been sent. Check your inbox and spam folder."
5. Back to Login `MudButton` (full-width, primary, 48px)

Keep all `@code` logic unchanged.

---

### 3. VerifyEmail.razor

**Page heading (loading state):** "Verifying Email…"
**Page heading (success state):** "Email Verified!"
**Page heading (error/invalid state):** "Verification Failed"

The page shows one of four states based on `_verifying`, `_verified`, `_error` flags. Each state renders:

**Verifying state:**
1. App logo area
2. Centered `MudProgressCircular Size.Large Indeterminate Color.Primary` with 48px margin
3. Heading "Verifying Email…"
4. Subtitle "Please wait while we verify your email address."

**Verified (success) state:**
1. App logo area
2. Centered success check circle — `MudIcon Icons.Material.Filled.CheckCircle` `font-size: 5rem; color: #22C55E`
3. Heading "Email Verified!" `color: #22C55E`
4. Subtitle "Your email is confirmed. You can now sign in."
5. Sign In `MudButton` (full-width, primary, 48px)

**Error state:**
1. App logo area
2. Centered error icon — `MudIcon Icons.Material.Filled.Cancel` `font-size: 5rem; color: #EF4444`
3. Heading "Verification Failed"
4. `MudAlert Severity.Error` with `_errorMessage`
5. Back to Login `MudButton` (full-width, primary, 48px)
6. Resend link below button: `MudLink Href="/auth/resend-verification" Typo.body2 Color.Secondary`

**Invalid (no params) state:**
1. App logo area
2. Warning icon `Icons.Material.Filled.LinkOff` `font-size: 5rem; color: #F59E0B`
3. Heading "Invalid Link"
4. Subtitle "The verification link is invalid or missing required parameters."
5. Back to Login `MudButton`
6. Resend link

Keep all `@code` logic unchanged.

---

### 4. TwoFactor.razor — QR Barcode Setup View

Shown when `AuthState.IsFirstTimeSetup == true`.

**Page heading:** "Set Up Authenticator"
**Subtitle:** "Scan the QR code with Google Authenticator or Authy to get started."

Left panel body:
1. App logo area
2. Heading + subtitle
3. QR code block — white rounded box `MudPaper Elevation.2 Class="pa-4" Style="display:inline-block; border-radius:12px"` containing the `<img>` tag from current code (`data:image/png;base64,...`) `width: 180px; height: 180px`, centered
4. Instruction text below QR: `MudText Typo.caption Color.Secondary Align.Center` — "After scanning, enter the 6-digit code from your app below."
5. OTP `MudTextField` — existing field with 6-digit pin style (`letter-spacing: 0.5rem; font-size: 1.5rem; text-align: center`)
6. Verify Code `MudButton` (full-width, primary, 48px)
7. Back to Login link (text button, `Color.Secondary`, with left-arrow icon)

---

### 5. TwoFactor.razor — OTP Entry View

Shown when `AuthState.IsFirstTimeSetup == false`.

**Page heading:** "Two-Factor Authentication"
**Subtitle:** "Enter the 6-digit code from your authenticator app."

Left panel body:
1. App logo area
2. Heading + subtitle
3. User display — if `AuthState.UserDisplayName` is set: `MudAlert Severity.Info Dense` showing "Signing in as: **{name}**"
4. OTP `MudTextField` — large, centered, `letter-spacing: 0.5rem; font-size: 1.8rem; text-align: center; border-radius: 12px`
5. Error alert if `_errorMessage` set
6. Verify Code `MudButton` (full-width, primary, 48px, loading spinner)
7. Back to Login link

Both views are conditional branches inside the same `TwoFactor.razor`. Keep all `@code` logic unchanged.

---

### 6. Error.razor

**Page heading:** "Something Went Wrong"
**Subtitle:** "An unexpected error has occurred. Please try refreshing or go back to the home screen."

Left panel body:
1. App logo area
2. Large error illustration — `MudIcon Icons.Material.Filled.CloudOff` `font-size: 6rem; color: #EF4444; display: block; text-align: center; margin-bottom: 1rem`
3. Heading + subtitle (centered)
4. Two action buttons in a row (`display: flex; gap: 12px`):
   - Reload: `MudButton Variant.Outlined Color.Primary` with refresh icon — existing `OnClick` logic
   - Go Home: `MudButton Variant.Filled Color.Primary` `Href="/select-clinic"` with home icon
5. Below buttons: `MudText Typo.caption Color.Secondary Align.Center` — "Error code: 500 · Contact support if this persists."

Keep `@inject NavigationManager Navigation` unchanged.

---

### 7. Register.razor (consistency update)

**Page heading:** "Create Your Account"
**Subtitle:** "Start managing your clinic today."

Remove the outer `MudGrid`/`MudItem`/`MudPaper` wrapper. Add app logo area. Keep all form fields, validation, and `@code` logic. Apply the same `div.auth-form-body` wrapper.

---

### 8. ResendVerification.razor (consistency update)

**Page heading:** "Resend Verification Email"
**Subtitle:** "Enter your email address to receive a new verification link."

Remove outer wrapper. Add app logo area. Keep form logic.

---

### 9. ResetPassword.razor (consistency update)

**Page heading:** "Set New Password"
**Subtitle:** "Choose a strong password to protect your account."

Remove outer wrapper. Add app logo area. Keep form logic.

---

## Files to Create

None. No new entities, services, or HTTP clients.

---

## Files to Modify

| File | Change Summary |
|------|---------------|
| `src/TenantCore.Web.Client/Layout/AuthLayout.razor` | Complete redesign: split-panel shell replacing the gradient centered card layout. Includes all right-panel CSS, ring decorations, illustration, tagline, and responsive breakpoint. |
| `src/TenantCore.Web.Client/Pages/Auth/Login.razor` | Remove `MudGrid`/`MudItem`/`MudPaper` wrapper; add brand area; update headings; restructure links; keep all `@code` logic. |
| `src/TenantCore.Web.Client/Pages/Auth/ForgotPassword.razor` | Remove wrapper; add brand area; redesign success state with email icon; keep all `@code` logic. |
| `src/TenantCore.Web.Client/Pages/Auth/VerifyEmail.razor` | Remove wrapper; add brand area; redesign all four states with large status icons; keep all `@code` logic. |
| `src/TenantCore.Web.Client/Pages/Auth/TwoFactor.razor` | Remove wrapper; add brand area; separate QR setup and OTP entry views into clearly distinct UI sections; keep all `@code` logic. |
| `src/TenantCore.Web.Client/Pages/Error.razor` | Remove `MudContainer` wrapper; add brand area; new cloud-off icon illustration; side-by-side action buttons; keep `@code` logic. |
| `src/TenantCore.Web.Client/Pages/Auth/Register.razor` | Consistency update: remove wrapper, add brand area, keep form and logic. |
| `src/TenantCore.Web.Client/Pages/Auth/ResendVerification.razor` | Consistency update: remove wrapper, add brand area, keep form and logic. |
| `src/TenantCore.Web.Client/Pages/Auth/ResetPassword.razor` | Consistency update: remove wrapper, add brand area, keep form and logic. |

---

## CSS Strategy

All shared auth shell CSS (split layout, ring decorations, illustration, dots, responsive) lives in the `<style>` block of `AuthLayout.razor`. This injects globally in Blazor WASM (no CSS isolation file is used for layouts).

Each page may have a small `<style>` block only for page-specific overrides (e.g., TwoFactor QR centering, OTP field letter-spacing). If a style is already defined in `AuthLayout.razor`, do not repeat it in a page.

Do not create a separate `.css` file — the existing project has no precedent for a dedicated auth CSS file.

---

## Implementation Order

Execute in this sequence:

1. `AuthLayout.razor` — must be done first; all other pages depend on the new shell
2. `Login.razor` — primary screen; validate the design end-to-end
3. `ForgotPassword.razor`
4. `VerifyEmail.razor`
5. `TwoFactor.razor`
6. `Error.razor`
7. `Register.razor`
8. `ResendVerification.razor`
9. `ResetPassword.razor`

---

## Test Files to Create

None. This feature contains no business logic, handlers, validators, or translators. All `@code` blocks are existing logic preserved unchanged. ADR-009 test requirements apply only to Application-layer code.

---

## EF Migration

Not required. No database changes.

---

## Multi-Tenancy Checklist

Not applicable. Auth screens are public (unauthenticated); no clinic context is involved.

---

## Open Questions / Risks

- **Register.razor structure unknown:** The file was not read before planning. The execute command should read it before modifying to avoid breaking existing fields or validation logic.
- **ResendVerification.razor structure unknown:** Same — read before modifying.
- **ResetPassword.razor structure unknown:** Same — read before modifying.
- **Right panel illustration complexity:** The orbiting icon bubbles and dashboard card mock use absolute positioning inside a fixed-size container. If MudBlazor renders wrapper divs that interfere with the CSS, switch to plain HTML `<div>` elements for the right panel instead of MudBlazor components.
- **Right panel on very tall narrow screens:** The decorative rings are 520px wide; on narrow tall screens (portrait tablet) they may clip. The `overflow: hidden` on `.auth-right` handles this — verify after implementation.
