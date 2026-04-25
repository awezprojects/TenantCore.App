# Feature Plan: Endpoint Integration Hardening

## Repo
TenantCore.App

## Overview
Full audit of all Auth and Application controller endpoints against their Blazor UI counterparts revealed 4 confirmed defects: one security vulnerability (unauthenticated access to all tenant data), one broken pagination interaction, one missing 2FA management UI (the three API methods exist but are never called from any page), and one duplicate `using` directive. This plan addresses every defect with precise, targeted changes — no new files needed.

## Domain Area
`Mixed` — touches Tenants (backend + UI), Auth/Profile (UI), and Infrastructure client code.

---

## Files to Create

None. All changes are modifications to existing files.

---

## Files to Modify

| File | Change |
|------|--------|
| `src/TenantCore.Api/Controllers/TenantsController.cs` | Add `[Authorize]` class-level attribute |
| `src/TenantCore.Web.Client/Clients/AuthApiClient.cs` | Remove duplicate `using` on line 2 |
| `src/TenantCore.Web.Client/Pages/Tenants/TenantList.razor` | Wire `MudPagination` `@bind-Selected:after` to `LoadTenantsAsync` |
| `src/TenantCore.Web.Client/Pages/Auth/Profile.razor` | Add 2FA enable/disable buttons + two dialogs + backing @code |

---

## API Endpoints

All endpoints already exist. This plan only fixes integration defects — no new routes.

---

## Defect Details & Exact Fixes

---

### FIX 1 — `TenantsController.cs`: Missing `[Authorize]`

**Problem:** `TenantsController` has no `[Authorize]` attribute. Every CRUD endpoint (`GET /api/tenants`, `POST`, `PUT`, `DELETE`) is publicly accessible without a JWT token.

**Fix:** Add `[Authorize]` between `[Produces]` and the class declaration:

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]                          // ADD THIS LINE
public class TenantsController(ISender sender) : ControllerBase
```

Required using: `using Microsoft.AspNetCore.Authorization;` — check if already present in the file's usings block; add only if missing.

---

### FIX 2 — `AuthApiClient.cs`: Duplicate `using` directive

**Problem:** Lines 1 and 2 both read:
```
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
```

**Fix:** Delete line 2 (the exact duplicate). The final top of the file must have the statement exactly once.

---

### FIX 3 — `TenantList.razor`: Pagination does not re-fetch from server

**Problem:** `MudPagination @bind-Selected="_currentPage"` updates `_currentPage` in state but never triggers `LoadTenantsAsync()`. Clicking any page other than page 1 shows the same data.

**Fix:** Change the `MudPagination` element from:
```razor
<MudPagination Count="_result.TotalPages" @bind-Selected="_currentPage"
               ShowFirstButton="true" ShowLastButton="true" />
```
to:
```razor
<MudPagination Count="_result.TotalPages" @bind-Selected="_currentPage"
               @bind-Selected:after="LoadTenantsAsync"
               ShowFirstButton="true" ShowLastButton="true" />
```

No other changes to this file.

---

### FIX 4 — `Profile.razor`: Add 2FA management UI

**Problem:** The Security section shows a chip (`Enabled`/`Disabled`) but provides no button to change the state. Three `AuthApiClient` methods are wired and proxied but never called from any UI component:
- `EnableTwoFactorAsync(Guid userId)` — initiates setup, returns `ApiResponse<EnableTwoFactorResponseDto>` with `QrCodeBase64`
- `ConfirmEnableTwoFactorAsync(Guid userId, ValidateTwoFactorRequestDto)` — confirms OTP to complete setup
- `DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequestDto)` — disables with OTP verification

#### 4a — Replace the 2FA row in the Security section

Current markup (inside `<MudStack Spacing="3">`):
```razor
<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
    <MudStack Spacing="0">
        <MudText Typo="Typo.body1">Two-Factor Authentication</MudText>
        <MudText Typo="Typo.caption" Color="Color.Secondary">
            Add an extra layer of security to your account
        </MudText>
    </MudStack>
    <MudChip T="string" Color="@(_isTwoFactorEnabled ? Color.Success : Color.Default)" 
             Size="Size.Small" Variant="Variant.Filled">
        @(_isTwoFactorEnabled ? "Enabled" : "Disabled")
    </MudChip>
