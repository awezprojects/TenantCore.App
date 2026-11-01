namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// The clinic + billing snapshot the Admin portal resolves from TenantCore.Auth and sends with
/// every grant or payment link. The App has no clinic registry of its own, so this is how it
/// learns who to bill and who to email.
///
/// ContactName must survive as non-empty all the way to the notification queue — a blank
/// RecipientName is dead-lettered without a send attempt (workspace CLAUDE.md, rule 9). The
/// translator falls back to ClinicName when it is blank.
/// </summary>
public record ClinicContactRequest
{
    public string ClinicName { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string ContactEmail { get; init; } = string.Empty;
    public string? ContactPhone { get; init; }
}
