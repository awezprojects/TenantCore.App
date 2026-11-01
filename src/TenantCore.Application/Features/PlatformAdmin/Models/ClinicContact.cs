namespace TenantCore.Application.Features.PlatformAdmin.Models;

/// <summary>
/// The clinic + billing snapshot the Admin portal resolved from TenantCore.Auth. The App has no
/// clinic registry of its own, so this is how a grant or admin link learns who to bill and email.
///
/// Name is guaranteed non-empty by the translator (it falls back to ClinicName) because the
/// notification consumer dead-letters a message with a blank RecipientName without attempting
/// delivery — see the workspace CLAUDE.md, cross-repo rule 9.
/// </summary>
public sealed record ClinicContact(string ClinicName, string Name, string Email, string? Phone);