</MudStack>
```

Replace with:
```razor
<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
    <MudStack Spacing="0">
        <MudText Typo="Typo.body1">Two-Factor Authentication</MudText>
        <MudText Typo="Typo.caption" Color="Color.Secondary">
            Add an extra layer of security to your account
        </MudText>
    </MudStack>
    <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
        <MudChip T="string" Color="@(_isTwoFactorEnabled ? Color.Success : Color.Default)"
                 Size="Size.Small" Variant="Variant.Filled">
            @(_isTwoFactorEnabled ? "Enabled" : "Disabled")
        </MudChip>
        @if (_isTwoFactorEnabled)
        {
            <MudButton Variant="Variant.Outlined" Color="Color.Error" Size="Size.Small"
                       OnClick="@(() => _disable2faDialogVisible = true)"
                       Disabled="_processing2fa">
                Disable
            </MudButton>
        }
        else
        {
            <MudButton Variant="Variant.Outlined" Color="Color.Primary" Size="Size.Small"
                       OnClick="InitiateEnable2faAsync"
                       Disabled="_processing2fa">
                @if (_processing2fa)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-1" />
                }
                Enable
            </MudButton>
        }
    </MudStack>
</MudStack>
```

#### 4b — Add two dialogs at the end of the razor markup (after the existing `<!-- Sign Out All Confirmation -->` dialog closing tag)

```razor
<!-- Enable 2FA Dialog -->
<MudDialog @bind-Visible="_enable2faDialogVisible"
           Options="@(new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = false })">
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            <MudIcon Icon="@Icons.Material.Filled.Security" Color="Color.Primary" />
            <MudText Typo="Typo.h6">Enable Two-Factor Authentication</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        @if (_enable2faStep == 1 && !string.IsNullOrEmpty(_enable2faQrCode))
        {
            <MudStack AlignItems="AlignItems.Center" Spacing="3">
                <MudText Typo="Typo.body2" Color="Color.Secondary" Align="Align.Center">
                    Scan this QR code with your authenticator app (e.g. Google Authenticator), then enter the 6-digit code below.
                </MudText>
                <MudPaper Elevation="2" Class="pa-3">
                    <img src="@($"data:image/png;base64,{_enable2faQrCode}")"
                         alt="2FA QR Code" style="width: 200px; height: 200px;" />
                </MudPaper>
                <MudTextField @bind-Value="_enable2faOtp"
                              Label="6-Digit Code"
                              Variant="Variant.Outlined"
                              MaxLength="6"
                              Placeholder="000000"
                              FullWidth="true"
                              Disabled="_processing2fa" />
                @if (!string.IsNullOrEmpty(_twoFaError))
                {
                    <MudAlert Severity="Severity.Error" Dense="true">@_twoFaError</MudAlert>
                }
            </MudStack>
        }
        else if (string.IsNullOrEmpty(_enable2faQrCode))
        {
            <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="CancelEnable2fa" Disabled="_processing2fa">Cancel</MudButton>
        @if (_enable2faStep == 1)
        {
            <MudButton Color="Color.Primary" Variant="Variant.Filled"
                       OnClick="ConfirmEnable2faAsync"
                       Disabled="_processing2fa || _enable2faOtp.Length != 6">
                @if (_processing2fa)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                }
                Verify & Enable
            </MudButton>
        }
    </DialogActions>
</MudDialog>

<!-- Disable 2FA Dialog -->
<MudDialog @bind-Visible="_disable2faDialogVisible"
           Options="@(new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true })">
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
            <MudIcon Icon="@Icons.Material.Filled.SecurityUpdateWarning" Color="Color.Warning" />
            <MudText Typo="Typo.h6">Disable Two-Factor Authentication</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mb-3">
            Enter the current 6-digit code from your authenticator app to confirm.
        </MudText>
        <MudTextField @bind-Value="_disable2faOtp"
                      Label="6-Digit Code"
                      Variant="Variant.Outlined"
                      MaxLength="6"
                      Placeholder="000000"
                      FullWidth="true"
                      Disabled="_processing2fa" />
        @if (!string.IsNullOrEmpty(_twoFaError))
        {
            <MudAlert Severity="Severity.Error" Dense="true" Class="mt-2">@_twoFaError</MudAlert>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="@(() => { _disable2faDialogVisible = false; _disable2faOtp = string.Empty; _twoFaError = null; })"
                   Disabled="_processing2fa">
            Cancel
        </MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled"
                   OnClick="Disable2faAsync"
                   Disabled="_processing2fa || _disable2faOtp.Length != 6">
            @if (_processing2fa)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
            }
            Disable 2FA
        </MudButton>
    </DialogActions>
