# Security Analysis: Doctor Prescription Feature

**Date**: 2026-04-26
**Analyst**: Claude
**Plan**: plan/doctor-prescription/PLAN.md

## Summary

Overall risk level: **Low** (after fixes applied). The feature correctly enforces tenant isolation via `ApplicationId` from JWT on all read and write paths, uses EF Core throughout with no raw SQL, and applies appropriate authorization policies. Four issues (HTML injection in email, missing file size/extension validation, unbounded page size, PII in file paths) were identified and **resolved before merge**.

## Findings

### CRITICAL (must fix before merge)

- **[C1]** HTML Injection in Prescription Email
  - **Location**: `SubmitPrescriptionHandler.cs` — `BuildPrescriptionEmail()`
  - **Risk**: User-controlled fields (medicine name, remarks, notes, patient/doctor name) embedded in HTML without encoding. Attacker who creates a prescription with script payloads could achieve email-based XSS when the email is opened.
  - **Fix**: Applied `WebUtility.HtmlEncode()` to all user-supplied values in the email template. ✅ **Fixed**

- **[C2]** No File Size Limit on Report Upload
  - **Location**: `PrescriptionsController.cs` — `UploadReport()`
  - **Risk**: No upper bound on `IFormFile.Length`. Attacker uploads multi-GB files causing memory exhaustion and service unavailability.
  - **Fix**: Added `if (file.Length > MaxUploadBytes) return BadRequest(...)` with 50 MB cap before reading into memory. ✅ **Fixed**

- **[C3]** No File Extension Whitelist on Report Upload
  - **Location**: `PrescriptionsController.cs` — `UploadReport()`
  - **Risk**: Any file type accepted. While the handler wraps files in a PDF container, malicious executables or scripts could be stored on disk for later exploitation.
  - **Fix**: Added extension whitelist check (`{ ".pdf", ".jpg", ".jpeg", ".png", ".bmp" }`) before processing. ✅ **Fixed**

### HIGH (should fix before merge)

- **[H1]** Unbounded Page Size (DoS / Bulk Data Leakage)
  - **Location**: `GetPrescriptionsHandler.cs`, `GetDosageRemarksHandler.cs`
  - **Risk**: Caller passes `pageSize=999999`. Server fetches all records into memory — DoS and potential bulk exfiltration of patient data.
  - **Fix**: Added `Math.Min(request.PageSize, 100)` cap in both handlers. ✅ **Fixed**

- **[H2]** Patient PII in File Storage Path
  - **Location**: `UploadPrescriptionReportHandler.cs` — `patientFolder` variable
  - **Risk**: Patient full name was included in folder path (`{id}+{FirstName} {LastName}`), which was returned in `FilePath` field of the API response. API consumers (including logs) would expose patient names.
  - **Fix**: Changed `patientFolder` to `patient.Id.ToString()` — opaque GUID only. ✅ **Fixed**

- **[H3]** Patient Email Logged on Failure
  - **Location**: `SubmitPrescriptionHandler.cs` — catch block
  - **Risk**: Patient email address in structured log entry. Accessible to anyone with log access.
  - **Fix**: Replaced `{Email}` log parameter with `{Id}` (prescription ID only). ✅ **Fixed**

### MEDIUM (fix in follow-up)

- **[M1]** Placeholder PDF Generation for Non-PDF Files
  - **Location**: `PdfConversionService.cs`
  - **Risk**: The image-to-PDF conversion uses hand-crafted PDF syntax that may produce malformed PDFs or incorrect stream lengths. Not a direct security risk but could cause parsing failures or unexpected behavior in PDF readers.
  - **Fix**: Replace with a proper PDF library (e.g., iTextSharp, PdfPig) before production.

- **[M2]** SMTP Credentials in Configuration
  - **Location**: `EmailService.cs`
  - **Risk**: `Email:Username` and `Email:Password` read from `appsettings.json`. If the file is committed or exposed, credentials leak.
  - **Fix**: Store SMTP credentials in environment variables or Azure Key Vault. Never commit to source control.

- **[M3]** No Rate Limiting on File Upload Endpoint
  - **Location**: `PrescriptionsController.cs` — `POST /api/prescriptions/{id}/reports`
  - **Risk**: Authenticated attacker repeatedly uploads files to fill disk.
  - **Fix**: Add per-user rate limiting middleware (e.g., AspNetCoreRateLimit) on the upload endpoint.

### LOW / Informational

- **[L1]** `Dense` Attribute Warnings on MudTextField (MUD0002)
  - `Dense` is not a valid MudTextField parameter. Use `Margin="Margin.Dense"` instead. Low priority — cosmetic warning only.

- **[L2]** Hard-Delete on Dosage Remarks
  - `DeleteDosageRemarkHandler` performs a hard delete. If audit trails are required for compliance, implement a soft-delete pattern (`IsDeleted` flag + date).

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All endpoints `[Authorize]`. Writes protected with `RequireClinical`/`RequireClinicAdmin`. |
| Input Validation | PASS | FluentValidation on all commands. Page size capped at 100 (H1 fixed). File validation added (C2, C3 fixed). |
| Data Access & Injection | PASS | All DB queries via EF Core LINQ. No raw SQL. No string-concatenated queries. |
| Sensitive Data Handling | PASS | Email PII removed from logs (H3 fixed). SMTP creds in config (M2 — medium, follow-up). |
| Business Logic Security | PASS | Tenant isolation verified: `ApplicationId` from JWT on all paths. Prescription ownership checked via `ApplicationId` comparison before every mutation. |
| OWASP Top 10 | PASS | A01 (access control) ✅, A03 (injection/XSS) ✅ fixed via HtmlEncode, A07 (auth) ✅ |

## Recommended Actions

1. **(MEDIUM — before production)** Replace placeholder PDF conversion (`PdfConversionService`) with a proper library.
2. **(MEDIUM — before production)** Move SMTP credentials to environment variables / secret manager.
3. **(MEDIUM — follow-up)** Add per-user rate limiting on the file upload endpoint.
4. **(LOW — follow-up)** Fix MUD0002 warnings: replace `Dense="true"` on `MudTextField` with `Margin="Margin.Dense"`.
5. **(LOW — follow-up)** Consider soft-delete for `DosageRemark` if audit compliance is required.

## Approval

- [x] All CRITICAL findings resolved
- [x] All HIGH findings resolved
- [ ] M1 (PDF library) — accepted risk for initial release; stub clearly commented
- [ ] M2 (SMTP creds) — accepted risk for dev; must be resolved before production deployment
- [x] Ready to merge
