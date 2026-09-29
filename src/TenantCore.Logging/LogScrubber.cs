using System.Text.RegularExpressions;

namespace TenantCore.Logging;

/// <summary>
/// Defence in depth for everything written to the log store: masks connection-string secrets,
/// bearer tokens, signatures and service keys that could ride along in an exception message, and
/// bounds the length (a table string property holds at most 32K characters). Callers never log
/// passwords, OTPs, tokens or request bodies in the first place — this only catches accidents.
/// Same rules as TenantCore.Admin's LogScrubber.
/// </summary>
public static partial class LogScrubber
{
    public const string Mask = "***";

    [GeneratedRegex(@"(?i)\b(password|pwd|accountkey|sharedaccesskey|sharedaccesssignature|secret|servicekey|keysecret|otp)\s*=\s*[^;\s""'&]+")]
    private static partial Regex KeyValueSecret();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9\-\._~\+/]+=*")]
    private static partial Regex BearerToken();

    [GeneratedRegex(@"(?i)((?:X-Internal-Service-Key|X-Razorpay-Signature|Authorization)\s*[:=]\s*)\S+")]
    private static partial Regex SecretHeader();

    public static string? Scrub(string? value, int maxLength = 30000)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var scrubbed = KeyValueSecret().Replace(value, m => $"{m.Groups[1].Value}={Mask}");
        scrubbed = BearerToken().Replace(scrubbed, $"Bearer {Mask}");
        scrubbed = SecretHeader().Replace(scrubbed, m => $"{m.Groups[1].Value}{Mask}");

        return scrubbed.Length <= maxLength ? scrubbed : scrubbed[..maxLength];
    }
}