</MudDialog>
```

#### 4c — Add private fields to `@code` block (place after existing `_signOutAllDialogVisible` field declaration)

```csharp
// 2FA management
private bool _enable2faDialogVisible;
private bool _disable2faDialogVisible;
private bool _processing2fa;
private int _enable2faStep;
private string _enable2faQrCode = string.Empty;
private string _enable2faOtp = string.Empty;
private string _disable2faOtp = string.Empty;
private string? _twoFaError;
```

#### 4d — Add three methods to `@code` block (place after `SignOutAllDevices` method)

```csharp
private async Task InitiateEnable2faAsync()
{
    if (AuthState.CurrentUser is null) return;

    _processing2fa = true;
    _twoFaError = null;
    _enable2faQrCode = string.Empty;
    _enable2faStep = 0;
    _enable2faOtp = string.Empty;
    _enable2faDialogVisible = true;
    StateHasChanged();

    try
    {
        var response = await AuthClient.EnableTwoFactorAsync(AuthState.CurrentUser.UserId);
        if (response.Success && response.Data?.QrCodeBase64 is not null)
        {
            _enable2faQrCode = response.Data.QrCodeBase64;
            _enable2faStep = 1;
        }
        else
        {
            _twoFaError = response.Message ?? "Failed to initiate 2FA setup.";
        }
    }
    catch (Exception ex)
    {
        _twoFaError = $"Error: {ex.Message}";
    }
    finally
    {
        _processing2fa = false;
        StateHasChanged();
    }
}

private async Task ConfirmEnable2faAsync()
{
    if (AuthState.CurrentUser is null) return;

    _processing2fa = true;
    _twoFaError = null;
    StateHasChanged();

    try
    {
        var request = new ValidateTwoFactorRequestDto { OtpCode = _enable2faOtp };
        var response = await AuthClient.ConfirmEnableTwoFactorAsync(AuthState.CurrentUser.UserId, request);
        if (response.Success)
        {
            _isTwoFactorEnabled = true;
            _enable2faDialogVisible = false;
            _enable2faOtp = string.Empty;
            _enable2faQrCode = string.Empty;
            Snackbar.Add("Two-factor authentication enabled successfully.", Severity.Success);
        }
        else
        {
            _twoFaError = response.Message ?? "Invalid code. Please try again.";
        }
    }
    catch (Exception ex)
    {
        _twoFaError = $"Error: {ex.Message}";
    }
    finally
    {
        _processing2fa = false;
        StateHasChanged();
    }
}

private void CancelEnable2fa()
{
    _enable2faDialogVisible = false;
    _enable2faOtp = string.Empty;
    _enable2faQrCode = string.Empty;
    _enable2faStep = 0;
    _twoFaError = null;
}

private async Task Disable2faAsync()
{
    if (AuthState.CurrentUser is null) return;

    _processing2fa = true;
    _twoFaError = null;
    StateHasChanged();

    try
    {
        var request = new DisableTwoFactorRequestDto { Code = _disable2faOtp };
        var response = await AuthClient.DisableTwoFactorAsync(AuthState.CurrentUser.UserId, request);
        if (response.Success)
        {
            _isTwoFactorEnabled = false;
            _disable2faDialogVisible = false;
            _disable2faOtp = string.Empty;
            Snackbar.Add("Two-factor authentication disabled.", Severity.Success);
        }
        else
        {
            _twoFaError = response.Message ?? "Invalid code. Please try again.";
        }
    }
    catch (Exception ex)
    {
        _twoFaError = $"Error: {ex.Message}";
    }
    finally
    {
        _processing2fa = false;
        StateHasChanged();
    }
}
```

---

## Entity Properties
N/A — no schema changes.

## Validation Rules
N/A — no new validation.

## Business Rules
N/A — no new business logic.

---

## Implementation Order

Apply changes in this order to avoid partial-broken states:

1. **`TenantsController.cs`** — Add `[Authorize]` + verify `using Microsoft.AspNetCore.Authorization;` is present
2. **`AuthApiClient.cs`** — Remove duplicate `using` line
3. **`TenantList.razor`** — Add `@bind-Selected:after="LoadTenantsAsync"` to `MudPagination`
4. **`Profile.razor`**:
   a. Replace 2FA row markup in Security section
   b. Add Enable 2FA dialog after the Sign-Out-All dialog closing tag
   c. Add Disable 2FA dialog immediately after the Enable 2FA dialog
   d. Add private fields in `@code`
   e. Add `InitiateEnable2faAsync`, `ConfirmEnable2faAsync`, `CancelEnable2fa`, `Disable2faAsync` methods

No migration required. No new projects or packages.

## Migration Name
None
