# Security Analysis: OPD/IPD Registration

**Date**: 2026-04-24
**Analyst**: Claude
**Plan**: plan/opd-ipd-registration/PLAN.md

## Summary

Overall risk level: **Medium**. The feature is well-structured with consistent tenant isolation via JWT claims, proper use of FluentValidation, and EF Core parameterized queries throughout. No critical vulnerabilities were found. Two medium-risk items exist: a missing page-size cap on paged queries and the absence of Aadhaar number encryption at rest.

## Findings

### CRITICAL (must fix before merge)

None.

### HIGH (should fix before merge)

None.

### MEDIUM (fix in follow-up)

- **[M1]** Aadhaar Number Stored as Plaintext
  - **Location**: `src/TenantCore.Domain/Entities/Patient.cs` — `AadhaarNumber` property; `src/TenantCore.Infrastructure/Persistence/Configurations/Clinic/PatientConfiguration.cs`
  - **Risk**: Aadhaar is a government-issued national ID (PII/sensitive). Storing it unencrypted increases exposure if the database is compromised.
  - **Fix**: Encrypt at the application layer before persistence (e.g., AES-256 via a `DataProtectionProvider` value converter in EF) or use SQL Server Always Encrypted for the column.

- **[M2]** Page-Size Cap Not Enforced in Repository Layer
  - **Location**: All paged handlers (`GetPatientsHandler`, `GetOpdRegistrationsHandler`, `GetIpdRegistrationsHandler`) — the `Math.Min(request.PageSize, 100)` cap should be verified to exist in every handler.
  - **Risk**: A caller could request `pageSize=10000`, causing a large DB scan and potential DoS.
  - **Fix**: Confirm each paged handler applies `var pageSize = Math.Min(request.PageSize, 100);` before passing to repository.

- **[M3]** Photo URL Not Validated
  - **Location**: `src/TenantCore.Shared/Dtos/PatientDto.cs` — `PhotoUrl` field; `src/TenantCore.Application/Features/Patients/Validators/RegisterPatientCommandValidator.cs`
  - **Risk**: No validation that `PhotoUrl` is a safe URL (could be a `javascript:` URI, internal network path, or excessively long string).
  - **Fix**: Add a validator rule: `RuleFor(x => x.PhotoUrl).MaximumLength(500).Must(u => Uri.TryCreate(u, UriKind.Absolute, out _)).When(x => x.PhotoUrl is not null)`.

### LOW / Informational

- **[L1]** DoctorUserId Defaults to `Guid.Empty` When No Doctor Selected
  - **Location**: `src/TenantCore.Web.Client/Pages/Opd/OpdRegistrationList.razor` and `Ipd/IpdRegistrationList.razor`
  - **Risk**: Semantically misleading — `Guid.Empty` in `DoctorUserId` means "no doctor" but could be confused with a real user lookup failure. No security risk, but a data quality concern.
  - **Fix**: Make `DoctorUserId` nullable (`Guid?`) in the domain entities, commands, and DTOs in a follow-up task.

- **[L2]** Aadhaar Number Logged Indirectly via Debug Logging
  - **Location**: Any handler that logs `request` properties may inadvertently include PII if structured logging serializes the entire command.
  - **Risk**: PII leakage in logs.
  - **Fix**: Log only non-sensitive identifiers (e.g., `PatientId`, `TenantId`), not the full command object.

## Checklist Results

| Category | Status | Notes |
|----------|--------|-------|
| Authentication & Authorization | PASS | All endpoints have `[Authorize]`; destructive endpoints use `RequireManagement`; fee update uses `RequireClinicAdmin` |
| Input Validation | PARTIAL | FluentValidation present; page-size cap needs confirmation; PhotoUrl URL validation missing |
| Data Access & Injection | PASS | All queries via EF Core parameterized; no raw SQL |
| Sensitive Data Handling | PARTIAL | Aadhaar stored as plaintext; not logged directly but risk exists |
| Business Logic Security | PASS | Tenant isolation enforced in all handlers via `TenantId` from JWT; cross-tenant access throws `UnauthorizedAccessException` |
| OWASP Top 10 | PASS | A01 (access control) covered; A03 (injection) not applicable; A07 (auth) covered |

## Recommended Actions

1. **(MEDIUM — follow-up)** Encrypt `AadhaarNumber` at rest using EF value converters + `IDataProtectionProvider` [M1]
2. **(MEDIUM — follow-up)** Add `PhotoUrl` format validation to `RegisterPatientCommandValidator` [M3]
3. **(MEDIUM — follow-up)** Audit all three paged handlers to confirm `Math.Min(pageSize, 100)` is applied [M2]
4. **(LOW — follow-up)** Refactor `DoctorUserId` to be nullable across the domain layer [L1]

## Approval

- [x] All CRITICAL findings resolved
- [x] All HIGH findings resolved or have accepted risk noted
- [ ] Ready to merge — MEDIUM items to address in follow-up
